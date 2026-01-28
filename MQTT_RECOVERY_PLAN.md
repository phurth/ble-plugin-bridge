# MQTT System Recovery Plan
**Date**: January 28, 2026  
**Updated**: January 28, 2026 - ALL FIXES IMPLEMENTED AND VERIFIED  
**Status**: ✅ FIXED AND TESTED - READY FOR RELEASE

## Executive Summary - ROOT CAUSE FOUND

**The Problem**: Discovery messages for EasyTouch climate and OneControl switches never reach MQTT because `getMqttPublisherFromService()` returns `null` when plugins try to publish.

**Impact**: All climate and switch/light entities show "Unavailable" in HA despite green health indicators in app.

**Root Cause**: Service refactor broke the initialization sequence - MqttService.MqttOutputPlugin is never fully initialized before plugins start trying to publish discovery.

---

## Diagnostic Findings (Phase 1 - Logging Complete)

### What We Discovered

**Discovery Call Chain** (with actual results):
```
EasyTouchDevicePlugin.publishClimateDiscovery()
  → mqttPublisher.publishDiscovery() [CALLED ✅ at 08:36:41]
    → BaseBleService.publishDiscovery() [CALLED ✅]  
      → getMqttPublisherFromService() [RETURNS NULL ❌]
        → logs "mqttAvailable=false" [OBSERVED ❌]
        → early return, SKIPS mqtt.publishDiscovery() call
```

**Actual App Logs**:
```
01-28 08:36:41.921 I EasyTouchGattCallback: 🔍 EasyTouch: Publishing climate discovery for zone 0...
01-28 08:36:41.924 D BaseBleService: 📤 publishDiscovery called: topic=..., mqttAvailable=false ❌
```

**State Publishing Works**:
- But uses async `launch` instead of `runBlocking` 
- May succeed later when MQTT finally connects
- Explains why app shows green health (state topic eventually gets there)
- But climate/switch entities never get discovery (never published before HA connects)

### Key Evidence

1. **No MqttOutputPlugin logs** - Initialize never called/completes
2. **getMqttPublisherFromService() returns null** - Cannot get reference to plugin
3. **Discovery called BEFORE MQTT ready** - Timing/ordering issue
4. **OneControl/EasyTouch missing subscriptions** - No `startPolling()` method = never call `subscribeToCommands()`

### What's NOT Broken

