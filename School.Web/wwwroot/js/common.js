/* Where the browser finds the API.
 *
 * Over HTTPS this MUST be a same-origin path. Cloudflare only proxies a fixed
 * set of ports and 5266 is not one of them, so "https://host:5266/api" can
 * never be reached from outside; and a page served over https is not allowed
 * to call an http address at all — the browser blocks it as mixed content.
 * IIS therefore proxies /api through to localhost:5266 (URL Rewrite + ARR),
 * which also means no CORS, one certificate and one open port.
 *
 * Over plain HTTP — a developer machine, or a LAN address during the move to
 * HTTPS — there is no proxy in front, so talk to the API's own port directly.
 * Once everything is behind https this branch simply stops being used.
 */
const isLocalDev = window.location.hostname === 'localhost'
                || window.location.hostname === '127.0.0.1';

// Development: School.API runs as its own process with nothing proxying in
// front of it, so address it directly on 5266 — which BOTH of its launch
// profiles bind ("http://localhost:5266" and
// "https://localhost:7191;http://localhost:5266"), so this holds whichever one
// is running. Port 7191 does not, which is why it must not be used here.
//
// 127.0.0.1 and NOT localhost, deliberately: appsettings.json binds the API to
// "http://0.0.0.0:5266", which is IPv4 only. On Windows localhost resolves to
// ::1 first, and Chrome does not fall back to IPv4 — it just reports
// ERR_CONNECTION_REFUSED. curl does fall back, so the API looks reachable from
// a terminal while the browser cannot touch it. (Binding the API to
// "http://*:5266" would cover both families and make localhost work too.)
//
// An https dev page may call this without tripping the mixed-content block:
// 127.0.0.1 counts as a trustworthy origin. CORS is satisfied by the API's
// AllowAll policy.
const API_BASE = isLocalDev
    ? 'http://127.0.0.1:5266/api'
    : (window.location.protocol === 'https:'
        ? '/api'
        : `http://${window.location.hostname}:5266/api`);

// Session values (injected from _DashboardLayout.cshtml)
let USER_ID = 0;
let SESSION_ID = 0;

// The API refuses calls without the signed-in user's token (a short JWT the
// web server signs - School.Web/Services/ApiToken.cs). Dashboard pages get it
// from the layout; a print page that loads common.js on its own fetches it
// from the keep-alive ping before its first API call. The ping renews it.
let API_TOKEN = '';
const _originalFetch = window.fetch;
const _PING_URL = '/EasySchool/session/ping';
let _tokenRequest = null;

function refreshApiToken() {
    if (!_tokenRequest) {
        _tokenRequest = _originalFetch.call(window, _PING_URL, { credentials: 'same-origin', cache: 'no-store' })
            .then(function (r) { return r.ok ? r.json() : null; })
            .then(function (j) { if (j && j.token) API_TOKEN = j.token; return API_TOKEN; })
            .catch(function () { return API_TOKEN; })
            .finally(function () { _tokenRequest = null; });
    }
    return _tokenRequest;
}

function _isApiUrl(url) {
    const href = typeof url === 'string' ? url : (url instanceof URL ? url.href : '');
    return !!href && href.startsWith(API_BASE);
}

function _withApiHeaders(headers) {
    const h = new Headers(headers || {});
    if (API_TOKEN) h.set('Authorization', 'Bearer ' + API_TOKEN);
    if (USER_ID) h.set('X-User-Id', String(USER_ID));
    return h;
}

// Every fetch to the API carries the token. A 401 means the token went stale
// (a tab left open for hours): get a fresh one and try once more; still 401
// means the sign-in itself has ended, so go back to the sign-in page.
window.fetch = async function (url, options) {
    options = options || {};
    if (!_isApiUrl(url)) return _originalFetch.call(this, url, options);
    if (!API_TOKEN) await refreshApiToken();
    const send = () => _originalFetch.call(window, url, Object.assign({}, options, { headers: _withApiHeaders(options.headers) }));
    let res = await send();
    if (res.status === 401) {
        const before = API_TOKEN;
        await refreshApiToken();
        if (API_TOKEN && API_TOKEN !== before) res = await send();
        if (res.status === 401) window.location.href = '/EasySchool/login';
    }
    return res;
};

