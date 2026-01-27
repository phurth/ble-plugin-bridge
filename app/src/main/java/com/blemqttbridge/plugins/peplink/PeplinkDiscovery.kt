package com.blemqttbridge.plugins.peplink

import android.util.Log

/**
 * Hardware discovery system for Peplink routers.
 *
 * Peplink routers share the same API but have varying hardware configurations:
 * - Different numbers of WAN connections (Ethernet, cellular, WiFi, vWAN)
 * - Different numbers of cellular modems (1 or 2)
 * - Different SIM slot configurations (single vs dual SIM per modem)
 * - Different WiFi capabilities (2.4GHz only, 5GHz only, or both)
 *
 * This system dynamically discovers the available hardware by querying the
 * router's API and inspecting response structures. Discovery results are
 * logged to logcat for testing and debugging.
 */
object PeplinkDiscovery {

    private const val TAG = "PeplinkDiscovery"

    /**
     * Discovered hardware configuration for a Peplink router.
     * Cached after initial discovery to avoid repeated API calls.
     */
    data class HardwareConfig(
        val wanConnections: Map<Int, WanConnection>,
        val discoveryTimestamp: Long = System.currentTimeMillis()
    ) {
        /**
         * Check if discovery is stale (older than 1 hour).
         * Stale discovery should trigger a refresh.
         */
        fun isStale(): Boolean {
            val ageMs = System.currentTimeMillis() - discoveryTimestamp
            return ageMs > (60 * 60 * 1000)  // 1 hour
        }

        /**
         * Get cellular connections with their detailed info.
         */
        fun getCellularConnections(): List<WanConnection> {
            return wanConnections.values.filter { it.type == WanType.CELLULAR }
        }

        /**
         * Get WiFi connections.
         */
        fun getWifiConnections(): List<WanConnection> {
            return wanConnections.values.filter { it.type == WanType.WIFI }
        }

        /**
         * Get total number of SIM slots across all cellular connections.
         */
        fun getTotalSimSlots(): Int {
            return wanConnections.values
                .filter { it.type == WanType.CELLULAR }
                .sumOf { _: WanConnection ->
                    // Count SIM slots from usage data (not available in status)
                    0 as Int  // Will be populated after usage query
                }
        }
    }

    /**
     * Perform complete hardware discovery for a Peplink router.
     *
     * Discovery process:
     * 1. Query WAN status API with broad ID range (1-10)
     * 2. Parse response to identify available connections
     * 3. Determine connection types from response structure
     * 4. Query usage API to detect SIM slot configurations
     * 5. Enrich cellular connections with SIM slot data
     * 6. Log all discovered hardware to logcat
     *
     * @param apiClient Initialized API client for the router
     * @return Result containing HardwareConfig or error
     */
    suspend fun discoverHardware(apiClient: PeplinkApiClient): Result<HardwareConfig> {
        DiscoveryLogger.logDiscoveryStart()

        return try {
            // Step 1: Query WAN status with broad range
            val wanStatusResult = apiClient.getWanStatus(connIds = "1 2 3 4 5 6 7 8 9 10")
            if (wanStatusResult.isFailure) {
                val error = wanStatusResult.exceptionOrNull()!!
                DiscoveryLogger.logDiscoveryError(error)
                return Result.failure(error)
            }

            val wanConnections = wanStatusResult.getOrThrow().toMutableMap()
            DiscoveryLogger.logInitialConnections(wanConnections.size)

            // Step 2: Enrich cellular connections with SIM slot count (always 2 for Peplink cellular modems)
            // This happens regardless of usage API availability, ensuring per-SIM discovery always works
            enrichCellularWithSimSlots(wanConnections)

            // Step 3: Log each discovered connection
            wanConnections.values.sortedBy { it.connId }.forEach { conn ->
                DiscoveryLogger.logDiscoveredConnection(conn, usageData = null)
            }

            // Step 4: Calculate total entities that will be created
            val entityCount = estimateEntityCount(wanConnections)
            DiscoveryLogger.logDiscoveryComplete(wanConnections.size, entityCount)

            val config = HardwareConfig(wanConnections = wanConnections)
            Result.success(config)

        } catch (e: Exception) {
            DiscoveryLogger.logDiscoveryError(e)
            Result.failure(e)
        }
    }

