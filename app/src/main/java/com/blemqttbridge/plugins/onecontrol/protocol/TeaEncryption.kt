package com.blemqttbridge.plugins.onecontrol.protocol

/**
 * TEA (Tiny Encryption Algorithm) encryption
 * Based on C# BleDeviceUnlockManager implementation
 */
object TeaEncryption {
    /**
     * Encrypt seed using TEA algorithm with given cypher
     * @param cypher The 32-bit cypher constant (e.g., 0x8100080D)
     * @param seed The 32-bit seed value to encrypt
     * @return Encrypted 32-bit value
     */
    fun encrypt(cypher: Long, seed: Long): Long {
        var delta = Constants.TEA_DELTA
        var c = cypher
        var s = seed
        
        for (i in 0 until Constants.TEA_ROUNDS) {
            s = (s + (((c shl 4) + Constants.TEA_CONSTANT_1) xor 
                     (c + delta) xor 
                     ((c ushr 5) + Constants.TEA_CONSTANT_2))) and 0xFFFFFFFFL
            c = (c + (((s shl 4) + Constants.TEA_CONSTANT_3) xor 
                     (s + delta) xor 
                     ((s ushr 5) + Constants.TEA_CONSTANT_4))) and 0xFFFFFFFFL
            delta = (delta + Constants.TEA_DELTA) and 0xFFFFFFFFL
        }
        
        return s
    }
    
    /**
     * Decrypt value using TEA algorithm with given cypher
     * @param cypher The 32-bit cypher constant (e.g., 0x8100080D)
     * @param encrypted The 32-bit encrypted value to decrypt
     * @return Decrypted 32-bit value
     */
    fun decrypt(cypher: Long, encrypted: Long): Long {
        var delta = (Constants.TEA_DELTA * Constants.TEA_ROUNDS) and 0xFFFFFFFFL
        var c = cypher
        var s = encrypted
        
        for (i in 0 until Constants.TEA_ROUNDS) {
            c = (c - (((s shl 4) + Constants.TEA_CONSTANT_3) xor 
                     (s + delta) xor 
                     ((s ushr 5) + Constants.TEA_CONSTANT_4))) and 0xFFFFFFFFL
            s = (s - (((c shl 4) + Constants.TEA_CONSTANT_1) xor 
                     (c + delta) xor 
                     ((c ushr 5) + Constants.TEA_CONSTANT_2))) and 0xFFFFFFFFL
            delta = (delta - Constants.TEA_DELTA) and 0xFFFFFFFFL
        }
        
        return s
    }
    
    /**
     * Decrypt byte array using TEA algorithm
     * @param data The encrypted byte array (must be multiple of 8 bytes)
     * @param key The 64-bit key as byte array (8 bytes)
     * @return Decrypted byte array
     */
    fun decryptByteArray(data: ByteArray, key: ByteArray): ByteArray? {
        if (data.size % 8 != 0) {
            return null // TEA requires 8-byte blocks
        }
        if (key.size != 8) {
            return null // TEA requires 64-bit (8-byte) key
        }
        
        val result = ByteArray(data.size)
        val keyLong = bytesToLong(key, 0)
        
        for (i in data.indices step 8) {
            val block = bytesToLong(data, i)
            val decrypted = decrypt(keyLong, block)
            longToBytes(decrypted, result, i)
        }
        
        return result
    }
    
    /**
     * Convert 8 bytes to Long (little-endian)
     */
    private fun bytesToLong(bytes: ByteArray, offset: Int): Long {
        return ((bytes[offset].toLong() and 0xFF)) or
               ((bytes[offset + 1].toLong() and 0xFF) shl 8) or
               ((bytes[offset + 2].toLong() and 0xFF) shl 16) or
               ((bytes[offset + 3].toLong() and 0xFF) shl 24) or
               ((bytes[offset + 4].toLong() and 0xFF) shl 32) or
               ((bytes[offset + 5].toLong() and 0xFF) shl 40) or
               ((bytes[offset + 6].toLong() and 0xFF) shl 48) or
               ((bytes[offset + 7].toLong() and 0xFF) shl 56)
    }
    
    /**
     * Convert Long to 8 bytes (little-endian)
     */
    private fun longToBytes(value: Long, bytes: ByteArray, offset: Int) {
        bytes[offset] = (value and 0xFF).toByte()
        bytes[offset + 1] = ((value shr 8) and 0xFF).toByte()
        bytes[offset + 2] = ((value shr 16) and 0xFF).toByte()
        bytes[offset + 3] = ((value shr 24) and 0xFF).toByte()
        bytes[offset + 4] = ((value shr 32) and 0xFF).toByte()
        bytes[offset + 5] = ((value shr 40) and 0xFF).toByte()
        bytes[offset + 6] = ((value shr 48) and 0xFF).toByte()
        bytes[offset + 7] = ((value shr 56) and 0xFF).toByte()
    }
}

