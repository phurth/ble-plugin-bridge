using System;
using System.Threading.Tasks;
using IDS.Core.Tasks;
using IDS.Core.Types;

namespace IDS.Core.IDS_CAN
{
	public interface IPidClient
	{
		Task<Tuple<RESPONSE?, PidList>> ReadPidListAsync(AsyncOperation operation, IDevice tgtDevice);

		Task<Tuple<RESPONSE?, UInt48?>> ReadPidAsync(AsyncOperation operation, IDevice tgtDevice, PID id);

		Task<Tuple<RESPONSE?, UInt48?>> ReadPidAsync(AsyncOperation operation, IDevice tgtDevice, PID id, bool withadd, ushort add);

		Task<Tuple<RESPONSE?, UInt48?>> ReadPidAsync(AsyncOperation operation, PidInfo pidInfo);

		Task<Tuple<RESPONSE?, UInt48?>> WritePidAsync(AsyncOperation operation, IDevice tgtDevice, PID id, UInt48 value, ISessionClient session);
	}
}
