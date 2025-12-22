using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using IDS.Core.Events;
using IDS.Core.IDS_CAN.Devices;

namespace IDS.Core.IDS_CAN
{
	internal class RemoteDevice : Adapter.Object, IRemoteDevice, IDevice, IBusEndpoint, IUniqueDeviceInfo, IUniqueProductInfo, IEventSender
	{
		public abstract class Child : Disposable
		{
			protected readonly SubscriptionManager Subscriptions;

			protected readonly RemoteDevice mDevice;

			public IAdapter Adapter { get; private set; }

			public IRemoteDevice Device => mDevice;

			public Child(RemoteDevice device)
			{
				mDevice = device;
				Subscriptions = device.Subscriptions;
				Adapter = Device.Adapter;
				device.Children.Add(this);
			}

			public abstract void BackgroundTask();

			public abstract void OnDeviceTx(AdapterRxEvent tx);
		}

		public abstract class ChildNode : Child
		{
			protected readonly ITreeNode TreeNode;

			public string Text
			{
				get
				{
					return TreeNode.Text;
				}
				set
				{
					TreeNode.Text = value;
				}
			}

			public Enum Icon
			{
				get
				{
					return TreeNode.Icon;
				}
				set
				{
					TreeNode.Icon = value;
				}
			}

			public ChildNode(RemoteDevice device)
				: base(device)
			{
				TreeNode = IDS.Core.TreeNode.Create(this);
				base.Device.TreeNode.AddChild(TreeNode);
			}

			public override void Dispose(bool disposing)
			{
				if (disposing)
				{
					TreeNode.Dispose();
				}
			}
		}

		private class MacNode : ChildNode
		{
			public MacNode(RemoteDevice device)
				: base(device)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(5, 1);
				defaultInterpolatedStringHandler.AppendLiteral("MAC: ");
				defaultInterpolatedStringHandler.AppendFormatted(device.MAC);
				base.Text = defaultInterpolatedStringHandler.ToStringAndClear();
				base.Icon = IDS.Core.IDS_CAN.Adapter.ICON.INFO;
			}

			public override void BackgroundTask()
			{
			}

			public override void OnDeviceTx(AdapterRxEvent rx)
			{
			}
		}

		private class ProtocolNode : ChildNode
		{
			public ProtocolNode(RemoteDevice device)
				: base(device)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(9, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Protocol ");
				defaultInterpolatedStringHandler.AppendFormatted(device.ProtocolVersion);
				base.Text = defaultInterpolatedStringHandler.ToStringAndClear();
				base.Icon = IDS.Core.IDS_CAN.Adapter.ICON.INFO;
			}

			public override void BackgroundTask()
			{
			}

			public override void OnDeviceTx(AdapterRxEvent rx)
			{
			}
		}

		private class DeviceIdNode : ChildNode
		{
			public DeviceIdNode(RemoteDevice device)
				: base(device)
			{
				DeviceIDChanged(device.GetDeviceID());
			}

			public void DeviceIDChanged(DEVICE_ID id)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(11, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Device ID: ");
				defaultInterpolatedStringHandler.AppendFormatted(id);
				base.Text = defaultInterpolatedStringHandler.ToStringAndClear();
				base.Icon = (id.IsValid ? IDS.Core.IDS_CAN.Adapter.ICON.INFO : IDS.Core.IDS_CAN.Adapter.ICON.EXCLAMATION);
				base.Device.TreeNode.Text = base.Device.ToShortString(show_address: true);
				base.Device.TreeNode.Icon = id.Icon;
			}

			public override void BackgroundTask()
			{
			}

			public override void OnDeviceTx(AdapterRxEvent rx)
			{
			}
		}

		private class CircuitIdNode : ChildNode
		{
			public CircuitIdNode(RemoteDevice device)
				: base(device)
			{
				base.Icon = IDS.Core.IDS_CAN.Adapter.ICON.INFO;
				CircuitIDChanged();
			}

			public void CircuitIDChanged()
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(12, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Circuit ID: ");
				defaultInterpolatedStringHandler.AppendFormatted(base.Device.CircuitID);
				base.Text = defaultInterpolatedStringHandler.ToStringAndClear();
			}

