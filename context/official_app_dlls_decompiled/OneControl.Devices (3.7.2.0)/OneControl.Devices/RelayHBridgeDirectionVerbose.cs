namespace OneControl.Devices
{
	public enum RelayHBridgeDirectionVerbose
	{
		[RelayHBridgeDirectionVerboseLabel("FORWARD")]
		Foward = 1,
		[RelayHBridgeDirectionVerboseLabel("EXTEND")]
		Extend = 2,
		[RelayHBridgeDirectionVerboseLabel("CLOCKWISE")]
		Clockwise = 3,
		[RelayHBridgeDirectionVerboseLabel("OUT")]
		Out = 4,
		[RelayHBridgeDirectionVerboseLabel("UP")]
		Up = 5,
		[RelayHBridgeDirectionVerboseLabel("CLOSE")]
		Close = 6,
		[RelayHBridgeDirectionVerboseLabel("LOCK")]
		Lock = 7,
		[RelayHBridgeDirectionVerboseLabel("START")]
		Start = 8,
		[RelayHBridgeDirectionVerboseLabel("RAISE")]
		Raise = 9,
		[RelayHBridgeDirectionVerboseLabel("NONE")]
		None = 0,
		[RelayHBridgeDirectionVerboseLabel("REVERSE")]
		Reverse = -1,
		[RelayHBridgeDirectionVerboseLabel("RETRACT")]
		Retract = -2,
		[RelayHBridgeDirectionVerboseLabel("COUNTER CLOCKWISE")]
		CounterClockwise = -3,
		[RelayHBridgeDirectionVerboseLabel("IN")]
		In = -4,
		[RelayHBridgeDirectionVerboseLabel("DOWN")]
		Down = -5,
		[RelayHBridgeDirectionVerboseLabel("OPEN")]
		Open = -6,
		[RelayHBridgeDirectionVerboseLabel("UNLOCK")]
		Unlock = -7,
		[RelayHBridgeDirectionVerboseLabel("STOP")]
		Stop = -8,
		[RelayHBridgeDirectionVerboseLabel("LOWER")]
		Lower = -9
	}
}
