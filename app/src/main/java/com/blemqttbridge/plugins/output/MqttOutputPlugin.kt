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
    private var topicPrefix: String = "homeassistant"
    private val commandCallbacks = mutableMapOf<String, (String, String) -> Unit>()
    
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
            topicPrefix = config["topic_prefix"] ?: "homeassistant"
            
            Log.i(TAG, "Initializing MQTT client: $brokerUrl (client: $clientId)")
            
            mqttClient = MqttAndroidClient(context, brokerUrl, clientId).apply {
                setCallback(object : MqttCallback {
                    override fun connectionLost(cause: Throwable?) {
                        Log.w(TAG, "MQTT connection lost", cause)
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
                isCleanSession = false
                connectionTimeout = 30
                keepAliveInterval = 60
                maxInflight = 10
                
                if (!username.isNullOrBlank() && !password.isNullOrBlank()) {
                    userName = username
                    setPassword(password.toCharArray())
                }
                
                // LWT: Mark offline on unexpected disconnect
                val availTopic = "$topicPrefix/$AVAILABILITY_TOPIC"
                setWill(availTopic, "offline".toByteArray(), QOS, true)
            }
            
            mqttClient?.connect(options, null, object : IMqttActionListener {
                override fun onSuccess(asyncActionToken: IMqttToken?) {
                    Log.i(TAG, "MQTT connected successfully")
                    continuation.resume(Result.success(Unit))
                }
                
                override fun onFailure(asyncActionToken: IMqttToken?, exception: Throwable?) {
                    Log.e(TAG, "MQTT connection failed", exception)
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
        val fullTopic = "$topicPrefix/$topic"
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
            val fullPattern = "$topicPrefix/$topicPattern"
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
        val topic = "$topicPrefix/$AVAILABILITY_TOPIC"
        val payload = if (online) "online" else "offline"
        publish(topic, payload, retained = true)
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
}
