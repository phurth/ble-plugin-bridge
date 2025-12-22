using System.Collections.Generic;

namespace IDS.Core.IDS_CAN
{
	public sealed class PRODUCT_ID
	{
		private static readonly Dictionary<ushort, PRODUCT_ID> Lookup = new Dictionary<ushort, PRODUCT_ID>();

		private static readonly List<PRODUCT_ID> List = new List<PRODUCT_ID>();

		public static readonly PRODUCT_ID UNKNOWN = new PRODUCT_ID(0, 0, "UNKNOWN");

		public static readonly PRODUCT_ID IDS_CAN_NETWORK_ANALYZER_PC_TOOL = new PRODUCT_ID(1, 17513, "IDS-CAN Network Analyzer");

		public static readonly PRODUCT_ID LCI_LINCPAD_WIFI_HUB_ASSEMBLY = new PRODUCT_ID(2, 17778, "WiFi Gateway Controller");

		public static readonly PRODUCT_ID LCI_LINCPAD_2_MOTOR_VELOCITY_SYNC_INWALL_SLIDE_CONTROL_ASSEMBLY = new PRODUCT_ID(3, 17788, "Inwall Slide Control");

		public static readonly PRODUCT_ID LCI_LINCPAD_DOCKING_STATION_ASSEMBLY = new PRODUCT_ID(4, 17799, "Docking Station");

		public static readonly PRODUCT_ID LCI_MF_5F_3V_FUSE_MUX_RECEIVER_LINCPAD_ASSEMBLY = new PRODUCT_ID(5, 18438, "MultiFunction 5-Output w/Hydraulic Controller");

		public static readonly PRODUCT_ID LCI_MF_8F_5V_FUSE_MUX_RECEIVER_LINCPAD_ASSEMBLY = new PRODUCT_ID(6, 18439, "MultiFunction 8-Output w/Hydraulic Controller");

		public static readonly PRODUCT_ID LCI_LINCPAD_LIGHTING_CONTROL_ASSEMBLY = new PRODUCT_ID(7, 18447, "Lighting Control");

		public static readonly PRODUCT_ID LCI_LINCPAD_MULTIFUNCTION_8_OUTPUT_RECEIVER_ASSEMBLY = new PRODUCT_ID(8, 18448, "MultiFunction 8-Output Receiver");

		public static readonly PRODUCT_ID LCI_LINCPAD_MULTIFUNCTION_5_OUTPUT_RECEIVER_ASSEMBLY = new PRODUCT_ID(9, 18449, "MultiFunction 5-Output Receiver");

		public static readonly PRODUCT_ID LCI_LINCPAD_LEVEL_UP_LEVELING_CONTROL_ASSEMBLY = new PRODUCT_ID(10, 18450, "Level Up Leveling Controller");

		public static readonly PRODUCT_ID LCI_LINCPAD_TANK_MONITOR_CONTROL_ASSEMBLY = new PRODUCT_ID(11, 18451, "Tank Monitor Controller");

		public static readonly PRODUCT_ID LCI_LINCPAD_SWITCH_TO_CAN_CONVERTER_CONTROL_ASSEMBLY = new PRODUCT_ID(12, 18452, "Switch to CAN Converter");

		public static readonly PRODUCT_ID LCI_LINCPAD_LINC_TO_CAN_TOUCHPAD_ASSEMBLY = new PRODUCT_ID(13, 18453, "Linc to CAN TouchPad");

		public static readonly PRODUCT_ID LCI_LINCPAD_TABLET = new PRODUCT_ID(14, 18511, "MyRV Tablet");

		public static readonly PRODUCT_ID LCI_LINCPAD_6_LEG_HALL_EFFECT_EJ_LEVELER_ASSEMBLY = new PRODUCT_ID(15, 19251, "6-Leg Hall Effect EJ Leveler");

		public static readonly PRODUCT_ID LCI_LINCPAD_4PT_CAMPER_JACK_CONTROL_ASSEMBLY = new PRODUCT_ID(16, 19445, "4 Point Camper Jack Control");

		public static readonly PRODUCT_ID LCI_MYRV_4PT_5W_HALL_EFFECT_EJ_LEVELER_CONTROL_ASSEMBLY = new PRODUCT_ID(17, 20239, "4-Leg Hall Effect EJ Leveler");

		public static readonly PRODUCT_ID LCI_MYRV_5PT_HALL_EFFECT_HYBRID_EJ_TT_LEVELER_ASSY = new PRODUCT_ID(18, 20242, "5-Leg Hybrid EJ TT Leveler");

		public static readonly PRODUCT_ID LCI_MYRV_4PT_FOLDING_JACK_TT_LEVELER_ASSY = new PRODUCT_ID(19, 20334, "4-Leg Folding Jack TT Leveler");

		public static readonly PRODUCT_ID LCI_MYRV_HOUR_METER_WITH_START_STOP_DRIVE = new PRODUCT_ID(20, 20583, "MyRV Hour Meter");

		public static readonly PRODUCT_ID LCI_MYRV_RGB_LED_LIGHTING_CONTROLLER_ASSEMBLY = new PRODUCT_ID(21, 20590, "RGB LED Lighting Controller");

		public static readonly PRODUCT_ID LCI_BLE_MF_5F_3V_FUSE_MUX_RECEIVER_LINCTAB_ASSEMBLY = new PRODUCT_ID(22, 20678, "Bluetooth MultiFunction 5-Output w/Hydraulic Controller");

		public static readonly PRODUCT_ID LCI_BLE_MF_8F_5V_FUSE_MUX_RECEIVER_LINCTAB_ASSEMBLY = new PRODUCT_ID(23, 20679, "Bluetooth MultiFunction 8-Output w/Hydraulic Controller");

		public static readonly PRODUCT_ID LCI_LINCTAB_BLE_MULTIFUNCTION_8_OUTPUT_RECEIVER_ASSEMBLY = new PRODUCT_ID(24, 20680, "Bluetooth MultiFunction 8-Output Receiver");

		public static readonly PRODUCT_ID LCI_LINCTAB_BLE_MULTIFUNCTION_5_OUTPUT_RECEIVER_ASSEMBLY = new PRODUCT_ID(25, 20681, "Bluetooth MultiFunction 5-Output Receiver");

		public static readonly PRODUCT_ID LCI_IR_REMOTE_CONTROL_ASSEMBLY = new PRODUCT_ID(26, 20805, "Infrared Remote Control Dome");

		public static readonly PRODUCT_ID LCI_MYRV_HVAC_DUAL_ZONE_CONTROL_UNIT_ASSEMBLY = new PRODUCT_ID(27, 21068, "HVAC Dual Zone Controller");

		public static readonly PRODUCT_ID LCI_MULTICHANNEL_LED_CONTROLLER_ASSEMBLY = new PRODUCT_ID(28, 21184, "Multichannel LED Controller");

		public static readonly PRODUCT_ID LCI_MYRV_GENERATOR_GENIE_CONTROL_UNIT = new PRODUCT_ID(29, 21084, "Generator Genie Controller");

		public static readonly PRODUCT_ID LCI_MYRV_HVAC_SINGLE_ZONE_CONTROL_UNIT_ASSEMBLY = new PRODUCT_ID(30, 21186, "HVAC Single Zone Controller");

		public static readonly PRODUCT_ID LCI_MYRV_HVAC_DUAL_ZONE_CONTROL_UNIT_WITH_GEN_HOUR_METER = new PRODUCT_ID(31, 21187, "HVAC Dual Zone Controller w/ Gen Hr Meter");

		public static readonly PRODUCT_ID LCI_MYRV_HVAC_SINGLE_ZONE_CONTROL_UNIT_WITH_GEN_HOUR_METER = new PRODUCT_ID(32, 21188, "HVAC Single Zone Controller w/ Gen Hr Meter");

