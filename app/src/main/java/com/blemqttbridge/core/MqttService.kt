package com.blemqttbridge.core

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.app.Service
import android.content.Context
import android.content.Intent
import android.os.Build
import android.os.IBinder
import android.util.Log
import androidx.core.app.NotificationCompat
import com.blemqttbridge.data.AppSettings
import com.blemqttbridge.plugins.output.MqttOutputPlugin
import com.blemqttbridge.core.interfaces.OutputPluginInterface
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.launch
import kotlinx.coroutines.runBlocking

/**
 * Standalone MQTT service that manages the MQTT broker connection.
 * Can run independently of BLE scanning or HTTP polling.
 * Provides MQTT publishing interface to other components via getInstance().
 */
class MqttService : Service() {
    
    private val serviceScope = CoroutineScope(SupervisorJob() + Dispatchers.Main)
    private var mqttPlugin: MqttOutputPlugin? = null
    private var isConnected = false
    
    companion object {
        private const val TAG = "MqttService"
        const val NOTIFICATION_ID = 2000
        const val CHANNEL_ID = "mqtt_service_channel"
        
        const val ACTION_START = "com.blemqttbridge.mqtt.START"
        const val ACTION_STOP = "com.blemqttbridge.mqtt.STOP"
        const val ACTION_RECONNECT = "com.blemqttbridge.mqtt.RECONNECT"
        
        @Volatile
        private var instance: MqttService? = null
        
        fun getInstance(): MqttService? = instance
    }
    
    override fun onCreate() {
        super.onCreate()
        instance = this
        createNotificationChannel()
        Log.i(TAG, "MQTT Service created")
    }
    
    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        Log.i(TAG, "onStartCommand: action=${intent?.action}")
        
        when (intent?.action) {
            ACTION_START -> {
                startForeground(NOTIFICATION_ID, createNotification("Connecting..."))
                serviceScope.launch {
                    connectMqtt()
                }
            }
            ACTION_STOP -> {
                disconnectMqtt()
                stopSelf()
            }
            ACTION_RECONNECT -> {
                serviceScope.launch {
                    disconnectMqtt()
                    connectMqtt()
                }
            }
            else -> {
                // Service started without specific action - check settings
                startForeground(NOTIFICATION_ID, createNotification("Starting..."))
                serviceScope.launch {
                    val settings = AppSettings(applicationContext)
                    val mqttEnabled = settings.mqttEnabled.first()
                    if (mqttEnabled) {
                        connectMqtt()
                    } else {
                        Log.i(TAG, "MQTT disabled in settings, stopping service")
                        stopSelf()
                    }
                }
            }
        }
        
        return START_STICKY
    }
    
    override fun onBind(intent: Intent?): IBinder? = null
    
    override fun onDestroy() {
        Log.i(TAG, "MQTT Service destroyed")
        disconnectMqtt()
        serviceScope.cancel()
        instance = null
        super.onDestroy()
    }
    
    private suspend fun connectMqtt() {
        try {
            Log.i(TAG, "Connecting to MQTT broker...")
            updateNotification("Connecting...")
            
            val settings = AppSettings(applicationContext)
            val config = mapOf(
                "broker_url" to settings.mqttBrokerUrl.first(),
                "client_id" to settings.mqttClientId.first(),
                "username" to settings.mqttUsername.first(),
                "password" to settings.mqttPassword.first(),
                "topic_prefix" to settings.mqttTopicPrefix.first()
            )
            
            mqttPlugin = MqttOutputPlugin(applicationContext).apply {
                setConnectionStatusListener(object : OutputPluginInterface.ConnectionStatusListener {
                    override fun onConnectionStatusChanged(connected: Boolean) {
                        isConnected = connected
                        updateNotification(if (connected) "Connected" else "Disconnected")
                    }
                })
            }
            
            val result = mqttPlugin?.initialize(config)
            if (result?.isSuccess == true) {
                Log.i(TAG, "MQTT connected successfully")
                isConnected = true
                updateNotification("Connected")
            } else {
                Log.e(TAG, "Failed to connect to MQTT: ${result?.exceptionOrNull()}")
                isConnected = false
                updateNotification("Connection Failed")
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error connecting to MQTT", e)
            isConnected = false
            updateNotification("Error: ${e.message}")
        }
    }
    
    private fun disconnectMqtt() {
        Log.i(TAG, "Disconnecting from MQTT broker...")
        runBlocking {
            mqttPlugin?.disconnect()
        }
        mqttPlugin = null
        isConnected = false
    }
    
    /**
     * Get the MQTT publisher interface for other components to use.
     * Returns null if MQTT is not connected.
     */
    fun getMqttPublisher(): MqttOutputPlugin? {
        return if (isConnected) mqttPlugin else null
    }
    
    /**
     * Check if MQTT is currently connected.
     */
    fun isConnected(): Boolean = isConnected
    
    private fun createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(
                CHANNEL_ID,
                "MQTT Service",
                NotificationManager.IMPORTANCE_LOW
            ).apply {
                description = "MQTT broker connection status"
                setShowBadge(false)
            }
            
            val manager = getSystemService(NotificationManager::class.java)
            manager.createNotificationChannel(channel)
        }
    }
    
    private fun createNotification(status: String): Notification {
        val pendingIntent = PendingIntent.getActivity(
            this,
            0,
            packageManager.getLaunchIntentForPackage(packageName),
            PendingIntent.FLAG_IMMUTABLE or PendingIntent.FLAG_UPDATE_CURRENT
        )
        
        return NotificationCompat.Builder(this, CHANNEL_ID)
            .setContentTitle("MQTT Service")
            .setContentText(status)
            .setSmallIcon(android.R.drawable.ic_dialog_info)
            .setOngoing(true)
            .setContentIntent(pendingIntent)
            .setPriority(NotificationCompat.PRIORITY_LOW)
            .build()
    }
    
    private fun updateNotification(status: String) {
        val notification = createNotification(status)
        val manager = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
        manager.notify(NOTIFICATION_ID, notification)
    }
}
