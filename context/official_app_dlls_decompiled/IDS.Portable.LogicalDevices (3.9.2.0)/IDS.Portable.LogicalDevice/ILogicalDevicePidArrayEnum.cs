using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDevicePidArrayEnum<TValue> : ILogicalDevicePidProperty<TValue[]>, ILogicalDevicePid<TValue[]>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged where TValue : struct, Enum, IConvertible
	{
		public delegate TValue[] ValueToArrayEnums(ulong value);

		public delegate ulong ValueFromArrayEnums(TValue[] value);

		TValue[] ValueByteEnums { get; set; }

		ValueToArrayEnums ConvertToEnums { get; }

		ValueFromArrayEnums ConvertFromEnums { get; }

		Task<TValue[]> ReadArrayByteEnumAsync(CancellationToken cancellationToken);

		Task WriteArrayByteEnumAsync(TValue[] value, CancellationToken cancellationToken);
	}
}
