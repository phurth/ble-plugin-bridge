package com.blemqttbridge.ui

import android.Manifest
import android.content.Intent
import android.content.pm.PackageManager
import android.net.Uri
import android.os.Build
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.OpenInBrowser
import androidx.compose.material.icons.filled.QrCode
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import androidx.core.content.ContextCompat
import androidx.lifecycle.viewmodel.compose.viewModel
import com.blemqttbridge.core.BaseBleService
import com.blemqttbridge.core.ServiceStateManager
import com.blemqttbridge.ui.components.ExpandableSection
import com.blemqttbridge.ui.viewmodel.SettingsViewModel
import com.blemqttbridge.utils.BatteryOptimizationHelper
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SettingsScreen(
    viewModel: SettingsViewModel = viewModel(),
    onNavigateToSystemSettings: () -> Unit
) {
    val scrollState = rememberScrollState()
    val context = LocalContext.current
    val scope = rememberCoroutineScope()
    
    // Collect state flows
    val mqttEnabled by viewModel.mqttEnabled.collectAsState()
    val serviceEnabled by viewModel.serviceEnabled.collectAsState()
    val webServerPort by viewModel.webServerPort.collectAsState()
    val webAuthEnabled by viewModel.webAuthEnabled.collectAsState()
    val webAuthUsername by viewModel.webAuthUsername.collectAsState()
    val webAuthPassword by viewModel.webAuthPassword.collectAsState()
    
    // MQTT status
    val mqttConnected by viewModel.mqttConnectedStatus.collectAsState()
    val mqttBrokerHost by viewModel.mqttBrokerHost.collectAsState()
    val mqttBrokerPort by viewModel.mqttBrokerPort.collectAsState()
    
    // Plugin instances
    val pluginStatuses by viewModel.pluginStatuses.collectAsState()
    
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
                actions = {
                    IconButton(onClick = onNavigateToSystemSettings) {
                        Icon(
                            imageVector = Icons.Filled.Settings,
                            contentDescription = "System Settings"
                        )
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
                                imageVector = Icons.Filled.OpenInBrowser,
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
                        Text("Enable Authentication")
                        Switch(
                            checked = webAuthEnabled,
                            onCheckedChange = { 
                                scope.launch {
                                    viewModel.setWebAuthEnabled(it)
                                }
                            }
                        )
                    }
                    
                    if (webAuthEnabled) {
                        Spacer(modifier = Modifier.height(12.dp))
                        
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
                        
                        Spacer(modifier = Modifier.height(12.dp))
                        
                        Button(
                            onClick = {
                                scope.launch {
                                    // Hash password before saving
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
            // BLE SERVICE SECTION
            //============================================
            SectionHeader("BLE Bridge Service")
            
            ElevatedCard(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp, vertical = 8.dp)
            ) {
                Column(modifier = Modifier.padding(16.dp)) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column {
                            Text(
                                text = "Service Status",
                                style = MaterialTheme.typography.bodyMedium
                            )
                            Text(
                                text = if (serviceEnabled) "� Running" else "⚫ Stopped",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                        }
                        Switch(
                            checked = serviceEnabled,
                            onCheckedChange = { viewModel.setServiceEnabled(it) }
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
            // MQTT SECTION (SIMPLIFIED)
            //============================================
            SectionHeader("MQTT Output Service")
            
            ElevatedCard(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp, vertical = 8.dp)
            ) {
                Column(modifier = Modifier.padding(16.dp)) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column {
                            Text(
                                text = if (mqttConnected) "● Connected" else "○ Disconnected",
                                style = MaterialTheme.typography.bodyMedium,
                                color = if (mqttConnected) Color(0xFF4CAF50) else MaterialTheme.colorScheme.onSurfaceVariant
                            )
                            if (mqttConnected && mqttBrokerHost.isNotEmpty()) {
                                Text(
                                    text = "$mqttBrokerHost:$mqttBrokerPort",
                                    style = MaterialTheme.typography.bodySmall,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant
                                )
                            }
                        }
                        Switch(
                            checked = mqttEnabled,
                            onCheckedChange = { viewModel.setMqttEnabled(it) }
                        )
                    }
                    
                    Spacer(modifier = Modifier.height(12.dp))
                    
                    Text(
                        text = "Configure MQTT settings in web interface",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                    
                    OutlinedButton(
                        onClick = {
                            val intent = Intent(Intent.ACTION_VIEW, Uri.parse(webInterfaceUrl))
                            context.startActivity(intent)
                        },
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(top = 8.dp)
                    ) {
                        Text("Open Web Interface")
                    }
                }
            }
            
            Spacer(modifier = Modifier.height(8.dp))
            
            //============================================
            // PLUGIN INSTANCES SECTION
            //============================================
            SectionHeader("Plugin Instances")
            
            ElevatedCard(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp, vertical = 8.dp)
            ) {
                Column(modifier = Modifier.padding(16.dp)) {
                    // Load all instances and display them - reload when pluginStatuses changes
                    val instances = remember(pluginStatuses) { ServiceStateManager.getAllInstances(context) }
                    
                    if (instances.isEmpty()) {
                        Text(
                            text = "No plugin instances configured",
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    } else {
                        Column {
                            instances.values.forEachIndexed { index, instance ->
                                if (index > 0) {
                                    Spacer(modifier = Modifier.height(8.dp))
                                    HorizontalDivider()
                                    Spacer(modifier = Modifier.height(8.dp))
                                }
                                
                                val status = pluginStatuses[instance.instanceId]
                                val isHealthy = status?.let { 
                                    it.connected && it.authenticated && it.dataHealthy 
                                } ?: false
                                
                                Row(
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .clickable {
                                            if (!isHealthy) {
                                                val intent = Intent(Intent.ACTION_VIEW, Uri.parse(webInterfaceUrl))
                                                context.startActivity(intent)
                                            }
                                        },
                                    verticalAlignment = Alignment.CenterVertically
                                ) {
                                    Text(
                                        text = if (isHealthy) "🟢" else "🔴",
                                        style = MaterialTheme.typography.bodyLarge,
                                        modifier = Modifier.padding(end = 12.dp)
                                    )
                                    Column(modifier = Modifier.weight(1f)) {
                                        Text(
                                            text = instance.displayName ?: instance.instanceId,
                                            style = MaterialTheme.typography.bodyMedium
                                        )
                                        Text(
                                            text = "${instance.pluginType.uppercase()} - ${instance.deviceMac}",
                                            style = MaterialTheme.typography.bodySmall,
                                            color = MaterialTheme.colorScheme.onSurfaceVariant
                                        )
                                    }
                                }
                            }
                        }
                    }
                    
                    Spacer(modifier = Modifier.height(16.dp))
                    HorizontalDivider()
                    Spacer(modifier = Modifier.height(12.dp))
                    
                    Text(
                        text = "Manage instances in web interface",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                    
                    OutlinedButton(
                        onClick = {
                            val intent = Intent(Intent.ACTION_VIEW, Uri.parse(webInterfaceUrl))
                            context.startActivity(intent)
                        },
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(top = 8.dp)
                    ) {
                        Text("Open Web Interface")
                    }
                    
                    Text(
                        text = "🔴 = Unhealthy (tap for details in web UI)",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        modifier = Modifier.padding(top = 8.dp)
                    )
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
