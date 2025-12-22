using System;
using Android.Content;
using Android.Runtime;
using Java.Interop;
using Java.Lang;

namespace ids.portable.ble.BleScanner
{
	internal class AutoStatePreferenceListener : Java.Lang.Object, ISharedPreferencesOnSharedPreferenceChangeListener, IJavaObject, IDisposable, IJavaPeerable
	{
		private const string KEY = "isAutoRunning";

		private readonly Action<bool> _onChanged;

		public AutoStatePreferenceListener(Action<bool> onChanged)
		{
			_onChanged = onChanged;
		}

		public void OnSharedPreferenceChanged(ISharedPreferences prefs, string key)
		{
			if (key == "isAutoRunning")
			{
				bool boolean = prefs.GetBoolean("isAutoRunning", false);
				_onChanged(boolean);
			}
		}
	}
}
