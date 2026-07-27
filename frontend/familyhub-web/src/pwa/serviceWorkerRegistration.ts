/*
 * Service worker registration + update detection.
 *
 * Registers /sw.js and watches for a newer worker. When an update finishes installing and is
 * waiting to activate, subscribers (the "new version available" UI) are notified. Calling
 * applyUpdate() tells the waiting worker to activate; once it takes control the page reloads
 * once so the latest assets are used.
 */

type UpdateListener = () => void;

let waitingWorker: ServiceWorker | null = null;
const listeners = new Set<UpdateListener>();
let reloading = false;

/** Subscribe to "an update is ready". Fires immediately if one is already waiting. */
export function onUpdateAvailable(listener: UpdateListener): () => void {
  listeners.add(listener);
  if (waitingWorker) {
    listener();
  }
  return () => {
    listeners.delete(listener);
  };
}

/** Activate the waiting worker. The subsequent controllerchange reloads the page. */
export function applyUpdate(): void {
  waitingWorker?.postMessage({ type: 'SKIP_WAITING' });
}

function markWaiting(worker: ServiceWorker): void {
  waitingWorker = worker;
  listeners.forEach((listener) => listener());
}

export function registerServiceWorker(): void {
  if (!('serviceWorker' in navigator)) {
    return;
  }

  navigator.serviceWorker
    .register('/sw.js')
    .then((registration) => {
      // A worker already waiting on load means an update is ready to go.
      if (registration.waiting && navigator.serviceWorker.controller) {
        markWaiting(registration.waiting);
      }

      registration.addEventListener('updatefound', () => {
        const installing = registration.installing;
        if (!installing) {
          return;
        }
        installing.addEventListener('statechange', () => {
          // "installed" + an existing controller = an update (not the first install).
          if (installing.state === 'installed' && navigator.serviceWorker.controller) {
            markWaiting(installing);
          }
        });
      });
    })
    .catch(() => {
      // A failed registration must never break the app; it just won't be installable/offline.
    });

  // When the activated worker takes control, reload once to run the new assets.
  navigator.serviceWorker.addEventListener('controllerchange', () => {
    if (reloading) {
      return;
    }
    reloading = true;
    window.location.reload();
  });
}
