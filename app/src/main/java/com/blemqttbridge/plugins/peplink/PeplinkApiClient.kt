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
 * Peplink Router API Client with automatic token management.
 *
 * Handles:
 * - OAuth2-like token authentication with 48-hour expiration
 * - Automatic token refresh with exponential backoff
 * - All API endpoint calls (status, control, info)
 * - Error handling and retry logic
 *
 * Based on Peplink API Documentation for Firmware 8.5.0.
 */
class PeplinkApiClient(
    private val baseUrl: String,  // e.g., "http://192.168.1.1"
    private val clientId: String,
    private val clientSecret: String
) {
    private val httpClient = OkHttpClient.Builder()
        .connectTimeout(10, TimeUnit.SECONDS)
        .readTimeout(30, TimeUnit.SECONDS)
        .writeTimeout(10, TimeUnit.SECONDS)
        .build()

    private val jsonMediaType = "application/json; charset=utf-8".toMediaType()

    // Token management
    private var accessToken: String? = null
    private var tokenExpiresAt: Long = 0  // System.currentTimeMillis() when token expires
    private val tokenMutex = Mutex()

    // Retry configuration
    private var tokenRefreshAttempts = 0
    private val maxTokenRefreshAttempts = 3
    private val tokenRefreshBackoffMs = longArrayOf(1000, 5000, 15000)

    companion object {
        private const val TAG = "PeplinkApiClient"
        private const val TOKEN_GRANT_PATH = "/api/auth.token.grant"
        private const val WAN_STATUS_PATH = "/api/status.wan.connection"
        private const val WAN_USAGE_PATH = "/api/status.wan.connection.allowance"
        private const val WAN_PRIORITY_PATH = "/api/config.wan.connection.priority"
        private const val CELLULAR_RESET_PATH = "/api/cmd.cellularModule.reset"
    }

    // ===== TOKEN MANAGEMENT =====

    /**
     * Get a valid access token, refreshing if necessary.
     * Thread-safe with mutex to prevent concurrent refresh attempts.
     */
    private suspend fun getAccessToken(): Result<String> = tokenMutex.withLock {
        // Check if current token is still valid (with 5-minute buffer)
        val now = System.currentTimeMillis()
        if (accessToken != null && tokenExpiresAt > now + (5 * 60 * 1000)) {
            return Result.success(accessToken!!)
        }

        // Token expired or doesn't exist, refresh it
        Log.i(TAG, "Access token expired or missing, refreshing...")
        return refreshToken()
    }

    /**
     * Request a new access token from the router.
     * Uses exponential backoff on failures.
     */
    private suspend fun refreshToken(): Result<String> {
        val payload = JSONObject().apply {
            put("clientId", clientId)
            put("clientSecret", clientSecret)
            put("scope", "api")
        }.toString()

        val request = Request.Builder()
            .url("$baseUrl$TOKEN_GRANT_PATH")
            .post(payload.toRequestBody(jsonMediaType))
            .build()

        return try {
            val response = httpClient.newCall(request).execute()
            val responseBody = response.body?.string()

            if (!response.isSuccessful) {
                Log.e(TAG, "Token grant failed: HTTP ${response.code}")
                return handleTokenRefreshFailure()
            }

            if (responseBody == null) {
                Log.e(TAG, "Token grant failed: Empty response body")
                return handleTokenRefreshFailure()
            }

            val json = JSONObject(responseBody)
            if (json.optString("stat") != "ok") {
                val errorMsg = json.optString("message", "Unknown error")
                Log.e(TAG, "Token grant failed: $errorMsg")
                return handleTokenRefreshFailure()
            }

            val responseObj = json.getJSONObject("response")
            val token = responseObj.getString("accessToken")
            val expiresIn = responseObj.optInt("expiresIn", 172800)  // Default 48 hours

            // Store token with expiration time
            accessToken = token
            tokenExpiresAt = System.currentTimeMillis() + (expiresIn * 1000L)
            tokenRefreshAttempts = 0  // Reset failure counter

            Log.i(TAG, "Access token refreshed successfully (expires in ${expiresIn}s)")
            Result.success(token)

        } catch (e: Exception) {
            Log.e(TAG, "Token grant exception: ${e.message}", e)
            handleTokenRefreshFailure()
        }
    }

    /**
     * Handle token refresh failure with exponential backoff.
     */
    private suspend fun handleTokenRefreshFailure(): Result<String> {
        tokenRefreshAttempts++
        if (tokenRefreshAttempts >= maxTokenRefreshAttempts) {
            val error = "Token refresh failed after $maxTokenRefreshAttempts attempts"
            Log.e(TAG, error)
            tokenRefreshAttempts = 0  // Reset for next cycle
            return Result.failure(Exception(error))
        }

        val backoffMs = tokenRefreshBackoffMs[tokenRefreshAttempts - 1]
        Log.w(TAG, "Token refresh attempt $tokenRefreshAttempts failed, retrying in ${backoffMs}ms...")
        kotlinx.coroutines.delay(backoffMs)

        return refreshToken()
    }

    // ===== API CALLS =====

    /**
     * Get WAN connection status for specified connection IDs.
     *
     * @param connIds Space-separated connection IDs (e.g., "1 2 3 4 5")
     * @return Map of connId to WanConnection, or error
     */
    suspend fun getWanStatus(connIds: String = "1 2 3 4 5"): Result<Map<Int, WanConnection>> {
        val token = getAccessToken().getOrElse { return Result.failure(it) }

        val url = "$baseUrl$WAN_STATUS_PATH?accessToken=$token&id=$connIds"
        val request = Request.Builder()
            .url(url)
            .get()
            .build()

        return try {
            val response = httpClient.newCall(request).execute()
            val responseBody = response.body?.string()

            if (!response.isSuccessful || responseBody == null) {
                return Result.failure(Exception("WAN status request failed: HTTP ${response.code}"))
            }

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
        val token = getAccessToken().getOrElse { return Result.failure(it) }

        val url = "$baseUrl$WAN_USAGE_PATH?accessToken=$token"
        val request = Request.Builder()
            .url(url)
            .get()
            .build()

        return try {
            val response = httpClient.newCall(request).execute()
            val responseBody = response.body?.string()

            if (!response.isSuccessful || responseBody == null) {
                return Result.failure(Exception("WAN usage request failed: HTTP ${response.code}"))
            }

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
        val token = getAccessToken().getOrElse { return Result.failure(it) }

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

        val url = "$baseUrl$WAN_PRIORITY_PATH?accessToken=$token"
        val request = Request.Builder()
            .url(url)
            .post(payload.toRequestBody(jsonMediaType))
            .build()

        return try {
            val response = httpClient.newCall(request).execute()
            val responseBody = response.body?.string()

            if (!response.isSuccessful || responseBody == null) {
                return Result.failure(Exception("Priority change failed: HTTP ${response.code}"))
            }

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
        val token = getAccessToken().getOrElse { return Result.failure(it) }

        val payload = JSONObject().apply {
            put("connId", connId.toString())
        }.toString()

        val url = "$baseUrl$CELLULAR_RESET_PATH?accessToken=$token"
        val request = Request.Builder()
            .url(url)
            .post(payload.toRequestBody(jsonMediaType))
            .build()

        return try {
            val response = httpClient.newCall(request).execute()
            val responseBody = response.body?.string()

            if (!response.isSuccessful || responseBody == null) {
                return Result.failure(Exception("Modem reset failed: HTTP ${response.code}"))
            }

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
     * Close the HTTP client and release resources.
     */
    fun close() {
        httpClient.dispatcher.executorService.shutdown()
        httpClient.connectionPool.evictAll()
    }
}
