// Theme management
function initTheme() {
    const savedTheme = localStorage.getItem('theme') || 'light';
    document.documentElement.setAttribute('data-theme', savedTheme);
    updateThemeButton(savedTheme);
}

function toggleTheme() {
    const current = document.documentElement.getAttribute('data-theme') || 'light';
    const newTheme = current === 'light' ? 'dark' : 'light';
    document.documentElement.setAttribute('data-theme', newTheme);
    localStorage.setItem('theme', newTheme);
    updateThemeButton(newTheme);
}

function updateThemeButton(theme) {
    const btn = document.getElementById('theme-toggle');
    if (btn) {
        btn.textContent = theme === 'dark' ? '☀️ Light Mode' : '🌙 Dark Mode';
    }
}

// Initialize theme on page load
initTheme();

// Global state
let serviceRunning = false;
let bleEnabled = false;  // Track BLE service state separately
let pollingRunning = false; // Track HTTP polling service state
let configChanged = {}; // Track which configs have changed
let editingFields = {}; // Track which fields are currently being edited
let instanceToRemove = null;
let instanceToRemoveIsPolling = false;

// Track recently toggled switches to avoid re-rendering during state transition
let recentlyToggledSwitches = new Set();
const TOGGLE_DEBOUNCE_MS = 3000;

const PLUGIN_TYPE_NAMES = {
    'onecontrol': 'OneControl',
    'easytouch': 'EasyTouch',
    'gopower': 'GoPower',
    'hughes_watchdog': 'Hughes Watchdog',
    'hughes_gen2': 'Hughes Watchdog Gen2',
    'mopeka': 'Mopeka',
    'blescanner': 'BLE Scanner',
    'peplink': 'Peplink Router'
};

const MULTI_INSTANCE_PLUGINS = ['easytouch', 'mopeka', 'peplink']; // Plugins supporting multiple instances

// MAC address field handlers
function setupMacFieldHandlers(prefix) {
    for (let i = 0; i < 6; i++) {
        const field = document.getElementById(`${prefix}-${i}`);
        if (!field) continue;
        
        // Handle input - only allow hex characters
        field.addEventListener('input', function(e) {
            this.value = this.value.replace(/[^0-9A-Fa-f]/g, '').toUpperCase();
            
            // Auto-advance to next field when 2 characters entered
            if (this.value.length === 2 && i < 5) {
                document.getElementById(`${prefix}-${i + 1}`).focus();
            }
        });
        
        // Handle backspace - move to previous field if empty
        field.addEventListener('keydown', function(e) {
            if (e.key === 'Backspace' && this.value.length === 0 && i > 0) {
                const prevField = document.getElementById(`${prefix}-${i - 1}`);
                prevField.focus();
                prevField.setSelectionRange(prevField.value.length, prevField.value.length);
            }
        });
        
        // Handle paste - split MAC address across fields
        field.addEventListener('paste', function(e) {
            e.preventDefault();
            const pastedText = e.clipboardData.getData('text').trim();
            
            // Try to parse MAC address (handles AA:BB:CC:DD:EE:FF or AABBCCDDEEFF)
            const cleanMac = pastedText.replace(/[^0-9A-Fa-f]/g, '').toUpperCase();
            if (cleanMac.length === 12) {
                for (let j = 0; j < 6; j++) {
                    const macField = document.getElementById(`${prefix}-${j}`);
                    if (macField) {
                        macField.value = cleanMac.substring(j * 2, j * 2 + 2);
                    }
                }
                document.getElementById(`${prefix}-5`).focus();
            }
        });
    }
}

function getMacAddress(prefix) {
    const parts = [];
    for (let i = 0; i < 6; i++) {
        const field = document.getElementById(`${prefix}-${i}`);
        if (!field || !field.value) return '';
        parts.push(field.value);
    }
    return parts.join(':');
}

function setMacAddress(prefix, mac) {
    if (!mac) {
        for (let i = 0; i < 6; i++) {
            const field = document.getElementById(`${prefix}-${i}`);
            if (field) field.value = '';
        }
        return;
    }
    
    const parts = mac.split(':');
    for (let i = 0; i < 6 && i < parts.length; i++) {
        const field = document.getElementById(`${prefix}-${i}`);
        if (field) field.value = parts[i].toUpperCase();
    }
}

// Plugin type categorization
const BLE_PLUGINS = ['onecontrol', 'easytouch', 'gopower', 'hughes_watchdog', 'hughes_gen2', 'blescanner', 'mopeka'];
const HTTP_PLUGINS = ['peplink'];

function isBlePlugin(pluginType) {
    return BLE_PLUGINS.includes(pluginType);
}

function isHttpPlugin(pluginType) {
    return HTTP_PLUGINS.includes(pluginType);
}

// Load status on page load
window.addEventListener('load', () => {
    loadStatus();
    loadPollingStatus();
    loadConfig();
    loadInstances();
    // Auto-refresh status every 5 seconds
    setInterval(async () => {
        try {
            await loadStatus();
            await loadPollingStatus();
            await loadConfig();  // Also refresh MQTT config to update health indicator
            // Only refresh instances if not currently editing and no recent toggle
            // Skip during service transitions to avoid showing error state
            if (Object.keys(editingFields).length === 0 && recentlyToggledSwitches.size === 0) {
                await loadInstances();
            }
        } catch (e) {
            console.error('Auto-refresh error:', e);
        }
    }, 5000);
});

async function loadStatus() {
    try {
        const response = await fetch('/api/status');
        const data = await response.json();
        serviceRunning = data.running;
        bleEnabled = data.bleEnabled ?? data.running;  // Track BLE service state
        
        // Skip re-rendering toggle if it was recently toggled by user (debounce)
        if (!recentlyToggledSwitches.has('ble')) {
            // Move BLE toggle to BLE Plugins header
            const bleToggleHtml = `
                <label class="toggle-switch">
                    <input type="checkbox" ${data.running ? 'checked' : ''} onchange="toggleService(this.checked)">
                    <span class="toggle-slider"></span>
                </label>
            `;
            document.getElementById('ble-toggle-container').innerHTML = bleToggleHtml;
        }
        
        // Skip re-rendering MQTT toggle if it was recently toggled
        if (!recentlyToggledSwitches.has('mqtt')) {
            // Move MQTT toggle to MQTT Configuration header
            const mqttToggleHtml = `
                <label class="toggle-switch">
                    <input type="checkbox" ${data.mqttEnabled ? 'checked' : ''} onchange="toggleMqtt(this.checked)">
                    <span class="toggle-slider"></span>
                </label>
            `;
            document.getElementById('mqtt-toggle-container').innerHTML = mqttToggleHtml;
        }
        
        // Update add BLE instance button state (enabled when BLE service is DISABLED)
        const addBleBtnBtn = document.getElementById('add-ble-instance-btn');
        if (addBleBtnBtn) {
            addBleBtnBtn.disabled = bleEnabled;  // Disabled when service is enabled, enabled when disabled
        }
    } catch (error) {
        console.error('Failed to load status:', error);
    }
}

async function loadPollingStatus() {
    try {
        const response = await fetch('/api/polling/status');
        const data = await response.json();

        const isRunning = data.running || false;
        pollingRunning = isRunning;

        // Skip re-rendering toggle if it was recently toggled by user (debounce)
        if (!recentlyToggledSwitches.has('polling')) {
            // Move HTTP polling toggle to HTTP Plugins header
            const httpToggleHtml = `
                <label class="toggle-switch">
                    <input type="checkbox" ${isRunning ? 'checked' : ''} onchange="togglePolling(this.checked)">
                    <span class="toggle-slider"></span>
                </label>
            `;
            document.getElementById('http-toggle-container').innerHTML = httpToggleHtml;
        }
        
        // Update add HTTP instance button state (disabled when polling service is running)
        const addHttpBtn = document.getElementById('add-http-instance-btn');
        if (addHttpBtn) {
            addHttpBtn.disabled = isRunning;
        }
    } catch (error) {
        console.error('Failed to load polling status:', error);
    }
}

async function togglePolling(enable) {
    // Mark as recently toggled to prevent auto-refresh from re-rendering
    recentlyToggledSwitches.add('polling');
    
    // Optimistic UI update - toggle will stay in new position unless there's an error
    try {
        const action = enable ? 'start' : 'stop';
        const response = await fetch(`/api/polling/control/${action}`, {
            method: 'POST'
        });
        const result = await response.json();

        if (result.success) {
            // Success - just update state tracking, UI already reflects the change
            pollingRunning = enable;
            // Show warning if MQTT is not connected but service is enabled
            if (result.warning) {
                alert('Note: ' + result.warning + '\n\nHTTP Plugins will begin polling when MQTT connects.');
            }
            // Update add button state without full reload
            const addHttpBtn = document.getElementById('add-http-instance-btn');
            if (addHttpBtn) {
                addHttpBtn.disabled = enable;
            }
            // Clear debounce flag and refresh after waiting for backend state to settle
            setTimeout(() => {
                recentlyToggledSwitches.delete('polling');
                loadPollingStatus();
            }, TOGGLE_DEBOUNCE_MS);
        } else {
            alert('Failed to ' + action + ' polling: ' + (result.error || 'Unknown error'));
            // Immediately clear debounce on error and reload to revert
            recentlyToggledSwitches.delete('polling');
            loadPollingStatus();
            loadInstances();
        }
    } catch (error) {
        alert('Failed to toggle polling: ' + error.message);
        // Immediately clear debounce on error and reload to revert
        recentlyToggledSwitches.delete('polling');
        loadPollingStatus();
        loadInstances();
    }
}

