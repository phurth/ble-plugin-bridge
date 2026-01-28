# Web UI Analysis - Toggle Bounce & Plugin Availability Issues

## Summary of Changes Made

### 1. **Toggle Bounce Fix (COMPLETED)**
**Problem:** Users toggled a service → optimistic UI updated → 5-second auto-refresh called `loadStatus()` → `loadStatus()` re-rendered toggle from server state → toggle reverted back to previous state → slowly updated to final state.

**Root Cause:** Architectural conflict between optimistic UI updates and aggressive auto-refresh. The `setInterval` at line 69 calls `loadStatus()` every 5 seconds, which unconditionally re-renders toggle checkboxes using `.innerHTML`, destroying any optimistic UI changes.

**Solution Implemented:**
- Added `recentlyToggledSwitches` Set to track which toggles were just clicked
- Added `TOGGLE_DEBOUNCE_MS = 3000` constant (3-second debounce period)
- Modified `loadStatus()` to skip re-rendering toggle if switch is in `recentlyToggledSwitches`
- Modified `loadPollingStatus()` similarly for polling service toggle
- Modified each toggle function (`toggleService`, `toggleMqtt`, `togglePolling`) to:
  1. Add switch to `recentlyToggledSwitches` immediately
  2. Send API request with optimistic UI (toggle stays in new position)
  3. On success: wait 3 seconds then clear debounce flag and refresh to sync with server state
  4. On error: immediately clear debounce flag and reload to revert toggle
- Modified auto-refresh loop to skip `loadInstances()` if any toggle is in debounce period (prevents "plugins disappear" issue)

**Result:** Toggles now stay in their clicked position for 3 seconds without bouncing back, giving the backend time to process the change and the UI to stabilize.

### 2. **Plugin Disappearance Fix (COMPLETED)**
**Problem:** When toggling BLE/polling service, plugins list would temporarily show error message "Failed to load plugins".

**Root Cause:** `loadInstances()` was being called every 5 seconds via auto-refresh, and if the BLE service was transitioning (stopping/starting), the `/api/instances` endpoint might temporarily fail or timeout, causing the error state to display.

**Solution:** Modified auto-refresh condition at line 75 to skip `loadInstances()` if:
- Any fields are currently being edited (`editingFields.length > 0`)
- OR any toggle is in debounce period (`recentlyToggledSwitches.size > 0`)

**Result:** While a service transition is in progress (during the 3-second debounce period), the plugins list won't be refreshed, so error states won't flash on screen.

## Remaining Issues to Investigate

### 1. **EasyTouch Health Status**
**Observation:** EasyTouch appears as "Unhealthy" in the web UI app but appears as "Healthy" in Home Assistant.

**Technical Investigation:**
- Web UI health calculation (line 359): `healthy = connected && authenticated && dataHealthy`
- EasyTouch is classified as "Active" plugin (not in passive list), so requires all three flags to be true
- Backend status update (EasyTouchDevicePlugin.kt:1938): `val dataHealthy = isAuthenticated && isPollingActive`
- Issue: `dataHealthy` becomes false if EITHER `isAuthenticated` OR `isPollingActive` is false

**Hypothesis 1 (More likely):** The `isPollingActive` flag might not be set correctly after authentication. Once polling starts (line 467), it should remain true as long as the connection is active. However, if polling stops for any reason (error, timeout, etc.), the entire health status goes to "false".

**Hypothesis 2:** Time synchronization issue - between the moment status is read for MQTT publish and when the web UI queries it, the polling state might have changed momentarily.

**Next Steps for Investigation:**
- Check the EasyTouchDevicePlugin logs to see if `isPollingActive` is being set/cleared unexpectedly
- Compare MQTT published status with web API status response timing
- Check if there are any race conditions in status update code

### 2. **Toggle State Desynchronization During Service Transitions**
**Potential Issue:** If the backend is slow to process a service toggle, the 3-second debounce period might not be enough for the state to fully stabilize. After 3 seconds, `loadInstances()` resumes, and if the backend hasn't fully transitioned yet, there might still be brief error states.

**Monitor For:** If plugins still briefly flash "unavailable" after the 3-second debounce expires but before the backend fully transitions.

## Architecture Notes

The current web UI uses a "pull" architecture:
- Every 5 seconds, the UI pulls: `loadStatus()`, `loadPollingStatus()`, `loadInstances()`
- No server-pushed updates or WebSocket connections
- UI optimistically assumes commands succeeded, then reconciles with server state after debounce

This is fundamentally incompatible with the previous approach of immediately reloading on every action. The fix (debouncing + conditional refresh) is a reasonable middle ground but not ideal. Longer-term improvements could include:
- Event-driven updates (server pushes changes to UI)
- WebSocket connection for real-time state sync
- Separate UI state from server state (server state is authoritative, UI shows predicted state during transitions)

## Files Modified

1. **app/src/main/res/raw/web_ui_js.js**:
   - Lines 37-40: Added `recentlyToggledSwitches` Set and `TOGGLE_DEBOUNCE_MS` constant
   - Lines 75: Modified auto-refresh condition to skip `loadInstances()` during toggles
   - Lines 93-112: Modified `loadStatus()` and `loadPollingStatus()` to skip toggle re-render during debounce
   - Lines 151-186: Modified `togglePolling()` to use debounce mechanism
   - Lines 124-148: Modified `loadPollingStatus()` to skip toggle re-render during debounce
   - Lines 1216-1250: Modified `toggleService()` to use debounce mechanism
   - Lines 1253-1282: Modified `toggleMqtt()` to use debounce mechanism

## Testing Recommendations

1. **Toggle Bounce Test:**
   - Click BLE service toggle
   - Verify: Toggle stays in new position for ~3 seconds
   - Verify: Plugins list doesn't disappear
   - Verify: After 3 seconds, page auto-refreshes and confirms server state

2. **Concurrent Toggle Test:**
   - Click BLE service toggle
   - Immediately click MQTT toggle (while BLE is debouncing)
   - Verify: Both debounce independently and don't interfere

3. **Error Handling Test:**
   - Start service toggle
   - Manually kill the backend service before toggle completes
   - Verify: Toggle reverts after timeout (error case)
   - Verify: Debounce flag is cleared on error

4. **EasyTouch Health Test:**
   - Connect EasyTouch device
   - Monitor: Check if "Data Healthy" flag in web UI matches MQTT status
   - If mismatch: Check backend logs for `isPollingActive` state changes
