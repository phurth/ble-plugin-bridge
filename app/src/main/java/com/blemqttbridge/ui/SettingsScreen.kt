package com.blemqttbridge.ui

import android.Manifest
import android.content.pm.PackageManager
import android.os.Build
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.KeyboardArrowUp
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.rotate
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import androidx.core.content.ContextCompat
import androidx.lifecycle.viewmodel.compose.viewModel
import com.blemqttbridge.ui.components.ExpandableSection
import com.blemqttbridge.ui.viewmodel.SettingsViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SettingsScreen(
    viewModel: SettingsViewModel = viewModel(),
    onRequestPermissions: () -> Unit
) {
    val scrollState = rememberScrollState()
    val context = LocalContext.current
    
    // Collect all state flows
    val mqttEnabled by viewModel.mqttEnabled.collectAsState()
    val mqttBrokerHost by viewModel.mqttBrokerHost.collectAsState()
    val mqttBrokerPort by viewModel.mqttBrokerPort.collectAsState()
    val mqttUsername by viewModel.mqttUsername.collectAsState()
    val mqttPassword by viewModel.mqttPassword.collectAsState()
    val mqttTopicPrefix by viewModel.mqttTopicPrefix.collectAsState()
    
    val serviceEnabled by viewModel.serviceEnabled.collectAsState()
    
    val oneControlEnabled by viewModel.oneControlEnabled.collectAsState()
    val oneControlGatewayMac by viewModel.oneControlGatewayMac.collectAsState()
    val oneControlGatewayPin by viewModel.oneControlGatewayPin.collectAsState()
    
    val mqttExpanded by viewModel.mqttExpanded.collectAsState()
    val oneControlExpanded by viewModel.oneControlExpanded.collectAsState()
    
    // Status flows
    val bleConnected by viewModel.bleConnectedStatus.collectAsState()
    val dataHealthy by viewModel.dataHealthyStatus.collectAsState()
    val devicePaired by viewModel.devicePairedStatus.collectAsState()
    val mqttConnected by viewModel.mqttConnectedStatus.collectAsState()
    
    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("BLE-MQTT Bridge", style = MaterialTheme.typography.titleSmall) },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = MaterialTheme.colorScheme.primaryContainer,
                    titleContentColor = MaterialTheme.colorScheme.onPrimaryContainer
                )
            )
        }
    ) { paddingValues ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues)
                .verticalScroll(scrollState)
                .padding(bottom = 4.dp)
        ) {
            // Bridge Service Section
            SectionHeader("Bridge Service")
            
            // Bridge Service Toggle
            ElevatedCard(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 6.dp, vertical = 2.dp)
            ) {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(6.dp),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Column {
                        Text(
                            text = "Service",
                            style = MaterialTheme.typography.bodyMedium
                        )
                        Text(
                            text = if (serviceEnabled) "Running" else "Stopped",
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                    Switch(
                        checked = serviceEnabled,
                        onCheckedChange = { viewModel.setServiceEnabled(it) }
                    )
                }
            }
            
            // Permissions Header (above the card)
            Text(
                text = "Permissions",
                style = MaterialTheme.typography.labelLarge,
                color = MaterialTheme.colorScheme.primary,
                modifier = Modifier.padding(horizontal = 6.dp, vertical = 4.dp)
            )
            
            // Permissions Card
            ElevatedCard(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 6.dp, vertical = 2.dp)
            ) {
                Column(
                    modifier = Modifier.padding(6.dp),
                    verticalArrangement = Arrangement.spacedBy(2.dp)
                ) {
                    // Location Permission
                    val hasLocation = ContextCompat.checkSelfPermission(
                        context, Manifest.permission.ACCESS_FINE_LOCATION
                    ) == PackageManager.PERMISSION_GRANTED
                    
                    PermissionSwitch(
                        title = "Location",
                        description = "Required for BLE scanning",
                        isGranted = hasLocation,
                        onToggle = onRequestPermissions
                    )
                    
                    HorizontalDivider(color = MaterialTheme.colorScheme.outlineVariant)
                    
                    // Bluetooth Permissions (Android 12+)
                    val hasBluetooth = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
                        ContextCompat.checkSelfPermission(
                            context, Manifest.permission.BLUETOOTH_SCAN
                        ) == PackageManager.PERMISSION_GRANTED &&
                        ContextCompat.checkSelfPermission(
                            context, Manifest.permission.BLUETOOTH_CONNECT
                        ) == PackageManager.PERMISSION_GRANTED
                    } else {
                        true // Not needed on older versions
                    }
                    
                    PermissionSwitch(
                        title = "Bluetooth",
                        description = "Connect to BLE devices",
                        isGranted = hasBluetooth,
                        onToggle = onRequestPermissions
                    )
                    
                    HorizontalDivider(color = MaterialTheme.colorScheme.outlineVariant)
                    
                    // Notification Permission (Android 13+)
                    val hasNotifications = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                        ContextCompat.checkSelfPermission(
                            context, Manifest.permission.POST_NOTIFICATIONS
                        ) == PackageManager.PERMISSION_GRANTED
                    } else {
                        true // Not needed on older versions
                    }
                    
                    PermissionSwitch(
                        title = "Notifications",
                        description = "Service status updates",
                        isGranted = hasNotifications,
                        onToggle = onRequestPermissions
                    )
                }
            }
            
            Spacer(modifier = Modifier.height(4.dp))
            
            // Output Section
            SectionHeader("Output")
            
            // MQTT Card (with toggle and expandable settings)
            ElevatedCard(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 6.dp, vertical = 2.dp)
            ) {
                Column {
                    Row(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(6.dp),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column {
                            Text(
                                text = "MQTT",
                                style = MaterialTheme.typography.bodyMedium
                            )
                            Text(
                                text = if (mqttEnabled) "Enabled" else "Disabled",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                        }
                        Switch(
                            checked = mqttEnabled,
                            onCheckedChange = { viewModel.setMqttEnabled(it) }
                        )
                    }
                    
                    // MQTT Settings (expandable, inside the card)
                    if (mqttEnabled) {
                        HorizontalDivider(color = MaterialTheme.colorScheme.outlineVariant)
                        
                        // MQTT Status Indicator
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(horizontal = 6.dp, vertical = 4.dp),
                            horizontalArrangement = Arrangement.Center
                        ) {
                            StatusIndicator(
                                label = "Connection",
                                isActive = mqttConnected
                            )
                        }
                        
                        HorizontalDivider(color = MaterialTheme.colorScheme.outlineVariant)
                        
                        ExpandableSection(
                            title = "Broker Settings",
                            expanded = mqttExpanded,
                            onToggle = { viewModel.toggleMqttExpanded() },
                            modifier = Modifier.padding(top = 2.dp)
                        ) {
                            OutlinedTextField(
                                value = mqttBrokerHost,
                                onValueChange = { viewModel.setMqttBrokerHost(it) },
                                label = { Text("Host", style = MaterialTheme.typography.bodySmall) },
                                textStyle = MaterialTheme.typography.bodyMedium,
                                modifier = Modifier.fillMaxWidth()
                            )
                            
                            OutlinedTextField(
                                value = mqttBrokerPort.toString(),
                                onValueChange = { 
                                    it.toIntOrNull()?.let { port -> viewModel.setMqttBrokerPort(port) }
                                },
                                label = { Text("Port", style = MaterialTheme.typography.bodySmall) },
                                textStyle = MaterialTheme.typography.bodyMedium,
                                keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                                modifier = Modifier.fillMaxWidth()
                            )
                            
                            OutlinedTextField(
                                value = mqttUsername,
                                onValueChange = { viewModel.setMqttUsername(it) },
                                label = { Text("Username", style = MaterialTheme.typography.bodySmall) },
                                textStyle = MaterialTheme.typography.bodyMedium,
                                modifier = Modifier.fillMaxWidth()
                            )
                            
                            OutlinedTextField(
                                value = mqttPassword,
                                onValueChange = { viewModel.setMqttPassword(it) },
                                label = { Text("Password", style = MaterialTheme.typography.bodySmall) },
                                textStyle = MaterialTheme.typography.bodyMedium,
                                visualTransformation = PasswordVisualTransformation(),
                                modifier = Modifier.fillMaxWidth()
                            )
                            
                            OutlinedTextField(
                                value = mqttTopicPrefix,
                                onValueChange = { viewModel.setMqttTopicPrefix(it) },
                                label = { Text("Topic Prefix", style = MaterialTheme.typography.bodySmall) },
                                textStyle = MaterialTheme.typography.bodyMedium,
                                modifier = Modifier.fillMaxWidth()
                            )
                        }
                    }
                }
            }
            
            Spacer(modifier = Modifier.height(4.dp))
            
            // Plugins Section
            SectionHeader("Plugins")
            
            // Add Plugin Button (disabled)
            OutlinedButton(
                onClick = { },
                enabled = false,
                modifier = Modifier
                    .fillMaxWidth()
                    .height(32.dp)
                    .padding(horizontal = 6.dp, vertical = 2.dp),
                contentPadding = PaddingValues(horizontal = 8.dp, vertical = 4.dp)
            ) {
                Icon(
                    imageVector = Icons.Filled.Add,
                    contentDescription = "Add Plugin",
                    modifier = Modifier.size(14.dp)
                )
                Spacer(modifier = Modifier.width(4.dp))
                Text("Add Plugin", style = MaterialTheme.typography.bodySmall)
            }
            
            // OneControl Plugin
            ElevatedCard(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 6.dp, vertical = 2.dp)
            ) {
                Column {
                    Row(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(6.dp),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column {
                            Text(
                                text = "OneControl",
                                style = MaterialTheme.typography.bodyMedium
                            )
                            Text(
                                text = if (oneControlEnabled) "Enabled" else "Disabled",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                        }
                        Switch(
                            checked = oneControlEnabled,
                            onCheckedChange = { viewModel.setOneControlEnabled(it) }
                        )
                    }
                    
                    if (oneControlEnabled) {
                        HorizontalDivider(color = MaterialTheme.colorScheme.outlineVariant)
                        
                        // Status Indicators (visible when enabled, outside expandable section)
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(horizontal = 6.dp, vertical = 4.dp),
                            horizontalArrangement = Arrangement.SpaceEvenly
                        ) {
                            StatusIndicator(
                                label = "BLE",
                                isActive = bleConnected
                            )
                            StatusIndicator(
                                label = "Data",
                                isActive = dataHealthy
                            )
                            StatusIndicator(
                                label = "Paired",
                                isActive = devicePaired
                            )
                        }
                        
                        HorizontalDivider(color = MaterialTheme.colorScheme.outlineVariant)
                        
                        ExpandableSection(
                            title = "Gateway Settings",
                            expanded = oneControlExpanded,
                            onToggle = { viewModel.toggleOneControlExpanded() },
                            modifier = Modifier.padding(top = 2.dp)
                        ) {
                            OutlinedTextField(
                                value = oneControlGatewayMac,
                                onValueChange = { viewModel.setOneControlGatewayMac(it) },
                                label = { Text("MAC Address", style = MaterialTheme.typography.bodySmall) },
                                textStyle = MaterialTheme.typography.bodyMedium,
                                modifier = Modifier.fillMaxWidth()
                            )
                            
                            OutlinedTextField(
                                value = oneControlGatewayPin,
                                onValueChange = { viewModel.setOneControlGatewayPin(it) },
                                label = { Text("PIN", style = MaterialTheme.typography.bodySmall) },
                                textStyle = MaterialTheme.typography.bodyMedium,
                                keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                                modifier = Modifier.fillMaxWidth()
                            )
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun SectionHeader(text: String) {
    Text(
        text = text,
        style = MaterialTheme.typography.labelLarge,
        color = MaterialTheme.colorScheme.primary,
        modifier = Modifier.padding(horizontal = 6.dp, vertical = 4.dp)
    )
}

@Composable
private fun PermissionSwitch(
    title: String,
    description: String,
    isGranted: Boolean,
    onToggle: () -> Unit
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 2.dp),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Column(modifier = Modifier.weight(1f)) {
            Text(
                text = title,
                style = MaterialTheme.typography.bodyMedium
            )
            Text(
                text = description,
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
        Switch(
            checked = isGranted,
            onCheckedChange = { if (!isGranted) onToggle() }
        )
    }
}

@Composable
private fun StatusIndicator(
    label: String,
    isActive: Boolean
) {
    val activeColor = androidx.compose.ui.graphics.Color(0xFF4CAF50) // Green
    val inactiveColor = androidx.compose.ui.graphics.Color(0xFFF44336) // Red
    Row(
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(4.dp)
    ) {
        androidx.compose.foundation.Canvas(
            modifier = Modifier.size(8.dp)
        ) {
            drawCircle(
                color = if (isActive) activeColor else inactiveColor
            )
        }
        Text(
            text = label,
            style = MaterialTheme.typography.bodySmall,
            color = if (isActive) activeColor else inactiveColor
        )
    }
}