async function loadConfig() {
    try {
        const statusResponse = await fetch('/api/status');
        const statusData = await statusResponse.json();
        const mqttRunning = statusData.mqttEnabled; // Use MQTT enabled setting
        
        const response = await fetch('/api/config');
        const data = await response.json();
        const editDisabled = mqttRunning ? 'disabled' : '';
        
        // Fix: Check both mqttEnabled AND mqttConnected for health indicator
        const mqttConnected = statusData.mqttEnabled && statusData.mqttConnected;
        const mqttClass = mqttConnected ? 'mqtt-connected' : 'mqtt-disconnected';
        const html = `
            <div class="mqtt-config-item ${mqttClass}">
                <div class="mqtt-config-field">${buildEditableField('mqtt', 'broker', 'MQTT Broker', data.mqttBroker, editDisabled, false)}</div>
                <div class="mqtt-config-field">${buildEditableField('mqtt', 'port', 'MQTT Port', data.mqttPort, editDisabled, false)}</div>
                <div class="mqtt-config-field">${buildEditableField('mqtt', 'topicPrefix', 'Topic Prefix', data.mqttTopicPrefix, editDisabled, false, 'Default: homeassistant (for Home Assistant discovery)')}</div>
                <div class="mqtt-config-field">${buildEditableField('mqtt', 'username', 'MQTT Username', data.mqttUsername, editDisabled, false)}</div>
                <div class="mqtt-config-field">${buildEditableField('mqtt', 'password', 'MQTT Password', data.mqttPassword, editDisabled, true)}</div>
            </div>
        `;
        document.getElementById('config-info').innerHTML = html;
    } catch (error) {
        document.getElementById('config-info').innerHTML = 
            '<div style="color: #f44336;">Failed to load configuration</div>';
    }
}

async function loadInstances() {
    try {
        const [instancesResponse, pollingInstancesResponse, statusResponse] = await Promise.all([
            fetch('/api/instances'),
            fetch('/api/polling/instances'),
            fetch('/api/status')
        ]);
        const instances = await instancesResponse.json();
        const pollingInstances = await pollingInstancesResponse.json();
        const statusData = await statusResponse.json();

        // Merge regular and polling instances
        const allInstances = [
            ...instances,
            ...pollingInstances.map(p => ({
                instanceId: p.instanceId,
                pluginType: p.pluginId,
                deviceMac: '',  // Polling plugins don't have MAC
                displayName: p.displayName,
                config: p.config || {},  // Include config for display
                tokenExpiresAtMs: p.tokenExpiresAtMs,
                isPolling: true
            }))
        ];
        
        // Update service running states
        serviceRunning = statusData.running || false; // legacy/main
        // bleEnabled already tracked in loadStatus(); ensure fallback here
        if (typeof statusData.bleEnabled !== 'undefined') {
            bleEnabled = statusData.bleEnabled;
        }
        
        // Get plugin statuses (check both BLE and polling status maps)
        const pluginStatuses = {};
        for (const instance of allInstances) {
            const status = statusData.pluginStatuses?.[instance.instanceId] ||
                           statusData.pluginStatuses?.[instance.pluginType] ||
                           statusData.pollingPluginStatuses?.[instance.instanceId] ||
                           statusData.pollingPluginStatuses?.[instance.pluginType] || {};
            pluginStatuses[instance.instanceId] = status;
        }

        // Separate BLE and HTTP instances
        const bleInstances = allInstances.filter(i => isBlePlugin(i.pluginType));
        const httpInstances = allInstances.filter(i => isHttpPlugin(i.pluginType));
        
        // Group instances by plugin type within each category
        const groupInstances = (instList) => {
            const grouped = {};
            for (const instance of instList) {
                if (!grouped[instance.pluginType]) {
                    grouped[instance.pluginType] = [];
                }
                grouped[instance.pluginType].push(instance);
            }
            return grouped;
        };
        
        const bleGrouped = groupInstances(bleInstances);
        const httpGrouped = groupInstances(httpInstances);
        
        // Render BLE plugins section (buttons disabled when bleEnabled)
        const bleHtml = renderPluginSection(bleGrouped, pluginStatuses, /*isBleSection=*/true);
        document.getElementById('ble-plugin-status').innerHTML = bleHtml;
        
        // Render HTTP plugins section (buttons disabled when pollingRunning)
        const httpHtml = renderPluginSection(httpGrouped, pluginStatuses, /*isBleSection=*/false);
        document.getElementById('http-plugin-status').innerHTML = httpHtml;
        
        // Setup drag-and-drop for plugin sections
        setupDragAndDrop();
    } catch (error) {
        console.error('Failed to load instances:', error);
        document.getElementById('ble-plugin-status').innerHTML = 
            '<div style="color: #f44336;">Failed to load BLE plugins. Ensure the app and web service are running.</div>';
        document.getElementById('http-plugin-status').innerHTML = 
            '<div style="color: #f44336;">Failed to load HTTP plugins. Ensure the app and web service are running.</div>';
    }
}

