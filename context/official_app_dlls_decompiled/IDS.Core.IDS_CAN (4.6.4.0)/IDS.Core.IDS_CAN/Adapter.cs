using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IDS.Core.Events;
using IDS.Core.Tasks;

namespace IDS.Core.IDS_CAN
{
	public class Adapter : CAN.Adapter<CAN.MessageBuffer>, IAdapter, CAN.IAdapter<CAN.MessageBuffer>, Comm.IAdapter<CAN.MessageBuffer>, Comm.IAdapter, IEventSender, IDisposableManager, IDisposable, System.IDisposable, CAN.IAdapter
	{
		public new enum ICON
		{
			CROSS,
			LED_OFF,
			LED_BLUE,
			LED_GREEN,
			LED_YELLOW,
			LED_ORANGE,
			LED_RED,
			LED_PURPLE,
			INFO,
			QUESTION,
			EXCLAMATION,
			CLOSE,
			PLUS,
			CHECKMARK,
			NEXT,
			UP,
			DOWN,
			STAR
		}

		public abstract class Object : DisposableManager
		{
			public IAdapter Adapter { get; private set; }

			public ILocalDevice LocalHost => Adapter?.LocalHost;

			protected SubscriptionManager Subscriptions { get; private set; } = new SubscriptionManager();


			public Object(Adapter adapter)
			{
				Adapter = adapter;
				adapter.AddDisposable(this);
			}

			public override void Dispose(bool disposing)
			{
				base.Dispose(disposing);
				if (disposing)
				{
					Adapter = null;
					Subscriptions?.Dispose();
					Subscriptions = null;
				}
			}
		}

		public abstract class BackgroundTaskObject : Object
		{
			public BackgroundTaskObject(Adapter adapter)
				: base(adapter)
			{
				adapter.BackgroundTasks.Add(this);
			}

			public abstract void BackgroundTask();
		}

		private class AddressDetectManager : BackgroundTaskObject
		{
			private class AddressDetector
			{
				private readonly IAdapter Adapter;

				private readonly ADDRESS Address;

				private List<ADDRESS> UnusedAddressList = new List<ADDRESS>();

				private Timer Timeout = new Timer();

				private bool Detected;

				public AddressDetector(ADDRESS address, AddressDetectManager mgr)
				{
					Address = address;
					UnusedAddressList = mgr.UnusedAddressList;
					Adapter = mgr.Adapter;
				}

				public void Touch()
				{
					if (!Address.IsValidDeviceAddress)
					{
						return;
					}
					Timeout.Reset();
					if (Detected)
					{
						return;
					}
					lock (UnusedAddressList)
					{
						if (!Detected)
						{
							Detected = true;
							UnusedAddressList.Remove(Address);
						}
					}
				}

				public void Kill()
				{
					if (!Detected)
					{
						return;
					}
					lock (UnusedAddressList)
					{
						if (Detected)
						{
							Detected = false;
							UnusedAddressList.Add(Address);
							if (Adapter.Devices.GetDeviceByAddress(Address) is RemoteDevice remoteDevice)
							{
								remoteDevice.GoOffline();
							}
						}
					}
				}

				public void BackgroundTask()
				{
					if (Detected && (!Adapter.IsConnected || Timeout.ElapsedTime >= ADDRESS_DETECTED_TIMEOUT))
					{
						Kill();
					}
				}
			}

			private AddressDetector[] Address = new AddressDetector[256];

			private List<ADDRESS> UnusedAddressList = new List<ADDRESS>();

			private Timer ListenTime = new Timer();

			private IN_MOTION_LOCKOUT_LEVEL mInMotionLockoutLevel = (byte)0;

			private Timer[] InMotionLockoutTimer = new Timer[4];

			private NetworkInMotionLockoutLevelChangedEvent InMotionLockoutEvent;

