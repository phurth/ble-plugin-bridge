using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandActionSwitchResponseFailure : MyRvLinkCommandResponseFailure
	{
		public const string LogTag = "MyRvLinkCommandActionSwitchResponseFailure";

		public const int DeviceIdExtendedIndex = 0;

		public const int OptionalExtendedDataSize = 1;

		protected override int MinExtendedDataLength => 0;

		public bool HasDeviceId
		{
			get
			{
				if (base.ExtendedData != null)
				{
					return base.ExtendedData.Count != 0;
				}
				return false;
			}
		}

		public byte? DeviceId => DecodeDeviceId();

		public MyRvLinkCommandActionSwitchResponseFailure(ushort clientCommandId, MyRvLinkCommandResponseFailureCode failureCode)
			: base(clientCommandId, commandCompleted: true, failureCode)
		{
		}

		public MyRvLinkCommandActionSwitchResponseFailure(ushort clientCommandId, bool commandComplete, MyRvLinkCommandResponseFailureCode failureCode, byte deviceId)
			: base(clientCommandId, commandComplete, failureCode, EncodeExtendedData(deviceId))
		{
		}

		public MyRvLinkCommandActionSwitchResponseFailure(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public MyRvLinkCommandActionSwitchResponseFailure(MyRvLinkCommandResponseFailure response)
			: base(response.ClientCommandId, response.IsCommandCompleted, response.FailureCode, response.ExtendedData)
		{
		}

		protected byte? DecodeDeviceId()
		{
			if (base.ExtendedData == null || base.ExtendedData.Count == 0)
			{
				return null;
			}
			return base.ExtendedData[0];
		}

		private static IReadOnlyList<byte> EncodeExtendedData(byte deviceId)
		{
			return new ArraySegment<byte>(new byte[1] { deviceId }, 0, 1);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(39, 4);
			defaultInterpolatedStringHandler.AppendLiteral("Command(0x");
			defaultInterpolatedStringHandler.AppendFormatted(base.ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(") Response ");
			defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkCommandActionSwitchResponseFailure");
			defaultInterpolatedStringHandler.AppendLiteral(" Failure Code ");
			defaultInterpolatedStringHandler.AppendFormatted(base.FailureCode);
			defaultInterpolatedStringHandler.AppendLiteral("(0x");
			defaultInterpolatedStringHandler.AppendFormatted((int)base.FailureCode, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(")");
			StringBuilder stringBuilder = new StringBuilder(defaultInterpolatedStringHandler.ToStringAndClear());
			try
			{
				byte? deviceId = DeviceId;
				if (deviceId.HasValue)
				{
					StringBuilder stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder3 = stringBuilder2;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
					handler.AppendLiteral(" Device Id: 0x");
					handler.AppendFormatted(deviceId, "X2");
					stringBuilder3.Append(ref handler);
				}
			}
			catch (Exception ex)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder4 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(35, 1, stringBuilder2);
				handler.AppendLiteral(" Device Id: ERROR Getting DeviceId ");
				handler.AppendFormatted(ex.Message);
				stringBuilder4.Append(ref handler);
			}
			return stringBuilder.ToString();
		}
	}
}
