using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ids.portable.ble.Exceptions;
using IDS.Portable.Common;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;

namespace ids.portable.ble.BleManager
{
	public class BleStream : Stream, IAsyncValueStream
	{
		private const string LogTag = "BleStream";

		public const int MinimumBleMtuSize = 23;

		public const int DefaultBleMtuSize = 185;

		private const int DefaultConnectTimeoutMs = 30000;

		private const int MaxReadBuffers = 1024;

		private readonly IBleManager _bleManager;

		private readonly Guid? _readCharacteristicGuid;

		private readonly Guid? _writeCharacteristicGuid;

		private bool _didOpenConnection;

		private ICharacteristic? _readCharacteristic;

		private ICharacteristic? _writeCharacteristic;

		private readonly object _locker = new object();

		private CancellationTokenSource? _openConnectionBackgroundCts;

		private readonly int _requestedMtuSize;

		private readonly ArrayPool<byte> _writeArrayPool = ArrayPool<byte>.Create();

		private readonly ArrayPool<byte> _readArrayPool = ArrayPool<byte>.Create();

		private readonly Queue<(byte[] buffer, int size)> _readQueue = new Queue<(byte[], int)>(1024);

		private TaskCompletionSource<bool> _readDataAvailableTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		public const int MaxWriteCharacteristicQueueSize = 20;

		private readonly TaskSerialQueue _openOperationQueue = new TaskSerialQueue(20);

		public Guid ServiceGuid { get; }

		public BleStreamOption Options { get; }

		public IDevice? ConnectedBleDevice { get; private set; }

		public IService? BleService { get; private set; }

		public int ActualMtuSize { get; private set; }

		public bool LimitWriteSizeToMtuSize { get; }

		public bool IsDisposed { get; private set; }

		public override bool CanRead => _readCharacteristicGuid.HasValue;

		public override bool CanWrite => _writeCharacteristicGuid.HasValue;

		public override bool CanSeek => false;

