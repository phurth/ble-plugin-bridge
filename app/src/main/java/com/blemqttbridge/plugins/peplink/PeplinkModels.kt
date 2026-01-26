package com.blemqttbridge.plugins.peplink

import org.json.JSONObject

/**
 * Data models for Peplink Router API responses.
 *
 * Based on Peplink API Documentation for Firmware 8.5.0.
 */

// ===== TOKEN MANAGEMENT =====

/**
 * Authentication token response from /api/auth.token.grant
 */
data class TokenResponse(
    val accessToken: String,
    val expiresIn: Int  // Token lifetime in seconds (typically 172800 = 48 hours)
)

// ===== WAN CONNECTION TYPES =====

enum class WanType {
    ETHERNET,
    CELLULAR,
    WIFI,
    VWAN,  // Virtual WAN (VPN/tunnel)
    UNKNOWN
}

enum class ConnectionStatus {
    CONNECTED,
    DISCONNECTED,
    DISABLED,
    UNKNOWN
}

// ===== WAN CONNECTION STATUS =====

/**
 * WAN connection information from /api/status.wan.connection
 */
data class WanConnection(
    val connId: Int,
    val name: String,  // Friendly name from API
    val type: WanType,
    val enabled: Boolean,
    val status: ConnectionStatus,
    val message: String?,  // Status message (e.g., "Connected")
    val priority: Int?,  // Priority level (1-4, or null if disabled)
    val uptime: Int?,  // Uptime in seconds
    val ip: String?,  // Assigned IP address
    val statusLed: String?,  // LED status indicator
    val cellular: CellularInfo? = null,
    val wifi: WifiInfo? = null
) {
    companion object {
        /**
         * Parse WAN connection from JSON response.
         * Response format: { "connId": { "name": "...", "enable": true, ... } }
         */
        fun fromJson(connId: Int, json: JSONObject): WanConnection {
            val type = determineWanType(json)
            val enabled = json.optBoolean("enable", false)
            val message = if (json.has("message")) json.getString("message") else null

            return WanConnection(
                connId = connId,
                name = json.optString("name", "WAN $connId"),
                type = type,
                enabled = enabled,
                status = determineStatus(enabled, message),
                message = message,
                priority = if (json.has("priority")) json.optInt("priority") else null,
                uptime = if (json.has("uptime")) json.optInt("uptime") else null,
                ip = json.optString("ip", null),
                statusLed = json.optString("statusLed", null),
                cellular = if (type == WanType.CELLULAR && json.has("cellular")) {
                    CellularInfo.fromJson(json.getJSONObject("cellular"))
                } else null,
                wifi = if (type == WanType.WIFI && json.has("wifi")) {
                    WifiInfo.fromJson(json.getJSONObject("wifi"))
                } else null
            )
        }

        private fun determineWanType(json: JSONObject): WanType {
            return when {
                json.has("cellular") -> WanType.CELLULAR
                json.has("wifi") -> WanType.WIFI
                json.optString("name", "").contains("vWAN", ignoreCase = true) -> WanType.VWAN
                json.optString("name", "").contains("Ethernet", ignoreCase = true) -> WanType.ETHERNET
                else -> WanType.UNKNOWN
            }
        }

        private fun determineStatus(enabled: Boolean, message: String?): ConnectionStatus {
            return when {
                !enabled -> ConnectionStatus.DISABLED
                message?.contains("Disconnected", ignoreCase = true) == true -> ConnectionStatus.DISCONNECTED
                message?.contains("Connected", ignoreCase = true) == true -> ConnectionStatus.CONNECTED
                else -> ConnectionStatus.UNKNOWN
            }
        }
    }
}

// ===== CELLULAR INFORMATION =====

data class CellularInfo(
    val moduleName: String,  // Modem model (e.g., "Quectel EM20-G")
    val signalStrength: Int?,  // Signal strength in dBm
    val signalQuality: Int?,  // Signal quality percentage
    val carrier: String?,  // Carrier name
    val networkType: String?,  // Network type (e.g., "LTE")
    val band: String?  // Frequency band
) {
    companion object {
        fun fromJson(json: JSONObject): CellularInfo {
            return CellularInfo(
                moduleName = json.optString("moduleName", "Cellular Modem"),
                signalStrength = if (json.has("signalStrength")) json.optInt("signalStrength") else null,
                signalQuality = if (json.has("signalQuality")) json.optInt("signalQuality") else null,
                carrier = json.optString("carrier", null),
                networkType = json.optString("networkType", null),
                band = json.optString("band", null)
            )
        }
    }
}

// ===== WIFI INFORMATION =====

