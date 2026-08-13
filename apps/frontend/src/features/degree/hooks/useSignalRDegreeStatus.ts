/**
 * Extension point for SignalR realtime degree status updates.
 * Currently a no-op — will be activated when backend implements SignalR Hub.
 *
 * Design: When activated, this hook will:
 * 1. Connect to Hub at env.signalrUrl
 * 2. Listen for 'DegreeStatusUpdated' events
 * 3. On event: invalidate degreeKeys.lists() + degreeKeys.detail(degreeId)
 * 4. Set isConnected ref -> true, which disables polling in useDegreesQuery
 * 5. On disconnect: set isConnected -> false, polling resumes
 * 6. On reconnect: debounce 300ms, then invalidate + set isConnected -> true
 */
export function useSignalRDegreeStatus() {
  // No-op in Phase 1. Backend SignalR Hub does not exist yet.
  return { isConnected: false };
}
