using System;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandGetFirmwareInformationResponseSuccessVersion : MyRvLinkCommandGetFirmwareInformationResponseSuccess
	{
		public const int WlpVersionMajorIndexEx = 1;

		public const int WlpVersionMinorIndexEx = 2;

		public const int IdsCanVersionRawIndexEx = 3;

		public const int WlpDiagnosticRawIndexEx = 4;

		public const int VersionMajorIndexEx = 5;

		public const int VersionMinorIndexEx = 9;

		public const byte MinorVersionFlagBitmask = 128;

		public const byte MinorVersionValueBitmask = 127;

		public override FirmwareInformationCode FirmwareInformationCode => FirmwareInformationCode.Version;

		protected override int MinExtendedDataLength => 13;

		public MyRvLinkProtocolVersionMajor WlpVersionMajor => (MyRvLinkProtocolVersionMajor)WlpVersionMajorRaw;

		public byte WlpVersionMajorRaw => base.ExtendedData[1];

		public byte WlpVersionMinor => (byte)(base.ExtendedData[2] & 0x7Fu);

		public bool IsDebugVersion => (base.ExtendedData[2] & 0x80) != 0;

		public string WlpVersionString
		{
			get
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 3);
				defaultInterpolatedStringHandler.AppendFormatted(WlpVersionMajorRaw, "D3");
				defaultInterpolatedStringHandler.AppendLiteral(".");
				defaultInterpolatedStringHandler.AppendFormatted(WlpVersionMinor, "D3");
				defaultInterpolatedStringHandler.AppendLiteral(" ");
				defaultInterpolatedStringHandler.AppendFormatted(IsDebugVersion ? "DEBUG" : "Release");
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
		}

		public byte IdsCanVersionRaw => base.ExtendedData[3];

		public byte DiagnosticCodeRaw => base.ExtendedData[4];

		public Version Version => new Version((int)base.ExtendedData.GetValueUInt32(5), (int)base.ExtendedData.GetValueUInt32(9));

		public MyRvLinkCommandGetFirmwareInformationResponseSuccessVersion(MyRvLinkCommandGetFirmwareInformationResponseSuccess response)
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
				handler.AppendLiteral(" WlpVersion: ");
				handler.AppendFormatted(WlpVersionString);
				stringBuilder4.Append(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder5 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(18, 1, stringBuilder2);
				handler.AppendLiteral(" IdsCanVersion: 0x");
				handler.AppendFormatted(IdsCanVersionRaw, "X2");
				stringBuilder5.Append(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder6 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(19, 1, stringBuilder2);
				handler.AppendLiteral(" DiagnosticCode: 0x");
				handler.AppendFormatted(DiagnosticCodeRaw, "X2");
				stringBuilder6.Append(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder7 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
				handler.AppendLiteral(" Version: ");
				handler.AppendFormatted(Version);
				stringBuilder7.Append(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder8 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder2);
				handler.AppendLiteral(" Raw Data: ");
				handler.AppendFormatted(base.FirmwareInformationRaw.DebugDump(0, base.FirmwareInformationRaw.Count));
				stringBuilder8.Append(ref handler);
				if (base.IsCommandCompleted)
				{
					stringBuilder.Append(" COMPLETED");
				}
			}
			catch (Exception ex)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder9 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(33, 1, stringBuilder2);
				handler.AppendLiteral(" ERROR Trying to decode response ");
				handler.AppendFormatted(ex.Message);
				stringBuilder9.Append(ref handler);
			}
			return stringBuilder.ToString();
		}
	}
}
