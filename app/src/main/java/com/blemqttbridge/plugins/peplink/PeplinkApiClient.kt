package com.blemqttbridge.plugins.peplink

import android.util.Log
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import org.json.JSONObject
import java.util.concurrent.TimeUnit

/**
 * Peplink Router API Client with automatic cookie-based authentication.
 *
 * Handles:
 * - Cookie-based session authentication using admin username/password
 * - Automatic re-authentication on session expiration (401 errors)
 * - All API endpoint calls (status, control, info)
 * - Error handling and retry logic
 *
 * Based on Peplink API Documentation for Firmware 8.5.0.
 */
class PeplinkApiClient(
    private val baseUrl: String,  // e.g., "http://192.168.1.1"
    private val username: String,
    private val password: String
) {
    private val httpClient = OkHttpClient.Builder()
        .connectTimeout(10, TimeUnit.SECONDS)
        .readTimeout(30, TimeUnit.SECONDS)
        .writeTimeout(10, TimeUnit.SECONDS)
        .build()

    private val jsonMediaType = "application/json; charset=utf-8".toMediaType()

    // Cookie-based authentication management
    private var authCookie: String? = null  // pauth cookie value
    private var isConnected: Boolean = false
    private val authMutex = Mutex()
    
    /**
     * Check if currently authenticated (has valid session cookie).
     */
    fun isAuthenticated(): Boolean = isConnected && authCookie != null

    companion object {
        private const val TAG = "PeplinkApiClient"
        private const val LOGIN_PATH = "/api/login"
        private const val WAN_STATUS_PATH = "/api/status.wan.connection"
        private const val WAN_USAGE_PATH = "/api/status.wan.connection.allowance"
        private const val WAN_PRIORITY_PATH = "/api/config.wan.connection.priority"
        private const val CELLULAR_RESET_PATH = "/api/cmd.cellularModule.reset"
        private const val INFO_FIRMWARE_PATH = "/api/info.firmware"
        private const val STATUS_CLIENT_PATH = "/api/status.client"
        private const val STATUS_PEPVPN_PATH = "/api/status.pepvpn"
        private const val INFO_LOCATION_PATH = "/api/info.location"
    }

    // ===== AUTHENTICATION MANAGEMENT =====

    /**
     * Ensure connected with valid session cookie.
     * Thread-safe with mutex to prevent concurrent login attempts.
     */
    private suspend fun ensureConnected(forceReconnect: Boolean = false): Result<Unit> = authMutex.withLock {
        Log.d(TAG, "ensureConnected called: isConnected=$isConnected, hasCookie=${authCookie != null}, forceReconnect=$forceReconnect")
        
        if (isConnected && authCookie != null && !forceReconnect) {
            Log.d(TAG, "Already connected with valid cookie, skipping login")
            return Result.success(Unit)
        }

        // Not connected or forced reconnect, authenticate
        Log.i(TAG, "Authenticating with router at $baseUrl...")
        val result = login()
        Log.i(TAG, "Login result: success=${result.isSuccess}, error=${result.exceptionOrNull()?.message}")
        return result
    }

    /**
     * Login to router and extract auth cookie.
     */
    private suspend fun login(): Result<Unit> {
        val payload = JSONObject().apply {
            put("username", username)
            put("password", password)
            put("challenge", "challenge")  // Required by Peplink API
        }.toString()

        val loginUrl = "$baseUrl$LOGIN_PATH"
        Log.d(TAG, "Attempting login to: $loginUrl")
        
        val request = Request.Builder()
            .url(loginUrl)
            .post(payload.toRequestBody(jsonMediaType))
            .build()

        return try {
            Log.d(TAG, "Executing login request...")
            val response = httpClient.newCall(request).execute()
            val responseBody = response.body?.string()
            Log.d(TAG, "Login response: code=${response.code}, bodyLength=${responseBody?.length}")

            if (response.code == 401) {
                Log.e(TAG, "Login failed: Invalid credentials (401)")
                isConnected = false
                return Result.failure(Exception("Authentication failed: Invalid username or password"))
            }

            if (!response.isSuccessful) {
                Log.e(TAG, "Login failed: HTTP ${response.code}")
                isConnected = false
                return Result.failure(Exception("Login failed: HTTP ${response.code}"))
            }

            if (responseBody == null) {
                Log.e(TAG, "Login failed: Empty response body")
                isConnected = false
                return Result.failure(Exception("Login failed: Empty response"))
            }

            val json = JSONObject(responseBody)
            if (json.optString("stat") != "ok") {
                val errorMsg = json.optString("message", "Unknown error")
                Log.e(TAG, "Login failed: $errorMsg")
                isConnected = false
                return Result.failure(Exception("Login failed: $errorMsg"))
            }

            // Extract pauth cookie from response headers
            Log.d(TAG, "Response headers: ${response.headers}")
            val setCookieHeaders = response.headers("Set-Cookie")
            Log.d(TAG, "Set-Cookie headers count: ${setCookieHeaders.size}")
            setCookieHeaders.forEachIndexed { idx, header ->
                Log.d(TAG, "Set-Cookie[$idx]: $header")
            }
            
            val cookieHeader = setCookieHeaders.find { it.contains("pauth=") }
            if (cookieHeader != null) {
                Log.d(TAG, "Found pauth cookie header: $cookieHeader")
                // Parse cookie value (format: "pauth=VALUE; HttpOnly; SameSite=Strict")
                val cookieParts = cookieHeader.split(';')
                for (part in cookieParts) {
                    if (part.trim().startsWith("pauth=")) {
                        authCookie = part.trim().substring(6)  // Remove "pauth=" prefix
                        Log.d(TAG, "Extracted cookie value (length: ${authCookie?.length})")
                        break
                    }
                }
            } else {
                Log.w(TAG, "No Set-Cookie header with pauth found")
            }

            if (authCookie == null) {
                Log.e(TAG, "Login failed: No auth cookie received")
                isConnected = false
                return Result.failure(Exception("Login failed: No auth cookie"))
            }

            isConnected = true
            Log.i(TAG, "Successfully authenticated with router")
            Result.success(Unit)

        } catch (e: Exception) {
            Log.e(TAG, "Login exception: ${e.javaClass.simpleName} - ${e.message}", e)
            isConnected = false
            authCookie = null
            Result.failure(e)
        }
    }
    // ===== API CALLS =====

    /**
     * Make an API request with automatic authentication and re-authentication on 401.
     */
    private suspend fun makeAuthenticatedRequest(url: String, method: String = "GET", body: String? = null): Result<String> {
        // Ensure we're connected
        Log.d(TAG, "makeAuthenticatedRequest: $method $url")
        ensureConnected().getOrElse { 
            Log.e(TAG, "ensureConnected failed: ${it.message}")
            return Result.failure(it) 
        }

        val requestBuilder = Request.Builder().url(url)
        
        // Add auth cookie header
        authCookie?.let {
            requestBuilder.addHeader("Cookie", "pauth=$it")
            Log.d(TAG, "Added auth cookie to request")
        } ?: Log.w(TAG, "No auth cookie available!")

        // Add body if provided
        if (method == "POST" && body != null) {
            requestBuilder.post(body.toRequestBody(jsonMediaType))
        } else if (method == "GET") {
            requestBuilder.get()
        }

        val request = requestBuilder.build()

        return try {
            val response = httpClient.newCall(request).execute()
            val responseBody = response.body?.string()

            // Check for 401 - session expired, try to re-authenticate once
            if (response.code == 401) {
                Log.w(TAG, "Session expired (401), re-authenticating...")
                ensureConnected(forceReconnect = true).getOrElse { return Result.failure(it) }

                // Retry request with new cookie
                val retryRequest = Request.Builder()
                    .url(url)
                    .addHeader("Cookie", "pauth=$authCookie")

                if (method == "POST" && body != null) {
                    retryRequest.post(body.toRequestBody(jsonMediaType))
                } else {
                    retryRequest.get()
                }

                val retryResponse = httpClient.newCall(retryRequest.build()).execute()
                val retryBody = retryResponse.body?.string()

                if (retryResponse.code == 401) {
                    return Result.failure(Exception("Authentication failed after retry"))
                }

                if (!retryResponse.isSuccessful || retryBody == null) {
                    return Result.failure(Exception("Request failed after retry: HTTP ${retryResponse.code}"))
                }

                return Result.success(retryBody)
            }

            if (!response.isSuccessful || responseBody == null) {
                return Result.failure(Exception("Request failed: HTTP ${response.code}"))
            }

            Result.success(responseBody)

        } catch (e: Exception) {
            Log.e(TAG, "Request exception: ${e.message}", e)
            Result.failure(e)
        }
    }

    /**
     * Get number of currently connected clients.
     */
    suspend fun getConnectedDevicesCount(): Result<Int> {
        val url = "$baseUrl$STATUS_CLIENT_PATH"
        val responseBody = makeAuthenticatedRequest(url).getOrElse { return Result.failure(it) }

        return try {
            val json = JSONObject(responseBody)
            if (json.optString("stat") != "ok") {
                return Result.failure(Exception("API error: ${json.optString("message")}"))
            }

            val resp = json.optJSONObject("response")
            val list = resp?.optJSONArray("list")
            val count = list?.length() ?: 0
            Log.d(TAG, "Connected devices count: $count")
            Result.success(count)

        } catch (e: Exception) {
            Log.e(TAG, "Client status exception: ${e.message}", e)
            Result.failure(e)
        }
    }

    /**
     * Get PepVPN profiles and their status.
     * Returns a map of profileId to Triple(name, type, status).
     */
    suspend fun getPepVpnProfiles(): Result<Map<String, Triple<String, String, String>>> {
        val url = "$baseUrl$STATUS_PEPVPN_PATH"
        val responseBody = makeAuthenticatedRequest(url).getOrElse { return Result.failure(it) }

        return try {
            val json = JSONObject(responseBody)
            if (json.optString("stat") != "ok") {
                return Result.failure(Exception("API error: ${json.optString("message")}"))
            }

            val resp = json.getJSONObject("response")
            val profile = resp.optJSONObject("profile")
            val result = mutableMapOf<String, Triple<String, String, String>>()
            if (profile != null) {
                profile.keys().forEach { key ->
                    // Skip non-object entries like 'order'
                    val obj = profile.optJSONObject(key) ?: return@forEach
                    val name = obj.optString("name", key)
                    val type = obj.optString("type", "")
                    val status = obj.optString("status", "unknown")
                    result[key] = Triple(name, type, status)
                }
            }
            Log.d(TAG, "PepVPN profiles: ${result.keys}")
            Result.success(result)

        } catch (e: Exception) {
            Log.e(TAG, "PepVPN status exception: ${e.message}", e)
            Result.failure(e)
        }
    }

    /**
     * Get WAN connection status for specified connection IDs.
     *
     * @param connIds Space-separated connection IDs (e.g., "1 2 3 4 5")
     * @return Map of connId to WanConnection, or error
     */
    suspend fun getWanStatus(connIds: String = "1 2 3 4 5"): Result<Map<Int, WanConnection>> {
        val url = "$baseUrl$WAN_STATUS_PATH?id=$connIds"
        val responseBody = makeAuthenticatedRequest(url).getOrElse { return Result.failure(it) }

        return try {
            val json = JSONObject(responseBody)
            if (json.optString("stat") != "ok") {
                return Result.failure(Exception("API error: ${json.optString("message")}"))
            }

            val responseObj = json.getJSONObject("response")
            val connections = mutableMapOf<Int, WanConnection>()

            // Parse each connection ID
            responseObj.keys().forEach { key ->
                val connId = key.toIntOrNull()
                if (connId != null) {
                    val connData = responseObj.getJSONObject(key)
                    connections[connId] = WanConnection.fromJson(connId, connData)
                }
            }

            Log.d(TAG, "WAN status retrieved: ${connections.size} connection(s)")
            Result.success(connections)

        } catch (e: Exception) {
            Log.e(TAG, "WAN status exception: ${e.message}", e)
            Result.failure(e)
        }
    }

    /**
     * Get WAN connection usage/allowance data.
     *
     * @return Map of connId to WanUsage, or error
     */
    suspend fun getWanUsage(): Result<Map<Int, WanUsage>> {
        val url = "$baseUrl$WAN_USAGE_PATH"
        val responseBody = makeAuthenticatedRequest(url).getOrElse { return Result.failure(it) }

        return try {
            val json = JSONObject(responseBody)
            if (json.optString("stat") != "ok") {
                return Result.failure(Exception("API error: ${json.optString("message")}"))
            }

            val responseObj = json.getJSONObject("response")
            val usageData = mutableMapOf<Int, WanUsage>()

            // Parse each connection ID
            responseObj.keys().forEach { key ->
                val connId = key.toIntOrNull()
                if (connId != null) {
                    val connData = responseObj.getJSONObject(key)
                    usageData[connId] = WanUsage.fromJson(connId, connData)
                }
            }

            Log.d(TAG, "WAN usage retrieved: ${usageData.size} connection(s)")
            Result.success(usageData)

        } catch (e: Exception) {
            Log.e(TAG, "WAN usage exception: ${e.message}", e)
            Result.failure(e)
        }
    }

    /**
     * Set WAN connection priority.
     *
     * @param connId Connection ID (1-based)
     * @param priority Priority level (1-4), or null to disable
     * @return Result indicating success or error
     */
    suspend fun setWanPriority(connId: Int, priority: Int?): Result<Unit> {
        val payload = JSONObject().apply {
            put("instantActive", true)
            put("list", org.json.JSONArray().apply {
                put(JSONObject().apply {
                    put("connId", connId)
                    if (priority != null) {
                        put("priority", priority)
                    } else {
                        put("enable", false)
                    }
                })
            })
        }.toString()

        val url = "$baseUrl$WAN_PRIORITY_PATH"
        val responseBody = makeAuthenticatedRequest(url, "POST", payload).getOrElse { return Result.failure(it) }

        return try {
            val json = JSONObject(responseBody)
            if (json.optString("stat") != "ok") {
                return Result.failure(Exception("API error: ${json.optString("message")}"))
            }

            val priorityStr = priority?.toString() ?: "disabled"
            Log.i(TAG, "WAN $connId priority set to $priorityStr")
            Result.success(Unit)

        } catch (e: Exception) {
            Log.e(TAG, "Priority change exception: ${e.message}", e)
            Result.failure(e)
        }
    }

    /**
     * Reset cellular modem.
     *
     * @param connId Cellular connection ID (typically 2)
     * @return Result indicating success or error
     */
    suspend fun resetCellularModem(connId: Int): Result<Unit> {
        val payload = JSONObject().apply {
            put("connId", connId.toString())
        }.toString()

        val url = "$baseUrl$CELLULAR_RESET_PATH"
        val responseBody = makeAuthenticatedRequest(url, "POST", payload).getOrElse { return Result.failure(it) }

        return try {
            val json = JSONObject(responseBody)
            if (json.optString("stat") != "ok") {
                return Result.failure(Exception("API error: ${json.optString("message")}"))
            }

            Log.i(TAG, "Cellular modem $connId reset initiated")
            Result.success(Unit)

        } catch (e: Exception) {
            Log.e(TAG, "Modem reset exception: ${e.message}", e)
            Result.failure(e)
        }
    }

    /**
     * Get currently active firmware version string.
     * Parses /api/info.firmware and returns the version of the entry with inUse=true.
     */
    suspend fun getFirmwareVersion(): Result<String> {
        val url = "$baseUrl$INFO_FIRMWARE_PATH"
        val responseBody = makeAuthenticatedRequest(url).getOrElse { return Result.failure(it) }

        return try {
            val json = JSONObject(responseBody)
            if (json.optString("stat") != "ok") {
                return Result.failure(Exception("API error: ${json.optString("message")}"))
            }

            val responseObj = json.getJSONObject("response")
            // Entries are typically "1" and "2". Choose one with inUse=true.
            var version: String? = null
            responseObj.keys().forEach { key ->
                val obj = responseObj.optJSONObject(key)
                if (obj != null && obj.optBoolean("inUse", false)) {
                    version = obj.optString("version", null)
                }
            }
            // Fallback to entry "1" if none marked inUse
            if (version.isNullOrBlank()) {
                val first = responseObj.optJSONObject("1")
                version = first?.optString("version", null)
            }

            version = version ?: "unknown"
            Log.d(TAG, "Active firmware version: $version")
            Result.success(version!!)

        } catch (e: Exception) {
            Log.e(TAG, "Firmware info exception: ${e.message}", e)
            Result.failure(e)
        }
    }

    /**
     * Get system diagnostics (temperature, fans).
     * Queries /cgi-bin/MANGA/api.cgi for thermal sensor and fan speed data.
     */
    suspend fun getSystemDiagnostics(): Result<SystemDiagnostics> {
        // Use the MANGA API endpoint that combines thermal and fan data
        val url = "$baseUrl/cgi-bin/MANGA/api.cgi?func=status.system.info&infoType=thermalSensor%20fanSpeed"
        
        return try {
            val responseBody = makeAuthenticatedRequest(url).getOrNull()
            if (responseBody == null) {
                Log.w(TAG, "No response from system diagnostics endpoint")
                return Result.success(SystemDiagnostics(temperature = null, temperatureThreshold = null))
            }
            
            val json = JSONObject(responseBody)
            
            if (json.optString("stat") != "ok") {
                Log.w(TAG, "System diagnostics API returned error: ${json.optString("message")}")
                return Result.success(SystemDiagnostics(temperature = null, temperatureThreshold = null))
            }

            val responseObj = json.optJSONObject("response") ?: json
            
            // Parse thermal sensor data
            var temperature: Double? = null
            var temperatureThreshold: Double? = null
            val thermalArray = responseObj.optJSONArray("thermalSensor")
            if (thermalArray != null && thermalArray.length() > 0) {
                val thermalObj = thermalArray.getJSONObject(0)
                temperature = thermalObj.optDouble("temperature", Double.NaN).takeIf { !it.isNaN() }
                temperatureThreshold = thermalObj.optDouble("threshold", Double.NaN).takeIf { !it.isNaN() }
                Log.d(TAG, "System diagnostics: temp=${temperature}°C, threshold=${temperatureThreshold}°C")
            } else {
                Log.i(TAG, "Thermal sensor data not available on this device (endpoint returned empty response)")
            }
            
            // Parse fan speed data
            val fans = mutableListOf<FanInfo>()
            val fanArray = responseObj.optJSONArray("fanSpeed")
            if (fanArray != null) {
                for (i in 0 until fanArray.length()) {
                    val fanObj = fanArray.getJSONObject(i)
                    val speedRpm = fanObj.optInt("value").takeIf { it > 0 }
                    val speedPercent = fanObj.optInt("percentage").takeIf { it > 0 }
                    val isActive = fanObj.optBoolean("active", false)
                    
                    fans.add(FanInfo(
                        id = i + 1,
                        name = "Fan ${i + 1}",
                        speedRpm = speedRpm,
                        speedPercent = speedPercent,
                        status = if (isActive) "normal" else "off"
                    ))
                }
                Log.d(TAG, "Found ${fans.size} fans")
            }
            Result.success(SystemDiagnostics(
                temperature = temperature,
                temperatureThreshold = temperatureThreshold,
                fans = fans
            ))

        } catch (e: Exception) {
            Log.e(TAG, "System diagnostics exception: ${e.message}", e)
            Result.success(SystemDiagnostics(temperature = null, temperatureThreshold = null))
        }
    }

    /**
     * Get device information (serial number, model, hardware version).
     * Queries /cgi-bin/MANGA/api.cgi for device info.
     */
    suspend fun getDeviceInfo(): Result<DeviceInfo> {
        val url = "$baseUrl/cgi-bin/MANGA/api.cgi?func=status.system.info&infoType=device"
        
        return try {
            val responseBody = makeAuthenticatedRequest(url).getOrNull()
            if (responseBody == null) {
                Log.w(TAG, "No response from device info endpoint")
                return Result.success(DeviceInfo(serialNumber = "unknown", model = "unknown"))
            }
            
            val json = JSONObject(responseBody)
            
            if (json.optString("stat") != "ok") {
                Log.w(TAG, "Device info API returned error: ${json.optString("message")}")
                return Result.success(DeviceInfo(serialNumber = "unknown", model = "unknown"))
            }

            val responseObj = json.optJSONObject("response") ?: json
            val deviceObj = responseObj.optJSONObject("device")
            
            if (deviceObj == null) {
                Log.w(TAG, "No device object in response")
                return Result.success(DeviceInfo(serialNumber = "unknown", model = "unknown"))
            }
            
            val deviceInfo = DeviceInfo(
                serialNumber = deviceObj.optString("serialNumber", "unknown"),
                model = deviceObj.optString("model", "unknown"),
                hardwareVersion = deviceObj.optString("hardwareRevision", null)
            )
            
            Log.d(TAG, "Device info: ${deviceInfo.model} SN:${deviceInfo.serialNumber}")
            Result.success(deviceInfo)

        } catch (e: Exception) {
            Log.e(TAG, "Device info exception: ${e.message}", e)
            Result.success(DeviceInfo(serialNumber = "unknown", model = "unknown"))
        }
    }

    /**
     * Get bandwidth/traffic statistics.
     * Queries /cgi-bin/MANGA/api.cgi for traffic and bandwidth data.
     */
    suspend fun getTrafficStats(): Result<Map<Int, Pair<Double, Double>>> {
        val url = "$baseUrl/cgi-bin/MANGA/api.cgi?func=status.traffic"
        
        return try {
            val responseBody = makeAuthenticatedRequest(url).getOrNull()
            if (responseBody == null) {
                Log.w(TAG, "No response from traffic stats endpoint")
                return Result.success(emptyMap())
            }
            
            val json = JSONObject(responseBody)
            
            if (json.optString("stat") != "ok") {
                Log.w(TAG, "Traffic stats API returned error: ${json.optString("message")}")
                return Result.success(emptyMap())
            }

            val responseObj = json.optJSONObject("response") ?: json
            val bandwidthObj = responseObj.optJSONObject("bandwidth")
            
            if (bandwidthObj == null) {
                Log.w(TAG, "No bandwidth data in response")
                return Result.success(emptyMap())
            }
            
            val result = mutableMapOf<Int, Pair<Double, Double>>()
            bandwidthObj.keys().forEach { key ->
                val connId = key.toIntOrNull()
                if (connId != null) {
                    val connData = bandwidthObj.optJSONObject(key)
                    if (connData != null) {
                        val overall = connData.optJSONObject("overall")
                        if (overall != null) {
                            // Convert from kbps to Mbps
                            val downloadMbps = overall.optDouble("download", 0.0) / 1000.0
                            val uploadMbps = overall.optDouble("upload", 0.0) / 1000.0
                            result[connId] = Pair(downloadMbps, uploadMbps)
                            Log.d(TAG, "Traffic $connId: ↓${String.format("%.1f", downloadMbps)} Mbps, ↑${String.format("%.1f", uploadMbps)} Mbps")
                        }
                    }
                }
            }
            
            Result.success(result)

        } catch (e: Exception) {
            Log.e(TAG, "Traffic stats exception: ${e.message}", e)
            Result.success(emptyMap())
        }
    }

    /**
     * Get GPS location information.
     * 
     * Privacy Note: This endpoint is only queried when GPS polling is explicitly enabled.
     * GPS polling is disabled by default to respect user privacy.
     * 
     * Queries /api/info.location for GPS data.
     * Returns null if GPS is not available, not supported, or has no fix.
     * 
     * @return Result containing LocationInfo or null if GPS unavailable
     */
    suspend fun getLocation(): Result<LocationInfo?> {
        val url = "$baseUrl$INFO_LOCATION_PATH"
        
        return try {
            val responseBody = makeAuthenticatedRequest(url).getOrNull()
            if (responseBody == null) {
                Log.d(TAG, "No response from location endpoint (GPS may not be supported)")
                return Result.success(null)
            }
            
            val json = JSONObject(responseBody)
            
            if (json.optString("stat") != "ok") {
                Log.d(TAG, "Location API returned error: ${json.optString("message")} (GPS may not be available)")
                return Result.success(null)
            }

            val responseObj = json.optJSONObject("response")
            if (responseObj == null) {
                Log.d(TAG, "No response object in location data")
                return Result.success(null)
            }
            
            // Try both nested "location" object and direct response
            val locationObj = responseObj.optJSONObject("location") ?: responseObj
            val location = LocationInfo.fromJson(locationObj)
            
            if (location != null && location.hasValidFix) {
                Log.d(TAG, "GPS location: ${location.latitude}, ${location.longitude}, " +
                          "speed=${location.speed}m/s, heading=${location.heading}°, " +
                          "altitude=${location.altitude}m, accuracy=${location.accuracy}m")
            } else {
                Log.d(TAG, "No GPS fix available")
            }
            
            Result.success(location)

        } catch (e: Exception) {
            Log.e(TAG, "Location exception: ${e.message}", e)
            Result.success(null)  // Return null instead of error - GPS may simply be unavailable
        }
    }

    /**
     * Close the HTTP client and release resources.
     */
    fun close() {
        httpClient.dispatcher.executorService.shutdown()
        httpClient.connectionPool.evictAll()
    }
}
