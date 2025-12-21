package com.blemqttbridge.plugins.device.onecontrol.protocol

import android.util.Log

/**
 * MyRvLink Event Factory - Decodes MyRvLink events from raw data
 * Based on MyRvLinkEventFactory.TryDecodeEvent() from decompiled code
 */
object MyRvLinkEventFactory {
    private const val TAG = "MyRvLinkEventFactory"
    private const val EVENT_TYPE_INDEX = 0

    /**
     * Try to decode a MyRvLink event from raw data
     * Returns the event type and data, or null if not a valid event
     */
    // Removed unused tryDecodeEvent and MyRvLinkEvent

    /**
     * Check if data looks like a MyRvLink command response
     * Format: [ClientCommandId (2 bytes)][CommandType (1 byte)][Response data...]
     * Valid command types: 0x01 (GetDevices), 0x02 (GetDevicesMetadata)
     */
    fun isCommandResponse(data: ByteArray): Boolean {
        if (data.size < 3) {
            return false
        }
        val commandId = ((data[1].toInt() and 0xFF) shl 8) or (data[0].toInt() and 0xFF)
        if (commandId !in 1..0xFFFE) {
            return false
        }
        val commandType = data[2].toInt() and 0xFF
        return commandType == 0x01 || commandType == 0x02
    }
}