		public static readonly PRODUCT_ID LCI_CAN_TO_ETHERNET_GATEWAY = new PRODUCT_ID(33, 21049, "CAN To Ethernet Gateway");

		public static readonly PRODUCT_ID LCI_LEVEL_UP_UNITY_CONTROL_ASSY = new PRODUCT_ID(34, 21296, "Level Up Unity Controller");

		public static readonly PRODUCT_ID LCI_3PT_CLASS_C_HYDRAULIC_LEVELER_ASSEMBLY = new PRODUCT_ID(35, 21299, "3 Point Class C Hydraulic Leveler");

		public static readonly PRODUCT_ID LCI_MYRV_5IN_ONECONTROL_TOUCH_PANEL_ASSEMBLY = new PRODUCT_ID(36, 21115, "5\" OneControl Touch Panel");

		public static readonly PRODUCT_ID LCI_MYRV_7IN_ONECONTROL_TOUCH_PANEL_ASSEMBLY = new PRODUCT_ID(37, 21116, "7\" OneControl Touch Panel");

		public static readonly PRODUCT_ID LCI_MULTIFUNCTION_OMEGA_10_REVERSING_4_LATCHING = new PRODUCT_ID(38, 21400, "Multifunction Omega 10 + 4");

		public static readonly PRODUCT_ID LCI_TT_LEVELER_4_X_3K_C_JACK_ASSEMBLY = new PRODUCT_ID(39, 21417, "TT Leveler (4 x 3k C-Jack)");

		public static readonly PRODUCT_ID LCI_MYRV_MOTORIZED_4PT_HYDRAULIC_LEVELER_ASSEMBLY = new PRODUCT_ID(40, 21419, "Motorized 4 Point Hydraulic Leveler");

		public static readonly PRODUCT_ID LCI_MYRV_MOTORIZED_3PT_HYDRAULIC_LEVELER_ASSEMBLY = new PRODUCT_ID(41, 21420, "Motorized 3 Point Hydraulic Leveler");

		public static readonly PRODUCT_ID LCI_IPDM_CONTROLLER_ASSEMBLY = new PRODUCT_ID(42, 21421, "In Transit Power Disconnect Controller");

		public static readonly PRODUCT_ID LCI_TANK_MONITOR_V2_CONTROL_ASSEMBLY = new PRODUCT_ID(43, 21422, "Tank Monitor Controller V2");

		public static readonly PRODUCT_ID LCI_MYRV_SMART_ARM_AWNING_CONTROL_ASSEMBLY = new PRODUCT_ID(44, 21425, "SMART Arm Awning Controller");

		public static readonly PRODUCT_ID LCI_MYRV_10IN_ONECONTROL_TOUCH_PANEL_ASSEMBLY = new PRODUCT_ID(45, 21428, "10\" OneControl Touch Panel");

		public static readonly PRODUCT_ID LCI_ONECONTROL_ANDROID_MOBILE_APPLICATION = new PRODUCT_ID(46, 21429, "OneControl Android Mobile App");

		public static readonly PRODUCT_ID LCI_ONECONTROL_IOS_MOBILE_APPLICATION = new PRODUCT_ID(47, 21430, "OneControl iOS Mobile App");

		public static readonly PRODUCT_ID LCI_MYRV_TT_LEVELER_HD_ASSY = new PRODUCT_ID(48, 21460, "TT Leveler HD");

		public static readonly PRODUCT_ID LCI_MYRV_TT_LEVELER_SE_ASSY = new PRODUCT_ID(49, 21461, "TT Leveler SE");

		public static readonly PRODUCT_ID LCI_MYRV_TT_LEVELER_LT_ASSY = new PRODUCT_ID(50, 21462, "TT Leveler LT");

		public static readonly PRODUCT_ID LCI_MYRV_5W_6PT_GC_3_0_LEVELER_TYPE_3_ASSY = new PRODUCT_ID(51, 21463, "Ground Control 3.0 5th Wheel Leveler (6-Point)");

		public static readonly PRODUCT_ID LCI_MYRV_5W_4PT_GC_3_0_LEVELER_TYPE_3_ASSY = new PRODUCT_ID(52, 21464, "Ground Control 3.0 5th Wheel Leveler (4-Point)");

		public static readonly PRODUCT_ID LCI_MYRV_SMART_POWER_TONGUE_JACK_CONTROL_ASSY = new PRODUCT_ID(53, 21465, "Smart Power-Tongue Jack Controller");

		public static readonly PRODUCT_ID LCI_MULTIFUNCTION_OMEGA_8_REVERSING_4_LATCHING = new PRODUCT_ID(54, 21480, "Multifunction Omega 8 + 4");

		public static readonly PRODUCT_ID LCI_MULTIFUNCTION_OMEGA_6_REVERSING_4_LATCHING = new PRODUCT_ID(55, 21481, "Multifunction Omega 6 + 4");

		public static readonly PRODUCT_ID LCI_MYRV_MULTIFUNCTION_5_OUTPUT_RECEIVER_ASSEMBLY_20A = new PRODUCT_ID(56, 21656, "MultiFunction 5-Output Receiver (20A)");

		public static readonly PRODUCT_ID LCI_MYRV_TT_LEVELER_S_3K_GC = new PRODUCT_ID(57, 21817, "TT Leveler S-3K-GC");

		public static readonly PRODUCT_ID LCI_MYRV_TT_LEVELER_S_2K_GC = new PRODUCT_ID(58, 21818, "TT Leveler S-2K-GC");

		public static readonly PRODUCT_ID LCI_MYRV_TT_LEVELER_S_3K_3K = new PRODUCT_ID(59, 21819, "TT Leveler S-3K-3K");

		public static readonly PRODUCT_ID LCI_MYRV_TT_LEVELER_S_2K_3K = new PRODUCT_ID(60, 21820, "TT Leveler S-2K-3K");

		public static readonly PRODUCT_ID LCI_MYRV_TT_LEVELER_S_2K_2K = new PRODUCT_ID(61, 21821, "TT Leveler S-2K-2K");

		public static readonly PRODUCT_ID LCI_LED_LIGHTING_CONTROLLER_8_1_1_OUTPUT_CAN_ONLY_ASSEMBLY = new PRODUCT_ID(62, 21866, "LED Lighting Controller (8 dimming, 1 latching, 1 RGB)");

		public static readonly PRODUCT_ID LCI_LED_LIGHTING_CONTROLLER_8_OUTPUT_ASSEMBLY = new PRODUCT_ID(63, 21867, "LED Lighting Controller (8 dimming, digital inputs)");

		public static readonly PRODUCT_ID LCI_LED_LIGHTING_CONTROLLER_8_1_OUTPUT_CAN_ONLY_ASSEMBLY = new PRODUCT_ID(64, 21868, "LED Lighting Controller (8 dimming, 1 latching)");

		public static readonly PRODUCT_ID LCI_LED_LIGHTING_CONTROLLER_8_OUTPUT_CAN_ONLY_ASSEMBLY = new PRODUCT_ID(65, 21869, "LED Lighting Controller (8 dimming)");

		public static readonly PRODUCT_ID LCI_MYRV_TT_LEVELER_M_3K_3K_W_LCD_TOUCHPAD_ASSY = new PRODUCT_ID(66, 21878, "TT Leveler M-3K-3K with LCD TouchPad");

		public static readonly PRODUCT_ID LCI_9_ZONE_LED_LIGHTING_CONTROL_8_DIMMING_1_LATCHING_ASSEMBLY = new PRODUCT_ID(67, 21882, "LED Lighting Controller (8 dimming, 1 latching, digital inputs)");