		public override long Length
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public override long Position
		{
			get
			{
				throw new NotSupportedException();
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		public BleStream(IBleManager bleManager, Guid serviceGuid, Guid readCharacteristicGuid)
			: this(bleManager, serviceGuid, readCharacteristicGuid, null)
		{
		}

		public BleStream(IBleManager bleManager, Guid serviceGuid, Guid? readCharacteristicGuid, Guid? writeCharacteristicGuid, int requestedMtuSize = 185, bool limitWriteSizeToMtuSize = true, BleStreamOption option = BleStreamOption.None)
		{
			Options = option;
			_bleManager = bleManager ?? throw new ArgumentNullException("bleManager");
			ServiceGuid = serviceGuid;
			if (!readCharacteristicGuid.HasValue && !writeCharacteristicGuid.HasValue)
			{
				throw new ArgumentException("Must specify at least one characteristic.  readCharacteristicGuid and writeCharacteristicGuid can't both be null!");
			}
			_readCharacteristicGuid = readCharacteristicGuid;
			_writeCharacteristicGuid = writeCharacteristicGuid;
			_openConnectionBackgroundCts = null;
			_requestedMtuSize = requestedMtuSize;
			LimitWriteSizeToMtuSize = limitWriteSizeToMtuSize;
			_bleManager.DeviceDisconnected += BleAdapterOnDeviceDisconnected;
			_bleManager.DeviceConnectionLost += BleAdapterOnDeviceDisconnected;
		}

		private void BleAdapterOnDeviceDisconnected(object? sender, DeviceEventArgs args)
		{
			lock (_locker)
			{
				if (ConnectedBleDevice != null)
				{
					IDevice device = args.Device;
					if (device != null && !(ConnectedBleDevice!.Id != device.Id))
					{
						string text = ((args is DeviceErrorEventArgs deviceErrorEventArgs) ? deviceErrorEventArgs.ErrorMessage : "Device Disconnected");
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(68, 3);
						defaultInterpolatedStringHandler.AppendLiteral("BLE Auto closing connection for ");
						defaultInterpolatedStringHandler.AppendFormatted(device.Id);
						defaultInterpolatedStringHandler.AppendLiteral("(");
						defaultInterpolatedStringHandler.AppendFormatted(device.Name);
						defaultInterpolatedStringHandler.AppendLiteral(") because adapter connection lost: ");
						defaultInterpolatedStringHandler.AppendFormatted(text);
						TaggedLog.Debug("BleStream", defaultInterpolatedStringHandler.ToStringAndClear());
						Close();
					}
				}
			}
		}

		protected virtual Task<IDevice?> ConnectToDeviceAsync(Guid deviceId, CancellationToken cancellationToken)
		{
			return _bleManager.TryConnectToDeviceAsync(deviceId, cancellationToken);
		}

		public async Task OpenAsync(Guid deviceId, string deviceName, CancellationToken openCancellationToken, TimeSpan? connectionTimeout = null)
		{
			using CancellationTokenSource timeoutCts = new CancellationTokenSource(connectionTimeout ?? TimeSpan.FromMilliseconds(30000.0));
			using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(openCancellationToken, timeoutCts.Token);
			CancellationToken cancellationToken;
			lock (_locker)
			{
				Close();
				_openConnectionBackgroundCts?.TryCancelAndDispose();
				_openConnectionBackgroundCts = cts;
				cancellationToken = cts.Token;
			}
			using (await _openOperationQueue.GetLockAsync(cancellationToken))
			{
				IDevice connectedBleDevice = null;
				try
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(43, 2);
					defaultInterpolatedStringHandler.AppendLiteral("BLEStream: Attempt Connection To Device ");
					defaultInterpolatedStringHandler.AppendFormatted(deviceId);
					defaultInterpolatedStringHandler.AppendLiteral(" (");
					defaultInterpolatedStringHandler.AppendFormatted(deviceName);
					defaultInterpolatedStringHandler.AppendLiteral(")");
					TaggedLog.Debug("BleStream", defaultInterpolatedStringHandler.ToStringAndClear());
					connectedBleDevice = await ConnectToDeviceAsync(deviceId, cancellationToken);
					if (connectedBleDevice == null)
					{
						throw new Exception("Unable to connect to device (connection returned null)");
					}
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(42, 1);
					defaultInterpolatedStringHandler.AppendLiteral("BLEStream: Connected, requesting MTU size ");
					defaultInterpolatedStringHandler.AppendFormatted(_requestedMtuSize);
					TaggedLog.Debug("BleStream", defaultInterpolatedStringHandler.ToStringAndClear());
					int num = await connectedBleDevice.RequestMtuAsync(_requestedMtuSize);
					if (num <= 0)
					{
						num = 23;
						defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(49, 3);
						defaultInterpolatedStringHandler.AppendLiteral("RequestMtuAsync ");
						defaultInterpolatedStringHandler.AppendFormatted(deviceId);
						defaultInterpolatedStringHandler.AppendLiteral("(");
						defaultInterpolatedStringHandler.AppendFormatted(deviceName);
						defaultInterpolatedStringHandler.AppendLiteral("): Failed, assuming mtu size of ");
						defaultInterpolatedStringHandler.AppendFormatted(23);
						TaggedLog.Debug("BleStream", defaultInterpolatedStringHandler.ToStringAndClear());
					}
					ActualMtuSize = num;
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(46, 3);
					defaultInterpolatedStringHandler.AppendLiteral("BLE Connection ");
					defaultInterpolatedStringHandler.AppendFormatted(deviceId);
					defaultInterpolatedStringHandler.AppendLiteral("(");
					defaultInterpolatedStringHandler.AppendFormatted(deviceName);
					defaultInterpolatedStringHandler.AppendLiteral("): Connected with MTU size of ");
					defaultInterpolatedStringHandler.AppendFormatted(ActualMtuSize);
					TaggedLog.Debug("BleStream", defaultInterpolatedStringHandler.ToStringAndClear());
					var (bleService, readCharacteristic, writeCharacteristic) = await InternalOpenServiceAndCharacteristic(connectedBleDevice, cancellationToken);
					lock (_locker)
					{
						cancellationToken.ThrowIfCancellationRequested();
						BleService = bleService;
						ConnectedBleDevice = connectedBleDevice;
						_didOpenConnection = true;
						_readCharacteristic = readCharacteristic;
						_writeCharacteristic = writeCharacteristic;
					}
				}
				catch (Exception ex)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(29, 3);
					defaultInterpolatedStringHandler.AppendLiteral("Error opening connection ");
					defaultInterpolatedStringHandler.AppendFormatted(deviceId);
					defaultInterpolatedStringHandler.AppendLiteral("(");
					defaultInterpolatedStringHandler.AppendFormatted(deviceName);
					defaultInterpolatedStringHandler.AppendLiteral("): ");
					defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
					TaggedLog.Debug("BleStream", defaultInterpolatedStringHandler.ToStringAndClear());
					if (connectedBleDevice != null)
					{
						_bleManager.DisconnectDeviceAsync(connectedBleDevice).TryAwaitAsync();
					}
					lock (_locker)
					{
						ConnectedBleDevice = null;
						_didOpenConnection = false;
						BleService = null;
						_readCharacteristic = null;
						_writeCharacteristic = null;
					}
					throw;
				}
			}
		}

