using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IDS.Portable.Common
{
	public class CommonNotifyPropertyChanged : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler? PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
		{
			NotifyPropertyChanged(propertyName);
		}

		protected void NotifyPropertyChanged(string propertyName, bool notifyOnMainThread = true)
		{
			if (notifyOnMainThread)
			{
				MainThread.RequestMainThreadAction(delegate
				{
					this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
				});
			}
			else
			{
				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		protected bool SetBackingField<TValue>(ref TValue field, TValue value, [CallerMemberName] string notifyPropertyName = "", params string[] notifyPropertyNameEnumeration)
		{
			if (EqualityComparer<TValue>.Default.Equals(field, value))
			{
				return false;
			}
			field = value;
			if (!string.IsNullOrEmpty(notifyPropertyName))
			{
				OnPropertyChanged(notifyPropertyName);
			}
			if (notifyPropertyNameEnumeration == null)
			{
				return true;
			}
			foreach (string text in notifyPropertyNameEnumeration)
			{
				if (!string.IsNullOrEmpty(text))
				{
					OnPropertyChanged(text);
				}
			}
			return true;
		}

		protected void RemoveAllPropertyChangedEventHandler()
		{
			this.PropertyChanged = null;
		}
	}
}
