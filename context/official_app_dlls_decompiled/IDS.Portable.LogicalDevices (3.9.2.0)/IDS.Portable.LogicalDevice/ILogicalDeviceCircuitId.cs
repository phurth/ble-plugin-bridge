using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceCircuitId : INotifyPropertyChanged, ICommonDisposable, IDisposable
	{
		bool HasValueBeenLoaded { get; }

		bool IsWriting { get; }

		CIRCUIT_ID LastValue { get; }

		CIRCUIT_ID Value { get; }

		void UpdateValue(CIRCUIT_ID circuitId);

		Task<LogicalDeviceCircuitIdWriteResult> WriteValueAsync(CIRCUIT_ID circuitId, CancellationToken cancellationToken);
	}
}