function renderPluginSection(grouped, pluginStatuses, isBleSection) {
    if (Object.keys(grouped).length === 0) {
        return '<div style="padding: 20px; text-align: center; color: #666;">No plugins configured.</div>';
    }
    
    let html = '';
    
    // Get saved plugin order from localStorage (or use default alphabetical)
    const savedOrder = JSON.parse(localStorage.getItem('pluginOrder') || '[]');
    const pluginTypes = Object.keys(grouped);
    
    // Sort by saved order, then alphabetically for new plugins
    const sortedTypes = pluginTypes.sort((a, b) => {
        const aIndex = savedOrder.indexOf(a);
        const bIndex = savedOrder.indexOf(b);
        if (aIndex !== -1 && bIndex !== -1) return aIndex - bIndex;
        if (aIndex !== -1) return -1;
        if (bIndex !== -1) return 1;
        return a.localeCompare(b);
    });
    
    // Render each plugin type section
    for (const pluginType of sortedTypes) {
        const typeInstances = grouped[pluginType];
        const typeName = PLUGIN_TYPE_NAMES[pluginType] || pluginType;
        
        html += `
            <div class="plugin-type-section" draggable="true" data-plugin-type="${pluginType}">
                <div class="plugin-type-header">
                    <span class="plugin-type-title">⋮⋮ ${typeName}</span>
                </div>
        `;
        
        // Render each instance
        for (const instance of typeInstances) {
            const status = pluginStatuses[instance.instanceId] || {};
            const connected = status.connected || false;
            const authenticated = status.authenticated || false;
            const dataHealthy = status.dataHealthy || false;
            
            // Passive plugins (mopeka, gopower, blescanner) only need dataHealthy to be green
            // Active plugins (onecontrol, easytouch) need full connection
            // HTTP plugins (peplink) need all three flags
            const isPassive = pluginType === 'mopeka' || pluginType === 'gopower' || pluginType === 'blescanner' || pluginType === 'hughes_watchdog' || pluginType === 'hughes_gen2';
            const isHttpPlugin = pluginType === 'peplink';
            
            // Use correct service state per section
            const sectionActive = isBleSection ? bleEnabled : pollingRunning;
            // If corresponding service is stopped, all instances are unhealthy
            let healthy = false;
            if (sectionActive) {
                if (isPassive) {
                    healthy = dataHealthy;
                } else if (isHttpPlugin) {
                    healthy = connected && authenticated && dataHealthy;
                } else {
                    healthy = connected && authenticated && dataHealthy;
                }
            }
            
            const healthClass = healthy ? 'instance-healthy' : 'instance-unhealthy';
            const displayName = instance.displayName || instance.instanceId;
            
            // Get plugin-specific config
            let configDetails = '';
            if (pluginType === 'onecontrol') {
                const pin = instance.config?.gateway_pin || 'Not set';
                configDetails = `<div class="instance-detail-line">PIN: ${pin}</div>`;
            } else if (pluginType === 'easytouch') {
                const hasPassword = instance.config?.password ? 'Set' : 'Not set';
                configDetails = `<div class="instance-detail-line">Password: ${hasPassword}</div>`;
            } else if (pluginType === 'hughes_watchdog') {
                const expectedName = instance.config?.expected_name || 'Any';
                const forceVersion = instance.config?.force_version || 'Auto';
                configDetails = `<div class="instance-detail-line">Name: ${expectedName} | Gen: ${forceVersion}</div>`;
            } else if (pluginType === 'hughes_gen2') {
                const expectedName = instance.config?.expected_name || 'Any';
                configDetails = `<div class="instance-detail-line">Name: ${expectedName}</div>`;
            } else if (pluginType === 'mopeka') {
                const mediumType = instance.config?.medium_type || 'propane';
                const tankType = instance.config?.tank_type || '20lb_v';
                configDetails = `<div class="instance-detail-line">Medium: ${mediumType} | Tank: ${tankType}</div>`;
            } else if (pluginType === 'peplink') {
                const baseUrl = instance.config?.base_url || 'Not set';
                const statusPoll = instance.config?.status_poll_interval || '10';
                const usagePoll = instance.config?.usage_poll_interval || '60';
                const diagPoll = instance.config?.diagnostics_poll_interval || '30';
                const vpnPoll = instance.config?.vpn_poll_interval || '0';
                const gpsPoll = instance.config?.gps_poll_interval || '0';
                
                configDetails = `<div class="instance-detail-line">URL: ${baseUrl}</div>`;
                configDetails += `<div class="instance-detail-line">Polling: Status=${statusPoll}s, Usage=${usagePoll}s, Diag=${diagPoll}s, VPN=${vpnPoll}s, GPS=${gpsPoll}s</div>`;
            }

            const tokenAuthEnabled = pluginType === 'peplink' && instance.config?.auth_mode === 'token';
            const tokenExpiresAtMs = instance.tokenExpiresAtMs;
            const tokenRenewLine = tokenAuthEnabled && tokenExpiresAtMs
                ? `<div class="instance-detail-line">Token renews on: ${new Date(tokenExpiresAtMs).toLocaleString()}</div>`
                : '';

            const buttonsDisabled = isBleSection ? bleEnabled : pollingRunning;
            html += `
                <div class="instance-card ${healthClass}">
                    <div class="instance-header">
                        <div>
                            <span class="instance-name">${displayName}</span>
                        </div>
                        <div class="instance-actions">
                            <button class="icon-btn edit-icon-btn" onclick="showEditInstanceDialog('${instance.instanceId}')" ${buttonsDisabled ? 'disabled' : ''} title="Edit">
                                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path>
                                    <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path>
                                </svg>
                            </button>
                            <button class="icon-btn delete-icon-btn" onclick="showRemoveInstanceDialog('${instance.instanceId}', '${displayName}', ${instance.isPolling || false})" ${buttonsDisabled ? 'disabled' : ''} title="Remove">
                                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                    <polyline points="3 6 5 6 21 6"></polyline>
                                    <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
                                    <line x1="10" y1="11" x2="10" y2="17"></line>
                                    <line x1="14" y1="11" x2="14" y2="17"></line>
                                </svg>
                            </button>
                        </div>
                    </div>
                    <div class="instance-details">
                        <div class="instance-detail-line">ID: ${instance.instanceId}</div>
                        ${pluginType !== 'blescanner' && pluginType !== 'peplink' ? `<div class="instance-detail-line">MAC: ${instance.deviceMac}</div>` : ''}
                        ${configDetails}
                        ${(!isPassive) ? `<div class="instance-detail-line">
                            Connected: <span class="${connected ? 'plugin-healthy' : 'plugin-unhealthy'}">${connected ? 'Yes' : 'No'}</span>
                            | Authenticated: <span class="${authenticated ? 'plugin-healthy' : 'plugin-unhealthy'}">${authenticated ? 'Yes' : 'No'}</span>
                            | Data: <span class="${dataHealthy ? 'plugin-healthy' : 'plugin-unhealthy'}">${dataHealthy ? 'Healthy' : 'Unhealthy'}</span>
                        </div>` : ''}
                        ${tokenRenewLine}
                    </div>
                </div>
            `;
        }
        
        html += '</div>';
    }
    
    return html;
}

function setupDragAndDrop() {
    const sections = document.querySelectorAll('.plugin-type-section');
    let draggedElement = null;
    let touchIdentifier = null;
    let touchStartY = 0;
    
    // Mouse drag events
    sections.forEach(section => {
        section.addEventListener('dragstart', (e) => {
            draggedElement = section;
            section.classList.add('dragging');
            e.dataTransfer.effectAllowed = 'move';
        });
        
        section.addEventListener('dragend', (e) => {
            section.classList.remove('dragging');
            sections.forEach(s => s.classList.remove('drag-over'));
            
            // Save new order to localStorage
            const newOrder = Array.from(document.querySelectorAll('.plugin-type-section'))
                .map(s => s.dataset.pluginType);
            localStorage.setItem('pluginOrder', JSON.stringify(newOrder));
        });
        
        section.addEventListener('dragover', (e) => {
            e.preventDefault();
            e.dataTransfer.dropEffect = 'move';
            
            if (draggedElement && draggedElement !== section) {
                section.classList.add('drag-over');
                
                const container = section.parentNode;
                const afterElement = getDragAfterElement(container, e.clientY);
                
                if (afterElement == null) {
                    container.appendChild(draggedElement);
                } else {
                    container.insertBefore(draggedElement, afterElement);
                }
            }
        });
        
        section.addEventListener('dragleave', (e) => {
            section.classList.remove('drag-over');
        });
        
        section.addEventListener('drop', (e) => {
            e.preventDefault();
            section.classList.remove('drag-over');
        });
        
        // Touch drag events for mobile
        section.addEventListener('touchstart', (e) => {
            draggedElement = section;
            touchIdentifier = e.touches[0].identifier;
            touchStartY = e.touches[0].clientY;
            section.classList.add('dragging');
        }, false);
        
        section.addEventListener('touchmove', (e) => {
            if (!draggedElement || draggedElement !== section) return;
            
            const touch = Array.from(e.touches).find(t => t.identifier === touchIdentifier);
            if (!touch) return;
            
            e.preventDefault();
            
            const container = section.parentNode;
            const afterElement = getDragAfterElement(container, touch.clientY);
            
            if (afterElement == null) {
                container.appendChild(draggedElement);
            } else {
                container.insertBefore(draggedElement, afterElement);
            }
        }, false);
        
        section.addEventListener('touchend', (e) => {
            if (draggedElement === section) {
                section.classList.remove('dragging');
                sections.forEach(s => s.classList.remove('drag-over'));
                
                // Save new order to localStorage
                const newOrder = Array.from(document.querySelectorAll('.plugin-type-section'))
                    .map(s => s.dataset.pluginType);
                localStorage.setItem('pluginOrder', JSON.stringify(newOrder));
                
                draggedElement = null;
                touchIdentifier = null;
            }
        }, false);
    });
}

function getDragAfterElement(container, y) {
    const draggableElements = [...container.querySelectorAll('.plugin-type-section:not(.dragging)')];
    
    return draggableElements.reduce((closest, child) => {
        const box = child.getBoundingClientRect();
        const offset = y - box.top - box.height / 2;
        
        // Return the element closest to the cursor position (both above and below)
        if (closest.offset === null) {
            return { offset: offset, element: child };
        }
        
        if (Math.abs(offset) < Math.abs(closest.offset)) {
            return { offset: offset, element: child };
        } else {
            return closest;
        }
    }, { offset: null, element: null }).element;
}

async function showAddInstanceDialog(serviceType = '') {
    // Reset form
    document.getElementById('new-plugin-type').value = '';
    document.getElementById('new-display-name').value = '';
    setMacAddress('new-mac', ''); // Clear MAC fields
    document.getElementById('multi-instance-warning').style.display = 'none';
    updatePluginSpecificFields();
    
    // Set up MAC field handlers
    setupMacFieldHandlers('new-mac');
    
    // Fetch current instances to check which plugin types exist
    try {
        const response = await fetch('/api/instances');
        const instances = await response.json();
        const existingTypes = [...new Set(instances.map(i => i.pluginType))];
        
        // Update select options based on service type and existing instances
        const select = document.getElementById('new-plugin-type');
        for (const option of select.options) {
            if (option.value) {
                // Filter options by service type
                let shouldShow = true;
                if (serviceType === 'ble') {
                    shouldShow = isBlePlugin(option.value);
                } else if (serviceType === 'http') {
                    shouldShow = isHttpPlugin(option.value);
                }
                
                option.style.display = shouldShow ? '' : 'none';
                
                if (shouldShow) {
                    const isMulti = option.getAttribute('data-multi') === 'true';
                    const alreadyExists = existingTypes.includes(option.value);
                    
                    // Disable if it already exists and doesn't support multiple instances
                    if (alreadyExists && !isMulti) {
                        option.disabled = true;
                        option.text = option.text.replace(' - Supports multiple', '') + ' (Already configured)';
                    } else {
                        option.disabled = false;
                    }
                }
            }
        }
    } catch (error) {
        console.error('Failed to load instances:', error);
    }
    
    document.getElementById('addInstanceModal').classList.add('show');
}

function closeAddInstanceDialog() {
    document.getElementById('addInstanceModal').classList.remove('show');
}

