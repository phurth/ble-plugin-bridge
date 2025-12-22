using System;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.LogicalDevice;
using OneControl.Direct.IdsCanAccessoryBle.Connections;

namespace OneControl.Direct.IdsCanAccessoryBle
{
	public interface IAccessoryBleDeviceDriverLocap : IAccessoryBleDeviceDriver, ICommonDisposable, IDisposable
	{
		Guid BleDeviceId { get; }

		MAC AccessoryMacAddress { get; }

		string SoftwarePartNumber { get; }

		string BleDeviceName { get; }
	}
	public interface IAccessoryBleDeviceDriverLocap<out TSensorConnection, TLogicalDevice> : IAccessoryBleDeviceDriverLocap, IAccessoryBleDeviceDriver, ICommonDisposable, IDisposable where TSensorConnection : ISensorConnectionBleLocap where TLogicalDevice : class, ILogicalDeviceAccessory
	{
		TSensorConnection SensorConnection { get; }

		AccessoryConnectionManager<TLogicalDevice>? AccessoryConnectionManager { get; }

		TLogicalDevice? LogicalDevice { get; }
	}
}