			public override void BackgroundTask()
			{
			}

			public override void OnDeviceTx(AdapterRxEvent rx)
			{
			}
		}

		private class PartNumberNode : ChildNode
		{
			private static readonly StringBuilder StringBuilder = new StringBuilder(50);

			private readonly Timer LastTxTime = new Timer();

			private string mPartNumber;

			private bool ReadPartNumber;

			public string PartNumber
			{
				get
				{
					return mPartNumber;
				}
				private set
				{
					if (!(mPartNumber != value))
					{
						return;
					}
					mPartNumber = value;
					if (value == null || value.Length == 0 || value == "")
					{
						base.Text = "Part Number: UNKNOWN";
						base.Icon = IDS.Core.IDS_CAN.Adapter.ICON.EXCLAMATION;
						return;
					}
					base.Text = "Part Number: " + value;
					if (char.IsDigit(mPartNumber[0]) && char.IsDigit(mPartNumber[1]) && char.IsDigit(mPartNumber[2]) && char.IsDigit(mPartNumber[3]) && char.IsDigit(mPartNumber[4]))
					{
						base.Icon = IDS.Core.IDS_CAN.Adapter.ICON.INFO;
					}
					else
					{
						base.Icon = IDS.Core.IDS_CAN.Adapter.ICON.QUESTION;
					}
				}
			}

			public PartNumberNode(RemoteDevice device)
				: base(device)
			{
				base.Text = "Part Number: <not queried>";
				ReadPartNumber = base.Adapter.Options.HasFlag(ADAPTER_OPTIONS.AUTO_READ_DEVICE_PART_NUMBER);
				base.Adapter.Events.Subscribe<Comm.TransmitTurnEvent>(OnTransmitNextMessage, SubscriptionType.Weak, Subscriptions);
			}

			public void QueryDevice()
			{
				ReadPartNumber = true;
			}

			public override void BackgroundTask()
			{
			}

			public override void OnDeviceTx(AdapterRxEvent rx)
			{
				if ((byte)rx.MessageType != 129 || rx.MessageData != 0 || rx.Count != 8)
				{
					return;
				}
				lock (StringBuilder)
				{
					StringBuilder.Length = 0;
					for (int i = 0; i < 8; i++)
					{
						char c = (char)rx[i];
						if (c == '\0')
						{
							break;
						}
						StringBuilder.Append(c);
					}
					PartNumber = StringBuilder.ToString();
				}
			}

			private void OnTransmitNextMessage(Comm.TransmitTurnEvent message)
			{
				if (base.Device.IsOnline && mPartNumber == null && ReadPartNumber && base.Adapter.LocalHost.IsOnline && !(LastTxTime.ElapsedTime < PART_NUMBER_TIMEOUT))
				{
					message.Handled = base.Adapter.LocalHost.Transmit29((byte)128, 0, base.Device);
					if (message.Handled)
					{
						LastTxTime.Reset();
					}
				}
			}
		}

		private class MessageNode : ChildNode
		{
			public readonly MESSAGE_TYPE MessageType;

			public readonly Timer Age = new Timer();

			public readonly string BaseText;

			private CAN.PAYLOAD? mPayload;

			public CAN.PAYLOAD? Payload
			{
				get
				{
					return mPayload;
				}
				protected set
				{
					if (mPayload != value)
					{
						mPayload = value;
						base.Text = BaseText + FormatMessage();
					}
				}
			}

			public MessageNode(RemoteDevice device, MESSAGE_TYPE message_type, string base_text)
				: base(device)
			{
				MessageType = message_type;
				BaseText = base_text + ": ";
				Reset();
				base.Text = BaseText + FormatMessage();
			}

			protected void Reset()
			{
				Payload = null;
				base.Icon = IDS.Core.IDS_CAN.Adapter.ICON.EXCLAMATION;
			}

