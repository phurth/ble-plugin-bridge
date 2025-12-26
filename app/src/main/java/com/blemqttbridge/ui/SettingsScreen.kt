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
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.KeyboardArrowUp
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
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
    
    val easyTouchEnabled by viewModel.easyTouchEnabled.collectAsState()
    val easyTouchThermostatMac by viewModel.easyTouchThermostatMac.collectAsState()
    val easyTouchThermostatPassword by viewModel.easyTouchThermostatPassword.collectAsState()
    
    val goPowerEnabled by viewModel.goPowerEnabled.collectAsState()
    val goPowerControllerMac by viewModel.goPowerControllerMac.collectAsState()
    
    val bleScannerEnabled by viewModel.bleScannerEnabled.collectAsState()
    
    val mqttExpanded by viewModel.mqttExpanded.collectAsState()
    val oneControlExpanded by viewModel.oneControlExpanded.collectAsState()
    val easyTouchExpanded by viewModel.easyTouchExpanded.collectAsState()
    val goPowerExpanded by viewModel.goPowerExpanded.collectAsState()
    val bleScannerExpanded by viewModel.bleScannerExpanded.collectAsState()
    val showPluginPicker by viewModel.showPluginPicker.collectAsState()
    
    // Status flows
    val bleConnected by viewModel.bleConnectedStatus.collectAsState()
    val dataHealthy by viewModel.dataHealthyStatus.collectAsState()
    val devicePaired by viewModel.devicePairedStatus.collectAsState()
    val mqttConnected by viewModel.mqttConnectedStatus.collectAsState()
    
    // Confirmation dialog state
    var showRemoveConfirmation by remember { mutableStateOf(false) }
    var pluginToRemove by remember { mutableStateOf<String?>(null) }
    
    // Get app version
    val appVersion = remember {
        try {
            context.packageManager.getPackageInfo(context.packageName, 0).versionName ?: "?"
        } catch (e: Exception) {
            "?"
        }
    }
    
    Scaffold(
        topBar = {
            TopAppBar(
                title = { 
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Text("BLE-MQTT Bridge", style = MaterialTheme.typography.titleSmall)
                        Text("v$appVersion", style = MaterialTheme.typography.labelSmall)
                    }
                },
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
                            text = "BLE Service",
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
                    
                    // MQTT Settings (always visible, locked when enabled)
                    HorizontalDivider(color = MaterialTheme.colorScheme.outlineVariant)
                    
                    // MQTT Status Indicator (only show when enabled)
                    if (mqttEnabled) {
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
                    }
                    
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
                            modifier = Modifier.fillMaxWidth(),
                            enabled = !mqttEnabled
                        )
                        
                        OutlinedTextField(
                            value = mqttBrokerPort.toString(),
                            onValueChange = { 
                                it.toIntOrNull()?.let { port -> viewModel.setMqttBrokerPort(port) }
                            },
                            label = { Text("Port", style = MaterialTheme.typography.bodySmall) },
                            textStyle = MaterialTheme.typography.bodyMedium,
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                            modifier = Modifier.fillMaxWidth(),
                            enabled = !mqttEnabled
                        )
                        
                        OutlinedTextField(
                            value = mqttUsername,
                            onValueChange = { viewModel.setMqttUsername(it) },
                            label = { Text("Username", style = MaterialTheme.typography.bodySmall) },
                            textStyle = MaterialTheme.typography.bodyMedium,
                            modifier = Modifier.fillMaxWidth(),
                            enabled = !mqttEnabled
                        )
                        
                        OutlinedTextField(
                            value = mqttPassword,
                            onValueChange = { viewModel.setMqttPassword(it) },
                            label = { Text("Password", style = MaterialTheme.typography.bodySmall) },
                            textStyle = MaterialTheme.typography.bodyMedium,
                            visualTransformation = PasswordVisualTransformation(),
                            modifier = Modifier.fillMaxWidth(),
                            enabled = !mqttEnabled
                        )
                        
                        OutlinedTextField(
                            value = mqttTopicPrefix,
                            onValueChange = { viewModel.setMqttTopicPrefix(it) },
                            label = { Text("Topic Prefix", style = MaterialTheme.typography.bodySmall) },
                            textStyle = MaterialTheme.typography.bodyMedium,
                            modifier = Modifier.fillMaxWidth(),
                            enabled = !mqttEnabled
                        )
                    }
                }
            }
            
            Spacer(modifier = Modifier.height(4.dp))
            
            // Plugins Section
            SectionHeader("Plugins")
            
            // Add Plugin Button
            OutlinedButton(
                onClick = { viewModel.showPluginPicker() },
                enabled = true,
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
            
            // Plugin Picker Dialog
            if (showPluginPicker) {
                PluginPickerDialog(
                    onDismiss = { viewModel.hidePluginPicker() },
                    onPluginSelected = { viewModel.addPlugin(it) },
                    enabledPlugins = listOf(
                        // OneControl is always enabled, not in picker
                        if (easyTouchEnabled) "easytouch" else null,
                        if (goPowerEnabled) "gopower" else null,
                        if (bleScannerEnabled) "ble_scanner" else null
                    ).filterNotNull()
                )
            }
            
            // Remove Plugin Confirmation Dialog
            if (showRemoveConfirmation && pluginToRemove != null) {
                AlertDialog(
                    onDismissRequest = { 
                        showRemoveConfirmation = false
                        pluginToRemove = null
                    },
                    title = { Text("Remove Plugin") },
                    text = { Text("Are you sure you want to remove this plugin?") },
                    confirmButton = {
                        TextButton(
                            onClick = {
                                pluginToRemove?.let { viewModel.removePlugin(it) }
                                showRemoveConfirmation = false
                                pluginToRemove = null
                            }
                        ) {
                            Text("Remove", color = MaterialTheme.colorScheme.error)
                        }
                    },
                    dismissButton = {
                        TextButton(
                            onClick = {
                                showRemoveConfirmation = false
                                pluginToRemove = null
                            }
                        ) {
                            Text("Cancel")
                        }
                    }
                )
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
                                text = "LCI/Lippert RV Gateway",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                        }
                    }
                    
                    HorizontalDivider(color = MaterialTheme.colorScheme.outlineVariant)
                    
                    // Status Indicators (always visible for OneControl)
                    Row(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(horizontal = 6.dp, vertical = 4.dp),
                        horizontalArrangement = Arrangement.SpaceEvenly
                    ) {
                        StatusIndicator(
                            label = "Connected",
                            isActive = bleConnected
                        )
                        StatusIndicator(
                            label = "Data",
                            isActive = dataHealthy
                        )
                        StatusIndicator(
                            label = "Authenticated",
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
                            modifier = Modifier.fillMaxWidth(),
                            enabled = true
                        )
                        
                        OutlinedTextField(
                            value = oneControlGatewayPin,
                            onValueChange = { viewModel.setOneControlGatewayPin(it) },
                            label = { Text("PIN", style = MaterialTheme.typography.bodySmall) },
                            textStyle = MaterialTheme.typography.bodyMedium,
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                            modifier = Modifier.fillMaxWidth(),
                            enabled = true
                        )
                    }
                }
            }
            
            // EasyTouch Plugin (only show if enabled)
            if (easyTouchEnabled) {
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
                            Column(modifier = Modifier.weight(1f)) {
                                Text(
                                    text = "EasyTouch",
                                    style = MaterialTheme.typography.bodyMedium
                                )
                                Text(
                                    text = "Micro-Air RV thermostat",
                                    style = MaterialTheme.typography.bodySmall,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant
                                )
                            }
                            Row(verticalAlignment = Alignment.CenterVertically) {
                                IconButton(
                                    onClick = { 
                                        pluginToRemove = "easytouch"
                                        showRemoveConfirmation = true
                                    },
                                    modifier = Modifier.size(32.dp)
                                ) {
                                    Icon(
                                        imageVector = Icons.Default.Close,
                                        contentDescription = "Remove plugin",
                                        tint = MaterialTheme.colorScheme.error,
                                        modifier = Modifier.size(20.dp)
                                    )
                                }
                            }
                        }
                        
                        HorizontalDivider(color = MaterialTheme.colorScheme.outlineVariant)
                        
                        // Health Status Indicators
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(horizontal = 6.dp, vertical = 4.dp),
                            horizontalArrangement = Arrangement.SpaceEvenly
                        ) {
                            StatusIndicator(
                                label = "Connected",
                                isActive = bleConnected
                            )
                            StatusIndicator(
                                label = "Data",
                                isActive = dataHealthy
                            )
                            StatusIndicator(
                                label = "Authenticated",
                                isActive = devicePaired
                            )
                        }
                        
                        HorizontalDivider(color = MaterialTheme.colorScheme.outlineVariant)
                        
                        // Thermostat Settings
                        ExpandableSection(
                            title = "Thermostat Settings",
                            expanded = easyTouchExpanded,
                            onToggle = { viewModel.toggleEasyTouchExpanded() },
                            modifier = Modifier.padding(top = 2.dp)
                        ) {
                            OutlinedTextField(
                                value = easyTouchThermostatMac,
                                onValueChange = { viewModel.setEasyTouchThermostatMac(it) },
                                label = { Text("MAC Address", style = MaterialTheme.typography.bodySmall) },
                                textStyle = MaterialTheme.typography.bodyMedium,
                                placeholder = { Text("AA:BB:CC:DD:EE:FF", style = MaterialTheme.typography.bodySmall) },
                                modifier = Modifier.fillMaxWidth(),
                                enabled = true
                            )
                            
                            OutlinedTextField(
                                value = easyTouchThermostatPassword,
                                onValueChange = { viewModel.setEasyTouchThermostatPassword(it) },
                                label = { Text("Password", style = MaterialTheme.typography.bodySmall) },
                                textStyle = MaterialTheme.typography.bodyMedium,
                                visualTransformation = PasswordVisualTransformation(),
                                modifier = Modifier.fillMaxWidth(),
                                enabled = true
                            )
                            
                            Text(
                                text = "Enter the thermostat MAC address and password from the EasyTouch app.",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant,
                                modifier = Modifier.padding(vertical = 4.dp)
                            )
                        }
                    }
                }
            }
            
            // GoPower Plugin (only show if enabled)
            if (goPowerEnabled) {
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
                            Column(modifier = Modifier.weight(1f)) {
                                Text(
                                    text = "GoPower Solar",
                                    style = MaterialTheme.typography.bodyMedium
                                )
                                Text(
                                    text = "PWM Solar Charge Controller",
                                    style = MaterialTheme.typography.bodySmall,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant
                                )
                            }
                            Row(verticalAlignment = Alignment.CenterVertically) {
                                IconButton(
                                    onClick = { 
                                        pluginToRemove = "gopower"
                                        showRemoveConfirmation = true
                                    },
                                    modifier = Modifier.size(32.dp)
                                ) {
                                    Icon(
                                        imageVector = Icons.Default.Close,
                                        contentDescription = "Remove plugin",
                                        tint = MaterialTheme.colorScheme.error,
                                        modifier = Modifier.size(20.dp)
                                    )
                                }
                            }
                        }
                        
                        HorizontalDivider(color = MaterialTheme.colorScheme.outlineVariant)
                        
                        // Health Status Indicators (only show when MAC is configured)
                        // Note: Status only reflects GoPower when it's the active plugin
                        if (goPowerControllerMac.isNotBlank()) {
                            Row(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(horizontal = 6.dp, vertical = 4.dp),
                                horizontalArrangement = Arrangement.SpaceEvenly
                            ) {
                                StatusIndicator(
                                    label = "Connected",
                                    isActive = bleConnected
                                )
                                StatusIndicator(
                                    label = "Data",
                                    isActive = dataHealthy
                                )
                            }
                            
                            HorizontalDivider(color = MaterialTheme.colorScheme.outlineVariant)
                        }
                        
                        // Controller Settings
                        ExpandableSection(
                            title = "Controller Settings",
                            expanded = goPowerExpanded,
                            onToggle = { viewModel.toggleGoPowerExpanded() },
                            modifier = Modifier.padding(top = 2.dp)
                        ) {
                            OutlinedTextField(
                                value = goPowerControllerMac,
                                onValueChange = { viewModel.setGoPowerControllerMac(it) },
                                label = { Text("MAC Address", style = MaterialTheme.typography.bodySmall) },
                                textStyle = MaterialTheme.typography.bodyMedium,
                                placeholder = { Text("AA:BB:CC:DD:EE:FF", style = MaterialTheme.typography.bodySmall) },
                                modifier = Modifier.fillMaxWidth(),
                                enabled = true
                            )
                            
                            Text(
                                text = "Enter the solar controller MAC address. No password required.",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant,
                                modifier = Modifier.padding(vertical = 4.dp)
                            )
                        }
                    }
                }
            }
            
            // BLE Scanner Plugin (only show if enabled)
            if (bleScannerEnabled) {
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
                            Column(modifier = Modifier.weight(1f)) {
                                Text(
                                    text = "BLE Scanner",
                                    style = MaterialTheme.typography.bodyMedium
                                )
                                Text(
                                    text = "Scan for nearby BLE devices",
                                    style = MaterialTheme.typography.bodySmall,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant
                                )
                            }
                            Row(verticalAlignment = Alignment.CenterVertically) {
                                IconButton(
                                    onClick = { 
                                        pluginToRemove = "ble_scanner"
                                        showRemoveConfirmation = true
                                    },
                                    modifier = Modifier.size(32.dp)
                                ) {
                                    Icon(
                                        imageVector = Icons.Default.Close,
                                        contentDescription = "Remove plugin",
                                        tint = MaterialTheme.colorScheme.error,
                                        modifier = Modifier.size(20.dp)
                                    )
                                }
                            }
                        }
                        
                        HorizontalDivider(color = MaterialTheme.colorScheme.outlineVariant)
                        
                        // Info section
                        ExpandableSection(
                            title = "Info",
                            expanded = bleScannerExpanded,
                            onToggle = { viewModel.toggleBleScannerExpanded() },
                            modifier = Modifier.padding(top = 2.dp)
                        ) {
                            Text(
                                text = "Press the 'Start Scan' button in Home Assistant to scan for 60 seconds. Results will appear in the 'Devices Found' sensor attributes.",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant,
                                modifier = Modifier.padding(vertical = 4.dp)
                            )
                        }
                    }
                }
            }
            
            Spacer(modifier = Modifier.height(4.dp))
            
            // Diagnostics Section
            SectionHeader("Diagnostics")
            
            // Debug Log Export
            ElevatedCard(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 6.dp, vertical = 2.dp)
            ) {
                OutlinedButton(
                    onClick = { viewModel.exportDebugLog() },
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(6.dp),
                    contentPadding = PaddingValues(horizontal = 8.dp, vertical = 8.dp)
                ) {
                    Text("Export Debug Log", style = MaterialTheme.typography.bodyMedium)
                }
            }
            
            // BLE Trace Toggle
            val traceActive by viewModel.traceActive.collectAsState()
            val traceFilePath by viewModel.traceFilePath.collectAsState()
            
            ElevatedCard(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 6.dp, vertical = 2.dp)
            ) {
                Column(modifier = Modifier.padding(6.dp)) {
                    OutlinedButton(
                        onClick = { viewModel.toggleBleTrace() },
                        modifier = Modifier.fillMaxWidth(),
                        contentPadding = PaddingValues(horizontal = 8.dp, vertical = 8.dp)
                    ) {
                        Text(
                            text = if (traceActive) "Stop Trace & Send" else "Start BLE Trace",
                            style = MaterialTheme.typography.bodyMedium
                        )
                    }
                    
                    // Trace status indicator
                    Spacer(modifier = Modifier.height(4.dp))
                    Text(
                        text = when {
                            traceActive -> "Trace: active"
                            traceFilePath != null -> "Trace: saved to $traceFilePath"
                            else -> "Trace: inactive"
                        },
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        modifier = Modifier.padding(horizontal = 4.dp)
                    )
                }
            }
        }
    }
}

