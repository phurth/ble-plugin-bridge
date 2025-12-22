using System;

namespace OneControl.Devices
{
	public class MomentaryRelayDirectionException : MomentaryRelayException
	{
		public MomentaryRelayDirectionException(RelayHBridgeDirection direction, Exception? innerException = null)
			: base($"Momentary Relay direction: {direction} not supported.", innerException)
		{
		}
	}
}