async function confirmAddInstance() {
    const pluginType = document.getElementById('new-plugin-type').value;
    const displayName = document.getElementById('new-display-name').value.trim();
    const deviceMac = getMacAddress('new-mac').toUpperCase();
    
    if (!pluginType) {
        alert('Please select a plugin type');
        return;
    }
    
    // Check if this plugin type already exists and doesn't support multiple instances
    const response = await fetch('/api/instances');
    const instances = await response.json();
    const existingTypes = instances.map(i => i.pluginType);
    const supportsMultiple = MULTI_INSTANCE_PLUGINS.includes(pluginType);
    
    if (existingTypes.includes(pluginType) && !supportsMultiple) {
        alert('This plugin type already exists and does not support multiple instances. Please edit the existing instance instead.');
        return;
    }

    if (!displayName) {
        alert('Please enter a display name');
        return;
    }
    // Skip MAC validation for non-BLE plugins (blescanner, peplink)
    if (!deviceMac && pluginType !== 'blescanner' && pluginType !== 'peplink') {
        alert('Please enter a device MAC address');
        return;
    }

    // Collect plugin-specific config
    const config = {};
    if (pluginType === 'onecontrol') {
        const pin = document.getElementById('new-gateway-pin')?.value.trim();
        if (pin) config.gateway_pin = pin;
    } else if (pluginType === 'easytouch') {
        const password = document.getElementById('new-password')?.value.trim();
        if (password) config.password = password;
    } else if (pluginType === 'hughes_watchdog') {
        const expectedName = document.getElementById('new-expected-name')?.value.trim();
        const forceVersion = document.getElementById('new-force-version')?.value;
        if (expectedName) config.expected_name = expectedName;
        if (forceVersion && forceVersion !== 'auto') config.force_version = forceVersion;
    } else if (pluginType === 'hughes_gen2') {
        const expectedName = document.getElementById('new-expected-name')?.value.trim();
        if (expectedName) config.expected_name = expectedName;
    } else if (pluginType === 'mopeka') {
        const mediumType = document.getElementById('new-medium-type')?.value || 'propane';
        const tankType = document.getElementById('new-tank-type')?.value || '20lb_v';
        config.medium_type = mediumType;
        config.tank_type = tankType;
    } else if (pluginType === 'peplink') {
        // Peplink is a polling plugin - collect REST API config
        let baseUrl = document.getElementById('new-base-url')?.value.trim();
        const authMode = document.getElementById('new-auth-mode')?.value || 'userpass';
        const username = document.getElementById('new-username')?.value.trim();
        const password = document.getElementById('new-password')?.value.trim();
        const clientId = document.getElementById('new-client-id')?.value.trim();
        const clientSecret = document.getElementById('new-client-secret')?.value.trim();
        const statusPollInterval = document.getElementById('new-status-poll-interval')?.value.trim();
        const usagePollInterval = document.getElementById('new-usage-poll-interval')?.value.trim();
        const diagnosticsPollInterval = document.getElementById('new-diagnostics-poll-interval')?.value.trim();
        const vpnPollInterval = document.getElementById('new-vpn-poll-interval')?.value.trim();
        const gpsPollInterval = document.getElementById('new-gps-poll-interval')?.value.trim();

        if (!baseUrl) {
            alert('Please enter a router URL');
            return;
        }

        if (authMode === 'token') {
            if (!clientId || !clientSecret) {
                alert('Please fill in all required Peplink fields (Client ID, Client Secret)');
                return;
            }
        } else if (!username || !password) {
            alert('Please fill in all required Peplink fields (Username, Password)');
            return;
        }

        // Ensure URL has protocol scheme
        if (!baseUrl.startsWith('http://') && !baseUrl.startsWith('https://')) {
            baseUrl = 'http://' + baseUrl;
        }

        config.base_url = baseUrl;
        config.auth_mode = authMode;
        if (authMode === 'token') {
            config.client_id = clientId;
            config.client_secret = clientSecret;
        } else {
            config.username = username;
            config.password = password;
        }
        
        // Add polling configuration
        if (statusPollInterval) config.status_poll_interval = statusPollInterval;
        if (usagePollInterval) config.usage_poll_interval = usagePollInterval;
        if (diagnosticsPollInterval) config.diagnostics_poll_interval = diagnosticsPollInterval;
        if (vpnPollInterval) config.vpn_poll_interval = vpnPollInterval;
        if (gpsPollInterval) config.gps_poll_interval = gpsPollInterval;

        // Use polling plugin API instead of regular instance API
        try {
            const response = await fetch('/api/polling/instances/add', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    pluginType: pluginType,
                    instanceName: displayName.toLowerCase().replace(/\s+/g, '_'),
                    displayName: displayName,
                    config: config
                })
            });

            const result = await response.json();
            if (result.success) {
                closeAddInstanceDialog();
                loadStatus();
            } else {
                alert('Failed to add Peplink router: ' + (result.error || 'Unknown error'));
            }
        } catch (error) {
            console.error('Error adding Peplink instance:', error);
            alert('Failed to add Peplink router: ' + error.message);
        }
        return;  // Early return for polling plugins
    }

    try {
        const response = await fetch('/api/instances/add', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                pluginType: pluginType,
                displayName: displayName,
                deviceMac: deviceMac || '',
                config: config
            })
        });
        const result = await response.json();
        if (result.success) {
            closeAddInstanceDialog();
            loadInstances();
        } else {
            alert('Failed to add instance: ' + (result.error || 'Unknown error'));
        }
    } catch (error) {
        alert('Error adding instance: ' + error.message);
    }
}

async function showEditInstanceDialog(instanceId) {
    try {
        // Fetch both BLE and polling instances
        const [instancesResponse, pollingInstancesResponse] = await Promise.all([
            fetch('/api/instances'),
            fetch('/api/polling/instances')
        ]);
        const instances = await instancesResponse.json();
        const pollingInstances = await pollingInstancesResponse.json();

        // Merge regular and polling instances
        const allInstances = [
            ...instances,
            ...pollingInstances.map(p => ({
                instanceId: p.instanceId,
                pluginType: p.pluginId,
                deviceMac: '',
                displayName: p.displayName,
                config: p.config || {},
                isPolling: true
            }))
        ];

        const instance = allInstances.find(i => i.instanceId === instanceId);
        
        if (!instance) {
            alert('Instance not found');
            return;
        }
        
        document.getElementById('edit-instance-id').value = instanceId;
        document.getElementById('edit-plugin-type').value = instance.pluginType;
        document.getElementById('edit-is-polling').value = instance.isPolling ? 'true' : 'false';
        document.getElementById('edit-display-name').value = instance.displayName || '';
        setMacAddress('edit-mac', instance.deviceMac || ''); // Set MAC fields
        
        // Set up MAC field handlers
        setupMacFieldHandlers('edit-mac');
        
        // Hide MAC field for BLE Scanner and polling plugins
        const macContainer = document.getElementById('edit-mac-container');
        if (macContainer) {
            macContainer.style.display = (instance.pluginType === 'blescanner' || instance.isPolling) ? 'none' : 'block';
        }
        
        // Populate plugin-specific fields
        updateEditPluginSpecificFields(instance.pluginType, instance.config);
        
        document.getElementById('editInstanceModal').classList.add('show');
    } catch (error) {
        alert('Error loading instance: ' + error.message);
    }
}

function closeEditInstanceDialog() {
    document.getElementById('editInstanceModal').classList.remove('show');
}