    /**
     * Enrich cellular connections with SIM slot information.
     * Peplink cellular modems support up to 5 SIM slots:
     * 1=SIM A, 2=SIM B, 3=RemoteSIM, 4=FusionSIM, 5=Peplink eSIM
     * This is not dependent on usage data availability.
     */
    private fun enrichCellularWithSimSlots(
        wanConnections: MutableMap<Int, WanConnection>
    ) {
        wanConnections.forEach { (connId, conn) ->
            if (conn.type == WanType.CELLULAR) {
                // All Peplink cellular connections support up to 5 SIM slots
                Log.d(TAG, "WAN $connId (cellular) has 5 possible SIM slots")
                wanConnections[connId] = conn.copy(simSlotCount = 5)
            }
        }
    }

    /**
     * Estimate total Home Assistant entities that will be created.
     *
     * Per WAN connection:
     * - Binary sensor: Connection status
     * - Sensor: Signal strength (if cellular/WiFi)
     * - Sensor: Uptime
     * - Sensor: Usage (if tracking enabled)
     * - Select: Priority control
     * - Button: Modem reset (cellular only)
     *
     * Additional per router:
     * - Button: Rediscover hardware
     */
    private fun estimateEntityCount(wanConnections: Map<Int, WanConnection>): Int {
        var count = 0

        wanConnections.values.forEach { conn ->
            count += 3  // Binary sensor, uptime, priority select (always)

            if (conn.type == WanType.CELLULAR || conn.type == WanType.WIFI) {
                count += 1  // Signal strength sensor
            }

            if (conn.type == WanType.CELLULAR) {
                count += 1  // Modem reset button
            }

            // Usage sensors (may be per-SIM for cellular)
            count += 1
        }

        count += 1  // Rediscover button

        return count
    }

    /**
     * Compare previous and new hardware configurations to detect changes.
     * Used for logging differences during manual rediscovery.
     */
    fun compareConfigs(previous: HardwareConfig?, new: HardwareConfig): List<String> {
        if (previous == null) {
            return listOf("Initial discovery - no previous config")
        }

        val changes = mutableListOf<String>()

        // Check for added connections
        val added = new.wanConnections.keys - previous.wanConnections.keys
        if (added.isNotEmpty()) {
            changes.add("Added WAN connections: ${added.sorted().joinToString()}")
        }

        // Check for removed connections
        val removed = previous.wanConnections.keys - new.wanConnections.keys
        if (removed.isNotEmpty()) {
            changes.add("Removed WAN connections: ${removed.sorted().joinToString()}")
        }

        // Check for changed connection types
        val common = previous.wanConnections.keys.intersect(new.wanConnections.keys)
        common.forEach { connId ->
            val oldConn = previous.wanConnections[connId]!!
            val newConn = new.wanConnections[connId]!!

            if (oldConn.type != newConn.type) {
                changes.add("WAN $connId type changed: ${oldConn.type} -> ${newConn.type}")
            }

            if (oldConn.name != newConn.name) {
                changes.add("WAN $connId name changed: ${oldConn.name} -> ${newConn.name}")
            }
        }

        if (changes.isEmpty()) {
            changes.add("No hardware changes detected")
        }

        return changes
    }
}

/**
 * Logger for hardware discovery process.
 * Outputs formatted logs to logcat for debugging and testing.
 */
object DiscoveryLogger {
    private const val TAG = "PeplinkDiscovery"

    fun logDiscoveryStart() {
        Log.i(TAG, "╔════════════════════════════════════════════════════════════")
        Log.i(TAG, "║ Starting Peplink Hardware Discovery")
        Log.i(TAG, "╚════════════════════════════════════════════════════════════")
    }

    fun logInitialConnections(count: Int) {
        Log.i(TAG, "Found $count WAN connection(s) in API response")
    }