		public static readonly PRODUCT_ID LCI_EUROSLIDE_ASSEMBLY = new PRODUCT_ID(68, 22017, "EuroSlide Controller");

		public static readonly PRODUCT_ID SIMULATED_PRODUCT = new PRODUCT_ID(69, 0, "Simulated Product");

		public static readonly PRODUCT_ID IDS_CAN_SNIFFER = new PRODUCT_ID(70, 0, "IDS-CAN Sniffer");

		public static readonly PRODUCT_ID LCI_MYRV_TT_LEVELER_M_2K_GC_W_LCD_TP_ASSSEMBLY = new PRODUCT_ID(71, 22170, "TT Leveler M-2K-GC w LCD TP");

		public static readonly PRODUCT_ID LCI_MYRV_TT_LEVELER_M_2K_3K_W_LCD_TP_ASSEMBLY = new PRODUCT_ID(72, 22172, "TT Leveler M-2K-3K w LCD TP");

		public static readonly PRODUCT_ID LCI_MYRV_TT_LEVELER_S_3K_GC_W_LCD_TP_ASSEMBLY = new PRODUCT_ID(73, 22173, "TT Leveler S-3K-GC w LCD TP");

		public static readonly PRODUCT_ID LCI_MYRV_TT_LEVELER_S_2K_GC_W_LCD_TP_ASSEMBLY = new PRODUCT_ID(74, 22174, "TT Leveler S-2K-GC w LCD TP");

		public static readonly PRODUCT_ID LCI_MYRV_TT_LEVELER_S_3K_3K_W_LCD_TP_ASSEMBLY = new PRODUCT_ID(75, 22175, "TT Leveler S-3K-3K w LCD TP");

		public static readonly PRODUCT_ID LCI_MYRV_TT_LEVELER_S_2K_3K_W_LCD_TP_ASSEMBLY = new PRODUCT_ID(76, 22176, "TT Leveler S-2K-3K w LCD TP");

		public static readonly PRODUCT_ID LCI_MYRV_TT_LEVELER_M_2K_GC_ASSEMBLY = new PRODUCT_ID(77, 22177, "TT Leveler M-2K-GC");

		public static readonly PRODUCT_ID LCI_MYRV_TT_LEVELER_M_2K_3K_ASSEMBLY = new PRODUCT_ID(78, 22178, "TT Leveler M-2K-3K");

		public static readonly PRODUCT_ID CLASS_C_STABILIZER_CONTROL_2K_C_JACKS_ASSEMBLY = new PRODUCT_ID(79, 22282, "Class C Stabilizer Control (2K C-Jacks)");

		public static readonly PRODUCT_ID MYRV_IN_WALL_SLIDE_CONTROLLER_ASSEMBLY = new PRODUCT_ID(80, 22368, "In-Wall Slide Controller");

		public static readonly PRODUCT_ID MYRV_HVAC_SINGLE_ZONE_CONTROL_WITH_AUTO_START_GEN_HOUR_METER = new PRODUCT_ID(81, 22409, "Single-Zone HVAC Control + Generator Genie w/ Auto-Start");

		public static readonly PRODUCT_ID MYRV_HVAC_DUAL_ZONE_CONTROL_WITH_AUTO_START_GEN_HOUR_METER = new PRODUCT_ID(82, 22410, "Dual-Zone HVAC Control + Generator Genie w/ Auto-Start");

		public static readonly PRODUCT_ID MYRV_AUTO_START_GENERATOR_GENIE_CONTROL_UNIT = new PRODUCT_ID(83, 22411, "Generator Genie Controller w/ Auto-Start");

		public static readonly PRODUCT_ID MYRV_PG_IN_WALL_SLIDE_CONTROLLER_ASSEMBLY_TOWABLE_W_MANUAL_PROGRAM = new PRODUCT_ID(84, 22383, "PG In-Wall Slide Controller (Towable w/ Manual)");

		public static readonly PRODUCT_ID MYRV_PG_IN_WALL_SLIDE_CONTROLLER_ASSEMBLY_MOTORIZED_W_AUTO_PROGRAM = new PRODUCT_ID(85, 22384, "PG In-Wall Slide Controller (Motorized w/ Auto)");

		public static readonly PRODUCT_ID MYRV_PG_IN_WALL_SLIDE_CONTROLLER_ASSEMBLY_MOTORIZED_W_MANUAL_PROGRAM = new PRODUCT_ID(86, 22385, "PG In-Wall Slide Controller (Motorized w/ Manual)");

		public static readonly PRODUCT_ID MYRV_HVAC_V2_SINGLE_ZONE_CONTROL_WITH_AUTO_START_GEN_HOUR_METER = new PRODUCT_ID(87, 22503, "HVAC V2 Single-Zone Control with Auto-Start Gen Hour Meter");

		public static readonly PRODUCT_ID MYRV_HVAC_V2_DUAL_ZONE_CONTROL_WITH_AUTO_START_GEN_HOUR_METER = new PRODUCT_ID(88, 22504, "HVAC V2 Dual-Zone Control with Auto-Start Gen Hour Meter");

		public static readonly PRODUCT_ID MYRV_HVAC_V2_TRIPLE_ZONE_CONTROL_WITH_AUTO_START_GEN_HOUR_METER = new PRODUCT_ID(89, 22505, "HVAC V2 Triple-Zone Control with Auto-Start Gen Hour Meter");

		public static readonly PRODUCT_ID MYRV_HVAC_V2_SINGLE_ZONE_CONTROL = new PRODUCT_ID(90, 22506, "HVAC V2 Single-Zone Control");

		public static readonly PRODUCT_ID MYRV_HVAC_V2_DUAL_ZONE_CONTROL = new PRODUCT_ID(91, 22507, "HVAC V2 Dual-Zone Control");

		public static readonly PRODUCT_ID MYRV_HVAC_V2_TRIPLE_ZONE_CONTROL = new PRODUCT_ID(92, 22508, "HVAC V2 Triple-Zone Control");

		public static readonly PRODUCT_ID MULTIFUNCTION_UNITY_PARTIAL_ASSEMBLY_1 = new PRODUCT_ID(93, 22765, "Multifunction Unity Relay Control");

		public static readonly PRODUCT_ID MULTIFUNCTION_UNITY_PARTIAL_ASSEMBLY_2 = new PRODUCT_ID(94, 22765, "Multifunction Unity HVAC Control");

		public static readonly PRODUCT_ID MULTIFUNCTION_UNITY_PARTIAL_ASSEMBLY_3 = new PRODUCT_ID(95, 22765, "Multifunction Unity Tank Monitor Control");

		public static readonly PRODUCT_ID MYRV_SWITCH_BLOCK_8BV001_ASSEMBLY = new PRODUCT_ID(96, 22814, "myRV Switch Block 8BV001");

		public static readonly PRODUCT_ID ONECONTROL_CLOUD_GATEWAY_ASSEMBLY = new PRODUCT_ID(97, 22829, "OneControl Cloud Gateway");

		public static readonly PRODUCT_ID CAN_TO_ETHERNET_GATEWAY_12V_OUT = new PRODUCT_ID(98, 23011, "CAN To Ethernet Gateway (12V out)");

		public static readonly PRODUCT_ID MYRV_CLASS_C_LEVELER_ASSEMBLY = new PRODUCT_ID(99, 23610, "myRV Class C Leveler Assembly");

		public static readonly PRODUCT_ID MYRV_BLUETOOTH_GATEWAY_ASSEMBLY = new PRODUCT_ID(100, 23357, "myRV Bluetooth Gateway Assembly");

		public static readonly PRODUCT_ID MULTIFUNCTION_UBER_PARTIAL_ASSEMBLY_1_RELAY_CONTROL = new PRODUCT_ID(101, 23649, "Multifunction Uber Partial Assembly 1 (Relay Control)");

