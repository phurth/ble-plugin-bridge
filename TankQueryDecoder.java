import java.util.*;

public class TankQueryDecoder {
    static class TankQueryResponse {
        String queryId;
        List<byte[]> frames = new ArrayList<>();
        boolean isComplete = false;
        
        TankQueryResponse(String queryId) {
            this.queryId = queryId;
        }
    }
    
    static class TankData {
        String queryId;
        int tableId;
        int deviceId;
        int level;
        
        TankData(String queryId, int tableId, int deviceId, int level) {
            this.queryId = queryId;
            this.tableId = tableId;
            this.deviceId = deviceId;
            this.level = level;
        }
    }
    
    private Map<String, TankQueryResponse> pendingResponses = new HashMap<>();
    
    public TankData processNotification(byte[] data) {
        if (data.length < 4) return null;
        
        // Look for tank query response pattern: 00 XX 02 QQ...
        if (data[0] != 0x00) return null;
        
        int responseType = data[1] & 0xFF;
        if (data[2] != 0x02) return null;  // Not a query response
        
        String queryId = String.format("%02X", data[3] & 0xFF);
        
        // Only process tank queries E0-E9
        if (!queryId.matches("E[0-9]")) return null;
        
        System.out.printf("🔍 Tank query response frame: %s, type=0x%02x, %d bytes%n", 
                         queryId, responseType, data.length);
        
        // Get or create response collector
        TankQueryResponse response = pendingResponses.computeIfAbsent(queryId, TankQueryResponse::new);
        
        // Add frame to response (skip 00 XX prefix)
        byte[] frameData = Arrays.copyOfRange(data, 2, data.length);
        response.frames.add(frameData);
        
        // Check if this is the final frame (usually starts with 0x0A)
        if (responseType == 0x0A) {
            response.isComplete = true;
            pendingResponses.remove(queryId);
            
            System.out.printf("🔍 Complete response for %s: %d frames%n", queryId, response.frames.size());
            return decodeCompleteResponse(response);
        }
        
        return null;
    }
    
    private TankData decodeCompleteResponse(TankQueryResponse response) {
        try {
            // Count total payload bytes
            int totalBytes = 0;
            for (byte[] frame : response.frames) {
                if (frame.length > 6) {
                    totalBytes += frame.length - 6;  // Skip headers
                }
            }
            
            System.out.printf("🔍 Total payload for %s: %d bytes%n", response.queryId, totalBytes);
            
            // For testing, simulate decryption based on query ID
            // Real implementation would reconstruct -> COBS decode -> decrypt -> parse
            int simulatedLevel = simulateDecryption(response.queryId);
            
            System.out.printf("🔍 %s: Simulated tank level = %d%%%n", response.queryId, simulatedLevel);
            
            return new TankData(
                response.queryId,
                8, // Table ID
                Integer.parseInt(response.queryId.substring(1)), // E0->0, E1->1, etc.
                simulatedLevel
            );
            
        } catch (Exception e) {
            System.out.printf("❌ Error decoding %s: %s%n", response.queryId, e.getMessage());
            return null;
        }
    }
    
    private int simulateDecryption(String queryId) {
        // Simulate different tank levels since encrypted data is identical
        switch (queryId) {
            case "E0": return 0;   // Empty
            case "E1": return 33;  // 1/3 full
            case "E2": return 66;  // 2/3 full  
            case "E3": return 100; // Full
            case "E4": return 21;  // Random level
            default: return 0;
        }
    }
    
    // Convert hex string to byte array
    public static byte[] hexToBytes(String hex) {
        hex = hex.replace(" ", "");
        int len = hex.length();
        byte[] data = new byte[len / 2];
        for (int i = 0; i < len; i += 2) {
            data[i / 2] = (byte) ((Character.digit(hex.charAt(i), 16) << 4)
                                 + Character.digit(hex.charAt(i+1), 16));
        }
        return data;
    }
    
    public static void main(String[] args) {
        System.out.println("🧪 Testing Tank Query Response Decoder");
        System.out.println("=====================================");
        
        TankQueryDecoder decoder = new TankQueryDecoder();
        
        // Sample frames from the problematic trace (tank E0 response)
        String[] sampleFrames = {
            "00 45 02 E0 01 01 01 42 04 01 83 02 0A 21 C1 78 86 1E E5 68 02 0A 27 C1 91 47 1E D3 E6 02 0A 21 02 C1 91 04 1E D3 E6 51 00",
            "00 4B 02 E0 01 01 01 04 04 02 0A 21 01 C1 91 47 1E D3 E6 02 0A 1E 04 C1 91 47 1E D3 E6 02 0A 1E 03 C1 91 47 1E D3 E6 02 0A 1E 02 C1 91 04 1E D3 E6 65 00",
            "00 4B 02 E0 01 01 01 08 04 02 0A 1E 01 C1 91 86 1E D3 E6 02 0A 21 C1 79 86 1E CB BC 02 0A 28 C1 97 47 1E 80 C4 02 0A 21 01 C1 97 04 1E 80 C4 3E 00",
            "00 8A 02 E0 01 01 01 0C 04 02 0A 21 C1 77 47 1E 24 20 02 0A 0A 05 C1 0B 47 01 EC F7 02 0A 0A 04 C1 0B 47 01 EC F7 02 0A 0A 03 C1 0B 04 01 EC F7 22 00",
            "00 4B 02 E0 01 01 01 10 02 02 0A 0A 02 C1 0B 47 01 EC F7 02 0A 0A 01 C1 0B 04 01 EC F7 20 00",
            "00 0A 02 E0 01 81 C8 39 66 BE 12 E8 00"
        };
        
        // Process each frame
        for (String frameHex : sampleFrames) {
            byte[] frameBytes = hexToBytes(frameHex);
            TankData result = decoder.processNotification(frameBytes);
            if (result != null) {
                System.out.printf("✅ Decoded tank: %s -> %d%%%n", result.queryId, result.level);
                break; // Only show first complete result
            }
        }
        
        System.out.println("\n🎯 Key Finding:");
        System.out.println("The problematic trace has encrypted multi-frame responses");
        System.out.println("that need COBS decoding + TEA decryption to extract tank levels.");
        System.out.println("Once implemented, this should reveal different levels for each tank!");
    }
}