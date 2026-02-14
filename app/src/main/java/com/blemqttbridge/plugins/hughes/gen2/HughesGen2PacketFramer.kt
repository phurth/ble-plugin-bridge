package com.blemqttbridge.plugins.hughes.gen2

import android.util.Log
import java.io.ByteArrayInputStream
import java.io.ByteArrayOutputStream
import java.nio.ByteBuffer
import java.nio.ByteOrder

/**
 * Packet framer for the Hughes Gen2 binary protocol.
 *
 * Handles building outbound command packets and parsing inbound data
 * from BLE notifications into structured PacketData objects.
 *
 * The framer maintains an internal buffer for packet reassembly since
 * BLE notifications may split packets across multiple chunks.
 *
 * Protocol source: Decompiled com.yw.watchdog Package.java
 */
class HughesGen2PacketFramer {

    companion object {
        private const val TAG = "Gen2PacketFramer"
    }

    /** Rolling message ID (1-100) */
    private var msgIdCounter: Byte = 0

    /** Reassembly buffer for fragmented packets */
    private var buffer: ByteArray? = null

    /**
     * Parsed packet data from the device.
     */
    data class PacketData(
        val version: Byte,
        val msgId: Byte,
        val cmd: Byte,
        val dataLen: Int,
        val body: ByteArray
    ) {
        override fun equals(other: Any?): Boolean {
            if (this === other) return true
            if (other !is PacketData) return false
            return version == other.version && msgId == other.msgId &&
                   cmd == other.cmd && dataLen == other.dataLen &&
                   body.contentEquals(other.body)
        }

        override fun hashCode(): Int {
            var result = version.toInt()
            result = 31 * result + msgId
            result = 31 * result + cmd
            result = 31 * result + dataLen
            result = 31 * result + body.contentHashCode()
            return result
        }
    }

    /**
     * Parsed telemetry data for a single line.
     */
    data class DLData(
        val inputVoltage: Double,   // Volts
        val current: Double,        // Amps
        val power: Double,          // Watts
        val energy: Double,         // kWh
        val outputVoltage: Double,  // Volts (E8/V8+ only, 0 otherwise)
        val backlight: Int,
        val neutralDetection: Boolean,
        val boost: Boolean,         // E8/V8+ only
        val temperature: Int,       // E8/V8+ only, degrees
        val frequency: Double,      // Hz
        val errorCode: Int,
        val relayStatus: Int        // 1=ON, 2=OFF
    )

    /**
     * Parsed error record.
     */
    data class ErrorRecord(
        val errorCode: Int,
        val subCode: Int,
        val startTime: String,  // yyyy-MM-dd HH:mm or "*****-**-** **:**"
        val endTime: String     // yyyy-MM-dd HH:mm or "*****-**-** **:**" (ongoing)
    )

    // ===== BUILDING OUTBOUND PACKETS =====

    /**
     * Get the next rolling message ID (1-100).
     */
    private fun nextMsgId(): Byte {
        msgIdCounter = ((msgIdCounter.toInt() % 100) + 1).toByte()
        return msgIdCounter
    }

    /**
     * Build a framed binary packet for sending to the device.
     *
     * @param cmd Command byte (from HughesGen2Constants.CMD_*)
     * @param body Optional command body (null for commands with no payload)
     * @return Complete framed packet ready to write to BLE characteristic
     */
    fun buildPacket(cmd: Byte, body: ByteArray? = null): ByteArray {
        val out = ByteArrayOutputStream()

        // Magic identifier (4 bytes, big-endian)
        writeInt32BE(out, HughesGen2Constants.PACKET_MAGIC)

        // Version
        out.write(HughesGen2Constants.PROTOCOL_VERSION.toInt())

        // Message ID
        out.write(nextMsgId().toInt())

        // Command
        out.write(cmd.toInt())

        // Data length (2 bytes, big-endian)
        val dataLen = body?.size ?: 0
        writeInt16BE(out, dataLen)

        // Body
        if (body != null) {
            out.write(body)
        }

        // Tail (2 bytes, big-endian)
        writeInt16BE(out, HughesGen2Constants.PACKET_TAIL.toInt() and 0xFFFF)

        return out.toByteArray()
    }

