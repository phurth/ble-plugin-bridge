using System;

namespace OneControl.Devices.TextDevice
{
	public interface ITextDevice
	{
		string Title { get; }

		string Notes { get; }

		TimeSpan Duration { get; }

		bool IsReminder { get; }
	}
}
