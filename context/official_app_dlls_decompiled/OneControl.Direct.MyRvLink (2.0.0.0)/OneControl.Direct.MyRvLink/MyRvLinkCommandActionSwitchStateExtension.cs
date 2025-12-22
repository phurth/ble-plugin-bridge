namespace OneControl.Direct.MyRvLink
{
	public static class MyRvLinkCommandActionSwitchStateExtension
	{
		public static byte Encode(this MyRvLinkCommandActionSwitchState switchState)
		{
			return (byte)switchState;
		}

		public static MyRvLinkCommandActionSwitchState Decode(byte data)
		{
			return (MyRvLinkCommandActionSwitchState)(data & 1);
		}
	}
}
