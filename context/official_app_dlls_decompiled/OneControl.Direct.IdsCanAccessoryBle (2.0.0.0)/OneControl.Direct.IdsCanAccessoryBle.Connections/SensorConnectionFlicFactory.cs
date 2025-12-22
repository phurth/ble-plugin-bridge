using System;
using System.Collections.Generic;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Direct.IdsCanAccessoryBle.FlicButton;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;

namespace OneControl.Direct.IdsCanAccessoryBle.Connections
{
	public class SensorConnectionFlicFactory : ISensorConnectionFactory
	{
		public const string LogTag = "SensorConnectionFlicFactory";

		public DEVICE_TYPE DeviceType => (byte)60;

		public bool IsStandardSource => true;

		public Type SensorConnectionType => typeof(SensorConnectionFlic);

		public ILogicalDeviceSourceDirect DeviceSource => FlicDeviceSource;

		protected IFlicButtonBleDeviceSource FlicDeviceSource { get; }

		public IEnumerable<ISensorConnection> SensorConnectionsAll => FlicDeviceSource.SensorConnectionsAll;

		public SensorConnectionFlicFactory(IFlicButtonBleDeviceSource flicButtonBleDeviceSource)
		{
			IFlicButtonBleDeviceSource flicButtonBleDeviceSource2 = flicButtonBleDeviceSource;
			base._002Ector();
			FlicDeviceSource = flicButtonBleDeviceSource2;
			Singleton<AccessoryRegistrationManager>.Instance.FlicButtonBleDeviceSource = flicButtonBleDeviceSource2;
			Resolver<IFlicButtonBleDeviceSource>.LazyConstructAndRegister(() => flicButtonBleDeviceSource2);
		}

		public (ISensorConnection? SensorConnection, bool NewRegistration) TryAddSensorConnection(IAccessoryScanResult accessoryScanResult)
		{
			MAC accessoryMacAddress = accessoryScanResult.AccessoryMacAddress;
			if ((object)accessoryMacAddress == null)
			{
				return (null, false);
			}
			IdsCanAccessoryStatus? accessoryStatus = accessoryScanResult.GetAccessoryStatus(accessoryMacAddress);
			if (!accessoryStatus.HasValue)
			{
				return (null, false);
			}
			if (accessoryStatus.Value.DeviceType != DeviceType)
			{
				return (null, false);
			}
			SensorConnectionFlic sensorConnectionFlic = new SensorConnectionFlic(accessoryScanResult.DeviceName, accessoryScanResult.DeviceId, accessoryScanResult.AccessoryMacAddress, accessoryScanResult.SoftwarePartNumber ?? string.Empty);
			bool flag = TryAddSensorConnection(sensorConnectionFlic);
			return (sensorConnectionFlic, flag);
		}

		public bool TryAddSensorConnection(ISensorConnection sensorConnection)
		{
			if (!(sensorConnection is SensorConnectionFlic sensorConnection2))
			{
				return false;
			}
			return FlicDeviceSource.RegisterSensor(sensorConnection2);
		}

		public bool TryRemoveSensorConnection(ISensorConnection sensorConnection)
		{
			if (!(sensorConnection is SensorConnectionFlic sensorConnectionFlic))
			{
				return false;
			}
			if (!FlicDeviceSource.IsSensorRegistered(sensorConnectionFlic.ConnectionGuid))
			{
				return false;
			}
			FlicDeviceSource.UnRegisterSensor(sensorConnectionFlic.ConnectionGuid);
			return true;
		}
	}
}
