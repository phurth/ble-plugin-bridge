namespace OneControl.Direct.MyRvLink
{
	public static class GatewayVersionSupportLevelExtension
	{
		public const MyRvLinkProtocolVersionMajor SupportedProtocolMinimumVersion = MyRvLinkProtocolVersionMajor.Version5;

		public const byte DebugMinorVersionMask = 128;

		public static bool IsSupported(this GatewayVersionSupportLevel protocolVersionSupported)
		{
			switch (protocolVersionSupported)
			{
			case GatewayVersionSupportLevel.Supported:
			case GatewayVersionSupportLevel.SupportedForTest:
				return true;
			default:
				return false;
			}
		}

		public static GatewayVersionSupportLevel MakeVersionSupportedInfo(MyRvLinkProtocolVersionMajor majorVersion, byte wlpVersionMinor)
		{
			switch (majorVersion)
			{
			case MyRvLinkProtocolVersionMajor.VersionInvalid:
				return GatewayVersionSupportLevel.NotSupported;
			case MyRvLinkProtocolVersionMajor.VersionUnknown:
				return GatewayVersionSupportLevel.Unknown;
			default:
				if ((wlpVersionMinor & 0x80) == 128)
				{
					return GatewayVersionSupportLevel.SupportedForTest;
				}
				if (majorVersion >= MyRvLinkProtocolVersionMajor.Version5)
				{
					return GatewayVersionSupportLevel.Supported;
				}
				return GatewayVersionSupportLevel.NotSupported;
			}
		}

		public static bool IsMinimumRequiredVersion(MyRvLinkProtocolVersionMajor majorVersion)
		{
			if (majorVersion == MyRvLinkProtocolVersionMajor.VersionInvalid)
			{
				return false;
			}
			if (majorVersion == MyRvLinkProtocolVersionMajor.VersionUnknown)
			{
				return false;
			}
			if (majorVersion < MyRvLinkProtocolVersionMajor.Version5)
			{
				return false;
			}
			return true;
		}
	}
}
