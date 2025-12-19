package com.blemqttbridge.core

import android.app.Application
import android.content.ComponentCallbacks2
import android.util.Log
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

/**
 * Memory manager for handling low memory conditions.
 * Critical for coexistence with Fully Kiosk on low-end tablets.
 */
class MemoryManager(private val application: Application) {
    
    companion object {
        private const val TAG = "MemoryManager"
    }
    
    private val scope = CoroutineScope(Dispatchers.Main)
    private var memoryCallback: MemoryCallback? = null
    
    interface MemoryCallback {
        /**
         * Called when memory pressure is detected.
         * @param level Memory pressure level (ComponentCallbacks2.TRIM_MEMORY_*)
         */
        suspend fun onMemoryPressure(level: Int)
    }
    
    /**
     * Register a callback for memory pressure events.
     */
    fun setMemoryCallback(callback: MemoryCallback) {
        this.memoryCallback = callback
    }
    
    /**
     * Initialize memory monitoring.
     */
    fun initialize() {
        application.registerComponentCallbacks(object : ComponentCallbacks2 {
            override fun onTrimMemory(level: Int) {
                handleMemoryTrim(level)
            }
            
            override fun onConfigurationChanged(newConfig: android.content.res.Configuration) {
                // Not needed
            }
            
            override fun onLowMemory() {
                handleLowMemory()
            }
        })
        
        Log.i(TAG, "Memory manager initialized")
    }
    
    /**
     * Handle memory trim events from Android system.
     */
    private fun handleMemoryTrim(level: Int) {
        val levelName = when (level) {
            ComponentCallbacks2.TRIM_MEMORY_RUNNING_MODERATE -> "RUNNING_MODERATE"
            ComponentCallbacks2.TRIM_MEMORY_RUNNING_LOW -> "RUNNING_LOW"
            ComponentCallbacks2.TRIM_MEMORY_RUNNING_CRITICAL -> "RUNNING_CRITICAL"
            ComponentCallbacks2.TRIM_MEMORY_UI_HIDDEN -> "UI_HIDDEN"
            ComponentCallbacks2.TRIM_MEMORY_BACKGROUND -> "BACKGROUND"
            ComponentCallbacks2.TRIM_MEMORY_MODERATE -> "MODERATE"
            ComponentCallbacks2.TRIM_MEMORY_COMPLETE -> "COMPLETE"
            else -> "UNKNOWN($level)"
        }
        
        Log.w(TAG, "Memory trim requested: $levelName")
        
        scope.launch {
            try {
                memoryCallback?.onMemoryPressure(level)
            } catch (e: Exception) {
                Log.e(TAG, "Error handling memory pressure", e)
            }
        }
        
        // Take immediate action based on severity
        when (level) {
            ComponentCallbacks2.TRIM_MEMORY_RUNNING_CRITICAL,
            ComponentCallbacks2.TRIM_MEMORY_COMPLETE -> {
                // Critical memory pressure - aggressive cleanup
                Log.w(TAG, "Critical memory pressure - requesting GC")
                System.gc()
            }
            
            ComponentCallbacks2.TRIM_MEMORY_RUNNING_LOW,
            ComponentCallbacks2.TRIM_MEMORY_MODERATE -> {
                // Moderate pressure - suggest GC
                Log.i(TAG, "Moderate memory pressure - suggesting GC")
                System.gc()
            }
        }
    }
    
    /**
     * Handle low memory callback (critical).
     */
    private fun handleLowMemory() {
        Log.e(TAG, "Low memory warning received - critical!")
        
        scope.launch {
            try {
                memoryCallback?.onMemoryPressure(ComponentCallbacks2.TRIM_MEMORY_COMPLETE)
            } catch (e: Exception) {
                Log.e(TAG, "Error handling low memory", e)
            }
        }
        
        // Force GC
        System.gc()
    }
    
    /**
     * Get current memory usage information.
     */
    fun getMemoryInfo(): MemoryInfo {
        val runtime = Runtime.getRuntime()
        val totalMemory = runtime.totalMemory()
        val freeMemory = runtime.freeMemory()
        val maxMemory = runtime.maxMemory()
        val usedMemory = totalMemory - freeMemory
        
        return MemoryInfo(
            usedMemoryMb = usedMemory / (1024 * 1024),
            totalMemoryMb = totalMemory / (1024 * 1024),
            maxMemoryMb = maxMemory / (1024 * 1024),
            freeMemoryMb = freeMemory / (1024 * 1024),
            usagePercentage = (usedMemory.toFloat() / maxMemory.toFloat() * 100).toInt()
        )
    }
    
    /**
     * Log current memory usage.
     */
    fun logMemoryUsage() {
        val info = getMemoryInfo()
        Log.i(TAG, "Memory: ${info.usedMemoryMb}MB used / ${info.maxMemoryMb}MB max (${info.usagePercentage}%)")
    }
    
    data class MemoryInfo(
        val usedMemoryMb: Long,
        val totalMemoryMb: Long,
        val maxMemoryMb: Long,
        val freeMemoryMb: Long,
        val usagePercentage: Int
    )
}
