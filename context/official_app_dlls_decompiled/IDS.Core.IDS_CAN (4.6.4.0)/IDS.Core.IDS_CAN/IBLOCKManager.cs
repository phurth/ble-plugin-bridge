using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using IDS.Core.Tasks;

namespace IDS.Core.IDS_CAN
{
	public interface IBLOCKManager : IEnumerable<IDeviceBLOCK>, IEnumerable
	{
		IRemoteDevice Device { get; }

		int Count { get; }

		bool DeviceQueryComplete { get; }

		void QueryDevice();

		bool Contains(BLOCK_ID id);

		IDeviceBLOCK GetBLOCK(BLOCK_ID id);

		Task<BLOCKValue> ReadPropertyAsync(BLOCK_ID id, AsyncOperation operation);

		Task<BLOCKValue> StartReadData(BLOCK_ID id, uint Offset, byte Size_Msg, byte DelayMs, AsyncOperation operation);

		Task<BLOCKValue> ReadDataBufferReadyAsync(BLOCK_ID id, AsyncOperation operation);
	}
}
