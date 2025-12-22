using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandGetFirmwareInformationResponseSuccess : MyRvLinkCommandResponseSuccess
	{
		public const string LogTag = "MyRvLinkCommandGetFirmwareInformationResponseSuccess";

		public const int FirmwareInformationCodeIndexEx = 0;

		public const int FirmwareInformationStartIndexEx = 1;

		public const int ExtendedDataSizeMin = 2;

		protected override int MinExtendedDataLength => 2;

		public virtual FirmwareInformationCode FirmwareInformationCode => DecodeFirmwareInformationCode();

		public IReadOnlyList<byte> FirmwareInformationRaw => DecodeFirmwareInformation();

		public MyRvLinkCommandGetFirmwareInformationResponseSuccess(ushort clientCommandId, bool commandComplete, FirmwareInformationCode firmwareInformationCode, byte[] rawFirmwareInformation)
			: base(clientCommandId, commandComplete, EncodeExtendedData(firmwareInformationCode, rawFirmwareInformation))
		{
		}

		public MyRvLinkCommandGetFirmwareInformationResponseSuccess(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public MyRvLinkCommandGetFirmwareInformationResponseSuccess(MyRvLinkCommandResponseSuccess response)
			: base(response.ClientCommandId, response.IsCommandCompleted, response.ExtendedData)
		{
		}

		protected FirmwareInformationCode DecodeFirmwareInformationCode()
		{
			if (base.ExtendedData == null || base.ExtendedData.Count <= 0)
			{
				throw new MyRvLinkDecoderException("Expected extended data to contain FirmwareInformationCode, but there is no extended data.");
			}
			return (FirmwareInformationCode)base.ExtendedData[0];
		}

		protected IReadOnlyList<byte> DecodeFirmwareInformation()
		{
			if (base.ExtendedData == null || base.ExtendedData.Count <= 1)
			{
				throw new MyRvLinkDecoderException("Expected extended data to contain FirmwareInformationCode, but there is no extended data.");
			}
			return GetExtendedData(1, base.ExtendedDataLength - 1);
		}

		private static IReadOnlyList<byte> EncodeExtendedData(FirmwareInformationCode firmwareInformationCode, byte[] rawFirmwareInformation)
		{
			byte[] array = new byte[rawFirmwareInformation.Length + 1];
			array[0] = (byte)firmwareInformationCode;
			Buffer.BlockCopy(rawFirmwareInformation, 0, array, 1, rawFirmwareInformation.Length);
			return array;
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(21, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Command(0x");
			defaultInterpolatedStringHandler.AppendFormatted(base.ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(") Response ");
			defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkCommandGetFirmwareInformationResponseSuccess");
			StringBuilder stringBuilder = new StringBuilder(defaultInterpolatedStringHandler.ToStringAndClear());
			try
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder3 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(30, 2, stringBuilder2);
				handler.AppendLiteral(" FirmwareInformationCode: 0x");
				handler.AppendFormatted((byte)FirmwareInformationCode, "X2");
				handler.AppendLiteral("(");
				handler.AppendFormatted(FirmwareInformationCode);
				handler.AppendLiteral(")");
				stringBuilder3.Append(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder4 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(2, 1, stringBuilder2);
				handler.AppendLiteral(": ");
				handler.AppendFormatted(FirmwareInformationRaw.DebugDump(0, FirmwareInformationRaw.Count));
				stringBuilder4.Append(ref handler);
			}
			catch (Exception ex)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder5 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(33, 1, stringBuilder2);
				handler.AppendLiteral(" ERROR Trying to decode response ");
				handler.AppendFormatted(ex.Message);
				stringBuilder5.Append(ref handler);
			}
			return stringBuilder.ToString();
		}
	}
}
