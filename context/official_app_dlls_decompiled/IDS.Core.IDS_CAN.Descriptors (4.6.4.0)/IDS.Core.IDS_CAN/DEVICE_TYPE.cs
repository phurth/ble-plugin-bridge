using System.Collections.Generic;

namespace IDS.Core.IDS_CAN
{
	public sealed class DEVICE_TYPE
	{
		private static readonly DEVICE_TYPE[] Table;

		private static readonly List<DEVICE_TYPE> List;

		public const byte UNKNOWN = 0;

		public const byte GENERIC = 1;

		public const byte TABLET = 2;

		public const byte LATCHING_RELAY = 3;

		public const byte MOMENTARY_RELAY = 4;

		public const byte LATCHING_H_BRIDGE = 5;

		public const byte MOMENTARY_H_BRIDGE = 6;

		public const byte LEVELER_TYPE_1 = 7;

		public const byte SWITCH = 8;

		public const byte TOUCHSCREEN_SWITCH = 9;

		public const byte TANK_SENSOR = 10;

		public const byte LEVELER_TYPE_2 = 11;

		public const byte HOUR_METER = 12;

		public const byte RGB_LIGHT = 13;

		public const byte REAL_TIME_CLOCK = 14;

		public const byte IR_REMOTE_CONTROL = 15;

		public const byte HVAC_CONTROL = 16;

		public const byte LEVELER_TYPE_3 = 17;

		public const byte CAN_TO_ETHERNET_GATEWAY = 18;

		public const byte IN_TRANSIT_POWER_DISCONNECT = 19;

		public const byte DIMMABLE_LIGHT = 20;

		public const byte ONECONTROL_TOUCH_PAD = 21;

		public const byte ANDROID_MOBILE_DEVICE = 22;

		public const byte IOS_MOBILE_DEVICE = 23;

		public const byte GENERATOR_GENIE = 24;

		public const byte TEMPERATURE_SENSOR = 25;

		public const byte AC_POWER_MONITOR = 26;

		public const byte DC_POWER_MONITOR = 27;

		public const byte SETEC_POWER_MANAGER = 28;

		public const byte ONECONTROL_CLOUD_GATEWAY = 29;

		public const byte LATCHING_RELAY_TYPE_2 = 30;

		public const byte MOMENTARY_RELAY_TYPE_2 = 31;

		public const byte LATCHING_H_BRIDGE_TYPE_2 = 32;

		public const byte MOMENTARY_H_BRIDGE_TYPE_2 = 33;

		public const byte ONECONTROL_APPLICATION = 34;

		public const byte CONFIGURATOR_APPLICATION = 35;

		public const byte BLUETOOTH_GATEWAY = 36;

		public const byte MAXX_FAN = 37;

		public const byte RAIN_SENSOR = 38;

		public const byte CHASSIS_INFO = 39;

		public const byte LEVELER_TYPE_4 = 40;

		public const byte WIFI_GATEWAY = 41;

		public const byte TPMS_TIRE_LINC = 42;

		public const byte MONITOR_PANEL = 43;

		public const byte ACCESSORY_GATEWAY = 44;

		public const byte CAMERA = 45;

		public const byte JAYCO_AUS_TBB_GW = 46;

		public const byte AWNING_SENSOR = 47;

		public const byte BRAKE_CONTROLLER = 48;

		public const byte BATTERY_MONITOR = 49;

		public const byte REFLASH_BOOTLOADER = 50;

		public const byte DOOR_LOCK = 51;

		public const byte AUDIBLE_ALERT = 52;

		public const byte ECHO_BRAKE_CONTROL = 53;

		public const byte OCTP_WITH_RVLINK = 54;

		public const byte ANGLE_SENSOR = 55;

		public const byte LEVELER_TYPE_5 = 56;

		public const byte BASECAMP_TOUCHPAD = 57;

		public const byte ELECTRIC_MECHANICAL_BRAKE_CONTROLLER = 58;

		public const byte HEADLESS_STEREO = 59;

		public const byte BUTTON_FLIC = 60;

		public const byte RGBW_LIGHT = 61;

		public const byte HIL_TEST_BENCH = 62;

		public readonly byte Value;

		public readonly string Name;

		public readonly ICON Icon;

		public bool IsValid => this?.Value > 0;

		public static IEnumerable<DEVICE_TYPE> GetEnumerator()
		{
			return List;
		}