// The same for jQuery's $.ajax (a few pages use it).
document.addEventListener('DOMContentLoaded', function () {
    // A print page has no token in the page: fetch it now, before anything is clicked.
    if (!API_TOKEN) refreshApiToken();
    if (!window.jQuery) return;
    jQuery.ajaxPrefilter(function (opts, _orig, xhr) {
        if (_isApiUrl(opts.url) && API_TOKEN) {
            xhr.setRequestHeader('Authorization', 'Bearer ' + API_TOKEN);
            if (USER_ID) xhr.setRequestHeader('X-User-Id', String(USER_ID));
        }
    });
});

// For what the browser opens by itself and so cannot send a header with -
// <img src>, window.open of a PDF or Excel file: the token rides in the URL.
function apiFileUrl(url) {
    if (!API_TOKEN || !_isApiUrl(url)) return url;
    return url + (url.indexOf('?') >= 0 ? '&' : '?') + 'access_token=' + encodeURIComponent(API_TOKEN);
}

// Keep the MVC session alive while the tab is open — AJAX API calls don't
// touch it — and the API token fresh.
(function () {
    function ping() {
        if (document.hidden) return;
        refreshApiToken();
    }
    setInterval(ping, 5 * 60 * 1000);
    document.addEventListener('visibilitychange', function () { if (!document.hidden) ping(); });
})();

/* ---------------------------------------------------------------------------
   Date / time helpers

   The API sends two shapes, and they are read back differently on purpose:

     instant   "2026-08-27T09:00:00.000Z"  — a moment, always UTC, with the "Z".
                new Date() converts it to the machine's timezone for free, so
                fmtDateTime/fmtTime just format it.

     calendar  "2026-08-27"                — a date on a calendar, no timezone.
                new Date("2026-08-27") parses as UTC midnight per spec, which
                renders as the PREVIOUS day anywhere west of UTC. fmtDate goes
                through parseCalendarDate instead, which builds the date from
                its parts at local midnight so it can never shift.

   toInputDate replaces `new Date().toISOString().split('T')[0]`, which is a
   UTC date: at UTC+5 it returns yesterday between 00:00 and 05:00 local.
   --------------------------------------------------------------------------- */

// Builds a Date at LOCAL midnight from a calendar date, with no zone maths.
function parseCalendarDate(value) {
    if (value === null || value === undefined || value === '') return null;
    if (value instanceof Date) {
        return isNaN(value) ? null : new Date(value.getFullYear(), value.getMonth(), value.getDate());
    }

    const parts = String(value).match(/^(\d{4})-(\d{2})-(\d{2})/);
    if (parts) return new Date(+parts[1], +parts[2] - 1, +parts[3]);

    const parsed = new Date(value);
    return isNaN(parsed)
        ? null
        : new Date(parsed.getFullYear(), parsed.getMonth(), parsed.getDate());
}

// The month names the whole application prints, spelled out here rather than
// taken from the browser. A machine set to another language would otherwise
// render "ago" or "أغسطس" on a sheet that goes to the department.
const MONTH_ABBR = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
                    'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

// Instant -> "16-Aug-2026, 3:45 pm" in the viewer's timezone.
function fmtDateTime(value, options) {
    if (!value) return '';
    const d = new Date(value);
    if (isNaN(d)) return '';
    if (options) return d.toLocaleString(undefined, options);

    return `${fmtDate(d)}, ${d.toLocaleTimeString('en-GB', { hour: 'numeric', minute: '2-digit', hour12: true })}`;
}

// Instant -> time only, in the viewer's timezone.
function fmtTime(value, options) {
    if (!value) return '';
    const d = new Date(value);
    return isNaN(d) ? '' : d.toLocaleTimeString(undefined, options);
}

// Calendar date -> dd-MMM-yyyy, e.g. 16-Aug-1970. THE date format of this
// application: every screen, every report, every printed sheet.
//
// Never shifts, and never changes with the machine it is read on. This used to
// hand the date to the BROWSER's locale, so the same sheet read 16/08/1970 on
// one computer and 8/16/1970 on the next, from nothing but a Windows region
// setting. The month is spelled, which also settles that question for good:
// 08-09-2026 is ambiguous to a reader, 08-Sep-2026 is not.
//
// Pass `options` to ask for something else deliberately; an explicit request
// still goes through toLocaleDateString as before.
function fmtDate(value, options) {
    const d = parseCalendarDate(value);
    if (!d) return '';
    if (options) return d.toLocaleDateString(undefined, options);

    return `${String(d.getDate()).padStart(2, '0')}-${MONTH_ABBR[d.getMonth()]}-${d.getFullYear()}`;
}

