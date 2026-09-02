const CACHE_NAME = 'parksentry-shell-v1';
const SHELL_ASSETS = [
    '/',
    '/app.css',
    '/manifest.json',
    '/icons/icon-192.svg',
    '/icons/icon-512.svg',
    '/icons/maskable-icon.svg'
];

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => cache.addAll(SHELL_ASSETS)).then(() => self.skipWaiting())
    );
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(keys =>
            Promise.all(keys.filter(k => k !== CACHE_NAME).map(k => caches.delete(k)))
        ).then(() => self.clients.claim())
    );
});

self.addEventListener('fetch', event => {
    if (event.request.method !== 'GET') return;

    const url = new URL(event.request.url);
    // Network-first for API/hub; cache-first for shell assets
    if (url.pathname.startsWith('/api/') || url.pathname.startsWith('/hubs/')) {
        return;
    }

    if (SHELL_ASSETS.some(a => url.pathname === a || url.pathname.endsWith(a))) {
        event.respondWith(
            caches.match(event.request).then(cached => cached || fetch(event.request))
        );
    }
});
