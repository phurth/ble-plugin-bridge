package com.onecontrol.blebridge

import android.util.Log

/**
 * COBS byte-by-byte decoder (stateful)
 * Based on CobsDecoder.DecodeByte() from decompiled code
 * This maintains state between calls, accumulating bytes until a complete frame is received
 */
class CobsByteDecoder(private val useCrc: Boolean = true) {
    private val TAG = "CobsByteDecoder"
    private val FRAME_CHAR: Byte = 0x00
    private val MAX_DATA_BYTES = 63  // 2^6 - 1
    private val FRAME_BYTE_COUNT_LSB = 64  // 2^6
    
    private var codeByte = 0
    private val outputBuffer = ByteArray(382)  // Max buffer size from decompiled code
    private var destinationNextByteIndex = 0
    private val minPayloadSize = if (useCrc) 1 else 0
    
    /**
     * Decode a single byte
     * Returns decoded frame when frame character (0x00) is received, null otherwise
     */
    fun decodeByte(b: Byte): ByteArray? {
        if (b == FRAME_CHAR) {
            // Frame terminator received
            try {
                if (codeByte != 0) {
                    // Invalid - code byte not consumed
                    reset()
                    return null
                }
                if (destinationNextByteIndex <= minPayloadSize) {
                    // No data
                    reset()
                    return null
                }
                
                if (useCrc) {
                    val receivedCrc = outputBuffer[destinationNextByteIndex - 1]
                    destinationNextByteIndex--
                    val calculatedCrc = Crc8.calculate(outputBuffer, 0, destinationNextByteIndex)
                    if (calculatedCrc != receivedCrc) {
                        Log.w(TAG, "COBS CRC mismatch: received 0x${receivedCrc.toString(16)}, calculated 0x${calculatedCrc.toString(16)}")
                        reset()
                        return null
                    }
                }
                
                val result = outputBuffer.sliceArray(0 until destinationNextByteIndex)
                reset()
                return result
            } finally {
                // Reset is called above, but ensure it happens
            }
        }
        
        if (codeByte <= 0) {
            // Start of new code block
            codeByte = b.toInt() and 0xFF
        } else {
            // Data byte
            codeByte--
            outputBuffer[destinationNextByteIndex++] = b
        }
        
        // Check if we need to insert frame characters
        if ((codeByte and MAX_DATA_BYTES) == 0) {
            while (codeByte > 0) {
                outputBuffer[destinationNextByteIndex++] = FRAME_CHAR
                codeByte -= FRAME_BYTE_COUNT_LSB
            }
        }
        
        return null
    }
    
    /**
     * Reset decoder state
     */
    fun reset() {
        codeByte = 0
        destinationNextByteIndex = 0
    }
    
    /**
     * Check if decoder has partial data
     */
    fun hasPartialData(): Boolean {
        return codeByte > 0 || destinationNextByteIndex > 0
    }
}

