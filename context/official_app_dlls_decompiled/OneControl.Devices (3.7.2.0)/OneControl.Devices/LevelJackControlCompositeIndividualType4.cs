namespace OneControl.Devices
{
	public readonly struct LevelJackControlCompositeIndividualType4
	{
		public LevelerJackDirection FrontLeftDirection { get; }

		public LevelerJackDirection FrontRightDirection { get; }

		public LevelerJackDirection RearLeftDirection { get; }

		public LevelerJackDirection RearRightDirection { get; }

		public LevelerJackDirection TongueDirection { get; }

		public LevelJackControlCompositeIndividualType4(LevelerJackDirection frontLeftDirection, LevelerJackDirection frontRightDirection, LevelerJackDirection rearLeftDirection, LevelerJackDirection rearRightDirection, LevelerJackDirection tongueDirection)
		{
			FrontLeftDirection = frontLeftDirection;
			FrontRightDirection = frontRightDirection;
			RearLeftDirection = rearLeftDirection;
			RearRightDirection = rearRightDirection;
			TongueDirection = tongueDirection;
		}

		internal LogicalDeviceLevelerJackMovementType4 ToJackMovement()
		{
			return LogicalDeviceLevelerJackMovementType4.None | LogicalDeviceLevelerJackMovementExtensionType4.MakeJackMovement(FrontLeftDirection, (FrontLeftDirection != 0) ? LevelerJackLocation.FrontLeft : LevelerJackLocation.None) | LogicalDeviceLevelerJackMovementExtensionType4.MakeJackMovement(FrontRightDirection, (FrontRightDirection != 0) ? LevelerJackLocation.FrontRight : LevelerJackLocation.None) | LogicalDeviceLevelerJackMovementExtensionType4.MakeJackMovement(RearLeftDirection, (RearLeftDirection != 0) ? LevelerJackLocation.RearLeft : LevelerJackLocation.None) | LogicalDeviceLevelerJackMovementExtensionType4.MakeJackMovement(RearRightDirection, (RearRightDirection != 0) ? LevelerJackLocation.RearRight : LevelerJackLocation.None) | LogicalDeviceLevelerJackMovementExtensionType4.MakeJackMovement(TongueDirection, (TongueDirection != 0) ? LevelerJackLocation.Tongue : LevelerJackLocation.None);
		}
	}
}
