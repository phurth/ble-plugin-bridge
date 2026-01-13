package com.blemqttbridge.plugins.onecontrol.protocol

import org.junit.Test
import kotlin.test.assertEquals
import kotlin.test.assertTrue

/**
 * Unit tests for OneControl protocol parsers
 */
class DeviceStatusParserTest {
    
    @Test
    fun testParseRelayBasicLatchingStatusType1() {
        // Format: [EventType][DeviceTableId][DeviceId1][State1][DeviceId2][State2]
        val data = byteArrayOf(
            0x01, // EventType
            0x02, // DeviceTableId
            0x01, 0x01.toByte(), // Device 1, State ON
            0x02, 0x00 // Device 2, State OFF
        )
        
        val result = DeviceStatusParser.parseRelayBasicLatchingStatusType1(data)
        
        assertEquals(2, result.size)
        assertEquals(0x02, result[0].deviceTableId.toLong())
        assertEquals(0x01, result[0].deviceId.toLong())
        assertEquals(0x01.toByte(), result[0].state)
        assertEquals(0x02, result[1].deviceId.toLong())
        assertEquals(0x00.toByte(), result[1].state)
    }
    
    @Test
    fun testParseRelayBasicLatchingStatusType1_TooShort() {
        val data = byteArrayOf(0x01) // Only event type
        val result = DeviceStatusParser.parseRelayBasicLatchingStatusType1(data)
        assertTrue(result.isEmpty())
    }
    
    @Test
    fun testParseRelayBasicLatchingStatusType1_SingleDevice() {
        val data = byteArrayOf(
            0x01, // EventType
            0x03, // DeviceTableId
            0x05, 0x01.toByte() // Device 5, State ON
        )
        
        val result = DeviceStatusParser.parseRelayBasicLatchingStatusType1(data)
        
        assertEquals(1, result.size)
        assertEquals(0x05, result[0].deviceId.toLong())
        assertEquals(0x01.toByte(), result[0].state)
    }
    
    @Test
    fun testParseRelayBasicLatchingStatusType1_NoDevices() {
        val data = byteArrayOf(
            0x01, // EventType
            0x02 // DeviceTableId only
        )
        
        val result = DeviceStatusParser.parseRelayBasicLatchingStatusType1(data)
        assertTrue(result.isEmpty())
    }
}
