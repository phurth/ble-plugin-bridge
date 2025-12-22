using System;

namespace OneControl.Direct.IdsCanAccessoryBle.ScanResults
{
	public interface IIdsCanAccessoryScanResultHistoryTracker
	{
		void TrackRawMessage(Guid deviceId, IdsCanAccessoryMessageType messageType, byte[] rawMessage);
	}
}
