using System.Runtime.CompilerServices;

namespace ids.portable.common
{
	public static class DebugUtils
	{
		public static (int lineNumber, string caller) GetLineInfo([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = "")
		{
			return (lineNumber, caller);
		}
	}
}
