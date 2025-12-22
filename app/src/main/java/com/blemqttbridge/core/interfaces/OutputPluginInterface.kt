package com.blemqttbridge.core.interfaces

/**
 * Interface for output destination implementations (MQTT, REST, webhook, etc.).
 * Handles publishing device states and receiving commands.
 * 
 * Phase 1: MQTT implementation only
 * Future: REST API, webhooks, WebSocket
 */
interface OutputPluginInterface {
    
    /**
     * Output plugin identifier (e.g., "mqtt", "ha_rest")
     */
    fun getOutputId(): String
    
    /**
     * Human-readable name for UI
     */
    fun getOutputName(): String
    
    /**
     * Initialize the output plugin with user configuration.
     * @param config Plugin-specific configuration (e.g., broker URL, credentials)
     * @return Result.success if initialization succeeded
     */
    suspend fun initialize(config: Map<String, String>): Result<Unit>
    
    /**
     * Publish a device state update.
     * @param topic Topic/path for the state (plugin interprets format)
     * @param payload Serialized state data (typically JSON)
     * @param retained Whether to retain the message (if supported)
     */
    suspend fun publishState(topic: String, payload: String, retained: Boolean = false)
    
    /**
     * Publish a Home Assistant discovery payload.
     * @param topic Discovery topic
     * @param payload JSON discovery configuration
     */
    suspend fun publishDiscovery(topic: String, payload: String)
    
    /**
     * Subscribe to command topics.
     * @param topicPattern Topic pattern to subscribe to
     * @param callback Called when command received
     */
    suspend fun subscribeToCommands(
        topicPattern: String,
        callback: (topic: String, payload: String) -> Unit
    )
    
    /**
     * Publish availability status.
     * @param online True if service is online
     */
    suspend fun publishAvailability(online: Boolean)
    
    /**
     * Disconnect and clean up resources.
     */
    fun disconnect()
    
    /**
     * Check if output is currently connected.
     */
    fun isConnected(): Boolean
    
    /**
     * Get connection status details for debugging.
     */
    fun getConnectionStatus(): String
    
    /**
     * Get the configured topic prefix.
     * Used by plugins to construct discovery payloads with correct full topic paths.
     * @return The topic prefix (e.g., "homeassistant")
     */
    fun getTopicPrefix(): String
    
    /**
     * Connection status listener interface.
     */
    interface ConnectionStatusListener {
        fun onConnectionStatusChanged(connected: Boolean)
    }
    
    /**
     * Set a listener for connection status changes.
     * @param listener The listener to notify on connection state changes
     */
    fun setConnectionStatusListener(listener: ConnectionStatusListener?)
}
