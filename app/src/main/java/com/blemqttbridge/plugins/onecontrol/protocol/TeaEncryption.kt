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
}