@Composable
private fun PluginPickerDialog(
    onDismiss: () -> Unit,
    onPluginSelected: (String) -> Unit,
    enabledPlugins: List<String>
) {
    // Available optional plugins (OneControl is always present, not shown here)
    val availablePlugins = listOf(
        PluginInfo("easytouch", "EasyTouch", "Micro-Air RV thermostat"),
        PluginInfo("gopower", "GoPower Solar", "PWM Solar Charge Controller"),
        PluginInfo("ble_scanner", "BLE Scanner", "Scan for nearby BLE devices")
    )
    
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Add Plugin") },
        text = {
            Column {
                availablePlugins.forEach { plugin ->
                    val isEnabled = enabledPlugins.contains(plugin.id)
                    Row(
                        modifier = Modifier
                            .fillMaxWidth()
                            .clickable(enabled = !isEnabled) { 
                                if (!isEnabled) onPluginSelected(plugin.id) 
                            }
                            .padding(vertical = 8.dp),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column(modifier = Modifier.weight(1f)) {
                            Text(
                                text = plugin.name,
                                style = MaterialTheme.typography.bodyMedium,
                                color = if (isEnabled) MaterialTheme.colorScheme.outline else MaterialTheme.colorScheme.onSurface
                            )
                            Text(
                                text = if (isEnabled) "Already added" else plugin.description,
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                        }
                    }
                    if (plugin != availablePlugins.last()) {
                        HorizontalDivider()
                    }
                }
            }
        },
        confirmButton = {
            TextButton(onClick = onDismiss) {
                Text("Cancel")
            }
        }
    )
}

private data class PluginInfo(
    val id: String,
    val name: String,
    val description: String
)

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
