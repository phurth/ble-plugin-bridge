using System;
using System.Collections.Generic;
using System.Linq;
using IDS.Core.IDS_CAN;
using ids.portable.common;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public sealed class DeviceCategory : IEquatable<DeviceCategory>, IComparable<DeviceCategory>
	{
		public enum AppType
		{
			App,
			Awning,
			BedLift,
			Doors,
			DoorLock,
			Fan,
			Generator,
			Hvac,
			IrRemote,
			LandingGear,
			Leveler,
			Light,
			Router,
			Slide,
			Stabilizer,
			Tank,
			TireMonitor,
			TvLift,
			VentCover,
			MonitorPanel,
			Camera,
			PowerMonitor,
			TemperatureSensor,
			AwningSensor,
			LiquidPropane,
			SafetySystems,
			NetworkBridges,
			TextDevices,
			Unknown
		}

		public enum AppFunction
		{
			App,
			Fan,
			Hvac,
			IrRemote,
			Leveler,
			Light,
			RelayHBridge,
			Router,
			TankMonitor,
			TireMonitor,
			MonitorPanel,
			Camera,
			PowerMonitor,
			TemperatureSensor,
			AwningSensor,
			LiquidPropane,
			SafetySystems,
			NetworkBridges,
			TextDevices,
			DoorLock,
			Unknown
		}

		private readonly string _rawName;

		public static readonly DeviceCategory Awning = new DeviceCategory(AppType.Awning, AppFunction.RelayHBridge, FUNCTION_CLASS.AWNING, (DEVICE_TYPE deviceType, FUNCTION_NAME functionName) => Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.AWNING));

		public static readonly DeviceCategory Fan = new DeviceCategory(AppType.Fan, AppFunction.Fan, FUNCTION_CLASS.FAN, (DEVICE_TYPE deviceType, FUNCTION_NAME functionName) => ((Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.VENT) || Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.FAN)) && IsRelayBasicLatching(deviceType)) || (byte)deviceType == 37);

		public static readonly DeviceCategory Bedlift = new DeviceCategory(AppType.BedLift, AppFunction.RelayHBridge, "Bed Lifts", (DEVICE_TYPE deviceType, FUNCTION_NAME functionName) => Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.LIFT) && functionName.Name.Contains("Bed") && IsRelayHBridgeMomentary(deviceType));

		public static readonly DeviceCategory MonitorPanel = new DeviceCategory(AppType.MonitorPanel, AppFunction.MonitorPanel, FUNCTION_CLASS.MonitorPanel, (DEVICE_TYPE deviceType, FUNCTION_NAME functionName) => Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.MonitorPanel));

		public static readonly DeviceCategory Camera = new DeviceCategory(AppType.Camera, AppFunction.Camera, FUNCTION_CLASS.Camera, (DEVICE_TYPE deviceType, FUNCTION_NAME functionName) => Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.Camera));

		public static readonly DeviceCategory PowerMonitor = new DeviceCategory(AppType.PowerMonitor, AppFunction.PowerMonitor, FUNCTION_CLASS.POWER, IsPowerMonitor);

		public static readonly DeviceCategory TemperatureSensor = new DeviceCategory(AppType.TemperatureSensor, AppFunction.TemperatureSensor, FUNCTION_CLASS.TEMPERATURE_SENSOR, (DEVICE_TYPE deviceType, FUNCTION_NAME functionName) => IsTemperatureSensor(deviceType));

		public static readonly DeviceCategory AwningSensor = new DeviceCategory(AppType.AwningSensor, AppFunction.AwningSensor, FUNCTION_CLASS.AwningSensor, (DEVICE_TYPE deviceType, FUNCTION_NAME functionName) => IsAwningSensor(deviceType));

		public static readonly DeviceCategory Doors = new DeviceCategory(AppType.Doors, AppFunction.RelayHBridge, FUNCTION_CLASS.DOOR, (DEVICE_TYPE deviceType, FUNCTION_NAME functionName) => (Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.DOOR) || Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.LOCK)) && IsRelayHBridgeMomentary(deviceType));

		public static readonly DeviceCategory DoorLock = new DeviceCategory(AppType.DoorLock, AppFunction.DoorLock, FUNCTION_CLASS.LOCK, (DEVICE_TYPE deviceType, FUNCTION_NAME functionName) => Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.LOCK) && IsDoorLock(deviceType));

		public static readonly DeviceCategory Generator = new DeviceCategory(AppType.Generator, AppFunction.RelayHBridge, FUNCTION_CLASS.GENERATOR, (DEVICE_TYPE deviceType, FUNCTION_NAME functionName) => Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.GENERATOR) && (IsRelayHBridgeMomentary(deviceType) || (byte)deviceType == 24));

		public static readonly DeviceCategory Hvac = new DeviceCategory(AppType.Hvac, AppFunction.Hvac, FUNCTION_CLASS.HVAC_CONTROL, (DEVICE_TYPE deviceType, FUNCTION_NAME functionName) => Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.HVAC_CONTROL));

		public static readonly DeviceCategory IrRemote = new DeviceCategory(AppType.IrRemote, AppFunction.IrRemote, FUNCTION_CLASS.IR_REMOTE_CONTROL, (DEVICE_TYPE deviceType, FUNCTION_NAME functionName) => Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.IR_REMOTE_CONTROL) && Platform.IsPackageInstalled(IrRemotePackage));

		public static readonly DeviceCategory LandingGear = new DeviceCategory(AppType.LandingGear, AppFunction.RelayHBridge, FUNCTION_CLASS.LANDING_GEAR, (DEVICE_TYPE deviceType, FUNCTION_NAME functionName) => Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.LANDING_GEAR) && IsRelayHBridgeMomentary(deviceType));

		public static readonly DeviceCategory Leveler = new DeviceCategory(AppType.Leveler, AppFunction.Leveler, "Leveling", (DEVICE_TYPE deviceType, FUNCTION_NAME functionName) => Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.LEVELER) && ((byte)deviceType == 7 || (byte)deviceType == 17 || (byte)deviceType == 40 || (byte)deviceType == 56));

		public static readonly DeviceCategory Light = new DeviceCategory(AppType.Light, AppFunction.Light, "Lighting", delegate(DEVICE_TYPE deviceType, FUNCTION_NAME functionName)
		{
			if ((byte)deviceType == 13 || (byte)deviceType == 20)
			{
				return true;
			}
			return IsRelayBasicLatching(deviceType) && Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.LIGHT);
		});

		public static readonly DeviceCategory MyRV = new DeviceCategory(AppType.App, AppFunction.App, "MyRV", (DEVICE_TYPE deviceType, FUNCTION_NAME functionName) => false);

		public static readonly DeviceCategory Slide = new DeviceCategory(AppType.Slide, AppFunction.RelayHBridge, "Slides", (DEVICE_TYPE deviceType, FUNCTION_NAME functionName) => Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.SLIDE) || (Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.LIFT) && (ushort)functionName == 316 && IsRelayHBridgeMomentary(deviceType)));

		public static readonly DeviceCategory Stabilizer = new DeviceCategory(AppType.Stabilizer, AppFunction.RelayHBridge, "Stabilizers", (DEVICE_TYPE deviceType, FUNCTION_NAME functionName) => Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.STABILIZER) && IsRelayHBridgeMomentary(deviceType));

		public static readonly DeviceCategory Tank = new DeviceCategory(AppType.Tank, AppFunction.TankMonitor, FUNCTION_CLASS.MonitorPanel, (DEVICE_TYPE deviceType, FUNCTION_NAME functionName) => Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.TANK_HEATER) || Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.WATER_HEATER) || Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.PUMP) || (Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.VALVE) && IsRelayBasicLatching(deviceType)) || (Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.TANK) && (byte)deviceType == 10));

		public static readonly DeviceCategory TvLift = new DeviceCategory(AppType.TvLift, AppFunction.RelayHBridge, "TV Lifts", (DEVICE_TYPE deviceType, FUNCTION_NAME functionName) => Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.LIFT) && functionName.Name.Contains("TV") && IsRelayHBridgeMomentary(deviceType));

		public static readonly DeviceCategory Unknown = new DeviceCategory(AppType.Unknown, AppFunction.Unknown, "Unknown", (DEVICE_TYPE deviceType, FUNCTION_NAME functionName) => Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.UNKNOWN) || Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.MISCELLANEOUS) || (byte)deviceType == 0);

		public static readonly DeviceCategory VentCover = new DeviceCategory(AppType.VentCover, AppFunction.RelayHBridge, "Vent Covers", (DEVICE_TYPE deviceType, FUNCTION_NAME functionName) => Enumerable.Contains(functionName.GetFunctionClasses(), FUNCTION_CLASS.VENT_COVER) && IsRelayHBridgeMomentary(deviceType));

		public static readonly DeviceCategory Router = new DeviceCategory(AppType.Router, AppFunction.Router, "Router", (DEVICE_TYPE deviceType, FUNCTION_NAME functionName) => FunctionClassExtension.Get(deviceType, functionName) == FUNCTION_CLASS.Router);

		public static readonly DeviceCategory LiquidPropane = new DeviceCategory(AppType.LiquidPropane, AppFunction.LiquidPropane, FUNCTION_CLASS.LiquidPropane, (DEVICE_TYPE deviceType, FUNCTION_NAME functionName) => (byte)deviceType == 10 && FunctionClassExtension.Get(deviceType, functionName) == FUNCTION_CLASS.LiquidPropane);

		public static readonly DeviceCategory SafetySystems = new DeviceCategory(AppType.SafetySystems, AppFunction.SafetySystems, FUNCTION_CLASS.SafetySystems, (DEVICE_TYPE deviceType, FUNCTION_NAME functionName) => ((byte)deviceType == 48 && FunctionClassExtension.Get(deviceType, functionName) == FUNCTION_CLASS.SafetySystems) || ((byte)deviceType == 42 && FunctionClassExtension.Get(deviceType, functionName) == FUNCTION_CLASS.TireMonitor));

		public static readonly DeviceCategory NetworkBridges = new DeviceCategory(AppType.NetworkBridges, AppFunction.NetworkBridges, FUNCTION_CLASS.NETWORK_BRIDGE, (DEVICE_TYPE deviceType, FUNCTION_NAME functionName) => FunctionClassExtension.Get(deviceType, functionName) == FUNCTION_CLASS.NETWORK_BRIDGE);

		public static readonly DeviceCategory TextDevices = new DeviceCategory(AppType.TextDevices, AppFunction.TextDevices, FUNCTION_CLASS.Text, (DEVICE_TYPE deviceType, FUNCTION_NAME functionName) => FunctionClassExtension.Get(deviceType, functionName) == FUNCTION_CLASS.Text);

		public static string IrRemotePackage => "com.idselectronics.linctab.app_remote";

		public static string MyRVPackage => "com.idselectronics.linctab.my_rv";

		public AppType Type { get; }

		public AppFunction Function { get; }

		public string Name => CommonLocalization.Localize(_rawName);

		public Func<DEVICE_TYPE, FUNCTION_NAME, bool> IsDeviceCategory { get; private set; }

		public string Icon { get; }

		public static List<DeviceCategory> SupportedApps { get; } = new List<DeviceCategory>
		{
			Awning, Bedlift, Doors, DoorLock, Fan, Generator, Hvac, IrRemote, LandingGear, Leveler,
			Light, MyRV, Router, Slide, Stabilizer, Tank, TvLift, VentCover, MonitorPanel, Camera,
			PowerMonitor, TemperatureSensor, AwningSensor, LiquidPropane, SafetySystems, NetworkBridges, TextDevices
		};


		private DeviceCategory(AppType type, AppFunction function, FUNCTION_CLASS functionClass, Func<DEVICE_TYPE, FUNCTION_NAME, bool> predicate)
			: this(type, function, functionClass.GetName(), predicate)
		{
		}

		private DeviceCategory(AppType type, AppFunction function, string name, Func<DEVICE_TYPE, FUNCTION_NAME, bool> predicate)
		{
			Type = type;
			Function = function;
			_rawName = name;
			IsDeviceCategory = predicate;
			Icon = "ic_app_" + Name.ToLower().Replace(" ", "_");
		}

		public static AppType GetSupportedDeepLinkAppType(AppType appType)
		{
			return appType switch
			{
				AppType.TireMonitor => AppType.SafetySystems, 
				AppType.Tank => AppType.LiquidPropane, 
				_ => appType, 
			};
		}

		private static bool IsPowerMonitor(DEVICE_TYPE deviceType, FUNCTION_NAME functionName)
		{
			if ((byte)deviceType != 46 && (byte)deviceType != 49)
			{
				return FunctionClassExtension.Get(deviceType, functionName) == FUNCTION_CLASS.POWER;
			}
			return true;
		}

		public static bool IsTemperatureSensor(DEVICE_TYPE deviceType)
		{
			return (byte)deviceType == 25;
		}

		public static bool IsAwningSensor(DEVICE_TYPE deviceType)
		{
			return (byte)deviceType == 47;
		}

		public static bool IsRelayHBridgeMomentary(DEVICE_TYPE deviceType)
		{
			if ((byte)deviceType != 6)
			{
				return (byte)deviceType == 33;
			}
			return true;
		}

		public static bool IsDoorLock(DEVICE_TYPE deviceType)
		{
			return (byte)deviceType == 51;
		}

		public static bool IsRelayBasicLatching(DEVICE_TYPE deviceType)
		{
			if ((byte)deviceType != 3)
			{
				return (byte)deviceType == 30;
			}
			return true;
		}

		public static DeviceCategory GetDeviceCategory(IDevice device)
		{
			foreach (DeviceCategory supportedApp in SupportedApps)
			{
				if (supportedApp.IsDeviceCategory(device.DeviceType, device.FunctionName))
				{
					return supportedApp;
				}
			}
			return Unknown;
		}

		public static DeviceCategory GetDeviceCategory(DEVICE_ID deviceId)
		{
			foreach (DeviceCategory supportedApp in SupportedApps)
			{
				if (supportedApp.IsDeviceCategory(deviceId.DeviceType, deviceId.FunctionName))
				{
					return supportedApp;
				}
			}
			return Unknown;
		}

		public static DeviceCategory GetDeviceCategory(ILogicalDeviceId? logicalDeviceId)
		{
			if (logicalDeviceId == null)
			{
				return Unknown;
			}
			foreach (DeviceCategory supportedApp in SupportedApps)
			{
				if (supportedApp.IsDeviceCategory(logicalDeviceId!.DeviceType, logicalDeviceId!.FunctionName))
				{
					return supportedApp;
				}
			}
			return Unknown;
		}

		public static DeviceCategory? GetDeviceCategory(string name)
		{
			string name2 = name;
			using (IEnumerator<DeviceCategory> enumerator = Enumerable.Where(SupportedApps, (DeviceCategory category) => category.Name.Equals(name2)).GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					return enumerator.Current;
				}
			}
			return Unknown;
		}

		public bool Equals(DeviceCategory other)
		{
			return Name == other.Name;
		}

		public int CompareTo(DeviceCategory other)
		{
			if (Name.Contains("MyRV") && !other.Name.Contains("MyRV"))
			{
				return -1;
			}
			if (other.Name.Contains("MyRV") && !Name.Contains("MyRV"))
			{
				return 1;
			}
			return string.Compare(Name, other.Name, StringComparison.Ordinal);
		}
	}
}
