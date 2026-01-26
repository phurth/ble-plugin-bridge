package com.blemqttbridge.plugins.peplink

import org.json.JSONObject
import org.junit.Test
import org.junit.Assert.*

/**
 * Unit tests for Peplink data models and parsing logic.
 */
class PeplinkModelsTest {

    // ===== WAN CONNECTION PARSING =====

    @Test
    fun `parse Ethernet WAN connection`() {
        val json = JSONObject("""
            {
                "name": "Ethernet WAN",
                "enable": true,
                "message": "Connected",
                "priority": 1,
                "uptime": 86400,
                "ip": "10.0.0.100",
                "statusLed": "green"
            }
        """.trimIndent())

        val wan = WanConnection.fromJson(1, json)

        assertEquals(1, wan.connId)
        assertEquals("Ethernet WAN", wan.name)
        assertEquals(WanType.ETHERNET, wan.type)
        assertTrue(wan.enabled)
        assertEquals(ConnectionStatus.CONNECTED, wan.status)
        assertEquals("Connected", wan.message)
        assertEquals(1, wan.priority)
        assertEquals(86400, wan.uptime)
        assertEquals("10.0.0.100", wan.ip)
        assertNull(wan.cellular)
        assertNull(wan.wifi)
    }

    @Test
    fun `parse cellular WAN connection with modem info`() {
        val json = JSONObject("""
            {
                "name": "Cat 20 LTE",
                "enable": true,
                "message": "Connected",
                "priority": 1,
                "uptime": 43200,
                "ip": "100.64.0.1",
                "cellular": {
                    "moduleName": "Quectel EM20-G",
                    "signalStrength": -75,
                    "signalQuality": 85,
                    "carrier": "Verizon",
                    "networkType": "LTE",
                    "band": "B13"
                }
            }
        """.trimIndent())

        val wan = WanConnection.fromJson(2, json)

        assertEquals(2, wan.connId)
        assertEquals("Cat 20 LTE", wan.name)
        assertEquals(WanType.CELLULAR, wan.type)
        assertTrue(wan.enabled)
        assertEquals(ConnectionStatus.CONNECTED, wan.status)

        assertNotNull(wan.cellular)
        assertEquals("Quectel EM20-G", wan.cellular!!.moduleName)
        assertEquals(-75, wan.cellular!!.signalStrength)
        assertEquals(85, wan.cellular!!.signalQuality)
        assertEquals("Verizon", wan.cellular!!.carrier)
        assertEquals("LTE", wan.cellular!!.networkType)
        assertEquals("B13", wan.cellular!!.band)
    }

    @Test
    fun `parse WiFi WAN connection`() {
        val json = JSONObject("""
            {
                "name": "WiFi 5GHz",
                "enable": true,
                "message": "Connected",
                "priority": 2,
                "wifi": {
                    "ssid": "Campground-5G",
                    "frequency": "5GHz",
                    "signalStrength": -62,
                    "channel": 36
                }
            }
        """.trimIndent())

        val wan = WanConnection.fromJson(4, json)

        assertEquals(4, wan.connId)
        assertEquals(WanType.WIFI, wan.type)
        assertNotNull(wan.wifi)
        assertEquals("Campground-5G", wan.wifi!!.ssid)
        assertEquals("5GHz", wan.wifi!!.frequency)
        assertEquals(-62, wan.wifi!!.signalStrength)
        assertEquals(36, wan.wifi!!.channel)
    }

    @Test
    fun `parse disabled WAN connection`() {
        val json = JSONObject("""
            {
                "name": "vWAN",
                "enable": false,
                "message": "Disabled"
            }
        """.trimIndent())

        val wan = WanConnection.fromJson(5, json)

        assertEquals(5, wan.connId)
        assertEquals(WanType.VWAN, wan.type)
        assertFalse(wan.enabled)
        assertEquals(ConnectionStatus.DISABLED, wan.status)
        assertNull(wan.priority)
    }

    // ===== USAGE DATA PARSING =====

    @Test
    fun `parse single SIM usage data`() {
        val json = JSONObject("""
            {
                "enable": true,
                "usage": 5120,
                "limit": 10240,
                "unit": "MB",
                "percent": 50,
                "start": "2026-01-01"
            }
        """.trimIndent())

        val usage = WanUsage.fromJson(1, json)

        assertEquals(1, usage.connId)
        assertTrue(usage.enabled)
        assertEquals(5120L, usage.usage)
        assertEquals(10240L, usage.limit)
        assertEquals(50, usage.percent)
        assertEquals("MB", usage.unit)
        assertEquals("2026-01-01", usage.startDate)
        assertNull(usage.simSlots)
    }

    @Test
    fun `parse dual SIM usage data`() {
        val json = JSONObject("""
            {
                "1": {
                    "enable": true,
                    "usage": 2048,
                    "limit": 5120,
                    "percent": 40,
                    "start": "2026-01-01"
                },
                "2": {
                    "enable": true,
                    "usage": 1024,
                    "limit": 5120,
                    "percent": 20,
                    "start": "2026-01-01"
                }
            }
        """.trimIndent())

        val usage = WanUsage.fromJson(2, json)

        assertEquals(2, usage.connId)
        assertNull(usage.usage)  // No top-level usage for multi-SIM
        assertNotNull(usage.simSlots)
        assertEquals(2, usage.simSlots!!.size)

        val sim1 = usage.simSlots!![1]!!
        assertEquals(1, sim1.slotId)
        assertTrue(sim1.enabled)
        assertTrue(sim1.hasUsageTracking)
        assertEquals(2048L, sim1.usage)
        assertEquals(5120L, sim1.limit)
        assertEquals(40, sim1.percent)

        val sim2 = usage.simSlots!![2]!!
        assertEquals(2, sim2.slotId)
        assertEquals(1024L, sim2.usage)
        assertEquals(20, sim2.percent)
    }