		public static readonly PRODUCT_ID MULTIFUNCTION_UBER_PARTIAL_ASSEMBLY_2_HVAC_CONTROL = new PRODUCT_ID(102, 23649, "Multifunction Uber Partial Assembly 2 (HVAC Control)");

		public static readonly PRODUCT_ID MULTIFUNCTION_UBER_PARTIAL_ASSEMBLY_3_LIGHTING_CONTROL = new PRODUCT_ID(103, 23649, "Multifunction Uber Partial Assembly 3 (Lighting Control)");

		public static readonly PRODUCT_ID MULTIFUNCTION_BASE_5_OUT_ELECTRICAL_BLE_ASSEMBLY = new PRODUCT_ID(104, 23651, "Multifunction Base 5-out Electrical BLE Assembly");

		public static readonly PRODUCT_ID MULTIFUNCTION_BASE_5_OUT_ELECTRICAL_NON_BLE_ASSEMBLY = new PRODUCT_ID(105, 23652, "Multifunction Base 5-out Electrical Non-BLE Assembly");

		public static readonly PRODUCT_ID MULTIFUNCTION_BASE_8_OUT_ELECTRICAL_BLE_ASSEMBLY = new PRODUCT_ID(106, 23653, "Multifunction Base 8-out Electrical BLE Assembly");

		public static readonly PRODUCT_ID MULTIFUNCTION_BASE_8_OUT_ELECTRICAL_NON_BLE_ASSEMBLY = new PRODUCT_ID(107, 23654, "Multifunction Base 8-out Electrical Non-BLE Assembly");

		public static readonly PRODUCT_ID MULTIFUNCTION_BASE_5_OUT_HYDRAULIC_BLE_ASSEMBLY = new PRODUCT_ID(108, 23656, "Multifunction Base 5-out Hydraulic BLE Assembly");

		public static readonly PRODUCT_ID MULTIFUNCTION_BASE_5_OUT_HYDRAULIC_NON_BLE_ASSEMBLY = new PRODUCT_ID(109, 23657, "Multifunction Base 5-out Hydraulic Non-BLE Assembly");

		public static readonly PRODUCT_ID MULTIFUNCTION_BASE_8_OUT_HYDRAULIC_BLE_ASSEMBLY = new PRODUCT_ID(110, 23658, "Multifunction Base 8-out Hydraulic BLE Assembly");

		public static readonly PRODUCT_ID MULTIFUNCTION_BASE_8_OUT_HYDRAULIC_NON_BLE_ASSEMBLY = new PRODUCT_ID(111, 23659, "Multifunction Base 8-out Hydraulic Non-BLE Assembly");

		public static readonly PRODUCT_ID LCI_LINCTAB_2_MOTOR_VELOCITY_SYNC_INWALL_SLIDE_CONTROL_ASSEMBLY_OPTION_2 = new PRODUCT_ID(112, 23756, "Inwall Slide Control (option 2)");

		public static readonly PRODUCT_ID LCI_LINCTAB_2_MOTOR_VELOCITY_SYNC_INWALL_SLIDE_CONTROL_ASSEMBLY_OPTION_3 = new PRODUCT_ID(113, 23757, "Inwall Slide Control (option 3)");

		public static readonly PRODUCT_ID LCI_LINCTAB_2_MOTOR_VELOCITY_SYNC_INWALL_SLIDE_CONTROL_ASSEMBLY_OPTION_4 = new PRODUCT_ID(114, 23758, "Inwall Slide Control (option 4)");

		public static readonly PRODUCT_ID ANDROID_DEVICE = new PRODUCT_ID(115, 0, "Android Device");

		public static readonly PRODUCT_ID IOS_DEVICE = new PRODUCT_ID(116, 0, "iOS Device");

		public static readonly PRODUCT_ID WINDOWS_DEVICE = new PRODUCT_ID(117, 0, "Windows Device");

		public static readonly PRODUCT_ID MYRV_RV_C_THERMOSTAT_CONTROL = new PRODUCT_ID(118, 23904, "RV-C Thermostat Control");

		public static readonly PRODUCT_ID LCI_ONECONTROL_2_MOTOR_VELOCITY_SYNC_INWALL_SLIDE_CONTROL_ASSEMBLY_OPTION_2 = new PRODUCT_ID(119, 24152, "Velocity Sync Inwall Slide Control (option 2)");

		public static readonly PRODUCT_ID LCI_ONECONTROL_2_MOTOR_VELOCITY_SYNC_INWALL_SLIDE_CONTROL_ASSEMBLY_OPTION_3 = new PRODUCT_ID(120, 24153, "Velocity Sync Inwall Slide Control (option 3)");

		public static readonly PRODUCT_ID LCI_ONECONTROL_2_MOTOR_VELOCITY_SYNC_INWALL_SLIDE_CONTROL_ASSEMBLY_OPTION_4 = new PRODUCT_ID(121, 24154, "Velocity Sync Inwall Slide Control (option 4)");

		public static readonly PRODUCT_ID MULTIFUNCTION_UNITY_M2_5_PARTIAL_ASSEMBLY_1_RELAY_CONTROL = new PRODUCT_ID(122, 24243, "Multifunction Unity M2 5 Partial Assembly 1 (Relay Control)");

		public static readonly PRODUCT_ID MULTIFUNCTION_UNITY_M2_5_PARTIAL_ASSEMBLY_2_HVAC_CONTROL = new PRODUCT_ID(123, 24243, "Multifunction Unity M2 5 Partial Assembly 2 (HVAC Control)");

		public static readonly PRODUCT_ID MULTIFUNCTION_UNITY_M2_5_PARTIAL_ASSEMBLY_3_TANK_MONITOR = new PRODUCT_ID(124, 24243, "Multifunction Unity M2 5 Partial Assembly 3 (Tank Monitor)");

		public static readonly PRODUCT_ID MULTIFUNCTION_UNITY_M3_PARTIAL_ASSEMBLY_1_RELAY_CONTROL = new PRODUCT_ID(125, 24244, "Multifunction Unity M3 Partial Assembly 1 (Relay Control)");

		public static readonly PRODUCT_ID MULTIFUNCTION_UNITY_M3_PARTIAL_ASSEMBLY_2_HVAC_CONTROL = new PRODUCT_ID(126, 24244, "Multifunction Unity M3 Partial Assembly 2 (HVAC Control)");

		public static readonly PRODUCT_ID MULTIFUNCTION_UNITY_X3_PARTIAL_ASSEMBLY_1_RELAY_CONTROL = new PRODUCT_ID(127, 24245, "Multifunction Unity X3 Partial Assembly 1 (Relay Control)");

		public static readonly PRODUCT_ID MULTIFUNCTION_UNITY_X3_PARTIAL_ASSEMBLY_2_HVAC_CONTROL = new PRODUCT_ID(128, 24245, "Multifunction Unity X3 Partial Assembly 2 (HVAC Control)");

		public static readonly PRODUCT_ID LCI_TT_M_5K_5K_LEVELER_FINAL_ASSY = new PRODUCT_ID(129, 24285, "TT Leveler M-5K-5K");

		public static readonly PRODUCT_ID LCI_TT_S_5K_5K_LEVELER_FINAL_ASSY = new PRODUCT_ID(130, 24289, "TT Leveler S-5K-5K");

		public static readonly PRODUCT_ID ONECONTROL_FIFTH_WHEEL_LEVELER_6PT_GC_3_0_V2_ASSEMBLY = new PRODUCT_ID(131, 24247, "Ground Control 3.0 5th Wheel Leveler v2 (6-Point)");

