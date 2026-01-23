package com.blemqttbridge.util

/**
 * Configuration validation utilities for MQTT and plugin settings.
 * Provides validation for broker URLs, topic names, and numeric ranges.
 */
object ConfigValidator {
    
    /**
     * Validate MQTT broker URL/hostname
     * Accepts IP addresses and domain names
     * 
     * @param broker Broker hostname or IP address
     * @return Pair of (isValid, errorMessage)
     */
    fun validateBrokerHost(broker: String): Pair<Boolean, String?> {
        return when {
            broker.isBlank() -> Pair(false, "Broker host cannot be empty")
            broker.length > 255 -> Pair(false, "Broker host is too long (max 255 characters)")
            !isValidHostname(broker) && !isValidIpAddress(broker) -> Pair(false, "Invalid broker hostname or IP address format")
            else -> Pair(true, null)
        }
    }
    
    /**
     * Validate MQTT port number
     * 
     * @param portStr Port number as string
     * @return Pair of (isValid, errorMessage)
     */
    fun validatePort(portStr: String): Pair<Boolean, String?> {
        return when {
            portStr.isBlank() -> Pair(false, "Port cannot be empty")
            else -> {
                val port = portStr.toIntOrNull()
                when {
                    port == null -> Pair(false, "Port must be a number")
                    port < 1 || port > 65535 -> Pair(false, "Port must be between 1 and 65535")
                    else -> Pair(true, null)
                }
            }
        }
    }
    
    /**
     * Validate MQTT topic prefix
     * Topic names must follow MQTT spec: no wildcards, valid UTF-8 characters
     * 
     * @param topic Topic prefix
     * @return Pair of (isValid, errorMessage)
     */
    fun validateTopicPrefix(topic: String): Pair<Boolean, String?> {
        return when {
            topic.isBlank() -> Pair(false, "Topic prefix cannot be empty")
            topic.length > 256 -> Pair(false, "Topic prefix is too long (max 256 characters)")
            topic.contains("+") || topic.contains("#") -> Pair(false, "Topic prefix cannot contain wildcards (+ or #)")
            topic.startsWith("/") || topic.endsWith("/") -> Pair(false, "Topic prefix cannot start or end with /")
            !topic.matches(Regex("[a-zA-Z0-9/_-]+")) -> Pair(false, "Topic prefix contains invalid characters")
            else -> Pair(true, null)
        }
    }
    
    /**
     * Validate Bluetooth MAC address format
     * 
     * @param mac MAC address string
     * @return Pair of (isValid, errorMessage)
     */
    fun validateMacAddress(mac: String): Pair<Boolean, String?> {
        return when {
            mac.isBlank() -> Pair(false, "MAC address cannot be empty")
            !mac.matches(Regex("^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$")) -> 
                Pair(false, "Invalid MAC address format. Expected: XX:XX:XX:XX:XX:XX or XX-XX-XX-XX-XX-XX")
            else -> Pair(true, null)
        }
    }
    
    /**
     * Validate numeric pin value
     * 
     * @param pin PIN as string
     * @return Pair of (isValid, errorMessage)
     */
    fun validatePin(pin: String): Pair<Boolean, String?> {
        return when {
            pin.isBlank() -> Pair(false, "PIN cannot be empty")
            !pin.matches(Regex("^[0-9]{1,10}$")) -> Pair(false, "PIN must be a numeric value")
            else -> Pair(true, null)
        }
    }
    
    /**
     * Validate MQTT username
     * 
     * @param username Username string
     * @return Pair of (isValid, errorMessage)
     */
    fun validateUsername(username: String): Pair<Boolean, String?> {
        return when {
            username.length > 128 -> Pair(false, "Username is too long (max 128 characters)")
            else -> Pair(true, null)
        }
    }
    
    /**
     * Validate MQTT password
     * 
     * @param password Password string
     * @return Pair of (isValid, errorMessage)
     */
    fun validatePassword(password: String): Pair<Boolean, String?> {
        return when {
            password.length > 128 -> Pair(false, "Password is too long (max 128 characters)")
            else -> Pair(true, null)
        }
    }
    
    /**
     * Check if string is a valid hostname
     * Allows domain names and localhost
     */
    private fun isValidHostname(hostname: String): Boolean {
        // Allow localhost
        if (hostname.equals("localhost", ignoreCase = true)) return true
        
        // Regex for valid domain names and subdomains
        val hostnameRegex = Regex("^([a-zA-Z0-9]([a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\\.)*[a-zA-Z0-9]([a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?$")
        return hostname.matches(hostnameRegex)
    }
    
    /**
     * Check if string is a valid IPv4 or IPv6 address
     */
    private fun isValidIpAddress(ip: String): Boolean {
        // Simple IPv4 validation
        val ipv4Regex = Regex("^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$")
        if (ip.matches(ipv4Regex)) return true
        
        // Simple IPv6 validation (allow :: notation)
        val ipv6Regex = Regex("^([0-9a-fA-F]{0,4}:){1,7}[0-9a-fA-F]{0,4}$|^::1$|^::$")
        return ip.matches(ipv6Regex)
    }
}
