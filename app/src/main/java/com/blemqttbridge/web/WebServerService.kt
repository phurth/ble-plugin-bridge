package com.blemqttbridge.web

import android.app.*
import android.content.Context
import android.content.Intent
import android.os.Build
import android.os.IBinder
import android.util.Log
import androidx.core.app.NotificationCompat
import com.blemqttbridge.R
import com.blemqttbridge.core.BaseBleService
import com.blemqttbridge.data.AppSettings
import kotlinx.coroutines.*
import kotlinx.coroutines.flow.first

/**
 * Independent web server service.
 * Runs separately from BLE service to provide web UI even when BLE is stopped.
 */
class WebServerService : Service() {
    
    companion object {
        private const val TAG = "WebServerService"
        private const val NOTIFICATION_ID = 2
        private const val CHANNEL_ID = "web_server_service"
        
        const val ACTION_STOP_SERVICE = "com.blemqttbridge.web.STOP_SERVICE"
        
        // Service status StateFlow for UI observation
        private val _serviceRunning = kotlinx.coroutines.flow.MutableStateFlow(false)
        val serviceRunning: kotlinx.coroutines.flow.StateFlow<Boolean> = _serviceRunning
    }
    
    private val serviceScope = CoroutineScope(Dispatchers.Default + SupervisorJob())
    private var webServer: WebServerManager? = null
    
    override fun onCreate() {
        super.onCreate()
        Log.i(TAG, "WebServerService created")
        createNotificationChannel()
        _serviceRunning.value = true
    }
    
    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        when (intent?.action) {
            ACTION_STOP_SERVICE -> {
                stopSelf()
                return START_NOT_STICKY
            }
        }
        
        // Start as foreground service
        val notification = createNotification()
        startForeground(NOTIFICATION_ID, notification)
        
        // Start web server
        serviceScope.launch {
            startWebServer()
        }
        
        return START_STICKY
    }
    
    private suspend fun startWebServer() {
        try {
            val settings = AppSettings(applicationContext)
            val port = settings.webServerPort.first()
            
            // Get BLE service instance if running (optional)
            val bleService = BaseBleService.getInstance()
            
            webServer = WebServerManager(applicationContext, bleService, port)
            webServer?.startServer()
            
            Log.i(TAG, "Web server started on port $port")
            updateNotification("Web UI running on port $port")
        } catch (e: Exception) {
            Log.e(TAG, "Failed to start web server", e)
            stopSelf()
        }
    }
    
    private fun stopWebServer() {
        try {
            webServer?.stopServer()
            webServer = null
            Log.i(TAG, "Web server stopped")
        } catch (e: Exception) {
            Log.e(TAG, "Error stopping web server", e)
        }
    }
    
    override fun onDestroy() {
        super.onDestroy()
        stopWebServer()
        serviceScope.cancel()
        _serviceRunning.value = false
        Log.i(TAG, "WebServerService destroyed")
    }
    
    override fun onBind(intent: Intent?): IBinder? = null
    
    private fun createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(
                CHANNEL_ID,
                "Web Server",
                NotificationManager.IMPORTANCE_LOW
            ).apply {
                description = "Web interface server"
                setShowBadge(false)
            }
            
            val notificationManager = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
            notificationManager.createNotificationChannel(channel)
        }
    }
    
    private fun createNotification(message: String = "Starting..."): Notification {
        val stopIntent = Intent(this, WebServerService::class.java).apply {
            action = ACTION_STOP_SERVICE
        }
        val stopPendingIntent = PendingIntent.getService(
            this,
            0,
            stopIntent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        )
        
        return NotificationCompat.Builder(this, CHANNEL_ID)
            .setContentTitle("Web Server")
            .setContentText(message)
            .setSmallIcon(android.R.drawable.ic_dialog_info)
            .setPriority(NotificationCompat.PRIORITY_LOW)
            .setOngoing(true)
            .addAction(
                android.R.drawable.ic_menu_close_clear_cancel,
                "Stop",
                stopPendingIntent
            )
            .build()
    }
    
    private fun updateNotification(message: String) {
        val notification = createNotification(message)
        val notificationManager = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
        notificationManager.notify(NOTIFICATION_ID, notification)
    }
}
