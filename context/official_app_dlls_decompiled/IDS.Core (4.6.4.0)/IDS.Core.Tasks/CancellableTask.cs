using System;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Core.Tasks
{
	public sealed class CancellableTask : Task
	{
		private readonly Func<CancellationToken, Task> UserAction;

		private CancellationTokenSource CTS;

		public bool IsDisposed { get; private set; }

		private string Name
		{
			get
			{
				if (UserAction.Target == null)
				{
					return UserAction.GetType().FullName;
				}
				return UserAction.Target.GetType().FullName;
			}
		}

		public static CancellableTask Run(Func<CancellationToken, Task> action)
		{
			Action start_action = null;
			CancellableTask t = new CancellableTask(action, delegate
			{
				start_action();
			}, new CancellationTokenSource());
			start_action = async delegate
			{
				try
				{
					await t.UserAction(t.CTS.Token).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					throw ex;
				}
				finally
				{
					_ = 0;
				}
			};
			t.Start();
			return t;
		}

		private CancellableTask(Func<CancellationToken, Task> user_action, Action startup_action, CancellationTokenSource cts)
			: base(startup_action, cts.Token)
		{
			UserAction = user_action;
			CTS = cts;
		}

		public void Cancel()
		{
			CancellationTokenSource cancellationTokenSource = Interlocked.Exchange(ref CTS, null);
			CancelAndWaitForCompletion(cancellationTokenSource);
			if (cancellationTokenSource != null)
			{
				Dispose();
			}
		}

		private void CancelAndWaitForCompletion(CancellationTokenSource cts)
		{
			try
			{
				cts?.Cancel();
			}
			catch
			{
			}
			try
			{
				cts?.Dispose();
			}
			catch
			{
			}
			try
			{
				Wait();
			}
			catch
			{
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				CancellationTokenSource cts = Interlocked.Exchange(ref CTS, null);
				CancelAndWaitForCompletion(cts);
			}
			base.Dispose(disposing);
			IsDisposed = true;
		}
	}
}
