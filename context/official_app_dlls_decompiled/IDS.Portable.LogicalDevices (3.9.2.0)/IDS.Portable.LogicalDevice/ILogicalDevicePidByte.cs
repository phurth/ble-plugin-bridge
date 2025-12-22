using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDevicePidByte : ILogicalDevicePidProperty<byte>, ILogicalDevicePid<byte>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		byte ValueByte { get; set; }

		Task<byte> ReadByteAsync(CancellationToken cancellationToken);

		Task WriteByteAsync(byte value, CancellationToken cancellationToken);
	}
}
