using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkRvStatus : MyRvLinkEvent<MyRvLinkRvStatus>
	{
		[Flags]
		private enum MyRvLinkRvStatusFeature
		{
			None = 0,
			VoltageAvailable = 1,
			ExternalTemperatureAvailable = 2
		}

		private const ushort InvalidVoltageFixedPoint = ushort.MaxValue;

		private const ushort InvalidTemperatureFixedPoint = 32767;

		private const int MaxPayloadLength = 6;

		private const int AverageVoltageStartIndex = 1;

		private const int ExternalTemperatureCelsiusStartIndex = 3;

		private const int FeatureIndex = 5;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.RvStatus;

		protected override int MinPayloadLength => 6;

		protected override byte[] _rawData { get; }

		public float BatteryVoltage => _rawData.GetFixedPointFloat(1u, FixedPointType.UnsignedBigEndian8x8);

		public float ExternalTemperatureCelsius => _rawData.GetFixedPointFloat(3u, FixedPointType.SignedBigEndian8x8);

		public bool IsBatteryVoltageAvailable => (_rawData[5] & 1) != 0;

		public bool IsBatteryVoltageValid
		{
			get
			{
				if (IsBatteryVoltageAvailable)
				{
					return _rawData.GetValueUInt16(1) != ushort.MaxValue;
				}
				return false;
			}
		}

		public bool IsExternalTemperatureAvailable => (_rawData[5] & 2) != 0;

		public bool IsExternalTemperatureValid
		{
			get
			{
				if (IsExternalTemperatureAvailable)
				{
					return _rawData.GetValueUInt16(3) != 32767;
				}
				return false;
			}
		}

		public MyRvLinkRvStatus(float? batteryVoltage, float? externalTemperatureCelsius, bool cloudConnectedToLan, bool cloudConnectedToWan, bool cloudGatewayAvailable)
		{
			_rawData = new byte[6];
			_rawData[0] = (byte)EventType;
			if (!batteryVoltage.HasValue)
			{
				_rawData.SetValueUInt16(ushort.MaxValue, 1);
			}
			else
			{
				_rawData.SetFixedPointFloat(batteryVoltage.Value, 1u, FixedPointType.UnsignedBigEndian8x8);
			}
			if (!externalTemperatureCelsius.HasValue)
			{
				_rawData.SetValueUInt16(32767, 3);
			}
			else
			{
				_rawData.SetFixedPointFloat(externalTemperatureCelsius.Value, 3u, FixedPointType.UnsignedBigEndian8x8);
			}
			MyRvLinkRvStatusFeature myRvLinkRvStatusFeature = MyRvLinkRvStatusFeature.None;
			if (batteryVoltage.HasValue)
			{
				myRvLinkRvStatusFeature = myRvLinkRvStatusFeature.SetFlag(MyRvLinkRvStatusFeature.VoltageAvailable);
			}
			if (externalTemperatureCelsius.HasValue)
			{
				myRvLinkRvStatusFeature = myRvLinkRvStatusFeature.SetFlag(MyRvLinkRvStatusFeature.ExternalTemperatureAvailable);
			}
			_rawData[5] = (byte)myRvLinkRvStatusFeature;
		}

		protected MyRvLinkRvStatus(IReadOnlyList<byte> rawData)
		{
			if (rawData == null)
			{
				throw new ArgumentNullException("rawData");
			}
			if (rawData.Count < MinPayloadLength)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(EventType);
				defaultInterpolatedStringHandler.AppendLiteral(" received less then ");
				defaultInterpolatedStringHandler.AppendFormatted(MinPayloadLength);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (rawData.Count > 6)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(EventType);
				defaultInterpolatedStringHandler.AppendLiteral(" received more then ");
				defaultInterpolatedStringHandler.AppendFormatted(6);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (EventType != (MyRvLinkEventType)rawData[0])
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(EventType);
				defaultInterpolatedStringHandler.AppendLiteral(" event type doesn't match ");
				defaultInterpolatedStringHandler.AppendFormatted(EventType);
				defaultInterpolatedStringHandler.AppendLiteral(": ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			_rawData = rawData.ToNewArray(0, rawData.Count);
		}

		public static MyRvLinkRvStatus Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkRvStatus(rawData);
		}

		public float? GetVoltage()
		{
			if (IsBatteryVoltageAvailable)
			{
				return BatteryVoltage;
			}
			return null;
		}

		public float? GetTemperature()
		{
			if (IsExternalTemperatureValid)
			{
				return ExternalTemperatureCelsius;
			}
			return null;
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(1, 2);
			defaultInterpolatedStringHandler.AppendFormatted(EventType);
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendFormatted(_rawData.DebugDump());
			StringBuilder stringBuilder = new StringBuilder(defaultInterpolatedStringHandler.ToStringAndClear());
			string value;
			if (!IsBatteryVoltageValid)
			{
				value = Environment.NewLine + "    Average Battery Voltage: --.-- V";
			}
			else
			{
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(31, 2);
				defaultInterpolatedStringHandler.AppendFormatted(Environment.NewLine);
				defaultInterpolatedStringHandler.AppendLiteral("    Average Battery Voltage: ");
				defaultInterpolatedStringHandler.AppendFormatted(BatteryVoltage, "F2");
				defaultInterpolatedStringHandler.AppendLiteral(" V");
				value = defaultInterpolatedStringHandler.ToStringAndClear();
			}
			stringBuilder.Append(value);
			string value2;
			if (!IsExternalTemperatureValid)
			{
				value2 = Environment.NewLine + "    External Temperature: --.-- °C";
			}
			else
			{
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(29, 2);
				defaultInterpolatedStringHandler.AppendFormatted(Environment.NewLine);
				defaultInterpolatedStringHandler.AppendLiteral("    External Temperature: ");
				defaultInterpolatedStringHandler.AppendFormatted(ExternalTemperatureCelsius, "F2");
				defaultInterpolatedStringHandler.AppendLiteral(" °C");
				value2 = defaultInterpolatedStringHandler.ToStringAndClear();
			}
			stringBuilder.Append(value2);
			return stringBuilder.ToString();
		}
	}
}
