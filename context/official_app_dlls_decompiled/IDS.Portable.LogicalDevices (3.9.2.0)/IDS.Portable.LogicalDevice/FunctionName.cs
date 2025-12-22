using System.ComponentModel;

namespace IDS.Portable.LogicalDevice
{
	[DefaultValue(FunctionName.Unknown)]
	public enum FunctionName : ushort
	{
		[FunctionClass(FUNCTION_CLASS.UNKNOWN)]
		[FunctionNameDetail(FunctionNameDetailLocation.Unknown, FunctionNameDetailPosition.Unknown, FunctionNameDetailRoom.Unknown, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Unknown,
		[FunctionClass(FUNCTION_CLASS.MISCELLANEOUS)]
		[FunctionNameDetail(FunctionNameDetailLocation.Unknown, FunctionNameDetailPosition.Unknown, FunctionNameDetailRoom.Unknown, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		DiagnosticTool,
		[FunctionClass(FUNCTION_CLASS.MISCELLANEOUS)]
		[FunctionNameDetail(FunctionNameDetailLocation.Unknown, FunctionNameDetailPosition.Unknown, FunctionNameDetailRoom.Unknown, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		MyRvTablet,
		[FunctionClass(FUNCTION_CLASS.WATER_HEATER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		GasWaterHeater,
		[FunctionClass(FUNCTION_CLASS.WATER_HEATER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		ElectricWaterHeater,
		[FunctionClass(FUNCTION_CLASS.PUMP)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		WaterPump,
		[FunctionClass(FUNCTION_CLASS.VENT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		BathVent,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Light,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		FloodLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		WorkLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		FrontBedroomCeilingLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		FrontBedroomOverheadLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		FrontBedroomVanityLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		FrontBedroomSconceLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Loft, FunctionNameDetailUse.Unknown)]
		FrontBedroomLoftLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		RearBedroomCeilingLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		RearBedroomOverheadLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		RearBedroomVanityLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		RearBedroomSconceLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Loft, FunctionNameDetailUse.Unknown)]
		RearBedroomLoftLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Loft, FunctionNameDetailUse.Unknown)]
		LoftLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Hall, FunctionNameDetailUse.Unknown)]
		FrontHallLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Hall, FunctionNameDetailUse.Unknown)]
		RearHallLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		FrontBathroomLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		FrontBathroomVanityLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		FrontBathroomCeilingLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		FrontBathroomShowerLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		FrontBathroomSconceLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		RearBathroomVanityLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		RearBathroomCeilingLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		RearBathroomShowerLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		RearBathroomSconceLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Kitchen, FunctionNameDetailUse.Unknown)]
		KitchenCeilingLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Kitchen, FunctionNameDetailUse.Unknown)]
		KitchenSconceLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Kitchen, FunctionNameDetailUse.Unknown)]
		KitchenPendantsLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Kitchen, FunctionNameDetailUse.Unknown)]
		KitchenRangeLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Kitchen, FunctionNameDetailUse.Unknown)]
		KitchenCounterLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Kitchen, FunctionNameDetailUse.Unknown)]
		KitchenBarLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Kitchen, FunctionNameDetailUse.Unknown)]
		KitchenIslandLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Kitchen, FunctionNameDetailUse.Unknown)]
		KitchenChandelierLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Kitchen, FunctionNameDetailUse.Unknown)]
		KitchenUnderCabinetLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.LivingRoom, FunctionNameDetailUse.Unknown)]
		LivingRoomCeilingLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.LivingRoom, FunctionNameDetailUse.Unknown)]
		LivingRoomSconceLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.LivingRoom, FunctionNameDetailUse.Unknown)]
		LivingRoomPendantsLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.LivingRoom, FunctionNameDetailUse.Unknown)]
		LivingRoomBarLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Garage, FunctionNameDetailUse.Unknown)]
		GarageCeilingLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Garage, FunctionNameDetailUse.Unknown)]
		GarageCabinetLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		SecurityLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		PorchLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		AwningLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		BathroomLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		BathroomVanityLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		BathroomCeilingLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		BathroomShowerLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		BathroomSconceLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Hall, FunctionNameDetailUse.Unknown)]
		HallLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bunk, FunctionNameDetailUse.Unknown)]
		BunkRoomLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		BedroomLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.LivingRoom, FunctionNameDetailUse.Unknown)]
		LivingRoomLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Kitchen, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		KitchenLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		LoungeLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		CeilingLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		EntryLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		BedCeilingLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		BedroomLavLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		ShowerLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Kitchen, FunctionNameDetailUse.Unknown)]
		GalleyLight,
		[FunctionClass(FUNCTION_CLASS.TANK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		FreshTank,
		[FunctionClass(FUNCTION_CLASS.TANK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		GreyTank,
		[FunctionClass(FUNCTION_CLASS.TANK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		BlackTank,
		[FunctionClass(FUNCTION_CLASS.TANK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		FuelTank,
		[FunctionClass(FUNCTION_CLASS.TANK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Fuel" })]
		GeneratorFuelTank,
		[FunctionClass(FUNCTION_CLASS.TANK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "Auxiliary Fuel Tank" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Auxiliary" })]
		AuxilliaryFuelTank,
		[FunctionClass(FUNCTION_CLASS.TANK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Grey" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.ApplyDefaultRules, new string[] { })]
		FrontBathGreyTank,
		[FunctionClass(FUNCTION_CLASS.TANK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Fresh" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.ApplyDefaultRules, new string[] { })]
		FrontBathFreshTank,
		[FunctionClass(FUNCTION_CLASS.TANK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Black" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.ApplyDefaultRules, new string[] { })]
		FrontBathBlackTank,
		[FunctionClass(FUNCTION_CLASS.TANK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Grey" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.ApplyDefaultRules, new string[] { })]
		RearBathGreyTank,
		[FunctionClass(FUNCTION_CLASS.TANK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Fresh" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.ApplyDefaultRules, new string[] { })]
		RearBathFreshTank,
		[FunctionClass(FUNCTION_CLASS.TANK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Black" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.ApplyDefaultRules, new string[] { })]
		RearBathBlackTank,
		[FunctionClass(FUNCTION_CLASS.TANK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Grey" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.ApplyDefaultRules, new string[] { })]
		MainBathGreyTank,
		[FunctionClass(FUNCTION_CLASS.TANK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Fresh" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.ApplyDefaultRules, new string[] { })]
		MainBathFreshTank,
		[FunctionClass(FUNCTION_CLASS.TANK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Black" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.ApplyDefaultRules, new string[] { })]
		MainBathBlackTank,
		[FunctionClass(FUNCTION_CLASS.TANK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Grey" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.ApplyDefaultRules, new string[] { })]
		GalleyGreyTank,
		[FunctionClass(FUNCTION_CLASS.TANK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Fresh" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.ApplyDefaultRules, new string[] { })]
		GalleyFreshTank,
		[FunctionClass(FUNCTION_CLASS.TANK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Black" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.ApplyDefaultRules, new string[] { })]
		GalleyBlackTank,
		[FunctionClass(FUNCTION_CLASS.TANK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Grey" })]
		KitchenGreyTank,
		[FunctionClass(FUNCTION_CLASS.TANK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Fresh" })]
		KitchenFreshTank,
		[FunctionClass(FUNCTION_CLASS.TANK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Black" })]
		KitchenBlackTank,
		[FunctionClass(FUNCTION_CLASS.LANDING_GEAR)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		LandingGear,
		[FunctionClass(FUNCTION_CLASS.STABILIZER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		FrontStabilizer,
		[FunctionClass(FUNCTION_CLASS.STABILIZER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		RearStabilizer,
		[FunctionClass(FUNCTION_CLASS.LIFT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Lift" })]
		TvLift,
		[FunctionClass(FUNCTION_CLASS.LIFT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Lift" })]
		BedLift,
		[FunctionClass(FUNCTION_CLASS.VENT_COVER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "Bathroom" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		BathVentCover,
		[FunctionClass(FUNCTION_CLASS.LOCK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		DoorLock,
		[FunctionClass(FUNCTION_CLASS.GENERATOR, new FUNCTION_CLASS[] { FUNCTION_CLASS.HOUR_METER })]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Generator,
		[FunctionClass(FUNCTION_CLASS.SLIDE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Slide,
		[FunctionClass(FUNCTION_CLASS.SLIDE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Main" })]
		MainSlide,
		[FunctionClass(FUNCTION_CLASS.SLIDE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Bedroom" })]
		BedroomSlide,
		[FunctionClass(FUNCTION_CLASS.SLIDE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Kitchen, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Galley" })]
		GalleySlide,
		[FunctionClass(FUNCTION_CLASS.SLIDE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Kitchen, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Kitchen" })]
		KitchenSlide,
		[FunctionClass(FUNCTION_CLASS.SLIDE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Closet" })]
		ClosetSlide,
		[FunctionClass(FUNCTION_CLASS.SLIDE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Optional" })]
		OptionalSlide,
		[FunctionClass(FUNCTION_CLASS.SLIDE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Door Side" })]
		DoorSideSlide,
		[FunctionClass(FUNCTION_CLASS.SLIDE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Off-Door" })]
		OffDoorSlide,
		[FunctionClass(FUNCTION_CLASS.AWNING)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Awning,
		[FunctionClass(FUNCTION_CLASS.LEVELER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		LevelUpLeveler,
		[FunctionClass(FUNCTION_CLASS.TANK_HEATER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		WaterTankHeater,
		[FunctionClass(FUNCTION_CLASS.MISCELLANEOUS)]
		[FunctionNameDetail(FunctionNameDetailLocation.Unknown, FunctionNameDetailPosition.Unknown, FunctionNameDetailRoom.Unknown, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		MyRvTouchscreen,
		[FunctionClass(FUNCTION_CLASS.LEVELER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Leveler,
		[FunctionClass(FUNCTION_CLASS.VENT_COVER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		VentCover,
		[FunctionClass(FUNCTION_CLASS.VENT_COVER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "Front Bedroom" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "F Bedroom" })]
		FrontBedroomVentCover,
		[FunctionClass(FUNCTION_CLASS.VENT_COVER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "Bedroom" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Bedroom" })]
		BedroomVentCover,
		[FunctionClass(FUNCTION_CLASS.VENT_COVER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "Front Bathroom" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Front Bath" })]
		FrontBathroomVentCover,
		[FunctionClass(FUNCTION_CLASS.VENT_COVER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "Main Bathroom" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Main Bath" })]
		MainBathroomVentCover,
		[FunctionClass(FUNCTION_CLASS.VENT_COVER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "Rear Bathroom" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Rear Bath" })]
		RearBathroomVentCover,
		[FunctionClass(FUNCTION_CLASS.VENT_COVER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Kitchen, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "Kitchen" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Kitchen" })]
		KitchenVentCover,
		[FunctionClass(FUNCTION_CLASS.VENT_COVER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.LivingRoom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "Living Room" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		LivingRoomVentCover,
		[FunctionClass(FUNCTION_CLASS.LEVELER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Leveler" })]
		FourLegTruckCamperLeveler,
		[FunctionClass(FUNCTION_CLASS.LEVELER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Leveler" })]
		SixLegHallEffectEjLeveler,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Patio, FunctionNameDetailUse.Awning)]
		PatioLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		HutchLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		ScareLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		DinetteLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		BarLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Overhead" })]
		OverheadLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		OverheadBarLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		FoyerLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Garage, FunctionNameDetailUse.Awning)]
		RampDoorLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Entertainment" })]
		EntertainmentLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		RearEntryDoorLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		CeilingFanLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		OverheadFanLight,
		[FunctionClass(FUNCTION_CLASS.SLIDE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Slide" })]
		BunkSlide,
		[FunctionClass(FUNCTION_CLASS.SLIDE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Slide" })]
		BedSlide,
		[FunctionClass(FUNCTION_CLASS.SLIDE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Slide" })]
		WardrobeSlide,
		[FunctionClass(FUNCTION_CLASS.SLIDE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Slide" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Entertainment" })]
		EntertainmentSlide,
		[FunctionClass(FUNCTION_CLASS.SLIDE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Slide" })]
		SofaSlide,
		[FunctionClass(FUNCTION_CLASS.AWNING)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Patio, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Awning" })]
		PatioAwning,
		[FunctionClass(FUNCTION_CLASS.AWNING)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Awning" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Rear" })]
		RearAwning,
		[FunctionClass(FUNCTION_CLASS.AWNING)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Side, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Awning" })]
		SideAwning,
		[FunctionClass(FUNCTION_CLASS.LEVELER_2)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Jacks,
		[FunctionClass(FUNCTION_CLASS.LEVELER_2)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Leveler2,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		ExteriorLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		LowerAccentLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		UpperAccentLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		DsSecurityLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		OdsSecurityLight,
		[FunctionClass(FUNCTION_CLASS.SLIDE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		SlideInSlide,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		HitchLight,
		[FunctionClass(FUNCTION_CLASS.REAL_TIME_CLOCK, new FUNCTION_CLASS[] { FUNCTION_CLASS.IR_REMOTE_CONTROL })]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Clock,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Tv,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Dvd,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		BluRay,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Vcr,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Pvr,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Cable,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Satellite,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Audio,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		CdPlayer,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Tuner,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Radio,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Speakers,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Game,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		ClockRadio,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL, new FUNCTION_CLASS[] { FUNCTION_CLASS.TEMPERATURE_SENSOR })]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Aux,
		[FunctionClass(FUNCTION_CLASS.HVAC_CONTROL, new FUNCTION_CLASS[] { FUNCTION_CLASS.IR_REMOTE_CONTROL })]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		ClimateZone,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Fireplace,
		[FunctionClass(FUNCTION_CLASS.HVAC_CONTROL, new FUNCTION_CLASS[] { FUNCTION_CLASS.IR_REMOTE_CONTROL })]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Thermostat,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		FrontCapLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		StepLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		DsFloodLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		InteriorLight,
		[FunctionClass(FUNCTION_CLASS.TANK_HEATER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		FreshTankHeater,
		[FunctionClass(FUNCTION_CLASS.TANK_HEATER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		GreyTankHeater,
		[FunctionClass(FUNCTION_CLASS.TANK_HEATER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		BlackTankHeater,
		[FunctionClass(FUNCTION_CLASS.VALVE, new FUNCTION_CLASS[] { FUNCTION_CLASS.TANK })]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Tank" })]
		LpTank,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		StallLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Main" })]
		MainLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		BathLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bunk, FunctionNameDetailUse.Unknown)]
		BunkLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		BedLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Cabinet" })]
		CabinetLight,
		[FunctionClass(FUNCTION_CLASS.NETWORK_BRIDGE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		NetworkBridge,
		[FunctionClass(FUNCTION_CLASS.NETWORK_BRIDGE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		EthernetBridge,
		[FunctionClass(FUNCTION_CLASS.NETWORK_BRIDGE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		WifiBridge,
		[FunctionClass(FUNCTION_CLASS.IPDM)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		InTransitPowerDisconnect,
		[FunctionClass(FUNCTION_CLASS.LEVELER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		LevelUpUnity,
		[FunctionClass(FUNCTION_CLASS.LEVELER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		TtLeveler,
		[FunctionClass(FUNCTION_CLASS.LEVELER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		TravelTrailerLeveler,
		[FunctionClass(FUNCTION_CLASS.LEVELER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		FifthWheelLeveler,
		[FunctionClass(FUNCTION_CLASS.PUMP)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		FuelPump,
		[FunctionClass(FUNCTION_CLASS.HVAC_CONTROL, new FUNCTION_CLASS[] { FUNCTION_CLASS.IR_REMOTE_CONTROL })]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Climate Zone" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Main" })]
		MainClimateZone,
		[FunctionClass(FUNCTION_CLASS.HVAC_CONTROL, new FUNCTION_CLASS[] { FUNCTION_CLASS.IR_REMOTE_CONTROL })]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Climate Zone" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Bedroom" })]
		BedroomClimateZone,
		[FunctionClass(FUNCTION_CLASS.HVAC_CONTROL, new FUNCTION_CLASS[] { FUNCTION_CLASS.IR_REMOTE_CONTROL })]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Garage, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Climate Zone" })]
		GarageClimateZone,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Compartment, FunctionNameDetailUse.Awning)]
		CompartmentLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Trunk, FunctionNameDetailUse.Awning)]
		TrunkLight,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bar, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "TV" })]
		BarTv,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "TV" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Bathroom" })]
		BathroomTv,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "TV" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Bedroom" })]
		BedroomTv,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bunk, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "TV" })]
		BunkRoomTv,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "TV" })]
		ExteriorTv,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "TV" })]
		FrontBathroomTv,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "TV" })]
		FrontBedroomTv,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Garage, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "TV" })]
		GarageTv,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Kitchen, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "TV" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Kitchen" })]
		KitchenTv,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.LivingRoom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "TV" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Living RM" })]
		LivingRoomTv,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Loft, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "TV" })]
		LoftTv,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "TV" })]
		LoungeTv,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "TV" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Main" })]
		MainTv,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Patio, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "TV" })]
		PatioTv,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "TV" })]
		RearBathroomTv,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "TV" })]
		RearBedroomTv,
		[FunctionClass(FUNCTION_CLASS.LOCK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		BathroomDoorLock,
		[FunctionClass(FUNCTION_CLASS.LOCK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		BedroomDoorLock,
		[FunctionClass(FUNCTION_CLASS.LOCK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		FrontDoorLock,
		[FunctionClass(FUNCTION_CLASS.LOCK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Garage, FunctionNameDetailUse.Unknown)]
		GarageDoorLock,
		[FunctionClass(FUNCTION_CLASS.LOCK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		MainDoorLock,
		[FunctionClass(FUNCTION_CLASS.LOCK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Patio, FunctionNameDetailUse.Unknown)]
		PatioDoorLock,
		[FunctionClass(FUNCTION_CLASS.LOCK)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		RearDoorLock,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		AccentLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		BathroomAccentLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		BedroomAccentLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		FrontBedroomAccentLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Garage, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		GarageAccentLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Kitchen, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		KitchenAccentLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Patio, FunctionNameDetailUse.Awning)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		PatioAccentLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		RearBedroomAccentLight,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Radio" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		BedroomRadio,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bunk, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Radio" })]
		BunkRoomRadio,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Radio" })]
		ExteriorRadio,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Radio" })]
		FrontBedroomRadio,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Garage, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Radio" })]
		GarageRadio,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Kitchen, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Radio" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		KitchenRadio,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.LivingRoom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Radio" })]
		LivingRoomRadio,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Loft, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Radio" })]
		LoftRadio,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Patio, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Radio" })]
		PatioRadio,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Radio" })]
		RearBedroomRadio,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "System" })]
		BedroomEntertainmentSystem,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bunk, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "System" })]
		BunkRoomEntertainmentSystem,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "System" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		EntertainmentSystem,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "System" })]
		ExteriorEntertainmentSystem,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "System" })]
		FrontBedroomEntertainmentSystem,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Garage, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "System" })]
		GarageEntertainmentSystem,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Kitchen, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "System" })]
		KitchenEntertainmentSystem,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.LivingRoom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "System" })]
		LivingRoomEntertainmentSystem,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Loft, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "System" })]
		LoftEntertainmentSystem,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "System" })]
		MainEntertainmentSystem,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Patio, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "System" })]
		PatioEntertainmentSystem,
		[FunctionClass(FUNCTION_CLASS.IR_REMOTE_CONTROL)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "System" })]
		RearBedroomEntertainmentSystem,
		[FunctionClass(FUNCTION_CLASS.STABILIZER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		LeftStabilizer,
		[FunctionClass(FUNCTION_CLASS.STABILIZER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		RightStabilizer,
		[FunctionClass(FUNCTION_CLASS.STABILIZER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Stabilizer,
		[FunctionClass(FUNCTION_CLASS.POWER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Solar,
		[FunctionClass(FUNCTION_CLASS.POWER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "Solar Power" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		SolarPower,
		[FunctionClass(FUNCTION_CLASS.POWER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.BatteryMonitor)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Battery,
		[FunctionClass(FUNCTION_CLASS.POWER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "Main Battery" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Battery" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		MainBattery,
		[FunctionClass(FUNCTION_CLASS.POWER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "Aux Battery" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Battery" })]
		AuxBattery,
		[FunctionClass(FUNCTION_CLASS.POWER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		ShorePower,
		[FunctionClass(FUNCTION_CLASS.POWER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		AcPower,
		[FunctionClass(FUNCTION_CLASS.POWER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		AcMains,
		[FunctionClass(FUNCTION_CLASS.POWER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "Aux Power" })]
		AuxPower,
		[FunctionClass(FUNCTION_CLASS.POWER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Outputs,
		[FunctionClass(FUNCTION_CLASS.DOOR)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Garage, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		RampDoor,
		[FunctionClass(FUNCTION_CLASS.FAN)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Fan,
		[FunctionClass(FUNCTION_CLASS.FAN)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		BathFan,
		[FunctionClass(FUNCTION_CLASS.FAN)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		RearFan,
		[FunctionClass(FUNCTION_CLASS.FAN)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		FrontFan,
		[FunctionClass(FUNCTION_CLASS.FAN)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Kitchen, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		KitchenFan,
		[FunctionClass(FUNCTION_CLASS.FAN)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		CeilingFan,
		[FunctionClass(FUNCTION_CLASS.TANK_HEATER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		TankHeater,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		FrontCeilingLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		RearCeilingLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Compartment, FunctionNameDetailUse.Awning)]
		CargoLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		FasciaLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		SlideCeilingLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		SlideOverheadLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		DecorLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		ReadingLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		FrontReadingLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		RearReadingLight,
		[FunctionClass(FUNCTION_CLASS.HVAC_CONTROL, new FUNCTION_CLASS[] { FUNCTION_CLASS.IR_REMOTE_CONTROL })]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.LivingRoom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Climate Zone" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		LivingRoomClimateZone,
		[FunctionClass(FUNCTION_CLASS.HVAC_CONTROL, new FUNCTION_CLASS[] { FUNCTION_CLASS.IR_REMOTE_CONTROL })]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.LivingRoom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Climate Zone" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		FrontLivingRoomClimateZone,
		[FunctionClass(FUNCTION_CLASS.HVAC_CONTROL, new FUNCTION_CLASS[] { FUNCTION_CLASS.IR_REMOTE_CONTROL })]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.LivingRoom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Climate Zone" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		RearLivingRoomClimateZone,
		[FunctionClass(FUNCTION_CLASS.HVAC_CONTROL, new FUNCTION_CLASS[] { FUNCTION_CLASS.IR_REMOTE_CONTROL })]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Climate Zone" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		FrontBedroomClimateZone,
		[FunctionClass(FUNCTION_CLASS.HVAC_CONTROL, new FUNCTION_CLASS[] { FUNCTION_CLASS.IR_REMOTE_CONTROL })]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Climate Zone" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		RearBedroomClimateZone,
		[FunctionClass(FUNCTION_CLASS.LIFT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Tilt" })]
		BedTilt,
		[FunctionClass(FUNCTION_CLASS.LIFT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Tilt" })]
		FrontBedTilt,
		[FunctionClass(FUNCTION_CLASS.LIFT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Tilt" })]
		RearBedTilt,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		MensLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		WomensLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		ServiceLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		OdsFloodLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "UNDRBDY ACC" })]
		UnderbodyAccentLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		SpeakerLight,
		[FunctionClass(FUNCTION_CLASS.WATER_HEATER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		WaterHeater,
		[FunctionClass(FUNCTION_CLASS.WATER_HEATER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		WaterHeaters,
		[FunctionClass(FUNCTION_CLASS.Router)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Aquafi,
		[FunctionClass(FUNCTION_CLASS.NETWORK_BRIDGE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		ConnectAnywhere,
		[FunctionClass(FUNCTION_CLASS.SLIDE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Slide" })]
		SlideIfEquip,
		[FunctionClass(FUNCTION_CLASS.AWNING)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Awning" })]
		AwningIfEquip,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Light " })]
		AwningLightIfEquip,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Light " })]
		InteriorLightIfEquip,
		[FunctionClass(FUNCTION_CLASS.VALVE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		WasteValve,
		[FunctionClass(FUNCTION_CLASS.TireMonitor)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		TireLinc,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		FrontLockerLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		RearLockerLight,
		[FunctionClass(FUNCTION_CLASS.TANK_HEATER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		RearAuxPower,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		RockLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		ChassisLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		ExteriorShowerLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.LivingRoom, FunctionNameDetailUse.Unknown)]
		LivingRoomAccentLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		RearFloodLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		PassengerFloodLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		DriverFloodLight,
		[FunctionClass(FUNCTION_CLASS.SLIDE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Slide" })]
		BathroomSlide,
		[FunctionClass(FUNCTION_CLASS.LIFT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Lift" })]
		RoofLift,
		[FunctionClass(FUNCTION_CLASS.TANK_HEATER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		YetiPackage,
		[FunctionClass(FUNCTION_CLASS.TANK_HEATER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		PropaneLocker,
		[FunctionClass(FUNCTION_CLASS.AWNING)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Garage, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Awning" })]
		GarageAwning,
		[FunctionClass(FUNCTION_CLASS.MonitorPanel)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		MonitorPanel,
		[FunctionClass(FUNCTION_CLASS.Camera)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Camera,
		[FunctionClass(FUNCTION_CLASS.POWER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		JaycoAusTbbGw,
		[FunctionClass(FUNCTION_CLASS.NETWORK_BRIDGE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		GatewayRvLink,
		[FunctionClass(FUNCTION_CLASS.TEMPERATURE_SENSOR)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Temperature" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		AccessoryTemperature,
		[FunctionClass(FUNCTION_CLASS.TEMPERATURE_SENSOR)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		AccessoryRefrigerator,
		[FunctionClass(FUNCTION_CLASS.TEMPERATURE_SENSOR)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "ACCY FRIDGE" })]
		AccessoryFridge,
		[FunctionClass(FUNCTION_CLASS.TEMPERATURE_SENSOR)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		AccessoryFreezer,
		[FunctionClass(FUNCTION_CLASS.TEMPERATURE_SENSOR)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		AccessoryExternal,
		[FunctionClass(FUNCTION_CLASS.SafetySystems)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		TrailerBrakeController,
		[FunctionClass(FUNCTION_CLASS.TEMPERATURE_SENSOR)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Temperature" })]
		TempRefrigerator,
		[FunctionClass(FUNCTION_CLASS.TEMPERATURE_SENSOR)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Temperature " })]
		TempRefrigeratorHome,
		[FunctionClass(FUNCTION_CLASS.TEMPERATURE_SENSOR)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Temperature" })]
		TempFreezer,
		[FunctionClass(FUNCTION_CLASS.TEMPERATURE_SENSOR)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Temperature " })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		TempFreezerHome,
		[FunctionClass(FUNCTION_CLASS.TEMPERATURE_SENSOR)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Temperature" })]
		TempCooler,
		[FunctionClass(FUNCTION_CLASS.TEMPERATURE_SENSOR)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Kitchen, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Temperature" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		TempKitchen,
		[FunctionClass(FUNCTION_CLASS.TEMPERATURE_SENSOR)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.LivingRoom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Temperature" })]
		TempLivingRoom,
		[FunctionClass(FUNCTION_CLASS.TEMPERATURE_SENSOR)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Temperature" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		TempBedroom,
		[FunctionClass(FUNCTION_CLASS.TEMPERATURE_SENSOR)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Temperature" })]
		TempMasterBedroom,
		[FunctionClass(FUNCTION_CLASS.TEMPERATURE_SENSOR)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Garage, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Temperature" })]
		TempGarage,
		[FunctionClass(FUNCTION_CLASS.TEMPERATURE_SENSOR)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Temperature" })]
		TempBasement,
		[FunctionClass(FUNCTION_CLASS.TEMPERATURE_SENSOR)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Temperature" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		TempBathroom,
		[FunctionClass(FUNCTION_CLASS.TEMPERATURE_SENSOR)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Temperature" })]
		TempStorageArea,
		[FunctionClass(FUNCTION_CLASS.TEMPERATURE_SENSOR)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Temperature" })]
		TempDriversArea,
		[FunctionClass(FUNCTION_CLASS.TEMPERATURE_SENSOR)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bunk, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Temperature" })]
		TempBunks,
		[FunctionClass(FUNCTION_CLASS.LiquidPropane)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "RV LP" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "LP" })]
		LpTankRv,
		[FunctionClass(FUNCTION_CLASS.LiquidPropane)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "Home LP" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "LP" })]
		LpTankHome,
		[FunctionClass(FUNCTION_CLASS.LiquidPropane)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "Cabin LP" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "LP" })]
		LpTankCabin,
		[FunctionClass(FUNCTION_CLASS.LiquidPropane)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "BBQ LP" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "LP" })]
		LpTankBbq,
		[FunctionClass(FUNCTION_CLASS.LiquidPropane)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "Grill LP" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "LP" })]
		LpTankGrill,
		[FunctionClass(FUNCTION_CLASS.LiquidPropane)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "Submarine LP" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "LP" })]
		LpTankSubmarine,
		[FunctionClass(FUNCTION_CLASS.LiquidPropane)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "Other LP" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "LP" })]
		LpTankOther,
		[FunctionClass(FUNCTION_CLASS.SafetySystems)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		AntiLockBrakingSystem,
		[FunctionClass(FUNCTION_CLASS.NETWORK_BRIDGE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		LoCapGateway,
		[FunctionClass(FUNCTION_CLASS.MISCELLANEOUS)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Bootloader,
		[FunctionClass(FUNCTION_CLASS.POWER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.BatteryMonitor)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "Auxiliary Battery" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Battery" })]
		AuxiliaryBattery,
		[FunctionClass(FUNCTION_CLASS.POWER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.BatteryMonitor)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "Chassis Battery" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Battery" })]
		ChassisBattery,
		[FunctionClass(FUNCTION_CLASS.POWER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.BatteryMonitor)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "House Battery" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Battery" })]
		HouseBattery,
		[FunctionClass(FUNCTION_CLASS.POWER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Kitchen, FunctionNameDetailUse.BatteryMonitor)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "Kitchen Battery" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Battery" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		KitchenBattery,
		[FunctionClass(FUNCTION_CLASS.SafetySystems)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "True Course" })]
		ElectronicSwayControl,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Lights" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		JackLights,
		[FunctionClass(FUNCTION_CLASS.AwningSensor)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Side, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		AwningSensor,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		InteriorStepLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Awning)]
		ExteriorStepLight,
		[FunctionClass(FUNCTION_CLASS.Router)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		WifiBooster,
		[FunctionClass(FUNCTION_CLASS.MISCELLANEOUS)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		AudibleAlert,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		SoffitLight,
		[FunctionClass(FUNCTION_CLASS.POWER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.BatteryMonitor)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "Battery Bank" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		BatteryBank,
		[FunctionClass(FUNCTION_CLASS.POWER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.BatteryMonitor)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "RV Battery" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Battery" })]
		RVBattery,
		[FunctionClass(FUNCTION_CLASS.POWER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.BatteryMonitor)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "Solar Battery" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Battery" })]
		SolarBattery,
		[FunctionClass(FUNCTION_CLASS.POWER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Utility, FunctionNameDetailUse.BatteryMonitor)]
		[FunctionNameOverrideDisplayName(OverrideDisplayNameOperation.Override, new string[] { "Tongue Battery" })]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Battery" })]
		TongueBattery,
		[FunctionClass(FUNCTION_CLASS.SafetySystems)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Axle1BrakeController,
		[FunctionClass(FUNCTION_CLASS.SafetySystems)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Axle2BrakeController,
		[FunctionClass(FUNCTION_CLASS.SafetySystems)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Axle3BrakeController,
		[FunctionClass(FUNCTION_CLASS.POWER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		LeadAcid,
		[FunctionClass(FUNCTION_CLASS.POWER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		LiquidLeadAcid,
		[FunctionClass(FUNCTION_CLASS.POWER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		GelLeadAcid,
		[FunctionClass(FUNCTION_CLASS.POWER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		AgmAbsorbentGlassMat,
		[FunctionClass(FUNCTION_CLASS.POWER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Lithium,
		[FunctionClass(FUNCTION_CLASS.AWNING)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		FrontAwning,
		[FunctionClass(FUNCTION_CLASS.SLIDE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Kitchen, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Slide" })]
		DinetteSlide,
		[FunctionClass(FUNCTION_CLASS.TANK_HEATER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Holding Tanks Heater" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Holding Tanks" })]
		HoldingTanksHeater,
		[FunctionClass(FUNCTION_CLASS.POWER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		Inverter,
		[FunctionClass(FUNCTION_CLASS.POWER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		BatteryHeat,
		[FunctionClass(FUNCTION_CLASS.NETWORK_BRIDGE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		CameraPower,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Patio, FunctionNameDetailUse.Unknown)]
		PatioAwningLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Garage, FunctionNameDetailUse.Unknown)]
		GarageAwningLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		RearAwningLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Side, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		SideAwningLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		SlideAwningLight,
		[FunctionClass(FUNCTION_CLASS.AWNING)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		SlideAwning,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		FrontAwningLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Lights" })]
		CentralLights,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Side, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Lights" })]
		RightSideLights,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Outside, FunctionNameDetailPosition.Side, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Lights" })]
		LeftSideLights,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Lights" })]
		RightSceneLights,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Lights" })]
		LeftSceneLights,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Lights" })]
		RearSceneLights,
		[FunctionClass(FUNCTION_CLASS.FAN)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		ComputerFan,
		[FunctionClass(FUNCTION_CLASS.FAN)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		BatteryFan,
		[FunctionClass(FUNCTION_CLASS.SLIDE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Slide " })]
		RightSlideRoom,
		[FunctionClass(FUNCTION_CLASS.SLIDE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Slide " })]
		LeftSlideRoom,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		DumpLight,
		[FunctionClass(FUNCTION_CLASS.MonitorPanel)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		BaseCampTouchscreen,
		[FunctionClass(FUNCTION_CLASS.LEVELER)]
		[FunctionNameDetail(FunctionNameDetailLocation.Any, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Unchanged, new string[] { })]
		BaseCampLeveler,
		[FunctionClass(FUNCTION_CLASS.MonitorPanel)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Kitchen, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.ApplyDefaultRules, new string[] { })]
		Refrigerator,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Kitchen, FunctionNameDetailUse.Unknown)]
		KitchenPendantLight,
		[FunctionClass(FUNCTION_CLASS.SLIDE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Slide " })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "DS Sofa" })]
		DoorSideSofaSlide,
		[FunctionClass(FUNCTION_CLASS.SLIDE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Slide " })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Off-DS Sofa" })]
		OffDoorSideSofaSlide,
		[FunctionClass(FUNCTION_CLASS.SLIDE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.RemoveOccurrencesOf, new string[] { "Slide " })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "R Bed" })]
		RearBedSlide,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Theater" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "THTR LTS" })]
		TheaterLights,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Utility Cabinet" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Utility CAB" })]
		UtilityCabinetLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Chase" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Chase" })]
		ChaseLight,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Floor" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Floor" })]
		FloorLights,
		[FunctionClass(FUNCTION_CLASS.LIGHT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "RTT" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "RTT" })]
		RttLight,
		[FunctionClass(FUNCTION_CLASS.LIFT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Upper Shades" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Upper Shades" })]
		UpperPowerShades,
		[FunctionClass(FUNCTION_CLASS.LIFT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Lower Shades" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Lower Shades" })]
		LowerPowerShades,
		[FunctionClass(FUNCTION_CLASS.LIFT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.LivingRoom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Living Room Shades" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "LVG RM Shades" })]
		LivingRoomPowerShades,
		[FunctionClass(FUNCTION_CLASS.LIFT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bedroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Bedroom Shades" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "BED Shades" })]
		BedroomPowerShades,
		[FunctionClass(FUNCTION_CLASS.LIFT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Bathroom Shades" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "BATH Shades" })]
		BathroomPowerShades,
		[FunctionClass(FUNCTION_CLASS.LIFT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bunk, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Bunk Shades" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Bunk Shades" })]
		BunkPowerShades,
		[FunctionClass(FUNCTION_CLASS.LIFT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Loft, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Loft Shades" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Loft Shades" })]
		LoftPowerShades,
		[FunctionClass(FUNCTION_CLASS.LIFT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Front Shades" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "F Shades" })]
		FrontPowerShades,
		[FunctionClass(FUNCTION_CLASS.LIFT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Rear Shades" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "R Shades" })]
		RearPowerShades,
		[FunctionClass(FUNCTION_CLASS.LIFT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Main Shades" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "MN Shades" })]
		MainPowerShades,
		[FunctionClass(FUNCTION_CLASS.LIFT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Garage, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Garage Shades" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Garage Shades" })]
		GaragePowerShades,
		[FunctionClass(FUNCTION_CLASS.LIFT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Door Side Shades" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "DS Shades" })]
		DoorSidePowerShades,
		[FunctionClass(FUNCTION_CLASS.LIFT)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Off Door Side Shades" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Off-DS Shades" })]
		OffDoorSidePowerShades,
		[FunctionClass(FUNCTION_CLASS.VALVE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Fresh Valve" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Fresh VLV" })]
		FreshTankValve,
		[FunctionClass(FUNCTION_CLASS.VALVE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Grey Valve" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Grey VLV" })]
		GreyTankValve,
		[FunctionClass(FUNCTION_CLASS.VALVE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Any, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Black Valve" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Blk VLV" })]
		BlackTankValve,
		[FunctionClass(FUNCTION_CLASS.VALVE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Front Bath Grey Valve" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "F Bath VLV" })]
		FrontBathGreyTankValve,
		[FunctionClass(FUNCTION_CLASS.VALVE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Front Bath Fresh Valve" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "F Bath VLV" })]
		FrontBathFreshTankValve,
		[FunctionClass(FUNCTION_CLASS.VALVE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Front, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Front Bath Black Valve" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "F Bath VLV" })]
		FrontBathBlackTankValve,
		[FunctionClass(FUNCTION_CLASS.VALVE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Rear Bath Grey Valve" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "R Bath VLV" })]
		RearBathGreyTankValve,
		[FunctionClass(FUNCTION_CLASS.VALVE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Rear Bath Fresh Valve" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "R Bath VLV" })]
		RearBathFreshTankValve,
		[FunctionClass(FUNCTION_CLASS.VALVE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Rear, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Rear Bath Black Valve" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "R Bath VLV" })]
		RearBathBlackTankValve,
		[FunctionClass(FUNCTION_CLASS.VALVE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Main Bath Grey Valve" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "MN Bath VLV" })]
		MainBathGreyTankValve,
		[FunctionClass(FUNCTION_CLASS.VALVE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Main Bath Fresh Valve" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "MN Bath VLV" })]
		MainBathFreshTankValve,
		[FunctionClass(FUNCTION_CLASS.VALVE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Main Bath Black Valve" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "MN Bath VLV" })]
		MainBathBlackTankValve,
		[FunctionClass(FUNCTION_CLASS.VALVE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Galley Grey Valve" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Galley VLV" })]
		GalleyBathGreyTankValve,
		[FunctionClass(FUNCTION_CLASS.VALVE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Galley Fresh Valve" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Galley VLV" })]
		GalleyBathFreshTankValve,
		[FunctionClass(FUNCTION_CLASS.VALVE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Galley Black Valve" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Galley VLV" })]
		GalleyBathBlackTankValve,
		[FunctionClass(FUNCTION_CLASS.VALVE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Kitchen Grey Valve" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Kitchen VLV" })]
		KitchenBathGreyTankValve,
		[FunctionClass(FUNCTION_CLASS.VALVE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Kitchen Fresh Valve" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Kitchen VLV" })]
		KitchenBathFreshTankValve,
		[FunctionClass(FUNCTION_CLASS.VALVE)]
		[FunctionNameDetail(FunctionNameDetailLocation.Inside, FunctionNameDetailPosition.Any, FunctionNameDetailRoom.Bathroom, FunctionNameDetailUse.Unknown)]
		[FunctionNameOverrideDisplayNameShort(OverrideDisplayNameOperation.Override, new string[] { "Kitchen Black Valve" })]
		[FunctionNameOverrideDisplayNameShortAbbreviated(OverrideDisplayNameOperation.Override, new string[] { "Kitchen VLV" })]
		KitchenBathBlackTankValve
	}
}
