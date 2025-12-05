package com.onecontrol.blebridge

/**
 * COBS (Consistent Overhead Byte Stuffing) decoder/encoder with CRC8
 * Based on Python implementation from onecontrol_ble_fresh
 */
object CobsDecoder {
    private const val FRAME_CHAR: Byte = 0x00
    private const val MAX_DATA_BYTES = 63  // 2^6 - 1
    private const val FRAME_BYTE_COUNT_LSB = 64  // 2^6
    private const val MAX_COMPRESSED_FRAME_BYTES = 192  // 255 - 63
    
    /**
     * Calculate CRC8 for data
     * Uses Crc8 object with lookup table (initial value 85)
     */
    fun crc8Calculate(data: ByteArray): Int {
        return Crc8.calculate(data).toInt() and 0xFF
    }
    
    /**
     * Decode COBS-encoded data with CRC8 verification
     */
    fun decode(data: ByteArray, useCrc: Boolean = true): ByteArray? {
        if (data.isEmpty()) return null
        
        val output = mutableListOf<Byte>()
        var codeByte = 0
        val minPayloadSize = if (useCrc) 1 else 0
        
        for (byteVal in data) {
            if (byteVal == FRAME_CHAR) {
                // Frame terminator - check if we have valid data
                if (codeByte != 0) {
                    return null  // Invalid - code byte not consumed
                }
                
                if (output.size <= minPayloadSize) {
                    return null  // No data
                }
                
                // Verify CRC if enabled
                if (useCrc) {
                    if (output.size < 1) {
                        return null
                    }
                    val receivedCrc = output.removeAt(output.size - 1).toInt() and 0xFF
                    val calculatedCrc = crc8Calculate(output.toByteArray())
                    if (receivedCrc != calculatedCrc) {
                        android.util.Log.w("CobsDecoder", "COBS CRC mismatch: received 0x${receivedCrc.toString(16)}, calculated 0x${calculatedCrc.toString(16)}")
                        return null
                    }
                }
                
                return output.toByteArray()
            }
            
            if (codeByte == 0) {
                // Start of new code block
                codeByte = byteVal.toInt() and 0xFF
            } else {
                // Data byte
                codeByte--
                output.add(byteVal)
            }
            
            // Check if we need to insert frame characters
            if ((codeByte and MAX_DATA_BYTES) == 0) {
                while (codeByte > 0) {
                    output.add(FRAME_CHAR)
                    codeByte -= FRAME_BYTE_COUNT_LSB
                }
            }
        }
        
        // No frame terminator found
        return null
    }
    
    /**
     * Encode data using COBS with CRC8
     */
    fun encode(data: ByteArray, prependStartFrame: Boolean = true, useCrc: Boolean = true): ByteArray {
        val output = mutableListOf<Byte>()
        
        // Prepend start frame character if requested
        if (prependStartFrame) {
            output.add(FRAME_CHAR)
        }
        
        // Calculate CRC8
        val dataWithCrc = if (useCrc) {
            val crc = crc8Calculate(data)
            data + crc.toByte()
        } else {
            data
        }
        
        // COBS encoding
        var srcIndex = 0
        val srcLen = dataWithCrc.size
        
        while (srcIndex < srcLen) {
            var code = 1
            var codeIndex = output.size
            output.add(0)  // Placeholder for code byte
            
            // Encode data bytes (up to MAX_DATA_BYTES or until frame char)
            while (srcIndex < srcLen && code < MAX_DATA_BYTES) {
                val byteVal = dataWithCrc[srcIndex]
                if (byteVal == FRAME_CHAR) {
                    break
                }
                output.add(byteVal)
                code++
                srcIndex++
            }
            
            // Handle frame characters
            while (srcIndex < srcLen && dataWithCrc[srcIndex] == FRAME_CHAR) {
                srcIndex++
                code += FRAME_BYTE_COUNT_LSB
                if (code >= MAX_COMPRESSED_FRAME_BYTES) {
                    break
                }
            }
            
            output[codeIndex] = code.toByte()
        }
        
        // Append frame terminator
        output.add(FRAME_CHAR)
        
        return output.toByteArray()
    }
}

