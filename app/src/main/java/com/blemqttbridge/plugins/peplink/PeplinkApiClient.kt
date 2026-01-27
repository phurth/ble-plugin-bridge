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
    }

    // ===== AUTHENTICATION MANAGEMENT =====

    /**
     * Ensure connected with valid session cookie.
     * Thread-safe with mutex to prevent concurrent login attempts.
     */
    private suspend fun ensureConnected(forceReconnect: Boolean = false): Result<Unit> = authMutex.withLock {
        if (isConnected && authCookie != null && !forceReconnect) {
            return Result.success(Unit)
        }

        // Not connected or forced reconnect, authenticate
        Log.i(TAG, "Authenticating with router...")
        return login()
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

        val request = Request.Builder()
            .url("$baseUrl$LOGIN_PATH")
            .post(payload.toRequestBody(jsonMediaType))
            .build()

        return try {
            val response = httpClient.newCall(request).execute()
            val responseBody = response.body?.string()

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
            Log.e(TAG, "Login exception: ${e.message}", e)
            isConnected = false
            Result.failure(e)
        }
    }
    // ===== API CALLS =====

    /**
     * Make an API request with automatic authentication and re-authentication on 401.
     */
    private suspend fun makeAuthenticatedRequest(url: String, method: String = "GET", body: String? = null): Result<String> {
        // Ensure we're connected
        ensureConnected().getOrElse { return Result.failure(it) }

        val requestBuilder = Request.Builder().url(url)
        
        // Add auth cookie header
        authCookie?.let {
            requestBuilder.addHeader("Cookie", "pauth=$it")
        }

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
     * Close the HTTP client and release resources.
     */
    fun close() {
        httpClient.dispatcher.executorService.shutdown()
        httpClient.connectionPool.evictAll()
    }
}
