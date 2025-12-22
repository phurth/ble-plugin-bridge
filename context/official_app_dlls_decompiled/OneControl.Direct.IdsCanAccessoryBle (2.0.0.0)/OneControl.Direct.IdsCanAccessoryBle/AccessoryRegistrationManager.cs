using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Direct.IdsCanAccessoryBle.AwningSensor;
using OneControl.Direct.IdsCanAccessoryBle.BatteryMonitor;
using OneControl.Direct.IdsCanAccessoryBle.Connections;
using OneControl.Direct.IdsCanAccessoryBle.DoorLock;
using OneControl.Direct.IdsCanAccessoryBle.EchoBrakeControl;
using OneControl.Direct.IdsCanAccessoryBle.FlicButton;
using OneControl.Direct.IdsCanAccessoryBle.Mopeka;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;
using OneControl.Direct.IdsCanAccessoryBle.TankSensor;
using OneControl.Direct.IdsCanAccessoryBle.TemperatureSensor;
using OneControl.Direct.IdsCanAccessoryBle.TirePressureMonitor;
using OneControl.Direct.IdsCanAccessoryBle.TPMS;

namespace OneControl.Direct.IdsCanAccessoryBle
{
	public class AccessoryRegistrationManager : Singleton<AccessoryRegistrationManager>, IAccessoryRegistrationManager
	{
		public const string LogTag = "AccessoryRegistrationManager";

		private readonly ConcurrentDictionary<Type, ISensorConnectionFactory> _sensorConnectionFactories = new ConcurrentDictionary<Type, ISensorConnectionFactory>();

		public IAwningSensorBleDeviceSource? AwningSensorBleDeviceSource => Resolver<IAwningSensorBleDeviceSource>.Resolve;

		public IBatteryMonitorBleDeviceSource? BatteryMonitorBleDeviceSource => Resolver<IBatteryMonitorBleDeviceSource>.Resolve;

		public IDoorLockBleDeviceSource? DoorLockBleDeviceSource => Resolver<IDoorLockBleDeviceSource>.Resolve;

		public IEchoBrakeControlBleDeviceSource? EchoBrakeControlControllerBleDeviceSource => Resolver<IEchoBrakeControlBleDeviceSource>.Resolve;

		public ITirePressureMonitorBleDeviceSource TirePressureMonitorControllerBleDeviceSource => Resolver<ITirePressureMonitorBleDeviceSource>.Resolve;

		public IMopekaBleDeviceSource? MopekaBleDeviceSource => Resolver<IMopekaBleDeviceSource>.Resolve;

		public ITemperatureSensorBleDeviceSource? TemperatureSensorBleDeviceSource => Resolver<ITemperatureSensorBleDeviceSource>.Resolve;

		public ITankSensorBleDeviceSource? TankSensorBleDeviceSource => Resolver<ITankSensorBleDeviceSource>.Resolve;

		public IFlicButtonBleDeviceSource FlicButtonBleDeviceSource { get; set; }

		public ITpmsBleDeviceSource? TpmsBleDeviceSource => Resolver<ITpmsBleDeviceSource>.Resolve;

		public IEnumerable<ILogicalDeviceSourceDirect> StandardSharedSensorSources
		{
			get
			{
				foreach (ISensorConnectionFactory value in _sensorConnectionFactories.Values)
				{
					if (value.IsStandardSource)
					{
						yield return value.DeviceSource;
					}
				}
			}
		}

		public IEnumerable<ISensorConnection> SensorConnectionsAll
		{
			get
			{
				foreach (ISensorConnectionFactory value in _sensorConnectionFactories.Values)
				{
					foreach (ISensorConnection item in value.SensorConnectionsAll)
					{
						yield return item;
					}
				}
			}
		}

		public event SensorConnectionAdded? DoSensorConnectionAdded;

		public event SensorConnectionRemoved? DoSensorConnectionRemoved;

		private AccessoryRegistrationManager()
		{
		}