async function confirmEditInstance() {
    const instanceId = document.getElementById('edit-instance-id').value;
    const pluginType = document.getElementById('edit-plugin-type').value;
    const displayName = document.getElementById('edit-display-name').value.trim();
    const deviceMac = getMacAddress('edit-mac').toUpperCase();
    
    if (!displayName) {
        alert('Please enter a display name');
        return;
    }
    const isPolling = document.getElementById('edit-is-polling')?.value === 'true';

    // Skip MAC validation for non-BLE plugins (blescanner, polling)
    if (!deviceMac && pluginType !== 'blescanner' && !isPolling) {
        alert('Please enter a device MAC address');
        return;
    }

    // Collect plugin-specific config
    const config = {};
    const pinField = document.getElementById('edit-gateway-pin');
    const passwordField = document.getElementById('edit-password');
    const expectedNameField = document.getElementById('edit-expected-name');
    const forceVersionField = document.getElementById('edit-force-version');
    const mediumTypeField = document.getElementById('edit-medium-type');
    const tankTypeField = document.getElementById('edit-tank-type');
    
    // Peplink-specific fields
    const baseUrlField = document.getElementById('edit-base-url');
    const usernameField = document.getElementById('edit-username');
    const peplinkPasswordField = document.getElementById('edit-peplink-password');
    const statusPollIntervalField = document.getElementById('edit-status-poll-interval');
    const usagePollIntervalField = document.getElementById('edit-usage-poll-interval');
    const diagnosticsPollIntervalField = document.getElementById('edit-diagnostics-poll-interval');
    const vpnPollIntervalField = document.getElementById('edit-vpn-poll-interval');
    const gpsPollIntervalField = document.getElementById('edit-gps-poll-interval');
    const authModeField = document.getElementById('edit-auth-mode');
    const clientIdField = document.getElementById('edit-client-id');
    const clientSecretField = document.getElementById('edit-client-secret');
    
    if (pinField) {
        const pin = pinField.value.trim();
        if (pin) config.gateway_pin = pin;
    }
    if (passwordField) {
        const password = passwordField.value.trim();
        if (password) config.password = password;
    }
    if (expectedNameField) {
        const expectedName = expectedNameField.value.trim();
        if (expectedName) config.expected_name = expectedName;
    }
    if (forceVersionField) {
        const forceVersion = forceVersionField.value;
        if (forceVersion && forceVersion !== 'auto') config.force_version = forceVersion;
    }
    if (mediumTypeField) {
        config.medium_type = mediumTypeField.value || 'propane';
    }
    if (tankTypeField) {
        config.tank_type = tankTypeField.value || '20lb_v';
    }
    
    // Peplink config
    if (baseUrlField) {
        const baseUrl = baseUrlField.value.trim();
        if (!baseUrl) {
            alert('Please enter a router URL');
            return;
        }
        config.base_url = baseUrl;
    }
    if (authModeField) {
        const authMode = authModeField.value || 'userpass';
        config.auth_mode = authMode;

        if (authMode === 'token') {
            const clientId = clientIdField?.value.trim() || '';
            const clientSecret = clientSecretField?.value.trim() || '';
            if (!clientId || !clientSecret) {
                alert('Please enter client ID and client secret');
                return;
            }
            config.client_id = clientId;
            config.client_secret = clientSecret;
        } else {
            const username = usernameField?.value.trim() || '';
            const password = peplinkPasswordField?.value.trim() || '';
            if (!username) {
                alert('Please enter an admin username');
                return;
            }
            if (!password) {
                alert('Please enter an admin password');
                return;
            }
            config.username = username;
            config.password = password;
        }
    }
    if (statusPollIntervalField) {
        config.status_poll_interval = statusPollIntervalField.value.trim();
    }
    if (usagePollIntervalField) {
        config.usage_poll_interval = usagePollIntervalField.value.trim();
    }
    if (diagnosticsPollIntervalField) {
        config.diagnostics_poll_interval = diagnosticsPollIntervalField.value.trim();
    }
    if (vpnPollIntervalField) {
        config.vpn_poll_interval = vpnPollIntervalField.value.trim();
    }
    if (gpsPollIntervalField) {
        config.gps_poll_interval = gpsPollIntervalField.value.trim();
    }
    
    try {
        const endpoint = isPolling ? '/api/polling/instances/update' : '/api/instances/update';
        const response = await fetch(endpoint, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                instanceId: instanceId,
                displayName: displayName,
                deviceMac: deviceMac,
                config: config
            })
        });
        const result = await response.json();
        if (result.success) {
            closeEditInstanceDialog();
            loadInstances();
        } else {
            alert('Failed to update instance: ' + (result.error || 'Unknown error'));
        }
    } catch (error) {
        alert('Error updating instance: ' + error.message);
    }
}

function showRemoveInstanceDialog(instanceId, displayName, isPolling = false) {
    instanceToRemove = instanceId;
    instanceToRemoveIsPolling = isPolling;
    document.getElementById('remove-message').textContent =
        `Are you sure you want to remove "${displayName}"?`;
    document.getElementById('confirmRemoveModal').classList.add('show');
}

function closeRemoveDialog() {
    instanceToRemove = null;
    instanceToRemoveIsPolling = false;
    document.getElementById('confirmRemoveModal').classList.remove('show');
}

async function confirmRemove() {
    if (!instanceToRemove) return;

    try {
        // Use different endpoint based on plugin type
        const endpoint = instanceToRemoveIsPolling ? '/api/polling/instances/remove' : '/api/instances/remove';

        const response = await fetch(endpoint, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ instanceId: instanceToRemove })
        });
        const result = await response.json();
        if (result.success) {
            closeRemoveDialog();
            loadInstances();
        } else {
            alert('Failed to remove instance: ' + (result.error || 'Unknown error'));
        }
    } catch (error) {
        alert('Error removing instance: ' + error.message);
    }
}

// Update plugin-specific fields in add dialog
document.getElementById('new-plugin-type')?.addEventListener('change', updatePluginSpecificFields);

function updatePluginSpecificFields() {
    const pluginType = document.getElementById('new-plugin-type').value;
    const container = document.getElementById('plugin-specific-fields');
    const macContainer = document.getElementById('new-mac-container');
    
    if (!container) return;
    
    // Hide MAC address field for BLE Scanner
    if (macContainer) {
        macContainer.style.display = (pluginType === 'blescanner') ? 'none' : 'block';
    }
    
    if (pluginType === 'onecontrol') {
        container.innerHTML = `
            <div style="margin-bottom: 15px;">
                <label style="display: block; margin-bottom: 5px; font-weight: 500;">Gateway PIN:</label>
                <input type="text" id="new-gateway-pin" placeholder="e.g., 1234" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
            </div>
        `;
    } else if (pluginType === 'easytouch') {
        container.innerHTML = `
            <div style="margin-bottom: 15px;">
                <label style="display: block; margin-bottom: 5px; font-weight: 500;">Password:</label>
                <input type="password" id="new-password" placeholder="Device password" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
            </div>
        `;
    } else if (pluginType === 'hughes_watchdog') {
        container.innerHTML = `
            <div style="margin-bottom: 15px;">
                <label style="display: block; margin-bottom: 5px; font-weight: 500;">Expected Device Name (optional):</label>
                <input type="text" id="new-expected-name" placeholder="e.g., PWS123 (leave empty for any)" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
            </div>
            <div style="margin-bottom: 15px;">
                <label style="display: block; margin-bottom: 5px; font-weight: 500;">Force Generation (optional):</label>
                <select id="new-force-version" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                    <option value="auto">Auto-detect</option>
                    <option value="gen1">Gen 1 (E2)</option>
                    <option value="gen2">Gen 2+ (E3/E4)</option>
                </select>
            </div>
        `;
    } else if (pluginType === 'hughes_gen2') {
        container.innerHTML = `
            <div style="margin-bottom: 15px;">
                <label style="display: block; margin-bottom: 5px; font-weight: 500;">Expected Device Name (optional):</label>
                <input type="text" id="new-expected-name" placeholder="e.g., WD_E8_12345 (leave empty for any)" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                <div style="margin-top: 4px; font-size: 12px; color: #666;">Gen2 devices advertise as WD_{type}_{serial} (types: E5-E9, V5-V9)</div>
            </div>
        `;
    } else if (pluginType === 'mopeka') {
        container.innerHTML = `
            <div style="margin-bottom: 15px;">
                <label style="display: block; margin-bottom: 5px; font-weight: 500;">Medium Type:</label>
                <select id="new-medium-type" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                    <option value="propane">Propane</option>
                    <option value="air">Air (Tank Ratio)</option>
                    <option value="fresh_water">Fresh Water</option>
                    <option value="waste_water">Waste Water</option>
                    <option value="black_water">Black Water</option>
                    <option value="live_well">Live Well</option>
                    <option value="gasoline">Gasoline</option>
                    <option value="diesel">Diesel</option>
                    <option value="lng">LNG</option>
                    <option value="oil">Oil</option>
                    <option value="hydraulic_oil">Hydraulic Oil</option>
                    <option value="custom">Custom</option>
                </select>
            </div>
            <div style="margin-bottom: 15px;">
                <label style="display: block; margin-bottom: 5px; font-weight: 500;">Tank Type:</label>
                <select id="new-tank-type" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                    <optgroup label="Vertical Propane">
                        <option value="20lb_v">20lb Vertical</option>
                        <option value="30lb_v">30lb Vertical</option>
                        <option value="40lb_v">40lb Vertical</option>
                    </optgroup>
                    <optgroup label="Horizontal Propane">
                        <option value="250gal_h">250 Gallon Horizontal</option>
                        <option value="500gal_h">500 Gallon Horizontal</option>
                        <option value="1000gal_h">1000 Gallon Horizontal</option>
                    </optgroup>
                    <optgroup label="European">
                        <option value="europe_6kg">6kg European Vertical</option>
                        <option value="europe_11kg">11kg European Vertical</option>
                        <option value="europe_14kg">14kg European Vertical</option>
                    </optgroup>
                    <option value="custom">Custom Tank</option>
                </select>
            </div>
        `;
    } else if (pluginType === 'peplink') {
        // Peplink is a polling plugin - hide MAC field
        if (macContainer) {
            macContainer.style.display = 'none';
        }
        container.innerHTML = `
            <div style="margin-bottom: 15px;">
                <label style="display: block; margin-bottom: 5px; font-weight: 500;">Router URL: <span style="color: #f44336;">*</span></label>
                <input type="text" id="new-base-url" placeholder="e.g., http://192.168.1.1" required style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                <div style="margin-top: 4px; font-size: 12px; color: #666;">Router's local IP address or hostname</div>
            </div>
            <div style="margin-bottom: 15px;">
                <label style="display: block; margin-bottom: 5px; font-weight: 500;">Auth Mode:</label>
                <select id="new-auth-mode" onchange="togglePeplinkAuthModeFields('new')" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                    <option value="userpass">Username / Password (cookie)</option>
                    <option value="token">Client ID / Secret (token)</option>
                </select>
            </div>
            <div id="new-peplink-userpass-fields">
                <div style="margin-bottom: 15px;">
                    <label style="display: block; margin-bottom: 5px; font-weight: 500;">Username: <span style="color: #f44336;">*</span></label>
                    <input type="text" id="new-username" placeholder="admin" required style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                </div>
                <div style="margin-bottom: 15px;">
                    <label style="display: block; margin-bottom: 5px; font-weight: 500;">Password: <span style="color: #f44336;">*</span></label>
                    <input type="password" id="new-password" placeholder="Admin password" required style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                </div>
            </div>
            <div id="new-peplink-token-fields" style="display:none;">
                <div style="margin-bottom: 15px;">
                    <label style="display: block; margin-bottom: 5px; font-weight: 500;">Client ID: <span style="color: #f44336;">*</span></label>
                    <input type="text" id="new-client-id" placeholder="Client ID" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                </div>
                <div style="margin-bottom: 15px;">
                    <label style="display: block; margin-bottom: 5px; font-weight: 500;">Client Secret: <span style="color: #f44336;">*</span></label>
                    <input type="password" id="new-client-secret" placeholder="Client Secret" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                </div>
            </div>
            <div style="margin-bottom: 15px;">
                <label style="display: block; margin-bottom: 5px; font-weight: 500;">Polling Configuration</label>
                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin-bottom: 10px;">
                    <div>
                        <div style="font-size: 12px; color: #666; margin-bottom: 3px;">Status Poll (sec)</div>
                        <input type="number" id="new-status-poll-interval" value="10" min="5" max="3600" style="width: 100%; padding: 6px; border: 1px solid #ddd; border-radius: 3px; font-size: 12px;">
                        <div style="font-size: 10px; color: #999; margin-top: 2px;">Default: 10</div>
                    </div>
                    <div>
                        <div style="font-size: 12px; color: #666; margin-bottom: 3px;">Usage Poll (sec)</div>
                        <input type="number" id="new-usage-poll-interval" value="60" min="5" max="3600" style="width: 100%; padding: 6px; border: 1px solid #ddd; border-radius: 3px; font-size: 12px;">
                        <div style="font-size: 10px; color: #999; margin-top: 2px;">Default: 60</div>
                    </div>
                    <div>
                        <div style="font-size: 12px; color: #666; margin-bottom: 3px;">Diagnostics Poll (sec)</div>
                        <input type="number" id="new-diagnostics-poll-interval" value="30" min="5" max="3600" style="width: 100%; padding: 6px; border: 1px solid #ddd; border-radius: 3px; font-size: 12px;">
                        <div style="font-size: 10px; color: #999; margin-top: 2px;">Default: 30</div>
                    </div>
                    <div>
                        <div style="font-size: 12px; color: #666; margin-bottom: 3px;">VPN Poll (sec, 0=disabled)</div>
                        <input type="number" id="new-vpn-poll-interval" value="0" min="0" max="3600" style="width: 100%; padding: 6px; border: 1px solid #ddd; border-radius: 3px; font-size: 12px;">
                        <div style="font-size: 10px; color: #999; margin-top: 2px;">Default: 0 (off)</div>
                    </div>
                    <div>
                        <div style="font-size: 12px; color: #666; margin-bottom: 3px;">GPS Poll (sec, 0=disabled)</div>
                        <input type="number" id="new-gps-poll-interval" value="0" min="0" max="3600" style="width: 100%; padding: 6px; border: 1px solid #ddd; border-radius: 3px; font-size: 12px;">
                        <div style="font-size: 10px; color: #999; margin-top: 2px;">Default: 0 (off)</div>
                    </div>
                </div>
            </div>
        `;
        togglePeplinkAuthModeFields('new');
    } else {
        container.innerHTML = '';
    }
}

