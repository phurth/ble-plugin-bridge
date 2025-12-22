using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDevicePidUInt16 : ILogicalDevicePidProperty<ushort>, ILogicalDevicePid<ushort>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		ushort ValueUInt16 { get; set; }

		Task<ushort> ReadUInt16Async(CancellationToken cancellationToken);

		Task WriteUInt16Async(ushort value, CancellationToken cancellationToken);
	}
}
