using System.Collections.Generic;

namespace IDS.Core.IDS_CAN
{
	public interface ILocalBlock : IBlock
	{
		new LocalDevice Device { get; }

		IReadOnlyList<byte> Data { get; }

		bool WriteData(IReadOnlyList<byte> data);
	}
}
