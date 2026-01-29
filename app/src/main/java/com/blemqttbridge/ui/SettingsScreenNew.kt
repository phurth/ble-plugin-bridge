package com.blemqttbridge.ui

import android.Manifest
import android.content.Intent
import android.content.pm.PackageManager
import android.net.Uri
import android.os.Build
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.OpenInBrowser
import androidx.compose.material.icons.filled.OpenInNew
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalLifecycleOwner
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import androidx.core.content.ContextCompat
import androidx.lifecycle.viewmodel.compose.viewModel
import com.blemqttbridge.ui.viewmodel.SettingsViewModel
import com.blemqttbridge.utils.BatteryOptimizationHelper
import kotlinx.coroutines.launch
import androidx.activity.result.contract.ActivityResultContracts
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleEventObserver

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SettingsScreenNew(
    viewModel: SettingsViewModel = viewModel()
) {
    val scrollState = rememberScrollState()
    val context = LocalContext.current
    val lifecycleOwner = LocalLifecycleOwner.current
    val scope = rememberCoroutineScope()

    // Permissions state
    var hasLocationPermission by remember {
        mutableStateOf(
            ContextCompat.checkSelfPermission(
                context,
                Manifest.permission.ACCESS_FINE_LOCATION
            ) == PackageManager.PERMISSION_GRANTED
        )
    }

    var hasBluetoothPermission by remember {
        mutableStateOf(
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
                ContextCompat.checkSelfPermission(
                    context,
                    Manifest.permission.BLUETOOTH_SCAN
                ) == PackageManager.PERMISSION_GRANTED
            } else {
                true
            }
        )
    }

    var hasNotificationPermission by remember {
        mutableStateOf(
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                ContextCompat.checkSelfPermission(
                    context,
                    Manifest.permission.POST_NOTIFICATIONS
                ) == PackageManager.PERMISSION_GRANTED
            } else {
                true
            }
        )
    }

    var isIgnoringBatteryOpt by remember {
        mutableStateOf(BatteryOptimizationHelper.isIgnoringBatteryOptimizations(context))
    }

    fun refreshPermissionStates() {
        hasLocationPermission = ContextCompat.checkSelfPermission(
            context,
            Manifest.permission.ACCESS_FINE_LOCATION
        ) == PackageManager.PERMISSION_GRANTED

        hasBluetoothPermission = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            ContextCompat.checkSelfPermission(
                context,
                Manifest.permission.BLUETOOTH_SCAN
            ) == PackageManager.PERMISSION_GRANTED
        } else {
            true
        }

        hasNotificationPermission = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            ContextCompat.checkSelfPermission(
                context,
                Manifest.permission.POST_NOTIFICATIONS
            ) == PackageManager.PERMISSION_GRANTED
        } else {
            true
        }
    }

    val permissionLauncher = rememberLauncherForActivityResult(
        ActivityResultContracts.RequestMultiplePermissions()
    ) {
        refreshPermissionStates()
    }

    fun requestAllPermissions() {
        val permissionsToRequest = mutableListOf<String>()

        if (ContextCompat.checkSelfPermission(
                context,
                Manifest.permission.ACCESS_FINE_LOCATION
            ) != PackageManager.PERMISSION_GRANTED
        ) {
            permissionsToRequest.add(Manifest.permission.ACCESS_FINE_LOCATION)
        }

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            if (ContextCompat.checkSelfPermission(
                    context,
                    Manifest.permission.BLUETOOTH_SCAN
                ) != PackageManager.PERMISSION_GRANTED
            ) {
                permissionsToRequest.add(Manifest.permission.BLUETOOTH_SCAN)
            }
            if (ContextCompat.checkSelfPermission(
                    context,
                    Manifest.permission.BLUETOOTH_CONNECT
                ) != PackageManager.PERMISSION_GRANTED
            ) {
                permissionsToRequest.add(Manifest.permission.BLUETOOTH_CONNECT)
            }
        }

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            if (ContextCompat.checkSelfPermission(
                    context,
                    Manifest.permission.POST_NOTIFICATIONS
                ) != PackageManager.PERMISSION_GRANTED
            ) {
                permissionsToRequest.add(Manifest.permission.POST_NOTIFICATIONS)
            }
        }

        if (permissionsToRequest.isNotEmpty()) {
            permissionLauncher.launch(permissionsToRequest.toTypedArray())
        }
    }

    // Refresh permission states when screen becomes visible
    DisposableEffect(lifecycleOwner) {
        val observer = LifecycleEventObserver { _, event ->
            if (event == Lifecycle.Event.ON_RESUME) {
                refreshPermissionStates()
                isIgnoringBatteryOpt = BatteryOptimizationHelper.isIgnoringBatteryOptimizations(context)
            }
        }
        lifecycleOwner.lifecycle.addObserver(observer)
        onDispose {
            lifecycleOwner.lifecycle.removeObserver(observer)
        }
    }
    
    // Collect state flows
    val mqttEnabled by viewModel.mqttEnabled.collectAsState()
    val serviceRunning by viewModel.serviceRunningStatus.collectAsState()
    val webServerPort by viewModel.webServerPort.collectAsState()
    val webAuthEnabled by viewModel.webAuthEnabled.collectAsState()
    val webAuthUsername by viewModel.webAuthUsername.collectAsState()
    val webAuthPassword by viewModel.webAuthPassword.collectAsState()
    
    // MQTT status
    val mqttConnected by viewModel.mqttConnectedStatus.collectAsState()
    val mqttBrokerHost by viewModel.mqttBrokerHost.collectAsState()
    val mqttBrokerPort by viewModel.mqttBrokerPort.collectAsState()
    
    // BLE scanning and Bluetooth availability status
    val bleScanningActive by viewModel.bleScanningActive.collectAsState()
    val bluetoothAvailable by viewModel.bluetoothAvailable.collectAsState()
    
    // Polling service status
    val pollingRunning by viewModel.pollingRunningStatus.collectAsState()
    
    // Local state for text fields
    var portText by remember { mutableStateOf(webServerPort.toString()) }
    var portEditMode by remember { mutableStateOf(false) }
    var authUsernameText by remember { mutableStateOf(webAuthUsername) }
    var authPasswordText by remember { mutableStateOf(webAuthPassword) }
    var showAuthPasswordHashDialog by remember { mutableStateOf(false) }
    
    // Sync port field when flow changes
    LaunchedEffect(webServerPort) { portText = webServerPort.toString() }
    LaunchedEffect(webAuthUsername) { authUsernameText = webAuthUsername }
    LaunchedEffect(webAuthPassword) { authPasswordText = webAuthPassword }
    
    // Get app version
    val appVersion = remember {
        try {
            context.packageManager.getPackageInfo(context.packageName, 0).versionName ?: "?"
        } catch (e: Exception) {
            "?"
        }
    }
    
    // Get device IP for web interface URL
    val webInterfaceUrl by remember {
        derivedStateOf {
            val ip = try {
                java.net.NetworkInterface.getNetworkInterfaces().toList()
                    .flatMap { it.inetAddresses.toList() }
                    .firstOrNull { !it.isLoopbackAddress && it is java.net.Inet4Address }
                    ?.hostAddress
            } catch (e: Exception) {
                null
            }
            if (ip != null) {
                "http://$ip:$webServerPort"
            } else {
                "http://[device-ip]:$webServerPort"
            }
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
                .padding(bottom = 16.dp)
        ) {
            //============================================
            // WEB INTERFACE SECTION (TOP)
            //============================================
            SectionHeader("Web Interface")
            
            ElevatedCard(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp, vertical = 8.dp)
            ) {
                Column(modifier = Modifier.padding(16.dp)) {
                    // Web URL and Actions
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column(modifier = Modifier.weight(1f)) {
                            Text(
                                text = "Configure the bridge by visiting this web page",
                                style = MaterialTheme.typography.bodyMedium
                            )
                            Text(
                                text = webInterfaceUrl,
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.primary,
                                modifier = Modifier.clickable {
                                    val intent = Intent(Intent.ACTION_VIEW, Uri.parse(webInterfaceUrl))
                                    context.startActivity(intent)
                                }
                            )
                        }
                        IconButton(onClick = {
                            val intent = Intent(Intent.ACTION_VIEW, Uri.parse(webInterfaceUrl))
                            context.startActivity(intent)
                        }) {
                            Icon(
                                imageVector = Icons.Filled.OpenInNew,
                                contentDescription = "Open in browser"
                            )
                        }
                    }
                    
                    Spacer(modifier = Modifier.height(16.dp))
                    
                    // Port Configuration
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(8.dp),
                        verticalAlignment = Alignment.Top
                    ) {
                        OutlinedTextField(
                            value = portText,
                            onValueChange = { portText = it },
                            label = { Text("Port") },
                            supportingText = { Text(if (portEditMode) "Enter 1024-65535" else "Requires app restart to apply") },
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                            readOnly = !portEditMode,
                            modifier = Modifier.weight(1f)
                        )
                        if (!portEditMode) {
                            Button(
                                onClick = { portEditMode = true },
                                modifier = Modifier.padding(top = 8.dp)
                            ) {
                                Text("Edit")
                            }
                        } else {
                            Column(modifier = Modifier.padding(top = 8.dp)) {
                                Button(
                                    onClick = {
                                        portText.toIntOrNull()?.let { port ->
                                            if (port in 1024..65535) {
                                                scope.launch {
                                                    viewModel.setWebServerPort(port)
                                                    portEditMode = false
                                                }
                                            }
                                        }
                                    }
                                ) {
                                    Text("Save")
                                }
                                Spacer(modifier = Modifier.height(4.dp))
                                OutlinedButton(
                                    onClick = {
                                        portText = webServerPort.toString()
                                        portEditMode = false
                                    }
                                ) {
                                    Text("Cancel")
                                }
                            }
                        }
                    }
                    
                    Spacer(modifier = Modifier.height(16.dp))
                    HorizontalDivider()
                    Spacer(modifier = Modifier.height(16.dp))
                    
                    // HTTP Basic Authentication
                    Text(
                        text = "🔒 HTTP Basic Authentication",
                        style = MaterialTheme.typography.titleSmall
                    )
                    Spacer(modifier = Modifier.height(8.dp))
                    
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Text(
                            text = if (webAuthEnabled) "Enabled" else "Disabled",
                            style = MaterialTheme.typography.bodyMedium
                        )
                        Switch(
                            checked = webAuthEnabled,
                            onCheckedChange = { enabled ->
                                scope.launch {
                                    viewModel.setWebAuthEnabled(enabled)
                                }
                            }
                        )
                    }
                    
                    if (webAuthEnabled) {
                        Spacer(modifier = Modifier.height(8.dp))
                        OutlinedTextField(
                            value = authUsernameText,
                            onValueChange = { authUsernameText = it },
                            label = { Text("Username") },
                            modifier = Modifier.fillMaxWidth()
                        )
                        Spacer(modifier = Modifier.height(8.dp))
                        OutlinedTextField(
                            value = authPasswordText,
                            onValueChange = { authPasswordText = it },
                            label = { Text("Password") },
                            visualTransformation = PasswordVisualTransformation(),
                            modifier = Modifier.fillMaxWidth()
                        )
                        Spacer(modifier = Modifier.height(8.dp))
                        Button(
                            onClick = {
                                scope.launch {
                                    // Save hashed password
                                    val hashedPassword = hashPassword(authPasswordText)
                                    viewModel.setWebAuthUsername(authUsernameText)
                                    viewModel.setWebAuthPassword(hashedPassword)
                                    showAuthPasswordHashDialog = true
                                }
                            },
                            modifier = Modifier.fillMaxWidth()
                        ) {
                            Text("Save Credentials")
                        }
                        
                        Text(
                            text = "Applies to all endpoints when enabled",
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                            modifier = Modifier.padding(top = 8.dp)
                        )
                    }
                    
                    Text(
                        text = "Auto-starts with app",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        modifier = Modifier.padding(top = 8.dp)
                    )
                }
            }
            
            Spacer(modifier = Modifier.height(8.dp))
            
            //============================================
            // SERVICES STATUS SECTION (CONSOLIDATED)
            //============================================
            SectionHeader("Services Status")
            
            ElevatedCard(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp, vertical = 8.dp)
            ) {
                Column(modifier = Modifier.padding(16.dp)) {
                    // BLE Service Status
                    Text(
                        text = "BLE Bridge Service",
                        style = MaterialTheme.typography.titleSmall
                    )
                    Spacer(modifier = Modifier.height(4.dp))
                    
                    val (bleStatusText, bleStatusColor) = when {
                        !serviceRunning -> "⚫ Stopped" to MaterialTheme.colorScheme.onSurfaceVariant
                        !bluetoothAvailable -> "⚠️ Bluetooth OFF" to Color(0xFFFF9800) // Orange
                        bleScanningActive -> "🟢 Scanning" to Color(0xFF4CAF50) // Green
                        else -> "🟡 Running (not scanning)" to Color(0xFFFFC107) // Yellow
                    }
                    Text(
                        text = bleStatusText,
                        style = MaterialTheme.typography.bodyMedium,
                        color = bleStatusColor
                    )
                    
                    if (serviceRunning && !bleScanningActive && bluetoothAvailable) {
                        Text(
                            text = "⚠️ Service is enabled but not scanning",
                            style = MaterialTheme.typography.bodySmall,
                            color = Color(0xFFFFC107),
                            modifier = Modifier.padding(top = 4.dp)
                        )
                    }
                    
                    Spacer(modifier = Modifier.height(12.dp))
                    HorizontalDivider()
                    Spacer(modifier = Modifier.height(12.dp))
                    
                    // MQTT Service Status
                    Text(
                        text = "MQTT Output Service",
                        style = MaterialTheme.typography.titleSmall
                    )
                    Spacer(modifier = Modifier.height(4.dp))
                    
                    val (mqttStatusText, mqttStatusColor) = if (mqttConnected) {
                        "🟢 Connected" to Color(0xFF4CAF50)
                    } else {
                        "⚫ Disconnected" to MaterialTheme.colorScheme.onSurfaceVariant
                    }
                    Text(
                        text = mqttStatusText,
                        style = MaterialTheme.typography.bodyMedium,
                        color = mqttStatusColor
                    )
                    if (mqttConnected && mqttBrokerHost.isNotEmpty()) {
                        Text(
                            text = "$mqttBrokerHost:$mqttBrokerPort",
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                    
                    Spacer(modifier = Modifier.height(12.dp))
                    HorizontalDivider()
                    Spacer(modifier = Modifier.height(12.dp))
                    
                    // HTTP Polling Service Status
                    Text(
                        text = "HTTP Polling Service",
                        style = MaterialTheme.typography.titleSmall
                    )
                    Spacer(modifier = Modifier.height(4.dp))
                    
                    val (pollingStatusText, pollingStatusColor) = if (pollingRunning) {
                        "🟢 Running" to Color(0xFF4CAF50)
                    } else {
                        "⚫ Stopped" to MaterialTheme.colorScheme.onSurfaceVariant
                    }
                    Text(
                        text = pollingStatusText,
                        style = MaterialTheme.typography.bodyMedium,
                        color = pollingStatusColor
                    )
                    
                    Spacer(modifier = Modifier.height(12.dp))
                    
                    Row(
                        modifier = Modifier
                            .fillMaxWidth()
                            .clickable {
                                val intent = Intent(Intent.ACTION_VIEW, Uri.parse(webInterfaceUrl))
                                context.startActivity(intent)
                            }
                            .padding(vertical = 4.dp),
                        horizontalArrangement = Arrangement.spacedBy(8.dp),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Text(
                            text = "Configure services and plugins in web interface",
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                            modifier = Modifier.weight(1f)
                        )
                        Icon(
                            imageVector = Icons.Filled.OpenInNew,
                            contentDescription = "Open web interface",
                            modifier = Modifier.size(16.dp),
                            tint = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                }
            }
            
            Spacer(modifier = Modifier.height(8.dp))
            
            //============================================
            // PERMISSIONS & OPTIMIZATIONS SECTION
            //============================================
            SectionHeader("Permissions & Optimizations")

            // Runtime Permissions
            ElevatedCard(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp, vertical = 8.dp)
            ) {
                Column(modifier = Modifier.padding(16.dp)) {
                    Text(
                        text = "Runtime Permissions",
                        style = MaterialTheme.typography.titleSmall
                    )
                    Spacer(modifier = Modifier.height(8.dp))

                    PermissionRow(
                        name = "Location",
                        granted = hasLocationPermission,
                        onToggle = { requestAllPermissions() }
                    )

                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
                        PermissionRow(
                            name = "Bluetooth",
                            granted = hasBluetoothPermission,
                            onToggle = { requestAllPermissions() }
                        )
                    }

                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                        PermissionRow(
                            name = "Notifications",
                            granted = hasNotificationPermission,
                            onToggle = { requestAllPermissions() }
                        )
                    }

                    OutlinedButton(
                        onClick = { requestAllPermissions() },
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(top = 8.dp)
                    ) {
                        Text("Request Permissions")
                    }
                }
            }

            // Battery Optimization (only on battery-powered devices)
            val hasBattery = remember {
                try {
                    val batteryManager = context.getSystemService(android.content.Context.BATTERY_SERVICE) as? android.os.BatteryManager
                    batteryManager != null && context.registerReceiver(null, android.content.IntentFilter(Intent.ACTION_BATTERY_CHANGED))?.let { intent ->
                        intent.getIntExtra(android.os.BatteryManager.EXTRA_PRESENT, 0) == 1
                    } ?: true
                } catch (e: Exception) {
                    true // Assume battery if we can't determine
                }
            }
            
            if (hasBattery) {
                ElevatedCard(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 16.dp, vertical = 8.dp)
                ) {
                    Column(modifier = Modifier.padding(16.dp)) {
                        Text(
                            text = "Battery Optimization",
                            style = MaterialTheme.typography.titleSmall
                        )
                        Spacer(modifier = Modifier.height(8.dp))

                        Text(
                            text = if (isIgnoringBatteryOpt) "✅ Disabled (Recommended)" else "⚠️ Enabled",
                            style = MaterialTheme.typography.bodyMedium,
                            color = if (isIgnoringBatteryOpt) Color(0xFF4CAF50) else Color(0xFFFF9800)
                        )

                        if (!isIgnoringBatteryOpt) {
                            Text(
                                text = "Android battery optimization can prevent the service from running in the background. Use this option to disable battery optimization.",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant,
                                modifier = Modifier.padding(top = 4.dp)
                            )

                            OutlinedButton(
                                onClick = {
                                    val intent = BatteryOptimizationHelper.createBatteryOptimizationIntent(context)
                                    if (intent != null) {
                                        context.startActivity(intent)
                                    }
                                },
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(top = 8.dp)
                            ) {
                                Text("Disable Battery Optimization")
                            }
                        }
                    }
                }
            }
        }
    }
    
    // Password saved confirmation dialog
    if (showAuthPasswordHashDialog) {
        AlertDialog(
            onDismissRequest = { showAuthPasswordHashDialog = false },
            title = { Text("Credentials Saved") },
            text = { Text("Authentication credentials have been saved successfully.") },
            confirmButton = {
                TextButton(onClick = { showAuthPasswordHashDialog = false }) {
                    Text("OK")
                }
            }
        )
    }
}

@Composable
private fun PermissionRow(
    name: String,
    granted: Boolean,
    onToggle: () -> Unit
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 4.dp),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Column(modifier = Modifier.weight(1f)) {
            Text(
                text = name,
                style = MaterialTheme.typography.bodyMedium
            )
            Text(
                text = if (granted) "Granted" else "Not granted",
                style = MaterialTheme.typography.bodySmall,
                color = if (granted)
                    MaterialTheme.colorScheme.primary
                else
                    MaterialTheme.colorScheme.error
            )
        }

        Switch(
            checked = granted,
            onCheckedChange = { onToggle() }
        )
    }
}

@Composable
private fun SectionHeader(text: String) {
    Text(
        text = text,
        style = MaterialTheme.typography.titleMedium,
        color = MaterialTheme.colorScheme.primary,
        modifier = Modifier.padding(horizontal = 16.dp, vertical = 12.dp)
    )
}

/**
 * Simple SHA-256 hash for password storage
 * In production, this should use BCrypt or similar
 */
private fun hashPassword(password: String): String {
    val bytes = java.security.MessageDigest.getInstance("SHA-256").digest(password.toByteArray())
    return bytes.joinToString("") { "%02x".format(it) }
}
