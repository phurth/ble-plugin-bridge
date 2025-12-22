using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Exceptions;
using ids.portable.ble.Platforms.Shared.ScanResults;
using IDS.Portable.Common.Extensions;
using Plugin.BLE.Abstractions;

namespace OneControl.Direct.IdsCanAccessoryBle.Mopeka
{
	public class MopekaScanResult : BleScanResult
	{
		private const string LogTag = "MopekaScanResult";

		public int ManufacturerId { get; private set; }

		public int HardwareId { get; private set; }

		public float BatteryVoltage { get; private set; }

		public float BatteryPercentage { get; private set; }

		public bool IsSyncPressed { get; private set; }

		public int TemperatureInCelsius { get; private set; }

		public int RawTankLevel { get; private set; }

		public int MeasurementQuality { get; private set; }

		public MAC ShortMAC { get; private set; }

		public byte RawXAcceleration { get; private set; }

		public byte RawYAcceleration { get; private set; }

		public float XAccelerationInG { get; private set; }

		public float YAccelerationInG { get; private set; }

		public MopekaScanResult(Guid deviceId, string defaultDeviceName, int rssi, IEnumerable<AdvertisementRecord> advertisementRecords)
			: base(deviceId, defaultDeviceName, rssi, advertisementRecords)
		{
		}

		protected override void RawManufacturerSpecificDataUpdated(byte[] manufacturerSpecificData)
		{
			try
			{
				if (manufacturerSpecificData.Length != 12)
				{
					throw new ArgumentException();
				}
				ManufacturerId = manufacturerSpecificData.GetValueUInt16(0, ArrayExtension.Endian.Little);
				HardwareId = manufacturerSpecificData[2];
				BatteryVoltage = (float)(manufacturerSpecificData[3] & 0x7F) / 32f;
				BatteryPercentage = ConvertBatteryVoltageToPercentage(BatteryVoltage);
				IsSyncPressed = manufacturerSpecificData[4] >> 7 == 1;
				TemperatureInCelsius = (manufacturerSpecificData[4] & 0x7F) - 40;
				RawTankLevel = manufacturerSpecificData.GetValueUInt16(5, ArrayExtension.Endian.Little) & 0x3FFF;
				MeasurementQuality = (manufacturerSpecificData.GetValueUInt16(5, ArrayExtension.Endian.Little) & 0xC000) >> 14;
				byte[] buffer = new byte[6]
				{
					0,
					0,
					0,
					manufacturerSpecificData[9],
					manufacturerSpecificData[8],
					manufacturerSpecificData[7]
				};
				ShortMAC = new MAC(buffer);
				RawXAcceleration = manufacturerSpecificData[10];
				RawYAcceleration = manufacturerSpecificData[11];
				XAccelerationInG = (float)(-(sbyte)manufacturerSpecificData[10]) / 1024f;
				YAccelerationInG = (float)(sbyte)manufacturerSpecificData[11] / 1024f;
			}
			catch (Exception innerException)
			{
				throw new BleScannerScanResultParseException(innerException);
			}
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(152, 11);
			defaultInterpolatedStringHandler.AppendFormatted(base.ToString());
			defaultInterpolatedStringHandler.AppendLiteral(" ManufacturerId: ");
			defaultInterpolatedStringHandler.AppendFormatted(ManufacturerId);
			defaultInterpolatedStringHandler.AppendLiteral(" HardwareId: ");
			defaultInterpolatedStringHandler.AppendFormatted(HardwareId);
			defaultInterpolatedStringHandler.AppendLiteral(" Battery (V): ");
			defaultInterpolatedStringHandler.AppendFormatted(BatteryVoltage);
			defaultInterpolatedStringHandler.AppendLiteral(" IsSyncPressed: ");
			defaultInterpolatedStringHandler.AppendFormatted(IsSyncPressed);
			defaultInterpolatedStringHandler.AppendLiteral(" Temperature (°C): ");
			defaultInterpolatedStringHandler.AppendFormatted(TemperatureInCelsius);
			defaultInterpolatedStringHandler.AppendLiteral(" TankLevel (raw): ");
			defaultInterpolatedStringHandler.AppendFormatted(RawTankLevel);
			defaultInterpolatedStringHandler.AppendLiteral(" MeasurementQuality: ");
			defaultInterpolatedStringHandler.AppendFormatted(MeasurementQuality);
			defaultInterpolatedStringHandler.AppendLiteral(" ShortMAC: ");
			defaultInterpolatedStringHandler.AppendFormatted(ShortMAC);
			defaultInterpolatedStringHandler.AppendLiteral(" Acceleration (G): (");
			defaultInterpolatedStringHandler.AppendFormatted(XAccelerationInG);
			defaultInterpolatedStringHandler.AppendLiteral(", ");
			defaultInterpolatedStringHandler.AppendFormatted(YAccelerationInG);
			defaultInterpolatedStringHandler.AppendLiteral(")");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		public static double ConvertRawTankLevelToMillimetersForLPG(double rawTankLevel, double temperatureInCelcius)
		{
			if (rawTankLevel < 0.0 || rawTankLevel > 16383.0)
			{
				throw new ArgumentException("Tank level must be between 0 and 0x3FFF", "rawTankLevel");
			}
			if (temperatureInCelcius < -40.0 || temperatureInCelcius > 87.0)
			{
				throw new ArgumentException("Temperature must be between -40 and 87", "temperatureInCelcius");
			}
			double[] array = new double[3] { 0.573045, -0.002822, -5.35E-06 };
			double num = temperatureInCelcius + 40.0;
			return rawTankLevel * (array[0] + array[1] * num + array[2] * Math.Pow(num, 2.0));
		}

		public static float ConvertBatteryVoltageToPercentage(float voltage)
		{
			return MathCommon.Clamp((voltage - 2.2f) / 0.65f * 100f, 0f, 100f);
		}
	}
}
