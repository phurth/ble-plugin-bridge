using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using DynamicData;
using IDS.Portable.Common;

namespace OneControl.Devices.TPMS
{
	public class TpmsSensorGroup : CommonDisposable
	{
		private readonly IDisposable _deviceStatusExtendedObservableCacheSubscription;

		private readonly LogicalDeviceTpms _logicalDeviceTpms;

		public TpmsGroupId GroupId { get; }

		public ObservableCollection<LogicalDeviceTpmsStatusExtended> SensorCollection { get; } = new ObservableCollection<LogicalDeviceTpmsStatusExtended>();


		public event EventHandler<LogicalDeviceTpmsStatusExtended> SensorDataChanged;

		public TpmsSensorGroup(LogicalDeviceTpms logicalDeviceTpms, TpmsGroupId groupId)
		{
			TpmsSensorGroup tpmsSensorGroup = this;
			if (groupId == TpmsGroupId.Invalid)
			{
				throw new ArgumentOutOfRangeException("groupId", groupId, "Invalid GroupId");
			}
			_logicalDeviceTpms = logicalDeviceTpms;
			GroupId = groupId;
			_deviceStatusExtendedObservableCacheSubscription = ObservableExtensions.Subscribe(logicalDeviceTpms.DeviceStatusExtendedObservableCache.Connect().Filter((LogicalDeviceTpmsStatusExtended statusExtended) => statusExtended.SensorId.GroupId == groupId), delegate(IChangeSet<LogicalDeviceTpmsStatusExtended, TpmsPositionalSensorId> changeSet)
			{
				foreach (Change<LogicalDeviceTpmsStatusExtended, TpmsPositionalSensorId> item in changeSet)
				{
					switch (item.Reason)
					{
					case ChangeReason.Add:
						tpmsSensorGroup.SensorCollection.Add(item.Current);
						item.Current.PropertyChanged += tpmsSensorGroup.SensorAddedOrRemoved;
						tpmsSensorGroup.SensorDataChanged?.Invoke(tpmsSensorGroup, item.Current);
						break;
					case ChangeReason.Remove:
						tpmsSensorGroup.SensorCollection.Remove(item.Current);
						item.Current.PropertyChanged -= tpmsSensorGroup.SensorAddedOrRemoved;
						tpmsSensorGroup.SensorDataChanged?.Invoke(tpmsSensorGroup, item.Current);
						break;
					}
				}
			});
		}

		private void SensorAddedOrRemoved(object sender, PropertyChangedEventArgs e)
		{
			if (sender is LogicalDeviceTpmsStatusExtended e2)
			{
				this.SensorDataChanged?.Invoke(this, e2);
			}
		}

		public override void Dispose(bool disposing)
		{
			_deviceStatusExtendedObservableCacheSubscription.TryDispose();
			Dispose();
		}
	}
}
