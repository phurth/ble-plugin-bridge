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
}
