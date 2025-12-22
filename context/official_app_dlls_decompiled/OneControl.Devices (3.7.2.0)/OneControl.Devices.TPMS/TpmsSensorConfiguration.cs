using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace OneControl.Devices.TPMS
{
	public readonly struct TpmsSensorConfiguration : IEquatable<TpmsSensorConfiguration>
	{
		public TpmsPositionalSensorId PositionalSensorId { get; }

		public int HighTempLimit { get; }

		public int RelativeTempLimit { get; }

		public uint HighPressureLimit { get; }

		public uint LowPressureLimit { get; }

		public uint SensorMac { get; }

		public byte SensorModel { get; }

		public TpmsSensorConfiguration(TpmsPositionalSensorId positionalSensorId, int highTempLimit, int relativeTempLimit, uint highPressureLimit, uint lowPressureLimit, uint sensorMac, byte sensorModel)
		{
			PositionalSensorId = positionalSensorId;
			HighTempLimit = highTempLimit;
			RelativeTempLimit = relativeTempLimit;
			HighPressureLimit = highPressureLimit;
			LowPressureLimit = lowPressureLimit;
			SensorMac = sensorMac;
			SensorModel = sensorModel;
		}

		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("TpmsSensorConfiguration");
			stringBuilder.Append(" { ");
			if (PrintMembers(stringBuilder))
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append('}');
			return stringBuilder.ToString();
		}

		[CompilerGenerated]
		private bool PrintMembers(StringBuilder builder)
		{
			builder.Append("PositionalSensorId = ");
			builder.Append(PositionalSensorId.ToString());
			builder.Append(", HighTempLimit = ");
			builder.Append(HighTempLimit.ToString());
			builder.Append(", RelativeTempLimit = ");
			builder.Append(RelativeTempLimit.ToString());
			builder.Append(", HighPressureLimit = ");
			builder.Append(HighPressureLimit.ToString());
			builder.Append(", LowPressureLimit = ");
			builder.Append(LowPressureLimit.ToString());
			builder.Append(", SensorMac = ");
			builder.Append(SensorMac.ToString());
			builder.Append(", SensorModel = ");
			builder.Append(SensorModel.ToString());
			return true;
		}

		[CompilerGenerated]
		public static bool operator !=(TpmsSensorConfiguration left, TpmsSensorConfiguration right)
		{
			return !(left == right);
		}

		[CompilerGenerated]
		public static bool operator ==(TpmsSensorConfiguration left, TpmsSensorConfiguration right)
		{
			return left.Equals(right);
		}

		[CompilerGenerated]
		public override int GetHashCode()
		{
			return (((((EqualityComparer<TpmsPositionalSensorId>.Default.GetHashCode(PositionalSensorId) * -1521134295 + EqualityComparer<int>.Default.GetHashCode(HighTempLimit)) * -1521134295 + EqualityComparer<int>.Default.GetHashCode(RelativeTempLimit)) * -1521134295 + EqualityComparer<uint>.Default.GetHashCode(HighPressureLimit)) * -1521134295 + EqualityComparer<uint>.Default.GetHashCode(LowPressureLimit)) * -1521134295 + EqualityComparer<uint>.Default.GetHashCode(SensorMac)) * -1521134295 + EqualityComparer<byte>.Default.GetHashCode(SensorModel);
		}

		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			if (obj is TpmsSensorConfiguration)
			{
				return Equals((TpmsSensorConfiguration)obj);
			}
			return false;
		}

		[CompilerGenerated]
		public bool Equals(TpmsSensorConfiguration other)
		{
			if (EqualityComparer<TpmsPositionalSensorId>.Default.Equals(PositionalSensorId, other.PositionalSensorId) && EqualityComparer<int>.Default.Equals(HighTempLimit, other.HighTempLimit) && EqualityComparer<int>.Default.Equals(RelativeTempLimit, other.RelativeTempLimit) && EqualityComparer<uint>.Default.Equals(HighPressureLimit, other.HighPressureLimit) && EqualityComparer<uint>.Default.Equals(LowPressureLimit, other.LowPressureLimit) && EqualityComparer<uint>.Default.Equals(SensorMac, other.SensorMac))
			{
				return EqualityComparer<byte>.Default.Equals(SensorModel, other.SensorModel);
			}
			return false;
		}
	}
}
