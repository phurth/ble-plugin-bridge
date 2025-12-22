using System.Collections.Generic;

namespace IDS.Core.IDS_CAN
{
	public sealed class FUNCTION_NAME
	{
		private static readonly Dictionary<ushort, FUNCTION_NAME> Lookup;

		private static readonly List<FUNCTION_NAME> NameList;

		public static readonly FUNCTION_NAME UNKNOWN;

		public const ushort DIAGNOSTIC_TOOL = 1;

		public const ushort MYRV_TABLET = 2;

		public const ushort GAS_WATER_HEATER = 3;

		public const ushort ELECTRIC_WATER_HEATER = 4;

		public const ushort WATER_PUMP = 5;

		public const ushort BATH_VENT = 6;

		public const ushort LIGHT = 7;

		public const ushort FLOOD_LIGHT = 8;

		public const ushort WORK_LIGHT = 9;

		public const ushort FRONT_BEDROOM_CEILING_LIGHT = 10;

		public const ushort FRONT_BEDROOM_OVERHEAD_LIGHT = 11;

		public const ushort FRONT_BEDROOM_VANITY_LIGHT = 12;

		public const ushort FRONT_BEDROOM_SCONCE_LIGHT = 13;

		public const ushort FRONT_BEDROOM_LOFT_LIGHT = 14;

		public const ushort REAR_BEDROOM_CEILING_LIGHT = 15;

		public const ushort REAR_BEDROOM_OVERHEAD_LIGHT = 16;

		public const ushort REAR_BEDROOM_VANITY_LIGHT = 17;

		public const ushort REAR_BEDROOM_SCONCE_LIGHT = 18;

		public const ushort REAR_BEDROOM_LOFT_LIGHT = 19;

		public const ushort LOFT_LIGHT = 20;

		public const ushort FRONT_HALL_LIGHT = 21;

		public const ushort REAR_HALL_LIGHT = 22;

		public const ushort FRONT_BATHROOM_LIGHT = 23;

		public const ushort FRONT_BATHROOM_VANITY_LIGHT = 24;

		public const ushort FRONT_BATHROOM_CEILING_LIGHT = 25;

		public const ushort FRONT_BATHROOM_SHOWER_LIGHT = 26;

		public const ushort FRONT_BATHROOM_SCONCE_LIGHT = 27;

		public const ushort REAR_BATHROOM_VANITY_LIGHT = 28;

		public const ushort REAR_BATHROOM_CEILING_LIGHT = 29;

		public const ushort REAR_BATHROOM_SHOWER_LIGHT = 30;

		public const ushort REAR_BATHROOM_SCONCE_LIGHT = 31;

		public const ushort KITCHEN_CEILING_LIGHT = 32;

		public const ushort KITCHEN_SCONCE_LIGHT = 33;

		public const ushort KITCHEN_PENDANTS_LIGHT = 34;

		public const ushort KITCHEN_RANGE_LIGHT = 35;

		public const ushort KITCHEN_COUNTER_LIGHT = 36;

		public const ushort KITCHEN_BAR_LIGHT = 37;

		public const ushort KITCHEN_ISLAND_LIGHT = 38;

		public const ushort KITCHEN_CHANDELIER_LIGHT = 39;

		public const ushort KITCHEN_UNDER_CABINET_LIGHT = 40;

		public const ushort LIVING_ROOM_CEILING_LIGHT = 41;

		public const ushort LIVING_ROOM_SCONCE_LIGHT = 42;

		public const ushort LIVING_ROOM_PENDANTS_LIGHT = 43;

		public const ushort LIVING_ROOM_BAR_LIGHT = 44;

		public const ushort GARAGE_CEILING_LIGHT = 45;

		public const ushort GARAGE_CABINET_LIGHT = 46;

		public const ushort SECURITY_LIGHT = 47;

		public const ushort PORCH_LIGHT = 48;

		public const ushort AWNING_LIGHT = 49;

		public const ushort BATHROOM_LIGHT = 50;

		public const ushort BATHROOM_VANITY_LIGHT = 51;

		public const ushort BATHROOM_CEILING_LIGHT = 52;

		public const ushort BATHROOM_SHOWER_LIGHT = 53;

		public const ushort BATHROOM_SCONCE_LIGHT = 54;

		public const ushort HALL_LIGHT = 55;

		public const ushort BUNK_ROOM_LIGHT = 56;

		public const ushort BEDROOM_LIGHT = 57;

		public const ushort LIVING_ROOM_LIGHT = 58;

		public const ushort KITCHEN_LIGHT = 59;

		public const ushort LOUNGE_LIGHT = 60;

		public const ushort CEILING_LIGHT = 61;

		public const ushort ENTRY_LIGHT = 62;

		public const ushort BED_CEILING_LIGHT = 63;

		public const ushort BEDROOM_LAV_LIGHT = 64;

		public const ushort SHOWER_LIGHT = 65;

		public const ushort GALLEY_LIGHT = 66;

		public const ushort FRESH_TANK = 67;

		public const ushort GREY_TANK = 68;

		public const ushort BLACK_TANK = 69;

		public const ushort FUEL_TANK = 70;

		public const ushort GENERATOR_FUEL_TANK = 71;

		public const ushort AUXILIARY_FUEL_TANK = 72;

		public const ushort FRONT_BATH_GREY_TANK = 73;

		public const ushort FRONT_BATH_FRESH_TANK = 74;

		public const ushort FRONT_BATH_BLACK_TANK = 75;

		public const ushort REAR_BATH_GREY_TANK = 76;

		public const ushort REAR_BATH_FRESH_TANK = 77;

		public const ushort REAR_BATH_BLACK_TANK = 78;

		public const ushort MAIN_BATH_GREY_TANK = 79;

		public const ushort MAIN_BATH_FRESH_TANK = 80;

		public const ushort MAIN_BATH_BLACK_TANK = 81;

		public const ushort GALLEY_GREY_TANK = 82;

		public const ushort GALLEY_FRESH_TANK = 83;

		public const ushort GALLEY_BLACK_TANK = 84;

		public const ushort KITCHEN_GREY_TANK = 85;

		public const ushort KITCHEN_FRESH_TANK = 86;

		public const ushort KITCHEN_BLACK_TANK = 87;

		public const ushort LANDING_GEAR = 88;

		public const ushort FRONT_STABILIZER = 89;

		public const ushort REAR_STABILIZER = 90;

		public const ushort TV_LIFT = 91;

		public const ushort BED_LIFT = 92;

		public const ushort BATH_VENT_COVER = 93;

		public const ushort DOOR_LOCK = 94;

		public const ushort GENERATOR = 95;

		public const ushort SLIDE = 96;

		public const ushort MAIN_SLIDE = 97;

		public const ushort BEDROOM_SLIDE = 98;

		public const ushort GALLEY_SLIDE = 99;

		public const ushort KITCHEN_SLIDE = 100;

		public const ushort CLOSET_SLIDE = 101;

		public const ushort OPTIONAL_SLIDE = 102;

		public const ushort DOOR_SIDE_SLIDE = 103;

		public const ushort OFF_DOOR_SLIDE = 104;

		public const ushort AWNING = 105;

		public const ushort LEVEL_UP_LEVELER = 106;

		public const ushort WATER_TANK_HEATER = 107;

		public const ushort MYRV_TOUCHSCREEN = 108;

		public const ushort LEVELER = 109;

		public const ushort VENT_COVER = 110;

		public const ushort FRONT_BEDROOM_VENT_COVER = 111;

		public const ushort BEDROOM_VENT_COVER = 112;

		public const ushort FRONT_BATHROOM_VENT_COVER = 113;

		public const ushort MAIN_BATHROOM_VENT_COVER = 114;

		public const ushort REAR_BATHROOM_VENT_COVER = 115;

		public const ushort KITCHEN_VENT_COVER = 116;

		public const ushort LIVING_ROOM_VENT_COVER = 117;

		public const ushort FOUR_LEG_TRUCK_CAMPLER_LEVELER = 118;

		public const ushort SIX_LEG_HALL_EFFECT_EJ_LEVELER = 119;

		public const ushort PATIO_LIGHT = 120;

		public const ushort HUTCH_LIGHT = 121;

		public const ushort SCARE_LIGHT = 122;

		public const ushort DINETTE_LIGHT = 123;

		public const ushort BAR_LIGHT = 124;

		public const ushort OVERHEAD_LIGHT = 125;

		public const ushort OVERHEAD_BAR_LIGHT = 126;

