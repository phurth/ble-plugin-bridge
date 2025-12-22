using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	public interface IRemoteDevice : IDevice, IBusEndpoint, IUniqueDeviceInfo, IUniqueProductInfo, IEventSender
	{
		new IRemoteProduct Product { get; }

		bool IsCircuitIDWriteable { get; }

		IClientSessionManager Sessions { get; }

		IPIDManager PIDs { get; }

		IBLOCKManager BLOCKs { get; }

		ITreeNode TreeNode { get; }

		void QueryPartNumber();

		bool IsPIDSupported(PID id);

		bool IsSessionSupported(SESSION_ID id);

		IDevicePID GetPID(PID id);

		IDeviceBLOCK GetBLOCK(BLOCK_ID id);
	}
}
