// For Live
//const API_BASE = 'http://68.178.166.149:5266/api';
//const API_BASE = 'http://192.168.100.30:5266/api';

// For Local
const API_BASE = 'http://localhost:5266/api';

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