		public const ushort FOYER_LIGHT = 127;

		public const ushort RAMP_DOOR_LIGHT = 128;

		public const ushort ENTERTAINMENT_LIGHT = 129;

		public const ushort REAR_ENTRY_DOOR_LIGHT = 130;

		public const ushort CEILING_FAN_LIGHT = 131;

		public const ushort OVERHEAD_FAN_LIGHT = 132;

		public const ushort BUNK_SLIDE = 133;

		public const ushort BED_SLIDE = 134;

		public const ushort WARDROBE_SLIDE = 135;

		public const ushort ENTERTAINMENT_SLIDE = 136;

		public const ushort SOFA_SLIDE = 137;

		public const ushort PATIO_AWNING = 138;

		public const ushort REAR_AWNING = 139;

		public const ushort SIDE_AWNING = 140;

		public const ushort JACKS = 141;

		public const ushort LEVELER_2 = 142;

		public const ushort EXTERIOR_LIGHT = 143;

		public const ushort LOWER_ACCENT_LIGHT = 144;

		public const ushort UPPER_ACCENT_LIGHT = 145;

		public const ushort DS_SECURITY_LIGHT = 146;

		public const ushort ODS_SECURITY_LIGHT = 147;

		public const ushort SLIDE_IN_SLIDE = 148;

		public const ushort HITCH_LIGHT = 149;

		public const ushort CLOCK = 150;

		public const ushort TV = 151;

		public const ushort DVD = 152;

		public const ushort BLU_RAY = 153;

		public const ushort VCR = 154;

		public const ushort PVR = 155;

		public const ushort CABLE = 156;

		public const ushort SATELLITE = 157;

		public const ushort AUDIO = 158;

		public const ushort CD_PLAYER = 159;

		public const ushort TUNER = 160;

		public const ushort RADIO = 161;

		public const ushort SPEAKERS = 162;

		public const ushort GAME = 163;

		public const ushort CLOCK_RADIO = 164;

		public const ushort AUX = 165;

		public const ushort CLIMATE_ZONE = 166;

		public const ushort FIREPLACE = 167;

		public const ushort THERMOSTAT = 168;

		public const ushort FRONT_CAP_LIGHT = 169;

		public const ushort STEP_LIGHT = 170;

		public const ushort DS_FLOOD_LIGHT = 171;

		public const ushort INTERIOR_LIGHT = 172;

		public const ushort FRESH_TANK_HEATER = 173;

		public const ushort GREY_TANK_HEATER = 174;

		public const ushort BLACK_TANK_HEATER = 175;

		public const ushort LP_TANK = 176;

		public const ushort STALL_LIGHT = 177;

		public const ushort MAIN_LIGHT = 178;

		public const ushort BATH_LIGHT = 179;

		public const ushort BUNK_LIGHT = 180;

		public const ushort BED_LIGHT = 181;

		public const ushort CABINET_LIGHT = 182;

		public const ushort NETWORK_BRIDGE = 183;

		public const ushort ETHERNET_BRIDGE = 184;

		public const ushort WIFI_BRIDGE = 185;

		public const ushort IN_TRANSIT_POWER_DISCONNECT = 186;

		public const ushort LEVEL_UP_UNITY = 187;

		public const ushort TT_LEVELER = 188;

		public const ushort TRAVEL_TRAILER_LEVELER = 189;

		public const ushort FIFTH_WHEEL_LEVELER = 190;

		public const ushort FUEL_PUMP = 191;

		public const ushort MAIN_CLIMATE_ZONE = 192;

		public const ushort BEDROOM_CLIMATE_ZONE = 193;

		public const ushort GARAGE_CLIMATE_ZONE = 194;

		public const ushort COMPARTMENT_LIGHT = 195;

		public const ushort TRUNK_LIGHT = 196;

		public const ushort BAR_TV = 197;

		public const ushort BATHROOM_TV = 198;

		public const ushort BEDROOM_TV = 199;

		public const ushort BUNK_ROOM_TV = 200;

		public const ushort EXTERIOR_TV = 201;

		public const ushort FRONT_BATHROOM_TV = 202;

		public const ushort FRONT_BEDROOM_TV = 203;

		public const ushort GARAGE_TV = 204;

		public const ushort KITCHEN_TV = 205;

		public const ushort LIVING_ROOM_TV = 206;

		public const ushort LOFT_TV = 207;

		public const ushort LOUNGE_TV = 208;

		public const ushort MAIN_TV = 209;

		public const ushort PATIO_TV = 210;

		public const ushort REAR_BATHROOM_TV = 211;

		public const ushort REAR_BEDROOM_TV = 212;

		public const ushort BATHROOM_DOOR_LOCK = 213;

		public const ushort BEDROOM_DOOR_LOCK = 214;

		public const ushort FRONT_DOOR_LOCK = 215;

		public const ushort GARAGE_DOOR_LOCK = 216;

		public const ushort MAIN_DOOR_LOCK = 217;

		public const ushort PATIO_DOOR_LOCK = 218;

		public const ushort REAR_DOOR_LOCK = 219;

		public const ushort ACCENT_LIGHT = 220;

		public const ushort BATHROOM_ACCENT_LIGHT = 221;

		public const ushort BEDROOM_ACCENT_LIGHT = 222;

		public const ushort FRONT_BEDROOM_ACCENT_LIGHT = 223;

		public const ushort GARAGE_ACCENT_LIGHT = 224;

		public const ushort KITCHEN_ACCENT_LIGHT = 225;

		public const ushort PATIO_ACCENT_LIGHT = 226;

		public const ushort REAR_BEDROOM_ACCENT_LIGHT = 227;

		public const ushort BEDROOM_RADIO = 228;

		public const ushort BUNK_ROOM_RADIO = 229;

		public const ushort EXTERIOR_RADIO = 230;

		public const ushort FRONT_BEDROOM_RADIO = 231;

		public const ushort GARAGE_RADIO = 232;

		public const ushort KITCHEN_RADIO = 233;

		public const ushort LIVING_ROOM_RADIO = 234;

		public const ushort LOFT_RADIO = 235;

		public const ushort PATIO_RADIO = 236;

		public const ushort REAR_BEDROOM_RADIO = 237;

		public const ushort BEDROOM_ENTERTAINMENT_SYSTEM = 238;

		public const ushort BUNK_ROOM_ENTERTAINMENT_SYSTEM = 239;

		public const ushort ENTERTAINMENT_SYSTEM = 240;

		public const ushort EXTERIOR_ENTERTAINMENT_SYSTEM = 241;

		public const ushort FRONT_BEDROOM_ENTERTAINMENT_SYSTEM = 242;

		public const ushort GARAGE_ENTERTAINMENT_SYSTEM = 243;

		public const ushort KITCHEN_ENTERTAINMENT_SYSTEM = 244;

		public const ushort LIVING_ROOM_ENTERTAINMENT_SYSTEM = 245;

		public const ushort LOFT_ENTERTAINMENT_SYSTEM = 246;

		public const ushort MAIN_ENTERTAINMENT_SYSTEM = 247;

		public const ushort PATIO_ENTERTAINMENT_SYSTEM = 248;

		public const ushort REAR_BEDROOM_ENTERTAINMENT_SYSTEM = 249;

		public const ushort LEFT_STABILIZER = 250;

		public const ushort RIGHT_STABILIZER = 251;

		public const ushort STABILIZER = 252;

		public const ushort SOLAR = 253;

		public const ushort SOLAR_POWER = 254;

		public const ushort BATTERY = 255;

		public const ushort MAIN_BATTERY = 256;

		public const ushort AUX_BATTERY = 257;

		public const ushort SHORE_POWER = 258;

		public const ushort AC_POWER = 259;

		public const ushort AC_MAINS = 260;

		public const ushort AUX_POWER = 261;

		public const ushort OUTPUTS = 262;

		public const ushort RAMP_DOOR = 263;

		public const ushort FAN = 264;

		public const ushort BATH_FAN = 265;

		public const ushort REAR_FAN = 266;

		public const ushort FRONT_FAN = 267;

		public const ushort KITCHEN_FAN = 268;

		public const ushort CEILING_FAN = 269;

		public const ushort TANK_HEATER = 270;

		public const ushort FRONT_CEILING_LIGHT = 271;

		public const ushort REAR_CEILING_LIGHT = 272;

		public const ushort CARGO_LIGHT = 273;

		public const ushort FASCIA_LIGHT = 274;

		public const ushort SLIDE_CEILING_LIGHT = 275;

