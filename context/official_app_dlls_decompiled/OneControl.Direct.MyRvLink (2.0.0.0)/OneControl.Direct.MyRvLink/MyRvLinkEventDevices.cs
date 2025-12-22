namespace OneControl.Direct.MyRvLink
{
	public abstract class MyRvLinkEventDevices<TEvent> : MyRvLinkEvent<TEvent> where TEvent : IMyRvLinkEvent
	{
		protected override byte[] _rawData { get; }

		protected abstract int MaxPayloadLength(int deviceCount);

		protected MyRvLinkEventDevices(int deviceCount)
		{
			_rawData = new byte[MaxPayloadLength(deviceCount)];
			_rawData[0] = (byte)EventType;
		}

		protected MyRvLinkEventDevices(byte[] rawData)
		{
			_rawData = rawData;
			ValidateEventRawDataBasic(rawData);
		}
	}
}
