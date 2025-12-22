using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.OS;

namespace ids.portable.ble.Platforms.Android
{
	public abstract class BroadcastReceiverAsync<T> : BroadcastReceiver
	{
		private readonly Context _context;

		protected readonly TaskCompletionSource<T> _tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

		private readonly CancellationTokenSource _cts;

		private readonly CancellationTokenRegistration _registration;

		private readonly Intent? _intent;

		public Task<T> Task => _tcs.Task;

		private BroadcastReceiverAsync(Context context, CancellationToken cancellationToken)
		{
			_context = context;
			_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			_registration = _cts.Token.Register(delegate
			{
				_tcs?.TrySetCanceled();
			});
		}

		protected BroadcastReceiverAsync(Context context, CancellationToken cancellationToken, IntentFilter intentFilter)
			: this(context, cancellationToken)
		{
			_intent = context.RegisterReceiver(this, intentFilter);
		}

		protected BroadcastReceiverAsync(Context context, CancellationToken cancellationToken, IntentFilter intentFilter, string broadcastPermission, Handler scheduler)
			: this(context, cancellationToken)
		{
			_intent = context.RegisterReceiver(this, intentFilter, broadcastPermission, scheduler);
		}

		protected BroadcastReceiverAsync(Context context, CancellationToken cancellationToken, IntentFilter intentFilter, ActivityFlags activityFlags)
			: this(context, cancellationToken)
		{
		}

		protected BroadcastReceiverAsync(Context context, CancellationToken cancellationToken, IntentFilter intentFilter, string broadcastPermission, Handler scheduler, ActivityFlags activityFlags)
			: this(context, cancellationToken)
		{
		}

		public sealed override void OnReceive(Context? context, Intent? intent)
		{
			try
			{
				if (TryOnReceive(context, intent, out var result))
				{
					_tcs.TrySetResult(result);
				}
			}
			catch (Exception exception)
			{
				_tcs.SetException(exception);
			}
		}

		protected abstract bool TryOnReceive(Context? context, Intent? intent, out T result);

		protected override void Dispose(bool disposing)
		{
			_context.UnregisterReceiver(this);
			_intent?.Dispose();
			_cts.Cancel();
			_cts.Dispose();
			_tcs.TrySetCanceled();
			_registration.Dispose();
			base.Dispose(disposing);
		}
	}
}