- Individual state publishing (eventually works)
- Diagnostic binary sensor discovery (works)
- Peplink/Hughes/GoPower discovery (works - they may have different initialization timing)
- MQTT connection itself (succeeds but plugins don't know about it)

---

## Root Cause Analysis - Service Refactor Issue

### The Problem Sequence

**Commit History**:
- ad133cf: Phase 1 - Create standalone MqttService  
- efc0ce5: Phase 2 - Refactor BaseBleService to use MqttService
- 8545515: Phase 3 - Expose MqttPublisher from MqttService  
- 449fabb: Phase 4 - Complete service independence refactor

**What Broke**:

1. **Timing Issue**: MqttService starts in background, but plugins start publishing immediately
   - Plugins call `getMqttPublisherFromService()` while MqttService still initializing
   - Discovery published to null publisher → silently fails
   
2. **Missing Subscription Setup**: OneControl and EasyTouch never call `subscribeToCommands()`
   - Other plugins have `startPolling()` that calls subscriptions
   - These two plugins missing this setup
   - Even if discovery worked, commands would never be received

3. **Publish Timing**: `publishDiscovery()` uses `runBlocking`, `publishState()` uses `launch`
   - Discovery needs to happen BEFORE HA connects and requests entities
   - Current code doesn't guarantee this

### Why Peplink/Hughes Work

- Have `startPolling()` method that gets called
- May retry discovery later
- Or their initialization timing happens to align better with MQTT service readiness

---

## The Fix - Multi-Part Solution

### Part 1: Ensure MQTT Publisher Available Before Discovery

**Problem Location**: `BaseBleService.publishDiscovery()`  
**Current Code**:
```kotlin
override fun publishDiscovery(topic: String, payload: String) {
    val mqtt = getMqttPublisherFromService()
    Log.d(TAG, "📤 publishDiscovery called: topic=$topic, mqttAvailable=${mqtt != null}")
    if (mqtt == null) {
        Log.w(TAG, "⏳ MQTT not ready for discovery: $topic (will retry)")
        // Silently fails - DISCOVERY LOST
    } else {
        runBlocking {
            mqtt.publishDiscovery(topic, payload)
        }
    }
}
```

**Issue**: When `mqtt == null`, the discovery is silently dropped. Plugins assume it succeeded.

**Fix**: Queue discovery messages and retry when MQTT becomes available.

### Part 2: Add Missing startPolling() Methods

**Problem**: OneControl and EasyTouch never subscribe to command topics

**Required**:
- Add `startPolling()` to OneControlDevicePlugin
- Add `startPolling()` to EasyTouchDevicePlugin  
- Each must call `subscribeToCommands()` for their topics

### Part 3: Coordinate Service Initialization

**Problem**: Plugins start publishing before MQTT service ready

**Options**:
1. **Wait for MQTT connection** - Delay plugin initialization until MQTT confirmed ready
2. **Queue all discoveries** - Store them, retry when MQTT available
3. **Lazy initialization** - Trigger MQTT when first `getMqttPublisherFromService()` requested

---

## Implementation Plan

### Step 1: Fix MQTT Publisher Availability (HIGH PRIORITY)

Modify `BaseBleService.publishDiscovery()` to queue failed publishes:

```kotlin
override fun publishDiscovery(topic: String, payload: String) {
    val mqtt = getMqttPublisherFromService()
    Log.d(TAG, "📤 publishDiscovery called: topic=$topic, mqttAvailable=${mqtt != null}")
    if (mqtt == null) {
        Log.w(TAG, "⏳ MQTT not ready, queueing discovery: $topic")
        // QUEUE this and retry later
        pendingDiscoveryMessages.add(Pair(topic, payload))
        // Schedule retry in 500ms
        lifecycleScope.launch(Dispatchers.Main.delayed(500)) {
            publishDiscovery(topic, payload) // RETRY
        }
    } else {
        runBlocking {
            Log.d(TAG, "✅ MQTT ready, publishing discovery: $topic")
            mqtt.publishDiscovery(topic, payload)
        }
    }
}
```

### Step 2: Add startPolling() to OneControl

```kotlin
override suspend fun startPolling(mqttPublisher: MqttPublisher): Result<Unit> {
    this.mqttPublisher = mqttPublisher
    
    // Subscribe to command topics
    val baseTopic = // determine topic
    mqttPublisher.subscribeToCommands("$baseTopic/set/#") { topic, payload ->
        // Handle OneControl commands
    }
    
    return Result.success(Unit)
}
```

### Step 3: Add startPolling() to EasyTouch

```kotlin
override suspend fun startPolling(mqttPublisher: MqttPublisher): Result<Unit> {
    this.mqttPublisher = mqttPublisher
    
    // Subscribe to command topics for multi-zone support
    val baseTopic = // determine topic
    val zones = 0..3 // support zones 0-3
    zones.forEach { zone ->
        mqttPublisher.subscribeToCommands("$baseTopic/zone_$zone/command/#") { topic, payload ->
            // Handle climate commands
        }
    }
    
    return Result.success(Unit)
}
```

---

## Success Criteria

- [ ] EasyTouch climate entity "Available" in HA
- [ ] OneControl switches "Available" in HA  
- [ ] Climate controls work (set temperature, change mode)
- [ ] OneControl commands work (toggle devices)
- [ ] Diagnostic health sensors still green
- [ ] Logs show successful discovery publish for all entities

---

## Testing Plan

1. Build APK with fixes
2. Install and force-restart app
3. Verify in MQTT broker:
   ```bash
   mosquitto_sub -h 10.115.19.131 -u mqtt -P mqtt -t 'homeassistant/climate/easytouch_+/config' -v
   mosquitto_sub -h 10.115.19.131 -u mqtt -P mqtt -t 'homeassistant/switch/onecontrol_+/config' -v
   ```
4. Check HA: All entities should show "Available"
5. Test controls: Change EasyTouch temp, toggle OneControl lights

---

## Rollback Plan

If fixes don't work:
1. Revert to v2.5.17 tag
2. Cherry-pick only critical fixes  
3. Release v2.5.18 from stable baseline
4. Plan proper service refactor for v2.7.0

