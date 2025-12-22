namespace OneControl.Devices
{
	public readonly struct LevelJackControlCompositeIndividualAll
	{
		private readonly LevelJackControlCompositeIndividualType4 _compositeIndividualType4;

		public LevelJackControlCompositeIndividualType4 CompositeIndividualType4 => _compositeIndividualType4;

		public LevelerJackDirection FrontLeftDirection => _compositeIndividualType4.FrontLeftDirection;

		public LevelerJackDirection FrontRightDirection => _compositeIndividualType4.FrontRightDirection;

		public LevelerJackDirection MiddleLeftDirection { get; }

		public LevelerJackDirection MiddleRightDirection { get; }

		public LevelerJackDirection RearLeftDirection => _compositeIndividualType4.RearLeftDirection;

		public LevelerJackDirection RearRightDirection => _compositeIndividualType4.RearRightDirection;

		public LevelerJackDirection TongueDirection => _compositeIndividualType4.TongueDirection;

		public LevelJackControlCompositeIndividualAll(LevelerJackDirection frontLeftDirection, LevelerJackDirection frontRightDirection, LevelerJackDirection middleLeftDirection, LevelerJackDirection middleRightDirection, LevelerJackDirection rearLeftDirection, LevelerJackDirection rearRightDirection, LevelerJackDirection tongueDirection)
		{
			_compositeIndividualType4 = new LevelJackControlCompositeIndividualType4(frontLeftDirection, frontRightDirection, rearLeftDirection, rearRightDirection, tongueDirection);
			MiddleLeftDirection = LevelerJackDirection.None;
			MiddleRightDirection = LevelerJackDirection.None;
		}

		internal LogicalDeviceLevelerJackMovementType4 ToJackMovement()
		{
			return _compositeIndividualType4.ToJackMovement() | LogicalDeviceLevelerJackMovementExtensionType4.MakeJackMovement(MiddleLeftDirection, (MiddleLeftDirection != 0) ? LevelerJackLocation.MiddleLeft : LevelerJackLocation.None) | LogicalDeviceLevelerJackMovementExtensionType4.MakeJackMovement(MiddleRightDirection, (MiddleRightDirection != 0) ? LevelerJackLocation.MiddleRight : LevelerJackLocation.None);
		}
	}
}
