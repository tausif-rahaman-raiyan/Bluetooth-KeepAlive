// Bridge between the React UI and the native Windows host (WebView2).
// When the page runs in a normal browser (website / AI Studio preview) `isHosted` is false
// and the UI falls back to its built-in simulation.

export interface HostDevice {
  number: number;
  name: string;
  isDefault: boolean;
  isBluetooth: boolean;
}

export interface HostSettings {
  alwaysKeepStreamActive: boolean;
  autoReconnect: boolean;
  callProtection: boolean;
  autoPauseOnOtherAudio: boolean;
  startOnWindowsStartup: boolean;
  minimizeToTrayOnClose: boolean;
  showNotifications: boolean;
}

export interface HostState {
  type: 'state';
  version: string;
  engineState: string;
  isRunning: boolean;
  runningSeconds: number;
  lastError: string | null;
  deviceName: string;
  deviceNumber: number;
  devices: HostDevice[];
  speaker: {
    name: string;
    connected: boolean;
    battery: number | null;
    charging: boolean;
    status: string;
  };
  laptop: { level: number; charging: boolean; hasBattery: boolean };
  settings: HostSettings;
}

export interface HostUpdate {
  type: 'update';
  currentVersion: string;
  latestVersion: string;
  isUpdateAvailable: boolean;
  releaseName: string;
  releaseNotes: string;
  publishedAt: string;
  setupUrl: string | null;
  htmlUrl: string;
}

export type HostMessage =
  | HostState
  | HostUpdate
  | { type: 'toast'; message: string }
  | { type: 'navigate'; tab: 'dashboard' | 'controls' | 'devices' | 'settings' };

interface WebView2Bridge {
  postMessage(message: unknown): void;
  addEventListener(type: 'message', handler: (event: { data: unknown }) => void): void;
  removeEventListener(type: 'message', handler: (event: { data: unknown }) => void): void;
}

const bridge = (window as unknown as { chrome?: { webview?: WebView2Bridge } }).chrome?.webview;

export const isHosted = !!bridge;

export function sendToHost(message: Record<string, unknown>): void {
  bridge?.postMessage(message);
}

export function onHostMessage(handler: (message: HostMessage) => void): () => void {
  if (!bridge) return () => {};
  const listener = (event: { data: unknown }) => handler(event.data as HostMessage);
  bridge.addEventListener('message', listener);
  return () => bridge.removeEventListener('message', listener);
}

export function formatDuration(totalSeconds: number): string {
  const h = Math.floor(totalSeconds / 3600);
  const m = Math.floor((totalSeconds % 3600) / 60);
  const s = totalSeconds % 60;
  const pad = (n: number) => n.toString().padStart(2, '0');
  return h > 0 ? `${pad(h)}h ${pad(m)}m` : `${pad(m)}m ${pad(s)}s`;
}