		public static readonly PRODUCT_ID LEVEL_UP_UNITY_KNEELING_AXLE_LEVELER_ASSEMBLY = new PRODUCT_ID(132, 24321, "Level Up Unity (Kneeling Axle) Leveler");

		public static readonly PRODUCT_ID LCI_ONECONTROL_5W_6PT_GC_3_0_LEVELER_TYPE_3_ASSEMBLY = new PRODUCT_ID(133, 22284, "Ground Control 3.0 5th Wheel Leveler (6-Point)");

		public static readonly PRODUCT_ID LCI_ONECONTROL_5W_4PT_GC_3_0_LEVELER_TYPE_3_ASSEMBLY = new PRODUCT_ID(134, 22285, "Ground Control 3.0 5th Wheel Leveler (4-Point)");

		public static readonly PRODUCT_ID MULTIFUNCTION_UNITY_M3T_PARTIAL_ASSEMBLY_1_RELAY_CONTROL = new PRODUCT_ID(135, 24606, "Multifunction Unity M3T Partial Assembly 1 (Relay Control)");

		public static readonly PRODUCT_ID MULTIFUNCTION_UNITY_M3T_PARTIAL_ASSEMBLY_2_HVAC_CONTROL = new PRODUCT_ID(136, 24606, "Multifunction Unity M3T Partial Assembly 2 (HVAC Control)");

		public static readonly PRODUCT_ID MULTIFUNCTION_UNITY_X2_ASSEMBLY = new PRODUCT_ID(137, 24608, "Multifunction Unity X2");

		public static readonly PRODUCT_ID BLUETOOTH_GATEWAY_DAUGHTER_BOARD_XT_ASSEMBLY = new PRODUCT_ID(138, 24955, "Bluetooth Gateway Daughter Board XT Assembly");

		public static readonly PRODUCT_ID MULTIFUNCTION_UNITY_X1_ASSEMBLY = new PRODUCT_ID(139, 24951, "Multifunction Unity X1 Assembly");

		public static readonly PRODUCT_ID ONECONTROL_LEVEL_UP_ADVANTAGE_ASSY = new PRODUCT_ID(140, 24999, "OneControl Level-Up Advantage");

		public static readonly PRODUCT_ID ONECONTROL_GC_3_0_ADVANTAGE_6PT_FIFTH_WHEEL_LEVELER_ASSY = new PRODUCT_ID(141, 25055, "OneControl GC 3.0 Advantage 6pt Fifth Wheel Leveler");

		public static readonly PRODUCT_ID ONECONTROL_GC_3_0_ADVANTAGE_4PT_FIFTH_WHEEL_LEVELER_ASSY = new PRODUCT_ID(142, 25057, "OneControl GC 3.0 Advantage 4pt Fifth Wheel Leveler");

		public static readonly PRODUCT_ID LCI_ONECONTROL_RV_C_LEVELER_CONTROL_ASSEMBLY = new PRODUCT_ID(143, 25283, "LCI OneControl RV-C Leveler Control Assembly");

		public static readonly PRODUCT_ID MULTIFUNCTION_UNITY_X0_ASSEMBLY = new PRODUCT_ID(144, 25336, "Multifunction Unity X0 Assembly");

		public static readonly PRODUCT_ID MULTIFUNCTION_UNITY_X1_5_ASSEMBLY = new PRODUCT_ID(145, 25361, "Multifunction Unity X1.5 Assembly");

		public static readonly PRODUCT_ID MULTIFUNCTION_UNITY_X1_HD_JAYCO_ASSEMBLY = new PRODUCT_ID(146, 25366, "Multifunction Unity X1 HD JAYCO Assembly");

		public static readonly PRODUCT_ID AQUAFI_HOTSPOT_ASSEMBLY = new PRODUCT_ID(147, 25287, "AquaFi Hotspot Assembly");

		public static readonly PRODUCT_ID CELLULAR_GATEWAY_ASSEMBLY = new PRODUCT_ID(148, 24902, "Cellular Gateway Assembly");

		public static readonly PRODUCT_ID ONECONTROL_HOTSPOT_ASSEMBLY = new PRODUCT_ID(149, 23600, "OneControl Hotspot Assembly");

		public static readonly PRODUCT_ID BLUETOOTH_GATEWAY_DAUGHTER_BOARD_ESP32_PROGRAMMED_PCBA = new PRODUCT_ID(150, 25745, "Bluetooth Gateway Daughter Board Esp32 Programmed PCBA");

		public static readonly PRODUCT_ID ONECONTROL_LEVEL_UP_ADVANTAGE_SLIDE_ASSY = new PRODUCT_ID(151, 25499, "Onecontrol Level Up Advantage Slide Assy");

		public static readonly PRODUCT_ID TPMS_TIRE_LINC = new PRODUCT_ID(152, 25570, "TPMS Tire Linc");

		public static readonly PRODUCT_ID ONECONTROL_MOTORIZED_4PT_HYDRAULIC_LEVELER = new PRODUCT_ID(153, 25776, "OneControl Motorized 4pt Hydraulic Leveler");

		public static readonly PRODUCT_ID ONECONTROL_TT_LEVELER_ADVANTAGE_S_3K_3K_ASSEMBLY = new PRODUCT_ID(154, 25518, "OneControl TT Leveler Advantage S-3K-3K Assembly");

		public static readonly PRODUCT_ID ONECONTROL_TT_LEVELER_ADVANTAGE_S_3K_5K_ASSEMBLY = new PRODUCT_ID(155, 25520, "OneControl TT Leveler Advantage S-3K-5K Assembly");

		public static readonly PRODUCT_ID ONECONTROL_TT_LEVELER_ADVANTAGE_S_2K_3K_ASSEMBLY = new PRODUCT_ID(156, 25522, "OneControl TT Leveler Advantage S-2K-3K Assembly");

		public static readonly PRODUCT_ID ONECONTROL_TT_LEVELER_ADVANTAGE_S_2K_5K_ASSEMBLY = new PRODUCT_ID(157, 25524, "OneControl TT Leveler Advantage S-2K-5K Assembly");

		public static readonly PRODUCT_ID ONECONTROL_TT_LEVELER_ADVANTAGE_S_5K_5K_ASSEMBLY = new PRODUCT_ID(158, 25526, "OneControl TT Leveler Advantage S-5K-5K Assembly");

		public static readonly PRODUCT_ID EURO_SLIDE_SMART_ROOM_12VOLT_V3 = new PRODUCT_ID(159, 25801, "Euro Slide Smart Room 12Volt V3");

		public static readonly PRODUCT_ID ONECONTROL_4PT_TT_LEVELER_ADVANTAGE_ASSY = new PRODUCT_ID(160, 25934, "OneControl 4pt TT Leveler Advantage Assy");

		public static readonly PRODUCT_ID ONECONTROL_4PT_CLASS_A_ADVANTAGE_LEVELER_ASSEMBLY = new PRODUCT_ID(161, 26095, "OneControl 4pt Class A Advantage Leveler Assembly");

		public static readonly PRODUCT_ID MONITOR_PANEL_6X9_ASSEMBLY = new PRODUCT_ID(162, 26201, "Monitor Panel 6x9 Assembly");

		public static readonly PRODUCT_ID MULTIFUNCTION_UNITY_X180T_ASSEMBLY = new PRODUCT_ID(163, 26261, "Multifunction Unity X180T Assembly");

		public static readonly PRODUCT_ID BLUETOOTH_GATEWAY_DAUGHTER_BOARD_XT_V2_PCBA = new PRODUCT_ID(164, 26430, "Bluetooth Gateway Daughter Board XT V2 PCBA");

		public static readonly PRODUCT_ID LCI_SURESHADE_IOS_MOBILE_APPLICATION = new PRODUCT_ID(165, 26464, "LCI SureShade IOS Mobile Application");