			public override void BackgroundTask()
			{
				if (Payload.HasValue)
				{
					double totalSeconds = Age.ElapsedTime.TotalSeconds;
					if (totalSeconds > 2.5)
					{
						Reset();
					}
					else if ((Adapter.ICON)(object)base.Icon == IDS.Core.IDS_CAN.Adapter.ICON.LED_GREEN && totalSeconds > 1.5)
					{
						base.Icon = IDS.Core.IDS_CAN.Adapter.ICON.LED_YELLOW;
					}
				}
			}

			protected virtual string FormatMessage()
			{
				return Payload?.ToString(dataonly: true) ?? "???";
			}

			protected virtual void UpdateStatus(AdapterRxEvent rx)
			{
				Age.Reset();
				base.Icon = IDS.Core.IDS_CAN.Adapter.ICON.LED_GREEN;
				Payload = rx.Payload;
			}

			public override void OnDeviceTx(AdapterRxEvent rx)
			{
				if (rx.MessageType == MessageType)
				{
					UpdateStatus(rx);
				}
			}
		}

		private class NetworkStatusNode : MessageNode
		{
			private Timer LastRxTime = new Timer();

			private DeviceInMotionLockoutLevelChangedEvent LockoutEvent;

			public NETWORK_STATUS NetworkStatus { get; private set; }

			public NetworkStatusNode(RemoteDevice device)
				: base(device, (byte)0, "Network status")
			{
			}

			protected override string FormatMessage()
			{
				CAN.PAYLOAD? payload = base.Payload;
				if (payload.HasValue && payload.GetValueOrDefault().Length >= 1)
				{
					return base.Payload?[0].HexString();
				}
				return "???";
			}

			protected override void UpdateStatus(AdapterRxEvent rx)
			{
				base.UpdateStatus(rx);
				if ((byte)rx.MessageType != 0 || rx.Count != 8)
				{
					return;
				}
				LastRxTime.Reset();
				NETWORK_STATUS prev = NetworkStatus;
				NetworkStatus = new NETWORK_STATUS(rx[0], rx[1]);
				if (prev.InMotionLockoutLevel != NetworkStatus.InMotionLockoutLevel)
				{
					if (LockoutEvent == null)
					{
						LockoutEvent = new DeviceInMotionLockoutLevelChangedEvent(base.Device);
					}
					Task.Run(delegate
					{
						LockoutEvent.Publish(NetworkStatus.InMotionLockoutLevel, prev.InMotionLockoutLevel);
					});
				}
			}
		}

		private class StatusDetailNode : ChildNode
		{
			public StatusDetailNode(RemoteDevice device, ITreeNode parentNode, string label)
				: base(device)
			{
				base.Text = label;
				base.Icon = IDS.Core.IDS_CAN.Adapter.ICON.INFO;
				parentNode.AddChild(TreeNode);
			}

			public override void BackgroundTask()
			{
			}

			public override void OnDeviceTx(AdapterRxEvent rx)
			{
			}
		}

		private class EnhDeviceStatusNode : MessageNode
		{
			private Timer LastRxTime = new Timer();

			private IDeviceStatusParams devType;

			private IEnumerable<MemberInfo> members;

			private readonly List<StatusDetailNode> _subNodes = new List<StatusDetailNode>();

			private object objRef;

