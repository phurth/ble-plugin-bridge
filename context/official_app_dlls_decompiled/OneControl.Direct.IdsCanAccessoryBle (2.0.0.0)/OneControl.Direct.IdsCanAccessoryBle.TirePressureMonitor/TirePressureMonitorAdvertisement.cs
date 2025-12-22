using System.Runtime.CompilerServices;
using IDS.Portable.Devices.TPMS;

namespace OneControl.Direct.IdsCanAccessoryBle.TirePressureMonitor
{
	public class TirePressureMonitorAdvertisement : ITirePressureMonitorAdvertisement
	{
		private readonly string LogTag = "TirePressureMonitorAdvertisement";

		private readonly BleTirePressureMonitorScanResultManufacturerSpecificData? _parsedManufacturerSpecificData;

		public short Sequence => _parsedManufacturerSpecificData?.Sequence ?? 0;

		public byte TotalFaults => _parsedManufacturerSpecificData?.TotalFaults ?? 0;

		public byte Faults => _parsedManufacturerSpecificData?.Faults ?? 0;

		public TirePressureMonitorFaultType Fault => _parsedManufacturerSpecificData?.Fault ?? TirePressureMonitorFaultType.NoFault;

		public bool HasBatteryInformation => _parsedManufacturerSpecificData?.HasBatteryInformation ?? false;

		public bool RepeaterVoltageErrorFault => _parsedManufacturerSpecificData?.RepeaterVoltageErrorFault ?? false;

		public bool RepeaterBatteryLowFault => _parsedManufacturerSpecificData?.RepeaterBatteryLowFault ?? false;

		public int? RepeaterVoltagePercent => _parsedManufacturerSpecificData?.RepeaterVoltagePercent;

		public RepeaterPowerSource RepeaterPowerSource => _parsedManufacturerSpecificData?.RepeaterPowerSource ?? RepeaterPowerSource.Unknown;

		public bool RepeaterBatteryCharging => _parsedManufacturerSpecificData?.RepeaterBatteryCharging ?? false;

		private TirePressureMonitorFaultType CurrentFaultType { get; set; }

		public TirePressureMonitorAdvertisement(BleTirePressureMonitorScanResultManufacturerSpecificData parsedManufacturerSpecificData)
		{
			_parsedManufacturerSpecificData = parsedManufacturerSpecificData;
			CurrentFaultType = Fault;
		}

		public static bool TryMakeTpmsAdvertisement(byte[] data, out TirePressureMonitorAdvertisement? advertisement)
		{
			try
			{
				BleTirePressureMonitorScanResultManufacturerSpecificData parsedManufacturerSpecificData = new BleTirePressureMonitorScanResultManufacturerSpecificData(data);
				advertisement = new TirePressureMonitorAdvertisement(parsedManufacturerSpecificData);
				return true;
			}
			catch
			{
				advertisement = null;
				return false;
			}
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(46, 2);
			defaultInterpolatedStringHandler.AppendLiteral("TPMS Advertisement - Faults : ");
			defaultInterpolatedStringHandler.AppendFormatted(Faults);
			defaultInterpolatedStringHandler.AppendLiteral(", Total Faults: ");
			defaultInterpolatedStringHandler.AppendFormatted(TotalFaults);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
