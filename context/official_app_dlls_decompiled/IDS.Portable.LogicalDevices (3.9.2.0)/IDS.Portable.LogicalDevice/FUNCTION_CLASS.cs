namespace IDS.Portable.LogicalDevice
{
	public enum FUNCTION_CLASS
	{
		[FunctionClassDescription("UNKNOWN")]
		[FunctionClassDevices(new byte[] { 0 })]
		UNKNOWN,
		[FunctionClassDescription("Miscellaneous")]
		[FunctionClassDevices(new byte[] { 0 })]
		MISCELLANEOUS,
		[FunctionClassDescription("Awning")]
		[FunctionClassDevices(new byte[] { 8, 6, 33 })]
		[FunctionClassDefaultDevices(new byte[] { 6, 33 })]
		AWNING,
		[FunctionClassDescription("Lock")]
		[FunctionClassDevices(new byte[] { 51, 8, 6, 33 })]
		[FunctionClassExclusiveDevices(new byte[] { 51 })]
		LOCK,
		[FunctionClassDescription("Water Heater")]
		[FunctionClassDevices(new byte[] { 8, 3, 30 })]
		WATER_HEATER,
		[FunctionClassDescription("Generator")]
		[FunctionClassDevices(new byte[] { 8, 6, 33, 24 })]
		[FunctionClassExclusiveDevices(new byte[] { 24 })]
		GENERATOR,
		[FunctionClassDescription("Landing Gear")]
		[FunctionClassDevices(new byte[] { 8, 6, 33 })]
		LANDING_GEAR,
		[FunctionClassDescription("Leveler")]
		[FunctionClassDevices(new byte[] { 7, 17, 40, 56 })]
		[FunctionClassExclusiveDevices(new byte[] { 7, 17, 40, 56 })]
		LEVELER,
		[FunctionClassDescription("Light")]
		[FunctionClassDevices(new byte[] { 8, 3, 30, 13, 20 })]
		[FunctionClassDefaultDevices(new byte[] { 3, 30 })]
		[FunctionClassExclusiveDevices(new byte[] { 13, 20 })]
		LIGHT,
		[FunctionClassDescription("Pump")]
		[FunctionClassDevices(new byte[] { 8, 3, 30 })]
		PUMP,
		[FunctionClassDescription("Slide")]
		[FunctionClassDevices(new byte[] { 8, 6, 33 })]
		SLIDE,
		[FunctionClassDescription("Tank")]
		[FunctionClassDevices(new byte[] { 10 })]
		[FunctionClassDefaultDevices(new byte[] { 10 })]
		TANK,
		[FunctionClassDescription("Lift")]
		[FunctionClassDevices(new byte[] { 8, 6, 33 })]
		LIFT,
		[FunctionClassDescription("Vent")]
		[FunctionClassDevices(new byte[] { 8, 3, 30 })]
		VENT,
		[FunctionClassDescription("Vent Cover")]
		[FunctionClassDevices(new byte[] { 8, 6, 33 })]
		VENT_COVER,
		[FunctionClassDescription("Tank Heater")]
		[FunctionClassDevices(new byte[] { 8, 3, 30 })]
		TANK_HEATER,
		[FunctionClassDescription("Leveler")]
		[FunctionClassDevices(new byte[] { 11 })]
		[FunctionClassExclusiveDevices(new byte[] { 11 })]
		LEVELER_2,
		[FunctionClassDescription("Hour Meter")]
		[FunctionClassDevices(new byte[] { 12 })]
		[FunctionClassExclusiveDevices(new byte[] { 12 })]
		HOUR_METER,
		[FunctionClassDescription("Clock")]
		[FunctionClassDevices(new byte[] { 14 })]
		[FunctionClassExclusiveDevices(new byte[] { 14 })]
		REAL_TIME_CLOCK,
		[FunctionClassDescription("Infrared Remote Control")]
		[FunctionClassDevices(new byte[] { 15 })]
		[FunctionClassExclusiveDevices(new byte[] { 15 })]
		IR_REMOTE_CONTROL,
		[FunctionClassDescription("HVAC Control")]
		[FunctionClassDevices(new byte[] { 16 })]
		[FunctionClassExclusiveDevices(new byte[] { 16 })]
		HVAC_CONTROL,
		[FunctionClassDescription("Valve")]
		[FunctionClassDevices(new byte[] { 8, 3, 30 })]
		VALVE,
		[FunctionClassDescription("Network Bridge")]
		[FunctionClassDevices(new byte[] { 18, 29, 44, 36 })]
		[FunctionClassExclusiveDevices(new byte[] { 18, 29, 44, 36 })]
		NETWORK_BRIDGE,
		[FunctionClassDescription("In Transit Power Disconnect")]
		[FunctionClassDevices(new byte[] { 19 })]
		[FunctionClassExclusiveDevices(new byte[] { 19 })]
		IPDM,
		[FunctionClassDescription("Stabilizer")]
		[FunctionClassDevices(new byte[] { 8, 6, 33 })]
		STABILIZER,
		[FunctionClassDescription("Temperature Sensor")]
		[FunctionClassDevices(new byte[] { 25 })]
		[FunctionClassExclusiveDevices(new byte[] { 25 })]
		TEMPERATURE_SENSOR,
		[FunctionClassDescription("Power")]
		[FunctionClassDevices(new byte[] { 26, 27, 28, 46, 49, 3, 30 })]
		[FunctionClassExclusiveDevices(new byte[] { 26, 27, 28, 46, 49 })]
		POWER,
		[FunctionClassDescription("Door")]
		[FunctionClassDevices(new byte[] { 8, 6, 33 })]
		DOOR,
		[FunctionClassDescription("Fan")]
		[FunctionClassDevices(new byte[] { 8, 3, 30, 37 })]
		FAN,
		[FunctionClassDescription("Router")]
		[FunctionClassDevices(new byte[] { 41, 3, 30 })]
		[FunctionClassExclusiveDevices(new byte[] { 41 })]
		Router,
		[FunctionClassDescription("Tire Monitor")]
		[FunctionClassDevices(new byte[] { 42 })]
		[FunctionClassExclusiveDevices(new byte[] { 42 })]
		TireMonitor,
		[FunctionClassDescription("Monitor Panel")]
		[FunctionClassDevices(new byte[] { 43, 57, 21 })]
		[FunctionClassExclusiveDevices(new byte[] { 43, 57, 21 })]
		MonitorPanel,
		[FunctionClassDescription("Camera")]
		[FunctionClassDevices(new byte[] { 45 })]
		[FunctionClassExclusiveDevices(new byte[] { 45 })]
		Camera,
		[FunctionClassDescription("Awning Sensor")]
		[FunctionClassDevices(new byte[] { 47 })]
		[FunctionClassExclusiveDevices(new byte[] { 47 })]
		AwningSensor,
		[FunctionClassDescription("Safety Systems")]
		[FunctionClassDevices(new byte[] { 48, 58 })]
		[FunctionClassExclusiveDevices(new byte[] { 48, 58 })]
		SafetySystems,
		[FunctionClassDescription("Liquid Propane")]
		[FunctionClassDevices(new byte[] { 10 })]
		LiquidPropane,
		[FunctionClassDescription("Text")]
		[FunctionClassDevices(new byte[] { 1 })]
		Text
	}
}
