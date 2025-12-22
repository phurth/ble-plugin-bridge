using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace OneControl.Direct.MyRvLink
{
	public static class MyRvLinkCommandFactory
	{
		public static IMyRvLinkCommand Decode(IReadOnlyList<byte> rawData)
		{
			if (rawData == null)
			{
				throw new ArgumentNullException("rawData");
			}
			MyRvLinkCommandType myRvLinkCommandType = MyRvLinkCommand.DecodeCommandType(rawData);
			switch (myRvLinkCommandType)
			{
			case MyRvLinkCommandType.GetDevices:
				return MyRvLinkCommandGetDevices.Decode(rawData);
			case MyRvLinkCommandType.GetDevicesMetadata:
				return MyRvLinkCommandGetDevicesMetadata.Decode(rawData);
			case MyRvLinkCommandType.RemoveOfflineDevices:
				return MyRvLinkCommandRemoveOfflineDevices.Decode(rawData);
			case MyRvLinkCommandType.GetProductDtcValues:
				return MyRvLinkCommandGetProductDtcValues.Decode(rawData);
			case MyRvLinkCommandType.GetDevicePidList:
				return MyRvLinkCommandGetDevicePidList.Decode(rawData);
			case MyRvLinkCommandType.GetDevicePid:
				return MyRvLinkCommandGetDevicePid.Decode(rawData);
			case MyRvLinkCommandType.SetDevicePid:
				return MyRvLinkCommandSetDevicePid.Decode(rawData);
			case MyRvLinkCommandType.GetDevicePidWithAddress:
				return MyRvLinkCommandGetDevicePidWithAddress.Decode(rawData);
			case MyRvLinkCommandType.SetDevicePidWithAddress:
				return MyRvLinkCommandSetDevicePidWithAddress.Decode(rawData);
			case MyRvLinkCommandType.SoftwareUpdateAuthorization:
				return MyRvLinkCommandSoftwareUpdateAuthorization.Decode(rawData);
			case MyRvLinkCommandType.ActionGeneratorGenie:
				return MyRvLinkCommandActionGeneratorGenie.Decode(rawData);
			case MyRvLinkCommandType.ActionDimmable:
				return MyRvLinkCommandActionDimmable.Decode(rawData);
			case MyRvLinkCommandType.ActionRgb:
				return MyRvLinkCommandActionRgb.Decode(rawData);
			case MyRvLinkCommandType.ActionHvac:
				return MyRvLinkCommandActionHvac.Decode(rawData);
			case MyRvLinkCommandType.ActionSwitch:
				return MyRvLinkCommandActionSwitch.Decode(rawData);
			case MyRvLinkCommandType.ActionMovement:
				return MyRvLinkCommandActionMovement.Decode(rawData);
			case MyRvLinkCommandType.ActionAccessoryGateway:
				return MyRvLinkCommandActionAccessoryGateway.Decode(rawData);
			case MyRvLinkCommandType.Leveler5Command:
				return MyRvLinkCommandLeveler5.Decode(rawData);
			case MyRvLinkCommandType.Leveler4ButtonCommand:
				return MyRvLinkCommandLeveler4ButtonCommand.Decode(rawData);
			case MyRvLinkCommandType.Leveler3ButtonCommand:
				return MyRvLinkCommandLeveler3ButtonCommand.Decode(rawData);
			case MyRvLinkCommandType.Leveler1ButtonCommand:
				return MyRvLinkCommandLeveler1ButtonCommand.Decode(rawData);
			case MyRvLinkCommandType.Diagnostics:
				return MyRvLinkCommandDiagnostics.Decode(rawData);
			default:
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(32, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Command Not Implemented ");
				defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandType);
				defaultInterpolatedStringHandler.AppendLiteral(" see ");
				defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkCommandFactory");
				defaultInterpolatedStringHandler.AppendLiteral(".cs");
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			}
		}
	}
}
