namespace IDS.Core.IDS_CAN
{
	public static class IBlockExtensions
	{
		public static bool IsReadable(this IBlock block)
		{
			return block.ReadSessionID != null;
		}

		public static bool IsWritable(this IBlock block)
		{
			return block.WriteSessionID != null;
		}
	}
}
