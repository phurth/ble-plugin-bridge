package com.blemqttbridge.util

import com.google.common.truth.Truth.assertThat
import org.junit.Test

class ConfigValidatorTest {

    @Test
    fun validateBrokerHost_acceptsValidHostnamesAndIps() {
        val ok1 = ConfigValidator.validateBrokerHost("mqtt.example.com")
        val ok2 = ConfigValidator.validateBrokerHost("192.168.1.10")
        val ok3 = ConfigValidator.validateBrokerHost("localhost")

        assertThat(ok1.first).isTrue()
        assertThat(ok1.second).isNull()
        assertThat(ok2.first).isTrue()
        assertThat(ok2.second).isNull()
        assertThat(ok3.first).isTrue()
        assertThat(ok3.second).isNull()
    }

    @Test
    fun validateBrokerHost_rejectsInvalidValues() {
        val empty = ConfigValidator.validateBrokerHost("")
        val bad = ConfigValidator.validateBrokerHost("not a host!!")

        assertThat(empty.first).isFalse()
        assertThat(empty.second).isNotEmpty()
        assertThat(bad.first).isFalse()
        assertThat(bad.second).isNotEmpty()
    }

    @Test
    fun validatePort_checksBoundsAndNumbers() {
        assertThat(ConfigValidator.validatePort("1883").first).isTrue()
        assertThat(ConfigValidator.validatePort("0").first).isFalse()
        assertThat(ConfigValidator.validatePort("65536").first).isFalse()
        assertThat(ConfigValidator.validatePort("abc").first).isFalse()
    }

    @Test
    fun validateTopicPrefix_rejectsWildcardsAndSlashes() {
        assertThat(ConfigValidator.validateTopicPrefix("home/ble").first).isTrue()
        assertThat(ConfigValidator.validateTopicPrefix("/leading").first).isFalse()
        assertThat(ConfigValidator.validateTopicPrefix("trailing/").first).isFalse()
        assertThat(ConfigValidator.validateTopicPrefix("bad+#").first).isFalse()
    }

    @Test
    fun validateMacAddress_formatValidation() {
        assertThat(ConfigValidator.validateMacAddress("AA:BB:CC:DD:EE:FF").first).isTrue()
        assertThat(ConfigValidator.validateMacAddress("AA-BB-CC-DD-EE-FF").first).isTrue()
        assertThat(ConfigValidator.validateMacAddress("AABBCCDDEEFF").first).isFalse()
    }

    @Test
    fun validateCredentials_lengthLimits() {
        assertThat(ConfigValidator.validateUsername("user").first).isTrue()
        assertThat(ConfigValidator.validatePassword("pass").first).isTrue()
        val longUser = "u".repeat(129)
        val longPass = "p".repeat(129)
        assertThat(ConfigValidator.validateUsername(longUser).first).isFalse()
        assertThat(ConfigValidator.validatePassword(longPass).first).isFalse()
    }
}
