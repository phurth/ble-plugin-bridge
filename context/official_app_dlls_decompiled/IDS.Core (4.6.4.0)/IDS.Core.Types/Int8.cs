namespace IDS.Core.Types
{
	public struct Int8
	{
		private sbyte Value;

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
					Value &= -2;
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
					Value &= -3;
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
					Value &= -5;
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
					Value &= -9;
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
					Value &= -17;
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
					Value &= -33;
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
					Value &= -65;
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
					Value |= sbyte.MinValue;
				}
				else
				{
					Value &= sbyte.MaxValue;
				}
			}
		}

		private Int8(sbyte value)
		{
			Value = value;
		}

		public static implicit operator Int8(sbyte a)
		{
			return new Int8(a);
		}

		public static implicit operator sbyte(Int8 a)
		{
			return a.Value;
		}
	}
}
