package com.blemqttbridge.ui

import android.Manifest
import android.content.ClipData
import android.content.ClipboardManager
import android.content.Context
import android.content.pm.PackageManager
import android.os.Build
import android.widget.Toast
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.ContentCopy
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalLifecycleOwner
import androidx.compose.ui.unit.dp
import androidx.core.content.ContextCompat
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleEventObserver
import androidx.lifecycle.viewmodel.compose.viewModel
import com.blemqttbridge.ui.viewmodel.SettingsViewModel
import com.blemqttbridge.utils.AndroidTvHelper
import com.blemqttbridge.utils.BatteryOptimizationHelper

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SystemSettingsScreen(
    viewModel: SettingsViewModel = viewModel(),
    onRequestPermissions: () -> Unit,
    onNavigateBack: () -> Unit
) {
    val scrollState = rememberScrollState()
    val context = LocalContext.current
    val lifecycleOwner = LocalLifecycleOwner.current
    
    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("System Settings") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(
                            imageVector = Icons.Filled.ArrowBack,
                            contentDescription = "Back"
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
                .padding(horizontal = 12.dp, vertical = 8.dp)
                .verticalScroll(scrollState),
            verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            // Permissions & Optimizations Section
            SectionHeader("Permissions & Optimizations")
            
            // Runtime Permissions
            Text(
                text = "Runtime Permissions",
                style = MaterialTheme.typography.titleSmall,
                color = MaterialTheme.colorScheme.primary,
                modifier = Modifier.padding(start = 6.dp, end = 6.dp, bottom = 8.dp, top = 0.dp)
            )
            
            var hasLocation by remember {
                mutableStateOf(
                    ContextCompat.checkSelfPermission(
                        context,
                        Manifest.permission.ACCESS_FINE_LOCATION
                    ) == PackageManager.PERMISSION_GRANTED
                )
            }
            
            var hasBluetooth by remember {
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
            
            var hasNotifications by remember {
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
            
            // Refresh permission states when screen becomes visible (user returns from settings)
            DisposableEffect(lifecycleOwner) {
                val observer = LifecycleEventObserver { _, event ->
                    if (event == Lifecycle.Event.ON_RESUME) {
                        hasLocation = ContextCompat.checkSelfPermission(
                            context,
                            Manifest.permission.ACCESS_FINE_LOCATION
                        ) == PackageManager.PERMISSION_GRANTED
                        
                        hasBluetooth = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
                            ContextCompat.checkSelfPermission(
                                context,
                                Manifest.permission.BLUETOOTH_SCAN
                            ) == PackageManager.PERMISSION_GRANTED
                        } else {
                            true
                        }
                        
                        hasNotifications = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                            ContextCompat.checkSelfPermission(
                                context,
                                Manifest.permission.POST_NOTIFICATIONS
                            ) == PackageManager.PERMISSION_GRANTED
                        } else {
                            true
                        }
                    }
                }
                lifecycleOwner.lifecycle.addObserver(observer)
                onDispose {
                    lifecycleOwner.lifecycle.removeObserver(observer)
                }
            }
            
            Column(
                modifier = Modifier.padding(horizontal = 6.dp),
                verticalArrangement = Arrangement.spacedBy(2.dp)
            ) {
                // Location Permission
                PermissionRow(
                    name = "Location",
                    granted = hasLocation,
                    onToggle = onRequestPermissions
                )
                
                // Bluetooth Permission (Android 12+)
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
                    PermissionRow(
                        name = "Bluetooth",
                        granted = hasBluetooth,
                        onToggle = onRequestPermissions
                    )
                }
                
                // Notification Permission (Android 13+)
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                    PermissionRow(
                        name = "Notifications",
                        granted = hasNotifications,
                        onToggle = onRequestPermissions
                    )
                }
            }
            
            Spacer(modifier = Modifier.height(12.dp))
            
            // Battery Optimization subsection
            var batteryExempt by remember {
                mutableStateOf(BatteryOptimizationHelper.isIgnoringBatteryOptimizations(context))
            }
            
            // Refresh battery exemption status when screen becomes visible (user returns from settings)
            DisposableEffect(lifecycleOwner) {
                val observer = LifecycleEventObserver { _, event ->
                    if (event == Lifecycle.Event.ON_RESUME) {
                        batteryExempt = BatteryOptimizationHelper.isIgnoringBatteryOptimizations(context)
                    }
                }
                lifecycleOwner.lifecycle.addObserver(observer)
                onDispose {
                    lifecycleOwner.lifecycle.removeObserver(observer)
                }
            }
            
            Text(
                text = "Battery Optimization",
                style = MaterialTheme.typography.titleSmall,
                color = MaterialTheme.colorScheme.primary,
                modifier = Modifier.padding(horizontal = 6.dp, vertical = 8.dp)
            )
            
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 6.dp, vertical = 6.dp),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Column(modifier = Modifier.weight(1f)) {
                    Text(
                        text = "Battery Optimization Exemption",
                        style = MaterialTheme.typography.bodyMedium
                    )
                    Text(
                        text = if (batteryExempt)
                            "Active - Service protected"
                        else
                            "Inactive - Service may be killed",
                        style = MaterialTheme.typography.bodySmall,
                        color = if (batteryExempt)
                            MaterialTheme.colorScheme.primary
                        else
                            MaterialTheme.colorScheme.error
                    )
                }
                
                Switch(
                    checked = batteryExempt,
                    onCheckedChange = { wantsExempt ->
                        if (wantsExempt && !batteryExempt) {
                            // User wants exemption - open system settings
                            val intent = BatteryOptimizationHelper.createBatteryOptimizationIntent(context)
                            if (intent != null) {
                                context.startActivity(intent)
                            }
                        } else if (!wantsExempt && batteryExempt) {
                            // User wants to remove exemption - open battery settings
                            val intent = BatteryOptimizationHelper.createBatterySettingsIntent(context)
                            context.startActivity(intent)
                        }
                    }
                )
            }
            
            // Auto-start on Boot
            Text(
                text = "Auto-Start",
                style = MaterialTheme.typography.titleSmall,
                color = MaterialTheme.colorScheme.primary,
                modifier = Modifier.padding(start = 6.dp, end = 6.dp, bottom = 8.dp, top = 8.dp)
            )
            
            var autoStartEnabled by remember {
                mutableStateOf(com.blemqttbridge.receivers.BootReceiver.isAutoStartEnabled(context))
            }
            
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 6.dp, vertical = 6.dp),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Column(modifier = Modifier.weight(1f)) {
                    Text(
                        text = "Start on Boot",
                        style = MaterialTheme.typography.bodyMedium
                    )
                    Text(
                        text = if (autoStartEnabled)
                            "Service starts automatically when device boots"
                        else
                            "Service requires manual start after boot",
                        style = MaterialTheme.typography.bodySmall,
                        color = if (autoStartEnabled)
                            MaterialTheme.colorScheme.primary
                        else
                            MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f)
                    )
                }
                
                Switch(
                    checked = autoStartEnabled,
                    onCheckedChange = { enabled ->
                        autoStartEnabled = enabled
                        com.blemqttbridge.receivers.BootReceiver.setAutoStartEnabled(context, enabled)
                    }
                )
            }
            
            Spacer(modifier = Modifier.height(12.dp))
            
            // Doze Mode Keepalive Setting
            Text(
                text = "Doze Mode Prevention",
                style = MaterialTheme.typography.titleSmall,
                color = MaterialTheme.colorScheme.primary,
                modifier = Modifier.padding(start = 6.dp, end = 6.dp, bottom = 8.dp, top = 8.dp)
            )
            
            val prefs = context.getSharedPreferences("app_settings", android.content.Context.MODE_PRIVATE)
            var keepAliveEnabled by remember {
                mutableStateOf(prefs.getBoolean("keepalive_enabled", true))
            }
            
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 6.dp, vertical = 6.dp),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Column(modifier = Modifier.weight(1f)) {
                    Text(
                        text = "Keepalive Pings",
                        style = MaterialTheme.typography.bodyMedium
                    )
                    Text(
                        text = if (keepAliveEnabled)
                            "Active - Prevents disconnection during idle (30min interval)"
                        else
                            "Inactive - Connections may drop overnight",
                        style = MaterialTheme.typography.bodySmall,
                        color = if (keepAliveEnabled)
                            MaterialTheme.colorScheme.primary
                        else
                            MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f)
                    )
                }
                
                Switch(
                    checked = keepAliveEnabled,
                    onCheckedChange = { enabled ->
                        keepAliveEnabled = enabled
                        prefs.edit().putBoolean("keepalive_enabled", enabled).apply()
                        
                        // Notify service to update keepalive schedule
                        val intent = android.content.Intent(context, com.blemqttbridge.core.BaseBleService::class.java)
                        intent.action = if (enabled) {
                            com.blemqttbridge.core.BaseBleService.ACTION_KEEPALIVE_PING
                        } else {
                            // Send a dummy action to trigger service update
                            com.blemqttbridge.core.BaseBleService.ACTION_KEEPALIVE_PING
                        }
                        try {
                            context.startService(intent)
                        } catch (_: Exception) {}
                    }
                )
            }
            
            // Android TV Section - Only show on Android TV devices
            val isAndroidTv = remember { AndroidTvHelper.isAndroidTv(context) }
            
            if (isAndroidTv) {
                Spacer(modifier = Modifier.height(16.dp))
                
                SectionHeader("Android TV Power Fix")
                
                var hasSecureSettingsPermission by remember {
                    mutableStateOf(AndroidTvHelper.hasWriteSecureSettingsPermission(context))
                }
                var cecAutoOffEnabled by remember {
                    mutableStateOf(AndroidTvHelper.isCecAutoOffEnabled(context))
                }
                
                // Refresh state when screen becomes visible
                DisposableEffect(lifecycleOwner) {
                    val observer = LifecycleEventObserver { _, event ->
                        if (event == Lifecycle.Event.ON_RESUME) {
                            hasSecureSettingsPermission = AndroidTvHelper.hasWriteSecureSettingsPermission(context)
                            cecAutoOffEnabled = AndroidTvHelper.isCecAutoOffEnabled(context)
                        }
                    }
                    lifecycleOwner.lifecycle.addObserver(observer)
                    onDispose {
                        lifecycleOwner.lifecycle.removeObserver(observer)
                    }
                }
                
                ElevatedCard(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 6.dp, vertical = 2.dp)
                ) {
                    Column(modifier = Modifier.padding(12.dp)) {
                        Text(
                            text = "HDMI-CEC Auto Device Off",
                            style = MaterialTheme.typography.titleSmall
                        )
                        
                        Spacer(modifier = Modifier.height(8.dp))
                        
                        Text(
                            text = "When enabled, the TV can put this device to sleep via HDMI-CEC, which kills the service. Disable this to keep the service running when the TV powers off.",
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                        
                        Spacer(modifier = Modifier.height(12.dp))
                        
                        // Status indicator
                        Text(
                            text = AndroidTvHelper.getStatusMessage(context),
                            style = MaterialTheme.typography.bodySmall,
                            color = if (cecAutoOffEnabled) 
                                MaterialTheme.colorScheme.error 
                            else 
                                MaterialTheme.colorScheme.primary
                        )
                        
                        Spacer(modifier = Modifier.height(12.dp))
                        
                        if (hasSecureSettingsPermission) {
                            // User has permission - show toggle
                            Row(
                                modifier = Modifier.fillMaxWidth(),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Column(modifier = Modifier.weight(1f)) {
                                    Text(
                                        text = "CEC Auto Device Off",
                                        style = MaterialTheme.typography.bodyMedium
                                    )
                                    Text(
                                        text = if (cecAutoOffEnabled) "Enabled - Device sleeps with TV" else "Disabled - Service survives",
                                        style = MaterialTheme.typography.bodySmall,
                                        color = if (cecAutoOffEnabled) 
                                            MaterialTheme.colorScheme.error 
                                        else 
                                            MaterialTheme.colorScheme.primary
                                    )
                                }
                                
                                Switch(
                                    checked = !cecAutoOffEnabled,  // Inverted: switch ON = CEC disabled = good
                                    onCheckedChange = { wantDisabled ->
                                        val success = AndroidTvHelper.setCecAutoOff(context, !wantDisabled)
                                        if (success) {
                                            cecAutoOffEnabled = !wantDisabled
                                        }
                                    }
                                )
                            }
                        } else {
                            // No permission - show ADB instructions
                            Text(
                                text = "⚠ Permission Required",
                                style = MaterialTheme.typography.titleSmall,
                                color = MaterialTheme.colorScheme.error
                            )
                            
                            Spacer(modifier = Modifier.height(8.dp))
                            
                            Text(
                                text = "To enable automatic CEC control, grant WRITE_SECURE_SETTINGS via ADB (one-time setup):",
                                style = MaterialTheme.typography.bodySmall
                            )
                            
                            Spacer(modifier = Modifier.height(8.dp))
                            
                            val adbCommand = AndroidTvHelper.getGrantPermissionCommand(context)
                            
                            Surface(
                                color = MaterialTheme.colorScheme.surfaceVariant,
                                shape = MaterialTheme.shapes.small,
                                modifier = Modifier.fillMaxWidth()
                            ) {
                                Row(
                                    modifier = Modifier.padding(8.dp),
                                    verticalAlignment = Alignment.CenterVertically
                                ) {
                                    Text(
                                        text = adbCommand,
                                        style = MaterialTheme.typography.bodySmall,
                                        modifier = Modifier.weight(1f),
                                        fontFamily = androidx.compose.ui.text.font.FontFamily.Monospace
                                    )
                                    
                                    IconButton(
                                        onClick = {
                                            val clipboard = context.getSystemService(Context.CLIPBOARD_SERVICE) as ClipboardManager
                                            val clip = ClipData.newPlainText("ADB Command", adbCommand)
                                            clipboard.setPrimaryClip(clip)
                                            Toast.makeText(context, "Command copied!", Toast.LENGTH_SHORT).show()
                                        }
                                    ) {
                                        Icon(
                                            imageVector = Icons.Default.ContentCopy,
                                            contentDescription = "Copy command",
                                            tint = MaterialTheme.colorScheme.primary
                                        )
                                    }
                                }
                            }
                            
                            Spacer(modifier = Modifier.height(8.dp))
                            
                            Text(
                                text = "After granting permission, restart the app. The service will automatically disable CEC auto-off on startup.",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                            
                            Spacer(modifier = Modifier.height(12.dp))
                            
                            // Alternative: Manual ADB fix
                            Text(
                                text = "Alternative: Disable CEC directly via ADB:",
                                style = MaterialTheme.typography.bodySmall
                            )
                            
                            Spacer(modifier = Modifier.height(4.dp))
                            
                            val manualCommand = AndroidTvHelper.getDisableCecCommand()
                            
                            Surface(
                                color = MaterialTheme.colorScheme.surfaceVariant,
                                shape = MaterialTheme.shapes.small,
                                modifier = Modifier.fillMaxWidth()
                            ) {
                                Row(
                                    modifier = Modifier.padding(8.dp),
                                    verticalAlignment = Alignment.CenterVertically
                                ) {
                                    Text(
                                        text = manualCommand,
                                        style = MaterialTheme.typography.bodySmall,
                                        modifier = Modifier.weight(1f),
                                        fontFamily = androidx.compose.ui.text.font.FontFamily.Monospace
                                    )
                                    
                                    IconButton(
                                        onClick = {
                                            val clipboard = context.getSystemService(Context.CLIPBOARD_SERVICE) as ClipboardManager
                                            val clip = ClipData.newPlainText("ADB Command", manualCommand)
                                            clipboard.setPrimaryClip(clip)
                                            Toast.makeText(context, "Command copied!", Toast.LENGTH_SHORT).show()
                                        }
                                    ) {
                                        Icon(
                                            imageVector = Icons.Default.ContentCopy,
                                            contentDescription = "Copy command",
                                            tint = MaterialTheme.colorScheme.primary
                                        )
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            Spacer(modifier = Modifier.height(16.dp))
            
            // Web Interface Section
            SectionHeader("Web Interface")
            
            val webServerEnabled by viewModel.webServerEnabled.collectAsState(initial = false)
            
            ElevatedCard(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 6.dp, vertical = 2.dp)
            ) {
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(6.dp)
                ) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column(modifier = Modifier.weight(1f)) {
                            Text(
                                text = "Enable Web Interface",
                                style = MaterialTheme.typography.bodyMedium
                            )
                            Text(
                                text = "Access configuration from browser",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                        }
                        Switch(
                            checked = webServerEnabled,
                            onCheckedChange = { viewModel.setWebServerEnabled(it) }
                        )
                    }
                    
                    if (webServerEnabled) {
                        Spacer(modifier = Modifier.height(12.dp))
                        HorizontalDivider()
                        Spacer(modifier = Modifier.height(12.dp))
                        
                        // Get IP address
                        val ipAddress = remember { viewModel.getLocalIpAddress() }
                        val webUrl = "http://$ipAddress:8088"
                        
                        Column {
                            Text(
                                text = "Web Interface URL:",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                            Spacer(modifier = Modifier.height(4.dp))
                            Row(
                                modifier = Modifier.fillMaxWidth(),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Text(
                                    text = webUrl,
                                    style = MaterialTheme.typography.bodyMedium,
                                    color = MaterialTheme.colorScheme.primary,
                                    modifier = Modifier.weight(1f)
                                )
                                IconButton(
                                    onClick = {
                                        val clipboard = context.getSystemService(Context.CLIPBOARD_SERVICE) as ClipboardManager
                                        val clip = ClipData.newPlainText("Web URL", webUrl)
                                        clipboard.setPrimaryClip(clip)
                                        Toast.makeText(context, "URL copied to clipboard", Toast.LENGTH_SHORT).show()
                                    }
                                ) {
                                    Icon(
                                        imageVector = Icons.Filled.ContentCopy,
                                        contentDescription = "Copy URL",
                                        tint = MaterialTheme.colorScheme.primary
                                    )
                                }
                            }
                            Spacer(modifier = Modifier.height(8.dp))
                            Text(
                                text = "Open this URL in a browser on any device connected to the same network.",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                        }
                    }
                }
            }
            
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
        style = MaterialTheme.typography.labelLarge,
        color = MaterialTheme.colorScheme.primary,
        modifier = Modifier.padding(horizontal = 6.dp, vertical = 4.dp)
    )
}
