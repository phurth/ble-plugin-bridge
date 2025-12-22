using System;
using System.Collections.Generic;
using System.Linq;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;

namespace OneControl.Direct.IdsCanAccessoryBle.Connections
{
	public abstract class SensorConnectionFactoryLocap<TSensorConnection, TDeviceSourceInterface> : ISensorConnectionFactory where TSensorConnection : ISensorConnectionBleLocap where TDeviceSourceInterface : IAccessoryBleDeviceSource, IAccessoryBleDeviceSource<TSensorConnection>
	{
		public const string LogTag = "SensorConnectionFactoryLocap";

		public abstract DEVICE_TYPE DeviceType { get; }

		public abstract bool IsStandardSource { get; }

		public Type SensorConnectionType => typeof(TSensorConnection);

		public ILogicalDeviceSourceDirect DeviceSource => LocapBleDeviceSource;

		protected TDeviceSourceInterface LocapBleDeviceSource { get; }

		public IEnumerable<ISensorConnection> SensorConnectionsAll
		{
			get
			{
				TDeviceSourceInterface locapBleDeviceSource = LocapBleDeviceSource;
				return ((locapBleDeviceSource != null) ? ((IAccessoryBleDeviceSource)locapBleDeviceSource).SensorConnectionsAll : null) ?? Enumerable.Empty<ISensorConnection>();
			}
		}

		public SensorConnectionFactoryLocap(TDeviceSourceInterface locapBleDeviceSource)
		{
			LocapBleDeviceSource = locapBleDeviceSource;
			Resolver<TDeviceSourceInterface>.LazyConstructAndRegister(() => LocapBleDeviceSource);
		}

		public abstract TSensorConnection MakeSensorConnection(string connectionNameFriendly, Guid connectionGuid, MAC accessoryMac, string softwarePartNumber);

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
			TSensorConnection val = MakeSensorConnection(accessoryScanResult.DeviceName, accessoryScanResult.DeviceId, accessoryScanResult.AccessoryMacAddress, accessoryScanResult.SoftwarePartNumber ?? string.Empty);
			bool flag = TryAddSensorConnection(val);
			return (val, flag);
		}

		public bool TryAddSensorConnection(ISensorConnection sensorConnection)
		{
			if (!(sensorConnection is TSensorConnection sensorConnection2))
			{
				return false;
			}
			return LocapBleDeviceSource.RegisterSensor(sensorConnection2);
		}

		public bool TryRemoveSensorConnection(ISensorConnection sensorConnection)
		{
			if (!(sensorConnection is TSensorConnection val))
			{
				return false;
			}
			if (!((IAccessoryBleDeviceSource)LocapBleDeviceSource).IsSensorRegistered(val.ConnectionGuid))
			{
				return false;
			}
			((IAccessoryBleDeviceSource)LocapBleDeviceSource).UnRegisterSensor(val.ConnectionGuid);
			return true;
		}
	}
}
