package com.blemqttbridge.core.interfaces

import android.content.Context

/**
 * Plugin interface for network-based polling devices (non-BLE).
 *
 * This interface is designed for devices accessed via HTTP/REST APIs rather than
 * Bluetooth Low Energy. Examples include routers, smart home controllers, or other
 * IP-based devices that expose REST endpoints for monitoring and control.
 *
 * Unlike BleDevicePlugin which handles BLE scanning and GATT callbacks, this interface
 * uses periodic polling to fetch device state and REST calls to send commands.
 *
 * Architecture:
 * - Plugin periodically polls device REST API for state updates
 * - State is published to MQTT for Home Assistant consumption
 * - MQTT commands are translated to REST API calls
 * - Supports multi-instance for managing multiple devices of same type
 */
interface PollingDevicePlugin {

    // ===== IDENTIFICATION =====

    /**
     * Unique plugin type identifier (e.g., "peplink", "victronvrm").
     * Used for plugin factory and configuration lookup.
     * This is stable across instances of the same plugin type.
     */
    val pluginId: String

    /**
     * Unique instance identifier for this plugin instance.
     * For multi-instance plugins, this distinguishes between multiple devices.
     * Format: "{pluginType}_{identifier}" e.g., "peplink_main", "peplink_towed"
     *
     * For single-instance plugins, this is the same as pluginId.
     */
    var instanceId: String

    /**
     * Whether this plugin supports multiple simultaneous instances.
     * If true, multiple devices of this type can be managed independently.
     * Example: Multiple Peplink routers in an RV setup (main + towed vehicle).
     */
    val supportsMultipleInstances: Boolean
        get() = true  // Default to true for polling plugins

    /**
     * Human-readable display name for this device type.
     * Example: "Peplink Router", "Victron VRM Portal"
     */
    val displayName: String

    // ===== LIFECYCLE =====

    /**
     * Initialize the plugin with instance-specific configuration.
     * Called when plugin instance is created or service starts.
     *
     * Plugin should extract all device-specific settings from config:
     * - Device IP address or hostname
     * - API credentials (username, password, tokens)
     * - Custom polling intervals
     * - Device-specific options
     *
     * @param context Android application context
     * @param config Plugin-specific configuration map
     */
    fun initialize(context: Context, config: PluginConfig)

    /**
     * Called when the plugin is being unloaded or service stops.
     * Clean up all resources:
     * - Stop any running coroutines
     * - Close HTTP clients
     * - Cancel pending requests
     */
    fun destroy()

    // ===== POLLING =====

    /**
     * Get the polling interval in milliseconds.
     *
     * This determines how often poll() is called to fetch device state.
     * Can be overridden per-instance via configuration.
     *
     * Recommended intervals:
     * - Fast: 5-10 seconds (critical monitoring data)
     * - Medium: 30-60 seconds (general state)
     * - Slow: 5+ minutes (firmware info, static data)
     *
     * @return Polling interval in milliseconds
     */
    fun getPollingInterval(): Long

    /**
     * Start polling for this device.
     * Called when service starts or plugin is enabled.
     *
     * Plugin should:
     * - Initialize API client
     * - Perform any one-time setup (token acquisition, hardware discovery)
     * - Publish Home Assistant discovery payloads
     * - Begin periodic state polling
     *
     * @param mqttPublisher Interface to publish MQTT messages
     * @return Result indicating success or error
     */
    suspend fun startPolling(mqttPublisher: MqttPublisher): Result<Unit>

    /**
     * Stop polling for this device.
     * Called when service stops or plugin is disabled.
     *
     * Plugin should:
     * - Cancel all pending API requests
     * - Stop periodic polling
     * - Optionally publish offline state
     * - Clean up resources (but keep config for restart)
     */
    suspend fun stopPolling()

    /**
     * Poll the device for current state.
     * Called periodically based on getPollingInterval().
     *
     * Plugin should:
     * - Query device REST API endpoints
     * - Parse response data
     * - Update internal state cache
     * - Publish state changes to MQTT
     * - Handle errors gracefully (retry logic, exponential backoff)
     *
     * @return Result indicating success or error
     */
    suspend fun poll(): Result<Unit>

    // ===== MQTT INTEGRATION =====

    /**
     * Get the base MQTT topic for this device instance.
     *
     * All state and discovery messages will be published under this topic.
     * Example: "peplink/main", "peplink/towed"
     *
     * For multi-instance plugins, this should include the instance identifier.
     *
     * @return Base MQTT topic path (no leading/trailing slashes)
     */
    fun getMqttBaseTopic(): String

    /**
     * Get Home Assistant MQTT Discovery payloads for this device.
     *
     * Called after successful initialization to publish device entities.
     * Each payload is a topic/payload pair for MQTT discovery.
     *
     * For devices with dynamic hardware (like Peplink routers with varying
     * connection types), this should be called after hardware discovery.
     *
     * Example topics:
     * - "homeassistant/binary_sensor/peplink_main_wan1_status/config"
     * - "homeassistant/sensor/peplink_main_wan1_signal/config"
     * - "homeassistant/select/peplink_main_wan1_priority/config"
     *
     * @return List of (topic, jsonPayload) pairs
     */
    fun getDiscoveryPayloads(): List<Pair<String, String>>

    // ===== COMMAND HANDLING =====

    /**
     * Handle incoming MQTT command for this device.
     *
     * Plugin is responsible for:
     * - Parsing the command topic and payload
     * - Translating to appropriate REST API call
     * - Executing the API request
     * - Publishing updated state if successful
     * - Returning success/failure result
     *
     * Example command topics:
     * - "peplink/main/wan/1/priority/command" with payload "2"
     * - "peplink/main/cellular/reset/command" with empty payload
     *
     * @param topic The MQTT command topic (full topic path)
     * @param payload The command payload (often JSON or simple string)
     * @return Result indicating success or error
     */
    suspend fun handleCommand(topic: String, payload: String): Result<Unit>

    // ===== OPTIONAL: DYNAMIC DISCOVERY =====

    /**
     * Perform hardware discovery to detect available capabilities.
     *
     * Optional method for devices with variable hardware configurations.
     * Example: Peplink routers may have different numbers of cellular modems,
     * WiFi radios, or SIM slots depending on model.
     *
     * If implemented:
     * - Called during startPolling() before publishing discovery payloads
     * - Can be triggered manually via API or UI
     * - Results should be cached and used for entity generation
     * - Should log discovered hardware for debugging
     *
     * Default implementation is no-op (device has fixed capabilities).
     *
     * @return Result containing discovered hardware info or error
     */
    suspend fun discoverHardware(): Result<Unit> {
        return Result.success(Unit)
    }
}
