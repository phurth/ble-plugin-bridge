package com.blemqttbridge.workers

import android.content.Context
import android.content.Intent
import android.util.Log
import androidx.work.CoroutineWorker
import androidx.work.WorkerParameters
import com.blemqttbridge.core.BaseBleService
import com.blemqttbridge.core.ServiceStateManager

/**
 * Periodic worker that monitors the BLE service health.
 * 
 * Runs every 15 minutes to:
 * - Check if service should be running
 * - Verify service is actually running
 * - Restart service if it was killed
 * 
 * This provides resilience against:
 * - OS killing service due to memory pressure
 * - Service crashes
 * - Aggressive battery optimization
 * 
 * WorkManager advantages over AlarmManager:
 * - Survives device reboot
 * - Respects Doze mode (runs when allowed)
 * - Guaranteed execution (deferred if needed)
 * - No need for wake locks
 */
class ServiceWatchdogWorker(
    context: Context,
    params: WorkerParameters
) : CoroutineWorker(context, params) {
    
    companion object {
        private const val TAG = "ServiceWatchdog"
        const val WORK_NAME = "service_watchdog"
    }
    
    override suspend fun doWork(): Result {
        try {
            Log.d(TAG, "Watchdog check running...")
            
            // Check if service should be running
            val shouldBeRunning = ServiceStateManager.wasServiceRunning(applicationContext)
            
            if (!shouldBeRunning) {
                Log.d(TAG, "Service not configured to run, skipping check")
                return Result.success()
            }
            
            // Check if service is actually running
            val isActuallyRunning = isServiceActuallyRunning()
            
            if (!isActuallyRunning) {
                Log.w(TAG, "⚠️ Service should be running but isn't - restarting!")
                restartService()
                return Result.success()
            }
            
            Log.d(TAG, "✓ Service health check passed")
            return Result.success()
            
        } catch (e: Exception) {
            Log.e(TAG, "Watchdog check failed", e)
            // Retry on failure
            return Result.retry()
        }
    }
    
    /**
     * Check if service is actually running by checking StateFlow.
     * This is more reliable than ActivityManager checks.
     */
    private fun isServiceActuallyRunning(): Boolean {
        // ServiceStateManager tracks actual service state via StateFlow
        // If the service crashed or was killed, this will be false
        return BaseBleService.serviceRunning.value
    }
    
    /**
     * Restart the service by sending START_SCAN intent.
     */
    private fun restartService() {
        try {
            val intent = Intent(applicationContext, BaseBleService::class.java).apply {
                action = BaseBleService.ACTION_START_SCAN
            }
            applicationContext.startForegroundService(intent)
            Log.i(TAG, "Service restart initiated")
        } catch (e: Exception) {
            Log.e(TAG, "Failed to restart service", e)
        }
    }
}