		private async Task<(IService service, ICharacteristic? readCharacteristic, ICharacteristic? writeCharacteristic)> InternalOpenServiceAndCharacteristic(IDevice connectedBleDevice, CancellationToken cancellationToken)
		{
			ICharacteristic readCharacteristic = null;
			ICharacteristic writeCharacteristic = null;
			IService bleService = null;
			try
			{
				IService obj = await _bleManager.GetServiceAsync(connectedBleDevice, ServiceGuid, cancellationToken);
				if (obj == null)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(22, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Unable to get service ");
					defaultInterpolatedStringHandler.AppendFormatted(ServiceGuid);
					throw new BleServiceException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				bleService = obj;
				if (_readCharacteristicGuid.HasValue)
				{
					readCharacteristic = await _bleManager.GetCharacteristicAsync(connectedBleDevice, bleService, _readCharacteristicGuid.Value, cancellationToken);
					if (readCharacteristic == null)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(60, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Unable to get read characteristic ");
						defaultInterpolatedStringHandler.AppendFormatted(_readCharacteristicGuid);
						defaultInterpolatedStringHandler.AppendLiteral(", stream won't be readable");
						TaggedLog.Warning("BleStream", defaultInterpolatedStringHandler.ToStringAndClear());
					}
					else
					{
						readCharacteristic.ValueUpdated += ReadCharacteristicOnValueUpdated;
						try
						{
							if (!(await _bleManager.StartCharacteristicUpdatesAsync(readCharacteristic)))
							{
								throw new BleServiceException("Unable to subscribe to read updates for an unknown reason");
							}
						}
						catch (Exception ex)
						{
							TaggedLog.Error("BleStream", "Unable to Subscribe to read Characteristics: " + ex.Message);
							CloseReadCharacteristic(readCharacteristic);
							readCharacteristic = null;
						}
					}
				}
				if (_writeCharacteristicGuid.HasValue)
				{
					writeCharacteristic = await _bleManager.GetCharacteristicAsync(connectedBleDevice, bleService, _writeCharacteristicGuid.Value, cancellationToken);
					if (writeCharacteristic == null)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(60, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Unable to get Write characteristic ");
						defaultInterpolatedStringHandler.AppendFormatted(_writeCharacteristicGuid);
						defaultInterpolatedStringHandler.AppendLiteral(", stream will be writable");
						TaggedLog.Warning("BleStream", defaultInterpolatedStringHandler.ToStringAndClear());
					}
				}
			}
			catch
			{
				CloseReadCharacteristic(readCharacteristic);
				bleService?.TryDispose();
				throw;
			}
			return (bleService, readCharacteristic, writeCharacteristic);
		}

		public override void Close()
		{
			lock (_locker)
			{
				_openConnectionBackgroundCts?.TryCancelAndDispose();
				CloseReadCharacteristic(_readCharacteristic);
				if (ConnectedBleDevice != null && _didOpenConnection && !Options.HasFlag(BleStreamOption.DisableDisconnectDeviceOnClose))
				{
					_bleManager.DisconnectDeviceAsync(ConnectedBleDevice).TryAwaitAsync();
				}
				(byte[], int) tuple;
				while (_readQueue.TryDequeue(out tuple))
				{
					_readArrayPool.Return(tuple.Item1);
				}
				BleService?.TryDispose();
				ConnectedBleDevice = null;
				_readCharacteristic = null;
				_writeCharacteristic = null;
			}
		}

		private void CloseReadCharacteristic(ICharacteristic? readCharacteristic)
		{
			lock (_locker)
			{
				if (readCharacteristic == null)
				{
					return;
				}
				try
				{
					readCharacteristic!.ValueUpdated -= ReadCharacteristicOnValueUpdated;
				}
				catch
				{
				}
				try
				{
					_bleManager.StopCharacteristicUpdates(readCharacteristic);
				}
				catch
				{
				}
			}
		}

		protected override void Dispose(bool disposing)
		{
			Close();
			try
			{
				_bleManager.DeviceDisconnected -= BleAdapterOnDeviceDisconnected;
			}
			catch
			{
			}
			try
			{
				_bleManager.DeviceConnectionLost -= BleAdapterOnDeviceDisconnected;
			}
			catch
			{
			}
			IsDisposed = true;
			_readDataAvailableTcs.TrySetException(new ObjectDisposedException("BleStream"));
			base.Dispose(disposing);
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return ReadAsync(buffer, offset, count, cancellationToken, null).AsTask();
		}

		[AsyncStateMachine(typeof(_003CReadAsync_003Ed__55))]
		public ValueTask<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken, TimeSpan? readTimeout)
		{
			_003CReadAsync_003Ed__55 _003CReadAsync_003Ed__ = default(_003CReadAsync_003Ed__55);
			_003CReadAsync_003Ed__._003C_003Et__builder = AsyncValueTaskMethodBuilder<int>.Create();
			_003CReadAsync_003Ed__._003C_003E4__this = this;
			_003CReadAsync_003Ed__.buffer = buffer;
			_003CReadAsync_003Ed__.offset = offset;
			_003CReadAsync_003Ed__.count = count;
			_003CReadAsync_003Ed__.cancellationToken = cancellationToken;
			_003CReadAsync_003Ed__.readTimeout = readTimeout;
			_003CReadAsync_003Ed__._003C_003E1__state = -1;
			_003CReadAsync_003Ed__._003C_003Et__builder.Start(ref _003CReadAsync_003Ed__);
			return _003CReadAsync_003Ed__._003C_003Et__builder.Task;
		}

