/*
 * FamilyHub service worker.
 *
 * Goals:
 *  - Make the app installable and give it an offline-capable app shell.
 *  - Never interfere with API access: /api requests always go straight to the network,
 *    so authentication and data stay fresh and private responses are never cached.
 *  - Keep client-side deep links working offline by falling back to the cached shell,
 *    and show a branded offline page when even the shell is unavailable.
 *  - Support user-controlled updates: a new worker waits until the app tells it to
 *    activate (see the SKIP_WAITING message), which powers the "new version" prompt.
 *  - Receive push notifications and route the app to the right page when one is tapped.
 */

// Bump this whenever the precached shell changes so clients pick up a new worker.
const CACHE = 'familyhub-v3';

// The minimal shell needed to boot the app (and an offline fallback). Hashed build assets
// under /assets are cached at runtime instead (their names are not known ahead of time).
const APP_SHELL = [
  '/',
  '/index.html',
  '/offline.html',
  '/favicon.svg',
  '/manifest.webmanifest',
  '/icons/icon-192.png',
  '/icons/icon-512.png',
];

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches
      .open(CACHE)
      // Ignore individual failures so a single missing asset doesn't abort installation.
      .then((cache) => Promise.allSettled(APP_SHELL.map((url) => cache.add(url)))),
  );
  // NB: no skipWaiting() here. A freshly installed worker stays in "waiting" while the old
  // one still controls the page; that waiting state is what the app surfaces as an available
  // update. The user activates it on demand via the SKIP_WAITING message below.
});

self.addEventListener('message', (event) => {
  if (event.data && event.data.type === 'SKIP_WAITING') {
    self.skipWaiting();
  }
});

// --- Push notifications ---

self.addEventListener('push', (event) => {
  // Payload shape from the backend: { title, body, url, tag }.
  let payload = {};
  if (event.data) {
    try {
      payload = event.data.json();
    } catch {
      payload = { body: event.data.text() };
    }
  }

  const title = payload.title || 'FamilyHub';
  const options = {
    body: payload.body || '',
    icon: '/icons/icon-192.png',
    badge: '/icons/icon-192.png',
    // Same tag collapses/replaces a prior notification; unique tag shows each separately.
    tag: payload.tag || undefined,
    data: { url: payload.url || '/' },
  };

  event.waitUntil(self.registration.showNotification(title, options));
});

self.addEventListener('notificationclick', (event) => {
  event.notification.close();

  const targetPath = (event.notification.data && event.notification.data.url) || '/';
  const targetUrl = new URL(targetPath, self.location.origin).href;

  event.waitUntil(
    (async () => {
      const clientList = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
      const existing = clientList.find((client) => new URL(client.url).origin === self.location.origin);

      if (existing) {
        await existing.focus();
        // Ask the running app to route in place (smooth SPA navigation, no reload).
        existing.postMessage({ type: 'PUSH_NAVIGATE', url: targetPath });
        return;
      }

      // No window open — open one directly at the target route.
      await self.clients.openWindow(targetUrl);
    })(),
  );
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches
      .keys()
      .then((keys) => Promise.all(keys.filter((key) => key !== CACHE).map((key) => caches.delete(key))))
      .then(() => self.clients.claim()),
  );
});

self.addEventListener('fetch', (event) => {
  const { request } = event;

  // Only ever handle GETs; mutations must always hit the network.
  if (request.method !== 'GET') {
    return;
  }

  const url = new URL(request.url);

  // Leave cross-origin requests (e.g. a dev API on another port) to the browser.
  if (url.origin !== self.location.origin) {
    return;
  }

  // Never cache API traffic — always go to the network so data and auth stay current.
  if (url.pathname.startsWith('/api')) {
    return;
  }

  // App navigations (including refresh on a deep link): try the network first, then fall back
  // to the cached shell so the SPA can still boot offline and resolve the route client-side.
  // If even the shell isn't cached, show the branded offline page.
  if (request.mode === 'navigate') {
    event.respondWith(
      fetch(request).catch(() =>
        caches
          .match('/index.html')
          .then((shell) => shell || caches.match('/offline.html')),
      ),
    );
    return;
  }

  // Static assets: cache-first, then network (caching cacheable same-origin build output).
  event.respondWith(
    caches.match(request).then((cached) => {
      if (cached) {
        return cached;
      }
      return fetch(request).then((response) => {
        const isCacheable =
          response.ok &&
          (url.pathname.startsWith('/assets') ||
            url.pathname.startsWith('/icons') ||
            url.pathname === '/favicon.svg' ||
            url.pathname === '/manifest.webmanifest');
        if (isCacheable) {
          const copy = response.clone();
          caches.open(CACHE).then((cache) => cache.put(request, copy));
        }
        return response;
      });
    }),
  );
});
