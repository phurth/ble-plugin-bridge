using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	internal class ClientSessionManager : RemoteDevice.ChildNode, IClientSessionManager, IEnumerable<SESSION_ID>, IEnumerable
	{
		private class SessionInstanceManager : Disposable
		{
			private class DeviceMgr : Disposable
			{
				private readonly Dictionary<SESSION_ID, SessionClient> Sessions = new Dictionary<SESSION_ID, SessionClient>();

				private readonly IRemoteDevice RemoteDevice;

				private readonly ILocalDevice LocalHost;

				public DeviceMgr(IRemoteDevice remotedevice, ILocalDevice localhost)
				{
					RemoteDevice = remotedevice;
					LocalHost = localhost;
				}

				public ISessionClient GetSession(SESSION_ID session_id)
				{
					if (base.IsDisposed)
					{
						return null;
					}
					if (!session_id.IsValid)
					{
						return null;
					}
					if (Sessions.TryGetValue(session_id, out var value) && value.IsDisposed)
					{
						Sessions.Remove(session_id);
						value = null;
					}
					if (value == null)
					{
						value = new SessionClient(session_id, RemoteDevice, LocalHost);
						Sessions.Add(session_id, value);
					}
					return value;
				}

				public void CloseAll()
				{
					foreach (SessionClient value in Sessions.Values)
					{
						value.Dispose();
					}
					Sessions.Clear();
				}

				public override void Dispose(bool disposing)
				{
					if (disposing)
					{
						CloseAll();
					}
				}
			}

			private Dictionary<ILocalDevice, DeviceMgr> Managers = new Dictionary<ILocalDevice, DeviceMgr>();

			private IRemoteDevice RemoteDevice;

			public SessionInstanceManager(IRemoteDevice device)
			{
				RemoteDevice = device;
			}

			public ISessionClient GetSession(ILocalDevice localhost, SESSION_ID session_id)
			{
				if (!session_id.IsValid)
				{
					return null;
				}
				if (localhost.IsDisposed)
				{
					return null;
				}
				if (!RemoteDevice.IsOnline)
				{
					return null;
				}
				if (!Managers.TryGetValue(localhost, out var value))
				{
					value = new DeviceMgr(RemoteDevice, localhost);
					Managers.Add(localhost, value);
					localhost.AddDisposable(value);
				}
				return value.GetSession(session_id);
			}

			public void CloseAll()
			{
				foreach (DeviceMgr value in Managers.Values)
				{
					value.CloseAll();
				}
			}

			public override void Dispose(bool disposing)
			{
				if (!disposing)
				{
					return;
				}
				foreach (DeviceMgr value in Managers.Values)
				{
					value.Dispose();
				}
				RemoteDevice = null;
			}
		}

		private static readonly List<SESSION_ID> EmptyList = new List<SESSION_ID>();

		private static readonly TimeSpan MESSAGE_RETRY_TIME = TimeSpan.FromMilliseconds(500.0);

		private readonly List<SESSION_ID> SupportedSessionList = new List<SESSION_ID>();

		private SessionInstanceManager SessionInstances;

		private bool ReadingSessionsFromDevice;

		private ushort SessionIndex;

		private ushort ReportedCount;

		private Timer TxTimer = new Timer();

		public int Count
		{
			get
			{
				if (!DeviceQueryComplete)
				{
					return 0;
				}
				return SupportedSessionList.Count;
			}
		}

		public bool DeviceQueryComplete { get; private set; }

		public ClientSessionManager(RemoteDevice device)
			: base(device)
		{
			SessionInstances = new SessionInstanceManager(device);
			Clear();
			ReadingSessionsFromDevice = base.Adapter.Options.HasFlag(ADAPTER_OPTIONS.AUTO_READ_DEVICE_SESSION_LIST);
			base.Adapter.Events.Subscribe<Comm.TransmitTurnEvent>(OnTransmitNextMessage, SubscriptionType.Weak, Subscriptions);
		}

		public override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing)
			{
				Clear();
				ReadingSessionsFromDevice = false;
				SessionInstances.CloseAll();
				SessionInstances.Dispose();
			}
		}

		public ISessionClient GetSession(ILocalDevice localhost, SESSION_ID session_id)
		{
			return SessionInstances.GetSession(localhost, session_id);
		}

		public IEnumerator<SESSION_ID> GetEnumerator()
		{
			return DeviceQueryComplete ? SupportedSessionList.GetEnumerator() : EmptyList.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void QueryDevice()
		{
			if (!DeviceQueryComplete)
			{
				ReadingSessionsFromDevice = true;
			}
		}

		public bool Contains(SESSION_ID id)
		{
			return SupportedSessionList.Contains(id);
		}

		private void Clear()
		{
			TxTimer.ElapsedTime = TimeSpan.FromMilliseconds(-250.0);
			SessionIndex = 0;
			ClearList();
			base.Text = "Supported sessions: UNKNOWN";
			base.Icon = IDS.Core.IDS_CAN.Adapter.ICON.QUESTION;
		}

		private void ClearList()
		{
			DeviceQueryComplete = false;
			SupportedSessionList.Clear();
		}

		public override void BackgroundTask()
		{
		}

		public override void OnDeviceTx(AdapterRxEvent tx)
		{
			if (!base.Device.IsOnline || (byte)tx.MessageType != 129 || tx.TargetAddress != base.Adapter.LocalHost.Address || tx.MessageData != 64 || !ReadingSessionsFromDevice)
			{
				return;
			}
			if (tx.Count == 1)
			{
				ClearList();
				DeviceQueryComplete = true;
				ReadingSessionsFromDevice = false;
				ReportedCount = 0;
				RESPONSE rESPONSE = (RESPONSE)tx[0];
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(20, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Supported sessions: ");
				defaultInterpolatedStringHandler.AppendFormatted(rESPONSE);
				base.Text = defaultInterpolatedStringHandler.ToStringAndClear();
				base.Icon = IDS.Core.IDS_CAN.Adapter.ICON.EXCLAMATION;
			}
			else
			{
				if (tx.Count != 8 || tx.GetUINT16(0) != SessionIndex)
				{
					return;
				}
				int num = 2;
				int i = SessionIndex * 3;
				if (SessionIndex == 0)
				{
					ReportedCount = tx.GetUINT16(num);
					ClearList();
					num += 2;
				}
				else
				{
					i--;
				}
				for (; i < ReportedCount; i++)
				{
					if (num >= 8)
					{
						break;
					}
					SESSION_ID sESSION_ID = tx.GetUINT16(num);
					if (sESSION_ID != SESSION_ID.UNKNOWN)
					{
						if (SupportedSessionList.Contains(sESSION_ID))
						{
							Clear();
							return;
						}
						SupportedSessionList.Add(sESSION_ID);
					}
					num += 2;
				}
				if (i >= ReportedCount)
				{
					SupportedSessionList.Sort((SESSION_ID first, SESSION_ID second) => first.Value.CompareTo(second.Value));
					DeviceQueryComplete = true;
					ReadingSessionsFromDevice = false;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(20, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Supported sessions: ");
					defaultInterpolatedStringHandler.AppendFormatted(Count);
					base.Text = defaultInterpolatedStringHandler.ToStringAndClear();
					base.Icon = IDS.Core.IDS_CAN.Adapter.ICON.INFO;
				}
				SessionIndex++;
				TxTimer.ElapsedTime = TimeSpan.FromSeconds(1.0);
			}
		}

		private void OnTransmitNextMessage(Comm.TransmitTurnEvent message)
		{
			if (ReadingSessionsFromDevice && base.Adapter.LocalHost.IsOnline && base.Device.IsOnline && !(TxTimer.ElapsedTime < MESSAGE_RETRY_TIME))
			{
				message.Handled = base.Adapter.LocalHost.Transmit29((byte)128, 64, base.Device, CAN.PAYLOAD.FromArgs(SessionIndex));
				if (message.Handled)
				{
					TxTimer.Reset();
				}
			}
		}
	}
}
