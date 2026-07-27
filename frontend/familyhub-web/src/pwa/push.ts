/*
 * Browser push helpers: permission, subscribe/unsubscribe, and keeping the backend in sync.
 *
 * The service worker (sw.js) handles the incoming `push` and `notificationclick` events; this
 * module is the page-side counterpart that manages the subscription and registers the device
 * with the API.
 */

import { pushService } from '../services/pushService';

export type SubscribeResult = 'subscribed' | 'denied' | 'unsupported' | 'not-configured';

/** True only when this browser can do Web Push in the current (secure) context. */
export function isPushSupported(): boolean {
  return (
    typeof window !== 'undefined' &&
    'serviceWorker' in navigator &&
    'PushManager' in window &&
    'Notification' in window
  );
}

export function getPermission(): NotificationPermission {
  return isPushSupported() ? Notification.permission : 'denied';
}

/** VAPID public key (base64url) → the byte buffer the PushManager expects. */
function urlBase64ToUint8Array(base64: string): Uint8Array<ArrayBuffer> {
  const padding = '='.repeat((4 - (base64.length % 4)) % 4);
  const normalized = (base64 + padding).replace(/-/g, '+').replace(/_/g, '/');
  const raw = atob(normalized);
  const output = new Uint8Array(new ArrayBuffer(raw.length));
  for (let i = 0; i < raw.length; i += 1) {
    output[i] = raw.charCodeAt(i);
  }
  return output;
}

/**
 * Resolves the active service worker registration, or null if none becomes ready shortly.
 * The timeout stops the UI hanging where no worker is running (e.g. the dev server).
 */
async function getRegistration(): Promise<ServiceWorkerRegistration | null> {
  if (!isPushSupported()) {
    return null;
  }
  try {
    const ready = navigator.serviceWorker.ready;
    const timeout = new Promise<null>((resolve) => window.setTimeout(() => resolve(null), 3000));
    return (await Promise.race([ready, timeout])) as ServiceWorkerRegistration | null;
  } catch {
    return null;
  }
}

export async function getExistingSubscription(): Promise<PushSubscription | null> {
  const registration = await getRegistration();
  if (!registration) {
    return null;
  }
  return registration.pushManager.getSubscription();
}

/** Sends a subscription's endpoint + keys to the backend (idempotent upsert). */
async function registerWithBackend(subscription: PushSubscription): Promise<void> {
  const json = subscription.toJSON();
  if (!json.endpoint || !json.keys?.p256dh || !json.keys?.auth) {
    throw new Error('Incomplete push subscription.');
  }
  await pushService.register({
    endpoint: json.endpoint,
    p256dh: json.keys.p256dh,
    auth: json.keys.auth,
    userAgent: navigator.userAgent,
  });
}

/** Requests permission (if needed), subscribes, and registers the device with the backend. */
export async function subscribeToPush(): Promise<SubscribeResult> {
  if (!isPushSupported()) {
    return 'unsupported';
  }

  const permission = await Notification.requestPermission();
  if (permission !== 'granted') {
    return 'denied';
  }

  const registration = await getRegistration();
  if (!registration) {
    return 'unsupported';
  }

  let subscription = await registration.pushManager.getSubscription();
  if (!subscription) {
    const publicKey = await pushService.getVapidPublicKey();
    if (!publicKey) {
      // The server has no VAPID key pair configured — can't subscribe.
      return 'not-configured';
    }
    subscription = await registration.pushManager.subscribe({
      userVisibleOnly: true,
      applicationServerKey: urlBase64ToUint8Array(publicKey),
    });
  }

  await registerWithBackend(subscription);
  return 'subscribed';
}

/** Unsubscribes locally and removes the device from the backend. */
export async function unsubscribeFromPush(): Promise<void> {
  const registration = await getRegistration();
  if (!registration) {
    return;
  }
  const subscription = await registration.pushManager.getSubscription();
  if (!subscription) {
    return;
  }

  const { endpoint } = subscription;
  // Stop delivery in the browser first, then clean up the backend row.
  await subscription.unsubscribe().catch(() => {});
  await pushService.removeByEndpoint(endpoint).catch(() => {});
}

/**
 * When permission is already granted and a subscription exists, re-register it with the backend.
 * Keeps the server in sync after a login on a device that previously enabled push. Best-effort.
 */
export async function syncSubscription(): Promise<void> {
  if (!isPushSupported() || Notification.permission !== 'granted') {
    return;
  }
  const subscription = await getExistingSubscription();
  if (subscription) {
    try {
      await registerWithBackend(subscription);
    } catch {
      // Ignore — a later action will retry.
    }
  }
}
