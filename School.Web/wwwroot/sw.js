/* ============================================================================
   EasySchool Portal - service worker (installed app on a phone or PC).

   Pages and data always come from the server: a school portal must never show
   yesterday's fees or attendance from a cache. The look of the site (CSS, JS,
   fonts, pictures) is kept as a spare for a weak connection, and when there is
   no internet a page says so instead of the browser's error.

   Raise VERSION when this file changes; old caches are then removed.
   ============================================================================ */
const VERSION = 'easyschool-v1';
const OFFLINE = '/EasySchool/offline.html';

self.addEventListener('install', event => {
    event.waitUntil(caches.open(VERSION).then(c => c.addAll([OFFLINE, '/EasySchool/images/pwa-192.png'])).then(() => self.skipWaiting()));
});

self.addEventListener('activate', event => {
    event.waitUntil(caches.keys()
        .then(keys => Promise.all(keys.filter(k => k !== VERSION).map(k => caches.delete(k))))
        .then(() => self.clients.claim()));
});

const isAsset = url => /\.(css|js|woff2?|ttf|png|jpe?g|gif|svg|ico)$/i.test(url.pathname)
    && (url.pathname.startsWith('/EasySchool/') || url.hostname !== self.location.hostname);

self.addEventListener('fetch', event => {
    const req = event.request;
    if (req.method !== 'GET') return;
    const url = new URL(req.url);

    // A page: always from the server; the offline page only when there is no network.
    if (req.mode === 'navigate') {
        event.respondWith(fetch(req).catch(() => caches.match(OFFLINE)));
        return;
    }

    // The site's look: from the server first (a new release must never run an old
    // script against the new API), the cached copy only when there is no network.
    // The API (data) is never cached.
    if (isAsset(url) && !url.pathname.startsWith('/api/')) {
        event.respondWith(caches.open(VERSION).then(cache =>
            fetch(req).then(res => {
                if (res.ok && (res.type === 'basic' || res.type === 'cors')) cache.put(req, res.clone());
                return res;
            }).catch(() => cache.match(req))
        ));
    }
});
