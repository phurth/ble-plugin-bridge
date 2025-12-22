using System;
using System.Threading;

namespace IDS.Portable.Common
{
	public static class CancellationTokenSourceExtension
	{
		private const string LogTag = "CancellationTokenSourceExtension";

		public static void TryCancel(this CancellationTokenSource cts)
		{
			try
			{
				if (!cts.IsCancellationRequested)
				{
					cts.Cancel();
				}
			}
			catch
			{
			}
		}

		public static void CancelAndDispose(this CancellationTokenSource cts)
		{
			cts.TryCancel();
			cts.Dispose();
		}

		public static void TryCancelAndDispose(this CancellationTokenSource cts)
		{
			try
			{
				cts.CancelAndDispose();
			}
			catch (Exception)
			{
			}
		}
	}
}
