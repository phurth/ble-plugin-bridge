using System;
using System.ComponentModel;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDevicePidBindableAsync<out TLogicalDevicePid, TValue> : IBindableAsyncValue<TValue>, INotifyPropertyChanged, ICommonDisposable, IDisposable where TValue : IEquatable<TValue>
	{
		uint ReadingValueCount { get; }

		uint WritingValueCount { get; }

		TValue ValueToUseWhenInvalidData { get; set; }

		TLogicalDevicePid PidComponent { get; }
	}
}