			public EnhDeviceStatusNode(RemoteDevice device)
				: base(device, (byte)3, "Device status")
			{
				switch ((byte)base.Device.GetDeviceID().DeviceType)
				{
				case 47:
					devType = new AWNING_SENSOR_STATUS_PARAMS();
					break;
				case 25:
					devType = new TEMPERATURE_SENSOR_STATUS_PARAMS();
					break;
				case 30:
				case 31:
				case 32:
				case 33:
					devType = new RELAY_TYPE_2_STATUS_PARAMS();
					break;
				case 10:
					devType = new TANK_SENSOR_STATUS_PARAMS();
					break;
				case 20:
					devType = new DIMMABLE_LIGHT_STATUS_PARAMS();
					break;
				}
				if (devType == null)
				{
					return;
				}
				List<MemberInfo> list = (List<MemberInfo>)(members = Enumerable.ToList(Enumerable.Where(devType.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public), (MemberInfo m) => ((m.MemberType == MemberTypes.Field && ((FieldInfo)m).FieldType == typeof(string)) || (m.MemberType == MemberTypes.Property && ((PropertyInfo)m).PropertyType == typeof(string))) && Attribute.IsDefined(m, typeof(DeviceDisplayAttribute)))));
				foreach (MemberInfo member in members)
				{
					DeviceDisplayAttribute deviceDisplayAttribute = (DeviceDisplayAttribute)Attribute.GetCustomAttribute(member, typeof(DeviceDisplayAttribute));
					string text = ((deviceDisplayAttribute != null) ? deviceDisplayAttribute.DisplayName : member.Name);
					object obj = ((member is PropertyInfo propertyInfo) ? propertyInfo.GetValue(devType) : ((FieldInfo)member).GetValue(devType));
					ITreeNode treeNode = TreeNode;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 2);
					defaultInterpolatedStringHandler.AppendFormatted(text);
					defaultInterpolatedStringHandler.AppendLiteral(": ");
					defaultInterpolatedStringHandler.AppendFormatted<object>(obj);
					StatusDetailNode item = new StatusDetailNode(device, treeNode, defaultInterpolatedStringHandler.ToStringAndClear());
					_subNodes.Add(item);
				}
			}

			protected override void UpdateStatus(AdapterRxEvent rx)
			{
				base.UpdateStatus(rx);
				if ((byte)rx.MessageType != 3)
				{
					return;
				}
				LastRxTime.Reset();
				if (members == null || devType == null)
				{
					return;
				}
				devType.SetPayload(rx.Payload);
				foreach (MemberInfo member in members)
				{
					object obj = ((member is PropertyInfo propertyInfo) ? propertyInfo.GetValue(devType) : ((FieldInfo)member).GetValue(devType));
					DeviceDisplayAttribute deviceDisplayAttribute = (DeviceDisplayAttribute)Attribute.GetCustomAttribute(member, typeof(DeviceDisplayAttribute));
					string label = ((deviceDisplayAttribute != null) ? deviceDisplayAttribute.DisplayName : member.Name);
					StatusDetailNode statusDetailNode = Enumerable.FirstOrDefault(_subNodes, (StatusDetailNode node) => node.Text.StartsWith(label + ":"));
					if (statusDetailNode != null)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 2);
						defaultInterpolatedStringHandler.AppendFormatted(label);
						defaultInterpolatedStringHandler.AppendLiteral(": ");
						defaultInterpolatedStringHandler.AppendFormatted<object>(obj);
						statusDetailNode.Text = defaultInterpolatedStringHandler.ToStringAndClear();
					}
				}
			}
		}

		private static readonly TimeSpan PART_NUMBER_TIMEOUT = TimeSpan.FromSeconds(1.0);

		private static readonly TimeSpan DEVICE_STATUS_TIMEOUT = TimeSpan.FromSeconds(5.0);

		private readonly List<Child> Children = new List<Child>();

		private readonly MacNode mMacNode;

		private readonly ProtocolNode mProtocolNode;

		private readonly CircuitIdNode mCircuitIdNode;

		private readonly DeviceIdNode mDeviceIdNode;

		private readonly PartNumberNode mPartNumberNode;

		private readonly NetworkStatusNode mNetworkStatusNode;

		private readonly RemoteProduct mProduct;

		private readonly EnhDeviceStatusNode mEnhStatusNode;

		private DeviceIDChangedEvent DeviceIDChangedEvent;

		private CIRCUIT_ID mCircuitID;

		private CAN.PAYLOAD mDeviceStatus;

		private Timer DeviceStatusAge = new Timer();

		private CircuitIDChangedEvent CircuitIDChangedEvent;

		public ADDRESS Address { get; private set; }

		public MAC MAC { get; private set; }

		public IDS_CAN_VERSION_NUMBER ProtocolVersion { get; private set; }

		public PRODUCT_ID ProductID { get; private set; }

		public byte ProductInstance { get; private set; }

		public DEVICE_TYPE DeviceType { get; private set; }

		public int DeviceInstance { get; private set; }

		public FUNCTION_NAME FunctionName { get; private set; }

		public int FunctionInstance { get; private set; }

		public byte? DeviceCapabilities { get; private set; }

		public ITreeNode TreeNode { get; private set; }

		public IPIDManager PIDs { get; private set; }

		public IBLOCKManager BLOCKs { get; private set; }

		public IClientSessionManager Sessions { get; private set; }

		public ITextConsole TextConsole { get; private set; }

		public NETWORK_STATUS NetworkStatus => mNetworkStatusNode.NetworkStatus;

		public string SoftwarePartNumber => mPartNumberNode.PartNumber;

		public bool IsOnline { get; private set; }

		public IRemoteProduct Product => mProduct;

		IProduct IDevice.Product => mProduct;

		public IEventPublisher Events { get; private set; }

		public CIRCUIT_ID CircuitID
		{
			get
			{
				return mCircuitID;
			}
			set
			{
				if ((uint)mCircuitID != (uint)value)
				{
					CIRCUIT_ID prev = mCircuitID;
					mCircuitID = value;
					mCircuitIdNode?.CircuitIDChanged();
					if (CircuitIDChangedEvent == null)
					{
						CircuitIDChangedEvent = new CircuitIDChangedEvent(this);
					}
					Task.Run(delegate
					{
						CircuitIDChangedEvent.Publish(prev);
					});
				}
			}
		}

		public CAN.PAYLOAD DeviceStatus
		{
			get
			{
				return mDeviceStatus;
			}
			set
			{
				DeviceStatusAge.Reset();
				if (mDeviceStatus != value)
				{
					mDeviceStatus = value;
				}
			}
		}

		private DEVICE_ID DeviceID
		{
			set
			{
				if (value.ProductID != ProductID || value.DeviceType != DeviceType || value.DeviceInstance != DeviceInstance || value.ProductInstance == 0)
				{
					return;
				}
				mProduct.UpdateInstance(value.ProductInstance);
				if (value.ProductInstance != ProductInstance || value.FunctionName != FunctionName || value.FunctionInstance != FunctionInstance || value.DeviceCapabilities != DeviceCapabilities)
				{
					ProductInstance = value.ProductInstance;
					FunctionName = value.FunctionName;
					FunctionInstance = value.FunctionInstance;
					DeviceCapabilities = value.DeviceCapabilities;
					mDeviceIdNode?.DeviceIDChanged(value);
					if (DeviceIDChangedEvent == null)
					{
						DeviceIDChangedEvent = new DeviceIDChangedEvent(this);
					}
					Task.Run(delegate
					{
						DeviceIDChangedEvent.Publish();
					});
				}
			}
		}

		public bool IsCircuitIDWriteable => GetPID(PID.IDS_CAN_CIRCUIT_ID)?.IsWritable ?? false;

		public bool IsPIDSupported(PID id)
		{
			return PIDs.Contains(id);
		}

		public IDevicePID GetPID(PID id)
		{
			return PIDs.GetPID(id);
		}

		public bool IsBLOCKSupported(BLOCK_ID id)
		{
			return BLOCKs.Contains(id);
		}

		public IDeviceBLOCK GetBLOCK(BLOCK_ID id)
		{
			return BLOCKs.GetBLOCK(id);
		}

		public bool IsSessionSupported(SESSION_ID id)
		{
			return Sessions.Contains(id);
		}

		public void QueryPartNumber()
		{
			mPartNumberNode.QueryDevice();
		}

		internal RemoteDevice(Adapter adapter, ADDRESS address, MAC mac, IDS_CAN_VERSION_NUMBER protocol, DEVICE_ID device_id, CIRCUIT_ID circuit_id, CAN.PAYLOAD device_status)
			: base(adapter)
		{
			Address = address;
			MAC = new MAC(mac);
			ProtocolVersion = protocol;
			ProductID = device_id.ProductID;
			ProductInstance = device_id.ProductInstance;
			DeviceType = device_id.DeviceType;
			DeviceInstance = device_id.DeviceInstance;
			FunctionName = device_id.FunctionName;
			FunctionInstance = device_id.FunctionInstance;
			DeviceCapabilities = device_id.DeviceCapabilities;
			mCircuitID = circuit_id;
			mDeviceStatus = device_status;
			DeviceStatusAge.Reset();
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(21, 1);
			defaultInterpolatedStringHandler.AppendLiteral("RemoteDevice[");
			defaultInterpolatedStringHandler.AppendFormatted(address);
			defaultInterpolatedStringHandler.AppendLiteral("].Events");
			Events = new EventPublisher(defaultInterpolatedStringHandler.ToStringAndClear());
			AddDisposable(Events);
			TreeNode = IDS.Core.TreeNode.Create(this);
			AddDisposable(TreeNode);
			mMacNode = new MacNode(this);
			mProtocolNode = new ProtocolNode(this);
			mCircuitIdNode = new CircuitIdNode(this);
			mDeviceIdNode = new DeviceIdNode(this);
			mPartNumberNode = new PartNumberNode(this);
			mNetworkStatusNode = new NetworkStatusNode(this);
			Sessions = new ClientSessionManager(this);
			PIDs = new PIDManager(this);
			BLOCKs = new BLOCKManager(this);
			TextConsole = new RemoteTextConsole(this);
			mEnhStatusNode = new EnhDeviceStatusNode(this);
			mProduct = (base.Adapter.Products as ProductManager)?.LocateOrCreateProductForDevice(this);
			IsOnline = true;
			TreeNode.Text = this.ToShortString(show_address: true);
			base.Adapter.TreeNode.AddChild(TreeNode);
			mProduct.AddOrUpdateDevice(this);
			this.GetDeviceID();
		}

		public override void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}
			GoOffline();
			foreach (Child child in Children)
			{
				child.Dispose();
			}
			Children.Clear();
		}

		public void GoOffline()
		{
			if (!IsOnline)
			{
				return;
			}
			TreeNode.RemoveFromParent();
			this.GetDeviceID();
			IsOnline = false;
			base.Subscriptions?.CancelAllSubscriptions();
			mProduct.RemoveOfflineDevice(this);
			if (!base.IsDisposed)
			{
				Task.Run(delegate
				{
					RemoteDeviceOfflineEvent remoteDeviceOfflineEvent = new RemoteDeviceOfflineEvent(this, this);
					remoteDeviceOfflineEvent.Publish(Address);
					base.Adapter.Events.Publish(remoteDeviceOfflineEvent);
				});
				Task.Delay(60000).ContinueWith(delegate
				{
					Dispose();
				});
			}
		}

		public void OnAdapterMessageRx(AdapterRxEvent rx)
		{
			if (!IsOnline || base.IsDisposed || !base.Adapter.IsConnected)
			{
				return;
			}
			switch ((byte)rx.MessageType)
			{
			case 1:
				if (rx.Count >= 4)
				{
					CircuitID = rx.GetUINT32(0);
				}
				break;
			case 2:
				if (rx.Count == 8)
				{
					DeviceID = new DEVICE_ID(rx.GetUINT16(0), rx[2], rx[3], rx[6] >> 4, rx.GetUINT16(4), rx[6] & 0xF, rx[7]);
				}
				else if (rx.Count == 7)
				{
					DeviceID = new DEVICE_ID(rx.GetUINT16(0), rx[2], rx[3], rx[6] >> 4, rx.GetUINT16(4), rx[6] & 0xF, null);
				}
				break;
			case 3:
				DeviceStatus = rx.Payload;
				break;
			}
			foreach (Child child in Children)
			{
				child.OnDeviceTx(rx);
			}
		}

		public override string ToString()
		{
			return this.GetDeviceID().ToString();
		}

		public void BackgroundTask()
		{
			if (base.IsDisposed || !base.Adapter.IsConnected)
			{
				return;
			}
			foreach (Child child in Children)
			{
				child.BackgroundTask();
			}
			if (DeviceStatus.Length > 0 && DeviceStatusAge.ElapsedTime >= DEVICE_STATUS_TIMEOUT)
			{
				mDeviceStatus.Length = 0;
			}
		}
	}
}
