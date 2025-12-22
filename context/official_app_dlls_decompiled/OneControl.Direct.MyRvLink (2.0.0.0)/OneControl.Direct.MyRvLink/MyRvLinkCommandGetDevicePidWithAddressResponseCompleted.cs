using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandGetDevicePidWithAddressResponseCompleted : MyRvLinkCommandResponseSuccess
	{
		public const string LogTag = "MyRvLinkCommandGetDevicePidWithAddressResponseCompleted";

		private const int RequiredAddressBytes = 2;

		private const int MaxValueBytes = 4;

		private const int MinExtendedDataSize = 2;

		private const int MaxExtendedDataSize = 6;

		private const int ExtendedDataAddressStartIndex = 0;

		private const int ExtendedDataValueStartIndex = 2;

		protected override int MinExtendedDataLength => 2;

		public uint PidValue => DecodePidValue();

		public uint PidAddress => DecodePidAddress();

		public MyRvLinkCommandGetDevicePidWithAddressResponseCompleted(ushort clientCommandId, ushort pidAddress, uint pidValue)
			: base(clientCommandId, commandCompleted: true, EncodeExtendedData(pidAddress, pidValue))
		{
		}

		public MyRvLinkCommandGetDevicePidWithAddressResponseCompleted(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public MyRvLinkCommandGetDevicePidWithAddressResponseCompleted(MyRvLinkCommandResponseSuccess response)
			: base(response.ClientCommandId, response.IsCommandCompleted, response.ExtendedData)
		{
		}

		protected uint DecodePidValue()
		{
			int num = MathCommon.Clamp(base.ExtendedData.Count - 2, 0, 4);
			if (num == 0)
			{
				return 0u;
			}
			uint num2 = 0u;
			foreach (byte extendedDatum in GetExtendedData(2, num))
			{
				num2 <<= 8;
				num2 |= extendedDatum;
			}
			return num2;
		}

		protected uint DecodePidAddress()
		{
			IReadOnlyList<byte> extendedData = base.ExtendedData;
			if (extendedData.Count < 2)
			{
				return 0u;
			}
			return extendedData.GetValueUInt16(0);
		}

		private static IReadOnlyList<byte> EncodeExtendedData(ushort address, uint pidValue)
		{
			byte[] array = new byte[6];
			array.SetValueUInt16(address, 0);
			int num = array.SetValueBigEndianRemoveLeadingZeros(pidValue, 2, 4);
			return new ArraySegment<byte>(array, 0, 2 + num);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(35, 3);
			defaultInterpolatedStringHandler.AppendLiteral("Command(0x");
			defaultInterpolatedStringHandler.AppendFormatted(base.ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(") Response ");
			defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkCommandGetDevicePidWithAddressResponseCompleted");
			defaultInterpolatedStringHandler.AppendLiteral(" PID Value: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(PidValue, "X");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