function updateEditPluginSpecificFields(pluginType, config) {
    const container = document.getElementById('edit-plugin-specific-fields');
    
    if (!container) return;
    
    if (pluginType === 'onecontrol') {
        const pin = config?.gateway_pin || '';
        container.innerHTML = `
            <div style="margin-bottom: 15px;">
                <label style="display: block; margin-bottom: 5px; font-weight: 500;">Gateway PIN:</label>
                <input type="text" id="edit-gateway-pin" value="${pin}" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
            </div>
        `;
    } else if (pluginType === 'easytouch') {
        const password = config?.password || '';
        container.innerHTML = `
            <div style="margin-bottom: 15px;">
                <label style="display: block; margin-bottom: 5px; font-weight: 500;">Password:</label>
                <input type="password" id="edit-password" value="${password}" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
            </div>
        `;
    } else if (pluginType === 'hughes_watchdog') {
        const expectedName = config?.expected_name || '';
        const forceVersion = config?.force_version || 'auto';
        container.innerHTML = `
            <div style="margin-bottom: 15px;">
                <label style="display: block; margin-bottom: 5px; font-weight: 500;">Expected Device Name (optional):</label>
                <input type="text" id="edit-expected-name" value="${expectedName}" placeholder="e.g., PWS123 (leave empty for any)" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
            </div>
            <div style="margin-bottom: 15px;">
                <label style="display: block; margin-bottom: 5px; font-weight: 500;">Force Generation (optional):</label>
                <select id="edit-force-version" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                    <option value="auto" ${forceVersion === 'auto' ? 'selected' : ''}>Auto-detect</option>
                    <option value="gen1" ${forceVersion === 'gen1' ? 'selected' : ''}>Gen 1 (E2)</option>
                    <option value="gen2" ${forceVersion === 'gen2' ? 'selected' : ''}>Gen 2+ (E3/E4)</option>
                </select>
            </div>
        `;
    } else if (pluginType === 'hughes_gen2') {
        const expectedName = config?.expected_name || '';
        container.innerHTML = `
            <div style="margin-bottom: 15px;">
                <label style="display: block; margin-bottom: 5px; font-weight: 500;">Expected Device Name (optional):</label>
                <input type="text" id="edit-expected-name" value="${expectedName}" placeholder="e.g., WD_E8_12345 (leave empty for any)" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                <div style="margin-top: 4px; font-size: 12px; color: #666;">Gen2 devices advertise as WD_{type}_{serial} (types: E5-E9, V5-V9)</div>
            </div>
        `;
    } else if (pluginType === 'mopeka') {
        const mediumType = config?.medium_type || 'propane';
        const tankType = config?.tank_type || '20lb_v';
        container.innerHTML = `
            <div style="margin-bottom: 15px;">
                <label style="display: block; margin-bottom: 5px; font-weight: 500;">Medium Type:</label>
                <select id="edit-medium-type" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                    <option value="propane" ${mediumType === 'propane' ? 'selected' : ''}>Propane</option>
                    <option value="air" ${mediumType === 'air' ? 'selected' : ''}>Air (Tank Ratio)</option>
                    <option value="fresh_water" ${mediumType === 'fresh_water' ? 'selected' : ''}>Fresh Water</option>
                    <option value="waste_water" ${mediumType === 'waste_water' ? 'selected' : ''}>Waste Water</option>
                    <option value="black_water" ${mediumType === 'black_water' ? 'selected' : ''}>Black Water</option>
                    <option value="live_well" ${mediumType === 'live_well' ? 'selected' : ''}>Live Well</option>
                    <option value="gasoline" ${mediumType === 'gasoline' ? 'selected' : ''}>Gasoline</option>
                    <option value="diesel" ${mediumType === 'diesel' ? 'selected' : ''}>Diesel</option>
                    <option value="lng" ${mediumType === 'lng' ? 'selected' : ''}>LNG</option>
                    <option value="oil" ${mediumType === 'oil' ? 'selected' : ''}>Oil</option>
                    <option value="hydraulic_oil" ${mediumType === 'hydraulic_oil' ? 'selected' : ''}>Hydraulic Oil</option>
                    <option value="custom" ${mediumType === 'custom' ? 'selected' : ''}>Custom</option>
                </select>
            </div>
            <div style="margin-bottom: 15px;">
                <label style="display: block; margin-bottom: 5px; font-weight: 500;">Tank Type:</label>
                <select id="edit-tank-type" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                    <optgroup label="Vertical Propane">
                        <option value="20lb_v" ${tankType === '20lb_v' ? 'selected' : ''}>20lb Vertical</option>
                        <option value="30lb_v" ${tankType === '30lb_v' ? 'selected' : ''}>30lb Vertical</option>
                        <option value="40lb_v" ${tankType === '40lb_v' ? 'selected' : ''}>40lb Vertical</option>
                    </optgroup>
                    <optgroup label="Horizontal Propane">
                        <option value="250gal_h" ${tankType === '250gal_h' ? 'selected' : ''}>250 Gallon Horizontal</option>
                        <option value="500gal_h" ${tankType === '500gal_h' ? 'selected' : ''}>500 Gallon Horizontal</option>
                        <option value="1000gal_h" ${tankType === '1000gal_h' ? 'selected' : ''}>1000 Gallon Horizontal</option>
                    </optgroup>
                    <optgroup label="European">
                        <option value="europe_6kg" ${tankType === 'europe_6kg' ? 'selected' : ''}>6kg European Vertical</option>
                        <option value="europe_11kg" ${tankType === 'europe_11kg' ? 'selected' : ''}>11kg European Vertical</option>
                        <option value="europe_14kg" ${tankType === 'europe_14kg' ? 'selected' : ''}>14kg European Vertical</option>
                    </optgroup>
                    <option value="custom" ${tankType === 'custom' ? 'selected' : ''}>Custom Tank</option>
                </select>
            </div>
        `;
    } else if (pluginType === 'peplink') {
        const baseUrl = config?.base_url || '';
        const authMode = config?.auth_mode || 'userpass';
        const username = config?.username || '';
        const password = config?.password || '';
        const clientId = config?.client_id || '';
        const clientSecret = config?.client_secret || '';
        const statusPollInterval = config?.status_poll_interval || '10';
        const usagePollInterval = config?.usage_poll_interval || '60';
        const diagnosticsPollInterval = config?.diagnostics_poll_interval || '30';
        const vpnPollInterval = config?.vpn_poll_interval || '0';
        const gpsPollInterval = config?.gps_poll_interval || '0';
        
        container.innerHTML = `
            <div style="margin-bottom: 15px;">
                <label style="display: block; margin-bottom: 5px; font-weight: 500;">Router URL:</label>
                <input type="text" id="edit-base-url" value="${baseUrl}" placeholder="http://192.168.50.1" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                <div style="font-size: 12px; color: #666; margin-top: 4px;">Router's local IP address or hostname</div>
            </div>
            <div style="margin-bottom: 15px;">
                <label style="display: block; margin-bottom: 5px; font-weight: 500;">Auth Mode:</label>
                <select id="edit-auth-mode" onchange="togglePeplinkAuthModeFields('edit')" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                    <option value="userpass" ${authMode === 'userpass' ? 'selected' : ''}>Username / Password (cookie)</option>
                    <option value="token" ${authMode === 'token' ? 'selected' : ''}>Client ID / Secret (token)</option>
                </select>
            </div>
            <div id="edit-peplink-userpass-fields">
                <div style="margin-bottom: 15px;">
                    <label style="display: block; margin-bottom: 5px; font-weight: 500;">Username:</label>
                    <input type="text" id="edit-username" value="${username}" placeholder="admin" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                </div>
                <div style="margin-bottom: 15px;">
                    <label style="display: block; margin-bottom: 5px; font-weight: 500;">Password:</label>
                    <input type="password" id="edit-peplink-password" value="${password}" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                </div>
            </div>
            <div id="edit-peplink-token-fields" style="display:none;">
                <div style="margin-bottom: 15px;">
                    <label style="display: block; margin-bottom: 5px; font-weight: 500;">Client ID:</label>
                    <input type="text" id="edit-client-id" value="${clientId}" placeholder="Client ID" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                </div>
                <div style="margin-bottom: 15px;">
                    <label style="display: block; margin-bottom: 5px; font-weight: 500;">Client Secret:</label>
                    <input type="password" id="edit-client-secret" value="${clientSecret}" placeholder="Client Secret" style="width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px;">
                </div>
            </div>
            <div style="margin-bottom: 15px;">
                <label style="display: block; margin-bottom: 5px; font-weight: 500;">Polling Configuration</label>
                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin-bottom: 10px;">
                    <div>
                        <div style="font-size: 12px; color: #666; margin-bottom: 3px;">Status Poll (sec)</div>
                        <input type="number" id="edit-status-poll-interval" value="${statusPollInterval}" min="5" max="3600" style="width: 100%; padding: 6px; border: 1px solid #ddd; border-radius: 3px; font-size: 12px;">
                        <div style="font-size: 10px; color: #999; margin-top: 2px;">Default: 10</div>
                    </div>
                    <div>
                        <div style="font-size: 12px; color: #666; margin-bottom: 3px;">Usage Poll (sec)</div>
                        <input type="number" id="edit-usage-poll-interval" value="${usagePollInterval}" min="5" max="3600" style="width: 100%; padding: 6px; border: 1px solid #ddd; border-radius: 3px; font-size: 12px;">
                        <div style="font-size: 10px; color: #999; margin-top: 2px;">Default: 60</div>
                    </div>
                    <div>
                        <div style="font-size: 12px; color: #666; margin-bottom: 3px;">Diagnostics Poll (sec)</div>
                        <input type="number" id="edit-diagnostics-poll-interval" value="${diagnosticsPollInterval}" min="5" max="3600" style="width: 100%; padding: 6px; border: 1px solid #ddd; border-radius: 3px; font-size: 12px;">
                        <div style="font-size: 10px; color: #999; margin-top: 2px;">Default: 30</div>
                    </div>
                    <div>
                        <div style="font-size: 12px; color: #666; margin-bottom: 3px;">VPN Poll (sec, 0=disabled)</div>
                        <input type="number" id="edit-vpn-poll-interval" value="${vpnPollInterval}" min="0" max="3600" style="width: 100%; padding: 6px; border: 1px solid #ddd; border-radius: 3px; font-size: 12px;">
                        <div style="font-size: 10px; color: #999; margin-top: 2px;">Default: 0 (off)</div>
                    </div>
                    <div>
                        <div style="font-size: 12px; color: #666; margin-bottom: 3px;">GPS Poll (sec, 0=disabled)</div>
                        <input type="number" id="edit-gps-poll-interval" value="${gpsPollInterval}" min="0" max="3600" style="width: 100%; padding: 6px; border: 1px solid #ddd; border-radius: 3px; font-size: 12px;">
                        <div style="font-size: 10px; color: #999; margin-top: 2px;">Default: 0 (off)</div>
                    </div>
                </div>
            </div>
        `;
        togglePeplinkAuthModeFields('edit');
    } else {
        container.innerHTML = '';
    }
}