data class WifiInfo(
    val ssid: String?,
    val frequency: String?,  // "2.4GHz" or "5GHz"
    val signalStrength: Int?,  // Signal strength in dBm
    val channel: Int?
) {
    companion object {
        fun fromJson(json: JSONObject): WifiInfo {
            return WifiInfo(
                ssid = json.optString("ssid", null),
                frequency = json.optString("frequency", null),
                signalStrength = if (json.has("signalStrength")) json.optInt("signalStrength") else null,
                channel = if (json.has("channel")) json.optInt("channel") else null
            )
        }
    }
}

// ===== USAGE/ALLOWANCE DATA =====

/**
 * SIM slot information from nested usage API response.
 * Peplink routers with multiple SIM slots return nested structure:
 * - Single SIM: response['2'] = flat object
 * - Multi-SIM: response['2']['1'] and response['2']['2'] for SIM A and SIM B
 */
data class SimSlotInfo(
    val slotId: Int,
    val enabled: Boolean,
    val hasUsageTracking: Boolean,
    val usage: Long?,  // Usage in MB
    val limit: Long?,  // Limit in MB
    val percent: Int?,  // Usage percentage
    val startDate: String?  // Billing cycle start date
) {
    companion object {
        fun fromJson(slotId: Int, json: JSONObject): SimSlotInfo {
            return SimSlotInfo(
                slotId = slotId,
                enabled = json.optBoolean("enable", false),
                hasUsageTracking = json.has("usage"),
                usage = if (json.has("usage")) json.optLong("usage") else null,
                limit = if (json.has("limit")) json.optLong("limit") else null,
                percent = if (json.has("percent")) json.optInt("percent") else null,
                startDate = json.optString("start", null)
            )
        }
    }
}

/**
 * WAN connection usage data from /api/status.wan.connection.allowance
 */
data class WanUsage(
    val connId: Int,
    val enabled: Boolean,
    val usage: Long?,  // Usage in MB
    val limit: Long?,  // Limit in MB
    val percent: Int?,  // Usage percentage
    val unit: String?,  // Unit (e.g., "MB", "GB")
    val startDate: String?,  // Billing cycle start
    val simSlots: Map<Int, SimSlotInfo>?  // For cellular with multiple SIMs
) {
    companion object {
        fun fromJson(connId: Int, json: JSONObject): WanUsage {
            // Check if this is a multi-SIM cellular connection (nested structure)
            val hasNestedSims = json.keys().asSequence()
                .any { it.toIntOrNull() != null }

            val simSlots = if (hasNestedSims) {
                // Multi-SIM: response['2']['1'], response['2']['2']
                json.keys().asSequence()
                    .mapNotNull { key ->
                        key.toIntOrNull()?.let { slotId ->
                            slotId to SimSlotInfo.fromJson(slotId, json.getJSONObject(key))
                        }
                    }
                    .toMap()
            } else {
                null
            }

            return WanUsage(
                connId = connId,
                enabled = json.optBoolean("enable", false),
                usage = if (!hasNestedSims && json.has("usage")) json.optLong("usage") else null,
                limit = if (!hasNestedSims && json.has("limit")) json.optLong("limit") else null,
                percent = if (!hasNestedSims && json.has("percent")) json.optInt("percent") else null,
                unit = if (!hasNestedSims) json.optString("unit", null) else null,
                startDate = if (!hasNestedSims) json.optString("start", null) else null,
                simSlots = simSlots
            )
        }
    }
}

// ===== API RESPONSE WRAPPER =====

/**
 * Generic API response wrapper.
 * All Peplink API responses follow this format:
 * {
 *   "stat": "ok" | "fail",
 *   "response": { ... },
 *   "code": <int>,  // Only on failure
 *   "message": "<string>"  // Only on failure
 * }
 */
data class ApiResponse<T>(
    val stat: String,
    val response: T?,
    val code: Int?,
    val message: String?
) {
    val isSuccess: Boolean
        get() = stat == "ok"

    val isFailure: Boolean
        get() = stat == "fail"
}

// ===== HARDWARE CONFIGURATION =====

/**
 * Hardware configuration discovered from Peplink router.
 * Contains all WAN connections and their capabilities.
 */
data class PeplinkHardwareConfig(
    val wanConnections: Map<Int, WanConnection>
) {
    /**
     * Get all cellular connections.
     */
    fun getCellularConnections(): List<Pair<Int, WanConnection>> {
        return wanConnections.filter { (_, conn) -> conn.type == WanType.CELLULAR }.toList()
    }

    /**
     * Get all WiFi WAN connections.
     */
    fun getWifiConnections(): List<Pair<Int, WanConnection>> {
        return wanConnections.filter { (_, conn) -> conn.type == WanType.WIFI }.toList()
    }

    /**
     * Get all Ethernet connections.
     */
    fun getEthernetConnections(): List<Pair<Int, WanConnection>> {
        return wanConnections.filter { (_, conn) -> conn.type == WanType.ETHERNET }.toList()
    }
}
