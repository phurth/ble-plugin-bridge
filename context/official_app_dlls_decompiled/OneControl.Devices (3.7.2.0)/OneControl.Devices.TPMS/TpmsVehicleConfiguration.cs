using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace OneControl.Devices.TPMS
{
	public readonly struct TpmsVehicleConfiguration : IEquatable<TpmsVehicleConfiguration>
	{
		public TpmsGroupId GroupId { get; }

		public TpmsVehicleTemplate VehicleTemplate { get; }

		public uint TiresPerAxle { get; }

		public bool HasSpare { get; }

		public byte NumberOfAxles { get; }

		public TpmsVehicleConfiguration(TpmsGroupId groupId, TpmsVehicleTemplate vehicleTemplate, uint tiresPerAxle, bool hasSpare, byte numberOfAxles)
		{
			GroupId = groupId;
			VehicleTemplate = vehicleTemplate;
			TiresPerAxle = tiresPerAxle;
			HasSpare = hasSpare;
			NumberOfAxles = numberOfAxles;
		}

		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("TpmsVehicleConfiguration");
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
			builder.Append("GroupId = ");
			builder.Append(GroupId.ToString());
			builder.Append(", VehicleTemplate = ");
			builder.Append(VehicleTemplate.ToString());
			builder.Append(", TiresPerAxle = ");
			builder.Append(TiresPerAxle.ToString());
			builder.Append(", HasSpare = ");
			builder.Append(HasSpare.ToString());
			builder.Append(", NumberOfAxles = ");
			builder.Append(NumberOfAxles.ToString());
			return true;
		}

		[CompilerGenerated]
		public static bool operator !=(TpmsVehicleConfiguration left, TpmsVehicleConfiguration right)
		{
			return !(left == right);
		}

		[CompilerGenerated]
		public static bool operator ==(TpmsVehicleConfiguration left, TpmsVehicleConfiguration right)
		{
			return left.Equals(right);
		}

		[CompilerGenerated]
		public override int GetHashCode()
		{
			return (((EqualityComparer<TpmsGroupId>.Default.GetHashCode(GroupId) * -1521134295 + EqualityComparer<TpmsVehicleTemplate>.Default.GetHashCode(VehicleTemplate)) * -1521134295 + EqualityComparer<uint>.Default.GetHashCode(TiresPerAxle)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(HasSpare)) * -1521134295 + EqualityComparer<byte>.Default.GetHashCode(NumberOfAxles);
		}

		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			if (obj is TpmsVehicleConfiguration)
			{
				return Equals((TpmsVehicleConfiguration)obj);
			}
			return false;
		}

		[CompilerGenerated]
		public bool Equals(TpmsVehicleConfiguration other)
		{
			if (EqualityComparer<TpmsGroupId>.Default.Equals(GroupId, other.GroupId) && EqualityComparer<TpmsVehicleTemplate>.Default.Equals(VehicleTemplate, other.VehicleTemplate) && EqualityComparer<uint>.Default.Equals(TiresPerAxle, other.TiresPerAxle) && EqualityComparer<bool>.Default.Equals(HasSpare, other.HasSpare))
			{
				return EqualityComparer<byte>.Default.Equals(NumberOfAxles, other.NumberOfAxles);
			}
			return false;
		}
	}
}
