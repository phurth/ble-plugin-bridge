package com.blemqttbridge.plugins.output

import android.content.Context
import android.util.Log
import com.blemqttbridge.core.interfaces.OutputPluginInterface
import kotlinx.coroutines.suspendCancellableCoroutine
import info.mqtt.android.service.MqttAndroidClient
import org.eclipse.paho.client.mqttv3.*
import kotlin.coroutines.resume
import kotlin.coroutines.resumeWithException

/**
 * MQTT output plugin using Eclipse Paho client.
 * Implements OutputPluginInterface for MQTT broker connectivity.
 */
class MqttOutputPlugin(private val context: Context) : OutputPluginInterface {
    
    companion object {
        private const val TAG = "MqttOutputPlugin"
        private const val QOS = 1
        private const val AVAILABILITY_TOPIC = "availability"
    }
    
    private var mqttClient: MqttAndroidClient? = null
    private var _topicPrefix: String = "homeassistant"
    private val commandCallbacks = mutableMapOf<String, (String, String) -> Unit>()
    private var connectionStatusListener: OutputPluginInterface.ConnectionStatusListener? = null
    
    override fun getTopicPrefix(): String = _topicPrefix
    private var connectOptions: MqttConnectOptions? = null
    
    override fun setConnectionStatusListener(listener: OutputPluginInterface.ConnectionStatusListener?) {
        connectionStatusListener = listener
        // Immediately notify current state
        listener?.onConnectionStatusChanged(isConnected())
    }
    
    override fun getOutputId() = "mqtt"
    
    override fun getOutputName() = "MQTT Broker"
    
    override suspend fun initialize(config: Map<String, String>): Result<Unit> = suspendCancellableCoroutine { continuation ->
        try {
            val brokerUrl = config["broker_url"] 
                ?: return@suspendCancellableCoroutine continuation.resumeWithException(
                    IllegalArgumentException("broker_url required")
                )
            val username = config["username"]
            val password = config["password"]
            val clientId = config["client_id"] ?: "ble_mqtt_bridge_${System.currentTimeMillis()}"
            _topicPrefix = config["topic_prefix"] ?: "homeassistant"
            
            Log.i(TAG, "Initializing MQTT client: $brokerUrl (client: $clientId)")
            
            mqttClient = MqttAndroidClient(context, brokerUrl, clientId).apply {
                setCallback(object : MqttCallback {
                    override fun connectionLost(cause: Throwable?) {
                        Log.w(TAG, "MQTT connection lost", cause)
                        connectionStatusListener?.onConnectionStatusChanged(false)
                        Log.i(TAG, "Automatic reconnect will be attempted...")
                        // Re-subscribe to topics after reconnection
                        resubscribeAll()
                    }
                    
                    override fun messageArrived(topic: String, message: MqttMessage) {
                        val payload = String(message.payload)
                        Log.d(TAG, "Message arrived: $topic = $payload")
                        
                        // Find matching callback
                        commandCallbacks.forEach { (pattern, callback) ->
                            if (topic.matches(Regex(pattern.replace("+", "[^/]+").replace("#", ".*")))) {
                                callback(topic, payload)
                            }
                        }
                    }
                    
                    override fun deliveryComplete(token: IMqttDeliveryToken?) {
                        // Message published successfully
                    }
                })
            }
            
            val options = MqttConnectOptions().apply {
                isAutomaticReconnect = true
                isCleanSession = true  // Use clean sessions to avoid persistence issues
                connectionTimeout = 30
                keepAliveInterval = 120  // Increased to 2 minutes to prevent keep-alive timeouts
                maxInflight = 100  // Increased to handle initial discovery burst
                
                if (!username.isNullOrBlank() && !password.isNullOrBlank()) {
                    userName = username
                    setPassword(password.toCharArray())
                }
                
                // LWT: Mark offline on unexpected disconnect
                val availTopic = "$_topicPrefix/$AVAILABILITY_TOPIC"
                setWill(availTopic, "offline".toByteArray(), QOS, true)
            }
            
            // Store options for potential reconnection
            connectOptions = options
            
            mqttClient?.connect(options, null, object : IMqttActionListener {
                override fun onSuccess(asyncActionToken: IMqttToken?) {
                    Log.i(TAG, "MQTT connected successfully")
                    connectionStatusListener?.onConnectionStatusChanged(true)
                    // Publish "online" to availability topic to clear any "offline" LWT message
                    onMqttConnected()
                    continuation.resume(Result.success(Unit))
                }
                
                override fun onFailure(asyncActionToken: IMqttToken?, exception: Throwable?) {
                    Log.e(TAG, "MQTT connection failed", exception)
                    connectionStatusListener?.onConnectionStatusChanged(false)
                    continuation.resumeWithException(
                        exception ?: Exception("MQTT connection failed")
                    )
                }
            })
            
            continuation.invokeOnCancellation {
                disconnect()
            }
            
        } catch (e: Exception) {
            Log.e(TAG, "MQTT initialization error", e)
            continuation.resumeWithException(e)
        }
    }
    