    // ===== COMMAND BUILDERS =====

    /** Build energy reset command (no body) */
    fun buildEnergyReset(): ByteArray = buildPacket(HughesGen2Constants.CMD_ENERGY_RESET)

    /** Build energy restart command (no body) */
    fun buildEnergyRestart(): ByteArray = buildPacket(HughesGen2Constants.CMD_ENERGY_RESTART)

    /** Build relay control command (1=ON, 2=OFF) */
    fun buildSetOpen(on: Boolean): ByteArray {
        val status = if (on) HughesGen2Constants.RELAY_ON else HughesGen2Constants.RELAY_OFF
        return buildPacket(HughesGen2Constants.CMD_SET_OPEN, byteArrayOf(status))
    }

    /** Build backlight level command (0-5) */
    fun buildSetBacklight(level: Int): ByteArray {
        return buildPacket(HughesGen2Constants.CMD_SET_BACKLIGHT, byteArrayOf(level.toByte()))
    }

    /** Build delete error record command */
    fun buildErrorDel(recordId: Int): ByteArray {
        return buildPacket(HughesGen2Constants.CMD_ERROR_DEL, byteArrayOf(recordId.toByte()))
    }

    /** Build set time command (syncs device clock to current time) */
    fun buildSetTime(): ByteArray {
        val cal = java.util.Calendar.getInstance()
        val body = byteArrayOf(
            (cal.get(java.util.Calendar.YEAR) - 2000).toByte(),
            (cal.get(java.util.Calendar.MONTH) + 1).toByte(),
            cal.get(java.util.Calendar.DAY_OF_MONTH).toByte(),
            cal.get(java.util.Calendar.HOUR_OF_DAY).toByte(),
            cal.get(java.util.Calendar.MINUTE).toByte(),
            cal.get(java.util.Calendar.SECOND).toByte()
        )
        return buildPacket(HughesGen2Constants.CMD_SET_TIME, body)
    }

    /** Build neutral detection toggle command */
    fun buildNeutralDetection(enable: Boolean): ByteArray {
        return buildPacket(HughesGen2Constants.CMD_NEUTRAL_DETECTION, byteArrayOf(if (enable) 1 else 0))
    }

    /** Build read energy start time command (no body) */
    fun buildReadStartTime(): ByteArray = buildPacket(HughesGen2Constants.CMD_READ_START_TIME)

    // ===== PARSING INBOUND PACKETS =====