    @Test
    fun `parse SIM slot without usage tracking`() {
        val json = JSONObject("""
            {
                "enable": true
            }
        """.trimIndent())

        val simSlot = SimSlotInfo.fromJson(1, json)

        assertEquals(1, simSlot.slotId)
        assertTrue(simSlot.enabled)
        assertFalse(simSlot.hasUsageTracking)
        assertNull(simSlot.usage)
        assertNull(simSlot.limit)
    }

    // ===== CONNECTION TYPE DETECTION =====

    @Test
    fun `detect connection type from structure - cellular`() {
        val json = JSONObject("""
            {
                "name": "LTE Modem",
                "enable": true,
                "cellular": {
                    "moduleName": "Test Modem"
                }
            }
        """.trimIndent())

        val wan = WanConnection.fromJson(2, json)
        assertEquals(WanType.CELLULAR, wan.type)
    }

    @Test
    fun `detect connection type from structure - wifi`() {
        val json = JSONObject("""
            {
                "name": "WiFi Connection",
                "enable": true,
                "wifi": {
                    "ssid": "Test SSID"
                }
            }
        """.trimIndent())

        val wan = WanConnection.fromJson(3, json)
        assertEquals(WanType.WIFI, wan.type)
    }

    @Test
    fun `detect connection type from name - ethernet`() {
        val json = JSONObject("""
            {
                "name": "Ethernet WAN",
                "enable": true
            }
        """.trimIndent())

        val wan = WanConnection.fromJson(1, json)
        assertEquals(WanType.ETHERNET, wan.type)
    }

    @Test
    fun `detect connection type from name - vwan`() {
        val json = JSONObject("""
            {
                "name": "vWAN Connection",
                "enable": true
            }
        """.trimIndent())

        val wan = WanConnection.fromJson(5, json)
        assertEquals(WanType.VWAN, wan.type)
    }

    @Test
    fun `unknown connection type defaults to UNKNOWN`() {
        val json = JSONObject("""
            {
                "name": "Mystery Connection",
                "enable": true
            }
        """.trimIndent())

        val wan = WanConnection.fromJson(99, json)
        assertEquals(WanType.UNKNOWN, wan.type)
    }

    // ===== CONNECTION STATUS DETECTION =====

    @Test
    fun `status detection - connected`() {
        val json = JSONObject("""
            {
                "name": "Test",
                "enable": true,
                "message": "Connected"
            }
        """.trimIndent())

        val wan = WanConnection.fromJson(1, json)
        assertEquals(ConnectionStatus.CONNECTED, wan.status)
    }

    @Test
    fun `status detection - disconnected`() {
        val json = JSONObject("""
            {
                "name": "Test",
                "enable": true,
                "message": "Disconnected"
            }
        """.trimIndent())

        val wan = WanConnection.fromJson(1, json)
        assertEquals(ConnectionStatus.DISCONNECTED, wan.status)
    }

    @Test
    fun `status detection - disabled`() {
        val json = JSONObject("""
            {
                "name": "Test",
                "enable": false,
                "message": "Disabled"
            }
        """.trimIndent())

        val wan = WanConnection.fromJson(1, json)
        assertEquals(ConnectionStatus.DISABLED, wan.status)
    }

    @Test
    fun `status detection - unknown`() {
        val json = JSONObject("""
            {
                "name": "Test",
                "enable": true,
                "message": "Initializing"
            }
        """.trimIndent())

        val wan = WanConnection.fromJson(1, json)
        assertEquals(ConnectionStatus.UNKNOWN, wan.status)
    }

    // ===== EDGE CASES =====

    @Test
    fun `parse connection with minimal data`() {
        val json = JSONObject("""
            {
                "enable": false
            }
        """.trimIndent())

        val wan = WanConnection.fromJson(1, json)

        assertEquals(1, wan.connId)
        assertEquals("WAN 1", wan.name)  // Default name
        assertFalse(wan.enabled)
        assertEquals(ConnectionStatus.DISABLED, wan.status)
    }

    @Test
    fun `parse cellular connection without optional fields`() {
        val json = JSONObject("""
            {
                "name": "LTE",
                "enable": true,
                "cellular": {
                    "moduleName": "Basic Modem"
                }
            }
        """.trimIndent())

        val wan = WanConnection.fromJson(2, json)

        assertNotNull(wan.cellular)
        assertEquals("Basic Modem", wan.cellular!!.moduleName)
        assertNull(wan.cellular!!.signalStrength)
        assertNull(wan.cellular!!.carrier)
    }

    @Test
    fun `hardware config staleness check`() {
        val connections = mapOf(
            1 to WanConnection(
                connId = 1,
                name = "Test",
                type = WanType.ETHERNET,
                enabled = true,
                status = ConnectionStatus.CONNECTED,
                message = null,
                priority = 1,
                uptime = 0,
                ip = null,
                statusLed = null
            )
        )

        // Fresh config
        val freshConfig = PeplinkDiscovery.HardwareConfig(
            wanConnections = connections,
            discoveryTimestamp = System.currentTimeMillis()
        )
        assertFalse(freshConfig.isStale())

        // Stale config (2 hours old)
        val staleConfig = PeplinkDiscovery.HardwareConfig(
            wanConnections = connections,
            discoveryTimestamp = System.currentTimeMillis() - (2 * 60 * 60 * 1000)
        )
        assertTrue(staleConfig.isStale())
    }
}
