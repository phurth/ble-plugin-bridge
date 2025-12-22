namespace IDS.Core
{
	public static class ByteExtensions
	{
		private static readonly string[] ByteString;

		static ByteExtensions()
		{
			ByteString = new string[256];
			for (int i = 0; i < ByteString.Length; i++)
			{
				ByteString[i] = i.ToString("X2");
			}
		}

		public static string HexString(this byte b)
		{
			return ByteString[b];
		}
	}
}
