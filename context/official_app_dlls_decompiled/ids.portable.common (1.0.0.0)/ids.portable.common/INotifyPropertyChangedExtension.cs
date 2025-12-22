using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IDS.Portable.Common
{
	public static class INotifyPropertyChangedExtension
	{
		public static void NotifyMainThread(this INotifyPropertyChanged sender, PropertyChangedEventHandler? handler, [CallerMemberName] string propertyName = "")
		{
			PropertyChangedEventHandler handler2 = handler;
			MainThread.RequestMainThreadAction(delegate
			{
				handler2?.Invoke(sender, new PropertyChangedEventArgs(propertyName));
			});
		}

		public static void Notify(this INotifyPropertyChanged sender, PropertyChangedEventHandler? handler, [CallerMemberName] string propertyName = "")
		{
			handler?.Invoke(sender, new PropertyChangedEventArgs(propertyName));
		}

		public static void UpdateAndThenNotifyMainThreadIfNeeded<TProperty>(this INotifyPropertyChanged sender, ref TProperty backingStore, TProperty newValue, PropertyChangedEventHandler? handler, [CallerMemberName] string propertyName = "") where TProperty : IEquatable<TProperty>
		{
			PropertyChangedEventHandler handler2 = handler;
			if (!backingStore.Equals(newValue))
			{
				backingStore = newValue;
				MainThread.RequestMainThreadAction(delegate
				{
					handler2?.Invoke(sender, new PropertyChangedEventArgs(propertyName));
				});
			}
		}

		public static void UpdateAndNotifyIfNeeded<TProperty>(this INotifyPropertyChanged sender, ref TProperty backingStore, TProperty newValue, PropertyChangedEventHandler? handler, [CallerMemberName] string propertyName = "") where TProperty : IEquatable<TProperty>
		{
			if (!backingStore.Equals(newValue))
			{
				backingStore = newValue;
				handler?.Invoke(sender, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}
