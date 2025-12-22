using System.Collections;
using System.Collections.Generic;
using IDS.Core.Collections;

namespace IDS.Core.IDS_CAN
{
	internal class Circuit : ICircuit, IEnumerable<IRemoteDevice>, IEnumerable
	{
		private readonly ConcurrentHashSet<IRemoteDevice> Members = new ConcurrentHashSet<IRemoteDevice>();

		public CIRCUIT_ID CircuitID { get; private set; }

		public int DeviceCount => Members.Count;

		public bool IsEmpty => Members.Count == 0;

		public Circuit(CIRCUIT_ID circuit_id)
		{
			CircuitID = circuit_id;
		}

		public IEnumerator<IRemoteDevice> GetEnumerator()
		{
			foreach (IRemoteDevice member in Members)
			{
				if (member != null && member.IsOnline)
				{
					yield return member;
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public bool Cleanup()
		{
			bool flag = false;
			foreach (IRemoteDevice member in Members)
			{
				if (!member.IsOnline || (uint)member.CircuitID != (uint)CircuitID)
				{
					flag |= Members.Remove(member);
				}
			}
			return flag;
		}

		public bool AddDeviceToCircuit(IRemoteDevice device)
		{
			if ((uint?)device?.CircuitID != (uint)CircuitID)
			{
				return false;
			}
			if (Members.Contains(device))
			{
				return false;
			}
			Members.Add(device);
			return true;
		}
	}
}
