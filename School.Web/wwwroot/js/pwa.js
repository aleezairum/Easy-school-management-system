/* ============================================================================
   pwa.js - the portal as an installed app.

   Registers the service worker (sw.js) and shows every element marked
   [data-install-app] while Chrome can install the portal; tapping one opens
   Chrome's own install dialog. Already installed (or a browser that cannot
   install) - the buttons stay hidden.
   ============================================================================ */
(function () {
    if ('serviceWorker' in navigator) {
        window.addEventListener('load', function () {
            navigator.serviceWorker.register('/EasySchool/sw.js', { scope: '/EasySchool/' }).catch(function () { });
        });
    }

    var deferred = null;
    function show(on) {
        document.querySelectorAll('[data-install-app]').forEach(function (el) { el.classList.toggle('d-none', !on); });
    }

    window.addEventListener('beforeinstallprompt', function (e) {
        e.preventDefault();           // our own button instead of Chrome's mini bar
        deferred = e;
        show(true);
    });

    window.addEventListener('appinstalled', function () {
        deferred = null;
        show(false);
        if (typeof toastSuccess === 'function') toastSuccess('EasySchool is installed. Open it from your home screen.');
    });

    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-install-app]');
        if (!btn) return;
        e.preventDefault();
        if (!deferred) return;
        deferred.prompt();
        deferred.userChoice.then(function () { deferred = null; show(false); });
    });

    document.addEventListener('DOMContentLoaded', function () { show(!!deferred); });
})();
