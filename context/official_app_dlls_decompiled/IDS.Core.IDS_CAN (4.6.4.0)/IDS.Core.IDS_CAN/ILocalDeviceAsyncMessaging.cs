using System;
using System.Threading.Tasks;
using IDS.Core.Tasks;

namespace IDS.Core.IDS_CAN
{
	public interface ILocalDeviceAsyncMessaging
	{
		Task<Tuple<RESPONSE, CAN.MessageBuffer>> TransmitRequestAsync(AsyncOperation operation, IBusEndpoint target, REQUEST request, CAN.PAYLOAD payload, Func<LocalDeviceRxEvent, RESPONSE?> validator);
	}
}