		public const ushort SLIDE_OVERHEAD_LIGHT = 276;

		public const ushort DECOR_LIGHT = 277;

		public const ushort READING_LIGHT = 278;

		public const ushort FRONT_READING_LIGHT = 279;

		public const ushort REAR_READING_LIGHT = 280;

		public const ushort LIVING_ROOM_CLIMATE_ZONE = 281;

		public const ushort FRONT_LIVING_ROOM_CLIMATE_ZONE = 282;

		public const ushort REAR_LIVING_ROOM_CLIMATE_ZONE = 283;

		public const ushort FRONT_BEDROOM_CLIMATE_ZONE = 284;

		public const ushort REAR_BEDROOM_CLIMATE_ZONE = 285;

		public const ushort BED_TILT = 286;

		public const ushort FRONT_BED_TILT = 287;

		public const ushort REAR_BED_TILT = 288;

		public const ushort MENS_LIGHT = 289;

		public const ushort WOMENS_LIGHT = 290;

		public const ushort SERVICE_LIGHT = 291;

		public const ushort ODS_FLOOD_LIGHT = 292;

		public const ushort UNDERBODY_ACCENT_LIGHT = 293;

		public const ushort SPEAKER_LIGHT = 294;

		public const ushort WATER_HEATER = 295;

		public const ushort WATER_HEATERS = 296;

		public const ushort AQUAFI = 297;

		public const ushort CONNECT_ANYWHERE = 298;

		public const ushort SLIDE_IF_EQUIP = 299;

		public const ushort AWNING_IF_EQUIP = 300;

		public const ushort AWNING_LIGHT_IF_EQUIP = 301;

		public const ushort INTERIOR_LIGHT_IF_EQUIP = 302;

		public const ushort WASTE_VALVE = 303;

		public const ushort TIRE_LINC = 304;

		public const ushort FRONT_LOCKER_LIGHT = 305;

		public const ushort REAR_LOCKER_LIGHT = 306;

		public const ushort REAR_AUX_POWER = 307;

		public const ushort ROCK_LIGHT = 308;

		public const ushort CHASSIS_LIGHT = 309;

		public const ushort EXTERIOR_SHOWER_LIGHT = 310;

		public const ushort LIVING_ROOM_ACCENT_LIGHT = 311;

		public const ushort REAR_FLOOD_LIGHT = 312;

		public const ushort PASSENGER_FLOOD_LIGHT = 313;

		public const ushort DRIVER_FLOOD_LIGHT = 314;

		public const ushort BATHROOM_SLIDE = 315;

		public const ushort ROOF_LIFT = 316;

		public const ushort YETI_PACKAGE = 317;

		public const ushort PROPANE_LOCKER = 318;

		public const ushort GARAGE_AWNING = 319;

		public const ushort MONITOR_PANEL = 320;

		public const ushort CAMERA = 321;

		public const ushort JAYCO_AUS_TBB_GW = 322;

		public const ushort GATEWAY_RVLINK = 323;

		public const ushort ACCESSORY_TEMPERATURE = 324;

		public const ushort ACCESSORY_REFRIGERATOR = 325;

		public const ushort ACCESSORY_FRIDGE = 326;

		public const ushort ACCESSORY_FREEZER = 327;

		public const ushort ACCESSORY_EXTERNAL = 328;

		public const ushort TRAILER_BRAKE_CONTROLLER = 329;

		public const ushort TEMP_REFRIGERATOR = 330;

		public const ushort TEMP_REFRIGERATOR_HOME = 331;

		public const ushort TEMP_FREEZER = 332;

		public const ushort TEMP_FREEZER_HOME = 333;

		public const ushort TEMP_COOLER = 334;

		public const ushort TEMP_KITCHEN = 335;

		public const ushort TEMP_LIVING_ROOM = 336;

		public const ushort TEMP_BEDROOM = 337;

		public const ushort TEMP_MASTER_BEDROOM = 338;

		public const ushort TEMP_GARAGE = 339;

		public const ushort TEMP_BASEMENT = 340;

		public const ushort TEMP_BATHROOM = 341;

		public const ushort TEMP_STORAGE_AREA = 342;

		public const ushort TEMP_DRIVERS_AREA = 343;

		public const ushort TEMP_BUNKS = 344;

		public const ushort LP_TANK_RV = 345;

		public const ushort LP_TANK_HOME = 346;

		public const ushort LP_TANK_CABIN = 347;

		public const ushort LP_TANK_BBQ = 348;

		public const ushort LP_TANK_GRILL = 349;

		public const ushort LP_TANK_SUBMARINE = 350;

		public const ushort LP_TANK_OTHER = 351;

		public const ushort ANTI_LOCK_BRAKING_SYSTEM = 352;

		public const ushort LOCAP_GATEWAY = 353;

		public const ushort BOOTLOADER = 354;

		public const ushort AUXILIARY_BATTERY = 355;

		public const ushort CHASSIS_BATTERY = 356;

		public const ushort HOUSE_BATTERY = 357;

		public const ushort KITCHEN_BATTERY = 358;

		public const ushort ELECTRONIC_SWAY_CONTROL = 359;

		public const ushort JACKS_LIGHTS = 360;

		public const ushort AWNING_SENSOR = 361;

		public const ushort INTERIOR_STEP_LIGHT = 362;

		public const ushort EXTERIOR_STEP_LIGHT = 363;

		public const ushort WIFI_BOOSTER = 364;

		public const ushort AUDIBLE_ALERT = 365;

		public const ushort SOFFIT_LIGHT = 366;

		public const ushort BATTERY_BANK = 367;

		public const ushort RV_BATTERY = 368;

		public const ushort SOLAR_BATTERY = 369;

		public const ushort TONGUE_BATTERY = 370;

		public const ushort AXLE1_BRAKECONTROLLER = 371;

		public const ushort AXLE2_BRAKECONTROLLER = 372;

		public const ushort AXLE3_BRAKECONTROLLER = 373;

		public const ushort LEAD_ACID = 374;

		public const ushort LIQUID_LEAD_ACID = 375;

		public const ushort GEL_LEAD_ACID = 376;

		public const ushort AGM_ABSORBENT_GLASS_MAT = 377;

		public const ushort LITHIUM = 378;

		public const ushort FRONT_AWNING = 379;

		public const ushort DINETTE_SLIDE = 380;

		public const ushort HOLDING_TANKS_HEATER = 381;

		public const ushort INVERTER = 382;

		public const ushort BATTERY_HEAT = 383;

		public const ushort CAMERA_POWER = 384;

		public const ushort PATIO_AWNING_LIGHT = 385;

		public const ushort GARAGE_AWNING_LIGHT = 386;

		public const ushort REAR_AWNING_LIGHT = 387;

		public const ushort SIDE_AWNING_LIGHT = 388;

		public const ushort SLIDE_AWNING_LIGHT = 389;

		public const ushort SLIDE_AWNING = 390;

		public const ushort FRONT_AWNING_LIGHT = 391;

		public const ushort CENTRAL_LIGHTS = 392;

		public const ushort RIGHT_SIDE_LIGHTS = 393;

		public const ushort LEFT_SIDE_LIGHTS = 394;

		public const ushort RIGHT_SCENE_LIGHTS = 395;

		public const ushort LEFT_SCENE_LIGHTS = 396;

		public const ushort REAR_SCENE_LIGHTS = 397;

		public const ushort COMPUTER_FAN = 398;

		public const ushort BATTERY_FAN = 399;

		public const ushort RIGHT_SLIDE_ROOM = 400;

		public const ushort LEFT_SLIDE_ROOM = 401;

		public const ushort DUMP_LIGHT = 402;

		public const ushort BASE_CAMP_TOUCHSCREEN = 403;

		public const ushort BASE_CAMP_LEVELER = 404;

		public const ushort REFRIGERATOR = 405;

		public const ushort KITCHEN_PENDANT_LIGHT = 406;

		public const ushort DOOR_SIDE_SOFA_SLIDE = 407;

		public const ushort OFF_DOOR_SIDE_SOFA_SLIDE = 408;

		public const ushort REAR_BED_SLIDE = 409;

		public const ushort THEATER_LIGHTS = 410;

		public const ushort UTILITY_CABINET_LIGHT = 411;

		public const ushort CHASE_LIGHT = 412;

		public const ushort FLOOR_LIGHTS = 413;

		public const ushort RTT_LIGHT = 414;

		public const ushort UPPER_POWER_SHADES = 415;

		public const ushort LOWER_POWER_SHADES = 416;

