using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ids.portable.ble.Ble;
using ids.portable.ble.BleManager;
using ids.portable.ble.ScanResults;
using IDS.Portable.Common;
using IDS.Portable.Common.COBS;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.LogicalDevice;
using IDS.Portable.LogicalDevice.LogicalDeviceSource.ConnectionFailure;
using OneControl.Direct.MyRvLink;

namespace OneControl.Direct.MyRvLinkBle
{
	public class DirectConnectionMyRvLinkBle : DirectConnectionMyRvLink, ILogicalDeviceSourceConnectionFailure, ILogicalDeviceSourceConnection, ILogicalDeviceSource, ILogicalDeviceSourceDirect
	{
		private const string LogTag = "DirectConnectionMyRvLink";

		private const int BleRequestedMtuSize = 185;

		public static readonly Guid BleServiceGuid = Guid.Parse("00000030-0200-A58E-E411-AFE28044E62C");

		private static readonly Guid BleVersion = Guid.Parse("00000031-0200-A58E-E411-AFE28044E62C");

		private static readonly Guid BleReadCharacteristic = Guid.Parse("00000034-0200-A58E-E411-AFE28044E62C");

		private static readonly Guid BleWriteCharacteristic = Guid.Parse("00000033-0200-A58E-E411-AFE28044E62C");

		private readonly BackgroundOperation? _backgroundOperation;

		private readonly IBleService _bleService;

		private readonly ILogicalDeviceService _logicalDeviceService;

		private readonly DirectConnectionMyRvLinkBleConnectionFailure _connectionFailureManager = new DirectConnectionMyRvLinkBleConnectionFailure();

		protected readonly CobsEncoder CobsEncoder = new CobsEncoder(prependStartFrame: true, useCrc: true, 0);

		protected readonly CobsDecoder CobsDecoder = new CobsDecoder(useCrc: true, 0);

		private readonly CobsStream? _cobsStream;

		private readonly BleStream _bleStream;

		private const int ConnectionErrorRetryTimeMs = 2000;

		private const int ConnectionReceiveDataTimeMs = 8000;

		private const int TagCount = 1;

		private readonly ILogicalDeviceTag[] _deviceSourceTags = new ILogicalDeviceTag[1];

		public Guid BleGuid { get; private set; }

		public string? BleName { get; private set; }

		public IConnectionFailure ConnectionFailure => _connectionFailureManager;

		protected Stream? OpenStream { get; set; }

		public override string DeviceSourceToken { get; }

		public override IEnumerable<ILogicalDeviceTag> DeviceSourceTags => _deviceSourceTags;

		public override event Action<ILogicalDeviceSourceDirectConnection>? DidConnectEvent;

		public override event Action<ILogicalDeviceSourceDirectConnection>? DidDisconnectEvent;

		public override event UpdateDeviceSourceReachabilityEventHandler? UpdateDeviceSourceReachabilityEvent;

		public DirectConnectionMyRvLinkBle(IBleService bleService, ILogicalDeviceService deviceService, IEndPointConnectionBle bleConnection, string DeviceSourceToken, ILogicalDeviceTag? logicalDeviceTag)
			: this(bleService, deviceService, bleConnection.ConnectionGuid, bleConnection.ConnectionId, DeviceSourceToken, logicalDeviceTag)
		{
		}

