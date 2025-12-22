using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandActionSwitchResponseSuccess : MyRvLinkCommandResponseSuccess
	{
		public const string LogTag = "MyRvLinkCommandActionSwitchResponseSuccess";

		public const int DeviceIdExtendedIndex = 0;

		public const int ExtendedDataSize = 1;

		protected override int MinExtendedDataLength => 1;

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

		public byte DeviceId => DecodeDeviceId();

		public MyRvLinkCommandActionSwitchResponseSuccess(ushort clientCommandId, bool commandComplete, byte deviceId)
			: base(clientCommandId, commandComplete, EncodeExtendedData(deviceId))
		{
		}

		public MyRvLinkCommandActionSwitchResponseSuccess(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public MyRvLinkCommandActionSwitchResponseSuccess(MyRvLinkCommandResponseSuccess response)
			: base(response.ClientCommandId, response.IsCommandCompleted, response.ExtendedData)
		{
		}

		protected byte DecodeDeviceId()
		{
			if (base.ExtendedData == null || base.ExtendedData.Count == 0)
			{
				throw new MyRvLinkDecoderException("Expected extended data to contain DeviceId, but there is no extended data.");
			}
			return base.ExtendedData[0];
		}

		private static IReadOnlyList<byte> EncodeExtendedData(byte deviceId)
		{
			return new ArraySegment<byte>(new byte[1] { deviceId }, 0, 1);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(21, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Command(0x");
			defaultInterpolatedStringHandler.AppendFormatted(base.ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(") Response ");
			defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkCommandActionSwitchResponseSuccess");
			StringBuilder stringBuilder = new StringBuilder(defaultInterpolatedStringHandler.ToStringAndClear());
			try
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder3 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
				handler.AppendLiteral(" Device Id: 0x");
				handler.AppendFormatted(DeviceId, "X2");
				stringBuilder3.Append(ref handler);
				if (base.IsCommandCompleted)
				{
					stringBuilder.Append(" COMPLETED");
				}
			}
			catch (Exception ex)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder4 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(37, 1, stringBuilder2);
				handler.AppendLiteral(" Device Id: ERROR Trying to DeviceId ");
				handler.AppendFormatted(ex.Message);
				stringBuilder4.Append(ref handler);
			}
			return stringBuilder.ToString();
		}
	}
}
