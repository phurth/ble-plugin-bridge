using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Core.Types;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandGetDevicePidResponseCompleted : MyRvLinkCommandResponseSuccess
	{
		public const string LogTag = "MyRvLinkCommandGetDevicePidResponseCompleted";

		private const int MaxPidValueSize = 6;

		private const int MinPidValueSize = 0;

		protected override int MinExtendedDataLength => 0;

		public UInt48 PidValue => DecodePidValue();

		public MyRvLinkCommandGetDevicePidResponseCompleted(ushort clientCommandId, UInt48 pidValue)
			: base(clientCommandId, commandCompleted: true, EncodeExtendedData(pidValue))
		{
		}

		public MyRvLinkCommandGetDevicePidResponseCompleted(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public MyRvLinkCommandGetDevicePidResponseCompleted(MyRvLinkCommandResponseSuccess response)
			: base(response.ClientCommandId, response.IsCommandCompleted, response.ExtendedData)
		{
		}

		protected UInt48 DecodePidValue()
		{
			if (base.ExtendedData == null || base.ExtendedData.Count == 0)
			{
				return (byte)0;
			}
			UInt48 result = (byte)0;
			foreach (byte extendedDatum in base.ExtendedData)
			{
				result <<= 8;
				result |= (UInt48)extendedDatum;
			}
			return result;
		}

		private static IReadOnlyList<byte> EncodeExtendedData(UInt48 pidValue)
		{
			byte[] array = new byte[6];
			int num = array.SetValueBigEndianRemoveLeadingZeros(pidValue, 0, 6);
			if (num == 0)
			{
				return Array.Empty<byte>();
			}
			return new ArraySegment<byte>(array, 0, num);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(35, 3);
			defaultInterpolatedStringHandler.AppendLiteral("Command(0x");
			defaultInterpolatedStringHandler.AppendFormatted(base.ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(") Response ");
			defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkCommandGetDevicePidResponseCompleted");
			defaultInterpolatedStringHandler.AppendLiteral(" PID Value: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(PidValue, "X");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
