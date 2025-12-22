using System;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandGetFirmwareInformationResponseSuccessCpu : MyRvLinkCommandGetFirmwareInformationResponseSuccess
	{
		public const int CpuModelIndexEx = 1;

		public const int CpuFeaturesIndexEx = 2;

		public const int CpuCoresIndexEx = 6;

		public const int CpuRevisionIndexEx = 7;

		public const int XtalFrequencyIndexEx = 8;

		public const int CpuFrequencyIndexEx = 9;

		public override FirmwareInformationCode FirmwareInformationCode => FirmwareInformationCode.Cpu;

		protected override int MinExtendedDataLength => 15;

		public byte CpuModelRaw => base.ExtendedData[1];

		public uint CpuFeaturesRaw => base.ExtendedData.GetValueUInt32(2);

		public byte CpuCores => base.ExtendedData[6];

		public byte CpuRevision => base.ExtendedData[7];

		public byte XtalFrequency => base.ExtendedData[8];

		public ushort CpuFrequency => base.ExtendedData.GetValueUInt16(9);

		public MyRvLinkCommandGetFirmwareInformationResponseSuccessCpu(MyRvLinkCommandGetFirmwareInformationResponseSuccess response)
			: base(response)
		{
			if (response.FirmwareInformationCode != FirmwareInformationCode)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(23, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Expected ");
				defaultInterpolatedStringHandler.AppendFormatted(FirmwareInformationCode);
				defaultInterpolatedStringHandler.AppendLiteral(" but received ");
				defaultInterpolatedStringHandler.AppendFormatted(response.FirmwareInformationCode);
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
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
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(31, 2, stringBuilder2);
				handler.AppendLiteral(" FirmwareInformationCode: 0x");
				handler.AppendFormatted((byte)FirmwareInformationCode, "X2");
				handler.AppendLiteral(" (");
				handler.AppendFormatted(FirmwareInformationCode);
				handler.AppendLiteral(")");
				stringBuilder3.Append(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder4 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
				handler.AppendLiteral(" CpuModel: 0x");
				handler.AppendFormatted(CpuModelRaw, "X2");
				stringBuilder4.Append(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder5 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
				handler.AppendLiteral(" CpuFeatures: 0x");
				handler.AppendFormatted(CpuFeaturesRaw, "X8");
				stringBuilder5.Append(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder6 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder2);
				handler.AppendLiteral(" CpuCores: ");
				handler.AppendFormatted(CpuCores);
				stringBuilder6.Append(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder7 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
				handler.AppendLiteral(" CpuRevision: 0x");
				handler.AppendFormatted(CpuRevision);
				stringBuilder7.Append(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder8 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(20, 1, stringBuilder2);
				handler.AppendLiteral(" XtalFrequency: ");
				handler.AppendFormatted(XtalFrequency);
				handler.AppendLiteral(" Mhz");
				stringBuilder8.Append(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder9 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(19, 1, stringBuilder2);
				handler.AppendLiteral(" CpuFrequency: ");
				handler.AppendFormatted(CpuFrequency);
				handler.AppendLiteral(" Mhz");
				stringBuilder9.Append(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder10 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder2);
				handler.AppendLiteral(" Raw Data: ");
				handler.AppendFormatted(base.FirmwareInformationRaw.DebugDump(0, base.FirmwareInformationRaw.Count));
				stringBuilder10.Append(ref handler);
				if (base.IsCommandCompleted)
				{
					stringBuilder.Append(" COMPLETED");
				}
			}
			catch (Exception ex)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder11 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(33, 1, stringBuilder2);
				handler.AppendLiteral(" ERROR Trying to decode response ");
				handler.AppendFormatted(ex.Message);
				stringBuilder11.Append(ref handler);
			}
			return stringBuilder.ToString();
		}
	}
}
