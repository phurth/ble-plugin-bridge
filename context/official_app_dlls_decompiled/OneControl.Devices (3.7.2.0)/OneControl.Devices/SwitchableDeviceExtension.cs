using System;
using System.Threading.Tasks;

namespace OneControl.Devices
{
	public static class SwitchableDeviceExtension
	{
		[Obsolete("ToggleAsync should be used in place of this helper method")]
		public static void Toggle(this ISwitchableDevice switchable, bool restore = true)
		{
			Task.Run(async () => await switchable.ToggleAsync(restore).ConfigureAwait(false));
		}

		[Obsolete("TurnOnAsync should be used in place of this helper method")]
		public static void TurnOn(this ISwitchableDevice switchable, bool restore)
		{
			Task.Run(async () => await switchable.TurnOnAsync(restore).ConfigureAwait(false));
		}

		[Obsolete("TurnOffAsync should be used in place of this helper method")]
		public static void TurnOff(this ISwitchableDevice switchable, bool restore)
		{
			Task.Run(async () => await switchable.TurnOffAsync().ConfigureAwait(false));
		}
	}
}
