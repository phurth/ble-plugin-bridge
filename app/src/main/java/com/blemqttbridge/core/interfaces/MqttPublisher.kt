package com.blemqttbridge.core.interfaces

/**
 * Interface for publishing MQTT messages.
 * 
 * Plugins use this interface to publish state updates and discovery payloads
 * without being coupled to the MQTT implementation (Paho, Mosquitto, etc.).
 * 
 * BaseBleService implements this interface and delegates to the actual MQTT client.
 */
interface MqttPublisher {
    
    /**
     * Publish a state update message.
     * 
     * Used for ongoing device state updates (sensor values, status changes, etc.).
     * 
     * @param topic Full MQTT topic path (e.g., "onecontrol/24dcc3ed1e0a/light/1/state")
     * @param payload Message payload (typically JSON string or simple value)
     * @param retained Whether the message should be retained by the broker
     */
    fun publishState(topic: String, payload: String, retained: Boolean = true)
    
    /**
     * Publish a Home Assistant MQTT Discovery configuration message.
     * 
     * These messages tell Home Assistant about available entities.
     * Should always be retained.
     * 
     * @param topic Discovery config topic (e.g., "homeassistant/light/unique_id/config")
     * @param payload JSON configuration payload
     */
    fun publishDiscovery(topic: String, payload: String)
    
    /**
     * Publish availability status for a device.
     * 
     * Indicates whether the device is online (connected) or offline.
     * Typically published to a topic like "onecontrol/24dcc3ed1e0a/availability".
     * 
     * @param topic Availability topic
     * @param online true if device is online/connected, false if offline
     */
    fun publishAvailability(topic: String, online: Boolean)
    
    /**
     * Check if MQTT client is currently connected to broker.
     * Plugins can use this to avoid publishing when broker is unavailable.
     * 
     * @return true if connected, false otherwise
     */
    fun isConnected(): Boolean
    
    /**
     * Get the configured topic prefix.
     * Used to construct discovery payloads with correct full topic paths.
     * The prefix is prepended to topics in publishState(), so discovery payloads
     * must include it to match the actual published topic paths.
     * 
     * @return The topic prefix (e.g., "homeassistant")
     */
    val topicPrefix: String
    
    /**
     * Update diagnostic status for UI display.
     * @deprecated Use updatePluginStatus instead for per-plugin status tracking
     * @param dataHealthy Whether data is being received (recent frames seen)
     */
    @Deprecated("Use updatePluginStatus instead")
    fun updateDiagnosticStatus(dataHealthy: Boolean)
    
    /**
     * Update BLE connection status for UI display.
     * @deprecated Use updatePluginStatus instead for per-plugin status tracking
     * @param connected Whether BLE is connected
     * @param paired Whether device is paired/authenticated
     */
    @Deprecated("Use updatePluginStatus instead")
    fun updateBleStatus(connected: Boolean, paired: Boolean)
    
    /**
     * Update plugin status for UI display (per-plugin tracking).
     * Called by plugins to update their specific status indicators in the app UI.
     * 
     * @param pluginId Unique plugin identifier (e.g., "easytouch", "gopower", "onecontrol")
     * @param connected Whether BLE is connected to the device
     * @param authenticated Whether device is authenticated (if applicable, same as connected for non-auth plugins)
     * @param dataHealthy Whether data is being received and valid
     */
    fun updatePluginStatus(pluginId: String, connected: Boolean, authenticated: Boolean, dataHealthy: Boolean)
    
    /**
     * Update MQTT connection status for UI display.
     * @param connected Whether MQTT broker is connected
     */
    fun updateMqttStatus(connected: Boolean)
    
    /**
     * Subscribe to command topics.
     * Used by plugins to receive commands from Home Assistant.
     * 
     * @param topicPattern Topic pattern (relative to topic prefix)
     * @param callback Called when a message arrives on the topic
     */
    fun subscribeToCommands(topicPattern: String, callback: (topic: String, payload: String) -> Unit)
    
    /**
     * Log a BLE event for debug log and trace capture.
     * Plugins call this to report BLE operations (reads, writes, notifications) for debugging.
     * 
     * @param message Event description (e.g., "WRITE uuid: hex_data (status=0)")
     */
    fun logBleEvent(message: String)

    /**
     * Log a service-level event to the debug log (serviceLogBuffer).
     * Use this for important diagnostic messages that should survive BLE trace buffer churn.
     * These appear in the "Export Debug Log" output.
     *
     * @param message Event description
     */
    fun logServiceEvent(message: String) { /* default no-op */ }
}