// LOCAL yyyy-MM-dd, for <input type="date">. Called with no argument it means
// "today"; called with an empty value it means empty, so a blank date coming
// back from the API cannot silently become today.
function toInputDate(value) {
    if (value === undefined) value = new Date();
    if (value === null || value === '') return '';

    const d = value instanceof Date ? value : parseCalendarDate(value);
    if (!d || isNaN(d)) return '';

    const pad = n => String(n).padStart(2, '0');
    return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate());
}

/* ---------------------------------------------------------------------------
   Double-submit guard

   Records were being saved twice because nothing stopped a second click while
   the first save was still in flight. Every save in the app is a plain
   `<button onclick="saveX()">` and only a handful of screens had their own
   disable/re-enable code, so rather than edit ~40 near-identical save
   functions this hooks the two things they all have in common:

     click  ->  the button that was pressed is "armed" for a moment
     fetch  ->  a POST/PUT/PATCH/DELETE started while a button is armed
                belongs to that button

   The button is disabled for as long as its writes are in flight and released
   a beat after the last one settles — i.e. as the success/error toast appears.
   A native (non-AJAX) form post disables its button until the page navigates.

   Consequences worth knowing:
     - A save that fails validation and returns before calling fetch never
       locks anything, so the user can fix the input and press Save again.
     - A screen that disabled the button itself keeps ownership of it: the
       guard re-enables only buttons it disabled, so the two can't fight.
     - MAX_BUSY_MS is a safety net — a request that never settles releases the
       button instead of leaving the screen stuck for good.
     - Opt out with data-no-submit-guard on the button or the form.
   --------------------------------------------------------------------------- */
