using System.Threading.Tasks;
using IDS.Core.Events;
using IDS.Core.Tasks;

namespace IDS.Core.IDS_CAN
{
	public interface IDeviceBLOCK : IEventSender
	{
		BLOCK_ID ID { get; }

		IRemoteDevice Device { get; }

		string Name { get; }

		bool IsValueValid { get; }

		string ValueString { get; }

		byte GetData(int nb);

		bool RequestRead();

		Task<BLOCKPropertyValue> ReadPropertyAsync(byte Property, AsyncOperation operation);

		Task<bool> StartReadData(uint Offset, byte Size_Msg, byte DelayMs, AsyncOperation operation);

		Task<bool> ReadDataBufferReadyAsync(AsyncOperation operation);
	}
}
