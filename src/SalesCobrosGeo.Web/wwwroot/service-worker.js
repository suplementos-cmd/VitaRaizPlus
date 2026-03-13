const CACHE_NAME = 'vrp-shell-v1';
const APP_SHELL = [
  '/',
  '/Account/Login',
  '/lib/bootstrap/dist/css/bootstrap.min.css',
  '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
  '/lib/jquery/dist/jquery.min.js',
  '/css/site.css',
  '/css/modules/design-tokens.css',
  '/css/modules/layout.css',
  '/css/modules/dashboard.css',
  '/css/modules/sales.css',
  '/css/modules/collections.css',
  '/css/modules/maintenance.css',
  '/css/modules/administration.css',
  '/css/modules/security.css',
  '/js/site.js'
];

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME).then((cache) => cache.addAll(APP_SHELL))
  );
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((keys) => Promise.all(keys.filter((key) => key !== CACHE_NAME).map((key) => caches.delete(key))))
  );
});

self.addEventListener('fetch', (event) => {
  const request = event.request;
  if (request.method !== 'GET') {
    return;
  }

  event.respondWith(
    caches.match(request).then((cached) => cached || fetch(request).then((response) => {
      const copy = response.clone();
      caches.open(CACHE_NAME).then((cache) => cache.put(request, copy));
      return response;
    }).catch(() => caches.match('/')))
  );
});