		public DirectConnectionMyRvLinkBle(IBleService bleService, ILogicalDeviceService deviceService, Guid bleGuid, string bleName, string deviceSourceToken, ILogicalDeviceTag? logicalDeviceTag = null)
			: base(deviceService, bleName, (logicalDeviceTag == null) ? new List<ILogicalDeviceTag>() : new List<ILogicalDeviceTag> { logicalDeviceTag })
		{
			BleGuid = bleGuid;
			BleName = bleName;
			if (MyRvLinkBleGatewayScanResult.GatewayTypeFromDeviceName(bleName) == RvLinkGatewayType.Gateway)
			{
				MyRvLinkDeviceHost.SetDefaultHostDeviceIdMac(BleGuid);
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(9, 2);
			defaultInterpolatedStringHandler.AppendFormatted(base.LogPrefix);
			defaultInterpolatedStringHandler.AppendLiteral(" CREATED ");
			defaultInterpolatedStringHandler.AppendFormatted(this);
			TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
			DeviceSourceToken = deviceSourceToken;
			_deviceSourceTags[0] = MakeDeviceSourceTag();
			_bleService = bleService;
			_bleStream = new BleStream(_bleService.Manager, BleServiceGuid, BleReadCharacteristic, BleWriteCharacteristic, 185, limitWriteSizeToMtuSize: false);
			_cobsStream = new CobsStream(_bleStream, CobsEncoder, CobsDecoder);
			_backgroundOperation = new BackgroundOperation((BackgroundOperation.BackgroundOperationFunc)BackgroundOperationAsync);
			_logicalDeviceService = deviceService;
			_bleService.Scanner.DidReceiveScanResult += ReceivedBleScanResult;
		}

		private async void ReceivedBleScanResult(IBleScanResult scanResult)
		{
			if (scanResult.DeviceId != BleGuid || !(scanResult is MyRvLinkBleGatewayScanResult myRvLinkBleGatewayScanResult))
			{
				return;
			}
			MyRvLinkBleGatewayScanResult.RawAlertData? formattedResult = myRvLinkBleGatewayScanResult.GetAlertStatus();
			if (!formattedResult.HasValue)
			{
				return;
			}
			try
			{
				IReadOnlyList<IMyRvLinkDevice> readOnlyList = await base.DeviceTableIdCache.GetDevicesForDeviceTableIdAsync(formattedResult.Value.RvLinkTableId);
				if (readOnlyList == null)
				{
					return;
				}
				IMyRvLinkDevice myRvLinkDevice = readOnlyList[formattedResult.Value.RvLinkDeviceId];
				if (myRvLinkDevice == null)
				{
					return;
				}
				foreach (ILogicalDevice item in myRvLinkDevice.FindLogicalDevicesMatchingPhysicalHardware(_logicalDeviceService))
				{
					if (item is ILogicalDeviceWithStatusAlerts logicalDeviceWithStatusAlerts)
					{
						logicalDeviceWithStatusAlerts.UpdateAlert(formattedResult.Value.AlertId, formattedResult.Value.AlertData);
					}
				}
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(74, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to generate alert with the following scan result: ");
				defaultInterpolatedStringHandler.AppendFormatted(scanResult);
				defaultInterpolatedStringHandler.AppendLiteral(", error message: ");
				defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
				TaggedLog.Warning("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
			}
		}

		public override void Start()
		{
			if (_backgroundOperation == null)
			{
				throw new ObjectDisposedException("DirectConnectionMyRvLink");
			}
			if (!_backgroundOperation!.StartedOrWillStart)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(11, 3);
				defaultInterpolatedStringHandler.AppendFormatted(base.LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" Starting ");
				defaultInterpolatedStringHandler.AppendFormatted(BleName);
				defaultInterpolatedStringHandler.AppendLiteral("/");
				defaultInterpolatedStringHandler.AppendFormatted(BleGuid);
				TaggedLog.Information("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				_backgroundOperation?.Start();
			}
		}

		public override void Stop()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(11, 3);
			defaultInterpolatedStringHandler.AppendFormatted(base.LogPrefix);
			defaultInterpolatedStringHandler.AppendLiteral(" Stopping ");
			defaultInterpolatedStringHandler.AppendFormatted(BleName);
			defaultInterpolatedStringHandler.AppendLiteral("/");
			defaultInterpolatedStringHandler.AppendFormatted(BleGuid);
			TaggedLog.Information("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
			_backgroundOperation?.Stop();
			base.Stop();
		}

		protected virtual async Task<Stream> OpenStreamAsync(CancellationToken cancellationToken)
		{
			try
			{
				base.IsConnected = false;
				OpenStream = null;
				await _bleStream.OpenAsync(BleGuid, BleName ?? string.Empty, cancellationToken);
				OpenStream = _cobsStream;
				base.IsConnected = true;
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (TimeoutException)
			{
				throw;
			}
			catch (Exception ex3)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(64, 4);
				defaultInterpolatedStringHandler.AppendFormatted(base.LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" Error Connecting to BLE Connection (Unable to Open Stream) ");
				defaultInterpolatedStringHandler.AppendFormatted(BleGuid);
				defaultInterpolatedStringHandler.AppendLiteral("(");
				defaultInterpolatedStringHandler.AppendFormatted(BleName);
				defaultInterpolatedStringHandler.AppendLiteral("): ");
				defaultInterpolatedStringHandler.AppendFormatted(ex3.Message);
				TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				throw;
			}
			return OpenStream ?? throw new MyRvLinkDeviceServiceNotConnectedException(this, "Unable to open stream");
		}

		protected virtual Task TryCloseStreamAsync()
		{
			base.IsConnected = false;
			try
			{
				OpenStream?.Close();
			}
			catch
			{
			}
			OpenStream = null;
			return Task.CompletedTask;
		}

		protected virtual Task<int> ReadDataAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return (OpenStream ?? throw new MyRvLinkBleServiceException("Stream isn't connected/opened"))!.ReadAsync(buffer, offset, count, cancellationToken);
		}

		protected virtual Task WriteDataAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return (OpenStream ?? throw new MyRvLinkBleServiceException("Stream isn't connected/opened"))!.WriteAsync(buffer, offset, count, cancellationToken);
		}

		protected async Task BackgroundOperationAsync(CancellationToken cancellationToken)
		{
			base.Start();
			byte[] readBuffer = new byte[255];
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(17, 4);
			defaultInterpolatedStringHandler.AppendFormatted(base.LogPrefix);
			defaultInterpolatedStringHandler.AppendLiteral(" Starting ");
			defaultInterpolatedStringHandler.AppendFormatted("DirectConnectionMyRvLink");
			defaultInterpolatedStringHandler.AppendLiteral(" for ");
			defaultInterpolatedStringHandler.AppendFormatted(BleGuid);
			defaultInterpolatedStringHandler.AppendLiteral("(");
			defaultInterpolatedStringHandler.AppendFormatted(BleName);
			defaultInterpolatedStringHandler.AppendLiteral(")");
			TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
			base.IsConnected = false;
			_connectionFailureManager.Clear();
			while (true)
			{
				if (!cancellationToken.IsCancellationRequested)
				{
					try
					{
						TaggedLog.Debug("DirectConnectionMyRvLink", base.LogPrefix + " TryOpenStreamAsync Started");
						await OpenStreamAsync(cancellationToken);
						TaggedLog.Debug("DirectConnectionMyRvLink", base.LogPrefix + " TryOpenStreamAsync Success");
					}
					catch (Exception exception)
					{
						await _connectionFailureManager.TryDelayForRetry(2000.0, exception, cancellationToken);
						continue;
					}
					_connectionFailureManager.Clear();
					try
					{
						try
						{
							DidConnectEvent?.Invoke(this);
							UpdateDeviceSourceReachabilityEvent?.Invoke(this);
							Stream openStream = OpenStream;
							if (!(openStream is CobsStream cobsStream))
							{
								throw new MyRvLinkBleServiceException("Expected COBS Stream");
							}
							while (!cancellationToken.IsCancellationRequested)
							{
								int num = await cobsStream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken, TimeSpan.FromMilliseconds(8000.0));
								IMyRvLinkEvent myRvLinkEvent = Singleton<MyRvLinkEventFactory>.Instance.TryDecodeEvent(new ArraySegment<byte>(readBuffer, 0, num), base.IsFirmwareVersionSupported, base.GetPendingCommand);
								if (myRvLinkEvent == null)
								{
									if (base.IsFirmwareVersionSupported)
									{
										TaggedLog.Error("DirectConnectionMyRvLink", base.LogPrefix + " Ignoring event because unable to decode it: " + readBuffer.DebugDump(0, readBuffer.Length));
									}
								}
								else
								{
									OnReceivedEvent(myRvLinkEvent);
								}
							}
						}
						catch (OperationCanceledException)
						{
						}
						catch (TimeoutException)
						{
						}
						catch (MyRvLinkDeviceServiceNotStartedException)
						{
							goto IL_05ea;
						}
						catch (MyRvLinkDeviceServiceNotConnectedException)
						{
							goto IL_05ea;
						}
						catch (Exception ex5)
						{
							defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(39, 4);
							defaultInterpolatedStringHandler.AppendFormatted(base.LogPrefix);
							defaultInterpolatedStringHandler.AppendLiteral(" Error Reading from BLE Connection ");
							defaultInterpolatedStringHandler.AppendFormatted(BleGuid);
							defaultInterpolatedStringHandler.AppendLiteral("(");
							defaultInterpolatedStringHandler.AppendFormatted(BleName);
							defaultInterpolatedStringHandler.AppendLiteral("): ");
							defaultInterpolatedStringHandler.AppendFormatted(ex5.Message);
							TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
							await TaskExtension.TryDelay(2000, cancellationToken);
						}
						goto end_IL_025d;
						IL_05ea:
						if (base.IsConnected)
						{
							DidDisconnectEvent?.Invoke(this);
							UpdateDeviceSourceReachabilityEvent?.Invoke(this);
						}
						await TryCloseStreamAsync();
						base.Stop();
						_connectionFailureManager.Clear();
						defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(16, 4);
						defaultInterpolatedStringHandler.AppendFormatted(base.LogPrefix);
						defaultInterpolatedStringHandler.AppendLiteral(" Stopped ");
						defaultInterpolatedStringHandler.AppendFormatted("DirectConnectionMyRvLink");
						defaultInterpolatedStringHandler.AppendLiteral(" for ");
						defaultInterpolatedStringHandler.AppendFormatted(BleGuid);
						defaultInterpolatedStringHandler.AppendLiteral("(");
						defaultInterpolatedStringHandler.AppendFormatted(BleName);
						defaultInterpolatedStringHandler.AppendLiteral(")");
						TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
						break;
						end_IL_025d:;
					}
					finally
					{
						DidDisconnectEvent?.Invoke(this);
						await TryCloseStreamAsync();
						UpdateDeviceSourceReachabilityEvent?.Invoke(this);
					}
					continue;
				}
				goto IL_05ea;
			}
		}

