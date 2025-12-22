using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceLightMasterSwitchControllableMyRvLinkEx : LogicalDeviceExBase<ILogicalDeviceSwitchableLight>
	{
		private class BucketGroup<TValue>
		{
			private readonly List<List<TValue>> _bucketList = new List<List<TValue>>();

			private List<TValue>? _buildingLightList;

			public int MaxBucketSize { get; }

			public IEnumerable<List<TValue>> Buckets => _bucketList;

			public BucketGroup(int maxBucketSize)
			{
				MaxBucketSize = maxBucketSize;
			}

			public void Add(TValue item)
			{
				if (_buildingLightList == null || _buildingLightList!.Count >= MaxBucketSize)
				{
					_buildingLightList = new List<TValue>();
					_bucketList.Add(_buildingLightList);
				}
				_buildingLightList!.Add(item);
			}
		}

		private int _performingOperation;

		public const int MaxAllLightsOnOff = 24;

		protected override string LogTag => "LogicalDeviceLightMasterSwitchControllableMyRvLinkEx";

		public static LogicalDeviceLightMasterSwitchControllableMyRvLinkEx? SharedExtension => LogicalDeviceExBase<ILogicalDeviceSwitchableLight>.GetSharedExtension<LogicalDeviceLightMasterSwitchControllableMyRvLinkEx>(autoCreate: true);

		public static ILogicalDeviceEx? LogicalDeviceExFactory(ILogicalDevice logicalDevice)
		{
			return LogicalDeviceExBase<ILogicalDeviceSwitchableLight>.LogicalDeviceExFactory<LogicalDeviceLightMasterSwitchControllableMyRvLinkEx>(logicalDevice, GetLogicalDeviceScope);
		}

		protected static LogicalDeviceExScope GetLogicalDeviceScope(ILogicalDeviceSwitchableLight logicalDevice)
		{
			return LogicalDeviceExScope.Shared;
		}

		public async Task<bool> TryChangeStateAsync(ILogicalDeviceService deviceService, LightMasterSwitchControllableState state, Func<ILogicalDeviceSwitchableLight, bool> lightFilter, CancellationToken cancellationToken)
		{
			if (Interlocked.Exchange(ref _performingOperation, 1) != 0)
			{
				TaggedLog.Debug(LogTag, $"Unable to perform All {state} operation as an operation is already in progress.");
				return false;
			}
			Dictionary<ILogicalDeviceSourceDirectSwitchMasterControllable, BucketGroup<ILogicalDeviceSwitchableLight>> dictionary = new Dictionary<ILogicalDeviceSourceDirectSwitchMasterControllable, BucketGroup<ILogicalDeviceSwitchableLight>>();
			try
			{
				List<ILogicalDeviceSwitchableLight> attachedLogicalDevices = GetAttachedLogicalDevices();
				if (!Enumerable.Any(attachedLogicalDevices))
				{
					TaggedLog.Debug(LogTag, $"Unable to perform All {state} operation as an operation is already in progress.");
					return false;
				}
				foreach (ILogicalDeviceSwitchableLight item in attachedLogicalDevices)
				{
					if (item.IsMasterSwitchControllable && (item.ActiveConnection == LogicalDeviceActiveConnection.Direct || item.ActiveConnection == LogicalDeviceActiveConnection.Cloud) && deviceService?.GetPrimaryDeviceSourceDirect(item) is ILogicalDeviceSourceDirectSwitchMasterControllable logicalDeviceSourceDirectSwitchMasterControllable && (lightFilter?.Invoke(item) ?? true))
					{
						if (!dictionary.TryGetValue(logicalDeviceSourceDirectSwitchMasterControllable, out var value))
						{
							value = (dictionary[logicalDeviceSourceDirectSwitchMasterControllable] = new BucketGroup<ILogicalDeviceSwitchableLight>(24));
						}
						TaggedLog.Debug(LogTag, $"MyRvLink Switch to {state} for {item.LogicalId.ToString(LogicalDeviceIdFormat.FunctionNameFull)}");
						value.Add(item);
					}
				}
				if (dictionary.Count <= 0)
				{
					TaggedLog.Debug(LogTag, $"Unable to perform All {state} operation as no devices were found.");
					return false;
				}
				bool turnOn = state != LightMasterSwitchControllableState.Off;
				foreach (KeyValuePair<ILogicalDeviceSourceDirectSwitchMasterControllable, BucketGroup<ILogicalDeviceSwitchableLight>> directManagerSwitchItem in dictionary)
				{
					foreach (List<ILogicalDeviceSwitchableLight> bucket in directManagerSwitchItem.Value.Buckets)
					{
						await directManagerSwitchItem.Key.TrySwitchAllMasterControllable(bucket, turnOn, cancellationToken);
					}
				}
			}
			finally
			{
				_performingOperation = 0;
			}
			return true;
		}
	}
}