		public static readonly PRODUCT_ID LCI_SURESHADE_ANDROID_MOBILE_APPLICATION = new PRODUCT_ID(166, 26465, "LCI SureShade Android Mobile Application");

		public static readonly PRODUCT_ID CAMERA_REAR_OBSERVATION_OEM_ASSEMBLY = new PRODUCT_ID(167, 23670, "Camera Rear Observation OEM Assembly");

		public static readonly PRODUCT_ID CAMERA_REAR_OBSERVATION_AFTERMARKET_ASSEMBLY = new PRODUCT_ID(168, 23671, "Camera Rear Observation AfterMarket Assembly");

		public static readonly PRODUCT_ID ONECONTROL_3PT_MOTORIZED_ADVANTAGE_LEVELER_ASSEMBLY = new PRODUCT_ID(169, 26711, "OneControl 3pt Motorized Advantage Leveler Assembly");

		public static readonly PRODUCT_ID MULTIFUNCTION_UNITY_X145_ASSEMBLY = new PRODUCT_ID(170, 26813, "Multifunction Unity X145 Assembly");

		public static readonly PRODUCT_ID ONECONTROL_BT_GW_PARTIAL_ASSEMBLY_1_RS485_GW = new PRODUCT_ID(171, 26853, "Onecontrol BT GW Partial Assembly 1 RS485 GW");

		public static readonly PRODUCT_ID ONECONTROL_BT_GW_PARTIAL_ASSEMBLY_2_RVLINK_TPMS_GW = new PRODUCT_ID(172, 26853, "Onecontrol BT GW Partial Assembly 2 RvLink TPMS GW");

		public static readonly PRODUCT_ID ONECONTROL_BT_GW_PARTIAL_ASSEMBLY_3_ACCESSORY_GW = new PRODUCT_ID(173, 26853, "Onecontrol BT GW Partial Assembly 3 Accessory GW");

		public static readonly PRODUCT_ID BOTTLECHECK_WIRELESS_LP_TANK_SENSOR = new PRODUCT_ID(174, 27074, "BOTTLECHECK Wireless LP Tank Sensor");

		public static readonly PRODUCT_ID DUMP_TRAILER_CONTROLLER_2_SWITCH_ASSEMBLY = new PRODUCT_ID(175, 27080, "Dump Trailer Controller (2 switch) Assembly");

		public static readonly PRODUCT_ID DUMP_TRAILER_CONTROLLER_4_SWITCH_ASSEMBLY = new PRODUCT_ID(176, 27081, "Dump Trailer Controller (4 switch) Assembly");

		public static readonly PRODUCT_ID CELLULAR_ROUTER_GEN2_HOTSPOT_ONLY = new PRODUCT_ID(177, 27293, "Cellular Router Gen2 Hotspot Only");

		public static readonly PRODUCT_ID CELLULAR_ROUTER_GEN2_TELEMATICS_ONLY = new PRODUCT_ID(178, 27294, "Cellular Router Gen2 Telematics Only");

		public static readonly PRODUCT_ID CELLULAR_ROUTER_GEN2_HOTSPOT_WITH_TELEMATICS = new PRODUCT_ID(179, 27295, "Cellular Router Gen2 Hotspot With Telematics");

		public static readonly PRODUCT_ID ONECONTROL_TEMPERATURE_SENSOR_BT_ASSEMBLY = new PRODUCT_ID(180, 27217, "OneControl Temperature Sensor BT Assembly");

		public static readonly PRODUCT_ID UNITY_X260_ASSEMBLY = new PRODUCT_ID(181, 27395, "Unity X260 Assembly");

		public static readonly PRODUCT_ID ABS_CONTROLLER_ASSEMBLY = new PRODUCT_ID(182, 27376, "ABS controller assembly");

		public static readonly PRODUCT_ID LCI_MYRV_RV_C_STANDALONE_THERMOSTAT_ASSEMBLY = new PRODUCT_ID(183, 27312, "Standalone Thermostat Assembly");

		public static readonly PRODUCT_ID BLUETOOTH_GATEWAY_DAUGHTER_BOARD_RVLINK_ESP32_PROGRAMMED_PCBA = new PRODUCT_ID(184, 26275, "Bluetooth Gateway Daughter Board RvLink ESP32 Programmed PCBA");

		public static readonly PRODUCT_ID PREMIUM_MONITOR_PANEL_ASSEMBLY = new PRODUCT_ID(185, 27521, "Premium Monitor Panel Assembly");

		public static readonly PRODUCT_ID RVC_HVAC_V2_SINGLE_ZONE_CONTROL_ASSEMBLY = new PRODUCT_ID(186, 27529, "RVC HVAC V2 Single Zone Control Assembly");

		public static readonly PRODUCT_ID ONECONTROL_LEVEL_UP_ADV_BT_AL = new PRODUCT_ID(187, 27431, "Onecontrol Level Up Advantage BT Assembly");

		public static readonly PRODUCT_ID ONECONTROL_LEVEL_UP_ADV_SLIDE_OUTPUT_BT_AL = new PRODUCT_ID(188, 27440, "Onecontrol Level Up Advantage Slide Output BT Assembly");

		public static readonly PRODUCT_ID ONECONTROL_GC_3_0_ADV_4PT_5_WHEEL_LEVELER_BT_AL = new PRODUCT_ID(189, 27451, "Onecontrol Gc 3 0 Advantage 4pt Fifth Wheel Leveler BT Assembly");

		public static readonly PRODUCT_ID ONECONTROL_GC_3_0_ADV_6PT_5_WHEEL_LEVELER_BT_AL = new PRODUCT_ID(190, 27444, "Onecontrol Gc 3 0 Advantage 6pt Fifth Wheel Leveler BT Assembly");

		public static readonly PRODUCT_ID ONECONTROL_GC_3_0_HD_ADV_6PT_5_WHEEL_LEVELER_BT_AL = new PRODUCT_ID(191, 27540, "Onecontrol Gc 3 0 Hd Advantage 6pt Fifth Wheel Leveler BT Assembly");

		public static readonly PRODUCT_ID ONECONTROL_GC_3_0_HD_ADV_6PT_5_WHEEL_LEVELER_AL = new PRODUCT_ID(192, 27541, "Onecontrol Gc 3 0 Hd Advantage 6pt Fifth Wheel Leveler Assembly");

		public static readonly PRODUCT_ID ONECONTROL_M_3K_3K_TT_ADV_LEVELER_BT_AL = new PRODUCT_ID(193, 27504, "Onecontrol M 3k 3k TT Advantage Leveler BT Assembly");

		public static readonly PRODUCT_ID ONECONTROL_M_3K_5K_TT_ADV_LEVELER_BT_AL = new PRODUCT_ID(194, 27505, "Onecontrol M 3k 5k TT Advantage Leveler BT Assembly");

		public static readonly PRODUCT_ID ONECONTROL_M_5K_5K_TT_ADV_LEVELER_BT_AL = new PRODUCT_ID(195, 27506, "Onecontrol M 5k 5k TT Advantage Leveler BT Assembly");

		public static readonly PRODUCT_ID ONECONTROL_M_3K_3K_TT_ADV_LEVELER_AL = new PRODUCT_ID(196, 27463, "Onecontrol M 3k 3k TT Advantage Leveler Assembly");

		public static readonly PRODUCT_ID ONECONTROL_M_3K_5K_TT_ADV_LEVELER_AL = new PRODUCT_ID(197, 27470, "Onecontrol M 3k 5k TT Advantage Leveler Assembly");

		public static readonly PRODUCT_ID ONECONTROL_M_5K_5K_TT_ADV_LEVELER_AL = new PRODUCT_ID(198, 27474, "Onecontrol M 5k 5k TT Advantage Leveler Assembly");

