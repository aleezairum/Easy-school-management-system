// For Live
const API_BASE = 'http://68.178.166.149:5266/api';
//const API_BASE = 'http://192.168.1.7:5266/api';

// For Local
//const API_BASE = 'http://localhost:5266/api';

// Session values (injected from _DashboardLayout.cshtml)
let USER_ID = 0;
let SESSION_ID = 0;

// Auto-attach X-User-Id header to all API fetch calls
const _originalFetch = window.fetch;
window.fetch = function (url, options = {}) {
    if (typeof url === 'string' && url.startsWith(API_BASE) && USER_ID) {
        options.headers = Object.assign({}, options.headers, {
            'X-User-Id': String(USER_ID)
        });
    }
    return _originalFetch.call(this, url, options);
};

// Keep MVC session alive while the tab is open — AJAX API calls don't touch it.
(function () {
    function ping() {
        if (document.hidden) return;
        fetch('/EasySchool/session/ping', { credentials: 'same-origin', cache: 'no-store' })
            .catch(function () { });
    }
    setInterval(ping, 5 * 60 * 1000);
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

// Instant -> date + time in the viewer's timezone.
function fmtDateTime(value, options) {
    if (!value) return '';
    const d = new Date(value);
    return isNaN(d) ? '' : d.toLocaleString(undefined, options);
}

// Instant -> time only, in the viewer's timezone.
function fmtTime(value, options) {
    if (!value) return '';
    const d = new Date(value);
    return isNaN(d) ? '' : d.toLocaleTimeString(undefined, options);
}

// Calendar date -> date only. Never shifts.
function fmtDate(value, options) {
    const d = parseCalendarDate(value);
    return d ? d.toLocaleDateString(undefined, options) : '';
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
