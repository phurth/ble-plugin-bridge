package com.blemqttbridge.plugins.onecontrol.protocol

import android.util.Log

/**
 * CAN Message Parser - Converts V2MessageType messages to CAN message format
 * Based on BleCommunicationsAdapter.OnDataReceived() from decompiled code
 */
object CanMessageParser {
    private const val TAG = "CanMessageParser"

    /**
     * Parse V2MessageType message and convert to CAN message format
     * Returns list of CAN messages (each message has format: length, data_length, message_type, data...)
     */
    fun parseV2Message(rawMessage: ByteArray): List<ByteArray> {
        if (rawMessage.isEmpty()) {
            Log.w(TAG, "Empty message received")
            return emptyList()
        }

        val messageType = V2MessageType.fromByte(rawMessage[0])
        if (messageType == null) {
            Log.w(TAG, "Unknown V2MessageType: 0x${rawMessage[0].toString(16)}")
            return emptyList()
        }

        return when (messageType) {
            V2MessageType.Packed -> parsePackedMessage(rawMessage)
            V2MessageType.ElevenBit -> parseElevenBitMessage(rawMessage)
            V2MessageType.TwentyNineBit -> parseTwentyNineBitMessage(rawMessage)
        }
    }

    /**
     * Parse Packed message (V2MessageType.Packed = 1)
     * Format: 0x01, DeviceAddress, NetworkStatus, IdsCanVersion, DeviceMacAddress (6 bytes), ProductId (2 bytes), ProductInstance, DeviceType, FunctionName (2 bytes), DeviceInstance/FunctionInstance, DeviceCapabilities, DeviceStatusDataLength, DeviceStatusData...
     * 
     * Converts to multiple CAN messages:
     * 1. 11, 8, 0, DeviceAddress, NetworkStatus, IdsCanVersion, DeviceMacAddress (6 bytes)
     * 2. 11, 8, 2, DeviceAddress, ProductId (2 bytes), ProductInstance, DeviceType, FunctionName (2 bytes), DeviceInstance/FunctionInstance, DeviceCapabilities
     * 3. (3+dataLength), dataLength, 3, DeviceAddress, DeviceStatusData...
     */
    private fun parsePackedMessage(rawMessage: ByteArray): List<ByteArray> {
        if (rawMessage.size < 19) {
            Log.w(TAG, "Packed message too short: ${rawMessage.size} bytes")
            return emptyList()
        }

        val deviceAddress = rawMessage[1]
        val networkStatus = rawMessage[2]
        val idsCanVersion = rawMessage[3]
        val deviceMacAddress = rawMessage.sliceArray(4..9)
        val productId = rawMessage.sliceArray(10..11)
        val productInstance = rawMessage[12]
        val deviceType = rawMessage[13]
        val functionName = rawMessage.sliceArray(14..15)
        val deviceInstanceFunctionInstance = rawMessage[16]
        val deviceCapabilities = rawMessage[17]
        val deviceStatusDataLength = rawMessage[18].toInt() and 0xFF
        val deviceStatusData = if (rawMessage.size >= 19 + deviceStatusDataLength) {
            rawMessage.sliceArray(19 until 19 + deviceStatusDataLength)
        } else {
            ByteArray(0)
        }

        val messages = mutableListOf<ByteArray>()

        // Message 1: 11, 8, 0, DeviceAddress, NetworkStatus, IdsCanVersion, DeviceMacAddress (6 bytes)
        val msg1 = ByteArray(12)
        msg1[0] = 11
        msg1[1] = 8
        msg1[2] = 0
        msg1[3] = deviceAddress
        msg1[4] = networkStatus
        msg1[5] = idsCanVersion
        System.arraycopy(deviceMacAddress, 0, msg1, 6, 6)
        messages.add(msg1)

        // Message 2: 11, 8, 2, DeviceAddress, ProductId (2 bytes), ProductInstance, DeviceType, FunctionName (2 bytes), DeviceInstance/FunctionInstance, DeviceCapabilities
        val msg2 = ByteArray(12)
        msg2[0] = 11
        msg2[1] = 8
        msg2[2] = 2
        msg2[3] = deviceAddress
        System.arraycopy(productId, 0, msg2, 4, 2)
        msg2[6] = productInstance
        msg2[7] = deviceType
        System.arraycopy(functionName, 0, msg2, 8, 2)
        msg2[10] = deviceInstanceFunctionInstance
        msg2[11] = deviceCapabilities
        messages.add(msg2)

        // Message 3: (3+dataLength), dataLength, 3, DeviceAddress, DeviceStatusData...
        if (deviceStatusDataLength > 0) {
            val msg3 = ByteArray(4 + deviceStatusDataLength)
            msg3[0] = (3 + deviceStatusDataLength).toByte()
            msg3[1] = deviceStatusDataLength.toByte()
            msg3[2] = 3
            msg3[3] = deviceAddress
            System.arraycopy(deviceStatusData, 0, msg3, 4, deviceStatusDataLength)
            messages.add(msg3)
        }

        Log.d(TAG, "Parsed Packed message: ${messages.size} CAN messages")
        return messages
    }