function togglePeplinkAuthModeFields(prefix) {
    const mode = document.getElementById(`${prefix}-auth-mode`)?.value || 'userpass';
    const userPassFields = document.getElementById(`${prefix}-peplink-userpass-fields`);
    const tokenFields = document.getElementById(`${prefix}-peplink-token-fields`);

    if (userPassFields) {
        userPassFields.style.display = mode === 'userpass' ? 'block' : 'none';
    }
    if (tokenFields) {
        tokenFields.style.display = mode === 'token' ? 'block' : 'none';
    }
}

function buildEditableField(pluginId, fieldName, label, value, editDisabled, isSecret, helperText) {
    const fieldId = `${pluginId}_${fieldName}`;
    const displayValue = value || 'None';
    const maskedValue = isSecret && value ? '•'.repeat(value.length) : displayValue;
    const helper = helperText ? `<div style="font-size: 12px; color: #888; margin-top: 0; margin-bottom: 0; line-height: 1;">${helperText}</div>` : '';
    
    return `
        <div class="plugin-config-field">
            ${label}: 
            <span id="${fieldId}_display">${maskedValue}</span>
            <input type="text" id="${fieldId}_input" class="config-input" value="${value}" style="display:none;">
            <button id="${fieldId}_edit" class="icon-btn config-edit-icon-btn" ${editDisabled} onclick="editField('${pluginId}', '${fieldName}', ${isSecret})" title="Edit">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path>
                    <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path>
                </svg>
            </button>
            <button id="${fieldId}_save" class="icon-btn config-save-icon-btn" style="display:none;" onclick="saveField('${pluginId}', '${fieldName}')" title="Save">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <polyline points="20 6 9 17 4 12"></polyline>
                </svg>
            </button>
            ${helper}
        </div>
    `;
}

function editField(pluginId, fieldName, isSecret) {
    const fieldId = `${pluginId}_${fieldName}`;
    editingFields[fieldId] = true;
    document.getElementById(`${fieldId}_display`).style.display = 'none';
    document.getElementById(`${fieldId}_input`).style.display = 'inline-block';
    document.getElementById(`${fieldId}_edit`).style.display = 'none';
    document.getElementById(`${fieldId}_save`).style.display = 'inline-block';
}

async function saveField(pluginId, fieldName) {
    const fieldId = `${pluginId}_${fieldName}`;
    const value = document.getElementById(`${fieldId}_input`).value;
    
    try {
        const endpoint = pluginId === 'mqtt' ? '/api/config/mqtt' : '/api/config/plugin';
        const response = await fetch(endpoint, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ 
                pluginId: pluginId,
                field: fieldName,
                value: value
            })
        });
        const result = await response.json();
        if (result.success) {
            configChanged[pluginId] = true;
            delete editingFields[fieldId];
            if (pluginId === 'mqtt') {
                loadConfig();
            } else {
                loadInstances();
            }
        } else {
            alert('Failed to save: ' + (result.error || 'Unknown error'));
        }
    } catch (error) {
        alert('Error saving configuration: ' + error.message);
    }
}

