namespace IDS.Core
{
	public static class CANExtensions
	{
		public static int EstimateNumberOfBitsInMessage(this CAN.IReadOnlyPacket msg)
		{
			return CAN.EstimateNumberOfBitsInMessage(msg.ID, msg.Payload.Length);
		}
	}
}
