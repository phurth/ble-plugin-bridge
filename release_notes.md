## 🔑 OneControl TEA Encryption Support

This release adds complete support for OneControl systems that use encrypted tank communication, enabling proper detection of all tanks in 5-tank systems.

### ✨ New Features
- **Session Key Management**: Stores 16-byte authentication key from SEED notification
- **TEA Encryption/Decryption**: Complete implementation with 8-byte key support  
- **Tank Query Response Handler**: Detects and processes E0-E9 encrypted frames
- **Multi-Frame Reconstruction**: Assembles 6-frame responses totaling 187 bytes per tank
- **Dual Protocol Support**: Handles both autonomous broadcasts and encrypted query responses

### 🐛 Fixes
- **5-Tank Detection**: Systems with encrypted communication now show individual tank entities in Home Assistant instead of only 1 tank
- **Encrypted Tank Data**: OneControl gateways using query-based communication are now fully supported

### 🔧 Technical Details
- Added TEA (Tiny Encryption Algorithm) implementation for OneControl protocol
- Session key derived from SEED notification authentication process
- Tank query responses (E0-E9) are now properly decrypted and published to MQTT
- Each tank gets unique Home Assistant entity with query_response communication type

This resolves the issue where some OneControl systems only showed 1 tank in Home Assistant despite having 5 physical tanks connected.