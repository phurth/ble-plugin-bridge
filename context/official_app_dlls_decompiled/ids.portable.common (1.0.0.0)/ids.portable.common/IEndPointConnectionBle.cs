using System;

namespace IDS.Portable.Common
{
	public interface IEndPointConnectionBle
	{
		string ConnectionId { get; }

		Guid ConnectionGuid { get; }
	}
}