			public IN_MOTION_LOCKOUT_LEVEL InMotionLockoutLevel
			{
				get
				{
					return mInMotionLockoutLevel;
				}
				private set
				{
					if (mInMotionLockoutLevel != value)
					{
						IN_MOTION_LOCKOUT_LEVEL prev = mInMotionLockoutLevel;
						mInMotionLockoutLevel = value;
						InMotionLockoutEvent.Publish(value, prev);
					}
				}
			}

			public AddressDetectManager(Adapter adapter)
				: base(adapter)
			{
				for (int i = 0; i < Address.Length; i++)
				{
					Address[i] = new AddressDetector((byte)i, this);
				}
				List<ADDRESS> list = new List<ADDRESS>();
				foreach (ADDRESS item2 in ADDRESS.GetEnumerator())
				{
					if (item2.IsValidDeviceAddress)
					{
						list.Add(item2);
					}
				}
				while (list.Count > 0)
				{
					int num = ThreadLocalRandom.Next(list.Count);
					ADDRESS item = list[num];
					list.RemoveAt(num);
					UnusedAddressList.Add(item);
				}
				for (int j = 0; j < InMotionLockoutTimer.Length; j++)
				{
					InMotionLockoutTimer[j] = new Timer();
				}
				base.Adapter.Events.Subscribe<Comm.AdapterOpenedEvent>(OnAdapterOpened, SubscriptionType.Strong, base.Subscriptions);
				base.Adapter.Events.Subscribe<Comm.AdapterClosedEvent>(OnAdapterClosed, SubscriptionType.Strong, base.Subscriptions);
				base.Adapter.Events.Subscribe<AdapterRxEvent>(OnAdapterRx, SubscriptionType.Strong, base.Subscriptions);
				InMotionLockoutEvent = new NetworkInMotionLockoutLevelChangedEvent(base.Adapter);
			}

			public override void Dispose(bool disposing)
			{
				if (disposing)
				{
					UnusedAddressList = null;
					Address = null;
				}
			}

			private void OnAdapterOpened(Comm.AdapterOpenedEvent message)
			{
				ListenTime.Reset();
				AddressDetector[] address = Address;
				for (int i = 0; i < address.Length; i++)
				{
					address[i].Kill();
				}
			}

			private void OnAdapterClosed(Comm.AdapterClosedEvent message)
			{
				AddressDetector[] address = Address;
				for (int i = 0; i < address.Length; i++)
				{
					address[i].Kill();
				}
			}

			private void OnAdapterRx(AdapterRxEvent rx)
			{
				if (!base.Adapter.IsConnected)
				{
					return;
				}
				if (rx.SourceAddress.IsValidDeviceAddress)
				{
					Address[(byte)rx.SourceAddress].Touch();
				}
				if ((byte)rx.MessageType != 0 || rx.Count != 8)
				{
					return;
				}
				if (rx.SourceAddress == ADDRESS.BROADCAST)
				{
					ADDRESS aDDRESS = rx[0];
					if (aDDRESS.IsValidDeviceAddress)
					{
						Address[(byte)aDDRESS].Touch();
					}
				}
				else if (rx.SourceAddress.IsValidDeviceAddress)
				{
					IN_MOTION_LOCKOUT_LEVEL inMotionLockoutLevel = new NETWORK_STATUS(rx[0], rx[1]).InMotionLockoutLevel;
					InMotionLockoutTimer[(byte)inMotionLockoutLevel].Reset();
					if ((byte)InMotionLockoutLevel < (byte)inMotionLockoutLevel)
					{
						InMotionLockoutLevel = inMotionLockoutLevel;
					}
				}
			}

			public ADDRESS GetUnusedDeviceAddress()
			{
				if (!base.Adapter.IsConnected)
				{
					return ADDRESS.INVALID;
				}
				if (ListenTime.ElapsedTime < BUS_LISTEN_TIME)
				{
					return ADDRESS.INVALID;
				}
				lock (UnusedAddressList)
				{
					if (UnusedAddressList.Count <= 0)
					{
						return ADDRESS.INVALID;
					}
					ADDRESS aDDRESS = UnusedAddressList[0];
					UnusedAddressList.RemoveAt(0);
					UnusedAddressList.Add(aDDRESS);
					return aDDRESS;
				}
			}

