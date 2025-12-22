using System;
using System.ComponentModel;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDevicePidUInt48 : ILogicalDevicePidProperty<UInt48>, ILogicalDevicePid<UInt48>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		UInt48 ValueUInt48 { get; set; }
	}
}