    override suspend fun publishState(topic: String, payload: String, retained: Boolean) {
        val fullTopic = "$_topicPrefix/$topic"
        publish(fullTopic, payload, retained)
    }
    
    override suspend fun publishDiscovery(topic: String, payload: String) {
        publish(topic, payload, retained = true)
    }
    
    override suspend fun subscribeToCommands(
        topicPattern: String,
        callback: (topic: String, payload: String) -> Unit
    ) = suspendCancellableCoroutine<Unit> { continuation ->
        try {
            val fullPattern = "$_topicPrefix/$topicPattern"
            commandCallbacks[fullPattern] = callback
            
            mqttClient?.subscribe(fullPattern, QOS, null, object : IMqttActionListener {
                override fun onSuccess(asyncActionToken: IMqttToken?) {
                    Log.i(TAG, "Subscribed to: $fullPattern")
                    continuation.resume(Unit)
                }
                
                override fun onFailure(asyncActionToken: IMqttToken?, exception: Throwable?) {
                    Log.e(TAG, "Subscribe failed: $fullPattern", exception)
                    continuation.resumeWithException(
                        exception ?: Exception("Subscribe failed")
                    )
                }
            })
            
            continuation.invokeOnCancellation {
                mqttClient?.unsubscribe(fullPattern)
            }
            
        } catch (e: Exception) {
            Log.e(TAG, "Subscribe error", e)
            continuation.resumeWithException(e)
        }
    }
    
    override suspend fun publishAvailability(online: Boolean) {
        val topic = "$_topicPrefix/$AVAILABILITY_TOPIC"
        val payload = if (online) "online" else "offline"
        publish(topic, payload, retained = true)
    }
    
    /**
     * Called when MQTT connection is established.
     * Publishes "online" to availability topic and discovery payload.
     */
    private fun onMqttConnected() {
        try {
            val client = mqttClient ?: return
            
            // Publish "online" to availability topic (clears LWT "offline")
            val availTopic = "$_topicPrefix/$AVAILABILITY_TOPIC"
            val onlineMessage = MqttMessage("online".toByteArray()).apply {
                qos = QOS
                isRetained = true
            }
            client.publish(availTopic, onlineMessage)
            Log.i(TAG, "📡 Published availability: online")
            
            // Publish HA discovery for availability binary sensor
            publishAvailabilityDiscovery()
            
        } catch (e: Exception) {
            Log.e(TAG, "Error in onMqttConnected", e)
        }
    }
    
