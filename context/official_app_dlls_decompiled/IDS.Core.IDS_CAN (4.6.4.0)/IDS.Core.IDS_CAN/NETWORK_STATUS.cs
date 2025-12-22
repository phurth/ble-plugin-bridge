namespace IDS.Core.IDS_CAN
{
	public struct NETWORK_STATUS
	{
		public byte Value;

		public bool HasActiveDTCs => (Value & 1) != 0;

		public bool HasStoredDTCs => (Value & 2) != 0;

		public bool HasDTCs => (Value & 3) != 0;

		public bool HasOpenSessions => (Value & 4) != 0;

		public IN_MOTION_LOCKOUT_LEVEL InMotionLockoutLevel => (byte)((uint)(Value >> 3) & 3u);

		public bool HasExtemdedCloudCapabilities => (Value & 0x40) != 0;

		public bool IsHazardousDevice => (Value & 0x80) != 0;

		public NETWORK_STATUS(byte value)
		{
			Value = value;
		}

		public NETWORK_STATUS(byte value, IDS_CAN_VERSION_NUMBER version)
		{
			if ((byte)version <= 16)
			{
				value = (byte)(value & 7u);
			}
			else if ((byte)version <= 17)
			{
				value = (byte)(value & 0xFu);
			}
			else if ((byte)version <= 18)
			{
				value = (byte)(value & 0x9Fu);
			}
			else if ((byte)version <= 19)
			{
				value = (byte)(value & 0xDFu);
			}
			Value = value;
		}

		public static implicit operator byte(NETWORK_STATUS s)
		{
			return s.Value;
		}

		public static implicit operator NETWORK_STATUS(byte value)
		{
			return new NETWORK_STATUS(value);
		}
	}
}
