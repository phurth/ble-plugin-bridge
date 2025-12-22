using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.Types;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.FirmwareUpdate;

namespace OneControl.Devices
{
	public class FirmwareUpdateSim
	{
		private int _totalRetryCount;

		private int _currentCommandRetryCount;

		public const int SimBlockSize = 2048;

		public const int SimSendDelayTimeMs = 200;

		public const int SimMinBufferSize = 1024;

		public const int SimMaxBufferSize = 2097152;

		private const int ResendPercent = 5;

		public ILogicalDevice LogicalDevice { get; }

		public bool RequireStartAddress { get; set; } = true;


		public FirmwareUpdateSim(ILogicalDevice logicalDevice)
		{
			LogicalDevice = logicalDevice;
		}

		public Task<FirmwareUpdateSupport> TryGetFirmwareUpdateSupportAsync(CancellationToken cancelToken)
		{
			return Task.FromResult(FirmwareUpdateSupport.SupportedViaDevice);
		}

		public async Task UpdateFirmwareAsync(IReadOnlyList<byte> data, Func<ILogicalDeviceTransferProgress, bool> progressAck, CancellationToken cancellationToken, Dictionary<FirmwareUpdateOption, object>? options = null)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			if (progressAck == null)
			{
				throw new ArgumentNullException("progressAck");
			}
			if (data.Count < 1024)
			{
				throw new FirmwareUpdateTooSmallException(LogicalDevice, data.Count);
			}
			if (data.Count >= 2097152)
			{
				throw new FirmwareUpdateTooBigException(LogicalDevice, data.Count);
			}
			FirmwareUpdateSupport firmwareUpdateSupport = await TryGetFirmwareUpdateSupportAsync(cancellationToken);
			if (!firmwareUpdateSupport.IsSupported())
			{
				throw new FirmwareUpdateNotSupportedException(LogicalDevice, firmwareUpdateSupport);
			}
			if (RequireStartAddress)
			{
				if (options == null || options!.Count == 0)
				{
					throw new FirmwareUpdateMissingRequiredOptionException(LogicalDevice, FirmwareUpdateOption.StartAddress);
				}
				if (!options.TryGetStartAddress(out var _))
				{
					throw new FirmwareUpdateInvalidOptionException(LogicalDevice, FirmwareUpdateOption.StartAddress);
				}
			}
			cancellationToken.ThrowIfCancellationRequested();
			Stopwatch timer = Stopwatch.StartNew();
			Random random = new Random();
			await Task.Delay(200, cancellationToken);
			for (int bytesSent = 0; bytesSent < data.Count; bytesSent += 2048)
			{
				int num;
				do
				{
					if (_currentCommandRetryCount != 0)
					{
						_totalRetryCount++;
					}
					LogicalDeviceTransferProgress logicalDeviceTransferProgress = new LogicalDeviceTransferProgress((UInt48)bytesSent, (UInt48)_totalRetryCount, (UInt48)_currentCommandRetryCount, TimeSpan.FromMilliseconds(timer.ElapsedMilliseconds));
					if (!progressAck(logicalDeviceTransferProgress))
					{
						throw new OperationCanceledException();
					}
					await Task.Delay(200, cancellationToken);
					num = random.Next(100);
					_currentCommandRetryCount++;
				}
				while (num <= 5);
				_currentCommandRetryCount = 0;
			}
			if (!progressAck(new LogicalDeviceTransferProgress((UInt48)data.Count, (UInt48)_totalRetryCount, (UInt48)_currentCommandRetryCount, TimeSpan.FromMilliseconds(timer.ElapsedMilliseconds))))
			{
				throw new OperationCanceledException();
			}
		}
	}
}
