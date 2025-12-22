using System.Threading.Tasks;
using IDS.Core.Events;
using IDS.Core.Tasks;

namespace IDS.Core.IDS_CAN
{
	public interface IDevicePID : IEventSender
	{
		PID ID { get; }

		byte Flags { get; }

		IRemoteDevice Device { get; }

		string Name { get; }

		bool IsReadable { get; }

		bool IsWritable { get; }

		bool IsNonVolatile { get; }

		bool IsWithAddress { get; }

		ulong Value { get; }

		uint Data { get; }

		ushort Address { get; }

		bool IsValueValid { get; }

		string ValueString { get; }

		bool RequestRead();

		bool RequestRead(ushort address);

		Task<bool> ReadAsync(AsyncOperation obj);

		Task<bool> ReadAsync(ushort address, AsyncOperation obj);

		Task<bool> WriteAsync(ulong value, ISessionClient session, AsyncOperation obj);

		Task<bool> WriteAsync(ushort address, uint data, ISessionClient session, AsyncOperation obj);
	}
}
