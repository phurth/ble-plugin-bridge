using System.Collections.Generic;

namespace IDS.Core.IDS_CAN
{
	public interface ITextConsole
	{
		IDevice Device { get; }

		bool IsDetected { get; }

		IReadOnlyList<string> Lines { get; }

		TEXT_CONSOLE_SIZE Size { get; }
	}
}
