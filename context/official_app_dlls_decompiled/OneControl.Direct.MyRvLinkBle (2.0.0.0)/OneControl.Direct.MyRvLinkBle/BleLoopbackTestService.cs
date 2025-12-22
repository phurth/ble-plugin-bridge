using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ids.portable.ble.Ble;
using ids.portable.ble.BleManager;
using IDS.Portable.Common;
using IDS.Portable.Common.COBS;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLinkBle
{
	public class BleLoopbackTestService : BackgroundOperationService<BleLoopbackTestService>
	{
		private readonly IBleService _bleService;

		private const int ConnectionErrorRetryTimeMs = 1000;

		private const int ConnectionReceiveDataTimeMs = 20000;

		private const int BleRequestedMtuSize = 185;

		public static readonly Guid BleServiceGuid = Guid.Parse("00000000-7DC6-4253-9C17-202022F5AA1A");

		private static readonly Guid _bleReadCharacteristic = Guid.Parse("00000002-7DC6-4253-9C17-202022F5AA1A");

		private static readonly Guid _bleWriteCharacteristic = Guid.Parse("00000001-7DC6-4253-9C17-202022F5AA1A");

		public static readonly Guid BleGuid = Guid.Parse("00000000-0000-0000-0000-840d8ee0b096");

		public static readonly string BleName = "BLE_RB";

		protected readonly CobsEncoder CobsEncoder = new CobsEncoder(prependStartFrame: true, useCrc: true, 0);

		protected readonly CobsDecoder CobsDecoder = new CobsDecoder(useCrc: true, 0);

		protected string LogTag { get; } = "BleLoopbackTestService";


		public BleLoopbackTestService(IBleService bleService)
		{
			_bleService = bleService;
		}

		protected override async Task BackgroundOperationAsync(CancellationToken cancellationToken)
		{
			byte[] readBuffer = new byte[255];
			BleStream bleStream = new BleStream(_bleService.Manager, BleServiceGuid, _bleReadCharacteristic, _bleWriteCharacteristic, 185, limitWriteSizeToMtuSize: false);
			CobsStream cobsStream = new CobsStream(bleStream, CobsEncoder, CobsDecoder);
			string logTag = LogTag;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(20, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Start service for ");
			defaultInterpolatedStringHandler.AppendFormatted(BleGuid);
			defaultInterpolatedStringHandler.AppendLiteral("(");
			defaultInterpolatedStringHandler.AppendFormatted(BleName);
			defaultInterpolatedStringHandler.AppendLiteral(")");
			TaggedLog.Debug(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					string logTag2 = LogTag;
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(25, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Opening Connection for ");
					defaultInterpolatedStringHandler.AppendFormatted(BleGuid);
					defaultInterpolatedStringHandler.AppendLiteral("(");
					defaultInterpolatedStringHandler.AppendFormatted(BleName);
					defaultInterpolatedStringHandler.AppendLiteral(")");
					TaggedLog.Debug(logTag2, defaultInterpolatedStringHandler.ToStringAndClear());
					await bleStream.OpenAsync(BleGuid, BleName, cancellationToken);
					TaggedLog.Debug(LogTag, "Opened Connection");
					while (!cancellationToken.IsCancellationRequested)
					{
						int length = await cobsStream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken, TimeSpan.FromMilliseconds(20000.0));
						TaggedLog.Information(LogTag, "READ DATA " + readBuffer.DebugDump(0, length));
					}
				}
				catch (Exception ex)
				{
					string logTag3 = LogTag;
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(25, 3);
					defaultInterpolatedStringHandler.AppendLiteral("Error BLE Connection ");
					defaultInterpolatedStringHandler.AppendFormatted(BleGuid);
					defaultInterpolatedStringHandler.AppendLiteral("(");
					defaultInterpolatedStringHandler.AppendFormatted(BleName);
					defaultInterpolatedStringHandler.AppendLiteral("): ");
					defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
					TaggedLog.Debug(logTag3, defaultInterpolatedStringHandler.ToStringAndClear());
					await TaskExtension.TryDelay(1000, cancellationToken);
				}
				cobsStream.Close();
			}
			string logTag4 = LogTag;
			defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(22, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Stopped service for ");
			defaultInterpolatedStringHandler.AppendFormatted(BleGuid);
			defaultInterpolatedStringHandler.AppendLiteral("(");
			defaultInterpolatedStringHandler.AppendFormatted(BleName);
			defaultInterpolatedStringHandler.AppendLiteral(")");
			TaggedLog.Debug(logTag4, defaultInterpolatedStringHandler.ToStringAndClear());
		}
	}
}