    /**
     * Feed raw BLE notification data into the framer.
     * Returns a parsed PacketData if a complete packet was assembled, or null
     * if more data is needed.
     *
     * Handles packet reassembly across multiple BLE notifications and
     * magic-byte scanning for synchronization.
     */
    fun feedData(data: ByteArray): PacketData? {
        try {
            // Combine with any existing buffer
            val combined = ByteArrayOutputStream()
            val buf = buffer
            if (buf != null) {
                if (buf.size > HughesGen2Constants.MAX_BUFFER_SIZE) {
                    buffer = null
                } else {
                    combined.write(buf)
                }
            }
            combined.write(data)

            val bytes = combined.toByteArray()
            val stream = ByteArrayInputStream(bytes)

            // Scan for magic identifier
            if (stream.available() < 4) {
                buffer = if (stream.available() > 0) bytes else null
                return null
            }

            val magic = readInt32BE(stream)
            if (magic != HughesGen2Constants.PACKET_MAGIC) {
                // Not at packet boundary — try to find magic by skipping byte by byte
                // Reset and skip first byte, save remainder
                val remainder = bytes.copyOfRange(1, bytes.size)
                buffer = if (remainder.isNotEmpty()) remainder else null
                if (buffer != null) {
                    return feedData(ByteArray(0)) // Recurse to try finding magic
                }
                return null
            }

            // Read header (5 bytes: version, msgId, cmd, dataLen)
            if (stream.available() < 5) {
                buffer = bytes
                return null
            }

            val version = stream.read().toByte()
            val msgId = stream.read().toByte()
            val cmd = stream.read().toByte()
            val dataLen = readInt16BE(stream)

            if (dataLen > HughesGen2Constants.MAX_BUFFER_SIZE) {
                Log.e(TAG, "Data length exceeds max: $dataLen")
                buffer = null
                return null
            }

            // Need body + tail
            if (stream.available() < dataLen + HughesGen2Constants.TAIL_SIZE) {
                buffer = bytes
                return null
            }

            // Read body
            val body = ByteArray(dataLen)
            if (dataLen > 0) {
                stream.read(body)
            }

            // Read and validate tail
            val tail = readInt16BE(stream)
            if (tail != (HughesGen2Constants.PACKET_TAIL.toInt() and 0xFFFF)) {
                Log.e(TAG, "Invalid tail: 0x${String.format("%04X", tail)}")
                buffer = null
                return null
            }

            // Save any remaining data for next call
            buffer = if (stream.available() > 0) {
                val remaining = ByteArray(stream.available())
                stream.read(remaining)
                remaining
            } else {
                null
            }

            return PacketData(version, msgId, cmd, dataLen, body)

        } catch (e: Exception) {
            Log.e(TAG, "Error parsing packet", e)
            buffer = null
            return null
        }
    }

    /**
     * Parse a DLReport body into a list of DLData (1 for single-line, 2 for dual-line).
     */
    fun parseDLReport(body: ByteArray): List<DLData>? {
        return when (body.size) {
            HughesGen2Constants.DL_DATA_SIZE -> {
                listOf(parseDLData(body, 0))
            }
            HughesGen2Constants.DL_DATA_DUAL_SIZE -> {
                listOf(
                    parseDLData(body, 0),
                    parseDLData(body, HughesGen2Constants.DL_DATA_SIZE)
                )
            }
            else -> {
                Log.e(TAG, "Invalid DLReport body size: ${body.size} (expected ${ HughesGen2Constants.DL_DATA_SIZE} or ${HughesGen2Constants.DL_DATA_DUAL_SIZE})")
                null
            }
        }
    }

    /**
     * Parse a single 34-byte DLData block from the body at the given offset.
     */
    private fun parseDLData(body: ByteArray, offset: Int): DLData {
        val o = offset
        return DLData(
            inputVoltage = readInt32BE(body, o + HughesGen2Constants.DL_OFFSET_INPUT_VOLTAGE) / 10000.0,
            current = readInt32BE(body, o + HughesGen2Constants.DL_OFFSET_CURRENT) / 10000.0,
            power = readInt32BE(body, o + HughesGen2Constants.DL_OFFSET_POWER) / 10000.0,
            energy = readInt32BE(body, o + HughesGen2Constants.DL_OFFSET_ENERGY) / 10000.0,
            outputVoltage = readInt32BE(body, o + HughesGen2Constants.DL_OFFSET_OUTPUT_VOLTAGE) / 10000.0,
            backlight = body[o + HughesGen2Constants.DL_OFFSET_BACKLIGHT].toInt() and 0xFF,
            neutralDetection = (body[o + HughesGen2Constants.DL_OFFSET_NEUTRAL_DET].toInt() and 0xFF) == 1,
            boost = (body[o + HughesGen2Constants.DL_OFFSET_BOOST].toInt() and 0xFF) == 1,
            temperature = body[o + HughesGen2Constants.DL_OFFSET_TEMPERATURE].toInt() and 0xFF,
            frequency = readInt32BE(body, o + HughesGen2Constants.DL_OFFSET_FREQUENCY) / 100.0,
            errorCode = body[o + HughesGen2Constants.DL_OFFSET_ERROR].toInt() and 0xFF,
            relayStatus = body[o + HughesGen2Constants.DL_OFFSET_STATUS].toInt() and 0xFF
        )
    }

