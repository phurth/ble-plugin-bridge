using System;
using Serilog;

namespace IDS.Portable.Common
{
	public static class WrappedRunner
	{
		public static void TryInvoke(Action action)
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				Log.Debug("WrappedRunner Exception " + ex.Message + "\n" + ex.StackTrace);
			}
		}
	}
}
