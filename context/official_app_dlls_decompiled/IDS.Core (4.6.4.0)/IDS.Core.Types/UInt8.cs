namespace IDS.Core.Types
{
	public struct UInt8
	{
		private byte Value;

		public bool Bit0
		{
			get
			{
				return (Value & 1) != 0;
			}
			set
			{
				if (value)
				{
					Value |= 1;
				}
				else
				{
					Value &= 254;
				}
			}
		}

		public bool Bit1
		{
			get
			{
				return (Value & 2) != 0;
			}
			set
			{
				if (value)
				{
					Value |= 2;
				}
				else
				{
					Value &= 253;
				}
			}
		}

		public bool Bit2
		{
			get
			{
				return (Value & 4) != 0;
			}
			set
			{
				if (value)
				{
					Value |= 4;
				}
				else
				{
					Value &= 251;
				}
			}
		}

		public bool Bit3
		{
			get
			{
				return (Value & 8) != 0;
			}
			set
			{
				if (value)
				{
					Value |= 8;
				}
				else
				{
					Value &= 247;
				}
			}
		}

		public bool Bit4
		{
			get
			{
				return (Value & 0x10) != 0;
			}
			set
			{
				if (value)
				{
					Value |= 16;
				}
				else
				{
					Value &= 239;
				}
			}
		}

		public bool Bit5
		{
			get
			{
				return (Value & 0x20) != 0;
			}
			set
			{
				if (value)
				{
					Value |= 32;
				}
				else
				{
					Value &= 223;
				}
			}
		}

		public bool Bit6
		{
			get
			{
				return (Value & 0x40) != 0;
			}
			set
			{
				if (value)
				{
					Value |= 64;
				}
				else
				{
					Value &= 191;
				}
			}
		}

		public bool Bit7
		{
			get
			{
				return (Value & 0x80) != 0;
			}
			set
			{
				if (value)
				{
					Value |= 128;
				}
				else
				{
					Value &= 127;
				}
			}
		}

		private UInt8(byte value)
		{
			Value = value;
		}

		public static implicit operator UInt8(byte a)
		{
			return new UInt8(a);
		}

		public static implicit operator byte(UInt8 a)
		{
			return a.Value;
		}
	}
}
