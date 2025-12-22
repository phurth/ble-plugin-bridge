using System;
using System.Runtime.CompilerServices;
using IDS.Portable.Common;

namespace ids.portable.ble.Platforms.Shared.Reachability
{
	public class BleDeviceReachabilityManager : CommonDisposable, IBleDeviceReachabilityManager, ICommonDisposable, IDisposable
	{
		protected string LogTag = "BleDeviceReachabilityManager";

		private TimeSpan _lastKnownTimeout;

		private ReachabilityChangedHandler? _autoAddedReachabilityChangedHandler;

		private readonly object _lock = new object();

		private Watchdog? _checkForLostReachabilityWatchdog;

		private BleDeviceReachability _reachability = BleDeviceReachability.Unknown;

		private AssociatedDeviceDescriptionSynthesizer _associatedDeviceDescriptionSynthesizer;

		public TimeSpan MinReachabilityTimeout { get; }

		public TimeSpan MaxReachabilityTimeout { get; }

		public BleDeviceReachability Reachability
		{
			get
			{
				return _reachability;
			}
			private set
			{
				lock (_lock)
				{
					if (value != _reachability)
					{
						BleDeviceReachability reachability = _reachability;
						_reachability = value;
						if (value == BleDeviceReachability.Unknown)
						{
							string logTag = LogTag;
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(75, 2);
							defaultInterpolatedStringHandler.AppendLiteral("Reachability Changed ");
							defaultInterpolatedStringHandler.AppendFormatted(_associatedDeviceDescriptionSynthesizer());
							defaultInterpolatedStringHandler.AppendLiteral(" is ");
							defaultInterpolatedStringHandler.AppendFormatted(Reachability);
							defaultInterpolatedStringHandler.AppendLiteral(" (my be because device has been un-linked/removed)");
							TaggedLog.Information(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
						}
						else
						{
							string logTag2 = LogTag;
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(25, 2);
							defaultInterpolatedStringHandler.AppendLiteral("Reachability Changed ");
							defaultInterpolatedStringHandler.AppendFormatted(_associatedDeviceDescriptionSynthesizer());
							defaultInterpolatedStringHandler.AppendLiteral(" is ");
							defaultInterpolatedStringHandler.AppendFormatted(Reachability);
							TaggedLog.Information(logTag2, defaultInterpolatedStringHandler.ToStringAndClear());
						}
						this.ReachabilityChanged?.Invoke(reachability, value);
					}
				}
			}
		}

		public event ReachabilityChangedHandler? ReachabilityChanged;

		public BleDeviceReachabilityManager(TimeSpan minReachabilityTimeout, TimeSpan maxReachabilityTimeout, ReachabilityChangedHandler? reachabilityChangedHandler = null, AssociatedDeviceDescriptionSynthesizer? associatedDeviceDescriptionSynthesizer = null)
		{
			MinReachabilityTimeout = minReachabilityTimeout;
			MaxReachabilityTimeout = maxReachabilityTimeout;
			_lastKnownTimeout = minReachabilityTimeout;
			_autoAddedReachabilityChangedHandler = reachabilityChangedHandler;
			if (reachabilityChangedHandler != null)
			{
				ReachabilityChanged += reachabilityChangedHandler;
			}
			_associatedDeviceDescriptionSynthesizer = associatedDeviceDescriptionSynthesizer ?? new AssociatedDeviceDescriptionSynthesizer(DefaultAssociatedDeviceDescriptionSynthesizer);
		}

		public static string DefaultAssociatedDeviceDescriptionSynthesizer()
		{
			return "Unknown Device";
		}

		public void DeviceReachableUntil(TimeSpan timeoutTimeSpan)
		{
			if (base.IsDisposed)
			{
				return;
			}
			if (timeoutTimeSpan < MinReachabilityTimeout)
			{
				if (timeoutTimeSpan != _lastKnownTimeout)
				{
					_lastKnownTimeout = timeoutTimeSpan;
					string logTag = LogTag;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(33, 3);
					defaultInterpolatedStringHandler.AppendFormatted(_associatedDeviceDescriptionSynthesizer());
					defaultInterpolatedStringHandler.AppendLiteral(": ");
					defaultInterpolatedStringHandler.AppendFormatted(timeoutTimeSpan);
					defaultInterpolatedStringHandler.AppendLiteral(" increased to minimum value of ");
					defaultInterpolatedStringHandler.AppendFormatted(MinReachabilityTimeout);
					TaggedLog.Warning(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
				}
				timeoutTimeSpan = MinReachabilityTimeout;
			}
			else if (timeoutTimeSpan > MaxReachabilityTimeout)
			{
				if (timeoutTimeSpan != _lastKnownTimeout)
				{
					_lastKnownTimeout = timeoutTimeSpan;
					string logTag2 = LogTag;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(33, 3);
					defaultInterpolatedStringHandler.AppendFormatted(_associatedDeviceDescriptionSynthesizer());
					defaultInterpolatedStringHandler.AppendLiteral(": ");
					defaultInterpolatedStringHandler.AppendFormatted(timeoutTimeSpan);
					defaultInterpolatedStringHandler.AppendLiteral(" decreased to maximum value of ");
					defaultInterpolatedStringHandler.AppendFormatted(MinReachabilityTimeout);
					TaggedLog.Warning(logTag2, defaultInterpolatedStringHandler.ToStringAndClear());
				}
				timeoutTimeSpan = MaxReachabilityTimeout;
			}
			if (_checkForLostReachabilityWatchdog == null)
			{
				_checkForLostReachabilityWatchdog = new Watchdog(timeoutTimeSpan, MakeUnreachable, autoStartOnFirstPet: true);
			}
			if (_checkForLostReachabilityWatchdog!.PetTimeout != timeoutTimeSpan)
			{
				string logTag3 = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(49, 3);
				defaultInterpolatedStringHandler.AppendFormatted(_associatedDeviceDescriptionSynthesizer());
				defaultInterpolatedStringHandler.AppendLiteral(": Reachability timespan changed from ");
				defaultInterpolatedStringHandler.AppendFormatted(_checkForLostReachabilityWatchdog!.PetTimeout);
				defaultInterpolatedStringHandler.AppendLiteral(" to ");
				defaultInterpolatedStringHandler.AppendFormatted(timeoutTimeSpan);
				defaultInterpolatedStringHandler.AppendLiteral(" IGNORED");
				TaggedLog.Warning(logTag3, defaultInterpolatedStringHandler.ToStringAndClear());
			}
			Reachability = BleDeviceReachability.Reachable;
			_checkForLostReachabilityWatchdog!.TryPet(autoReset: true);
		}

		private void MakeUnreachable()
		{
			if (!base.IsDisposed)
			{
				Reachability = BleDeviceReachability.Unreachable;
			}
		}

		public override void Dispose(bool disposing)
		{
			Reachability = BleDeviceReachability.Unknown;
			ReachabilityChangedHandler autoAddedReachabilityChangedHandler = _autoAddedReachabilityChangedHandler;
			if (autoAddedReachabilityChangedHandler != null)
			{
				try
				{
					ReachabilityChanged -= autoAddedReachabilityChangedHandler;
				}
				catch
				{
				}
			}
			_autoAddedReachabilityChangedHandler = null;
			_associatedDeviceDescriptionSynthesizer = DefaultAssociatedDeviceDescriptionSynthesizer;
			this.ReachabilityChanged = null;
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(17, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Reachability ");
			defaultInterpolatedStringHandler.AppendFormatted(_associatedDeviceDescriptionSynthesizer());
			defaultInterpolatedStringHandler.AppendLiteral(" is ");
			defaultInterpolatedStringHandler.AppendFormatted(Reachability);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