    /**
     * Publishes Home Assistant discovery for the bridge availability sensor.
     */
    private fun publishAvailabilityDiscovery() {
        try {
            val client = mqttClient ?: return
            val nodeId = "ble_mqtt_bridge"
            val uniqueId = "ble_mqtt_bridge_availability"
            
            val discoveryPayload = org.json.JSONObject().apply {
                put("name", "BLE Bridge Availability")
                put("unique_id", uniqueId)
                put("state_topic", "$_topicPrefix/$AVAILABILITY_TOPIC")
                put("payload_on", "online")
                put("payload_off", "offline")
                put("device_class", "connectivity")
                put("entity_category", "diagnostic")
                put("device", org.json.JSONObject().apply {
                    put("identifiers", org.json.JSONArray().put("ble_mqtt_bridge"))
                    put("name", "BLE MQTT Bridge")
                    put("model", "Android BLE Bridge")
                    put("manufacturer", "Custom")
                })
            }.toString()
            
            val discoveryTopic = "$_topicPrefix/binary_sensor/$nodeId/availability/config"
            val discoveryMessage = MqttMessage(discoveryPayload.toByteArray()).apply {
                qos = QOS
                isRetained = true
            }
            client.publish(discoveryTopic, discoveryMessage)
            Log.i(TAG, "📡 Published availability discovery to $discoveryTopic")
            
        } catch (e: Exception) {
            Log.e(TAG, "Error publishing availability discovery", e)
        }
    }
    
    override fun disconnect() {
        try {
            if (mqttClient?.isConnected == true) {
                Log.i(TAG, "Disconnecting MQTT client")
                mqttClient?.disconnect()
            }
            mqttClient?.close()
            mqttClient = null
            commandCallbacks.clear()
        } catch (e: Exception) {
            Log.e(TAG, "Error disconnecting MQTT", e)
        }
    }
    
    override fun isConnected(): Boolean {
        return mqttClient?.isConnected == true
    }
    
    override fun getConnectionStatus(): String {
        return when {
            mqttClient == null -> "Not initialized"
            mqttClient?.isConnected == true -> "Connected"
            else -> "Disconnected"
        }
    }
    
    /**
     * Internal publish helper with suspending coroutine.
     */
    private suspend fun publish(topic: String, payload: String, retained: Boolean) = suspendCancellableCoroutine<Unit> { continuation ->
        try {
            val client = mqttClient
            if (client == null || !client.isConnected) {
                Log.w(TAG, "Cannot publish - MQTT not connected")
                continuation.resumeWithException(Exception("MQTT not connected"))
                return@suspendCancellableCoroutine
            }
            
            val message = MqttMessage(payload.toByteArray()).apply {
                qos = QOS
                isRetained = retained
            }
            
            client.publish(topic, message, null, object : IMqttActionListener {
                override fun onSuccess(asyncActionToken: IMqttToken?) {
                    Log.d(TAG, "Published: $topic (${payload.length} bytes, retained=$retained)")
                    continuation.resume(Unit)
                }
                
                override fun onFailure(asyncActionToken: IMqttToken?, exception: Throwable?) {
                    Log.e(TAG, "Publish failed: $topic", exception)
                    continuation.resumeWithException(
                        exception ?: Exception("Publish failed")
                    )
                }
            })
            
        } catch (e: Exception) {
            Log.e(TAG, "Publish error", e)
            continuation.resumeWithException(e)
        }
    }
    
    /**
     * Re-subscribe to all topics after reconnection.
     * Called automatically when connection is restored.
     */
    private fun resubscribeAll() {
        val client = mqttClient
        if (client == null || !client.isConnected) {
            Log.d(TAG, "Cannot resubscribe - client not connected yet")
            return
        }
        
        if (commandCallbacks.isEmpty()) {
            Log.d(TAG, "No subscriptions to restore")
            return
        }
        
        Log.i(TAG, "Resubscribing to ${commandCallbacks.size} topic(s)")
        commandCallbacks.keys.forEach { topic ->
            try {
                client.subscribe(topic, QOS, null, object : IMqttActionListener {
                    override fun onSuccess(asyncActionToken: IMqttToken?) {
                        Log.i(TAG, "Resubscribed to: $topic")
                    }
                    
                    override fun onFailure(asyncActionToken: IMqttToken?, exception: Throwable?) {
                        Log.e(TAG, "Failed to resubscribe to: $topic", exception)
                    }
                })
            } catch (e: Exception) {
                Log.e(TAG, "Error resubscribing to $topic", e)
            }
        }
    }
}
