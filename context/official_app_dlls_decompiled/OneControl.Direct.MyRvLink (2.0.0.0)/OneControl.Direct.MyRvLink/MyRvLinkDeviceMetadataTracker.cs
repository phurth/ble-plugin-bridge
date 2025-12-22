using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;
using OneControl.Direct.MyRvLink.Devices;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkDeviceMetadataTracker : CommonDisposable
	{
		private string LogPrefix;

		private CancellationTokenSource? _commandGetDevicesMetadataTcs;

		protected virtual string LogTag { get; } = "MyRvLinkDeviceMetadataTracker";


		public IDirectConnectionMyRvLink MyRvLinkService => DeviceTracker.MyRvLinkService;

		public MyRvLinkDeviceTracker DeviceTracker { get; }

		public uint DeviceMetadataTableCrc { get; }

		public List<IMyRvLinkDeviceMetadata> DeviceMetadataList { get; private set; } = new List<IMyRvLinkDeviceMetadata>();


		public bool IsActive
		{
			get
			{
				if (!base.IsDisposed && DeviceMetadataTableCrc == MyRvLinkService.GatewayInfo?.DeviceMetadataTableCrc)
				{
					return DeviceTracker.IsActive;
				}
				return false;
			}
		}

		public MyRvLinkDeviceMetadataTracker(MyRvLinkDeviceTracker deviceTracker, uint deviceMetadataTableCrc)
		{
			LogPrefix = deviceTracker.MyRvLinkService.LogPrefix;
			DeviceTracker = deviceTracker;
			DeviceMetadataTableCrc = deviceMetadataTableCrc;
		}

		public void GetDevicesMetadataIfNeeded()
		{
			if (!IsActive)
			{
				_commandGetDevicesMetadataTcs?.TryCancelAndDispose();
			}
			else
			{
				if (DeviceTracker.DeviceList.Count <= 0 || DeviceMetadataList.Count > 0 || _commandGetDevicesMetadataTcs != null)
				{
					return;
				}
				CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
				Interlocked.Exchange(ref _commandGetDevicesMetadataTcs, cancellationTokenSource)?.TryCancelAndDispose();
				CancellationToken commandCancellationToken = cancellationTokenSource.Token;
				Task.Run(async delegate
				{
					DeviceMetadataList = new List<IMyRvLinkDeviceMetadata>();
					try
					{
						if (MyRvLinkDeviceMetadataTableSerializable.TryLoad(DeviceTracker.MyRvLinkService.DeviceSourceToken, DeviceMetadataTableCrc, out var deviceMetadataTableSerializable) && deviceMetadataTableSerializable != null)
						{
							DeviceMetadataList = Enumerable.ToList(deviceMetadataTableSerializable.TryDecode());
							if (DeviceTracker.DeviceList.Count != DeviceMetadataList.Count)
							{
								DeviceMetadataList.Clear();
								string logTag = LogTag;
								DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(58, 3);
								defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
								defaultInterpolatedStringHandler.AppendLiteral(" Cached metadata contained ");
								defaultInterpolatedStringHandler.AppendFormatted(DeviceMetadataList.Count);
								defaultInterpolatedStringHandler.AppendLiteral(" but we were expecting ");
								defaultInterpolatedStringHandler.AppendFormatted(DeviceTracker.DeviceList.Count);
								defaultInterpolatedStringHandler.AppendLiteral(" devices");
								TaggedLog.Warning(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
							}
							else
							{
								string logTag2 = LogTag;
								DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(43, 3);
								defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
								defaultInterpolatedStringHandler.AppendLiteral(" Getting Cached Devices for 0x");
								defaultInterpolatedStringHandler.AppendFormatted(DeviceMetadataTableCrc, "x8");
								defaultInterpolatedStringHandler.AppendLiteral(" has ");
								defaultInterpolatedStringHandler.AppendFormatted(DeviceMetadataList.Count);
								defaultInterpolatedStringHandler.AppendLiteral(" devices");
								TaggedLog.Information(logTag2, defaultInterpolatedStringHandler.ToStringAndClear());
								DeviceTracker.UpdateMetadata(DeviceMetadataList, DeviceMetadataTableCrc);
							}
						}
					}
					catch (Exception ex)
					{
						TaggedLog.Error(LogTag, LogPrefix + " Unable to load device Metadata " + ex.Message);
					}
					if (DeviceMetadataList.Count == 0)
					{
						try
						{
							List<IMyRvLinkDeviceMetadata> deviceMetadataList = await GetDevicesMetadataAsync(commandCancellationToken).ConfigureAwait(false);
							DeviceMetadataList = deviceMetadataList;
							DeviceTracker.UpdateMetadata(DeviceMetadataList, DeviceMetadataTableCrc);
							if (DeviceTracker.IsDeviceLoadComplete)
							{
								new MyRvLinkDeviceMetadataTableSerializable(DeviceMetadataTableCrc, Enumerable.ToList(Enumerable.Select(DeviceMetadataList, (IMyRvLinkDeviceMetadata device) => new MyRvLinkDeviceMetadataSerializable(device)))).TrySave(DeviceTracker.MyRvLinkService.DeviceSourceToken);
							}
						}
						catch (Exception ex2)
						{
							TaggedLog.Debug(LogTag, LogPrefix + " Get Devices Metadata failed: " + ex2.Message);
						}
					}
					_commandGetDevicesMetadataTcs?.TryCancelAndDispose();
					_commandGetDevicesMetadataTcs = null;
				}, commandCancellationToken);
			}
		}

		private async Task<List<IMyRvLinkDeviceMetadata>> GetDevicesMetadataAsync(CancellationToken cancellationToken)
		{
			MyRvLinkCommandGetDevicesMetadata commandGetDevicesMetadata = new MyRvLinkCommandGetDevicesMetadata(MyRvLinkService.GetNextCommandId(), DeviceTracker.DeviceTableId, 0, 255);
			IMyRvLinkCommandResponse myRvLinkCommandResponse = await MyRvLinkService.SendCommandAsync(commandGetDevicesMetadata, cancellationToken, MyRvLinkSendCommandOption.None);
			if (myRvLinkCommandResponse is MyRvLinkCommandResponseFailure)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(7, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Failed ");
				defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponse);
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (commandGetDevicesMetadata.ResponseState == MyRvLinkResponseState.Failed)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(35, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Get Device Metadata Command Failed ");
				defaultInterpolatedStringHandler.AppendFormatted(commandGetDevicesMetadata.ResponseState);
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (commandGetDevicesMetadata.ResponseReceivedMetadataTableCrc != DeviceMetadataTableCrc)
			{
				throw new MyRvLinkException("Response didn't match expected Device Metadata Table CRC, discarding response");
			}
			List<IMyRvLinkDeviceMetadata> devicesMetadata = commandGetDevicesMetadata.DevicesMetadata;
			if (devicesMetadata == null || devicesMetadata.Count == 0)
			{
				throw new MyRvLinkException("No Devices Found");
			}
			if (devicesMetadata.Count != DeviceTracker.DeviceList.Count)
			{
				throw new MyRvLinkException("Count of devices don't match between metadata and device list!");
			}
			return devicesMetadata;
		}

		public override void Dispose(bool disposing)
		{
			_commandGetDevicesMetadataTcs?.TryCancelAndDispose();
		}
	}
}
