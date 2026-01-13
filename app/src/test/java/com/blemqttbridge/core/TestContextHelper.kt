package com.blemqttbridge.core

import android.content.Context
import android.content.SharedPreferences
import org.mockito.Mockito.mock
import org.mockito.Mockito.`when`

/**
 * Helper for creating test contexts with functional SharedPreferences mocking
 */
object TestContextHelper {
    
    /**
     * Create a mock Context with in-memory SharedPreferences for testing
     */
    fun createMockContext(prefsName: String = "service_state"): Context {
        val mockContext = mock(Context::class.java)
        val mockPrefs = InMemorySharedPreferences()
        
        `when`(mockContext.getSharedPreferences(prefsName, Context.MODE_PRIVATE))
            .thenReturn(mockPrefs)
        
        return mockContext
    }
    
    /**
     * In-memory implementation of SharedPreferences for testing
     */
    private class InMemorySharedPreferences : SharedPreferences {
        private val data = mutableMapOf<String, Any?>()
        private val listeners = mutableListOf<SharedPreferences.OnSharedPreferenceChangeListener>()
        
        override fun getAll(): Map<String, *> = data.toMap()
        
        override fun getString(key: String?, defValue: String?): String? {
            return (data[key] as? String) ?: defValue
        }
        
        override fun getStringSet(key: String?, defValues: MutableSet<String>?): MutableSet<String>? {
            return (data[key] as? MutableSet<String>) ?: defValues
        }
        
        override fun getInt(key: String?, defValue: Int): Int {
            return (data[key] as? Int) ?: defValue
        }
        
        override fun getLong(key: String?, defValue: Long): Long {
            return (data[key] as? Long) ?: defValue
        }
        
        override fun getFloat(key: String?, defValue: Float): Float {
            return (data[key] as? Float) ?: defValue
        }
        
        override fun getBoolean(key: String?, defValue: Boolean): Boolean {
            return (data[key] as? Boolean) ?: defValue
        }
        
        override fun contains(key: String?): Boolean {
            return data.containsKey(key)
        }
        
        override fun edit(): SharedPreferences.Editor {
            return InMemoryEditor(data, listeners)
        }
        
        override fun registerOnSharedPreferenceChangeListener(listener: SharedPreferences.OnSharedPreferenceChangeListener?) {
            if (listener != null) {
                listeners.add(listener)
            }
        }
        
        override fun unregisterOnSharedPreferenceChangeListener(listener: SharedPreferences.OnSharedPreferenceChangeListener?) {
            if (listener != null) {
                listeners.remove(listener)
            }
        }
    }
    
    /**
     * In-memory implementation of SharedPreferences.Editor for testing
     */
    private class InMemoryEditor(
        private val data: MutableMap<String, Any?>,
        private val listeners: List<SharedPreferences.OnSharedPreferenceChangeListener>
    ) : SharedPreferences.Editor {
        private val edits = mutableMapOf<String, Any?>()
        
        override fun putString(key: String?, value: String?): SharedPreferences.Editor {
            edits[key ?: ""] = value
            return this
        }
        
        override fun putStringSet(key: String?, values: MutableSet<String>?): SharedPreferences.Editor {
            edits[key ?: ""] = values
            return this
        }
        
        override fun putInt(key: String?, value: Int): SharedPreferences.Editor {
            edits[key ?: ""] = value
            return this
        }
        
        override fun putLong(key: String?, value: Long): SharedPreferences.Editor {
            edits[key ?: ""] = value
            return this
        }
        
        override fun putFloat(key: String?, value: Float): SharedPreferences.Editor {
            edits[key ?: ""] = value
            return this
        }
        
        override fun putBoolean(key: String?, value: Boolean): SharedPreferences.Editor {
            edits[key ?: ""] = value
            return this
        }
        
        override fun remove(key: String?): SharedPreferences.Editor {
            edits[key ?: ""] = null
            return this
        }
        
        override fun clear(): SharedPreferences.Editor {
            data.clear()
            return this
        }
        
        override fun commit(): Boolean {
            edits.forEach { (key, value) ->
                if (value == null) {
                    data.remove(key)
                } else {
                    data[key] = value
                }
            }
            return true
        }
        
        override fun apply() {
            commit()
        }
    }
}