    /**
     * Parse ElevenBit message (V2MessageType.ElevenBit = 2)
     * Format: 0x02, ..., MESSAGE_TYPE, DeviceAddress, DataLength, Data...
     * Converts to: (3+dataLength), dataLength, MESSAGE_TYPE, DeviceAddress, Data...
     */
    private fun parseElevenBitMessage(rawMessage: ByteArray): List<ByteArray> {
        if (rawMessage.size < 6) {
            Log.w(TAG, "ElevenBit message too short: ${rawMessage.size} bytes")
            return emptyList()
        }

        val messageType = rawMessage[3]
        val deviceAddress = rawMessage[4]
        val dataLength = rawMessage[5].toInt() and 0xFF

        if (rawMessage.size < 6 + dataLength) {
            Log.w(TAG, "ElevenBit message incomplete: expected ${6 + dataLength} bytes, got ${rawMessage.size}")
            return emptyList()
        }

        val message = ByteArray(4 + dataLength)
        message[0] = (3 + dataLength).toByte()
        message[1] = dataLength.toByte()
        message[2] = messageType
        message[3] = deviceAddress
        System.arraycopy(rawMessage, 6, message, 4, dataLength)

        Log.d(TAG, "Parsed ElevenBit message: ${message.size} bytes")
        return listOf(message)
    }

    /**
     * Parse TwentyNineBit message (V2MessageType.TwentyNineBit = 3)
     * Format: 0x03, ..., MESSAGE_TYPE (bits), ..., DataLength, Data...
     * Converts to: (5+dataLength), dataLength, 4 bytes of CAN ID, Data...
     */
    private fun parseTwentyNineBitMessage(rawMessage: ByteArray): List<ByteArray> {
        if (rawMessage.size < 6) {
            Log.w(TAG, "TwentyNineBit message too short: ${rawMessage.size} bytes")
            return emptyList()
        }

        // MESSAGE_TYPE is in bits: ((rawMessage[1] & 0x1C) | (rawMessage[2] & 3))
        val messageType = ((rawMessage[1].toInt() and 0x1C) or (rawMessage[2].toInt() and 3)).toByte()
        val dataLength = rawMessage[5].toInt() and 0xFF

        if (rawMessage.size < 6 + dataLength) {
            Log.w(TAG, "TwentyNineBit message incomplete: expected ${6 + dataLength} bytes, got ${rawMessage.size}")
            return emptyList()
        }

        val message = ByteArray(6 + dataLength)
        message[0] = (5 + dataLength).toByte()
        message[1] = dataLength.toByte()
        // Copy 4 bytes of CAN ID (bytes 1-4 from rawMessage)
        System.arraycopy(rawMessage, 1, message, 2, 4)
        System.arraycopy(rawMessage, 6, message, 6, dataLength)

        Log.d(TAG, "Parsed TwentyNineBit message: ${message.size} bytes")
        return listOf(message)
    }

    /**
     * Extract MyRvLink event data from CAN message
     * CAN messages have format: length, data_length, message_type, data...
     * MyRvLink events start at byte 2 (after length and data_length)
     */
    fun extractMyRvLinkEventData(canMessage: ByteArray): ByteArray? {
        if (canMessage.size < 2) {
            return null
        }
        // Skip length and data_length bytes, return the rest
        return canMessage.sliceArray(2 until canMessage.size)
    }
}