(function () {
    // How long after a click a write still counts as "started by that click".
    // Every save calls fetch synchronously, so this only needs to cover the
    // few that await something first.
    const ARM_WINDOW_MS = 1000;
    // Grace after the last write settles, so a save that posts a header then
    // its details doesn't flicker between the two, and so the button is still
    // disabled while the toast is being raised.
    const SETTLE_MS = 400;
    // Hard release. A hung request must not brick the button forever.
    const MAX_BUSY_MS = 60000;

    const WRITE_METHOD = /^(POST|PUT|PATCH|DELETE)$/i;
    const CLICKABLE = 'button, input[type="submit"], input[type="button"], a[role="button"], a.btn';

    let armedEl = null;
    let armedAt = 0;

    // Injected here rather than in site.css, which the dashboard layout does
    // not load — this keeps the whole guard in one file.
    (function injectStyle() {
        const style = document.createElement('style');
        style.textContent =
            '.is-submitting{cursor:progress!important;opacity:.75;}' +
            'a.is-submitting{pointer-events:none;}';
        (document.head || document.documentElement).appendChild(style);
    })();

    function armedTarget() {
        if (!armedEl) return null;
        if (Date.now() - armedAt > ARM_WINDOW_MS) { armedEl = null; return null; }
        return armedEl;
    }

    function isBusy(el) {
        return !!el && el.hasAttribute && el.hasAttribute('data-submit-busy');
    }

    function lock(el) {
        if (!el || isBusy(el)) return;

        el.setAttribute('data-submit-busy', '1');
        el._guardPending = 0;

        if ('disabled' in el) {
            // Only re-enable later what we actually disabled — a screen that
            // manages the button itself (Student form, FeeVoucher) already set
            // this, and its own finally-block will clear it.
            el._guardDisabledByGuard = !el.disabled;
            el.disabled = true;
        } else {
            // <a> has no disabled property.
            el._guardDisabledByGuard = true;
            el.setAttribute('aria-disabled', 'true');
            el.style.pointerEvents = 'none';
        }

        el.classList.add('is-submitting');

        // Prepend a spinner rather than replacing the label, so the button
        // keeps its own icon and text and nothing shifts when it is released.
        // Skipped when the screen already put a spinner there itself.
        if (el.tagName === 'BUTTON' && !el.querySelector('.spinner-border')) {
            const spinner = document.createElement('span');
            spinner.className = 'spinner-border spinner-border-sm me-1 js-submit-spinner';
            spinner.setAttribute('role', 'status');
            spinner.setAttribute('aria-hidden', 'true');
            el.insertBefore(spinner, el.firstChild);
        }

        el._guardTimer = setTimeout(function () { release(el); }, MAX_BUSY_MS);
    }

    function release(el) {
        if (!isBusy(el)) return;

        clearTimeout(el._guardTimer);
        el.removeAttribute('data-submit-busy');
        el._guardPending = 0;

        if (el._guardDisabledByGuard) {
            if ('disabled' in el) {
                el.disabled = false;
            } else {
                el.removeAttribute('aria-disabled');
                el.style.pointerEvents = '';
            }
        }
        el._guardDisabledByGuard = false;

        el.classList.remove('is-submitting');

        const spinner = el.querySelector && el.querySelector('.js-submit-spinner');
        if (spinner) spinner.remove();
    }

    // Arm on the way down, before any onclick handler runs, and swallow a
    // repeat click on a button that is still saving.
    document.addEventListener('click', function (e) {
        const el = e.target && e.target.closest ? e.target.closest(CLICKABLE) : null;
        if (!el || el.hasAttribute('data-no-submit-guard')) return;

        if (isBusy(el)) {
            e.preventDefault();
            e.stopPropagation();
            e.stopImmediatePropagation();
            return;
        }

        armedEl = el;
        armedAt = Date.now();
    }, true);

    function submitterOf(e, form) {
        return e.submitter || form.querySelector(
            'button[type="submit"], input[type="submit"], button:not([type])');
    }

    // Pressing Enter in a field submits the form without ever firing a click,
    // so arm here too — in the capture phase, before the form's own handler
    // runs and calls fetch — and drop a repeat submit while one is in flight.
    document.addEventListener('submit', function (e) {
        const form = e.target;
        if (!form || !form.hasAttribute || form.hasAttribute('data-no-submit-guard')) return;

        const btn = submitterOf(e, form);
        if (!btn || btn.hasAttribute('data-no-submit-guard')) return;

        if (isBusy(btn)) {
            e.preventDefault();
            e.stopImmediatePropagation();
            return;
        }

        armedEl = btn;
        armedAt = Date.now();
    }, true);

    // Nothing cancelled it, so this form is posting for real: it navigates
    // away, and the button stays disabled until the new page loads.
    document.addEventListener('submit', function (e) {
        if (e.defaultPrevented) return;   // AJAX form — the fetch hook has it

        const form = e.target;
        if (!form || !form.hasAttribute || form.hasAttribute('data-no-submit-guard')) return;

        const btn = submitterOf(e, form);
        if (!btn || btn.hasAttribute('data-no-submit-guard')) return;

        lock(btn);
    });

    // Coming back via the browser's back button restores the page from the
    // bfcache with the button still mid-save. Clear any leftovers.
    window.addEventListener('pageshow', function () {
        document.querySelectorAll('[data-submit-busy]').forEach(release);
    });

    const _guardedFetch = window.fetch;
    window.fetch = function (input, init) {
        const method =
            (init && init.method) ||
            (input && typeof input === 'object' && input.method) ||
            'GET';

        const owner = WRITE_METHOD.test(method) ? armedTarget() : null;
        if (!owner) return _guardedFetch.apply(this, arguments);

        lock(owner);
        owner._guardPending = (owner._guardPending || 0) + 1;

        const settled = function () {
            owner._guardPending = Math.max(0, (owner._guardPending || 0) - 1);
            if (owner._guardPending > 0) return;
            setTimeout(function () {
                if (!owner._guardPending) release(owner);
            }, SETTLE_MS);
        };

        return _guardedFetch.apply(this, arguments).then(
            function (response) { settled(); return response; },
            function (error) { settled(); throw error; }
        );
    };

    // For the rare save that needs to hold or drop the lock by hand.
    window.SubmitGuard = { lock: lock, release: release, isBusy: isBusy };
})();
