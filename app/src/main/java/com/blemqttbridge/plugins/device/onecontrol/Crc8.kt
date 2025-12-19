package com.blemqttbridge.plugins.device.onecontrol

/**
 * CRC8 stateful calculator matching decompiled C# Crc8 struct
 * Uses lookup table with initial value 85 (0x55)
 */
class Crc8 {
    private var _value: Int = RESET_VALUE
    
    val value: Byte
        get() = _value.toByte()
    
    fun reset() {
        _value = RESET_VALUE
    }
    
    fun update(b: Byte) {
        _value = TABLE[(_value xor (b.toInt() and 0xFF)) and 0xFF].toInt() and 0xFF
    }
    
    companion object {
        private const val RESET_VALUE: Int = 85  // 0x55
        
        // CRC8 lookup table from decompiled C# code
        private val TABLE: ByteArray = byteArrayOf(
            0, 94, (-68).toByte(), (-30).toByte(), 97, 63, (-35).toByte(), (-125).toByte(), (-62).toByte(), (-100).toByte(),
            126, 32, (-93).toByte(), (-3).toByte(), 31, 65, (-99).toByte(), (-61).toByte(), 33, 127,
            (-4).toByte(), (-94).toByte(), 64, 30, 95, 1, (-29).toByte(), (-67).toByte(), 62, 96,
            (-126).toByte(), (-36).toByte(), 35, 125, (-97).toByte(), (-63).toByte(), 66, 28, (-2).toByte(), (-96).toByte(),
            (-31).toByte(), (-65).toByte(), 93, 3, (-128).toByte(), (-34).toByte(), 60, 98, (-66).toByte(), (-32).toByte(),
            2, 92, (-33).toByte(), (-127).toByte(), 99, 61, 124, 34, (-64).toByte(), (-98).toByte(),
            29, 67, (-95).toByte(), (-1).toByte(), 70, 24, (-6).toByte(), (-92).toByte(), 39, 121,
            (-101).toByte(), (-59).toByte(), (-124).toByte(), (-38).toByte(), 56, 102, (-27).toByte(), (-69).toByte(), 89, 7,
            (-37).toByte(), (-123).toByte(), 103, 57, (-70).toByte(), (-28).toByte(), 6, 88, 25, 71,
            (-91).toByte(), (-5).toByte(), 120, 38, (-60).toByte(), (-102).toByte(), 101, 59, (-39).toByte(), (-121).toByte(),
            4, 90, (-72).toByte(), (-26).toByte(), (-89).toByte(), (-7).toByte(), 27, 69, (-58).toByte(), (-104).toByte(),
            122, 36, (-8).toByte(), (-90).toByte(), 68, 26, (-103).toByte(), (-57).toByte(), 37, 123,
            58, 100, (-122).toByte(), (-40).toByte(), 91, 5, (-25).toByte(), (-71).toByte(), (-116).toByte(), (-46).toByte(),
            48, 110, (-19).toByte(), (-77).toByte(), 81, 15, 78, 16, (-14).toByte(), (-84).toByte(),
            47, 113, (-109).toByte(), (-51).toByte(), 17, 79, (-83).toByte(), (-13).toByte(), 112, 46,
            (-52).toByte(), (-110).toByte(), (-45).toByte(), (-115).toByte(), 111, 49, (-78).toByte(), (-20).toByte(), 14, 80,
            (-81).toByte(), (-15).toByte(), 19, 77, (-50).toByte(), (-112).toByte(), 114, 44, 109, 51,
            (-47).toByte(), (-113).toByte(), 12, 82, (-80).toByte(), (-18).toByte(), 50, 108, (-114).toByte(), (-48).toByte(),
            83, 13, (-17).toByte(), (-79).toByte(), (-16).toByte(), (-82).toByte(), 76, 18, (-111).toByte(), (-49).toByte(),
            45, 115, (-54).toByte(), (-108).toByte(), 118, 40, (-85).toByte(), (-11).toByte(), 23, 73,
            8, 86, (-76).toByte(), (-22).toByte(), 105, 55, (-43).toByte(), (-117).toByte(), 87, 9,
            (-21).toByte(), (-75).toByte(), 54, 104, (-118).toByte(), (-44).toByte(), (-107).toByte(), (-53).toByte(), 41, 119,
            (-12).toByte(), (-86).toByte(), 72, 22, (-23).toByte(), (-73).toByte(), 85, 11, (-120).toByte(), (-42).toByte(),
            52, 106, 43, 117, (-105).toByte(), (-55).toByte(), 74, 20, (-10).toByte(), (-88).toByte(),
            116, 42, (-56).toByte(), (-106).toByte(), 21, 75, (-87).toByte(), (-9).toByte(), (-74).toByte(), (-24).toByte(),
            10, 84, (-41).toByte(), (-119).toByte(), 107, 53
        )
        
        /**
         * Calculate CRC8 for a byte array
         * Initial value is 85 (RESET_VALUE)
         */
        fun calculate(data: ByteArray): Byte {
            return calculate(data, 0, data.size)
        }
        
        /**
         * Calculate CRC8 for a portion of a byte array
         */
        fun calculate(data: ByteArray, offset: Int, count: Int): Byte {
            var crc = RESET_VALUE
            for (i in offset until (offset + count)) {
                crc = TABLE[(crc xor (data[i].toInt() and 0xFF)) and 0xFF].toInt() and 0xFF
            }
            return crc.toByte()
        }
        
        /**
         * Update CRC8 with a single byte (static version)
         */
        fun update(crc: Byte, byte: Byte): Byte {
            val current = crc.toInt() and 0xFF
            val updated = TABLE[(current xor (byte.toInt() and 0xFF)) and 0xFF]
            return updated
        }
    }
}
