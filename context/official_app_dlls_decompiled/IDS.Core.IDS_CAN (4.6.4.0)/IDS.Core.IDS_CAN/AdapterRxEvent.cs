using System;
using System.Collections;
using System.Collections.Generic;
using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	public class AdapterRxEvent : Event, CAN.IMessage, Comm.IMessage, Comm.IByteList, IReadOnlyList<byte>, IEnumerable<byte>, IEnumerable, IReadOnlyCollection<byte>, Comm.ITimeStamp, CAN.IReadOnlyPacket
	{
		public readonly IAdapter Adapter;

		private CAN.IMessage Message;

		private CAN_ID IdsCanId;

		public bool Echo { get; private set; }

		public MESSAGE_TYPE MessageType => IdsCanId.MessageType;

		public ADDRESS SourceAddress => IdsCanId.SourceAddress;

		public ADDRESS TargetAddress => IdsCanId.TargetAddress;

		public byte MessageData => IdsCanId.MessageData;

		public CAN.PAYLOAD Payload { get; private set; }

		public int Length => Message.Length;

		public int Count => Message.Count;

		public CAN.ID ID => Message.ID;

		public TimeSpan Timestamp => Message.Timestamp;

		public byte this[int index] => Message[index];

		public IDevice SourceDevice => Adapter.Devices.GetDeviceByAddress(SourceAddress);

		public IDevice TargetDevice => Adapter.Devices.GetDeviceByAddress(TargetAddress);

		public AdapterRxEvent(IAdapter a)
			: base(a)
		{
			Adapter = a;
		}

		public void Publish(CAN.IMessage msg, bool echo)
		{
			Message = msg;
			Echo = echo;
			IdsCanId = msg.ID;
			Payload = new CAN.PAYLOAD(msg);
			Publish();
		}

		public void CopyTo(byte[] array, int index)
		{
			Message.CopyTo(array, index);
		}

		public void CopyRangeTo(int sourceIndex, int count, byte[] array, int destIndex)
		{
			Message.CopyRangeTo(sourceIndex, count, array, destIndex);
		}

		public IEnumerator<byte> GetEnumerator()
		{
			return Message.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Message.GetEnumerator();
		}

		public override string ToString()
		{
			return Message.ToString();
		}

		public string ToString(bool dataonly)
		{
			return Message.ToString(dataonly);
		}
	}
}