		private void ReadCharacteristicOnValueUpdated(object? sender, CharacteristicUpdatedEventArgs args)
		{
			byte[] value = args.Characteristic.Value;
			if (value == null || value.Length == 0)
			{
				return;
			}
			lock (_locker)
			{
				if (ConnectedBleDevice != null)
				{
					(byte[], int) tuple;
					while (_readQueue.Count >= 1024 && _readQueue.TryDequeue(out tuple))
					{
						TaggedLog.Debug("BleStream", "Read Buffer Overflow, throwing out oldest message");
						_readArrayPool.Return(tuple.Item1);
					}
					byte[] array = _readArrayPool.Rent(value.Length);
					Buffer.BlockCopy(value, 0, array, 0, value.Length);
					_readQueue.Enqueue((array, value.Length));
					_readDataAvailableTcs.TrySetResult(true);
				}
			}
		}

		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			ICharacteristic writeCharacteristic;
			lock (_locker)
			{
				writeCharacteristic = _writeCharacteristic;
				if (writeCharacteristic == null)
				{
					throw new IOException("Unable to write because stream is readonly (no write characteristic configured)");
				}
			}
			if (LimitWriteSizeToMtuSize && count > ActualMtuSize)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(55, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Can't send more data than what fits in a single MTU ");
				defaultInterpolatedStringHandler.AppendFormatted(count);
				defaultInterpolatedStringHandler.AppendLiteral(" > ");
				defaultInterpolatedStringHandler.AppendFormatted(ActualMtuSize);
				throw new ArgumentOutOfRangeException("count", defaultInterpolatedStringHandler.ToStringAndClear());
			}
			int sentBytes = 0;
			while (sentBytes < count)
			{
				byte[] rentedWriteBuffer = _writeArrayPool.Rent(ActualMtuSize);
				try
				{
					int bytesToCopy = Math.Min(count - sentBytes, ActualMtuSize);
					byte[] array = ((bytesToCopy == rentedWriteBuffer.Length) ? rentedWriteBuffer : new byte[bytesToCopy]);
					Buffer.BlockCopy(buffer, offset + sentBytes, array, 0, bytesToCopy);
					if (!Options.HasFlag(BleStreamOption.WriteWithoutResponse))
					{
						await _bleManager.WriteCharacteristicWithResponseAsync(writeCharacteristic, array, cancellationToken);
					}
					else
					{
						await _bleManager.WriteCharacteristicAsync(writeCharacteristic, array, cancellationToken);
					}
					sentBytes += bytesToCopy;
				}
				finally
				{
					_writeArrayPool.Return(rentedWriteBuffer);
				}
			}
		}

		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			Flush();
			return Task.FromResult(true);
		}

		public override void Flush()
		{
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException("Use ReadAsync");
		}

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
		{
			throw new NotSupportedException("Use ReadAsync");
		}

		public override int EndRead(IAsyncResult asyncResult)
		{
			throw new NotSupportedException("Use ReadAsync");
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException("Use WriteAsync");
		}

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
		{
			throw new NotSupportedException("Use WriteAsync");
		}

		public override void EndWrite(IAsyncResult asyncResult)
		{
			throw new NotSupportedException("Use WriteAsync");
		}
	}
}
