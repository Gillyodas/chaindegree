// --- Required variables: fail-fast if missing ---
const apiBaseUrl = import.meta.env.VITE_API_BASE_URL;
if (!apiBaseUrl) {
  throw new Error('Missing required env: VITE_API_BASE_URL');
}

const apiTimeoutRaw = Number(import.meta.env.VITE_API_TIMEOUT);
if (!Number.isFinite(apiTimeoutRaw) || apiTimeoutRaw <= 0) {
  throw new Error('VITE_API_TIMEOUT must be a positive number');
}

// --- Optional variables: may fallback ---
export const env = {
  appName: import.meta.env.VITE_APP_NAME ?? 'ChainDegree',
  apiBaseUrl,
  apiTimeout: apiTimeoutRaw,
  signalrUrl: import.meta.env.VITE_SIGNALR_URL ?? '',
  reputationEnabled: import.meta.env.VITE_REPUTATION_ENABLED === 'true',
} as const;
