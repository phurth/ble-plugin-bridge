using System.ComponentModel;

namespace IDS.Portable.LogicalDevice
{
	public enum PidCategory
	{
		[Description("Unknown")]
		Unknown,
		[Description("Manufacturing")]
		Manufacturing,
		[Description("Device")]
		Device,
		[Description("CAN Statistics")]
		StatisticsCan,
		[Description("Serial Statistics")]
		StatisticsSerial,
		[Description("Ethernet Statistics")]
		StatisticsEthernet,
		[Description("WIFI Statistics")]
		StatisticsWifi,
		[Description("BLE Statistics")]
		StatisticsBle,
		[Description("RVC Statistics")]
		StatisticsRvc,
		[Description("Battery")]
		Battery,
		[Description("Leveler")]
		Leveler,
		[Description("Fuse")]
		Fuse,
		[Description("Maintenance")]
		Maintenance,
		[Description("RTC")]
		Rtc,
		[Description("TCP/IP")]
		TcpIp,
		[Description("BLE")]
		Ble,
		[Description("RVC")]
		Rvc,
		[Description("Light")]
		Light,
		[Description("Generator")]
		Generator,
		[Description("Motor")]
		Motor,
		[Description("Cloud")]
		Cloud,
		[Description("MMYF")]
		MakeModelYearFloorplan,
		[Description("Misc")]
		Misc,
		[Description("Monitor Panel")]
		MonitorPanel,
		[Description("Power Management")]
		PowerManagement,
		[Description("Address With PID")]
		WithAddress,
		[Description("Accessory")]
		Accessory,
		[Description("Electronic Brake Control")]
		ElectronicBrakeControl,
		[Description("Awning Sensor")]
		AwningSensor,
		[Description("Accessory Gateway")]
		AccessoryGateway
	}
}
