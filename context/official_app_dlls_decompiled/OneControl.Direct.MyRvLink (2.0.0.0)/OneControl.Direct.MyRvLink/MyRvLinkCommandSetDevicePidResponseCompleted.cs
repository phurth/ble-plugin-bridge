using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Core.Types;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandSetDevicePidResponseCompleted : MyRvLinkCommandResponseSuccess
	{
		public const string LogTag = "MyRvLinkCommandSetDevicePidResponseCompleted";

		private const int RequiredAddressBytes = 2;

		private const int MaxValueBytes = 6;

		private const int MinExtendedDataSize = 2;

		private const int MaxExtendedDataSize = 8;

		private const int ExtendedDataValueStartIndex = 0;

		protected override int MinExtendedDataLength => 2;

		public UInt48 PidValue => DecodePidValue();

		public MyRvLinkCommandSetDevicePidResponseCompleted(ushort clientCommandId, ushort pidAddress, uint pidValue)
			: base(clientCommandId, commandCompleted: true, EncodeExtendedData(pidAddress, pidValue))
		{
		}

		public MyRvLinkCommandSetDevicePidResponseCompleted(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public MyRvLinkCommandSetDevicePidResponseCompleted(MyRvLinkCommandResponseSuccess response)
			: base(response.ClientCommandId, response.IsCommandCompleted, response.ExtendedData)
		{
		}

		protected UInt48 DecodePidValue()
		{
			int num = MathCommon.Clamp(base.ExtendedData.Count, 0, 6);
			if (num == 0)
			{
				return (byte)0;
			}
			uint num2 = 0u;
			foreach (byte extendedDatum in GetExtendedData(0, num))
			{
				num2 <<= 8;
				num2 |= extendedDatum;
			}
			return num2;
		}

		private static IReadOnlyList<byte> EncodeExtendedData(ushort address, uint pidValue)
		{
			byte[] array = new byte[8];
			int num = array.SetValueBigEndianRemoveLeadingZeros(pidValue, 0, 6);
			return new ArraySegment<byte>(array, 0, 2 + num);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(35, 3);
			defaultInterpolatedStringHandler.AppendLiteral("Command(0x");
			defaultInterpolatedStringHandler.AppendFormatted(base.ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(") Response ");
			defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkCommandSetDevicePidResponseCompleted");
			defaultInterpolatedStringHandler.AppendLiteral(" PID Value: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(PidValue, "X");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
