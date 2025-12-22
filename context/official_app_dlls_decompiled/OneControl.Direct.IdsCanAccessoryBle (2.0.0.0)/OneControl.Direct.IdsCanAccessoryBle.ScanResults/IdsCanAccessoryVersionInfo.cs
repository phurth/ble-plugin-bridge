using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.IdsCanAccessoryBle.ScanResults
{
	public readonly struct IdsCanAccessoryVersionInfo
	{
		private const string LogTag = "IdsCanAccessoryVersionInfo";

		private const int RequiredSize = 16;

		private const int MajorVersionIndex = 3;

		private const int MinorVersionIndex = 4;

		private const int SoftwarePartNumberBaseStartIndex = 5;

		private const int SoftwarePartNumberRevStartIndex = 10;

		private const int MicroControllerStartIndex = 12;

		private const int SoftwareStackStartIndex = 14;

		private const int SoftwarePartNumberBaseSize = 5;

		private const int SoftwarePartNumberRevSize = 2;

		public bool IsValid { get; }

		public byte MajorVersion { get; }

		public byte MinorVersion { get; }

		public string SoftwarePartNumberBase { get; }

		public string SoftwarePartNumberRev { get; }

		public AccessoryMicroController MicroController { get; }

		public AccessorySoftwareStack SoftwareStack { get; }

		public string SoftwarePartNumber => SoftwarePartNumberBase + SoftwarePartNumberRev;

		public IdsCanAccessoryVersionInfo(byte[] rawData)
		{
			IsValid = false;
			MajorVersion = 0;
			MinorVersion = 0;
			SoftwarePartNumberBase = string.Empty;
			SoftwarePartNumberRev = string.Empty;
			MicroController = AccessoryMicroController.Unknown;
			SoftwareStack = AccessorySoftwareStack.Unknown;
			if (rawData.Length != 16)
			{
				if (rawData.Length != 0)
				{
					TaggedLog.Warning("IdsCanAccessoryVersionInfo", "Unable to parse IdsCanAccessoryVersionInfo because of invalid size: " + rawData.DebugDump());
				}
				return;
			}
			MajorVersion = rawData[3];
			MinorVersion = rawData[4];
			SoftwarePartNumberBase = Encoding.UTF8.GetString(rawData, 5, 5);
			SoftwarePartNumberRev = Encoding.UTF8.GetString(rawData, 10, 2);
			ushort valueUInt = rawData.GetValueUInt16(12);
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
			switch (valueUInt)
			{
			case 1:
				MicroController = AccessoryMicroController.Esp32;
				break;
			case 2:
				MicroController = AccessoryMicroController.Dt369;
				break;
			case 3:
				MicroController = AccessoryMicroController.Esp32S3;
				break;
			case 4:
				MicroController = AccessoryMicroController.Nrf52810;
				break;
			default:
				MicroController = AccessoryMicroController.Unknown;
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(62, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Received unknown MicroController for ");
				defaultInterpolatedStringHandler.AppendFormatted("IdsCanAccessoryVersionInfo");
				defaultInterpolatedStringHandler.AppendLiteral(" 0x");
				defaultInterpolatedStringHandler.AppendFormatted(valueUInt, "x");
				defaultInterpolatedStringHandler.AppendLiteral(" defaulting to unknown");
				TaggedLog.Warning("IdsCanAccessoryVersionInfo", defaultInterpolatedStringHandler.ToStringAndClear());
				break;
			}
			ushort valueUInt2 = rawData.GetValueUInt16(14);
			switch (valueUInt2)
			{
			case 1:
				SoftwareStack = AccessorySoftwareStack.EspIdfV42;
				return;
			case 2:
				SoftwareStack = AccessorySoftwareStack.NordicSoftDeviceS132V720;
				return;
			case 3:
				SoftwareStack = AccessorySoftwareStack.EspIdfV441;
				return;
			case 4:
				SoftwareStack = AccessorySoftwareStack.NordicSoftDeviceS112;
				return;
			}
			SoftwareStack = AccessorySoftwareStack.Unknown;
			defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(60, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Received unknown SoftwareStack for ");
			defaultInterpolatedStringHandler.AppendFormatted("IdsCanAccessoryVersionInfo");
			defaultInterpolatedStringHandler.AppendLiteral(" 0x");
			defaultInterpolatedStringHandler.AppendFormatted(valueUInt2, "x");
			defaultInterpolatedStringHandler.AppendLiteral(" defaulting to unknown");
			TaggedLog.Warning("IdsCanAccessoryVersionInfo", defaultInterpolatedStringHandler.ToStringAndClear());
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(4, 5);
			defaultInterpolatedStringHandler.AppendFormatted(MajorVersion);
			defaultInterpolatedStringHandler.AppendLiteral(".");
			defaultInterpolatedStringHandler.AppendFormatted(MinorVersion);
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendFormatted(SoftwarePartNumber);
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendFormatted(MicroController);
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendFormatted(SoftwareStack);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
