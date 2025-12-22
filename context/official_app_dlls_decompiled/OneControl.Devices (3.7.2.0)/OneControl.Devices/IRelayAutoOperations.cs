using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface IRelayAutoOperations
	{
		bool AreAutoCommandsSupported { get; }

		bool IsAutoOperationInProgress { get; }

		bool IsWindSensorAutoOperationInProgress { get; }

		bool IsAutoOperation { get; }

		bool AutoForwardAllowed { get; }

		bool AutoReverseAllowed { get; }

		Task<CommandResult> TryAutoReverseAsync(CancellationToken cancellationToken);

		Task<CommandResult> TryAutoForwardAsync(CancellationToken cancellationToken);

		void TryAbortAutoOperation();
	}
}
