using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandGetDevicePidListResponseCompleted : MyRvLinkCommandResponseSuccess
	{
		private const string LogTag = "MyRvLinkCommandGetDevicePidListResponseCompleted";

		protected const int PidCountIndex = 0;

		protected override int MinExtendedDataLength => 1;

		public byte PidCount => base.ExtendedData[0];

		public MyRvLinkCommandGetDevicePidListResponseCompleted(ushort clientCommandId, byte pidCount)
			: base(clientCommandId, commandCompleted: true, EncodeExtendedData(pidCount))
		{
		}

		public MyRvLinkCommandGetDevicePidListResponseCompleted(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public MyRvLinkCommandGetDevicePidListResponseCompleted(MyRvLinkCommandResponseSuccess response)
			: base(response.ClientCommandId, response.IsCommandCompleted, response.ExtendedData)
		{
		}

		private static IReadOnlyList<byte> EncodeExtendedData(byte pidCount)
		{
			int num = 1;
			byte[] array = new byte[num];
			array[0] = pidCount;
			return new ArraySegment<byte>(array, 0, num);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(33, 3);
			defaultInterpolatedStringHandler.AppendLiteral("Command(0x");
			defaultInterpolatedStringHandler.AppendFormatted(base.ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(") Response ");
			defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkCommandGetDevicePidListResponseCompleted");
			defaultInterpolatedStringHandler.AppendLiteral(" PID Count: ");
			defaultInterpolatedStringHandler.AppendFormatted(PidCount);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
