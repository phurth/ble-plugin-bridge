using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface IRelayHBridge : IDevicesCommon, INotifyPropertyChanged, IRelayAutoOperations
	{
		DeviceCategory DeviceCategory { get; }

		bool FaultStatus { get; }

		bool UserClearRequired { get; }

		bool IsHoming { get; }

		DTC_ID UserMessageDtc { get; }

		RelayHBridgeEnergized RelayEnergized { get; }

		RelayHBridgeEnergized CommandRelayEnergized { get; }

		RelayHBridgeDirectionVerbose DirectionVerbose { get; }

		RelayHBridgeDirectionVerbose CommandDirectionVerbose { get; }

		bool Relay1Allowed { get; }

		bool Relay2Allowed { get; }

		bool IsHomingSupported { get; }

		bool IsAwningSensorSupported { get; }

		Task PerformMovementOperationAsync(RelayHBridgeDirection direction, Action<RelayMovementStatus> updateRelayMovementStatus, CancellationToken cancellationToken);

		Task PerformHomeResetOperationAsync(Action<RelayOperationStatus> updateRelayHomeStatus, CancellationToken cancellationToken);

		bool IsForwardRelayEnergized();

		bool IsReverseRelayEnergized();

		bool IsForwardRelayAllowed();

		bool IsReverseRelayAllowed();
	}
}