		public const ushort LIVING_ROOM_POWER_SHADES = 417;

		public const ushort BEDROOM_POWER_SHADES = 418;

		public const ushort BATHROOM_POWER_SHADES = 419;

		public const ushort BUNK_POWER_SHADES = 420;

		public const ushort LOFT_POWER_SHADES = 421;

		public const ushort FRONT_POWER_SHADES = 422;

		public const ushort REAR_POWER_SHADES = 423;

		public const ushort MAIN_POWER_SHADES = 424;

		public const ushort GARAGE_POWER_SHADES = 425;

		public const ushort DOOR_SIDE_POWER_SHADES = 426;

		public const ushort OFF_DOOR_SIDE_POWER_SHADES = 427;

		public const ushort FRESH_TANK_VALVE = 428;

		public const ushort GREY_TANK_VALVE = 429;

		public const ushort BLACK_TANK_VALVE = 430;

		public const ushort FRONT_BATH_GREY_TANK_VALVE = 431;

		public const ushort FRONT_BATH_FRESH_TANK_VALVE = 432;

		public const ushort FRONT_BATH_BLACK_TANK_VALVE = 433;

		public const ushort REAR_BATH_GREY_TANK_VALVE = 434;

		public const ushort REAR_BATH_FRESH_TANK_VALVE = 435;

		public const ushort REAR_BATH_BLACK_TANK_VALVE = 436;

		public const ushort MAIN_BATH_GREY_TANK_VALVE = 437;

		public const ushort MAIN_BATH_FRESH_TANK_VALVE = 438;

		public const ushort MAIN_BATH_BLACK_TANK_VALVE = 439;

		public const ushort GALLEY_BATH_GREY_TANK_VALVE = 440;

		public const ushort GALLEY_BATH_FRESH_TANK_VALVE = 441;

		public const ushort GALLEY_BATH_BLACK_TANK_VALVE = 442;

		public const ushort KITCHEN_BATH_GREY_TANK_VALVE = 443;

		public const ushort KITCHEN_BATH_FRESH_TANK_VALVE = 444;

		public const ushort KITCHEN_BATH_BLACK_TANK_VALVE = 445;

		public readonly ushort Value;

		public readonly string Name;

		public readonly ICON Icon;

		public bool IsValid => this?.Value > 0;

		public static IEnumerable<FUNCTION_NAME> GetEnumerator()
		{
			return NameList;
		}

