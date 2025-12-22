using System.Collections;
using System.Collections.Generic;

namespace IDS.Core.IDS_CAN
{
	public class PidList : IReadOnlyCollection<PidInfo>, IEnumerable<PidInfo>, IEnumerable
	{
		public readonly IDevice Device;

		private readonly Dictionary<ushort, PidInfo> Dict = new Dictionary<ushort, PidInfo>();

		public int Count => Dict.Count;

		public PidInfo this[PID id]
		{
			get
			{
				if (Dict.TryGetValue(id.Value, out var value))
				{
					return value;
				}
				return null;
			}
		}

		public IEnumerator<PidInfo> GetEnumerator()
		{
			return Dict.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Dict.Values.GetEnumerator();
		}

		public PidList(IDevice device, IEnumerable<PidInfo> list)
		{
			Device = device;
			foreach (PidInfo item in list)
			{
				Dict.Add(item.ID.Value, item);
			}
		}

		public bool Contains(PID id)
		{
			return Dict.ContainsKey(id.Value);
		}
	}
}
