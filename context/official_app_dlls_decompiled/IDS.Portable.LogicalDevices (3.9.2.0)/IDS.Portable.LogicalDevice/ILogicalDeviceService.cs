using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.FirmwareUpdate;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceService : ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		LogicalDeviceServiceOptions Options { get; }

		MakeDeviceName MakeDeviceName { get; set; }

		MakeDeviceName MakeDeviceNameShort { get; set; }

		MakeDeviceName MakeDeviceNameShortAbbreviated { get; set; }

		ILogicalDeviceRemoteManager? RemoteManager { get; }

		List<ILogicalDeviceFactory> LogicalDeviceFactoryList { get; }

		ILogicalDeviceProductManager? ProductManager { get; }

		ILogicalDeviceManager? DeviceManager { get; }

		bool SessionsEnabled { get; set; }

		IN_MOTION_LOCKOUT_LEVEL InMotionLockoutLevel { get; }

		DateTime RealTimeClockTime { get; set; }

		ILogicalDeviceSourceDirectManager DeviceSourceManager { get; }

		ILogicalDeviceFirmwareUpdateManager FirmwareUpdateManager { get; }

		ILogicalDeviceSourceDirect? GetPrimaryDeviceSourceDirect(ILogicalDevice logicalDevice);

		void RegisterLogicalDeviceFactory(ILogicalDeviceFactory factory);

		void RegisterLogicalDeviceExFactory(LogicalDeviceExFactory factory);

		void RegisterLogicalDeviceExFactory<TLogicalDevice>(Func<TLogicalDevice, ILogicalDeviceEx> factory) where TLogicalDevice : class, ILogicalDevice;

		void AutoRegisterLogicalDeviceExFactory(Assembly assembly);

		void RegisterRemoteManager(ILogicalDeviceRemoteManager remoteManager);

		void UpdateInMotionLockoutLevel();

		void EnableRealTimeClockUpdates(TimeSpan timeInterval);

		void DisableRealTimeClockUpdates();

		LogicalDeviceExclusiveOperation GetExclusiveOperation<TClass>();

		void Start();

		void Start(ILogicalDeviceSourceDirect deviceSource);

		void Start(List<ILogicalDeviceSourceDirect> deviceSources);

		void Stop();
	}
}
