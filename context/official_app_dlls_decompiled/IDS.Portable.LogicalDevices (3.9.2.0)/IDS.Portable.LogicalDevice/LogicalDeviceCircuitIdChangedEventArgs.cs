using System;
using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceCircuitIdChangedEventArgs : EventArgs
	{
		public CIRCUIT_ID PreviousCircuitId { get; }

		public LogicalDeviceCircuitIdChangedEventArgs(CIRCUIT_ID previousCircuitId)
		{
			PreviousCircuitId = previousCircuitId;
		}
	}
}
