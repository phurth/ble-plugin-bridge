## v2.5.10: Fix OneControl Connection Issues & TEA Encryption Support

This release fixes critical connection issues in OneControl BLE devices and adds complete TEA encryption support for tank query responses.

### Key Fixes
- Connection Stability: Fixed status 133 connection failures that prevented proper BLE authentication
- Session Management: Added defensive session key handling and cleanup to prevent authentication issues  
- Error Handling: Improved BLE error recovery and logging for better diagnosis

### New Features
- TEA Encryption: Complete implementation of TEA (Tiny Encryption Algorithm) for OneControl encrypted tank query responses
- Tank Query Support: Added framework for handling encrypted tank data (E0-E9 message patterns)
- Multi-Frame Assembly: Enhanced BLE response handling for larger encrypted payloads

### Technical Details
- Implements full OneControl dual protocol support (autonomous broadcasts + encrypted queries)
- TEA decryption uses first 8 bytes of 16-byte session auth key from SEED notification
- Connection diagnostics and defensive error handling prevent authentication flow failures
- Ready for testing with 5-tank encrypted OneControl systems that require query-based communication

### Testing
This release specifically addresses issues where OneControl systems show only 1 tank in Home Assistant instead of all configured tanks. The TEA encryption support enables proper decryption of tank query responses from systems that don't use autonomous broadcast mode.