		static FUNCTION_NAME()
		{
			Lookup = new Dictionary<ushort, FUNCTION_NAME>();
			NameList = new List<FUNCTION_NAME>();
			UNKNOWN = new FUNCTION_NAME(0, "UNKNOWN", ICON.UNKNOWN);
			new FUNCTION_NAME(1, "Diagnostic Tool", ICON.DIAGNOSTIC_TOOL);
			new FUNCTION_NAME(2, "MyRV Tablet", ICON.TABLET);
			new FUNCTION_NAME(3, "Gas Water Heater", ICON.GAS_WATER_HEATER);
			new FUNCTION_NAME(4, "Electric Water Heater", ICON.ELECTRIC_WATER_HEATER);
			new FUNCTION_NAME(5, "Water Pump", ICON.WATER_PUMP);
			new FUNCTION_NAME(6, "Bath Vent", ICON.VENT);
			new FUNCTION_NAME(7, "Light", ICON.LIGHT);
			new FUNCTION_NAME(8, "Flood Light", ICON.LIGHT);
			new FUNCTION_NAME(9, "Work Light", ICON.LIGHT);
			new FUNCTION_NAME(10, "Front Bedroom Ceiling Light", ICON.LIGHT);
			new FUNCTION_NAME(11, "Front Bedroom Overhead Light", ICON.LIGHT);
			new FUNCTION_NAME(12, "Front Bedroom Vanity Light", ICON.LIGHT);
			new FUNCTION_NAME(13, "Front Bedroom Sconce Light", ICON.LIGHT);
			new FUNCTION_NAME(14, "Front Bedroom Loft Light", ICON.LIGHT);
			new FUNCTION_NAME(15, "Rear Bedroom Ceiling Light", ICON.LIGHT);
			new FUNCTION_NAME(16, "Rear Bedroom Overhead Light", ICON.LIGHT);
			new FUNCTION_NAME(17, "Rear Bedroom Vanity Light", ICON.LIGHT);
			new FUNCTION_NAME(18, "Rear Bedroom Sconce Light", ICON.LIGHT);
			new FUNCTION_NAME(19, "Rear Bedroom Loft Light", ICON.LIGHT);
			new FUNCTION_NAME(20, "Loft Light", ICON.LIGHT);
			new FUNCTION_NAME(21, "Front Hall Light", ICON.LIGHT);
			new FUNCTION_NAME(22, "Rear Hall Light", ICON.LIGHT);
			new FUNCTION_NAME(23, "Front Bathroom Light", ICON.LIGHT);
			new FUNCTION_NAME(24, "Front Bathroom Vanity Light", ICON.LIGHT);
			new FUNCTION_NAME(25, "Front Bathroom Ceiling Light", ICON.LIGHT);
			new FUNCTION_NAME(26, "Front Bathroom Shower Light", ICON.LIGHT);
			new FUNCTION_NAME(27, "Front Bathroom Sconce Light", ICON.LIGHT);
			new FUNCTION_NAME(28, "Rear Bathroom Vanity Light", ICON.LIGHT);
			new FUNCTION_NAME(29, "Rear Bathroom Ceiling Light", ICON.LIGHT);
			new FUNCTION_NAME(30, "Rear Bathroom Shower Light", ICON.LIGHT);
			new FUNCTION_NAME(31, "Rear Bathroom Sconce Light", ICON.LIGHT);
			new FUNCTION_NAME(32, "Kitchen Ceiling Light", ICON.LIGHT);
			new FUNCTION_NAME(33, "Kitchen Sconce Light", ICON.LIGHT);
			new FUNCTION_NAME(34, "Kitchen Pendants Light", ICON.LIGHT);
			new FUNCTION_NAME(35, "Kitchen Range Light", ICON.LIGHT);
			new FUNCTION_NAME(36, "Kitchen Counter Light", ICON.LIGHT);
			new FUNCTION_NAME(37, "Kitchen Bar Light", ICON.LIGHT);
			new FUNCTION_NAME(38, "Kitchen Island Light", ICON.LIGHT);
			new FUNCTION_NAME(39, "Kitchen Chandelier Light", ICON.LIGHT);
			new FUNCTION_NAME(40, "Kitchen Under Cabinet Light", ICON.LIGHT);
			new FUNCTION_NAME(41, "Living Room Ceiling Light", ICON.LIGHT);
			new FUNCTION_NAME(42, "Living Room Sconce Light", ICON.LIGHT);
			new FUNCTION_NAME(43, "Living Room Pendants Light", ICON.LIGHT);
			new FUNCTION_NAME(44, "Living Room Bar Light", ICON.LIGHT);
			new FUNCTION_NAME(45, "Garage Ceiling Light", ICON.LIGHT);
			new FUNCTION_NAME(46, "Garage Cabinet Light", ICON.LIGHT);
			new FUNCTION_NAME(47, "Security Light", ICON.LIGHT);
			new FUNCTION_NAME(48, "Porch Light", ICON.LIGHT);
			new FUNCTION_NAME(49, "Awning Light", ICON.LIGHT);
			new FUNCTION_NAME(50, "Bathroom Light", ICON.LIGHT);
			new FUNCTION_NAME(51, "Bathroom Vanity Light", ICON.LIGHT);
			new FUNCTION_NAME(52, "Bathroom Ceiling Light", ICON.LIGHT);
			new FUNCTION_NAME(53, "Bathroom Shower Light", ICON.LIGHT);
			new FUNCTION_NAME(54, "Bathroom Sconce Light", ICON.LIGHT);
			new FUNCTION_NAME(55, "Hall Light", ICON.LIGHT);
			new FUNCTION_NAME(56, "Bunk Room Light", ICON.LIGHT);
			new FUNCTION_NAME(57, "Bedroom Light", ICON.LIGHT);
			new FUNCTION_NAME(58, "Living Room Light", ICON.LIGHT);
			new FUNCTION_NAME(59, "Kitchen Light", ICON.LIGHT);
			new FUNCTION_NAME(60, "Lounge Light", ICON.LIGHT);
			new FUNCTION_NAME(61, "Ceiling Light", ICON.LIGHT);
			new FUNCTION_NAME(62, "Entry Light", ICON.LIGHT);
			new FUNCTION_NAME(63, "Bed Ceiling Light", ICON.LIGHT);
			new FUNCTION_NAME(64, "Bedroom Lav Light", ICON.LIGHT);
			new FUNCTION_NAME(65, "Shower Light", ICON.LIGHT);
			new FUNCTION_NAME(66, "Galley Light", ICON.LIGHT);
			new FUNCTION_NAME(67, "Fresh Tank", ICON.FRESH_WATER_TANK);
			new FUNCTION_NAME(68, "Grey Tank", ICON.GREY_WATER_TANK);
			new FUNCTION_NAME(69, "Black Tank", ICON.BLACK_WATER_TANK);
			new FUNCTION_NAME(70, "Fuel Tank", ICON.FUEL_TANK);
			new FUNCTION_NAME(71, "Generator Fuel Tank", ICON.FUEL_TANK);
			new FUNCTION_NAME(72, "Auxiliary Fuel Tank", ICON.FUEL_TANK);
			new FUNCTION_NAME(73, "Front Bath Grey Tank", ICON.GREY_WATER_TANK);
			new FUNCTION_NAME(74, "Front Bath Fresh Tank", ICON.FRESH_WATER_TANK);
			new FUNCTION_NAME(75, "Front Bath Black Tank", ICON.BLACK_WATER_TANK);
			new FUNCTION_NAME(76, "Rear Bath Grey Tank", ICON.GREY_WATER_TANK);
			new FUNCTION_NAME(77, "Rear Bath Fresh Tank", ICON.FRESH_WATER_TANK);
			new FUNCTION_NAME(78, "Rear Bath Black Tank", ICON.BLACK_WATER_TANK);
			new FUNCTION_NAME(79, "Main Bath Grey Tank", ICON.GREY_WATER_TANK);
			new FUNCTION_NAME(80, "Main Bath Fresh Tank", ICON.FRESH_WATER_TANK);
			new FUNCTION_NAME(81, "Main Bath Black Tank", ICON.BLACK_WATER_TANK);
			new FUNCTION_NAME(82, "Galley Grey Tank", ICON.GREY_WATER_TANK);
			new FUNCTION_NAME(83, "Galley Fresh Tank", ICON.FRESH_WATER_TANK);
			new FUNCTION_NAME(84, "Galley Black Tank", ICON.BLACK_WATER_TANK);
			new FUNCTION_NAME(85, "Kitchen Grey Tank", ICON.GREY_WATER_TANK);
			new FUNCTION_NAME(86, "Kitchen Fresh Tank", ICON.FRESH_WATER_TANK);
			new FUNCTION_NAME(87, "Kitchen Black Tank", ICON.BLACK_WATER_TANK);
			new FUNCTION_NAME(88, "Landing Gear", ICON.LANDING_GEAR);
			new FUNCTION_NAME(89, "Front Stabilizer", ICON.STABILIZER);
			new FUNCTION_NAME(90, "Rear Stabilizer", ICON.STABILIZER);
			new FUNCTION_NAME(91, "TV Lift", ICON.TV_LIFT);
			new FUNCTION_NAME(92, "Bed Lift", ICON.BED_LIFT);
			new FUNCTION_NAME(93, "Bath Vent Cover", ICON.VENT_COVER);
			new FUNCTION_NAME(94, "Door Lock", ICON.LOCK);
			new FUNCTION_NAME(95, "Generator", ICON.GENERATOR);
			new FUNCTION_NAME(96, "Slide", ICON.SLIDE);
			new FUNCTION_NAME(97, "Main Slide", ICON.SLIDE);
			new FUNCTION_NAME(98, "Bedroom Slide", ICON.SLIDE);
			new FUNCTION_NAME(99, "Galley Slide", ICON.SLIDE);
			new FUNCTION_NAME(100, "Kitchen Slide", ICON.SLIDE);
			new FUNCTION_NAME(101, "Closet Slide", ICON.SLIDE);
			new FUNCTION_NAME(102, "Optional Slide", ICON.SLIDE);
			new FUNCTION_NAME(103, "Door Side Slide", ICON.SLIDE);
			new FUNCTION_NAME(104, "Off-Door Slide", ICON.SLIDE);
			new FUNCTION_NAME(105, "Awning", ICON.AWNING);
			new FUNCTION_NAME(106, "Level Up Leveler", ICON.LEVELER);
			new FUNCTION_NAME(107, "Water Tank Heater", ICON.WATER_TANK_HEATER);
			new FUNCTION_NAME(108, "MyRV Touchscreen", ICON.TOUCHSCREEN_SWITCH);
			new FUNCTION_NAME(109, "Leveler", ICON.LEVELER);
			new FUNCTION_NAME(110, "Vent Cover", ICON.VENT_COVER);
			new FUNCTION_NAME(111, "Front Bedroom Vent Cover", ICON.VENT_COVER);
			new FUNCTION_NAME(112, "Bedroom Vent Cover", ICON.VENT_COVER);
			new FUNCTION_NAME(113, "Front Bath Vent Cover", ICON.VENT_COVER);
			new FUNCTION_NAME(114, "Main Bath Vent Cover", ICON.VENT_COVER);
			new FUNCTION_NAME(115, "Rear Bath Vent Cover", ICON.VENT_COVER);
			new FUNCTION_NAME(116, "Kitchen Vent Cover", ICON.VENT_COVER);
			new FUNCTION_NAME(117, "Living Room Vent Cover", ICON.VENT_COVER);
			new FUNCTION_NAME(118, "4-Leg Truck Camper Leveler", ICON.LEVELER);
			new FUNCTION_NAME(119, "6-Leg Hall Effect EJ Leveler", ICON.LEVELER);
			new FUNCTION_NAME(120, "Patio Light", ICON.LIGHT);
			new FUNCTION_NAME(121, "Hutch Light", ICON.LIGHT);
			new FUNCTION_NAME(122, "Scare Light", ICON.LIGHT);
			new FUNCTION_NAME(123, "Dinette Light", ICON.LIGHT);
			new FUNCTION_NAME(124, "Bar Light", ICON.LIGHT);
			new FUNCTION_NAME(125, "Overhead Light", ICON.LIGHT);
			new FUNCTION_NAME(126, "Overhead Bar Light", ICON.LIGHT);
			new FUNCTION_NAME(127, "Foyer Light", ICON.LIGHT);
			new FUNCTION_NAME(128, "Ramp Door Light", ICON.LIGHT);
			new FUNCTION_NAME(129, "Entertainment Light", ICON.LIGHT);
			new FUNCTION_NAME(130, "Rear Entry Door Light", ICON.LIGHT);
			new FUNCTION_NAME(131, "Ceiling Fan Light", ICON.LIGHT);
			new FUNCTION_NAME(132, "Overhead Fan Light", ICON.LIGHT);
			new FUNCTION_NAME(133, "Bunk Slide", ICON.SLIDE);
			new FUNCTION_NAME(134, "Bed Slide", ICON.SLIDE);
			new FUNCTION_NAME(135, "Wardrobe Slide", ICON.SLIDE);
			new FUNCTION_NAME(136, "Entertainment Slide", ICON.SLIDE);
			new FUNCTION_NAME(137, "Sofa Slide", ICON.SLIDE);
			new FUNCTION_NAME(138, "Patio Awning", ICON.AWNING);
			new FUNCTION_NAME(139, "Rear Awning", ICON.AWNING);
			new FUNCTION_NAME(140, "Side Awning", ICON.AWNING);
			new FUNCTION_NAME(141, "Jacks", ICON.JACKS);
			new FUNCTION_NAME(142, "Leveler", ICON.JACKS);
			new FUNCTION_NAME(143, "Exterior Light", ICON.LIGHT);
			new FUNCTION_NAME(144, "Lower Accent Light", ICON.LIGHT);
			new FUNCTION_NAME(145, "Upper Accent Light", ICON.LIGHT);
			new FUNCTION_NAME(146, "DS Security Light", ICON.LIGHT);
			new FUNCTION_NAME(147, "ODS Security Light", ICON.LIGHT);
			new FUNCTION_NAME(148, "Slide In Slide", ICON.SLIDE);
			new FUNCTION_NAME(149, "Hitch Light", ICON.LIGHT);
			new FUNCTION_NAME(150, "Clock", ICON.CLOCK);
			new FUNCTION_NAME(151, "TV", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(152, "DVD", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(153, "Blu-ray", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(154, "VCR", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(155, "PVR", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(156, "Cable", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(157, "Satellite", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(158, "Audio", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(159, "CD Player", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(160, "Tuner", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(161, "Radio", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(162, "Speakers", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(163, "Game", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(164, "Clock Radio", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(165, "Aux", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(166, "Climate Zone", ICON.HVAC_CONTROL);
			new FUNCTION_NAME(167, "Fireplace", ICON.FIREPLACE);
			new FUNCTION_NAME(168, "Thermostat", ICON.HVAC_CONTROL);
			new FUNCTION_NAME(169, "Front Cap Light", ICON.LIGHT);
			new FUNCTION_NAME(170, "Step Light", ICON.LIGHT);
			new FUNCTION_NAME(171, "DS Flood Light", ICON.LIGHT);
			new FUNCTION_NAME(172, "Interior Light", ICON.LIGHT);
			new FUNCTION_NAME(173, "Fresh Tank Heater", ICON.WATER_TANK_HEATER);
			new FUNCTION_NAME(174, "Grey Tank Heater", ICON.WATER_TANK_HEATER);
			new FUNCTION_NAME(175, "Black Tank Heater", ICON.WATER_TANK_HEATER);
			new FUNCTION_NAME(176, "LP Tank", ICON.GAS_WATER_HEATER);
			new FUNCTION_NAME(177, "Stall Light", ICON.LIGHT);
			new FUNCTION_NAME(178, "Main Light", ICON.LIGHT);
			new FUNCTION_NAME(179, "Bath Light", ICON.LIGHT);
			new FUNCTION_NAME(180, "Bunk Light", ICON.LIGHT);
			new FUNCTION_NAME(181, "Bed Light", ICON.LIGHT);
			new FUNCTION_NAME(182, "Cabinet Light", ICON.LIGHT);
			new FUNCTION_NAME(183, "Network Bridge", ICON.NETWORK_BRIDGE);
			new FUNCTION_NAME(184, "Ethernet Bridge", ICON.NETWORK_BRIDGE);
			new FUNCTION_NAME(185, "WiFi Bridge", ICON.NETWORK_BRIDGE);
			new FUNCTION_NAME(186, "In Transit Power Disconnect", ICON.IPDM);
			new FUNCTION_NAME(187, "Level Up Unity", ICON.LEVELER);
			new FUNCTION_NAME(188, "TT Leveler", ICON.LEVELER);
			new FUNCTION_NAME(189, "Travel Trailer Leveler", ICON.LEVELER);
			new FUNCTION_NAME(190, "Fifth Wheel Leveler", ICON.LEVELER);
			new FUNCTION_NAME(191, "Fuel Pump", ICON.FUEL_PUMP);
			new FUNCTION_NAME(192, "Main Climate Zone", ICON.HVAC_CONTROL);
			new FUNCTION_NAME(193, "Bedroom Climate Zone", ICON.HVAC_CONTROL);
			new FUNCTION_NAME(194, "Garage Climate Zone", ICON.HVAC_CONTROL);
			new FUNCTION_NAME(195, "Compartment Light", ICON.LIGHT);
			new FUNCTION_NAME(196, "Trunk Light", ICON.LIGHT);
			new FUNCTION_NAME(197, "Bar TV", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(198, "Bathroom TV", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(199, "Bedroom TV", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(200, "Bunk Room TV", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(201, "Exterior TV", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(202, "Front Bathroom TV", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(203, "Front Bedroom TV", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(204, "Garage TV", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(205, "Kitchen TV", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(206, "Living Room TV", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(207, "Loft TV", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(208, "Lounge TV", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(209, "Main TV", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(210, "Patio TV", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(211, "Rear Bathroom TV", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(212, "Rear Bedroom TV", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(213, "Bathroom Door Lock", ICON.LOCK);
			new FUNCTION_NAME(214, "Bedroom Door Lock", ICON.LOCK);
			new FUNCTION_NAME(215, "Front Door Lock", ICON.LOCK);
			new FUNCTION_NAME(216, "Garage Door Lock", ICON.LOCK);
			new FUNCTION_NAME(217, "Main Door Lock", ICON.LOCK);
			new FUNCTION_NAME(218, "Patio Door Lock", ICON.LOCK);
			new FUNCTION_NAME(219, "Rear Door Lock", ICON.LOCK);
			new FUNCTION_NAME(220, "Accent Light", ICON.LIGHT);
			new FUNCTION_NAME(221, "Bathroom Accent Light", ICON.LIGHT);
			new FUNCTION_NAME(222, "Bedroom Accent Light", ICON.LIGHT);
			new FUNCTION_NAME(223, "Front Bedroom Accent Light", ICON.LIGHT);
			new FUNCTION_NAME(224, "Garage Accent Light", ICON.LIGHT);
			new FUNCTION_NAME(225, "Kitchen Accent Light", ICON.LIGHT);
			new FUNCTION_NAME(226, "Patio Accent Light", ICON.LIGHT);
			new FUNCTION_NAME(227, "Rear Bedroom Accent Light", ICON.LIGHT);
			new FUNCTION_NAME(228, "Bedroom Radio", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(229, "Bunk Room Radio", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(230, "Exterior Radio", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(231, "Front Bedroom Radio", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(232, "Garage Radio", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(233, "Kitchen Radio", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(234, "Living Room Radio", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(235, "Loft Radio", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(236, "Patio Radio", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(237, "Rear Bedroom Radio", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(238, "Bedroom Entertainment System", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(239, "Bunk Room Entertainment System", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(240, "Entertainment System", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(241, "Exterior Entertainment System", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(242, "Front Bedroom Entertainment System", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(243, "Garage Entertainment System", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(244, "Kitchen Entertainment System", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(245, "Living Room Entertainment System", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(246, "Loft Entertainment System", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(247, "Main Entertainment System", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(248, "Patio Entertainment System", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(249, "Rear Bedroom Entertainment System", ICON.IR_REMOTE_CONTROL);
			new FUNCTION_NAME(250, "Left Stabilizer", ICON.STABILIZER);
			new FUNCTION_NAME(251, "Right Stabilizer", ICON.STABILIZER);
			new FUNCTION_NAME(252, "Stabilizer", ICON.STABILIZER);
			new FUNCTION_NAME(253, "Solar", ICON.POWER_MANAGER);
			new FUNCTION_NAME(254, "Solar Power", ICON.POWER_MANAGER);
			new FUNCTION_NAME(255, "Battery", ICON.POWER_MANAGER);
			new FUNCTION_NAME(256, "Main Battery", ICON.POWER_MANAGER);
			new FUNCTION_NAME(257, "Aux Battery", ICON.POWER_MANAGER);
			new FUNCTION_NAME(258, "Shore Power", ICON.POWER_MANAGER);
			new FUNCTION_NAME(259, "AC Power", ICON.POWER_MANAGER);
			new FUNCTION_NAME(260, "AC Mains", ICON.POWER_MANAGER);
			new FUNCTION_NAME(261, "Aux Power", ICON.POWER_MANAGER);
			new FUNCTION_NAME(262, "Outputs", ICON.POWER_MANAGER);
			new FUNCTION_NAME(263, "Ramp Door", ICON.DOOR);
			new FUNCTION_NAME(264, "Fan", ICON.FAN);
			new FUNCTION_NAME(265, "Bath Fan", ICON.FAN);
			new FUNCTION_NAME(266, "Rear Fan", ICON.FAN);
			new FUNCTION_NAME(267, "Front Fan", ICON.FAN);
			new FUNCTION_NAME(268, "Kitchen Fan", ICON.FAN);
			new FUNCTION_NAME(269, "Ceiling Fan", ICON.FAN);
			new FUNCTION_NAME(270, "Tank Heater", ICON.WATER_TANK_HEATER);
			new FUNCTION_NAME(271, "Front Ceiling Light", ICON.LIGHT);
			new FUNCTION_NAME(272, "Rear Ceiling Light", ICON.LIGHT);
			new FUNCTION_NAME(273, "Cargo Light", ICON.LIGHT);
			new FUNCTION_NAME(274, "Fascia Light", ICON.LIGHT);
			new FUNCTION_NAME(275, "Slide Ceiling Light", ICON.LIGHT);
			new FUNCTION_NAME(276, "Slide Overhead Light", ICON.LIGHT);
			new FUNCTION_NAME(277, "Décor Light", ICON.LIGHT);
			new FUNCTION_NAME(278, "Reading Light", ICON.LIGHT);
			new FUNCTION_NAME(279, "Front Reading Light", ICON.LIGHT);
			new FUNCTION_NAME(280, "Rear Reading Light", ICON.LIGHT);
			new FUNCTION_NAME(281, "Living Room Climate Zone", ICON.HVAC_CONTROL);
			new FUNCTION_NAME(282, "Front Living Room Climate Zone", ICON.HVAC_CONTROL);
			new FUNCTION_NAME(283, "Rear Living Room Climate Zone", ICON.HVAC_CONTROL);
			new FUNCTION_NAME(284, "Front Bedroom Climate Zone", ICON.HVAC_CONTROL);
			new FUNCTION_NAME(285, "Rear Bedroom Climate Zone", ICON.HVAC_CONTROL);
			new FUNCTION_NAME(286, "Bed Tilt", ICON.BED_LIFT);
			new FUNCTION_NAME(287, "Front Bed Tilt", ICON.BED_LIFT);
			new FUNCTION_NAME(288, "Rear Bed Tilt", ICON.BED_LIFT);
			new FUNCTION_NAME(289, "Men's Light", ICON.LIGHT);
			new FUNCTION_NAME(290, "Women's Light", ICON.LIGHT);
			new FUNCTION_NAME(291, "Service Light", ICON.LIGHT);
			new FUNCTION_NAME(292, "ODS Flood Light", ICON.LIGHT);
			new FUNCTION_NAME(293, "Underbody Accent Light", ICON.LIGHT);
			new FUNCTION_NAME(294, "Speaker Light", ICON.LIGHT);
			new FUNCTION_NAME(295, "Water Heater", ICON.WATER_TANK_HEATER);
			new FUNCTION_NAME(296, "Water Heaters", ICON.WATER_TANK_HEATER);
			new FUNCTION_NAME(297, "AquaFi", ICON.GENERIC);
			new FUNCTION_NAME(298, "ConnectAnywhere", ICON.GENERIC);
			new FUNCTION_NAME(299, "Slide {0} (if equip)", ICON.SLIDE);
			new FUNCTION_NAME(300, "Awning {0} (if equip)", ICON.AWNING);
			new FUNCTION_NAME(301, "Awning Light {0} (if equip)", ICON.LIGHT);
			new FUNCTION_NAME(302, "Interior Light {0} (if equip)", ICON.LIGHT);
			new FUNCTION_NAME(303, "Waste valve", ICON.GENERIC);
			new FUNCTION_NAME(304, "Tire Linc", ICON.TPMS);
			new FUNCTION_NAME(305, "Front Locker Light", ICON.LIGHT);
			new FUNCTION_NAME(306, "Rear Locker Light", ICON.LIGHT);
			new FUNCTION_NAME(307, "Rear Aux Power", ICON.POWER_MANAGER);
			new FUNCTION_NAME(308, "Rock Light", ICON.LIGHT);
			new FUNCTION_NAME(309, "Chassis Light", ICON.LIGHT);
			new FUNCTION_NAME(310, "Exterior Shower Light", ICON.LIGHT);
			new FUNCTION_NAME(311, "Living Room Accent Light", ICON.LIGHT);
			new FUNCTION_NAME(312, "Rear Flood Light", ICON.LIGHT);
			new FUNCTION_NAME(313, "Passenger Flood Light", ICON.LIGHT);
			new FUNCTION_NAME(314, "Driver Flood Light", ICON.LIGHT);
			new FUNCTION_NAME(315, "Bathroom Slide", ICON.SLIDE);
			new FUNCTION_NAME(316, "Roof Lift", ICON.SLIDE);
			new FUNCTION_NAME(317, "Yeti Package", ICON.WATER_TANK_HEATER);
			new FUNCTION_NAME(318, "Propane Locker", ICON.WATER_TANK_HEATER);
			new FUNCTION_NAME(319, "Garage Awning", ICON.AWNING);
			new FUNCTION_NAME(320, "Monitor Panel", ICON.TOUCHSCREEN_SWITCH);
			new FUNCTION_NAME(321, "Camera", ICON.GENERIC);
			new FUNCTION_NAME(322, "Jayco Aus TBB GW", ICON.GENERIC);
			new FUNCTION_NAME(323, "GateWay RvLink", ICON.GENERIC);
			new FUNCTION_NAME(324, "Accessory Temperature", ICON.THERMOMETER);
			new FUNCTION_NAME(325, "Accessory Refrigerator", ICON.THERMOMETER);
			new FUNCTION_NAME(326, "Accessory Fridge", ICON.THERMOMETER);
			new FUNCTION_NAME(327, "Accessory Freezer", ICON.THERMOMETER);
			new FUNCTION_NAME(328, "Accessory External", ICON.THERMOMETER);
			new FUNCTION_NAME(329, "Trailer Brake Controller", ICON.GENERIC);
			new FUNCTION_NAME(330, "Refrigerator Temperature", ICON.THERMOMETER);
			new FUNCTION_NAME(331, "Refrigerator Temperature Home", ICON.THERMOMETER);
			new FUNCTION_NAME(332, "Freezer Temperature", ICON.THERMOMETER);
			new FUNCTION_NAME(333, "Freezer Temperature Home", ICON.THERMOMETER);
			new FUNCTION_NAME(334, "Cooler Temperature", ICON.THERMOMETER);
			new FUNCTION_NAME(335, "Kitchen Temperature", ICON.THERMOMETER);
			new FUNCTION_NAME(336, "Living Room Temperature", ICON.THERMOMETER);
			new FUNCTION_NAME(337, "Bedroom Temperature", ICON.THERMOMETER);
			new FUNCTION_NAME(338, "Master Bedroom Temperature", ICON.THERMOMETER);
			new FUNCTION_NAME(339, "Garage Temperature", ICON.GENERIC);
			new FUNCTION_NAME(340, "Basement Temperature", ICON.THERMOMETER);
			new FUNCTION_NAME(341, "Bathroom Temperature", ICON.THERMOMETER);
			new FUNCTION_NAME(342, "Storage Area Temperature", ICON.THERMOMETER);
			new FUNCTION_NAME(343, "Drivers Area Temperature", ICON.THERMOMETER);
			new FUNCTION_NAME(344, "Bunks Temperature", ICON.THERMOMETER);
			new FUNCTION_NAME(345, "RV Tank", ICON.LP_TANK_VALVE);
			new FUNCTION_NAME(346, "Home Tank", ICON.LP_TANK_VALVE);
			new FUNCTION_NAME(347, "Cabin Tank", ICON.LP_TANK_VALVE);
			new FUNCTION_NAME(348, "BBQ Tank", ICON.LP_TANK_VALVE);
			new FUNCTION_NAME(349, "Grill Tank", ICON.LP_TANK_VALVE);
			new FUNCTION_NAME(350, "Submarine Tank", ICON.LP_TANK_VALVE);
			new FUNCTION_NAME(351, "Other Tank", ICON.LP_TANK_VALVE);
			new FUNCTION_NAME(352, "Anti-Lock Braking System", ICON.GENERIC);
			new FUNCTION_NAME(353, "LoCAP Gateway", ICON.GENERIC);
			new FUNCTION_NAME(354, "BootLoader", ICON.GENERIC);
			new FUNCTION_NAME(355, "Auxiliary Battery", ICON.POWER_MONITOR);
			new FUNCTION_NAME(356, "Chassis Battery", ICON.POWER_MONITOR);
			new FUNCTION_NAME(357, "House Battery", ICON.POWER_MONITOR);
			new FUNCTION_NAME(358, "Kitchen Battery", ICON.POWER_MONITOR);
			new FUNCTION_NAME(359, "Electronic Sway Control", ICON.GENERIC);
			new FUNCTION_NAME(360, "Jack Lights", ICON.LIGHT);
			new FUNCTION_NAME(361, "Awning Sensor", ICON.AWNING);
			new FUNCTION_NAME(362, "Interior Step Light", ICON.LIGHT);
			new FUNCTION_NAME(363, "Exterior Step Light", ICON.LIGHT);
			new FUNCTION_NAME(364, "Wifi Booster", ICON.GENERIC);
			new FUNCTION_NAME(365, "Audible Alert", ICON.GENERIC);
			new FUNCTION_NAME(366, "Soffit Light", ICON.LIGHT);
			new FUNCTION_NAME(367, "Battery Bank", ICON.POWER_MANAGER);
			new FUNCTION_NAME(368, "RV Battery", ICON.POWER_MANAGER);
			new FUNCTION_NAME(369, "Solar Battery", ICON.POWER_MANAGER);
			new FUNCTION_NAME(370, "Tongue Battery", ICON.POWER_MANAGER);
			new FUNCTION_NAME(371, "Brake Controller Axle 1", ICON.GENERIC);
			new FUNCTION_NAME(372, "Brake Controller Axle 2", ICON.GENERIC);
			new FUNCTION_NAME(373, "Brake Controller Axle 3", ICON.GENERIC);
			new FUNCTION_NAME(374, "Lead-Acid", ICON.GENERIC);
			new FUNCTION_NAME(375, "Liquid Lead-Acid", ICON.GENERIC);
			new FUNCTION_NAME(376, "Gel Lead-Acid", ICON.GENERIC);
			new FUNCTION_NAME(377, "AGM - Absorbent Glass Mat", ICON.GENERIC);
			new FUNCTION_NAME(378, "Lithium", ICON.GENERIC);
			new FUNCTION_NAME(379, "Front Awning", ICON.AWNING);
			new FUNCTION_NAME(380, "Dinette Slide", ICON.SLIDE);
			new FUNCTION_NAME(381, "Holding Tanks Heater", ICON.GENERIC);
			new FUNCTION_NAME(382, "Inverter", ICON.GENERIC);
			new FUNCTION_NAME(383, "Battery Heat", ICON.THERMOMETER);
			new FUNCTION_NAME(384, "Camera Power", ICON.GENERIC);
			new FUNCTION_NAME(385, "Patio Awning Light", ICON.LIGHT);
			new FUNCTION_NAME(386, "Garage Awning Light", ICON.LIGHT);
			new FUNCTION_NAME(387, "Rear Awning Light", ICON.LIGHT);
			new FUNCTION_NAME(388, "Side Awning Light", ICON.LIGHT);
			new FUNCTION_NAME(389, "Slide Awning Light", ICON.LIGHT);
			new FUNCTION_NAME(390, "Slide Awning", ICON.AWNING);
			new FUNCTION_NAME(391, "Front Awning Light", ICON.LIGHT);
			new FUNCTION_NAME(392, "Central Lights", ICON.LIGHT);
			new FUNCTION_NAME(393, "Right Side Lights", ICON.LIGHT);
			new FUNCTION_NAME(394, "Left Side Lights", ICON.LIGHT);
			new FUNCTION_NAME(395, "Right Scene Lights", ICON.LIGHT);
			new FUNCTION_NAME(396, "Left Scene Lights", ICON.LIGHT);
			new FUNCTION_NAME(397, "Rear Scene Lights", ICON.LIGHT);
			new FUNCTION_NAME(398, "Computer Fan", ICON.FAN);
			new FUNCTION_NAME(399, "Battery Fan", ICON.FAN);
			new FUNCTION_NAME(400, "Right Slide Room", ICON.SLIDE);
			new FUNCTION_NAME(401, "Left Slide Room", ICON.SLIDE);
			new FUNCTION_NAME(402, "Dump Light", ICON.LIGHT);
			new FUNCTION_NAME(403, "Base Camp Touchscreen", ICON.TOUCHSCREEN_SWITCH);
			new FUNCTION_NAME(404, "Base Camp Leveler", ICON.LEVELER);
			new FUNCTION_NAME(405, "Refrigerator", ICON.GENERIC);
			new FUNCTION_NAME(406, "Kitchen Pendant Light", ICON.LIGHT);
			new FUNCTION_NAME(407, "Door Side Sofa Slide", ICON.SLIDE);
			new FUNCTION_NAME(408, "Off Door Side Sofa Slide", ICON.SLIDE);
			new FUNCTION_NAME(409, "Rear Bed Slide", ICON.SLIDE);
			new FUNCTION_NAME(410, "Theater Lights", ICON.LIGHT);
			new FUNCTION_NAME(411, "Utility Cabinet Light", ICON.LIGHT);
			new FUNCTION_NAME(412, "Chase Light", ICON.LIGHT);
			new FUNCTION_NAME(413, "Floor Lights", ICON.LIGHT);
			new FUNCTION_NAME(414, "Roof Top Tent Light", ICON.LIGHT);
			new FUNCTION_NAME(415, "Upper Power Shades", ICON.GENERIC);
			new FUNCTION_NAME(416, "Lower Power Shades", ICON.GENERIC);
			new FUNCTION_NAME(417, "Living Room Power Shades", ICON.GENERIC);
			new FUNCTION_NAME(418, "Bedroom Power Shades", ICON.GENERIC);
			new FUNCTION_NAME(419, "Bathroom Power Shades", ICON.GENERIC);
			new FUNCTION_NAME(420, "Bunk Power Shades", ICON.GENERIC);
			new FUNCTION_NAME(421, "Loft Power Shades", ICON.GENERIC);
			new FUNCTION_NAME(422, "Front Power Shades", ICON.GENERIC);
			new FUNCTION_NAME(423, "Rear Power Shades", ICON.GENERIC);
			new FUNCTION_NAME(424, "Main Power Shades", ICON.GENERIC);
			new FUNCTION_NAME(425, "Garage Power Shades", ICON.GENERIC);
			new FUNCTION_NAME(426, "Door Side Power Shades", ICON.GENERIC);
			new FUNCTION_NAME(427, "Off Door Side Power Shades", ICON.GENERIC);
			new FUNCTION_NAME(428, "Fresh Tank Valve", ICON.FRESH_WATER_TANK);
			new FUNCTION_NAME(429, "Grey Tank Valve", ICON.GREY_WATER_TANK);
			new FUNCTION_NAME(430, "Black Tank Valve", ICON.BLACK_WATER_TANK);
			new FUNCTION_NAME(431, "Front Bath Grey Tank Valve", ICON.GREY_WATER_TANK);
			new FUNCTION_NAME(432, "Front Bath Fresh Tank Valve", ICON.FRESH_WATER_TANK);
			new FUNCTION_NAME(433, "Front Bath Black Tank Valve", ICON.BLACK_WATER_TANK);
			new FUNCTION_NAME(434, "Rear Bath Grey Tank Valve", ICON.GREY_WATER_TANK);
			new FUNCTION_NAME(435, "Rear Bath Fresh Tank Valve", ICON.FRESH_WATER_TANK);
			new FUNCTION_NAME(436, "Rear Bath Black Tank Valve", ICON.BLACK_WATER_TANK);
			new FUNCTION_NAME(437, "Main Bath Grey Tank Valve", ICON.GREY_WATER_TANK);
			new FUNCTION_NAME(438, "Main Bath Fresh Tank Valve", ICON.FRESH_WATER_TANK);
			new FUNCTION_NAME(439, "Main Bath Black Tank Valve", ICON.BLACK_WATER_TANK);
			new FUNCTION_NAME(440, "Galley Grey Tank Valve", ICON.GREY_WATER_TANK);
			new FUNCTION_NAME(441, "Galley Fresh Tank Valve", ICON.FRESH_WATER_TANK);
			new FUNCTION_NAME(442, "Galley Black Tank Valve", ICON.BLACK_WATER_TANK);
			new FUNCTION_NAME(443, "Kitchen Grey Tank Valve", ICON.GREY_WATER_TANK);
			new FUNCTION_NAME(444, "Kitchen Fresh Tank Valve", ICON.FRESH_WATER_TANK);
			new FUNCTION_NAME(445, "Kitchen Black Tank Valve", ICON.BLACK_WATER_TANK);
		}

		private FUNCTION_NAME(ushort value)
			: this(value, "UNKNOWN_" + value.ToString("X4"), ICON.UNKNOWN)
		{
		}

		private FUNCTION_NAME(ushort value, string name, ICON icon)
		{
			Value = value;
			Name = name.Trim();
			Icon = icon;
			if (value > 0)
			{
				Lookup.Add(value, this);
				NameList.Add(this);
			}
		}

		public static implicit operator ushort(FUNCTION_NAME msg)
		{
			return msg?.Value ?? 0;
		}

		public static implicit operator FUNCTION_NAME(ushort value)
		{
			if (Lookup.TryGetValue(value, out var value2))
			{
				return value2;
			}
			if (value == 0)
			{
				return UNKNOWN;
			}
			return new FUNCTION_NAME(value);
		}

		public static implicit operator FUNCTION_NAME(string s)
		{
			s = s.ToUpper().Trim();
			foreach (FUNCTION_NAME item in GetEnumerator())
			{
				if (item.ToString().ToUpper() == s)
				{
					return item;
				}
			}
			return UNKNOWN;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
