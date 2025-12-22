using System.Runtime.CompilerServices;

namespace IDS.Core.IDS_CAN
{
	public struct CIRCUIT_ID
	{
		private uint Value;

		private CIRCUIT_ID(uint value)
		{
			Value = value;
		}

		public static implicit operator uint(CIRCUIT_ID circuit_id)
		{
			return circuit_id.Value;
		}

		public static implicit operator CIRCUIT_ID(uint value)
		{
			return new CIRCUIT_ID(value);
		}

		public override string ToString()
		{
			uint value = Value;
			if (value == 0)
			{
				return "none";
			}
			byte b = (byte)(value >> 24);
			byte b2 = (byte)(value >> 16);
			byte b3 = (byte)(value >> 8);
			byte b4 = (byte)value;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 4);
			defaultInterpolatedStringHandler.AppendFormatted(b.HexString());
			defaultInterpolatedStringHandler.AppendLiteral(":");
			defaultInterpolatedStringHandler.AppendFormatted(b2.HexString());
			defaultInterpolatedStringHandler.AppendLiteral(":");
			defaultInterpolatedStringHandler.AppendFormatted(b3.HexString());
			defaultInterpolatedStringHandler.AppendLiteral(":");
			defaultInterpolatedStringHandler.AppendFormatted(b4.HexString());
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