async function loadDebugLog() {
    const container = document.getElementById('debug-log');
    container.style.display = 'block';
    container.textContent = 'Loading...';
    try {
        const response = await fetch('/api/logs/debug');
        const text = await response.text();
        container.textContent = text;
    } catch (error) {
        container.textContent = 'Failed to load debug log: ' + error.message;
    }
}

async function loadBleTrace() {
    const container = document.getElementById('ble-trace');
    container.style.display = 'block';
    container.textContent = 'Loading...';
    try {
        const response = await fetch('/api/logs/ble');
        const text = await response.text();
        container.textContent = text;
    } catch (error) {
        container.textContent = 'Failed to load BLE trace: ' + error.message;
    }
}

function downloadDebugLog() {
    window.open('/api/logs/debug', '_blank');
}

function downloadBleTrace() {
    window.open('/api/logs/ble', '_blank');
}

// Close modals when clicking outside
window.onclick = function(event) {
    const addModal = document.getElementById('addInstanceModal');
    const editModal = document.getElementById('editInstanceModal');
    const removeModal = document.getElementById('confirmRemoveModal');
    if (event.target === addModal) {
        closeAddInstanceDialog();
    } else if (event.target === editModal) {
        closeEditInstanceDialog();
    } else if (event.target === removeModal) {
        closeRemoveDialog();
    }
}

async function toggleService(enable) {
    // Mark as recently toggled to prevent auto-refresh from re-rendering
    recentlyToggledSwitches.add('ble');
    
    // Optimistic UI update - toggle will stay in new position unless there's an error
    try {
        const response = await fetch('/api/control/service', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ enable: enable })
        });
        const result = await response.json();
        if (!result.success) {
            alert('Failed to ' + (enable ? 'start' : 'stop') + ' service: ' + (result.error || 'Unknown error'));
            // Immediately clear debounce on error and reload to revert
            recentlyToggledSwitches.delete('ble');
            loadStatus();
            loadInstances();
        } else {
            // Success - just update state tracking, UI already reflects the change
            if (enable) {
                configChanged = {};
            }
            bleEnabled = enable;
            // Update add button state without full reload
            const addBleBtnBtn = document.getElementById('add-ble-instance-btn');
            if (addBleBtnBtn) {
                addBleBtnBtn.disabled = enable;
            }
            // Clear debounce flag and refresh after waiting for backend state to settle
            setTimeout(() => {
                recentlyToggledSwitches.delete('ble');
                loadStatus();
            }, TOGGLE_DEBOUNCE_MS);
        }
    } catch (error) {
        alert('Error controlling service: ' + error.message);
        // Immediately clear debounce on error and reload to revert
        recentlyToggledSwitches.delete('ble');
        loadStatus();
        loadInstances();
    }
}

async function toggleMqtt(enable) {
    // Mark as recently toggled to prevent auto-refresh from re-rendering
    recentlyToggledSwitches.add('mqtt');
    
    // Optimistic UI update - toggle will stay in new position unless there's an error
    try {
        const response = await fetch('/api/control/mqtt', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ enable: enable })
        });
        const result = await response.json();
        if (!result.success) {
            alert('Failed to ' + (enable ? 'connect' : 'disconnect') + ' MQTT: ' + (result.error || 'Unknown error'));
            // Immediately clear debounce on error and reload to revert
            recentlyToggledSwitches.delete('mqtt');
            loadStatus();
            loadConfig();
        } else {
            // Success - UI already reflects the change
            // When enabling MQTT, service restarts and needs time to reconnect
            // Wait 2 seconds before refreshing config to allow connection to establish
            if (enable) {
                await new Promise(resolve => setTimeout(resolve, 2000));
            }
            // Clear debounce flag and light refresh of config without re-rendering the toggle
            recentlyToggledSwitches.delete('mqtt');
            loadConfig();
        }
    } catch (error) {
        alert('Error controlling MQTT: ' + error.message);
        // Immediately clear debounce on error and reload to revert
        recentlyToggledSwitches.delete('mqtt');
        loadStatus();
        loadConfig();
    }
}

// Android TV Power Fix section toggle
function toggleTvFixSection() {
    const content = document.getElementById('tv-fix-content');
    const toggle = document.getElementById('tv-fix-toggle');
    if (content.style.display === 'none') {
        content.style.display = 'block';
        toggle.textContent = '▼';
    } else {
        content.style.display = 'none';
        toggle.textContent = '▶';
    }
}

// Copy text to clipboard
function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(() => {
        // Show brief success feedback
        const event = new Event('copied');
        event.text = text;
        window.dispatchEvent(event);
        
        // Simple visual feedback
        const allButtons = document.querySelectorAll('button');
        allButtons.forEach(btn => {
            if (btn.textContent === 'Copy' && btn.onclick && btn.onclick.toString().includes(text.substring(0, 20))) {
                const originalText = btn.textContent;
                btn.textContent = 'Copied!';
                btn.style.background = '#4CAF50';
                setTimeout(() => {
                    btn.textContent = originalText;
                    btn.style.background = '#007acc';
                }, 2000);
            }
        });
    }).catch(err => {
        alert('Failed to copy: ' + err);
    });
}

// Configuration backup/restore functions
async function exportConfig() {
    try {
        const response = await fetch('/api/config/export');
        if (!response.ok) {
            throw new Error('Export failed: ' + response.statusText);
        }
        
        // Get the filename from Content-Disposition header if available
        const contentDisposition = response.headers.get('content-disposition');
        let filename = 'ble-bridge-backup.json';
        if (contentDisposition) {
            const filenamePart = contentDisposition.split('filename=')[1];
            if (filenamePart) {
                filename = filenamePart.replace(/"/g, '');
            }
        }
        
        // Create download link
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
        
        showImportStatus('✅ Configuration exported successfully', 'success');
    } catch (err) {
        console.error('Export error:', err);
        showImportStatus('❌ Export failed: ' + err.message, 'error');
    }
}

function handleFileImport(event) {
    const file = event.target.files[0];
    if (!file) return;
    
    if (!file.name.endsWith('.json')) {
        showImportStatus('❌ Please select a valid JSON backup file', 'error');
        return;
    }
    
    const reader = new FileReader();
    reader.onload = async (e) => {
        try {
            // Validate JSON
            const backup = JSON.parse(e.target.result);
            
            // Show confirmation dialog
            const confirmed = confirm(
                'Import configuration from ' + file.name + '?\n\n' +
                'Version: ' + (backup.appVersion || 'unknown') + '\n' +
                'Exported: ' + (backup.exportedAt || 'unknown') + '\n\n' +
                'This will overwrite your current configuration.\n\n' +
                'Continue?'
            );
            
            if (!confirmed) {
                showImportStatus('Import cancelled', 'info');
                return;
            }
            
            // Send to server for import
            const formData = new FormData();
            formData.append('backup', new Blob([JSON.stringify(backup)], { type: 'application/json' }));
            
            showImportStatus('Importing...', 'info');
            
            const response = await fetch('/api/config/import?replace=true', {
                method: 'POST',
                body: JSON.stringify(backup)
            });
            
            const result = await response.json();
            
            if (result.success) {
                showImportStatus('✅ Configuration imported successfully.\n\nNote: Restart the service to apply changes.', 'success');
                // Clear the file input
                event.target.value = '';
                
                // Reload page after 2 seconds
                setTimeout(() => {
                    location.reload();
                }, 2000);
            } else {
                showImportStatus('❌ Import failed: ' + result.message, 'error');
            }
        } catch (err) {
            console.error('Import error:', err);
            showImportStatus('❌ Invalid backup file or import error: ' + err.message, 'error');
        }
    };
    reader.readAsText(file);
}

function showImportStatus(message, type) {
    const statusDiv = document.getElementById('importStatus');
    statusDiv.style.display = 'block';
    statusDiv.textContent = message;
    
    // Set background color based on type
    if (type === 'success') {
        statusDiv.style.background = '#dff0d8';
        statusDiv.style.color = '#3c763d';
        statusDiv.style.border = '1px solid #d6e9c6';
    } else if (type === 'error') {
        statusDiv.style.background = '#f8d7da';
        statusDiv.style.color = '#721c24';
        statusDiv.style.border = '1px solid #f5c6cb';
    } else {
        statusDiv.style.background = '#d1ecf1';
        statusDiv.style.color = '#0c5460';
        statusDiv.style.border = '1px solid #bee5eb';
    }
    
    // Auto-hide success messages after 5 seconds
    if (type === 'success' || type === 'info') {
        setTimeout(() => {
            statusDiv.style.display = 'none';
        }, 5000);
    }
}
