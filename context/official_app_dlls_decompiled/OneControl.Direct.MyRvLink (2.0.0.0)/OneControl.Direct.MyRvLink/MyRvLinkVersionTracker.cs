using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace OneControl.Direct.MyRvLink
{
	internal class MyRvLinkVersionTracker : CommonDisposable
	{
		private const string LogTag = "MyRvLinkVersionTracker";

		private string LogPrefix;

		private GatewayVersionSupportLevel _detailedGatewayProtocolVersionSupport;

		private Task<IMyRvLinkCommandResponse>? _getFirmwareInformationResponse;

		private MyRvLinkCommandGetFirmwareInformationResponseSuccessVersion? _responseSuccess;

		public IDirectConnectionMyRvLink MyRvLinkService { get; }

		public bool IsVersionSupported
		{
			get
			{
				if (!base.IsDisposed)
				{
					return _detailedGatewayProtocolVersionSupport.IsSupported();
				}
				return false;
			}
		}

		public MyRvLinkVersionTracker(IDirectConnectionMyRvLink myRvLinkService)
		{
			MyRvLinkService = myRvLinkService ?? throw new ArgumentNullException("Invalid IMyRvLinkService", "myRvLinkService");
			LogPrefix = MyRvLinkService.LogPrefix;
		}

		public void GetVersionIfNeeded()
		{
			if (MyRvLinkService.HasMinimumExpectedProtocolVersion && _getFirmwareInformationResponse == null)
			{
				MyRvLinkCommandGetFirmwareInformation command = new MyRvLinkCommandGetFirmwareInformation(MyRvLinkService.GetNextCommandId(), FirmwareInformationCode.Version);
				_getFirmwareInformationResponse = MyRvLinkService.SendCommandAsync(command, CancellationToken.None, MyRvLinkSendCommandOption.None, GetVersionResponse);
				MyRvLinkCommandGetFirmwareInformation command2 = new MyRvLinkCommandGetFirmwareInformation(MyRvLinkService.GetNextCommandId(), FirmwareInformationCode.Cpu);
				MyRvLinkService.SendCommandAsync(command2, CancellationToken.None, MyRvLinkSendCommandOption.DontWaitForResponse);
			}
		}

		private void GetVersionResponse(IMyRvLinkCommandResponse response)
		{
			if (!(response is MyRvLinkCommandGetFirmwareInformationResponseSuccessVersion myRvLinkCommandGetFirmwareInformationResponseSuccessVersion))
			{
				if (response is MyRvLinkCommandResponseFailure myRvLinkCommandResponseFailure)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(26, 2);
					defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
					defaultInterpolatedStringHandler.AppendLiteral(" Firmware Version FAILED: ");
					defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponseFailure);
					TaggedLog.Warning("MyRvLinkVersionTracker", defaultInterpolatedStringHandler.ToStringAndClear());
					if (myRvLinkCommandResponseFailure.FailureCode == MyRvLinkCommandResponseFailureCode.CommandNotSupported)
					{
						_detailedGatewayProtocolVersionSupport = GatewayVersionSupportLevel.NotSupported;
						TaggedLog.Error("MyRvLinkVersionTracker", LogPrefix + " Getting of firmware version not supported, but is now required so this firmware version is assumed not compatible");
					}
					else
					{
						TaggedLog.Information("MyRvLinkVersionTracker", LogPrefix + " Clear getting firmware info response so we can try again");
						_responseSuccess = null;
						_getFirmwareInformationResponse = null;
					}
				}
				else
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(47, 3);
					defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
					defaultInterpolatedStringHandler.AppendLiteral(" Firmware Version Unknown/Unexpected Response ");
					defaultInterpolatedStringHandler.AppendFormatted(response.GetType());
					defaultInterpolatedStringHandler.AppendLiteral(" ");
					defaultInterpolatedStringHandler.AppendFormatted(response.CommandResult);
					TaggedLog.Error("MyRvLinkVersionTracker", defaultInterpolatedStringHandler.ToStringAndClear());
					_detailedGatewayProtocolVersionSupport = GatewayVersionSupportLevel.NotSupported;
				}
			}
			else
			{
				_responseSuccess = myRvLinkCommandGetFirmwareInformationResponseSuccessVersion;
				_detailedGatewayProtocolVersionSupport = (myRvLinkCommandGetFirmwareInformationResponseSuccessVersion.IsDebugVersion ? GatewayVersionSupportLevel.SupportedForTest : GatewayVersionSupportLevelExtension.MakeVersionSupportedInfo(myRvLinkCommandGetFirmwareInformationResponseSuccessVersion.WlpVersionMajor, myRvLinkCommandGetFirmwareInformationResponseSuccessVersion.WlpVersionMinor));
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(20, 3);
				defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" Firmware Version ");
				defaultInterpolatedStringHandler.AppendFormatted(_detailedGatewayProtocolVersionSupport);
				defaultInterpolatedStringHandler.AppendLiteral(": ");
				defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandGetFirmwareInformationResponseSuccessVersion);
				TaggedLog.Information("MyRvLinkVersionTracker", defaultInterpolatedStringHandler.ToStringAndClear());
				if (!_detailedGatewayProtocolVersionSupport.IsSupported())
				{
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(50, 3);
					defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
					defaultInterpolatedStringHandler.AppendLiteral(" This Protocol Firmware Version ");
					defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandGetFirmwareInformationResponseSuccessVersion.WlpVersionMajorRaw);
					defaultInterpolatedStringHandler.AppendLiteral(".");
					defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandGetFirmwareInformationResponseSuccessVersion.WlpVersionMinor);
					defaultInterpolatedStringHandler.AppendLiteral(" isn't supported.");
					TaggedLog.Warning("MyRvLinkVersionTracker", defaultInterpolatedStringHandler.ToStringAndClear());
				}
			}
		}

		public bool IsGatewayVersionValid(MyRvLinkGatewayInformation? gatewayInfo)
		{
			if (gatewayInfo == null)
			{
				return false;
			}
			if (!IsVersionSupported || _responseSuccess == null)
			{
				return false;
			}
			if (_responseSuccess!.WlpVersionMajorRaw != (byte)gatewayInfo!.ProtocolVersionMajor)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(95, 3);
				defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" Major version reported in GatewayInfo ");
				defaultInterpolatedStringHandler.AppendFormatted(gatewayInfo!.ProtocolVersionMajor);
				defaultInterpolatedStringHandler.AppendLiteral(", doesn't match major version reported in firmware info ");
				defaultInterpolatedStringHandler.AppendFormatted(_responseSuccess!.WlpVersionString);
				TaggedLog.Warning("MyRvLinkVersionTracker", defaultInterpolatedStringHandler.ToStringAndClear());
				return false;
			}
			return true;
		}

		public override void Dispose(bool disposing)
		{
		}
	}
}
