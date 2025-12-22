using IDS.Core.Types;

namespace IDS.Core.IDS_CAN
{
	public class MAC : Comm.PhysicalAddress
	{
		public MAC()
			: base(6)
		{
		}

		public MAC(byte[] buffer)
			: base(6)
		{
			CopyFrom(buffer);
		}

		public MAC(Comm.IPhysicalAddress mac)
			: base(6)
		{
			CopyFrom(mac);
		}

		public MAC(MAC mac)
			: base(6)
		{
			CopyFrom(mac);
		}

		public MAC(UInt48 value)
			: base(6)
		{
			Buffer[0] = (byte)(value >> 40);
			Buffer[1] = (byte)(value >> 32);
			Buffer[2] = (byte)(value >> 24);
			Buffer[3] = (byte)(value >> 16);
			Buffer[4] = (byte)(value >> 8);
			Buffer[5] = (byte)(value >> 0);
		}

		public bool UnloadFromMessage(AdapterRxEvent rx)
		{
			if ((byte)rx.MessageType == 0 && rx.Count == 8)
			{
				for (int i = 0; i < Buffer.Length; i++)
				{
					Buffer[i] = rx[i + 2];
				}
				return true;
			}
			return false;
		}

		public static implicit operator UInt48(MAC a)
		{
			return ((((((((((UInt48)a[0] << 8) | a[1]) << 8) | a[2]) << 8) | a[3]) << 8) | a[4]) << 8) | a[5];
		}
	}
}
