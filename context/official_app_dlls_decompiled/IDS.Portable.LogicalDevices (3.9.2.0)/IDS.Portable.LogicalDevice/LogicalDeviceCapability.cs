using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceCapability : ILogicalDeviceCapability, INotifyPropertyChanged
	{
		protected byte RawValue;

		public virtual IEnumerable<LogicalDeviceCapabilitySerializable> ActiveCapabilities
		{
			[IteratorStateMachine(typeof(_003Cget_ActiveCapabilities_003Ed__10))]
			get
			{
				//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
				return new _003Cget_ActiveCapabilities_003Ed__10(-2);
			}
		}

		public event DeviceCapabilityChangedEventHandler? DeviceCapabilityChangedEvent;

		public event PropertyChangedEventHandler? PropertyChanged;

		public byte GetRawValue()
		{
			return RawValue;
		}

		public LogicalDeviceCapability()
			: this(0)
		{
		}

		public LogicalDeviceCapability(byte? rawCapability)
		{
			RawValue = rawCapability.GetValueOrDefault();
		}

		public bool UpdateDeviceCapability(byte? rawDeviceCapability)
		{
			if (!rawDeviceCapability.HasValue)
			{
				return false;
			}
			byte b = (byte)(rawDeviceCapability & 0xFF).Value;
			if (RawValue == b)
			{
				return false;
			}
			RawValue = b;
			OnUpdateDeviceCapabilityChanged();
			return true;
		}

		protected virtual void OnUpdateDeviceCapabilityChanged()
		{
			this.DeviceCapabilityChangedEvent?.Invoke();
		}

		protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			string propertyName2 = propertyName;
			MainThread.RequestMainThreadAction(delegate
			{
				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName2));
			});
		}

		protected void NotifyPropertyChanged(string propertyName)
		{
			string propertyName2 = propertyName;
			MainThread.RequestMainThreadAction(delegate
			{
				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName2));
			});
		}
	}
}
