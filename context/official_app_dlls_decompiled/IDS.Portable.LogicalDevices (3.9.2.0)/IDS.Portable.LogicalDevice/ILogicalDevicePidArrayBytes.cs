using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDevicePidArrayBytes : ILogicalDevicePidProperty<byte[]>, ILogicalDevicePid<byte[]>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		public delegate byte[] ValueToArrayBytes(ulong value);

		public delegate ulong ValueFromArrayBytes(byte[] value);

		byte[] ValueBytes { get; set; }

		ValueToArrayBytes ConvertToBytes { get; }

		ValueFromArrayBytes ConvertFromBytes { get; }

		Task<byte[]> ReadArrayByteAsync(CancellationToken cancellationToken);

		Task WriteArrayByteAsync(byte[] value, CancellationToken cancellationToken);
	}
}
