using System;
using IDS.Core.Types;
using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.IdsCanAccessoryBle.ScanResults
{
	public readonly struct AccessoryPidStatus
	{
		public Pid Id { get; }

		public UInt48 Value { get; }

		public DateTime ReceivedTimeStamp { get; }

		public AccessoryPidStatus(Pid id, UInt48 value, DateTime receivedTimeStamp)
		{
			Id = id;
			Value = value;
			ReceivedTimeStamp = receivedTimeStamp;
		}
	}
}
