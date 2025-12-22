using System;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.LogicalDevice;
using OneControl.Direct.MyRvLink.Cache;

namespace OneControl.Direct.MyRvLink
{
	public interface IDirectConnectionMyRvLink : ILogicalDeviceSourceDirectConnectionMyRvLink, ILogicalDeviceSourceDirectConnection, ILogicalDeviceSourceDirect, ILogicalDeviceSource, ILogicalDeviceSourceConnection
	{
		string LogPrefix { get; }

		MyRvLinkGatewayInformation? GatewayInfo { get; }

		DeviceTableIdCache DeviceTableIdCache { get; }

		bool IsFirmwareVersionSupported { get; }

		bool HasMinimumExpectedProtocolVersion { get; }

		ushort GetNextCommandId();

		(byte DeviceTableId, byte DeviceId)? GetMyRvDeviceFromLogicalDevice(ILogicalDevice logicalDevice);

		ILogicalDevice? GetLogicalDeviceFromMyRvDevice(byte deviceTableId, byte deviceId);

		Task<IMyRvLinkCommandResponse> SendCommandAsync(IMyRvLinkCommand command, CancellationToken cancellationToken, MyRvLinkSendCommandOption commandOption, Action<IMyRvLinkCommandResponse>? response = null);

		Task<IMyRvLinkCommandResponse> SendCommandAsync(IMyRvLinkCommand command, CancellationToken cancellationToken, TimeSpan commandTimeout, MyRvLinkSendCommandOption commandOption = MyRvLinkSendCommandOption.None, Action<IMyRvLinkCommandResponse>? responseAction = null);
	}
}
