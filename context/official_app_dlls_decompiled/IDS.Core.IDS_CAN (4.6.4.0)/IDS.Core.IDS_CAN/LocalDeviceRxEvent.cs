using System;
using System.Collections;
using System.Collections.Generic;
using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	public class LocalDeviceRxEvent : Event, CAN.IMessage, Comm.IMessage, Comm.IByteList, IReadOnlyList<byte>, IEnumerable<byte>, IEnumerable, IReadOnlyCollection<byte>, Comm.ITimeStamp, CAN.IReadOnlyPacket
	{
		public readonly ILocalDevice LocalDevice;

		private AdapterRxEvent Rx;

		public bool Echo => Rx.Echo;

		public MESSAGE_TYPE MessageType => Rx.MessageType;

		public ADDRESS SourceAddress => Rx.SourceAddress;

		public ADDRESS TargetAddress => Rx.TargetAddress;

		public byte MessageData => Rx.MessageData;

		public CAN.PAYLOAD Payload => Rx.Payload;

		public int Length => Rx.Length;

		public CAN.ID ID => ((CAN.IReadOnlyPacket)Rx).ID;

		public TimeSpan Timestamp => Rx.Timestamp;

		public int Count => ((IReadOnlyCollection<byte>)Rx).Count;

		public byte this[int index] => Rx[index];

		public LocalDeviceRxEvent(ILocalDevice localdevice)
			: base(localdevice)
		{
			LocalDevice = localdevice;
		}

		public void Publish(AdapterRxEvent rx)
		{
			if (LocalDevice.IsOnline)
			{
				Rx = rx;
				Publish();
			}
		}

		public void CopyTo(byte[] array, int index)
		{
			Rx.CopyTo(array, index);
		}

		public void CopyRangeTo(int sourceIndex, int count, byte[] array, int destIndex)
		{
			Rx.CopyRangeTo(sourceIndex, count, array, destIndex);
		}

		public IEnumerator<byte> GetEnumerator()
		{
			return ((IEnumerable<byte>)Rx).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<byte>)Rx).GetEnumerator();
		}

		public override string ToString()
		{
			return Rx.ToString();
		}

		public string ToString(bool dataonly)
		{
			return Rx.ToString(dataonly);
		}
	}
}
