using System;
using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	internal class SessionClient : Disposable, ISessionClient, IEventSender, ISession, IDisposable, System.IDisposable
	{
		private class BusEndpoint : IBusEndpoint
		{
			public IAdapter Adapter { get; private set; }

			public ADDRESS Address { get; set; }

			public bool IsOnline
			{
				get
				{
					if (Adapter.IsConnected)
					{
						return Address.IsValidDeviceAddress;
					}
					return false;
				}
			}

			public BusEndpoint(IAdapter adapter, ADDRESS address)
			{
				Adapter = adapter;
				Address = address;
			}
		}

		private static readonly TimeSpan MESSAGE_RETRY_TIME = TimeSpan.FromMilliseconds(500.0);

		private readonly IRemoteDevice RemoteHost;

		private readonly ILocalDevice LocalHost;

		private readonly BusEndpoint mHost;

		private readonly BusEndpoint mClient;

		private readonly Timer mOpenTime = new Timer();

		private readonly Timer LastTxTime = new Timer();

		private bool mIsOpen;

		private RESPONSE CloseReason;

		private readonly ClientSessionOpenEvent SessionOpenEvent;

		private readonly ClientSessionClosedEvent SessionClosedEvent;

		private SubscriptionManager Subscriptions = new SubscriptionManager();

		public IEventPublisher Events { get; private set; }

		public SESSION_ID SessionID { get; private set; }

		public IBusEndpoint Host => mHost;

		public IBusEndpoint Client => mClient;

		public TimeSpan OpenTime
		{
			get
			{
				if (!IsOpen)
				{
					return TimeSpan.Zero;
				}
				return mOpenTime.ElapsedTime;
			}
		}

		public bool IsValid
		{
			get
			{
				if (base.IsDisposed)
				{
					return false;
				}
				if (!RemoteHost.IsOnline || RemoteHost.Address != Host.Address)
				{
					Dispose();
					return false;
				}
				if (Client.Address != LocalHost.Address || !LocalHost.IsOnline)
				{
					TerminateSession(RESPONSE.CONDITIONS_NOT_CORRECT);
					mClient.Address = LocalHost.Address;
				}
				return true;
			}
		}

		public bool IsOpen
		{
			get
			{
				if (!IsValid)
				{
					return false;
				}
				return mIsOpen;
			}
			set
			{
				if (base.IsDisposed)
				{
					value = false;
				}
				if (mIsOpen != value)
				{
					mOpenTime.Reset();
					mIsOpen = value;
					if (value)
					{
						SessionOpenEvent.Publish();
						LocalHost.Adapter.Events.Publish(SessionOpenEvent);
					}
					else
					{
						SessionClosedEvent.Publish();
						LocalHost.Adapter.Events.Publish(SessionClosedEvent);
						LastTxTime.ElapsedTime = TimeSpan.FromSeconds(1.0);
					}
				}
			}
		}

		public SessionClient(SESSION_ID session_id, IRemoteDevice host, ILocalDevice client)
		{
			SessionID = session_id;
			RemoteHost = host;
			LocalHost = client;
			mHost = new BusEndpoint(host.Adapter, host.Address);
			mClient = new BusEndpoint(client.Adapter, ADDRESS.INVALID);
			LastTxTime.ElapsedTime = TimeSpan.FromSeconds(1.0);
			Events = new EventPublisher("IDS.Core.IDS_CAN.Devices.ClientSession.Events");
			SessionOpenEvent = new ClientSessionOpenEvent(this);
			SessionClosedEvent = new ClientSessionClosedEvent(this);
			LocalHost.Adapter.Events.Subscribe<AdapterRxEvent>(OnAdapterRx, SubscriptionType.Weak, Subscriptions);
		}

		public override void Dispose(bool disposing)
		{
			if (disposing)
			{
				TerminateSession(RESPONSE.CONDITIONS_NOT_CORRECT);
				mHost.Address = ADDRESS.INVALID;
				mClient.Address = ADDRESS.INVALID;
				Events.Dispose();
				Subscriptions.Dispose();
			}
		}

		private void TerminateSession(RESPONSE reason)
		{
			if (mIsOpen)
			{
				CloseReason = reason;
				IsOpen = false;
			}
		}

		public bool TryOpenSession()
		{
			if (!IsValid)
			{
				return false;
			}
			if (!LocalHost.IsOnline)
			{
				TerminateSession(RESPONSE.CONDITIONS_NOT_CORRECT);
				return false;
			}
			if (LastTxTime.ElapsedTime < MESSAGE_RETRY_TIME)
			{
				return mIsOpen;
			}
			if (!mIsOpen)
			{
				if (LocalHost.Transmit29((byte)128, 66, Host, CAN.PAYLOAD.FromArgs(SessionID.Value)))
				{
					LastTxTime.Reset();
				}
			}
			else if (LocalHost.Transmit29((byte)128, 68, Host, CAN.PAYLOAD.FromArgs(SessionID.Value)))
			{
				LastTxTime.Reset();
			}
			return mIsOpen;
		}

		public bool CloseSession()
		{
			if (IsOpen)
			{
				return LocalHost.Transmit29((byte)128, 69, Host, CAN.PAYLOAD.FromArgs(SessionID.Value));
			}
			return false;
		}

		private void OnAdapterRx(AdapterRxEvent rx)
		{
			if (!IsValid || rx.SourceAddress != Host.Address || rx.TargetAddress != Client.Address || (byte)rx.MessageType != 129)
			{
				return;
			}
			switch (rx.MessageData)
			{
			case 66:
				if (rx.Count == 6 && (ushort)SessionID == rx.GetUINT16(0))
				{
					uint uINT = rx.GetUINT32(2);
					uint num = SessionID.Encrypt(uINT);
					LocalHost.Transmit29((byte)128, 67, Host, CAN.PAYLOAD.FromArgs(SessionID.Value, num));
				}
				break;
			case 67:
				if (rx.Count == 2 && rx.GetUINT16(0) == (ushort)SessionID)
				{
					IsOpen = true;
				}
				break;
			case 68:
				if (rx.Count == 3 && rx.GetUINT16(0) == (ushort)SessionID)
				{
					TerminateSession((RESPONSE)rx[2]);
				}
				break;
			case 69:
				if (rx.Count == 3 && rx.GetUINT16(0) == (ushort)SessionID)
				{
					TerminateSession((RESPONSE)rx[2]);
				}
				break;
			}
		}
	}
}
