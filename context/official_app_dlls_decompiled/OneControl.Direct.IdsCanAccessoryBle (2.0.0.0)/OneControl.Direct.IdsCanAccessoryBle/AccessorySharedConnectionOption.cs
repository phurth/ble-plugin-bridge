using System;

namespace OneControl.Direct.IdsCanAccessoryBle
{
	[Flags]
	public enum AccessorySharedConnectionOption
	{
		None = 0,
		ManualClose = 1,
		DontConnect = 2
	}
}
