using System.ComponentModel;

namespace OneControl.Devices
{
	public enum LogicalDeviceLevelerAutoStepType4
	{
		[Description("")]
		None,
		[Description("Level")]
		Level,
		[Description("Level Front")]
		LevelFront,
		[Description("Level Rear")]
		LevelRear,
		[Description("Retract Jacks")]
		RetractJacks,
		[Description("Retract Front")]
		RetractFront,
		[Description("Retract Rear")]
		RetractRear,
		[Description("Retract Middle")]
		RetractMiddle,
		[Description("Retract Tongue")]
		RetractTongue,
		[Description("Ground Jacks")]
		GroundJacks,
		[Description("Ground Front")]
		GroundFront,
		[Description("Ground Rear")]
		GroundRear,
		[Description("Ground Tongue")]
		GroundTongue,
		[Description("Extend Jacks")]
		ExtendJacks,
		[Description("Extend Front")]
		ExtendFront,
		[Description("Extend Rear")]
		ExtendRear,
		[Description("Extend Tongue")]
		ExtendTongue,
		[Description("Verify Ground")]
		VerifyGround,
		[Description("Find Hitch")]
		FindHitch,
		[Description("Clear Tongue Jack")]
		ClearTongueJack,
		[Description("Stabilize")]
		Stabilize,
		[Description("Lift Front")]
		LeftFront,
		[Description("Lift Rear")]
		LiftRear,
		[Description("Lower Front")]
		LowerFront,
		[Description("Lower Rear")]
		LowerRear,
		[Description("Fill Airbags")]
		FillAirBags,
		[Description("Dump Airbags")]
		EmptyAirBags,
		[Description("Raise Axle")]
		RaiseAxle,
		[Description("Lower Axle")]
		LowerAxle,
		[Description("Stow Axle")]
		StowAxle
	}
}