		public static readonly PRODUCT_ID RVC_HVAC_V2_SINGLE_ZONE_CONTROL_AL_OPTION_2 = new PRODUCT_ID(199, 27940, "RVC HVAC V2 Single Zone Control Assembly Option 2");

		public static readonly PRODUCT_ID RVC_HVAC_V2_SINGLE_ZONE_CONTROL_AL_OPTION_3 = new PRODUCT_ID(200, 27941, "RVC HVAC V2 Single Zone Control Assembly Option 3");

		public static readonly PRODUCT_ID RVC_HVAC_V2_SINGLE_ZONE_CONTROL_AL_OPTION_4 = new PRODUCT_ID(201, 27942, "RVC HVAC V2 Single Zone Control Assembly Option 4");

		public static readonly PRODUCT_ID CURT_GROUP_SWAY_COMMAND_LINE_2_0 = new PRODUCT_ID(202, 28887, "Curt Group Sway Command 2.0 Controller Assembly");

		public static readonly PRODUCT_ID CAN_RE_FLASH_BOOTLOADER = new PRODUCT_ID(203, 28295, "CAN Re-Flash Bootloader");

		public static readonly PRODUCT_ID DC_BATTERY_MONITOR = new PRODUCT_ID(204, 27317, "DC Battery Monitor");

		public static readonly PRODUCT_ID LIPPERT_ONE_WIND_SENSOR = new PRODUCT_ID(205, 27722, "Lippert One Wind Sensor");

		public static readonly PRODUCT_ID FIFTH_TANK_MONITOR_PANEL = new PRODUCT_ID(206, 28519, "Fifth Tank Monitor Panel");

		public static readonly PRODUCT_ID ONECONTROL_4PT_MOTORIZED_TRITON_ADVANTAGE_LEVELER_BT = new PRODUCT_ID(207, 28528, "OneControl 4pt Motorized Triton Advantage Leveler BT Assembly");

		public static readonly PRODUCT_ID ONECONTROL_5TH_WHEEL_TRITON_ADVANTAGE_LEVELER_SLIDE_BT = new PRODUCT_ID(208, 28729, "OneControl 5th Wheel Triton Advantage Leveler Slide BT Assembly");

		public static readonly PRODUCT_ID ONECONTROL_3PT_MOTORIZED_TRITON_ADVANTAGE_LEVELER_BT = new PRODUCT_ID(209, 28732, "OneControl 3pt Motorized Triton Advantage Leveler BT Assembly");

		public static readonly PRODUCT_ID LIPPERT_AM_BT_DOOR_LOCK_ASSEMBLY = new PRODUCT_ID(210, 28720, "Lippert AM BT Door Lock Assembly");

		public static readonly PRODUCT_ID UNITY_X270_ASSEMBLY = new PRODUCT_ID(211, 29620, "Unity X270 Assembly");

		public static readonly PRODUCT_ID CURT_ECHO_BRAKE_CONTROLLER = new PRODUCT_ID(212, 0, "Curt Echo Brake Controller");

		public static readonly PRODUCT_ID BASECAMP_LEVELER_5W_TOUCHPAD_ASSEMBLY = new PRODUCT_ID(213, 29932, "Basecamp Leveler 5W Touchpad Assembly");

		public static readonly PRODUCT_ID BASECAMP_LEVELER_MOTORIZED_TOUCHPAD_ASSEMBLY = new PRODUCT_ID(214, 29976, "Basecamp Leveler Motorized Touchpad Assembly");

		public static readonly PRODUCT_ID BASECAMP_HYDRAULIC_5W_LEVELER_ASSEMBLY = new PRODUCT_ID(215, 29942, "BaseCamp Hydraulic 5W Leveler Assembly");

		public static readonly PRODUCT_ID BASECAMP_MOTORIZED_4PT_LEVELER_ASSEMBLY = new PRODUCT_ID(216, 29946, "BaseCamp Motorized 4pt Leveler Assembly");

		public static readonly PRODUCT_ID BASECAMP_MOTORIZED_3PT_LEVELER_ASSEMBLY = new PRODUCT_ID(217, 30021, "BaseCamp Motorized 3pt Leveler Assembly");

		public static readonly PRODUCT_ID OC_PG_INWALL_SLIDE_CONTROL_TOWABLE_AUTO_OP2 = new PRODUCT_ID(218, 30047, "OC PG-Inwall Slide Control - Towable Auto op2");

		public static readonly PRODUCT_ID OC_PG_INWALL_SLIDE_CONTROL_TOWABLE_AUTO_OP3 = new PRODUCT_ID(219, 30048, "OC PG-Inwall Slide Control - Towable Auto op3");

		public static readonly PRODUCT_ID OC_PG_INWALL_SLIDE_CONTROL_TOWABLE_AUTO_OP4 = new PRODUCT_ID(220, 30049, "OC PG-Inwall Slide Control - Towable Auto op4");

		public static readonly PRODUCT_ID ABS_AUSTRALIA_CONTROLLER_ASSEMBLY = new PRODUCT_ID(221, 30126, "ABS Australia Controller Assembly");

		public static readonly PRODUCT_ID LP_TANK_SENSOR_ASSEMBLY = new PRODUCT_ID(222, 28236, "LCI LP Tank Sensor");

		public static readonly PRODUCT_ID FURRION_ONECONTROL_HEADLESS_STEREO_MAIN_ASSY = new PRODUCT_ID(223, 30287, "Furrion OneControl Headless Stereo Main Assy");

		public static readonly PRODUCT_ID FURRION_ONECONTROL_HEADLESS_STEREO_SATELLITE_ASSY = new PRODUCT_ID(224, 30288, "Furrion OneControl Headless Stereo Satellite Assy");

		public static readonly PRODUCT_ID SUPER_PREMIUM_MONITOR_PANEL = new PRODUCT_ID(225, 30255, "Super Premium Monitor Panel");

		public static readonly PRODUCT_ID CURT_TPMS_TIRE_LINC_AUTO = new PRODUCT_ID(226, 30033, "Curt TPMS Tire Linc Auto");

		public static readonly PRODUCT_ID ABS_SWAY_CONTROLLER_ASSEMBLY = new PRODUCT_ID(227, 30613, "ABS/Sway Controller Assembly");

		public static readonly PRODUCT_ID ABS_SWAY_PANIC_BRAKE_CONTROLLER_ASSEMBLY = new PRODUCT_ID(228, 30614, "ABS/Sway/Panic Brake Controller Assembly");

		public static readonly PRODUCT_ID LCI_ONECONTROL_2_MOTOR_VELOCITY_SYNC_INWALL_SLIDE_CONTROL_ASSEMBLY_OPTION_5 = new PRODUCT_ID(229, 30631, "Velocity Sync Inwall Slide Control (option 5)");

		public static readonly PRODUCT_ID OC_PG_INWALL_SLIDE_CONTROL_TOWABLE_AUTO_OP5 = new PRODUCT_ID(230, 30715, "OC PG-Inwall Slide Control - Towable Auto op5");

		public static readonly PRODUCT_ID UNITY_X180D_ASSEMBLY = new PRODUCT_ID(231, 30599, "Unity X180D Assembly");

		public static readonly PRODUCT_ID UNITY_X270D_ASSEMBLY = new PRODUCT_ID(232, 30606, "Unity X270D Assembly");

		public static readonly PRODUCT_ID EMB_ABS_CONTROLLER_ASSEMBLY = new PRODUCT_ID(233, 30332, "EMB ABS Controller Assembly");

		public static readonly PRODUCT_ID EMB_MOTOR_CONTROLLER_ASSEMBLY = new PRODUCT_ID(234, 30333, "EMB Motor Controller Assembly");