		protected override async Task SendCommandRawAsync(IMyRvLinkCommand command, CancellationToken cancellationToken)
		{
			byte[] array = Enumerable.ToArray(command.Encode());
			TaggedLog.Debug("DirectConnectionMyRvLink", base.LogPrefix + " WRITE DATA " + array.DebugDump(0, array.Length));
			await WriteDataAsync(array, 0, array.Length, cancellationToken).ConfigureAwait(false);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(33, 6);
			defaultInterpolatedStringHandler.AppendFormatted("DirectConnectionMyRvLink");
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendFormatted(base.LogPrefix);
			defaultInterpolatedStringHandler.AppendLiteral(" Connection: ");
			defaultInterpolatedStringHandler.AppendFormatted(BleGuid);
			defaultInterpolatedStringHandler.AppendLiteral("(");
			defaultInterpolatedStringHandler.AppendFormatted(BleName);
			defaultInterpolatedStringHandler.AppendLiteral(") Gateway: ");
			defaultInterpolatedStringHandler.AppendFormatted(base.GatewayInfo?.ToString() ?? "Gateway Connection Info Not Loaded Yet");
			defaultInterpolatedStringHandler.AppendLiteral(" Tags: ");
			defaultInterpolatedStringHandler.AppendFormatted(LogicalDeviceTagManager.DebugTagsAsString(base.ConnectionTagList));
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		private ILogicalDeviceTag MakeDeviceSourceTag()
		{
			return new LogicalDeviceTagSourceMyRvLinkBle(BleName ?? BleGuid.ToString(), BleGuid);
		}
	}
}
