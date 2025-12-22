using System;

namespace OneControl.Devices
{
	public static class DimmableLightCommandExtension
	{
		public static DimmableLightCommand ConvertToCommand(this DimmableLightMode mode)
		{
			return mode switch
			{
				DimmableLightMode.Off => DimmableLightCommand.Off, 
				DimmableLightMode.On => DimmableLightCommand.On, 
				DimmableLightMode.Blink => DimmableLightCommand.Blink, 
				DimmableLightMode.Swell => DimmableLightCommand.Swell, 
				_ => (DimmableLightCommand)mode, 
			};
		}

		public static DimmableLightMode ConvertToMode(this DimmableLightCommand command, DimmableLightMode restoreMode = DimmableLightMode.On)
		{
			return command switch
			{
				DimmableLightCommand.Off => DimmableLightMode.Off, 
				DimmableLightCommand.On => DimmableLightMode.On, 
				DimmableLightCommand.Blink => DimmableLightMode.Blink, 
				DimmableLightCommand.Swell => DimmableLightMode.Swell, 
				DimmableLightCommand.Restore => restoreMode, 
				DimmableLightCommand.Settings => throw new ArgumentException(string.Format("Can't convert from {0} to a {1}", DimmableLightCommand.Settings, "DimmableLightMode")), 
				_ => (DimmableLightMode)command, 
			};
		}
	}
}