		public static readonly PRODUCT_ID LCI_SURESHADE_AWNING_ASSEMBLY = new PRODUCT_ID(235, 31483, "LCI SureShade Awning Assembly");

		public static readonly PRODUCT_ID ELITETRACK_TOWABLE_SLIDE_CONTROLLER_ASSEMBLY = new PRODUCT_ID(236, 30954, "EliteTrack Towable Slide Controller Assembly");

		public static readonly PRODUCT_ID ELITETRACK_MOTORIZED_SLIDE_CONTROLLER_ASSEMBLY = new PRODUCT_ID(237, 31604, "EliteTrack Motorized Slide Controller Assembly");

		public static readonly PRODUCT_ID BASECAMP_ELECTRIC_5W_LEVELER_ASSEMBLY = new PRODUCT_ID(238, 31165, "BaseCamp Electric 5W Leveler Assembly");

		public static readonly PRODUCT_ID ONECONTROL_M_PSX2_GC_TT_LEVELER_ADVANTAGE_ASSY = new PRODUCT_ID(239, 31782, "OneControl M-PSX2-GC TT Leveler Advantage Assy");

		public static readonly PRODUCT_ID FLIC_BUTTON = new PRODUCT_ID(240, 0, "FLIC Button");

		public static readonly PRODUCT_ID ELITETRACK_TOWABLE_SLIDE_CONTROLLER_ASSEMBLY_OPTION_2 = new PRODUCT_ID(241, 31840, "EliteTrack Towable Slide Controller Assembly - Option 2");

		public static readonly PRODUCT_ID ELITETRACK_TOWABLE_SLIDE_CONTROLLER_ASSEMBLY_OPTION_3 = new PRODUCT_ID(242, 31843, "EliteTrack Towable Slide Controller Assembly - Option 3");

		public static readonly PRODUCT_ID ELITETRACK_TOWABLE_SLIDE_CONTROLLER_ASSEMBLY_OPTION_4 = new PRODUCT_ID(243, 31846, "EliteTrack Towable Slide Controller Assembly - Option 4");

		public static readonly PRODUCT_ID ELITETRACK_TOWABLE_SLIDE_CONTROLLER_ASSEMBLY_OPTION_5 = new PRODUCT_ID(244, 31849, "EliteTrack Towable Slide Controller Assembly - Option 5");

		public static readonly PRODUCT_ID TPMS_2_5_HANDHELD_DISPLAY_ASSEMBLY = new PRODUCT_ID(245, 27061, "LoCap Display Assembly");

		public static readonly PRODUCT_ID UNITY_X4C_PARTIAL_ASSEMBLY = new PRODUCT_ID(246, 32586, "Unity X4C Partial Assembly 4 (RV-C Thermostat Control)");

		public static readonly PRODUCT_ID TT3_LEVELER_GD_BT_ASSY = new PRODUCT_ID(247, 32621, "TT3 Leveler Gate Defender Bluetooth");

		public static readonly PRODUCT_ID TT3_LEVELER_GD_ASSY = new PRODUCT_ID(248, 32622, "TT3 Leveler Gate Defender");

		public static readonly PRODUCT_ID TT3_LEVELER_M_BT_ASSY = new PRODUCT_ID(249, 32623, "TT3 Leveler Modified Bluetooth");

		public static readonly PRODUCT_ID TT3_LEVELER_M_ASSY = new PRODUCT_ID(250, 32624, "TT3 Leveler Modified");

		public static readonly PRODUCT_ID UNITY_X340_ASSEMBLY = new PRODUCT_ID(251, 32178, "Unity X340 Assembly");

		public static readonly PRODUCT_ID TT2_LEVELER_M_BT_ASSY = new PRODUCT_ID(252, 33095, "TT2 Leveler Modified Bluetooth");

		public static readonly PRODUCT_ID TT2_LEVELER_M_ASSY = new PRODUCT_ID(253, 33120, "TT2 Leveler Modified");

		public static readonly PRODUCT_ID TRUECOURSE_OEM_AUSTRALIA_CONTROLLER_ASSEMBLY = new PRODUCT_ID(254, 33318, "TrueCourse OEM Australia Controller Assembly");

		public static readonly PRODUCT_ID LATCHXTEND_DOOR_LOCK = new PRODUCT_ID(255, 0, "LatchXtend Door Lock");

		public static readonly PRODUCT_ID TT2S_LEVELER_M_BT_ASSY = new PRODUCT_ID(256, 33353, "TT2S Leveler Modified Bluetooth");

		public static readonly PRODUCT_ID TT2S_LEVELER_M_ASSY = new PRODUCT_ID(257, 33358, "TT2S Leveler Modified");

		public static readonly PRODUCT_ID TT3S_LEVELER_M_BT_ASSY = new PRODUCT_ID(258, 33361, "TT3S Leveler Modified Bluetooth");

		public static readonly PRODUCT_ID TT3S_LEVELER_M_ASSY = new PRODUCT_ID(259, 33367, "TT3S Leveler Modified");

		public static readonly PRODUCT_ID TPMS_TIRELINC_PRO_REPEATER = new PRODUCT_ID(260, 30571, "TPMS TireLinc Pro Repeater");

		public static readonly PRODUCT_ID HIL_TEST_BENCH = new PRODUCT_ID(261, 0, "HIL Test Bench");

		public static readonly PRODUCT_ID PLATINUM_XL_MONITOR_PANEL = new PRODUCT_ID(262, 33718, "Platinum XL Monitor Panel");

		public static readonly PRODUCT_ID ONECONTROL_GC_3_0_ADVANTAGE_ALTERNATE_4PT_FIFTH_WHEEL_LEVELER_ASSY = new PRODUCT_ID(263, 33899, "OneControl GC 3.0 Advantage Alternate 4pt Fifth Wheel Leveler");

		public static readonly PRODUCT_ID GD15_SURESLIDE_MOTORIZED_SLIDE_CONTROLLER_ASSEMBLY = new PRODUCT_ID(264, 33892, "GD15 SureSlide Motorized Slide Controller Assembly");

		public static readonly PRODUCT_ID BTGW_DB_RVLINK_ESP32_PROGRAMMED_PCBA_LEVELER = new PRODUCT_ID(265, 26275, "Bluetooth Gateway Daughter Board RvLink ESP32 Programmed PCBA on Leveler");

		public static readonly PRODUCT_ID AWNING_MOUNTED_WIND_SENSOR = new PRODUCT_ID(266, 34251, "Awning Mounted Wind Sensor");

		public readonly ushort Value;

		public readonly int AssemblyPartNumber;

		public readonly string Name;

		public bool IsValid => this?.Value > 0;

		public static IEnumerable<PRODUCT_ID> GetEnumerator()
		{
			return List;
		}

		private PRODUCT_ID(ushort value)
		{
			Value = value;
			AssemblyPartNumber = 0;
			Name = "UNKNOWN_" + value.ToString("X4");
			if (value > 0 && !Lookup.ContainsKey(value))
			{
				Lookup.Add(value, this);
			}
		}

		private PRODUCT_ID(ushort value, int assembly_number, string name)
		{
			Value = value;
			AssemblyPartNumber = assembly_number;
			Name = name.Trim();
			if (value > 0)
			{
				List.Add(this);
				Lookup.Add(value, this);
			}
		}

		public static implicit operator ushort(PRODUCT_ID msg)
		{
			return msg?.Value ?? 0;
		}

		public static implicit operator PRODUCT_ID(ushort value)
		{
			if (value == 0)
			{
				return UNKNOWN;
			}
			if (!Lookup.TryGetValue(value, out var value2))
			{
				return new PRODUCT_ID(value);
			}
			return value2;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