			public override void BackgroundTask()
			{
				if ((byte)InMotionLockoutLevel > 0)
				{
					byte b = InMotionLockoutLevel;
					if (InMotionLockoutTimer[b].ElapsedTime >= IN_MOTION_LOCKOUT_TIMEOUT)
					{
						while (true)
						{
							if ((b = (byte)(b - 1)) <= 0)
							{
								InMotionLockoutLevel = (byte)0;
								break;
							}
							if (InMotionLockoutTimer[b].ElapsedTime < IN_MOTION_LOCKOUT_TIMEOUT)
							{
								InMotionLockoutLevel = b;
								break;
							}
						}
					}
				}
				AddressDetector[] address = Address;
				for (int i = 0; i < address.Length; i++)
				{
					address[i].BackgroundTask();
				}
			}
		}

		private const int MIN_BACKGROUND_MESSAGE_TX_RATE = 25;

		private const int DEFAULT_BACKGROUND_MESSAGE_TX_RATE = 400;

		private static readonly TimeSpan BUS_LISTEN_TIME = TimeSpan.FromSeconds(1.0);

		private static readonly TimeSpan ADDRESS_DETECTED_TIMEOUT = TimeSpan.FromSeconds(5.0);

		private static readonly TimeSpan IN_MOTION_LOCKOUT_TIMEOUT = TimeSpan.FromSeconds(5.0);

		private CAN.Adapter<CAN.MessageBuffer> Bridge;

		private PeriodicTask mBackgroundTask;

		private readonly List<BackgroundTaskObject> BackgroundTasks = new List<BackgroundTaskObject>();

		private readonly AddressDetectManager AddressDetector;

		private readonly AdapterRxEvent RxEvent;

		private readonly MAC mMAC;

		private readonly LocalProduct mLocalProduct;

		private readonly LocalDevice mLocalHost;

		private readonly ProductManager mRemoteProductManager;

		private LocalProductManager mLocalProductManager;

		public ADAPTER_OPTIONS Options { get; set; }

		public override Comm.IPhysicalAddress MAC => mMAC;

		public IDeviceDiscoverer Devices { get; private set; }

		public IProductManager Products => mRemoteProductManager;

		public ILocalProductManager LocalProducts => mLocalProductManager;

		public ICircuitManager Circuits { get; private set; }

		public int NumDevicesDetectedOnNetwork => Devices.NumDevicesDetectedOnNetwork;

		public ILocalDevice LocalHost => mLocalHost;

		public INetworkTime Clock { get; private set; }

		public IN_MOTION_LOCKOUT_LEVEL NetworkLevelInMotionLockoutLevel => AddressDetector.InMotionLockoutLevel;

		private new int BackgroundTxMessagesPerSecond
		{
			get
			{
				return base.BackgroundTxMessagesPerSecond;
			}
			set
			{
				base.BackgroundTxMessagesPerSecond = Math.Max(25, value);
			}
		}

		protected override async Task<bool> ConnectAsync(AsyncOperation obj)
		{
			return await Bridge.OpenAsync(obj);
		}

		protected override async Task<bool> DisconnectAsync(AsyncOperation obj)
		{
			return await Bridge.CloseAsync(obj);
		}

		protected override bool TransmitRaw(CAN.MessageBuffer message)
		{
			return Bridge.Transmit(message);
		}

		public ADDRESS GetUnusedDeviceAddress()
		{
			return AddressDetector.GetUnusedDeviceAddress();
		}

