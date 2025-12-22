namespace IDS.Core.IDS_CAN
{
	public interface IBlock
	{
		IDevice Device { get; }

		BLOCK_ID ID { get; }

		BLOCK_FLAGS Flags { get; }

		ulong Capacity { get; }

		ulong Size { get; }

		uint CRC { get; set; }

		ulong StartAddress { get; set; }

		ulong SetSize { get; set; }

		SESSION_ID ReadSessionID { get; }

		SESSION_ID WriteSessionID { get; }
	}
}