    fun logDiscoveredConnection(wan: WanConnection, usageData: WanUsage? = null) {
        Log.i(TAG, "")
        Log.i(TAG, "╔════════════════════════════════════════════════════════════")
        Log.i(TAG, "║ WAN Connection Discovered")
        Log.i(TAG, "╠════════════════════════════════════════════════════════════")
        Log.i(TAG, "║ ID:       ${wan.connId}")
        Log.i(TAG, "║ Name:     ${wan.name}")
        Log.i(TAG, "║ Type:     ${wan.type}")
        Log.i(TAG, "║ Enabled:  ${wan.enabled}")
        Log.i(TAG, "║ Status:   ${wan.status}")
        Log.i(TAG, "║ Priority: ${wan.priority ?: "N/A"}")

        if (wan.ip != null) {
            Log.i(TAG, "║ IP:       ${wan.ip}")
        }

        if (wan.uptime != null) {
            Log.i(TAG, "║ Uptime:   ${wan.uptime}s")
        }

        // Cellular-specific info
        if (wan.cellular != null) {
            Log.i(TAG, "╠════════════════════════════════════════════════════════════")
            Log.i(TAG, "║ Cellular Modem Details")
            Log.i(TAG, "╠════════════════════════════════════════════════════════════")
            Log.i(TAG, "║ Modem:    ${wan.cellular.moduleName}")

            if (wan.cellular.signalStrength != null) {
                Log.i(TAG, "║ Signal:   ${wan.cellular.signalStrength} dBm")
            }

            if (wan.cellular.carrier != null) {
                Log.i(TAG, "║ Carrier:  ${wan.cellular.carrier}")
            }

            if (wan.cellular.networkType != null) {
                Log.i(TAG, "║ Network:  ${wan.cellular.networkType}")
            }

            // SIM slot info from usage data
            if (usageData?.simSlots != null) {
                Log.i(TAG, "║ SIM Slots: ${usageData.simSlots.size}")
                usageData.simSlots.values.sortedBy { it.slotId }.forEach { sim ->
                    val status = if (sim.enabled) "Enabled" else "Disabled"
                    val tracking = if (sim.hasUsageTracking) "Tracked" else "Not tracked"
                    Log.i(TAG, "║   - Slot ${sim.slotId}: $status, Usage: $tracking")
                }
            }
        }

        // WiFi-specific info
        if (wan.wifi != null) {
            Log.i(TAG, "╠════════════════════════════════════════════════════════════")
            Log.i(TAG, "║ WiFi Details")
            Log.i(TAG, "╠════════════════════════════════════════════════════════════")

            if (wan.wifi.ssid != null) {
                Log.i(TAG, "║ SSID:     ${wan.wifi.ssid}")
            }

            if (wan.wifi.frequency != null) {
                Log.i(TAG, "║ Band:     ${wan.wifi.frequency}")
            }

            if (wan.wifi.signalStrength != null) {
                Log.i(TAG, "║ Signal:   ${wan.wifi.signalStrength} dBm")
            }
        }

        Log.i(TAG, "╚════════════════════════════════════════════════════════════")
    }

    fun logDiscoveryComplete(wanCount: Int, entityCount: Int) {
        Log.i(TAG, "")
        Log.i(TAG, "╔════════════════════════════════════════════════════════════")
        Log.i(TAG, "║ Discovery Complete")
        Log.i(TAG, "╠════════════════════════════════════════════════════════════")
        Log.i(TAG, "║ WAN Connections: $wanCount")
        Log.i(TAG, "║ HA Entities:     ~$entityCount")
        Log.i(TAG, "╚════════════════════════════════════════════════════════════")
    }

    fun logDiscoveryError(error: Throwable) {
        Log.e(TAG, "╔════════════════════════════════════════════════════════════")
        Log.e(TAG, "║ Discovery Failed")
        Log.e(TAG, "╠════════════════════════════════════════════════════════════")
        Log.e(TAG, "║ Error: ${error.message}")
        Log.e(TAG, "╚════════════════════════════════════════════════════════════")
    }

    fun logRediscovery(changes: List<String>) {
        Log.i(TAG, "")
        Log.i(TAG, "╔════════════════════════════════════════════════════════════")
        Log.i(TAG, "║ Hardware Rediscovery")
        Log.i(TAG, "╠════════════════════════════════════════════════════════════")
        changes.forEach { change ->
            Log.i(TAG, "║ $change")
        }
        Log.i(TAG, "╚════════════════════════════════════════════════════════════")
    }
}
