using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.FirmwareUpdate;
using IDS.Portable.LogicalDevice.LogicalDeviceEx;
using IDS.Portable.LogicalDevice.LogicalDeviceEx.Reactive;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceService : CommonNotifyPropertyChanged, ILogicalDeviceServiceIdsCan, ILogicalDeviceService, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		private const string LogTag = "LogicalDeviceService";

		private readonly object _lock = new object();

		private static readonly HashSet<Assembly> AutoRegisteredLogicalDeviceExFactories;

		private bool _sessionEnabled = true;

		public static readonly TimeSpan DefaultRealTimeClockUpdateInterval;

		private readonly BackgroundOperationDisposable _updateRealTimeClockOperation;

		private int _realTimeClockUpdateIntervalMs = (int)DefaultRealTimeClockUpdateInterval.TotalMilliseconds;

		private readonly ConcurrentDictionary<Type, LogicalDeviceExclusiveOperation> _cancelableExclusiveOperation = new ConcurrentDictionary<Type, LogicalDeviceExclusiveOperation>();

		private int _isDisposed;

		public LogicalDeviceServiceOptions Options { get; }

		public List<ILogicalDeviceTag> DefaultDeviceTagList { get; } = new List<ILogicalDeviceTag>();


		public List<ILogicalDeviceFactory> LogicalDeviceFactoryList { get; private set; } = new List<ILogicalDeviceFactory>();


		public ILogicalDeviceProductManager? ProductManager { get; private set; }

		public ILogicalDeviceManager? DeviceManager { get; private set; }

		public ILogicalDeviceSourceDirectManager DeviceSourceManager { get; }

		public ILogicalDeviceFirmwareUpdateManager FirmwareUpdateManager { get; }

		public ILogicalDeviceRemoteManager? RemoteManager { get; private set; }

		public MakeDeviceName MakeDeviceName { get; set; } = MakeDeviceNameDefault;


		public MakeDeviceName MakeDeviceNameShort { get; set; } = MakeDeviceNameShortDefault;


		public MakeDeviceName MakeDeviceNameShortAbbreviated { get; set; } = MakeDeviceNameShortAbbreviatedDefault;


		public IN_MOTION_LOCKOUT_LEVEL InMotionLockoutLevel
		{
			get
			{
				IN_MOTION_LOCKOUT_LEVEL iN_MOTION_LOCKOUT_LEVEL = (byte)0;
				foreach (ILogicalDeviceSourceDirect item in DeviceSourceManager.FindDeviceSources((ILogicalDeviceSourceDirect ds) => ds.IsDeviceSourceActive))
				{
					IN_MOTION_LOCKOUT_LEVEL iN_MOTION_LOCKOUT_LEVEL2 = item?.InTransitLockoutLevel ?? ((IN_MOTION_LOCKOUT_LEVEL)(byte)0);
					if ((byte)iN_MOTION_LOCKOUT_LEVEL2 > (byte)iN_MOTION_LOCKOUT_LEVEL)
					{
						iN_MOTION_LOCKOUT_LEVEL = iN_MOTION_LOCKOUT_LEVEL2;
					}
				}
				return iN_MOTION_LOCKOUT_LEVEL;
			}
		}

		public bool SessionsEnabled
		{
			get
			{
				return _sessionEnabled;
			}
			set
			{
				if (!SetBackingField(ref _sessionEnabled, value, "SessionsEnabled"))
				{
					return;
				}
				TaggedLog.Debug("LogicalDeviceService", string.Format("{0} changing from {1} to {2} ", "SessionsEnabled", _sessionEnabled, value));
				if (!value)
				{
					DeviceSourceManager.ForeachDeviceSource(delegate(ILogicalDeviceSourceDirectConnection dm)
					{
						dm.SessionManager?.CloseAllSessions();
					});
				}
			}
		}

		public DateTime RealTimeClockTime
		{
			get
			{
				return (DeviceSourceManager.FindFirstDeviceSource<ILogicalDeviceSourceDirect>(rtcDeviceSourceFilter) as ILogicalDeviceSourceDirectRealTimeClock)?.GetRealTimeClockTime ?? DateTime.MinValue;
				static bool rtcDeviceSourceFilter(ILogicalDeviceSourceDirect deviceSource)
				{
					if (!deviceSource.IsDeviceSourceActive || !(deviceSource is ILogicalDeviceSourceDirectRealTimeClock logicalDeviceSourceDirectRealTimeClock) || logicalDeviceSourceDirectRealTimeClock.GetRealTimeClockTime.Equals(DateTime.MinValue))
					{
						return false;
					}
					return true;
				}
			}
			set
			{
				_updateRealTimeClockOperation.Start();
				Task.WhenAll(Enumerable.Select((IEnumerable<ILogicalDeviceSourceDirectRealTimeClock>)DeviceSourceManager.FindDeviceSources((ILogicalDeviceSourceDirectRealTimeClock ds) => ds.IsDeviceSourceActive), (Func<ILogicalDeviceSourceDirectRealTimeClock, Task>)async delegate(ILogicalDeviceSourceDirectRealTimeClock directManagerRtc)
				{
					try
					{
						await directManagerRtc.SetRealTimeClockTimeAsync(value, CancellationToken.None);
					}
					catch (Exception ex)
					{
						TaggedLog.Warning("LogicalDeviceService", $"Unable to set RealTime Clock {directManagerRtc}: {ex.Message}");
					}
				})).ContinueWith(delegate
				{
					NotifyPropertyChanged("RealTimeClockTime");
				});
			}
		}

		public bool IsDisposed => _isDisposed != 0;

		public ILogicalDeviceSourceDirect? GetPrimaryDeviceSourceDirect(ILogicalDevice logicalDevice)
		{
			return DeviceSourceManager.GetPrimaryDeviceSource<ILogicalDeviceSourceDirect>(logicalDevice);
		}

		public static string MakeDeviceNameDefault(ILogicalDevice logicalDevice)
		{
			return logicalDevice.CustomDeviceName ?? logicalDevice.LogicalId.ToString(LogicalDeviceIdFormat.FunctionNameCommon);
		}

		public static string MakeDeviceNameShortDefault(ILogicalDevice logicalDevice)
		{
			return logicalDevice.CustomDeviceNameShort ?? logicalDevice.LogicalId.ToString(LogicalDeviceIdFormat.FunctionNameShortCommon);
		}

		public static string MakeDeviceNameShortAbbreviatedDefault(ILogicalDevice logicalDevice)
		{
			return logicalDevice.CustomDeviceNameShortAbbreviated ?? logicalDevice.LogicalId.ToString(LogicalDeviceIdFormat.FunctionNameShortAbbreviatedCommon);
		}

		static LogicalDeviceService()
		{
			AutoRegisteredLogicalDeviceExFactories = new HashSet<Assembly>();
			DefaultRealTimeClockUpdateInterval = TimeSpan.FromMilliseconds(5000.0);
			JsonSerializer.AutoRegisterJsonSerializersFromAssembly(Assembly.GetExecutingAssembly());
		}

		private static PRODUCT_ID CoreForceRegisterProductId(ushort value, int assemblyNumber, string name)
		{
			try
			{
				PRODUCT_ID pRODUCT_ID = (typeof(PRODUCT_ID).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[3]
				{
					typeof(ushort),
					typeof(int),
					typeof(string)
				}, null)?.Invoke(new object[3] { value, assemblyNumber, name }) as PRODUCT_ID) ?? PRODUCT_ID.UNKNOWN;
				TaggedLog.Warning("LogicalDeviceService", $"CoreForceRegisterProductId PRODUCT_ID for {name} Created: {pRODUCT_ID}");
				return pRODUCT_ID;
			}
			catch (Exception ex)
			{
				TaggedLog.Error("LogicalDeviceService", "CoreForceRegisterProductId Adding Custom " + name + " may no longer be required: " + ex.Message + "\n " + ex.InnerException?.Message);
			}
			return PRODUCT_ID.UNKNOWN;
		}

		private static DEVICE_TYPE CoreForceRegisterDeviceType(byte value, string name)
		{
			try
			{
				DEVICE_TYPE dEVICE_TYPE = (typeof(DEVICE_TYPE).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[3]
				{
					typeof(byte),
					typeof(string),
					typeof(ICON)
				}, null)?.Invoke(new object[3]
				{
					value,
					name,
					ICON.GENERIC
				}) as DEVICE_TYPE) ?? ((DEVICE_TYPE)(byte)0);
				TaggedLog.Warning("LogicalDeviceService", $"CoreForceRegisterDeviceType DEVICE_TYPE for {name} Created: {dEVICE_TYPE}");
				return dEVICE_TYPE;
			}
			catch (Exception ex)
			{
				TaggedLog.Error("LogicalDeviceService", "CoreForceRegisterDeviceType Adding Custom " + name + " may no longer be required: " + ex.Message + "\n " + ex.InnerException?.Message);
			}
			return (byte)0;
		}

		public LogicalDeviceService(LogicalDeviceServiceOptions options = LogicalDeviceServiceOptions.None)
		{
			Options = options;
			if (MainThread.RequestMainThreadActionFactory == null)
			{
				TaggedLog.Warning("LogicalDeviceService", "WARNING: RequestMainThreadActionFactory hasn't been initialized.");
			}
			_updateRealTimeClockOperation = new BackgroundOperationDisposable((BackgroundOperation.BackgroundOperationAction)UpdateRealTimeClockTask);
			RegisterLogicalDeviceFactory(new DefaultLogicalDeviceFactory());
			ProductManager = new LogicalDeviceProductManager(this);
			DeviceManager = new LogicalDeviceManager(this);
			DeviceSourceManager = new LogicalDeviceSourceDirectManager(this);
			FirmwareUpdateManager = new LogicalDeviceFirmwareUpdateManager(this);
			RegisterLogicalDeviceExFactory(LogicalDeviceExVoltageBattery.LogicalDeviceExFactory);
			RegisterLogicalDeviceExFactory(LogicalDeviceExTemperatureInside.LogicalDeviceExFactory);
			RegisterLogicalDeviceExFactory(LogicalDeviceExTemperatureOutside.LogicalDeviceExFactory);
			if (Options.HasFlag(LogicalDeviceServiceOptions.AutoRegisterReactiveOnlineChangedExtension))
			{
				RegisterLogicalDeviceExFactory(LogicalDeviceExDeviceAttached.LogicalDeviceExFactory);
				RegisterLogicalDeviceExFactory(LogicalDeviceExDeviceDetached.LogicalDeviceExFactory);
				RegisterLogicalDeviceExFactory(LogicalDeviceExReactiveOnlineChanged.LogicalDeviceExFactory);
			}
			if (Options.HasFlag(LogicalDeviceServiceOptions.AutoRegisterReactiveStatusChangedExtension))
			{
				RegisterLogicalDeviceExFactory(LogicalDeviceExReactiveStatusChanged.LogicalDeviceExFactory);
			}
			if (Options.HasFlag(LogicalDeviceServiceOptions.AutoRegisterReactiveAlertChangedExtension))
			{
				RegisterLogicalDeviceExFactory(LogicalDeviceExReactiveAlertChanged.LogicalDeviceExFactory);
			}
		}

		public void RegisterLogicalDeviceFactory(ILogicalDeviceFactory factory)
		{
			LogicalDeviceFactoryList.Insert(0, factory);
		}

		public void RegisterLogicalDeviceExFactory(LogicalDeviceExFactory factory)
		{
			(DeviceManager ?? throw new LogicalDeviceException("DeviceManager must be initialized before factory is registered"))!.RegisterLogicalDeviceExFactory(factory);
		}

		public void RegisterLogicalDeviceExFactory<TLogicalDevice>(Func<TLogicalDevice, ILogicalDeviceEx> factory) where TLogicalDevice : class, ILogicalDevice
		{
			Func<TLogicalDevice, ILogicalDeviceEx> factory2 = factory;
			DeviceManager?.RegisterLogicalDeviceExFactory(LogicalDeviceFactory);
			ILogicalDeviceEx? LogicalDeviceFactory(ILogicalDevice foundLogicalDevice)
			{
				if (!(foundLogicalDevice is TLogicalDevice arg))
				{
					return null;
				}
				return factory2(arg);
			}
		}

		public void AutoRegisterLogicalDeviceExFactory(Assembly assembly)
		{
			lock (AutoRegisteredLogicalDeviceExFactories)
			{
				if (AutoRegisteredLogicalDeviceExFactories.Contains(assembly))
				{
					return;
				}
				AutoRegisteredLogicalDeviceExFactories.Add(assembly);
				Type typeFromHandle = typeof(AutoRegisterLogicalDeviceExtensionAttribute);
				Type typeFromHandle2 = typeof(ILogicalDeviceEx);
				Type[] types = assembly.GetTypes();
				foreach (Type type in types)
				{
					if (!typeFromHandle2.IsAssignableFrom(type) || !type.IsDefined(typeFromHandle, false))
					{
						continue;
					}
					Type typeFromHandle3 = typeof(ILogicalDevice);
					bool flag = false;
					MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
					foreach (MethodInfo method in methods)
					{
						try
						{
							if (method.ReturnType != typeFromHandle2)
							{
								continue;
							}
							ParameterInfo[] parameters = method.GetParameters();
							if (parameters.Length != 1 || parameters[0].ParameterType != typeFromHandle3)
							{
								continue;
							}
							TaggedLog.Information("LogicalDeviceService", "Auto Register Logical Device Extension " + type.Name);
							RegisterLogicalDeviceExFactory((LogicalDeviceExFactory)((ILogicalDevice ld) => method.Invoke(null, new object[1] { ld }) as ILogicalDeviceEx));
							flag = true;
							break;
						}
						catch (Exception ex)
						{
							TaggedLog.Warning("LogicalDeviceService", type.Name + " was attributed with " + typeFromHandle.Name + " but encountered in error parsing methods: " + ex.Message);
						}
					}
					if (!flag)
					{
						TaggedLog.Warning("LogicalDeviceService", type.Name + " was attributed with " + typeFromHandle.Name + " but didn't implement factory static method conforming to static ILogicalDeviceEx? LogicalDeviceExFactory(ILogicalDevice logicalDevice);");
					}
				}
			}
		}

		public void RegisterRemoteManager(ILogicalDeviceRemoteManager remoteManager)
		{
			lock (_lock)
			{
				RemoteManager?.StopRemote();
				RemoteManager = remoteManager;
			}
		}

		public void UpdateInMotionLockoutLevel()
		{
			TaggedLog.Debug("LogicalDeviceService", "Update In Motion Lockout Level");
			NotifyPropertyChanged("InMotionLockoutLevel");
		}

		public void EnableRealTimeClockUpdates(TimeSpan timeInterval)
		{
			if (IsDisposed)
			{
				TaggedLog.Warning("LogicalDeviceService", "Unable to enable RTC updates because Logical Device Service is disposed.");
				return;
			}
			if (timeInterval == TimeSpan.Zero || timeInterval == TimeSpan.MaxValue)
			{
				_updateRealTimeClockOperation.Stop();
				return;
			}
			_realTimeClockUpdateIntervalMs = (int)timeInterval.TotalMilliseconds;
			_updateRealTimeClockOperation.Start();
		}

		public void DisableRealTimeClockUpdates()
		{
			EnableRealTimeClockUpdates(TimeSpan.Zero);
		}

		private void UpdateRealTimeClockTask(CancellationToken ct)
		{
			Task.Run(async delegate
			{
				DateTime dtNow = RealTimeClockTime;
				while (!ct.IsCancellationRequested)
				{
					if (RealTimeClockTime != dtNow)
					{
						NotifyPropertyChanged("RealTimeClockTime");
					}
					await Task.Delay(_realTimeClockUpdateIntervalMs, ct).TryAwaitAsync();
				}
			}, ct);
		}

		public void Start()
		{
			DeviceSourceManager.ForeachDeviceSource(delegate(ILogicalDeviceSourceDirectConnection directManager)
			{
				directManager.Start();
			});
		}

		public void Start(ILogicalDeviceSourceDirect deviceSource)
		{
			if (deviceSource == null)
			{
				throw new ArgumentNullException("deviceSource");
			}
			List<ILogicalDeviceSourceDirect> deviceSources = new List<ILogicalDeviceSourceDirect> { deviceSource };
			Start(deviceSources);
		}

		public void Start(List<ILogicalDeviceSourceDirect> deviceSources)
		{
			if (deviceSources == null)
			{
				throw new ArgumentNullException("deviceSources");
			}
			StopDeviceSourcesNotInList(deviceSources);
			DeviceSourceManager.SetDeviceSourceList(deviceSources);
			DeviceSourceManager.ForeachDeviceSource(delegate(ILogicalDeviceSourceDirectConnection ds)
			{
				ds.Start();
			});
		}

		public void Stop()
		{
			DeviceSourceManager.ForeachDeviceSource(delegate(ILogicalDeviceSourceDirectConnection ds)
			{
				ds.Stop();
			});
		}

		private void StopDeviceSourcesNotInList(List<ILogicalDeviceSourceDirect> deviceSources)
		{
			List<ILogicalDeviceSourceDirect> deviceSources2 = deviceSources;
			DeviceSourceManager.ForeachDeviceSource(delegate(ILogicalDeviceSourceDirectConnection ds)
			{
				if (!deviceSources2.Contains(ds))
				{
					ds.Stop();
				}
			});
		}

		public LogicalDeviceExclusiveOperation GetExclusiveOperation<TClass>()
		{
			Type typeFromHandle = typeof(TClass);
			if (_cancelableExclusiveOperation.TryGetValue(typeFromHandle, out var result))
			{
				return result;
			}
			result = new LogicalDeviceExclusiveOperation();
			_cancelableExclusiveOperation[typeFromHandle] = result;
			return result;
		}

		public void TryDispose()
		{
			try
			{
				if (!IsDisposed)
				{
					Dispose();
				}
			}
			catch
			{
			}
		}

		public void Dispose()
		{
			if (Options.HasFlag(LogicalDeviceServiceOptions.SingletonMode))
			{
				TaggedLog.Error("LogicalDeviceService", "LogicalDeviceService ignored Dispose because operating in Singleton Mode.");
			}
			else if (!IsDisposed && Interlocked.Exchange(ref _isDisposed, 1) == 0)
			{
				Dispose(disposing: true);
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			lock (_lock)
			{
				if (Options.HasFlag(LogicalDeviceServiceOptions.SingletonMode))
				{
					TaggedLog.Error("LogicalDeviceService", "LogicalDeviceService ignored Dispose because operating in Singleton Mode.");
					return;
				}
				RemoteManager?.StopRemote();
				RemoteManager = null;
				DeviceManager?.Dispose();
				DeviceManager = null;
				ProductManager?.Dispose();
				ProductManager = null;
				_updateRealTimeClockOperation.Stop();
				_updateRealTimeClockOperation.TryDispose();
			}
		}
	}
}
