package com.blemqttbridge.plugins.peplink

import android.util.Log
import kotlinx.coroutines.*
import kotlin.math.max

/**
 * Manages independent polling jobs for different Peplink data sources.
 *
 * Allows separate cadences for:
 * - Status polling (10s default): WAN connections, priority, uptime, bandwidth
 * - Usage polling (60s default): Data usage, SIM tracking
 * - Diagnostics polling (30s default): Temperature, fans, device health
 * - VPN polling (60s default): PepVPN connection status
 * - GPS polling (120s default): Location, speed, heading, altitude
 *
 * Each poll type can be independently enabled/disabled, and intervals are user-configurable.
 */
class PeplinkPollingManager(
    private val instanceId: String,
    private val scope: CoroutineScope
) {
    companion object {
        private const val TAG = "PeplinkPollingManager"
        private const val MIN_INTERVAL_SECONDS = 5
        private const val MAX_INTERVAL_SECONDS = 3600
    }

    // ===== CONFIGURATION =====

    data class PollingConfig(
        // Interval in seconds for each poll type
        val statusInterval: Int = 10,
        val usageInterval: Int = 60,
        val diagnosticsInterval: Int = 30,
        val vpnInterval: Int = 60,
        val gpsInterval: Int = 120,

        // Enable/disable flags
        val enableStatusPolling: Boolean = true,
        val enableUsagePolling: Boolean = true,
        val enableDiagnosticsPolling: Boolean = true,
        val enableVpnPolling: Boolean = false,
        val enableGpsPolling: Boolean = false
    ) {
        fun validate(): PollingConfig {
            return copy(
                statusInterval = statusInterval.coerceIn(MIN_INTERVAL_SECONDS, MAX_INTERVAL_SECONDS),
                usageInterval = usageInterval.coerceIn(MIN_INTERVAL_SECONDS, MAX_INTERVAL_SECONDS),
                diagnosticsInterval = diagnosticsInterval.coerceIn(MIN_INTERVAL_SECONDS, MAX_INTERVAL_SECONDS),
                vpnInterval = vpnInterval.coerceIn(MIN_INTERVAL_SECONDS, MAX_INTERVAL_SECONDS),
                gpsInterval = gpsInterval.coerceIn(MIN_INTERVAL_SECONDS, MAX_INTERVAL_SECONDS)
            )
        }
    }

    // ===== STATE =====

    private var config = PollingConfig()
    private var statusJob: Job? = null
    private var usageJob: Job? = null
    private var diagnosticsJob: Job? = null
    private var vpnJob: Job? = null
    private var gpsJob: Job? = null

    // ===== CALLBACKS =====

    var onStatusPoll: (suspend () -> Unit)? = null
    var onUsagePoll: (suspend () -> Unit)? = null
    var onDiagnosticsPoll: (suspend () -> Unit)? = null
    var onVpnPoll: (suspend () -> Unit)? = null
    var onGpsPoll: (suspend () -> Unit)? = null

    // ===== LIFECYCLE =====

    fun configure(newConfig: PollingConfig) {
        val validatedConfig = newConfig.validate()
        val configChanged = (config != validatedConfig)

        config = validatedConfig

        Log.i(TAG, "[$instanceId] Polling config updated: " +
                "status=${config.statusInterval}s(${if(config.enableStatusPolling) "ON" else "OFF"}), " +
                "usage=${config.usageInterval}s(${if(config.enableUsagePolling) "ON" else "OFF"}), " +
                "diag=${config.diagnosticsInterval}s(${if(config.enableDiagnosticsPolling) "ON" else "OFF"}), " +
                "vpn=${config.vpnInterval}s(${if(config.enableVpnPolling) "ON" else "OFF"}), " +
                "gps=${config.gpsInterval}s(${if(config.enableGpsPolling) "ON" else "OFF"})")

        // If polling is running and config changed, restart jobs
        if (configChanged && isRunning()) {
            stop()
            start()
        }
    }

    fun start() {
        if (isRunning()) {
            Log.w(TAG, "[$instanceId] Polling already running")
            return
        }

        Log.i(TAG, "[$instanceId] Starting polling manager")

        if (config.enableStatusPolling) {
            statusJob = startPeriodicPoll("status", config.statusInterval) {
                onStatusPoll?.invoke()
            }
        }

        if (config.enableUsagePolling) {
            usageJob = startPeriodicPoll("usage", config.usageInterval) {
                onUsagePoll?.invoke()
            }
        }

        if (config.enableDiagnosticsPolling) {
            diagnosticsJob = startPeriodicPoll("diagnostics", config.diagnosticsInterval) {
                onDiagnosticsPoll?.invoke()
            }
        }

        if (config.enableVpnPolling) {
            vpnJob = startPeriodicPoll("vpn", config.vpnInterval) {
                onVpnPoll?.invoke()
            }
        }

        if (config.enableGpsPolling) {
            gpsJob = startPeriodicPoll("gps", config.gpsInterval) {
                onGpsPoll?.invoke()
            }
        }

        Log.i(TAG, "[$instanceId] Polling manager started")
    }

    fun stop() {
        Log.i(TAG, "[$instanceId] Stopping polling manager")

        statusJob?.cancel()
        usageJob?.cancel()
        diagnosticsJob?.cancel()
        vpnJob?.cancel()
        gpsJob?.cancel()

        statusJob = null
        usageJob = null
        diagnosticsJob = null
        vpnJob = null
        gpsJob = null

        Log.i(TAG, "[$instanceId] Polling manager stopped")
    }

    fun isRunning(): Boolean {
        return statusJob?.isActive == true ||
                usageJob?.isActive == true ||
                diagnosticsJob?.isActive == true ||
                vpnJob?.isActive == true ||
                gpsJob?.isActive == true
    }

    // ===== PRIVATE HELPERS =====

    private fun startPeriodicPoll(
        name: String,
        intervalSeconds: Int,
        callback: suspend () -> Unit
    ): Job = scope.launch {
        val intervalMs = intervalSeconds * 1000L
        Log.d(TAG, "[$instanceId] Starting periodic poll: $name ($intervalSeconds seconds)")

        while (isActive) {
            try {
                callback()
            } catch (e: CancellationException) {
                Log.d(TAG, "[$instanceId] Poll $name cancelled")
                throw e
            } catch (e: Exception) {
                Log.e(TAG, "[$instanceId] Poll $name failed: ${e.message}", e)
            }

            delay(intervalMs)
        }
    }
}
