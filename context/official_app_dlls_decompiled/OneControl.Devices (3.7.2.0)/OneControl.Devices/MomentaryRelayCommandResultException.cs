using System;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class MomentaryRelayCommandResultException : MomentaryRelayException
	{
		public MomentaryRelayCommandResultException(CommandResult commandResult, Exception? innerException = null)
			: base($"Momentary Relay failed with command result: {commandResult}", innerException)
		{
		}
	}
}
