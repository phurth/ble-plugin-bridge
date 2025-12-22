package com.blemqttbridge.plugins.onecontrol.protocol

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
     * Uses Crc8.Companion.calculate() with lookup table (initial value 85)
     */
    fun crc8Calculate(data: ByteArray): Int {
        return Crc8.Companion.calculate(data).toInt() and 0xFF
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
     * Encoding that matches HCI capture format
     * Format: 00 40 data... [checksum] 00
     * 
     * The checksum algorithm is UNKNOWN. Testing without checksum.
     */
    fun encodeSimple(data: ByteArray, prependStartFrame: Boolean = true): ByteArray {
        val output = mutableListOf<Byte>()
        
        if (prependStartFrame) {
            output.add(FRAME_CHAR)  // Start frame: 0x00
        }
        
        // Fixed code byte 0x40
        output.add(0x40.toByte())
        
        // Add data bytes
        for (b in data) {
            output.add(b)
        }
        
        // TESTING: Try with checksum = 0x00 (neutral value)
        output.add(0x00.toByte())
        
        // End frame
        output.add(FRAME_CHAR)
        
        return output.toByteArray()
    }
    
    /**
     * Encode data using COBS with CRC8
     * 
     * Rewritten to exactly match decompiled CobsEncoder.cs from OneControl app
     */
    fun encode(data: ByteArray, prependStartFrame: Boolean = true, useCrc: Boolean = true): ByteArray {
        val output = ByteArray(382)  // Max output buffer size
        var outputIndex = 0
        
        // Prepend start frame character if requested
        if (prependStartFrame) {
            output[outputIndex++] = FRAME_CHAR
        }
        
        if (data.isEmpty()) {
            return output.copyOf(outputIndex)
        }
        
        // Build source data with CRC appended (CRC calculated incrementally during encoding)
        val sourceCount = data.size
        val totalCount = if (useCrc) sourceCount + 1 else sourceCount
        
        // CRC calculator - initialized to 85 (0x55)
        var crc = Crc8()
        
        var srcIndex = 0
        
        while (srcIndex < totalCount) {
            // Save position for code byte placeholder
            val codeIndex = outputIndex
            var code = 0
            output[outputIndex++] = 0xFF.toByte()  // Placeholder (official uses 0xFF)
            
            // Encode non-frame bytes
            while (srcIndex < totalCount) {
                val byteVal: Byte
                if (srcIndex < sourceCount) {
                    byteVal = data[srcIndex]
                    if (byteVal == FRAME_CHAR) {
                        break  // Stop at frame character (zero)
                    }
                    crc.update(byteVal)
                } else {
                    // This is the CRC byte position
                    byteVal = crc.value
                    if (byteVal == FRAME_CHAR) {
                        break
                    }
                }
                
                srcIndex++
                output[outputIndex++] = byteVal
                code++
                
                if (code >= MAX_DATA_BYTES) {
                    break
                }
            }
            
            // Handle consecutive frame characters (zeros)
            while (srcIndex < totalCount) {
                val byteVal = if (srcIndex < sourceCount) data[srcIndex] else crc.value
                if (byteVal != FRAME_CHAR) {
                    break
                }
                crc.update(FRAME_CHAR)
                srcIndex++
                code += FRAME_BYTE_COUNT_LSB
                if (code >= MAX_COMPRESSED_FRAME_BYTES) {
                    break
                }
            }
            
            // Write actual code byte
            output[codeIndex] = code.toByte()
        }
        
        // Append frame terminator
        output[outputIndex++] = FRAME_CHAR
        
        return output.copyOf(outputIndex)
    }
}

