using System;
using IDS.Portable.Common;
using ids.portable.common.Collection;

namespace IDS.Portable.LogicalDevice.LogicalDeviceEx.Reactive
{
	public abstract class LogicalDeviceExReactiveBase : LogicalDeviceExBase<ILogicalDevice>, IObservable<ILogicalDevice>
	{
		protected class LogicalDeviceObserver : CommonDisposable
		{
			private readonly IObserver<ILogicalDevice> _observer;

			private readonly Action<LogicalDeviceObserver> _disposeAction;

			public bool IsObserverNotifyAllowed { get; private set; } = true;


			public LogicalDeviceObserver(IObserver<ILogicalDevice> observer, Action<LogicalDeviceObserver> disposeAction)
			{
				_observer = observer;
				_disposeAction = disposeAction;
			}

			public void TryOnNext(ILogicalDevice logicalDevice)
			{
				try
				{
					if (IsObserverNotifyAllowed)
					{
						_observer.OnNext(logicalDevice);
					}
				}
				catch (Exception error)
				{
					_observer.OnError(error);
					IsObserverNotifyAllowed = false;
				}
			}

			public override void Dispose(bool disposing)
			{
				_disposeAction(this);
				Dispose();
			}
		}

		private readonly ConcurrentHashSet<LogicalDeviceObserver> _currentObservers = new ConcurrentHashSet<LogicalDeviceObserver>();

		protected override string LogTag => "LogicalDeviceExReactiveBase";

		protected virtual bool NotifyOnInitialSubscribeOrDeviceAttached => true;

		public IDisposable Subscribe(IObserver<ILogicalDevice> observer)
		{
			LogicalDeviceObserver logicalDeviceObserver = new LogicalDeviceObserver(observer, RemoveObserver);
			_currentObservers.Add(logicalDeviceObserver);
			if (NotifyOnInitialSubscribeOrDeviceAttached)
			{
				foreach (ILogicalDevice attachedLogicalDevice in GetAttachedLogicalDevices())
				{
					try
					{
						if (!logicalDeviceObserver.IsDisposed)
						{
							logicalDeviceObserver.TryOnNext(attachedLogicalDevice);
						}
					}
					catch (Exception ex)
					{
						TaggedLog.Warning(LogTag, "Logical Device Reactive Extension unable to notify subscriber " + ex.Message);
					}
				}
				return logicalDeviceObserver;
			}
			return logicalDeviceObserver;
		}

		private void RemoveObserver(LogicalDeviceObserver observer)
		{
			_currentObservers.TryRemove(observer);
		}

		protected void TryAllOnNext(ILogicalDevice logicalDevice)
		{
			foreach (LogicalDeviceObserver currentObserver in _currentObservers)
			{
				try
				{
					if (!currentObserver.IsDisposed)
					{
						currentObserver.TryOnNext(logicalDevice);
					}
				}
				catch (Exception ex)
				{
					TaggedLog.Warning(LogTag, "Logical Device Reactive Extension unable to notify subscriber " + ex.Message);
				}
			}
		}

		public override void LogicalDeviceAttached(ILogicalDevice logicalDevice)
		{
			base.LogicalDeviceAttached(logicalDevice);
			if (NotifyOnInitialSubscribeOrDeviceAttached)
			{
				TryAllOnNext(logicalDevice);
			}
		}
	}
}