		public Adapter(string name, string software_part_number, DEVICE_ID id, CAN.Adapter<CAN.MessageBuffer> bridge, ADAPTER_OPTIONS options)
			: base(name, bridge.BaudRate)
		{
			Options = options;
			if (bridge.IsConnected)
			{
				throw new InvalidOperationException("new IDS_CAN.Adapter(): CAN bridge must be in the disconnected state during construction");
			}
			Bridge = bridge;
			AddDisposable(bridge);
			mMAC = new MAC(bridge.MAC);
			mLocalProductManager = new LocalProductManager(this);
			mLocalProduct = new LocalProduct(this, new MAC(bridge.MAC), id.ProductID, (byte)28, software_part_number);
			mLocalHost = CreateDefaultLocalHost(mLocalProduct, id.DeviceType, id.DeviceInstance, id.FunctionName, id.FunctionInstance, id.DeviceCapabilities);
			AddDisposable(mLocalHost);
			Clock = new NetworkTime(this);
			AddressDetector = new AddressDetectManager(this);
			Devices = new DeviceDiscoverer(this);
			Circuits = new CircuitManager(this);
			mRemoteProductManager = new ProductManager(this);
			RxEvent = new AdapterRxEvent(this);
			bridge.Events.Subscribe<Comm.AdapterOpenedEvent>(OnBridgeAdapterOpened, SubscriptionType.Strong, Subscriptions);
			bridge.Events.Subscribe<Comm.AdapterClosedEvent>(OnBridgeAdapterClosed, SubscriptionType.Strong, Subscriptions);
			bridge.Events.Subscribe<Comm.AdapterRxEvent<CAN.MessageBuffer>>(OnBridgeAdapterRx, SubscriptionType.Strong, Subscriptions);
			base.Events.Subscribe<Comm.AdapterOpenedEvent>(OnAdapterOpened, SubscriptionType.Strong, Subscriptions);
			base.Events.Subscribe<Comm.AdapterRxEvent<CAN.MessageBuffer>>(OnAdapterMessageRx, SubscriptionType.Strong, Subscriptions);
			mBackgroundTask = new PeriodicTask(AdapterBackgroundTask, TimeSpan.FromMilliseconds(40.0), PeriodicTask.Type.FixedDelay);
			mLocalHost.EnableDevice = Options.HasFlag(ADAPTER_OPTIONS.ENABLE_LOCAL_HOST);
		}

		public override void Dispose(bool disposing)
		{
			if (disposing)
			{
				mBackgroundTask.Dispose();
				mLocalProductManager?.Dispose();
				mLocalProductManager = null;
			}
			base.Dispose(disposing);
		}

		protected virtual LocalDevice CreateDefaultLocalHost(LocalProduct local_product, DEVICE_TYPE device_type, int device_instance, FUNCTION_NAME function_name, int function_instance, byte? capabilties)
		{
			return local_product.CreateDevice(device_type, device_instance, function_name, function_instance, capabilties, LOCAL_DEVICE_OPTIONS.NONE);
		}

		public void EnableLocalHost()
		{
			mLocalHost.EnableDevice = true;
		}

		public void DisableLocalHost()
		{
			mLocalHost.EnableDevice = false;
		}

		private void OnBridgeAdapterOpened(Comm.AdapterOpenedEvent message)
		{
			RaiseAdapterOpened();
		}

		private void OnBridgeAdapterClosed(Comm.AdapterClosedEvent message)
		{
			RaiseAdapterClosed();
		}

		private void OnBridgeAdapterRx(Comm.AdapterRxEvent<CAN.MessageBuffer> rx)
		{
			RaiseMessageRx(rx.Message, rx.Echo);
		}

		private void OnAdapterOpened(Comm.AdapterOpenedEvent message)
		{
			if (BackgroundTxMessagesPerSecond < 25)
			{
				BackgroundTxMessagesPerSecond = 400;
			}
		}

		private void OnAdapterMessageRx(Comm.AdapterRxEvent<CAN.MessageBuffer> args)
		{
			RxEvent.Publish(args.Message, args.Echo);
		}

		private void AdapterBackgroundTask()
		{
			if (base.IsDisposed)
			{
				return;
			}
			try
			{
				foreach (BackgroundTaskObject backgroundTask in BackgroundTasks)
				{
					try
					{
						backgroundTask?.BackgroundTask();
					}
					catch
					{
					}
				}
			}
			catch
			{
			}
		}
	}
}