		public bool RegisterFactory(ISensorConnectionFactory factory)
		{
			if (!_sensorConnectionFactories.TryAdd(factory.SensorConnectionType, factory))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(56, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Ignoring ");
				defaultInterpolatedStringHandler.AppendFormatted("RegisterFactory");
				defaultInterpolatedStringHandler.AppendLiteral(" because a factory for ");
				defaultInterpolatedStringHandler.AppendFormatted(factory.SensorConnectionType.Name);
				defaultInterpolatedStringHandler.AppendLiteral(" was already registered.");
				TaggedLog.Warning("AccessoryRegistrationManager", defaultInterpolatedStringHandler.ToStringAndClear());
				return false;
			}
			return true;
		}

		public bool TryAddSensorConnection(IAccessoryScanResult accessoryScanResult, bool requestSave)
		{
			MAC accessoryMacAddress = accessoryScanResult.AccessoryMacAddress;
			if ((object)accessoryMacAddress == null)
			{
				return false;
			}
			IdsCanAccessoryStatus? accessoryStatus = accessoryScanResult.GetAccessoryStatus(accessoryMacAddress);
			if (!accessoryStatus.HasValue)
			{
				return false;
			}
			DEVICE_TYPE findDeviceType = accessoryStatus.Value.DeviceType;
			ISensorConnectionFactory sensorConnectionFactory = Enumerable.FirstOrDefault(_sensorConnectionFactories.Values, (ISensorConnectionFactory cf) => findDeviceType == cf.DeviceType);
			if (sensorConnectionFactory == null)
			{
				return false;
			}
			(ISensorConnection, bool) tuple = sensorConnectionFactory.TryAddSensorConnection(accessoryScanResult);
			var (sensorConnection, _) = tuple;
			if (sensorConnection == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(100, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Sensor can't be added from scan result, make sure it's associated connection factory is registered: ");
				defaultInterpolatedStringHandler.AppendFormatted(accessoryScanResult);
				TaggedLog.Warning("AccessoryRegistrationManager", defaultInterpolatedStringHandler.ToStringAndClear());
				return false;
			}
			this.DoSensorConnectionAdded?.Invoke(sensorConnection, tuple.Item2, requestSave);
			return tuple.Item2;
		}

		public bool TryAddSensorConnection(ISensorConnection sensorConnection, bool requestSave)
		{
			if (!(sensorConnection is ISensorConnectionBle sensorConnection2))
			{
				return false;
			}
			Type type = sensorConnection.GetType();
			if (!_sensorConnectionFactories.TryGetValue(type, out var sensorConnectionFactory))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(63, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Sensor can't be added because its type factory ");
				defaultInterpolatedStringHandler.AppendFormatted(sensorConnection.GetType().Namespace);
				defaultInterpolatedStringHandler.AppendLiteral(" is unknown for ");
				defaultInterpolatedStringHandler.AppendFormatted(sensorConnection);
				TaggedLog.Warning("AccessoryRegistrationManager", defaultInterpolatedStringHandler.ToStringAndClear());
				return false;
			}
			bool flag = sensorConnectionFactory.TryAddSensorConnection(sensorConnection2);
			this.DoSensorConnectionAdded?.Invoke(sensorConnection, flag, requestSave);
			return flag;
		}

		public bool TryRemoveSensorConnection(ISensorConnection sensorConnection, bool requestSave)
		{
			if (!(sensorConnection is ISensorConnectionBle))
			{
				return false;
			}
			Type type = sensorConnection.GetType();
			if (!_sensorConnectionFactories.TryGetValue(type, out var sensorConnectionFactory))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(65, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Sensor can't be removed because its type factory ");
				defaultInterpolatedStringHandler.AppendFormatted(sensorConnection.GetType().Namespace);
				defaultInterpolatedStringHandler.AppendLiteral(" is unknown for ");
				defaultInterpolatedStringHandler.AppendFormatted(sensorConnection);
				TaggedLog.Warning("AccessoryRegistrationManager", defaultInterpolatedStringHandler.ToStringAndClear());
				return false;
			}
			bool flag = sensorConnectionFactory.TryRemoveSensorConnection(sensorConnection);
			this.DoSensorConnectionRemoved?.Invoke(sensorConnection, flag, requestSave);
			return flag;
		}
	}
}