		static DEVICE_TYPE()
		{
			Table = new DEVICE_TYPE[256];
			List = new List<DEVICE_TYPE>();
			new DEVICE_TYPE(0, "UNKNOWN", ICON.UNKNOWN);
			new DEVICE_TYPE(1, "Generic Device", ICON.GENERIC);
			new DEVICE_TYPE(2, "Tablet", ICON.TABLET);
			new DEVICE_TYPE(3, "Latching Relay (type 1)", ICON.LATCHING_RELAY);
			new DEVICE_TYPE(4, "Momentary Relay (type 1)", ICON.MOMENTARY_RELAY);
			new DEVICE_TYPE(5, "Latching H-Bridge (type 1)", ICON.LATCHING_H_BRIDGE);
			new DEVICE_TYPE(6, "Momentary H-Bridge (type 1)", ICON.MOMENTARY_H_BRIDGE);
			new DEVICE_TYPE(7, "Leveler Type 1", ICON.LEVELER);
			new DEVICE_TYPE(8, "Switch", ICON.SWITCH);
			new DEVICE_TYPE(9, "Touchscreen Switch", ICON.TOUCHSCREEN_SWITCH);
			new DEVICE_TYPE(10, "Tank Sensor", ICON.TANK_SENSOR);
			new DEVICE_TYPE(11, "Leveler Type 2", ICON.JACKS);
			new DEVICE_TYPE(12, "Hour Meter", ICON.HOUR_METER);
			new DEVICE_TYPE(13, "RGB Light", ICON.RGB_LIGHT);
			new DEVICE_TYPE(14, "Clock", ICON.CLOCK);
			new DEVICE_TYPE(15, "Infrared Remote Control", ICON.IR_REMOTE_CONTROL);
			new DEVICE_TYPE(16, "HVAC Control", ICON.HVAC_CONTROL);
			new DEVICE_TYPE(17, "Leveler Type 3", ICON.LEVELER);
			new DEVICE_TYPE(18, "CAN to Ethernet Gateway", ICON.NETWORK_BRIDGE);
			new DEVICE_TYPE(19, "In Transit Power Disconnect", ICON.IPDM);
			new DEVICE_TYPE(20, "Dimmable Light", ICON.DIMMABLE_LIGHT);
			new DEVICE_TYPE(21, "OneControl Touch Pad", ICON.OCTP);
			new DEVICE_TYPE(22, "Android Mobile Device", ICON.ANDROID);
			new DEVICE_TYPE(23, "iOS Mobile Device", ICON.IOS);
			new DEVICE_TYPE(24, "Generator Genie", ICON.GENERATOR);
			new DEVICE_TYPE(25, "Temperature Sensor", ICON.THERMOMETER);
			new DEVICE_TYPE(26, "AC Power Monitor", ICON.POWER_MONITOR);
			new DEVICE_TYPE(27, "DC Power Monitor", ICON.POWER_MONITOR);
			new DEVICE_TYPE(28, "Power Manager", ICON.POWER_MANAGER);
			new DEVICE_TYPE(29, "OneControl Cloud Gateway", ICON.CLOUD);
			new DEVICE_TYPE(30, "Latching Relay (type 2)", ICON.LATCHING_RELAY);
			new DEVICE_TYPE(31, "Momentary Relay (type 2)", ICON.MOMENTARY_RELAY);
			new DEVICE_TYPE(32, "Latching H-Bridge (type 2)", ICON.LATCHING_H_BRIDGE);
			new DEVICE_TYPE(33, "Momentary H-Bridge (type 2)", ICON.MOMENTARY_H_BRIDGE);
			new DEVICE_TYPE(34, "OneControl App", ICON.DIAGNOSTIC_TOOL);
			new DEVICE_TYPE(35, "Configurator App", ICON.DIAGNOSTIC_TOOL);
			new DEVICE_TYPE(36, "Bluetooth Gateway", ICON.BLUETOOTH);
			new DEVICE_TYPE(37, "MaxxFan", ICON.VENT_COVER);
			new DEVICE_TYPE(38, "Rain Sensor", ICON.RAIN_SENSOR);
			new DEVICE_TYPE(39, "Chassis Info", ICON.CHASSIS);
			new DEVICE_TYPE(40, "Leveler Type 4", ICON.LEVELER);
			new DEVICE_TYPE(41, "WiFi Gateway", ICON.GENERIC);
			new DEVICE_TYPE(42, "TPMS Tire Linc", ICON.TPMS);
			new DEVICE_TYPE(43, "Monitor Panel", ICON.GENERIC);
			new DEVICE_TYPE(44, "Accessory Gateway", ICON.TABLET);
			new DEVICE_TYPE(45, "Camera", ICON.GENERIC);
			new DEVICE_TYPE(46, "Jayco Aus TBB GW", ICON.GENERIC);
			new DEVICE_TYPE(47, "Awning Sensor", ICON.AWNING);
			new DEVICE_TYPE(48, "Brake controller", ICON.GENERIC);
			new DEVICE_TYPE(49, "Battery Monitor", ICON.GENERIC);
			new DEVICE_TYPE(50, "ReFlash Bootloader", ICON.GENERIC);
			new DEVICE_TYPE(51, "Door Lock", ICON.LOCK);
			new DEVICE_TYPE(52, "Audible Alert", ICON.GENERIC);
			new DEVICE_TYPE(53, "Echo Break Controller", ICON.GENERIC);
			new DEVICE_TYPE(54, "OCTP RvLink", ICON.OCTP);
			new DEVICE_TYPE(55, "Angle Sensor", ICON.GENERIC);
			new DEVICE_TYPE(56, "Leveler Type 5", ICON.LEVELER);
			new DEVICE_TYPE(57, "Basecamp Touchpad", ICON.GENERIC);
			new DEVICE_TYPE(58, "Electric Mechanical Brake Controller", ICON.GENERIC);
			new DEVICE_TYPE(59, "Headless Stereo", ICON.GENERIC);
			new DEVICE_TYPE(60, "Button Flic", ICON.GENERIC);
			new DEVICE_TYPE(61, "RGBW Light", ICON.RGB_LIGHT);
			new DEVICE_TYPE(62, "HIL Test Bench", ICON.GENERIC);
		}

		private DEVICE_TYPE(byte value)
			: this(value, "UNKNOWN_" + value.ToString("X2"), ICON.UNKNOWN)
		{
		}

		private DEVICE_TYPE(byte value, string name, ICON icon)
		{
			Name = name.Trim();
			Value = value;
			Icon = icon;
			List.Add(this);
			Table[value] = this;
		}

		public static implicit operator byte(DEVICE_TYPE msg)
		{
			return msg.Value;
		}

		public static implicit operator DEVICE_TYPE(byte value)
		{
			DEVICE_TYPE dEVICE_TYPE = Table[value];
			if (dEVICE_TYPE != null)
			{
				return dEVICE_TYPE;
			}
			if (value == 0)
			{
				return (byte)0;
			}
			return new DEVICE_TYPE(value);
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
