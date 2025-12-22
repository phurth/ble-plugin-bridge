namespace IDS.Core.IDS_CAN
{
	public struct TEXT_CONSOLE_SIZE
	{
		public int Width;

		public int Height;

		public TEXT_CONSOLE_SIZE(int w, int h)
		{
			Width = w;
			Height = h;
		}

		public bool Equals(TEXT_CONSOLE_SIZE s)
		{
			if (Width == s.Width)
			{
				return Height == s.Height;
			}
			return false;
		}

		public static bool operator ==(TEXT_CONSOLE_SIZE s1, TEXT_CONSOLE_SIZE s2)
		{
			return s1.Equals(s2);
		}

		public static bool operator !=(TEXT_CONSOLE_SIZE s1, TEXT_CONSOLE_SIZE s2)
		{
			return !s1.Equals(s2);
		}

		public override bool Equals(object obj)
		{
			if (obj is TEXT_CONSOLE_SIZE)
			{
				return Equals((TEXT_CONSOLE_SIZE)obj);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (Width << 16) + Height;
		}

		public override string ToString()
		{
			return Width + " x " + Height;
		}
	}
}