    /**
     * Parse an ErrorReport body into a list of error records.
     */
    fun parseErrorReport(body: ByteArray): List<ErrorRecord> {
        val records = mutableListOf<ErrorRecord>()
        if (body.isEmpty()) return records

        val count = body.size / HughesGen2Constants.ERROR_RECORD_SIZE
        for (i in 0 until count) {
            val offset = i * HughesGen2Constants.ERROR_RECORD_SIZE
            records.add(parseErrorRecord(body, offset))
        }
        return records
    }

    /**
     * Parse a single 16-byte error record.
     */
    private fun parseErrorRecord(body: ByteArray, offset: Int): ErrorRecord {
        val o = offset
        val errorCode = body[o + 2].toInt() and 0xFF
        val subCode = body[o + 15].toInt() and 0xFF

        val startYear = body[o + 4].toInt() and 0xFF
        val startMonth = body[o + 5].toInt() and 0xFF
        val startDay = body[o + 6].toInt() and 0xFF
        val startHour = body[o + 7].toInt() and 0xFF
        val startMin = body[o + 8].toInt() and 0xFF

        val endYear = body[o + 9].toInt() and 0xFF
        val endMonth = body[o + 10].toInt() and 0xFF
        val endDay = body[o + 11].toInt() and 0xFF
        val endHour = body[o + 12].toInt() and 0xFF
        val endMin = body[o + 13].toInt() and 0xFF

        // 0xAA (170 / -86 signed) means invalid/ongoing
        val invalidTime = 0xAA

        val startTime = if (startYear == invalidTime && startMonth == invalidTime) {
            "*****-**-** **:**"
        } else if (startMonth > 12 || startDay > 31 || startHour > 60 || startMin > 60) {
            "*****-**-** **:**"
        } else {
            String.format("%04d-%02d-%02d %02d:%02d", startYear + 2000, startMonth, startDay, startHour, startMin)
        }

        val endTime = if (endYear == invalidTime && endMonth == invalidTime) {
            "*****-**-** **:**"
        } else if (endMonth > 12 || endDay > 31 || endHour > 60 || endMin > 60) {
            "*****-**-** **:**"
        } else {
            String.format("%04d-%02d-%02d %02d:%02d", endYear + 2000, endMonth, endDay, endHour, endMin)
        }

        return ErrorRecord(errorCode, subCode, startTime, endTime)
    }

    /**
     * Clear the internal reassembly buffer.
     */
    fun clear() {
        buffer = null
    }

    // ===== BYTE HELPERS =====

    private fun readInt32BE(stream: ByteArrayInputStream): Int {
        val b0 = stream.read() and 0xFF
        val b1 = stream.read() and 0xFF
        val b2 = stream.read() and 0xFF
        val b3 = stream.read() and 0xFF
        return (b0 shl 24) or (b1 shl 16) or (b2 shl 8) or b3
    }

    private fun readInt32BE(data: ByteArray, offset: Int): Int {
        if (offset + 3 >= data.size) return 0
        return ((data[offset].toInt() and 0xFF) shl 24) or
               ((data[offset + 1].toInt() and 0xFF) shl 16) or
               ((data[offset + 2].toInt() and 0xFF) shl 8) or
               (data[offset + 3].toInt() and 0xFF)
    }

    private fun readInt16BE(stream: ByteArrayInputStream): Int {
        val b0 = stream.read() and 0xFF
        val b1 = stream.read() and 0xFF
        return (b0 shl 8) or b1
    }

    private fun writeInt32BE(out: ByteArrayOutputStream, value: Int) {
        out.write((value ushr 24) and 0xFF)
        out.write((value ushr 16) and 0xFF)
        out.write((value ushr 8) and 0xFF)
        out.write(value and 0xFF)
    }

    private fun writeInt16BE(out: ByteArrayOutputStream, value: Int) {
        out.write((value ushr 8) and 0xFF)
        out.write(value and 0xFF)
    }
}
