using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using IDS.Core.Events;
using IDS.Core.Tasks;
using IDS.Core.Types;

namespace IDS.Core.IDS_CAN
{
	public class LocalDevice : DisposableManager, ILocalDevice, IDevice, IBusEndpoint, IUniqueDeviceInfo, IUniqueProductInfo, ILocalDeviceAsyncMessaging, IPidClient, IBlockClient, IEventSender, IDisposableManager, IDisposable, System.IDisposable
	{
		private class ReusableSubscription : ResourcePool.Object
		{
			private readonly object Mutex = new object();

			private LocalDevice Device;

			private SubscriptionToken Subscription;

			private Func<LocalDeviceRxEvent, bool> RxValidator;

			protected override void ResetPoolObjectState()
			{
				lock (Mutex)
				{
					RxValidator = null;
				}
			}

			public void SetDelegate(LocalDevice device, Func<LocalDeviceRxEvent, bool> rx_validator)
			{
				lock (Mutex)
				{
					Retain();
					if (RxValidator != null)
					{
						throw new InvalidOperationException("ReusableLocalDeviceRxEvent delegate is already in use");
					}
					RxValidator = rx_validator;
					if (Subscription == null)
					{
						Device = device;
						Subscription = device.Events.Subscribe<LocalDeviceRxEvent>(OnLocalDeviceRxEvent, SubscriptionType.Weak);
					}
				}
			}

			private void OnLocalDeviceRxEvent(LocalDeviceRxEvent rx)
			{
				if (RxValidator == null)
				{
					return;
				}
				lock (Mutex)
				{
					Func<LocalDeviceRxEvent, bool> rxValidator = RxValidator;
					if (rxValidator != null && rxValidator(rx))
					{
						RxValidator = null;
						ReturnToPool();
					}
				}
			}
		}

		private class BlockClient : IBlockClient
		{
			private class BlockOperationProgressReporter
			{
				public enum TYPE
				{
					READER,
					WRITER
				}

				public readonly AsyncOperation Operation;

				public readonly IBlock Block;

				public readonly LocalDevice Client;

				public readonly IDevice Target;

				private readonly Timer UpdateTime = new Timer();

				private readonly Timer OperationTime = new Timer();

				private readonly string s1;

				private readonly string s2;

				public BlockOperationProgressReporter(AsyncOperation operation, LocalDevice client, IBlock block, TYPE type)
				{
					Operation = operation;
					Block = block;
					Client = client;
					Target = block.Device;
					if (type == TYPE.READER)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(30, 2);
						defaultInterpolatedStringHandler.AppendLiteral("Reading block <");
						defaultInterpolatedStringHandler.AppendFormatted(block.ID);
						defaultInterpolatedStringHandler.AppendLiteral("> from device @");
						defaultInterpolatedStringHandler.AppendFormatted(block.Device.Address);
						s1 = defaultInterpolatedStringHandler.ToStringAndClear();
						s2 = "Download";
					}
					else
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(28, 2);
						defaultInterpolatedStringHandler.AppendLiteral("Writing block <");
						defaultInterpolatedStringHandler.AppendFormatted(block.ID);
						defaultInterpolatedStringHandler.AppendLiteral("> to device @");
						defaultInterpolatedStringHandler.AppendFormatted(block.Device.Address);
						s1 = defaultInterpolatedStringHandler.ToStringAndClear();
						s2 = "Upload";
					}
					UpdateTime.ElapsedTime = TimeSpan.FromSeconds(1.0);
				}

				public void ReportProgress(ulong num_bytes, ulong total_bytes)
				{
					if (UpdateTime.ElapsedTime.TotalMilliseconds >= 500.0)
					{
						UpdateTime.Reset();
						double totalSeconds = OperationTime.ElapsedTime.TotalSeconds;
						if (num_bytes == 0L || total_bytes == 0L || totalSeconds < 1.0)
						{
							AsyncOperation operation = Operation;
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(64, 5);
							defaultInterpolatedStringHandler.AppendFormatted(s1);
							defaultInterpolatedStringHandler.AppendLiteral(" (");
							defaultInterpolatedStringHandler.AppendFormatted($"{num_bytes:#,###0}");
							defaultInterpolatedStringHandler.AppendLiteral(" of ");
							defaultInterpolatedStringHandler.AppendFormatted($"{total_bytes:#,###0}");
							defaultInterpolatedStringHandler.AppendLiteral(" bytes)\n");
							defaultInterpolatedStringHandler.AppendFormatted(s2);
							defaultInterpolatedStringHandler.AppendLiteral(" speed = ???\nElapsed Time = ");
							defaultInterpolatedStringHandler.AppendFormatted(OperationTime);
							defaultInterpolatedStringHandler.AppendLiteral(", Remaining Time = ???");
							operation.ReportProgress(0f, defaultInterpolatedStringHandler.ToStringAndClear());
						}
						else
						{
							float percent_complete = 100f * (float)num_bytes / (float)total_bytes;
							double num = (double)num_bytes / totalSeconds;
							TimeSpan span = TimeSpan.FromSeconds((double)total_bytes / num - totalSeconds);
							AsyncOperation operation2 = Operation;
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(75, 7);
							defaultInterpolatedStringHandler.AppendFormatted(s1);
							defaultInterpolatedStringHandler.AppendLiteral(" (");
							defaultInterpolatedStringHandler.AppendFormatted($"{num_bytes:#,###0}");
							defaultInterpolatedStringHandler.AppendLiteral(" of ");
							defaultInterpolatedStringHandler.AppendFormatted($"{total_bytes:#,###0}");
							defaultInterpolatedStringHandler.AppendLiteral(" bytes)\n");
							defaultInterpolatedStringHandler.AppendFormatted(s2);
							defaultInterpolatedStringHandler.AppendLiteral(" speed = ");
							defaultInterpolatedStringHandler.AppendFormatted($"{Math.Round(num):#,###0}");
							defaultInterpolatedStringHandler.AppendLiteral(" bytes per second\nElapsed Time = ");
							defaultInterpolatedStringHandler.AppendFormatted(OperationTime);
							defaultInterpolatedStringHandler.AppendLiteral(", Remaining Time = ");
							defaultInterpolatedStringHandler.AppendFormatted(Timer.FormatString(span));
							operation2.ReportProgress(percent_complete, defaultInterpolatedStringHandler.ToStringAndClear());
						}
					}
				}
			}

			private class BlockReadManager : BlockOperationProgressReporter
			{
				private byte Delay_ms;

				private byte[] Buffer;

				private uint Offset;

				public bool TransferComplete { get; private set; }

				public bool CrcValid
				{
					get
					{
						if (!TransferComplete)
						{
							return false;
						}
						if (Buffer.Length == 0)
						{
							return true;
						}
						Array.Resize(ref Buffer, (int)Block.Size);
						uint num = CRC32_LE.Calculate(Buffer, Buffer.Length, 0u);
						return Block.CRC == num;
					}
				}

				public BlockReadManager(AsyncOperation operation, LocalDevice client, IBlock block, int bulk_xfer_delay_ms)
					: base(operation, client, block, TYPE.READER)
				{
					if (!block.IsReadable())
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(20, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Block <");
						defaultInterpolatedStringHandler.AppendFormatted(block.ID);
						defaultInterpolatedStringHandler.AppendLiteral(">is read-only");
						throw new NotImplementedException(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					if (bulk_xfer_delay_ms >= 255)
					{
						Delay_ms = byte.MaxValue;
					}
					else if (bulk_xfer_delay_ms >= 0)
					{
						Delay_ms = (byte)bulk_xfer_delay_ms;
					}
					else
					{
						Delay_ms = 0;
					}
					uint num = (uint)((int)Block.Size + 7) / 8u * 8;
					Buffer = new byte[num];
				}

				private async Task<RESPONSE> ReadPartialBlockAsync(bool BlockUseSession, ISessionClient session)
				{
					if (Offset >= Block.Size)
					{
						return RESPONSE.VALUE_OUT_OF_RANGE;
					}
					uint num = (uint)(Block.Size - Offset);
					uint val = 1 + num / 8u;
					int estimated_xfers = (int)Math.Min(255u, val);
					new object();
					TaskCompletionSource<RESPONSE> tcs = new TaskCompletionSource<RESPONSE>();
					bool message_sent = false;
					uint Read_xfer_size = 0u;
					uint bytes_read = 0u;
					byte sequence = 0;
					bool BulkSuccess = false;
					bool WaitFewSecondsToMute = false;
					ReusableSubscription rx_listener = Client.ReusableSubscriptionPool.Get();
					rx_listener.SetDelegate(Client, delegate(LocalDeviceRxEvent rx)
					{
						if (!message_sent)
						{
							return false;
						}
						if (rx.SourceAddress != Target.Address)
						{
							return false;
						}
						ReportProgress(Offset + bytes_read, Block.Size);
						switch ((byte)rx.MessageType)
						{
						case 129:
							switch (rx.MessageData)
							{
							case 34:
								if (rx.Length == 1)
								{
									tcs.SetResult((RESPONSE)rx[0]);
									return true;
								}
								if (rx.Length >= 7 && rx.GetUINT16(0) == (ushort)Block.ID && rx.GetUINT32(2) == Offset)
								{
									if (rx.Length == 7)
									{
										tcs.SetResult((RESPONSE)rx[6]);
										return true;
									}
									if (rx.Length == 8)
									{
										if ((Block.Flags & BLOCK_FLAGS.USE_SET_SIZE) != 0)
										{
											if (rx.GetUINT16(6) == (Block.Size & 0xFFFF))
											{
												Read_xfer_size = (uint)Block.Size;
												if (Offset == 0)
												{
													WaitFewSecondsToMute = true;
												}
											}
										}
										else
										{
											Read_xfer_size = rx.GetUINT16(6);
										}
										int newSize = (int)(Read_xfer_size + 7) / 8 * 8;
										Array.Resize(ref Buffer, newSize);
									}
								}
								return false;
							case 37:
								if (rx.Length == 8 && rx.GetUINT16(0) == (ushort)Block.ID && rx.GetUINT16(2) == (ushort)Offset)
								{
									uint num2 = CRC32_LE.Calculate(Buffer, (int)bytes_read, Offset);
									uint uINT = rx.GetUINT32(4);
									bool flag = false;
									if (num2 != uINT)
									{
										if (CRC32_LE.Calculate(Buffer, (int)Read_xfer_size, 0u) == uINT)
										{
											flag = true;
										}
									}
									else
									{
										flag = true;
									}
									if (!flag)
									{
										BulkSuccess = false;
										tcs.SetResult(RESPONSE.IN_PROGRESS);
									}
									else
									{
										BulkSuccess = true;
										if (bytes_read + Offset >= Read_xfer_size)
										{
											tcs.SetResult(RESPONSE.SUCCESS);
										}
										else
										{
											tcs.SetResult(RESPONSE.IN_PROGRESS);
										}
									}
									return true;
								}
								return false;
							default:
								return false;
							}
						case 159:
							if (Read_xfer_size != 0 && rx.MessageData == sequence)
							{
								for (int i = 0; i < rx.Length; i++)
								{
									Buffer[Offset + bytes_read] = rx[i];
									bytes_read++;
								}
								sequence++;
							}
							return false;
						default:
							return false;
						}
					});
					try
					{
						while (Client.IsOnline && Target.IsOnline && !Operation.IsCancellationRequested && !tcs.Task.IsCompleted)
						{
							if (BlockUseSession)
							{
								session.TryOpenSession();
							}
							if (!Client.Transmit29((byte)128, 34, Target, CAN.PAYLOAD.FromArgs((ushort)Block.ID, Offset, byte.MaxValue, Delay_ms)))
							{
								await Task.Delay(5);
								continue;
							}
							message_sent = true;
							await Task.WhenAny(tcs.Task, Task.Delay(10000 + Delay_ms * estimated_xfers, Operation.CancellationToken));
						}
					}
					catch (TimeoutException)
					{
					}
					catch (OperationCanceledException)
					{
					}
					finally
					{
						rx_listener.ReturnToPool();
					}
					if (!tcs.Task.IsCompleted)
					{
						return Operation.IsCancellationRequested ? RESPONSE.CANCELLED : RESPONSE.FAILED;
					}
					if (WaitFewSecondsToMute)
					{
						if (BlockUseSession)
						{
							session.TryOpenSession();
						}
						await Task.Delay(1500);
						if (BlockUseSession)
						{
							session.TryOpenSession();
						}
						await Task.Delay(1500);
						if (BlockUseSession)
						{
							session.TryOpenSession();
						}
						WaitFewSecondsToMute = false;
					}
					if (tcs.Task.Result == RESPONSE.SUCCESS)
					{
						if (BlockUseSession)
						{
							session.TryOpenSession();
						}
						if (bytes_read + Offset >= Read_xfer_size)
						{
							TransferComplete = true;
							return RESPONSE.SUCCESS;
						}
					}
					if (tcs.Task.Result == RESPONSE.IN_PROGRESS)
					{
						if (BlockUseSession)
						{
							session.TryOpenSession();
						}
						if (Read_xfer_size >= bytes_read + Offset && bytes_read != 0)
						{
							if (BulkSuccess)
							{
								Offset += bytes_read;
								if (Delay_ms > 0)
								{
									Delay_ms--;
								}
							}
							else
							{
								Delay_ms += 2;
							}
							return RESPONSE.SUCCESS;
						}
						return RESPONSE.FAILED;
					}
					return tcs.Task.Result;
				}

				public async Task<Tuple<RESPONSE, IReadOnlyList<byte>>> ReadBlockAsync(bool BlockUseSession, ISessionClient session)
				{
					while (!TransferComplete && !Operation.IsCancellationRequested)
					{
						RESPONSE rESPONSE = await ReadPartialBlockAsync(BlockUseSession, session);
						if (rESPONSE != 0)
						{
							return Tuple.Create<RESPONSE, IReadOnlyList<byte>>(rESPONSE, null);
						}
					}
					if (!TransferComplete)
					{
						return Tuple.Create<RESPONSE, IReadOnlyList<byte>>(RESPONSE.FAILED, null);
					}
					if (!CrcValid)
					{
						return Tuple.Create<RESPONSE, IReadOnlyList<byte>>(RESPONSE.CRC_INVALID, null);
					}
					return Tuple.Create(RESPONSE.SUCCESS, (IReadOnlyList<byte>)Buffer);
				}
			}

			private class BlockWriteManager : BlockOperationProgressReporter
			{
				private readonly IReadOnlyList<byte> Data;

				private readonly uint TotalCRC;

				private int MinBulkTransferPacketDelay_ms;

				private uint TotalNumberOfBytesToWrite;

				private int BulkTransferPacketDelay_ms;

				private uint bulkXferSize;

				private uint CurrentBlockOffset;

				private uint TotalBytesRemaining;

				public BlockWriteManager(AsyncOperation operation, LocalDevice client, IBlock block, IReadOnlyList<byte> data, int bulk_xfer_delay_ms)
					: base(operation, client, block, TYPE.WRITER)
				{
					Data = data;
					MinBulkTransferPacketDelay_ms = bulk_xfer_delay_ms;
					TotalCRC = CRC32_LE.Calculate(data);
				}

				public async Task<RESPONSE> WriteBlockAsync(bool BlockUseSession, ISessionClient session)
				{
					ReportProgress(0uL, (uint)Data.Count);
					RESPONSE rESPONSE = await Request23BeginBlockWriteAsync(BlockUseSession, session);
					if (rESPONSE == RESPONSE.BUSY)
					{
						await Request26EndBlockWriteAsync(BlockUseSession, session);
						rESPONSE = await Request23BeginBlockWriteAsync(BlockUseSession, session);
					}
					if (rESPONSE != 0)
					{
						return rESPONSE;
					}
					if (Data.Count != TotalNumberOfBytesToWrite)
					{
						return RESPONSE.FAILED;
					}
					CurrentBlockOffset = 0u;
					TotalBytesRemaining = TotalNumberOfBytesToWrite;
					if (TotalNumberOfBytesToWrite != 0)
					{
						if (BlockUseSession)
						{
							session.TryOpenSession();
						}
						await Task.Delay(1500);
						if (BlockUseSession)
						{
							session.TryOpenSession();
						}
						await Task.Delay(1500);
						if (BlockUseSession)
						{
							session.TryOpenSession();
						}
					}
					while (TotalBytesRemaining != 0)
					{
						if (Operation.IsCancellationRequested)
						{
							return RESPONSE.CANCELLED;
						}
						rESPONSE = await Request24BeginBulkTransferAsync();
						if (rESPONSE != 0 || bulkXferSize == 0)
						{
							return rESPONSE;
						}
						if (bulkXferSize == 0 || bulkXferSize > TotalBytesRemaining)
						{
							return RESPONSE.FAILED;
						}
						if (await DoBulkTransferAsync((int)bulkXferSize, (int)CurrentBlockOffset) != 0)
						{
							BulkTransferPacketDelay_ms += 10;
						}
						else if (BulkTransferPacketDelay_ms > MinBulkTransferPacketDelay_ms)
						{
							BulkTransferPacketDelay_ms--;
						}
						if (BlockUseSession)
						{
							session.TryOpenSession();
						}
						if (await Request25EndBulkTransferAsync(BlockUseSession, session) == RESPONSE.SUCCESS)
						{
							CurrentBlockOffset += bulkXferSize;
							TotalBytesRemaining -= bulkXferSize;
						}
					}
					if (BlockUseSession)
					{
						session.TryOpenSession();
					}
					return await Request26EndBlockWriteAsync(BlockUseSession, session);
				}

				private async Task<RESPONSE> Request23BeginBlockWriteAsync(bool BlockUseSession, ISessionClient session)
				{
					Tuple<RESPONSE, CAN.MessageBuffer> result = null;
					try
					{
						uint count = (uint)Data.Count;
						result = await Client.TransmitRequestAsync(Operation, Target, (byte)35, CAN.PAYLOAD.FromArgs((ushort)Block.ID, count), delegate(LocalDeviceRxEvent rx)
						{
							if (rx.Length < 2)
							{
								return null;
							}
							if (rx.GetUINT16(0) != (ushort)Block.ID)
							{
								return null;
							}
							if (rx.Length == 3)
							{
								RESPONSE rESPONSE = (RESPONSE)rx[2];
								if (rESPONSE == RESPONSE.IN_PROGRESS)
								{
									if (BlockUseSession)
									{
										session.TryOpenSession();
									}
									return null;
								}
								return rESPONSE;
							}
							return (rx.Length != 7) ? null : new RESPONSE?(RESPONSE.SUCCESS);
						});
						if (result.Item1 != 0)
						{
							return result.Item1;
						}
						CAN.MessageBuffer item = result.Item2;
						if (item == null || item.Length != 7)
						{
							return RESPONSE.FAILED;
						}
						if (result.Item2.GetUINT16(0) != (ushort)Block.ID)
						{
							return RESPONSE.FAILED;
						}
						TotalNumberOfBytesToWrite = result.Item2.GetUINT32(2);
						MinBulkTransferPacketDelay_ms = Math.Max(MinBulkTransferPacketDelay_ms, result.Item2.GetUINT8(6));
						BulkTransferPacketDelay_ms = MinBulkTransferPacketDelay_ms;
						return RESPONSE.SUCCESS;
					}
					finally
					{
						result?.Item2?.ReturnToPool();
					}
				}

				private async Task<RESPONSE> Request24BeginBulkTransferAsync()
				{
					Tuple<RESPONSE, CAN.MessageBuffer> result = null;
					try
					{
						ushort num = ushort.MaxValue;
						if ((byte)Block.Device.ProtocolVersion >= 25)
						{
							num = (ushort)(TotalBytesRemaining - 1);
						}
						bulkXferSize = 0u;
						result = await Client.TransmitRequestAsync(Operation, Target, (byte)36, CAN.PAYLOAD.FromArgs((ushort)Block.ID, CurrentBlockOffset, num), delegate(LocalDeviceRxEvent rx)
						{
							if (rx.Length < 2)
							{
								return null;
							}
							if (rx.GetUINT16(0) != (ushort)Block.ID)
							{
								return null;
							}
							if (rx.Length == 3)
							{
								return (RESPONSE)rx[2];
							}
							if (rx.Length < 6)
							{
								return null;
							}
							if (rx.GetUINT32(2) != CurrentBlockOffset)
							{
								return null;
							}
							return (rx.Length != 8) ? null : new RESPONSE?(RESPONSE.SUCCESS);
						});
						if (result.Item1 != 0)
						{
							return result.Item1;
						}
						CAN.MessageBuffer item = result.Item2;
						if (item == null || item.Length != 8)
						{
							return RESPONSE.FAILED;
						}
						if (result.Item2.GetUINT16(0) != (ushort)Block.ID)
						{
							return RESPONSE.FAILED;
						}
						if (result.Item2.GetUINT32(2) != CurrentBlockOffset)
						{
							return RESPONSE.FAILED;
						}
						bulkXferSize = Math.Min(result.Item2.GetUINT16(6), TotalBytesRemaining);
						return RESPONSE.SUCCESS;
					}
					finally
					{
						result?.Item2?.ReturnToPool();
					}
				}

				private async Task<RESPONSE> DoBulkTransferAsync(int count, int offset)
				{
					if (count <= 0)
					{
						return RESPONSE.FAILED;
					}
					if (count > TotalBytesRemaining)
					{
						return RESPONSE.FAILED;
					}
					if (offset >= TotalNumberOfBytesToWrite)
					{
						return RESPONSE.FAILED;
					}
					int bytes_left = count;
					int i = offset;
					byte sequence = 0;
					byte error_count = 0;
					CAN.PAYLOAD payload = default(CAN.PAYLOAD);
					while (Client.IsOnline && error_count < 5 && !Operation.IsCancellationRequested)
					{
						ReportProgress((uint)i, TotalNumberOfBytesToWrite);
						int len = Math.Min(8, bytes_left);
						if (len <= 0)
						{
							return RESPONSE.FAILED;
						}
						payload.Length = 0;
						for (int j = 0; j < len; j++)
						{
							payload.Append(Data[i + j]);
						}
						if (!Client.Transmit29((byte)159, sequence, Target, payload))
						{
							error_count = (byte)(error_count + 1);
							await Task.Delay(25);
							continue;
						}
						if (BulkTransferPacketDelay_ms > 0)
						{
							await Task.Delay(BulkTransferPacketDelay_ms);
						}
						error_count = 0;
						sequence = (byte)(sequence + 1);
						i += len;
						bytes_left -= len;
						if (bytes_left == 0)
						{
							return RESPONSE.SUCCESS;
						}
						if (bytes_left >= 0)
						{
							continue;
						}
						return RESPONSE.FAILED;
					}
					return RESPONSE.FAILED;
				}

				private async Task<RESPONSE> Request25EndBulkTransferAsync(bool BlockUseSession, ISessionClient session)
				{
					Tuple<RESPONSE, CAN.MessageBuffer> result = null;
					try
					{
						uint xfer_crc = CRC32_LE.Calculate(Data, (int)bulkXferSize, CurrentBlockOffset);
						result = await Client.TransmitRequestAsync(Operation, Target, (byte)37, CAN.PAYLOAD.FromArgs((ushort)Block.ID, (ushort)CurrentBlockOffset, xfer_crc), delegate(LocalDeviceRxEvent rx)
						{
							if (rx.Length < 5)
							{
								return null;
							}
							if (rx.GetUINT16(0) != (ushort)Block.ID)
							{
								return null;
							}
							if (rx.GetUINT16(2) != (ushort)CurrentBlockOffset)
							{
								return null;
							}
							if (rx.Length == 5)
							{
								RESPONSE rESPONSE = (RESPONSE)rx[4];
								if (rESPONSE == RESPONSE.IN_PROGRESS)
								{
									if (BlockUseSession)
									{
										session.TryOpenSession();
									}
									return null;
								}
								return rESPONSE;
							}
							return (rx.Length != 8) ? null : new RESPONSE?(RESPONSE.SUCCESS);
						});
						if (result.Item1 != 0)
						{
							return result.Item1;
						}
						CAN.MessageBuffer item = result.Item2;
						if (item == null || item.Length != 8)
						{
							return RESPONSE.FAILED;
						}
						if (result.Item2.GetUINT16(0) != (ushort)Block.ID)
						{
							return RESPONSE.FAILED;
						}
						if (result.Item2.GetUINT16(2) != (ushort)CurrentBlockOffset)
						{
							return RESPONSE.FAILED;
						}
						if (result.Item2.GetUINT32(4) != xfer_crc)
						{
							return RESPONSE.CRC_INVALID;
						}
						return RESPONSE.SUCCESS;
					}
					finally
					{
						result?.Item2?.ReturnToPool();
					}
				}

				private async Task<RESPONSE> Request26EndBlockWriteAsync(bool BlockUseSession, ISessionClient session)
				{
					Tuple<RESPONSE, CAN.MessageBuffer> result = null;
					try
					{
						result = await Client.TransmitRequestAsync(Operation, Target, (byte)38, CAN.PAYLOAD.FromArgs((ushort)Block.ID, TotalCRC), delegate(LocalDeviceRxEvent rx)
						{
							if (rx.Length != 3)
							{
								return null;
							}
							if (rx.GetUINT16(0) != (ushort)Block.ID)
							{
								return null;
							}
							RESPONSE rESPONSE = (RESPONSE)rx[2];
							if (rESPONSE == RESPONSE.IN_PROGRESS)
							{
								if (BlockUseSession)
								{
									session.TryOpenSession();
								}
								return null;
							}
							return rESPONSE;
						});
						if (result.Item1 != 0)
						{
							return result.Item1;
						}
						CAN.MessageBuffer item = result.Item2;
						if (item == null || item.Length != 3)
						{
							return RESPONSE.FAILED;
						}
						if (result.Item2.GetUINT16(0) != (ushort)Block.ID)
						{
							return RESPONSE.FAILED;
						}
						return (RESPONSE)result.Item2.GetUINT8(2);
					}
					finally
					{
						result?.Item2?.ReturnToPool();
					}
				}
			}

			private readonly LocalDevice Device;

			private ConcurrentDictionary<ulong, IReadOnlyList<BLOCK_ID>> BlockListCache = new ConcurrentDictionary<ulong, IReadOnlyList<BLOCK_ID>>();

			public BlockClient(LocalDevice device)
			{
				Device = device;
			}

			public async Task<Tuple<RESPONSE, IReadOnlyList<BLOCK_ID>>> ReadBlockListAsync(AsyncOperation operation, IDevice target)
			{
				ulong signature = target.GetDeviceUniqueID();
				if (BlockListCache.TryGetValue(signature, out var readOnlyList))
				{
					return Tuple.Create(RESPONSE.SUCCESS, readOnlyList);
				}
				List<BLOCK_ID> list = new List<BLOCK_ID>();
				int index = 0;
				int total = 0;
				operation.ReportProgress("Reading number of blocks...");
				while (true)
				{
					Tuple<RESPONSE, CAN.MessageBuffer> result = null;
					try
					{
						result = await Device.TransmitRequestAsync(operation, target, (byte)32, CAN.PAYLOAD.FromArgs((ushort)index), delegate(LocalDeviceRxEvent rx)
						{
							if (rx.Length == 8 && rx.GetUINT16(0) == index)
							{
								return RESPONSE.SUCCESS;
							}
							return (rx.Length == 1) ? new RESPONSE?((RESPONSE)rx[0]) : null;
						});
						CAN.MessageBuffer item = result.Item2;
						if (item == null)
						{
							return Tuple.Create<RESPONSE, IReadOnlyList<BLOCK_ID>>(result.Item1, null);
						}
						if (index++ == 0)
						{
							total = item.GetUINT16(2);
						}
						else if (list.Count < total)
						{
							list.Add(item.GetUINT16(2));
						}
						if (list.Count < total)
						{
							list.Add(item.GetUINT16(4));
						}
						if (list.Count < total)
						{
							list.Add(item.GetUINT16(6));
						}
						float percent_complete = 100f * (float)list.Count / (float)total;
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(19, 2);
						defaultInterpolatedStringHandler.AppendLiteral("Read ");
						defaultInterpolatedStringHandler.AppendFormatted(list.Count);
						defaultInterpolatedStringHandler.AppendLiteral(" of ");
						defaultInterpolatedStringHandler.AppendFormatted(total);
						defaultInterpolatedStringHandler.AppendLiteral(" blocks...");
						operation.ReportProgress(percent_complete, defaultInterpolatedStringHandler.ToStringAndClear());
						if (list.Count >= total)
						{
							list.Sort((BLOCK_ID first, BLOCK_ID second) => first.Value.CompareTo(second.Value));
							BlockListCache.TryAdd(signature, list);
							operation.ReportProgress(100f, "Success!");
							return Tuple.Create(RESPONSE.SUCCESS, (IReadOnlyList<BLOCK_ID>)list);
						}
					}
					finally
					{
						result?.Item2?.ReturnToPool();
					}
				}
			}

			private async Task<Tuple<RESPONSE, ulong?>> ReadBlockPropertyAsync(AsyncOperation operation, IDevice dest, BLOCK_ID block, int property)
			{
				Tuple<RESPONSE, CAN.MessageBuffer> result = null;
				try
				{
					result = await Device.TransmitRequestAsync(operation, dest, (byte)33, CAN.PAYLOAD.FromArgs((ushort)block, (byte)property), delegate(LocalDeviceRxEvent rx)
					{
						if (rx.Length == 1)
						{
							return (RESPONSE)rx[0];
						}
						if (rx.Length >= 4 && rx.GetUINT16(0) == (ushort)block && rx[2] == property)
						{
							if (rx.Length == 4)
							{
								RESPONSE rESPONSE = (RESPONSE)rx[3];
								if (rESPONSE == RESPONSE.IN_PROGRESS)
								{
									return null;
								}
								return rESPONSE;
							}
							if (rx.Length == 8)
							{
								return RESPONSE.SUCCESS;
							}
						}
						return null;
					});
					if (result.Item2 == null)
					{
						return Tuple.Create<RESPONSE, ulong?>(result.Item1, null);
					}
					ulong num = 0uL;
					for (int i = 3; i < result.Item2.Length; i++)
					{
						num <<= 8;
						num += result.Item2.GetUINT8(i);
					}
					return Tuple.Create(RESPONSE.SUCCESS, (ulong?)num);
				}
				finally
				{
					result?.Item2?.ReturnToPool();
				}
			}

			public async Task<Tuple<RESPONSE, IBlock>> ReadBlockPropertiesAsync(AsyncOperation operation, IDevice target, BLOCK_ID block)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(29, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Reading block ");
				defaultInterpolatedStringHandler.AppendFormatted(block);
				defaultInterpolatedStringHandler.AppendLiteral(" information...");
				operation.ReportProgress(defaultInterpolatedStringHandler.ToStringAndClear());
				Tuple<RESPONSE, ulong?> tuple = await ReadBlockPropertyAsync(operation, target, block, 0);
				if (!tuple.Item2.HasValue)
				{
					return Tuple.Create<RESPONSE, IBlock>(tuple.Item1, null);
				}
				operation.ReportProgress(16.666666f);
				BLOCK_FLAGS flags = (BLOCK_FLAGS)tuple.Item2.Value;
				SESSION_ID read_session_id = null;
				if (flags.HasFlag(BLOCK_FLAGS.READABLE))
				{
					Tuple<RESPONSE, ulong?> tuple2 = await ReadBlockPropertyAsync(operation, target, block, 1);
					if (!tuple2.Item2.HasValue)
					{
						return Tuple.Create<RESPONSE, IBlock>(tuple2.Item1, null);
					}
					read_session_id = (ushort)tuple2.Item2.Value;
				}
				operation.ReportProgress(33.333332f);
				SESSION_ID write_session_id = null;
				if (flags.HasFlag(BLOCK_FLAGS.WRITABLE))
				{
					Tuple<RESPONSE, ulong?> tuple3 = await ReadBlockPropertyAsync(operation, target, block, 2);
					if (!tuple3.Item2.HasValue)
					{
						return Tuple.Create<RESPONSE, IBlock>(tuple3.Item1, null);
					}
					write_session_id = (ushort)tuple3.Item2.Value;
				}
				operation.ReportProgress(50f);
				Tuple<RESPONSE, ulong?> tuple4 = await ReadBlockPropertyAsync(operation, target, block, 3);
				if (!tuple4.Item2.HasValue)
				{
					return Tuple.Create<RESPONSE, IBlock>(tuple4.Item1, null);
				}
				ulong capacity = tuple4.Item2.Value;
				operation.ReportProgress(66.666664f);
				Tuple<RESPONSE, ulong?> tuple5 = await ReadBlockPropertyAsync(operation, target, block, 4);
				if (!tuple5.Item2.HasValue)
				{
					return Tuple.Create<RESPONSE, IBlock>(tuple5.Item1, null);
				}
				ulong size = tuple5.Item2.Value;
				operation.ReportProgress(83.333336f);
				Tuple<RESPONSE, ulong?> tuple6 = await ReadBlockPropertyAsync(operation, target, block, 5);
				if (!tuple6.Item2.HasValue)
				{
					return Tuple.Create<RESPONSE, IBlock>(tuple6.Item1, null);
				}
				uint crc = (uint)tuple6.Item2.Value;
				operation.ReportProgress(100f, "Success!");
				ulong startaddress = 0uL;
				if (flags.HasFlag(BLOCK_FLAGS.USE_SET_START_ADDRESS))
				{
					Tuple<RESPONSE, ulong?> tuple7 = await ReadBlockPropertyAsync(operation, target, block, 7);
					if (!tuple7.Item2.HasValue)
					{
						return Tuple.Create<RESPONSE, IBlock>(tuple7.Item1, null);
					}
					startaddress = tuple7.Item2.Value;
					operation.ReportProgress(83.333336f);
				}
				SetBlock setBlock = new SetBlock(target, block, flags, read_session_id, write_session_id, capacity, size, crc, startaddress);
				return Tuple.Create(RESPONSE.SUCCESS, (IBlock)setBlock);
			}

			public async Task<Tuple<RESPONSE, uint?>> RecalculateBlockCrcAsync(AsyncOperation operation, IDevice target, BLOCK_ID block)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(23, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Computing block ");
				defaultInterpolatedStringHandler.AppendFormatted(block);
				defaultInterpolatedStringHandler.AppendLiteral(" CRC...");
				operation.ReportProgress(defaultInterpolatedStringHandler.ToStringAndClear());
				Tuple<RESPONSE, ulong?> tuple = await ReadBlockPropertyAsync(operation, target, block, 6);
				if (!tuple.Item2.HasValue)
				{
					return Tuple.Create<RESPONSE, uint?>(tuple.Item1, null);
				}
				operation.ReportProgress(100f, "Success!");
				return Tuple.Create(RESPONSE.SUCCESS, (uint?)(uint)tuple.Item2.Value);
			}

			public async Task<Tuple<RESPONSE, IReadOnlyList<byte>>> ReadBlockDataAsync(AsyncOperation operation, IBlock block, int bulk_xfer_delay_ms, ISessionClient session)
			{
				if (!block.IsReadable())
				{
					return Tuple.Create<RESPONSE, IReadOnlyList<byte>>(RESPONSE.WRITE_ONLY, null);
				}
				bool blockUseSession = false;
				if (session != null && (byte)block.Device.ProtocolVersion >= 24 && session.SessionID != SESSION_ID.UNKNOWN)
				{
					while (!session.IsOpen)
					{
						session.TryOpenSession();
						await Task.Delay(100);
					}
					if (!session.IsOpen)
					{
						return Tuple.Create<RESPONSE, IReadOnlyList<byte>>(RESPONSE.SESSION_NOT_OPEN, null);
					}
					blockUseSession = true;
				}
				return await new BlockReadManager(operation, Device, block, bulk_xfer_delay_ms).ReadBlockAsync(blockUseSession, session);
			}

			public async Task<Tuple<RESPONSE, IReadOnlyList<byte>>> ReadBlockDataAsync(AsyncOperation operation, IDevice target, BLOCK_ID block, int bulk_xfer_delay_ms, ISessionClient session)
			{
				Tuple<RESPONSE, IBlock> tuple = await ReadBlockPropertiesAsync(operation, target, block);
				if (tuple.Item2 == null)
				{
					return Tuple.Create<RESPONSE, IReadOnlyList<byte>>(tuple.Item1, null);
				}
				return await ReadBlockDataAsync(operation, tuple.Item2, bulk_xfer_delay_ms, session);
			}

			public async Task<RESPONSE> WriteBlockDataAsync(AsyncOperation operation, IBlock block, IReadOnlyList<byte> data, int bulk_xfer_delay_ms, ISessionClient session)
			{
				bool blockUseSession = false;
				if (session != null && (byte)block.Device.ProtocolVersion >= 24 && session.SessionID != SESSION_ID.UNKNOWN)
				{
					while (!session.IsOpen)
					{
						session.TryOpenSession();
						await Task.Delay(100);
					}
					if (!session.IsOpen)
					{
						return RESPONSE.SESSION_NOT_OPEN;
					}
					blockUseSession = true;
				}
				return await new BlockWriteManager(operation, Device, block, data, bulk_xfer_delay_ms).WriteBlockAsync(blockUseSession, session);
			}
		}

		public class LocalBlock : ILocalBlock, IBlock
		{
			private readonly object Mutex = new object();

			private BlockWriter Writer;

			private IReadOnlyList<byte> mData;

			public LocalDevice Device { get; private set; }

			IDevice IBlock.Device => Device;

			public BLOCK_ID ID { get; private set; }

			public BLOCK_FLAGS Flags { get; private set; }

			public ulong Capacity { get; private set; }

			public ulong Size => (uint)Data.Count;

			public uint CRC { get; set; }

			public ulong StartAddress { get; set; }

			public ulong SetSize { get; set; }

			public SESSION_ID ReadSessionID { get; private set; }

			public SESSION_ID WriteSessionID { get; private set; }

			public IReadOnlyList<byte> Data
			{
				get
				{
					return mData;
				}
				protected set
				{
					if (value == null)
					{
						value = new byte[0];
					}
					if ((uint)value.Count > Capacity)
					{
						throw new ArgumentException("Data is too large for buffer");
					}
					mData = value;
					CRC = CalculateCRC();
				}
			}

			public LocalBlock(LocalDevice device, BLOCK_ID id, ulong capacity, SESSION_ID read_session, SESSION_ID write_session)
				: this(device, id, capacity, read_session, write_session, new byte[0])
			{
			}

			public LocalBlock(LocalDevice device, BLOCK_ID id, ulong capacity, SESSION_ID read_session, SESSION_ID write_session, IReadOnlyList<byte> data)
			{
				Device = device;
				ID = id;
				Capacity = capacity;
				ReadSessionID = read_session;
				WriteSessionID = write_session;
				BLOCK_FLAGS bLOCK_FLAGS = BLOCK_FLAGS.NONE;
				if (this.IsReadable())
				{
					bLOCK_FLAGS |= BLOCK_FLAGS.READABLE;
				}
				if (this.IsWritable())
				{
					bLOCK_FLAGS |= BLOCK_FLAGS.WRITABLE;
				}
				Flags = bLOCK_FLAGS;
				Data = data;
			}

			public uint CalculateCRC()
			{
				return CRC32_LE.Calculate(Data);
			}

			public virtual bool WriteData(IReadOnlyList<byte> buf)
			{
				if (this.IsWritable() && (uint)(buf?.Count).Value <= Capacity)
				{
					Data = Enumerable.ToArray(buf);
					return true;
				}
				return false;
			}

			internal BlockWriter GetBlockWriter()
			{
				if (Writer == null && this.IsWritable())
				{
					lock (Mutex)
					{
						if (Writer == null)
						{
							Writer = new BlockWriter(this);
						}
					}
				}
				return Writer;
			}
		}

		internal class BlockWriter
		{
			private enum STATE
			{
				IDLE,
				WAITING_FOR_BULK_XFER,
				BULK_XFER
			}

			private class BusEndpoint : IBusEndpoint
			{
				public IAdapter Adapter { get; private set; }

				public ADDRESS Address { get; private set; }

				public bool IsOnline => true;

				public BusEndpoint(IAdapter adapter, ADDRESS address)
				{
					Adapter = adapter;
					Address = address;
				}
			}

			private const byte BULK_XFER_DELAY_MS = 0;

			private readonly LocalDevice Device;

			private readonly LocalBlock Block;

			private Timer LastRxTimer = new Timer();

			private STATE State;

			private ADDRESS Client = ADDRESS.BROADCAST;

			private uint BlockOffset;

			private byte[] WriteBuf;

			private bool IsBusy => State != STATE.IDLE;

			public BlockWriter(LocalBlock block)
			{
				Device = block.Device;
				Block = block;
			}

			private void Abort()
			{
				State = STATE.IDLE;
				Client = ADDRESS.BROADCAST;
				WriteBuf = null;
			}

			private void SanityCheck()
			{
				if (!IsBusy)
				{
					return;
				}
				if (!Block.IsWritable())
				{
					Abort();
					return;
				}
				if (LastRxTimer.ElapsedTime.TotalSeconds > 60.0)
				{
					Abort();
					return;
				}
				if (WriteBuf == null)
				{
					Abort();
					return;
				}
				if (!Client.IsValidDeviceAddress)
				{
					Abort();
					return;
				}
				if (Block.WriteSessionID != SESSION_ID.UNKNOWN && Device.GetLocalSessionClientAddress(Block.WriteSessionID) != Client)
				{
					Abort();
				}
				if (BlockOffset > WriteBuf.Length)
				{
					Abort();
				}
			}

			public RESPONSE BeginWrite(ADDRESS client, uint requested_size, out uint accepted_size, out byte bulk_xfer_delay_ms)
			{
				accepted_size = 0u;
				bulk_xfer_delay_ms = 0;
				SanityCheck();
				if (!Block.IsWritable())
				{
					return RESPONSE.READ_ONLY;
				}
				if (IsBusy)
				{
					return RESPONSE.BUSY;
				}
				if (requested_size > Block.Capacity)
				{
					return RESPONSE.VALUE_TOO_LARGE;
				}
				if (requested_size > int.MaxValue)
				{
					return RESPONSE.VALUE_TOO_LARGE;
				}
				if (Block.WriteSessionID != SESSION_ID.UNKNOWN && Device.GetLocalSessionClientAddress(Block.WriteSessionID) != client)
				{
					return RESPONSE.SESSION_NOT_OPEN;
				}
				LastRxTimer.Reset();
				Client = client;
				accepted_size = requested_size;
				BlockOffset = 0u;
				WriteBuf = new byte[accepted_size];
				State = STATE.WAITING_FOR_BULK_XFER;
				return RESPONSE.SUCCESS;
			}

			public RESPONSE BeginBulkTransfer(ADDRESS client, uint offset, ushort requested_bulk_xfer_size)
			{
				SanityCheck();
				if (IsBusy && Client != client)
				{
					return RESPONSE.BUSY;
				}
				if (State != STATE.WAITING_FOR_BULK_XFER)
				{
					return RESPONSE.CONDITIONS_NOT_CORRECT;
				}
				if (BlockOffset != offset)
				{
					return RESPONSE.VALUE_OUT_OF_RANGE;
				}
				if (offset > WriteBuf.Length)
				{
					return RESPONSE.VALUE_TOO_LARGE;
				}
				ulong num = (uint)WriteBuf.Length - offset;
				ushort accepted_bulk_xfer_size = requested_bulk_xfer_size;
				if (accepted_bulk_xfer_size > num)
				{
					accepted_bulk_xfer_size = (ushort)num;
				}
				if (accepted_bulk_xfer_size <= 0)
				{
					return RESPONSE.VALUE_OUT_OF_RANGE;
				}
				State = STATE.BULK_XFER;
				TaskCompletionSource<RESPONSE> tcs;
				bool message_sent;
				byte sequence;
				uint bytes_written;
				uint bulkXferCrc;
				Task.Run(async delegate
				{
					ReusableSubscription rx_listener = Device.ReusableSubscriptionPool.Get();
					try
					{
						tcs = new TaskCompletionSource<RESPONSE>();
						BusEndpoint dest = new BusEndpoint(Device.Adapter, client);
						LastRxTimer.Reset();
						message_sent = false;
						sequence = 0;
						bytes_written = 0u;
						bulkXferCrc = 0u;
						rx_listener.SetDelegate(Device, delegate(LocalDeviceRxEvent rx)
						{
							if (State != STATE.BULK_XFER)
							{
								return true;
							}
							if (!message_sent)
							{
								return false;
							}
							if (rx.SourceAddress != Client)
							{
								return false;
							}
							switch ((byte)rx.MessageType)
							{
							case 128:
								if (rx.MessageData == 37)
								{
									if (rx.Length != 8)
									{
										tcs.SetResult(RESPONSE.BAD_REQUEST);
									}
									else if (rx.GetUINT16(0) != (ushort)Block.ID || rx.GetUINT16(2) != (ushort)offset)
									{
										tcs.SetResult(RESPONSE.VALUE_OUT_OF_RANGE);
									}
									else
									{
										bulkXferCrc = rx.GetUINT32(4);
										tcs.SetResult(RESPONSE.SUCCESS);
									}
									return true;
								}
								break;
							case 159:
								if (rx.MessageData == sequence)
								{
									LastRxTimer.Reset();
									for (int i = 0; i < rx.Length; i++)
									{
										if (bytes_written < accepted_bulk_xfer_size)
										{
											WriteBuf[offset + bytes_written] = rx[i];
											bytes_written++;
										}
									}
									sequence++;
								}
								break;
							}
							return false;
						});
						int retry2 = 0;
						while (!Device.Transmit29((byte)129, 36, dest, CAN.PAYLOAD.FromArgs((ushort)Block.ID, offset, accepted_bulk_xfer_size)))
						{
							if (!Device.IsOnline)
							{
								return;
							}
							int num2 = retry2 + 1;
							retry2 = num2;
							if (num2 > 5)
							{
								return;
							}
							await Task.Delay(5);
						}
						message_sent = true;
						while (!tcs.Task.IsCompleted)
						{
							if (!Device.IsOnline)
							{
								return;
							}
							await Task.WhenAny(tcs.Task, Task.Delay(10000 + accepted_bulk_xfer_size));
						}
						RESPONSE response = tcs.Task.Result;
						if (response == RESPONSE.SUCCESS && (bytes_written != accepted_bulk_xfer_size || bulkXferCrc != CRC32_LE.Calculate(WriteBuf, (int)bytes_written, offset)))
						{
							response = RESPONSE.CRC_INVALID;
						}
						if (response != 0)
						{
							for (retry2 = 0; retry2 < 5; retry2++)
							{
								if (!Device.IsOnline)
								{
									break;
								}
								if (Device.Transmit29((byte)129, 37, dest, CAN.PAYLOAD.FromArgs((ushort)Block.ID, (ushort)offset, (byte)response)))
								{
									break;
								}
								await Task.Delay(5);
							}
						}
						else
						{
							for (retry2 = 0; retry2 < 5; retry2++)
							{
								if (!Device.IsOnline)
								{
									break;
								}
								if (Device.Transmit29((byte)129, 37, dest, CAN.PAYLOAD.FromArgs((ushort)Block.ID, (ushort)offset, bulkXferCrc)))
								{
									BlockOffset += bytes_written;
									break;
								}
								await Task.Delay(5);
							}
						}
					}
					catch (TimeoutException)
					{
					}
					catch (OperationCanceledException)
					{
					}
					finally
					{
						rx_listener?.ReturnToPool();
						State = STATE.WAITING_FOR_BULK_XFER;
					}
				});
				return RESPONSE.SUCCESS;
			}

			public RESPONSE EndBlockWrite(ADDRESS client, uint crc)
			{
				SanityCheck();
				if (IsBusy && Client != client)
				{
					return RESPONSE.BUSY;
				}
				try
				{
					if (State != STATE.WAITING_FOR_BULK_XFER)
					{
						return RESPONSE.CONDITIONS_NOT_CORRECT;
					}
					if (BlockOffset != WriteBuf.Length || crc != CRC32_LE.Calculate((IReadOnlyCollection<byte>)(object)WriteBuf))
					{
						return RESPONSE.CRC_INVALID;
					}
					return (!Block.WriteData(WriteBuf)) ? RESPONSE.FAILED : RESPONSE.SUCCESS;
				}
				finally
				{
					Abort();
				}
			}
		}

		private class BlockServer
		{
			private class BusEndpoint : IBusEndpoint
			{
				public IAdapter Adapter { get; private set; }

				public ADDRESS Address { get; private set; }

				public bool IsOnline => true;

				public BusEndpoint(IAdapter adapter, ADDRESS address)
				{
					Adapter = adapter;
					Address = address;
				}
			}

			private readonly LocalDevice Device;

			private Dictionary<BLOCK_ID, LocalBlock> Dict = new Dictionary<BLOCK_ID, LocalBlock>();

			private List<LocalBlock> List = new List<LocalBlock>();

			private LocalBlock this[BLOCK_ID id]
			{
				get
				{
					Dict.TryGetValue(id, out var value);
					return value;
				}
			}

			public BlockServer(LocalDevice device)
			{
				Device = device;
				Device.mRequestServer.AddRequestHandler((byte)32, Request20ReadBlockList);
				Device.mRequestServer.AddRequestHandler((byte)33, Request21ReadBlockProperties);
				Device.mRequestServer.AddRequestHandler((byte)34, Request22ReadBlockData);
				Device.mRequestServer.AddRequestHandler((byte)35, Request23BeginBlockWrite);
				Device.mRequestServer.AddRequestHandler((byte)36, Request24BeginBlockWriteBulkXfer);
				Device.mRequestServer.AddRequestHandler((byte)38, Request26EndBlockWrite);
			}

			public void Add(LocalBlock block)
			{
				if (Dict.ContainsKey(block.ID))
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(47, 1);
					defaultInterpolatedStringHandler.AppendLiteral("BLOCK_ID ");
					defaultInterpolatedStringHandler.AppendFormatted(block.ID);
					defaultInterpolatedStringHandler.AppendLiteral(" already exists within DeviceSimulator");
					throw new InvalidOperationException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				Dict.Add(block.ID, block);
				List.Add(block);
			}

			private CAN.PAYLOAD? Request20ReadBlockList(AdapterRxEvent rx)
			{
				if (rx.Length != 2)
				{
					return CAN.PAYLOAD.FromArgs((byte)2);
				}
				int num = rx.GetUINT16(0);
				CAN.PAYLOAD value = CAN.PAYLOAD.FromArgs((ushort)num);
				if (num == 0)
				{
					value.Append((ushort)List.Count);
				}
				else
				{
					num = num * 3 - 1;
				}
				while (value.Length < 8)
				{
					LocalBlock localBlock = null;
					if (num < List.Count)
					{
						localBlock = List[num++];
					}
					value.Append(localBlock?.ID ?? ((BLOCK_ID)(ushort)0));
				}
				return value;
			}

			private CAN.PAYLOAD? Request21ReadBlockProperties(AdapterRxEvent rx)
			{
				if (rx.Length != 3)
				{
					return CAN.PAYLOAD.FromArgs((byte)2);
				}
				BLOCK_ID bLOCK_ID = rx.GetUINT16(0);
				LocalBlock localBlock = this[bLOCK_ID];
				if (localBlock == null)
				{
					return CAN.PAYLOAD.FromArgs((ushort)bLOCK_ID, rx[2], (byte)4);
				}
				ulong num;
				switch (rx[2])
				{
				default:
					return CAN.PAYLOAD.FromArgs((ushort)bLOCK_ID, rx[2], (byte)10);
				case 0:
					num = (ulong)localBlock.Flags;
					break;
				case 1:
					num = (ushort)localBlock.ReadSessionID;
					break;
				case 2:
					num = (ushort)localBlock.WriteSessionID;
					break;
				case 3:
					num = localBlock.Capacity;
					break;
				case 4:
					num = localBlock.Size;
					break;
				case 5:
					num = localBlock.CRC;
					break;
				case 6:
					num = localBlock.CRC;
					break;
				}
				return CAN.PAYLOAD.FromArgs((ushort)bLOCK_ID, rx[2], (byte)(num >> 32), (uint)num);
			}

			private CAN.PAYLOAD? Request22ReadBlockData(AdapterRxEvent rx)
			{
				if (rx.Length != 8)
				{
					return CAN.PAYLOAD.FromArgs((byte)2);
				}
				ADDRESS dest_addr = rx.SourceAddress;
				BLOCK_ID id = rx.GetUINT16(0);
				uint start_offset = rx.GetUINT32(2);
				uint num = (uint)(rx[6] + 1);
				int xfer_delay_ms = rx.GetUINT8(7);
				LocalBlock block = this[id];
				if (block == null)
				{
					return CAN.PAYLOAD.FromArgs((ushort)id, start_offset, (byte)4);
				}
				if (!block.IsReadable())
				{
					return CAN.PAYLOAD.FromArgs((ushort)id, start_offset, (byte)8);
				}
				if (block.ReadSessionID != SESSION_ID.UNKNOWN && Device.GetLocalSessionClientAddress(block.ReadSessionID) != dest_addr)
				{
					return CAN.PAYLOAD.FromArgs((ushort)id, start_offset, (byte)14);
				}
				if (start_offset >= block.Size)
				{
					return CAN.PAYLOAD.FromArgs((ushort)id, start_offset, (byte)5);
				}
				ulong num2 = block.Size - start_offset;
				uint bytes_to_send = num * 8;
				if (bytes_to_send >= num2)
				{
					bytes_to_send = (uint)num2;
				}
				if (bytes_to_send == 0)
				{
					return CAN.PAYLOAD.FromArgs((byte)21);
				}
				Task.Run(async delegate
				{
					BusEndpoint dest = new BusEndpoint(Device.Adapter, dest_addr);
					int retry = 0;
					while (!Device.Transmit29((byte)129, 34, dest, CAN.PAYLOAD.FromArgs((ushort)id, start_offset, (ushort)bytes_to_send)))
					{
						if (!Device.IsOnline)
						{
							return;
						}
						int num3 = retry + 1;
						retry = num3;
						if (num3 > 5)
						{
							return;
						}
						await Task.Delay(5);
					}
					CAN.PAYLOAD payload = default(CAN.PAYLOAD);
					byte sequence = 0;
					retry = (int)start_offset;
					int bytes_left = (int)bytes_to_send;
					int retry3 = 0;
					while (bytes_left > 0)
					{
						if (block.ReadSessionID != SESSION_ID.UNKNOWN && Device.GetLocalSessionClientAddress(block.ReadSessionID) != dest_addr)
						{
							return;
						}
						payload.Length = Math.Min(bytes_left, 8);
						for (int i = 0; i < payload.Length; i++)
						{
							payload[i] = block.Data[retry + i];
						}
						if (!Device.Transmit29((byte)159, sequence, dest, payload))
						{
							if (!Device.IsOnline)
							{
								return;
							}
							int num3 = retry3 + 1;
							retry3 = num3;
							if (num3 > 5)
							{
								return;
							}
							await Task.Delay(5);
						}
						else
						{
							if (xfer_delay_ms != 0)
							{
								await Task.Delay(xfer_delay_ms);
							}
							retry3 = 0;
							sequence = (byte)(sequence + 1);
							retry += payload.Length;
							bytes_left -= payload.Length;
						}
					}
					uint crc = CRC32_LE.Calculate(block.Data, (int)bytes_to_send, start_offset);
					retry3 = 0;
					while (!Device.Transmit29((byte)129, 37, dest, CAN.PAYLOAD.FromArgs((ushort)id, (ushort)start_offset, crc)) && Device.IsOnline)
					{
						int num3 = retry3 + 1;
						retry3 = num3;
						if (num3 > 5)
						{
							break;
						}
						await Task.Delay(5);
					}
				});
				return null;
			}

			private CAN.PAYLOAD? Request23BeginBlockWrite(AdapterRxEvent rx)
			{
				if (rx.Length != 6)
				{
					return CAN.PAYLOAD.FromArgs((byte)2);
				}
				BLOCK_ID bLOCK_ID = rx.GetUINT16(0);
				LocalBlock localBlock = this[bLOCK_ID];
				if (localBlock == null)
				{
					return CAN.PAYLOAD.FromArgs((ushort)bLOCK_ID, (byte)4);
				}
				BlockWriter blockWriter = localBlock.GetBlockWriter();
				if (blockWriter == null)
				{
					return CAN.PAYLOAD.FromArgs((ushort)bLOCK_ID, (byte)7);
				}
				uint accepted_size;
				byte bulk_xfer_delay_ms;
				RESPONSE rESPONSE = blockWriter.BeginWrite(rx.SourceAddress, rx.GetUINT32(2), out accepted_size, out bulk_xfer_delay_ms);
				if (rESPONSE != 0)
				{
					return CAN.PAYLOAD.FromArgs((ushort)bLOCK_ID, (byte)rESPONSE);
				}
				return CAN.PAYLOAD.FromArgs((ushort)bLOCK_ID, accepted_size, bulk_xfer_delay_ms);
			}

			private CAN.PAYLOAD? Request24BeginBlockWriteBulkXfer(AdapterRxEvent rx)
			{
				if (rx.Length != 8)
				{
					return CAN.PAYLOAD.FromArgs((byte)2);
				}
				BLOCK_ID bLOCK_ID = rx.GetUINT16(0);
				LocalBlock localBlock = this[bLOCK_ID];
				if (localBlock == null)
				{
					return CAN.PAYLOAD.FromArgs((ushort)bLOCK_ID, (byte)4);
				}
				BlockWriter blockWriter = localBlock.GetBlockWriter();
				if (blockWriter == null)
				{
					return CAN.PAYLOAD.FromArgs((ushort)bLOCK_ID, (byte)7);
				}
				RESPONSE rESPONSE = blockWriter.BeginBulkTransfer(rx.SourceAddress, rx.GetUINT32(2), rx.GetUINT16(6));
				if (rESPONSE != 0)
				{
					return CAN.PAYLOAD.FromArgs((ushort)bLOCK_ID, (byte)rESPONSE);
				}
				return null;
			}

			private CAN.PAYLOAD? Request26EndBlockWrite(AdapterRxEvent rx)
			{
				if (rx.Length != 6)
				{
					return CAN.PAYLOAD.FromArgs((byte)2);
				}
				BLOCK_ID bLOCK_ID = rx.GetUINT16(0);
				LocalBlock localBlock = this[bLOCK_ID];
				if (localBlock == null)
				{
					return CAN.PAYLOAD.FromArgs((ushort)bLOCK_ID, (byte)4);
				}
				BlockWriter blockWriter = localBlock.GetBlockWriter();
				if (blockWriter == null)
				{
					return CAN.PAYLOAD.FromArgs((ushort)bLOCK_ID, (byte)7);
				}
				RESPONSE rESPONSE = blockWriter.EndBlockWrite(rx.SourceAddress, rx.GetUINT32(2));
				_ = 21;
				return CAN.PAYLOAD.FromArgs((ushort)bLOCK_ID, (byte)rESPONSE);
			}
		}

		private class MuteManager
		{
			private readonly LocalDevice Device;

			private bool mIsMuted;

			private Timer MuteTimer = new Timer();

			public bool IsMuted
			{
				get
				{
					return mIsMuted;
				}
				set
				{
					mIsMuted = value;
					Device.AddressClaim.Enabled = Device.EnableDevice && !IsMuted;
				}
			}

			public MuteManager(LocalDevice device)
			{
				Device = device;
				Device.mRequestServer.AddRequestHandler((byte)1, Request01MuteDevice);
			}

			private CAN.PAYLOAD? Request01MuteDevice(AdapterRxEvent rx)
			{
				if (rx.Count != 2)
				{
					return CAN.PAYLOAD.FromArgs((byte)2);
				}
				switch (rx[0])
				{
				default:
					return CAN.PAYLOAD.FromArgs((byte)10);
				case 0:
					IsMuted = false;
					return CAN.PAYLOAD.FromArgs((byte)0, (byte)0);
				case 1:
					Mute(TimeSpan.FromSeconds((int)rx[1]));
					if (IsMuted)
					{
						return CAN.PAYLOAD.FromArgs((byte)1, rx[1]);
					}
					return CAN.PAYLOAD.FromArgs((byte)0, (byte)0);
				case 2:
					if (IsMuted)
					{
						Mute(TimeSpan.FromSeconds((int)rx[1]));
					}
					return null;
				}
			}

			private void Mute(TimeSpan time)
			{
				if (time > TimeSpan.Zero)
				{
					MuteTimer.ElapsedTime = -time;
					IsMuted = true;
				}
			}

			public void BackgroundTask()
			{
				if (IsMuted && MuteTimer.ElapsedTime >= TimeSpan.Zero)
				{
					IsMuted = false;
				}
			}
		}

		private class AddressClaimManager : Disposable
		{
			private readonly LocalDevice Device;

			private readonly IAdapter Adapter;

			private readonly Timer ClaimTimer = new Timer();

			private readonly MAC TempMAC = new MAC();

			private ADDRESS AddressBeingClaimed = ADDRESS.INVALID;

			private ADDRESS mAddress = ADDRESS.INVALID;

			private int RetransmitAddressClaim;

			private bool mEnabled;

			public bool Enabled
			{
				get
				{
					return mEnabled;
				}
				set
				{
					if (mEnabled != value)
					{
						mEnabled = value;
						KillAddressClaim();
					}
				}
			}

			public ADDRESS Address
			{
				get
				{
					return mAddress;
				}
				private set
				{
					if (base.IsDisposed || Adapter.IsDisposed)
					{
						value = ADDRESS.INVALID;
					}
					ADDRESS aDDRESS = mAddress;
					if (aDDRESS != value)
					{
						mAddress = value;
						Device.OnAddressChanged(value, aDDRESS);
					}
				}
			}

			public AddressClaimManager(LocalDevice device)
			{
				Device = device;
				Adapter = device.Adapter;
			}

			public override void Dispose(bool disposing)
			{
				if (disposing)
				{
					AddressBeingClaimed = ADDRESS.INVALID;
					Address = ADDRESS.INVALID;
				}
			}

			public void OnAdapterOpened(Comm.AdapterOpenedEvent message)
			{
				if (!base.IsDisposed && !Adapter.IsDisposed)
				{
					KillAddressClaim();
				}
			}

			public void OnAdapterClosed(Comm.AdapterClosedEvent message)
			{
				if (!base.IsDisposed && !Adapter.IsDisposed)
				{
					KillAddressClaim();
				}
			}

			public void OnAdapterRx(AdapterRxEvent rx)
			{
				if (base.IsDisposed || Adapter.IsDisposed || !Enabled || (byte)rx.MessageType != 0 || rx.Count != 8 || !AddressBeingClaimed.IsValidDeviceAddress || (rx.SourceAddress != AddressBeingClaimed && (rx.SourceAddress != ADDRESS.BROADCAST || rx[0] != (byte)AddressBeingClaimed)) || !TempMAC.UnloadFromMessage(rx))
				{
					return;
				}
				switch (Device.MAC.CompareTo(TempMAC))
				{
				case 1:
					KillAddressClaim();
					break;
				case 0:
					if (rx[1] != (byte)Device.ProtocolVersion)
					{
						KillAddressClaim();
					}
					break;
				case -1:
					RetransmitAddressClaim = 3;
					break;
				}
			}

			private void KillAddressClaim()
			{
				AddressBeingClaimed = ADDRESS.INVALID;
				RetransmitAddressClaim = 0;
				Address = ADDRESS.INVALID;
			}

			public void BackgroundTask()
			{
				if (base.IsDisposed || Adapter.IsDisposed)
				{
					return;
				}
				if (!Adapter.IsConnected || !Enabled)
				{
					KillAddressClaim();
				}
				else if (!Address.IsValidDeviceAddress)
				{
					if (!AddressBeingClaimed.IsValidDeviceAddress)
					{
						ADDRESS aDDRESS = ChooseAddressToClaim();
						if (aDDRESS.IsValidDeviceAddress && TransmitAddressClaim(aDDRESS))
						{
							AddressBeingClaimed = aDDRESS;
							ClaimTimer.Reset();
						}
					}
					else if (ClaimTimer.ElapsedTime > ADDRESS_CLAIM_TIMEOUT)
					{
						Address = AddressBeingClaimed;
					}
				}
				else if (RetransmitAddressClaim > 0 && TransmitAddressClaim(Address))
				{
					RetransmitAddressClaim--;
				}
			}

			private ADDRESS ChooseAddressToClaim()
			{
				ADDRESS unusedDeviceAddress = Adapter.GetUnusedDeviceAddress();
				if (!unusedDeviceAddress.IsValidDeviceAddress)
				{
					return ADDRESS.INVALID;
				}
				foreach (IDevice item in Device.Product)
				{
					if ((item as LocalDevice)?.AddressClaim.AddressBeingClaimed == unusedDeviceAddress)
					{
						return ADDRESS.INVALID;
					}
				}
				return unusedDeviceAddress;
			}

			private bool TransmitAddressClaim(ADDRESS address)
			{
				if (!address.IsValidDeviceAddress || !Enabled)
				{
					return false;
				}
				return Device.Transmit(new CAN_ID((byte)0, ADDRESS.BROADCAST), CAN.PAYLOAD.FromArgs((byte)address, (byte)Device.ProtocolVersion, (UInt48)Device.MAC));
			}
		}

		private class InMotionLockoutManager
		{
			private static readonly TimeSpan ARMED_TIMEOUT = TimeSpan.FromSeconds(5.0);

			private static readonly TimeSpan CONTENTION_TIMEOUT = TimeSpan.FromSeconds(5.0);

			private static readonly TimeSpan ALL_CLEAR_TIME = TimeSpan.FromSeconds(2.2);

			private readonly LocalDevice Device;

			private Timer ArmedTimer = new Timer();

			private Timer AllClearTimer = new Timer();

			private Timer ContentionTimer = new Timer();

			private DeviceInMotionLockoutLevelChangedEvent InMotionLockoutEvent;

			private IN_MOTION_LOCKOUT_LEVEL mLockoutLevel = (byte)0;

			private IN_MOTION_LOCKOUT_LEVEL HighestLevelSeen = (byte)0;

			public IN_MOTION_LOCKOUT_LEVEL LockoutLevel
			{
				get
				{
					return mLockoutLevel;
				}
				private set
				{
					if (mLockoutLevel != value)
					{
						ArmedTimer.Stop();
						ContentionTimer.Stop();
						IN_MOTION_LOCKOUT_LEVEL prev = mLockoutLevel;
						mLockoutLevel = value;
						InMotionLockoutEvent.Publish(value, prev);
					}
				}
			}

			public IN_MOTION_LOCKOUT_LEVEL ProposedLevel { get; private set; }

			private bool IsArmed
			{
				get
				{
					if (ArmedTimer.IsRunning)
					{
						return ArmedTimer.ElapsedTime <= ARMED_TIMEOUT;
					}
					return false;
				}
			}

			public bool IsInContention
			{
				get
				{
					if (ContentionTimer.IsRunning)
					{
						return ContentionTimer.ElapsedTime <= CONTENTION_TIMEOUT;
					}
					return false;
				}
			}

			public InMotionLockoutManager(LocalDevice device)
			{
				Device = device;
				ArmedTimer.Stop();
				ContentionTimer.Stop();
				InMotionLockoutEvent = new DeviceInMotionLockoutLevelChangedEvent(device);
				device.mRequestServer.AddRequestHandler((byte)2, OnRequest02InMotionLockout);
			}

			public void IncreaseLockoutLevel(IN_MOTION_LOCKOUT_LEVEL level)
			{
				if ((byte)LockoutLevel < (byte)level)
				{
					LockoutLevel = level;
				}
			}

			private CAN.PAYLOAD? OnRequest02InMotionLockout(AdapterRxEvent rx)
			{
				if (rx.Count != 1)
				{
					return CAN.PAYLOAD.FromArgs((byte)2);
				}
				if (rx.TargetAddress != ADDRESS.BROADCAST)
				{
					return CAN.PAYLOAD.FromArgs((byte)6);
				}
				switch (rx[0])
				{
				default:
					return CAN.PAYLOAD.FromArgs(rx[0], (byte)2);
				case 1:
				case 2:
				case 3:
					IncreaseLockoutLevel(rx[0]);
					return CAN.PAYLOAD.FromArgs(rx[0], (byte)0);
				case 85:
					if (!IsInContention)
					{
						ArmedTimer.Reset();
					}
					return CAN.PAYLOAD.FromArgs(rx[0], (byte)0);
				case 170:
					if (!IsArmed)
					{
						return CAN.PAYLOAD.FromArgs(rx[0], (byte)9);
					}
					ArmedTimer.Stop();
					if ((byte)LockoutLevel > 0)
					{
						ProposedLevel = Device.IsOkToClearInMotionLockout;
						if ((byte)ProposedLevel < (byte)LockoutLevel)
						{
							ContentionTimer.Reset();
						}
					}
					return CAN.PAYLOAD.FromArgs(rx[0], (byte)0);
				}
			}

			public RESPONSE ProcessLockoutRequest(byte cmd)
			{
				switch (cmd)
				{
				default:
					return RESPONSE.BAD_REQUEST;
				case 1:
				case 2:
				case 3:
					IncreaseLockoutLevel(cmd);
					return RESPONSE.SUCCESS;
				case 85:
					if (!IsInContention)
					{
						ArmedTimer.Reset();
					}
					return RESPONSE.SUCCESS;
				case 170:
					if (!IsArmed)
					{
						return RESPONSE.CONDITIONS_NOT_CORRECT;
					}
					ArmedTimer.Stop();
					if ((byte)LockoutLevel > 0)
					{
						ProposedLevel = Device.IsOkToClearInMotionLockout;
						if ((byte)ProposedLevel < (byte)LockoutLevel)
						{
							ContentionTimer.Reset();
						}
					}
					return RESPONSE.SUCCESS;
				}
			}

			public void OnAdapterRx(AdapterRxEvent rx)
			{
				if ((byte)rx.MessageType == 0 && rx.Count == 8 && rx.SourceAddress.IsValidDeviceAddress)
				{
					IN_MOTION_LOCKOUT_LEVEL inMotionLockoutLevel = new NETWORK_STATUS(rx[0], rx[1]).InMotionLockoutLevel;
					if ((byte)HighestLevelSeen < (byte)inMotionLockoutLevel)
					{
						HighestLevelSeen = inMotionLockoutLevel;
					}
					if ((byte)LockoutLevel < (byte)inMotionLockoutLevel)
					{
						LockoutLevel = inMotionLockoutLevel;
					}
					if ((byte)inMotionLockoutLevel > 0)
					{
						AllClearTimer.Reset();
					}
				}
			}

			public void BackgroundTask()
			{
				if (ArmedTimer.IsRunning && ArmedTimer.ElapsedTime > ARMED_TIMEOUT)
				{
					ArmedTimer.Stop();
				}
				if (!ContentionTimer.IsRunning)
				{
					return;
				}
				TimeSpan elapsedTime = ContentionTimer.ElapsedTime;
				if (elapsedTime < CONTENTION_TIMEOUT)
				{
					if (elapsedTime < CONTENTION_TIMEOUT - ALL_CLEAR_TIME)
					{
						HighestLevelSeen = (byte)0;
					}
					if (AllClearTimer.ElapsedTime > ALL_CLEAR_TIME)
					{
						LockoutLevel = ProposedLevel;
						ContentionTimer.Stop();
					}
				}
				else
				{
					ContentionTimer.Stop();
					if ((byte)ProposedLevel > (byte)HighestLevelSeen)
					{
						LockoutLevel = ProposedLevel;
					}
					else
					{
						LockoutLevel = HighestLevelSeen;
					}
				}
			}
		}

		private class PidClient : IPidClient
		{
			private readonly LocalDevice Device;

			private ConcurrentDictionary<ulong, PidList> PidListCache = new ConcurrentDictionary<ulong, PidList>();

			public PidClient(LocalDevice device)
			{
				Device = device;
			}

			public async Task<Tuple<RESPONSE?, PidList>> ReadPidListAsync(AsyncOperation operation, IDevice tgtDevice)
			{
				PidList pidList = null;
				ulong signature = tgtDevice.GetDeviceUniqueID();
				if (PidListCache.TryGetValue(signature, out pidList) && pidList.Device != tgtDevice)
				{
					List<PidInfo> list2 = new List<PidInfo>();
					foreach (PidInfo item in pidList)
					{
						list2.Add(new PidInfo(tgtDevice, item.ID, item.Flags));
					}
					pidList = new PidList(tgtDevice, list2);
					if (tgtDevice.IsOnline)
					{
						PidListCache[signature] = pidList;
					}
					return Tuple.Create((RESPONSE?)RESPONSE.SUCCESS, pidList);
				}
				List<PidInfo> list = new List<PidInfo>();
				int index = 0;
				int total = 0;
				operation.ReportProgress("Reading PID list from device...");
				while (true)
				{
					Tuple<RESPONSE, CAN.MessageBuffer> result = null;
					try
					{
						result = await Device.TransmitRequestAsync(operation, tgtDevice, (byte)16, CAN.PAYLOAD.FromArgs((ushort)index), (LocalDeviceRxEvent rx) => (rx.Length == 8 && rx.GetUINT16(0) == index) ? new RESPONSE?(RESPONSE.SUCCESS) : null);
						if (result.Item1 != 0 || result.Item2 == null)
						{
							return Tuple.Create<RESPONSE?, PidList>(result.Item1, null);
						}
						if (index++ == 0)
						{
							total = result.Item2.GetUINT16(2);
						}
						else if (list.Count < total)
						{
							list.Add(new PidInfo(tgtDevice, result.Item2.GetUINT16(2), result.Item2.GetUINT8(4)));
						}
						if (list.Count < total)
						{
							list.Add(new PidInfo(tgtDevice, result.Item2.GetUINT16(5), result.Item2.GetUINT8(7)));
						}
						if (list.Count >= total)
						{
							list.Sort((PidInfo first, PidInfo second) => first.ID.Value.CompareTo(second.ID.Value));
							pidList = new PidList(tgtDevice, list);
							PidListCache[signature] = pidList;
							operation.ReportProgress(100f, "Success!");
							return Tuple.Create((RESPONSE?)RESPONSE.SUCCESS, pidList);
						}
						float percent_complete = 100f * (float)list.Count / (float)total;
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(17, 2);
						defaultInterpolatedStringHandler.AppendLiteral("Read ");
						defaultInterpolatedStringHandler.AppendFormatted(list.Count);
						defaultInterpolatedStringHandler.AppendLiteral(" of ");
						defaultInterpolatedStringHandler.AppendFormatted(total);
						defaultInterpolatedStringHandler.AppendLiteral(" PIDs...");
						operation.ReportProgress(percent_complete, defaultInterpolatedStringHandler.ToStringAndClear());
					}
					finally
					{
						result?.Item2?.ReturnToPool();
					}
				}
			}

			public async Task<Tuple<RESPONSE?, UInt48?>> ReadPidAsync(AsyncOperation operation, IDevice tgtDevice, PID pid)
			{
				operation.ReportProgress("Reading PID " + pid.Value.ToString("X4") + "h from device...");
				Tuple<RESPONSE, CAN.MessageBuffer> result = null;
				try
				{
					result = await Device.TransmitRequestAsync(operation, tgtDevice, (byte)17, CAN.PAYLOAD.FromArgs((ushort)pid), delegate(LocalDeviceRxEvent rx)
					{
						if (rx.Length >= 3)
						{
							ushort uINT = rx.GetUINT16(0);
							if (uINT == (ushort)pid)
							{
								return RESPONSE.SUCCESS;
							}
							if (uINT == 0 && rx.Length == 5 && (ushort)pid == rx.GetUINT16(2))
							{
								return (RESPONSE)rx[4];
							}
							if (uINT == 0 && rx.Length == 7 && (ushort)pid == rx.GetUINT16(2))
							{
								return (RESPONSE)rx[4];
							}
						}
						return null;
					});
					if (result.Item1 != 0 || result.Item2 == null)
					{
						return Tuple.Create<RESPONSE?, UInt48?>(result.Item1, null);
					}
					UInt48 value = (byte)0;
					for (int i = 2; i < result.Item2.Length; i++)
					{
						value <<= 8;
						value += (UInt48)result.Item2.GetUINT8(i);
					}
					return Tuple.Create((RESPONSE?)RESPONSE.SUCCESS, (UInt48?)value);
				}
				finally
				{
					result?.Item2?.ReturnToPool();
				}
			}

			public async Task<Tuple<RESPONSE?, UInt48?>> ReadPidAsync(AsyncOperation operation, IDevice tgtDevice, PID pid, bool withadd, ushort add)
			{
				operation.ReportProgress("Reading PID " + pid.Value.ToString("X4") + "h from device...");
				CAN.PAYLOAD payload = ((!withadd) ? CAN.PAYLOAD.FromArgs((ushort)pid) : CAN.PAYLOAD.FromArgs((uint)(((ushort)pid << 16) | add)));
				Tuple<RESPONSE, CAN.MessageBuffer> result = null;
				try
				{
					result = await Device.TransmitRequestAsync(operation, tgtDevice, (byte)17, payload, delegate(LocalDeviceRxEvent rx)
					{
						if (rx.Length >= 3)
						{
							ushort uINT = rx.GetUINT16(0);
							if (uINT == (ushort)pid)
							{
								return RESPONSE.SUCCESS;
							}
							if (uINT == 0 && rx.Length == 5 && (ushort)pid == rx.GetUINT16(2))
							{
								return (RESPONSE)rx[4];
							}
							if (uINT == 0 && rx.Length == 7 && (ushort)pid == rx.GetUINT16(2))
							{
								return (RESPONSE)rx[4];
							}
						}
						return null;
					});
					if (result.Item1 != 0 || result.Item2 == null)
					{
						return Tuple.Create<RESPONSE?, UInt48?>(result.Item1, null);
					}
					UInt48 value = (byte)0;
					for (int i = 2; i < result.Item2.Length; i++)
					{
						value <<= 8;
						value += (UInt48)result.Item2.GetUINT8(i);
					}
					return Tuple.Create((RESPONSE?)RESPONSE.SUCCESS, (UInt48?)value);
				}
				finally
				{
					result?.Item2?.ReturnToPool();
				}
			}

			public async Task<Tuple<RESPONSE?, UInt48?>> ReadPidAsync(AsyncOperation operation, PidInfo pidInfo)
			{
				if (!pidInfo.IsReadable)
				{
					return Tuple.Create<RESPONSE?, UInt48?>(RESPONSE.WRITE_ONLY, null);
				}
				if (pidInfo.IsWithAddress)
				{
					return await ReadPidAsync(operation, pidInfo.Device, pidInfo.ID, pidInfo.IsWithAddress, pidInfo.PID_Address);
				}
				return await ReadPidAsync(operation, pidInfo.Device, pidInfo.ID);
			}

			public async Task<Tuple<RESPONSE?, UInt48?>> WritePidAsync(AsyncOperation operation, IDevice tgtDevice, PID pid, UInt48 value, ISessionClient session)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(29, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Writing PID ");
				defaultInterpolatedStringHandler.AppendFormatted(pid.Value.ToString("X4"));
				defaultInterpolatedStringHandler.AppendLiteral("h = ");
				defaultInterpolatedStringHandler.AppendFormatted(value.ToString());
				defaultInterpolatedStringHandler.AppendLiteral(" to device...");
				operation.ReportProgress(defaultInterpolatedStringHandler.ToStringAndClear());
				Tuple<RESPONSE, CAN.MessageBuffer> result = null;
				try
				{
					if (session != null)
					{
						while (!session.IsOpen)
						{
							session.TryOpenSession();
							await Task.Delay(15);
						}
					}
					result = await Device.TransmitRequestAsync(operation, tgtDevice, (byte)17, CAN.PAYLOAD.FromArgs((ushort)pid, value), delegate(LocalDeviceRxEvent rx)
					{
						if (session != null)
						{
							session.TryOpenSession();
						}
						if (rx.Length >= 3)
						{
							ushort uINT = rx.GetUINT16(0);
							if (uINT == (ushort)pid)
							{
								return RESPONSE.SUCCESS;
							}
							if (uINT == 0 && rx.Length == 5 && (ushort)pid == rx.GetUINT16(2))
							{
								return (RESPONSE)rx[4];
							}
							if (uINT == 0 && rx.Length == 7 && (ushort)pid == rx.GetUINT16(2))
							{
								return (RESPONSE)rx[4];
							}
						}
						return null;
					});
					if (result.Item1 != 0 || result.Item2 == null)
					{
						return Tuple.Create<RESPONSE?, UInt48?>(result.Item1, null);
					}
					value = (byte)0;
					for (int i = 2; i < result.Item2.Length; i++)
					{
						value <<= 8;
						value += (UInt48)result.Item2.GetUINT8(i);
					}
					return Tuple.Create((RESPONSE?)RESPONSE.SUCCESS, (UInt48?)value);
				}
				finally
				{
					result?.Item2?.ReturnToPool();
				}
			}
		}

		private class PidServer
		{
			private class LocalPid
			{
				private readonly Func<UInt48> ReadDelegate;

				private readonly Action<UInt48> WriteDelegate;

				private const byte READONLY = 1;

				private const byte WRITEONLY = 2;

				private const byte READWRITE = 3;

				private const byte NONVOLATILE = 4;

				private const byte WITHADDRESS = 8;

				public PID ID { get; private set; }

				public byte Flags { get; private set; }

				public UInt48? RawValue
				{
					get
					{
						return ReadDelegate?.Invoke();
					}
					set
					{
						if (value.HasValue)
						{
							WriteDelegate?.Invoke(value.Value);
						}
					}
				}

				public bool IsReadable => (Flags & 1) != 0;

				public bool IsWritable => (Flags & 2) != 0;

				public bool IsNonVolatile => (Flags & 4) != 0;

				public bool IsWithAddress => (Flags & 8) != 0;

				public LocalPid(PID id, Func<UInt48> read_delegate, Action<UInt48> write_delegate)
				{
					ID = id;
					ReadDelegate = read_delegate;
					WriteDelegate = write_delegate;
					if (read_delegate != null)
					{
						Flags = (byte)((write_delegate == null) ? 1 : 3);
					}
					else if (write_delegate != null)
					{
						Flags = 2;
					}
					else
					{
						Flags = 0;
					}
				}
			}

			private readonly LocalDevice Device;

			private Dictionary<PID, LocalPid> PidDict = new Dictionary<PID, LocalPid>();

			private List<LocalPid> PidList = new List<LocalPid>();

			private LocalPid this[PID id]
			{
				get
				{
					PidDict.TryGetValue(id, out var value);
					return value;
				}
			}

			public PidServer(LocalDevice device)
			{
				Device = device;
				Device.mRequestServer.AddRequestHandler((byte)16, Request10PidReadList);
				Device.mRequestServer.AddRequestHandler((byte)17, Request11PidReadWrite);
				Device.mRequestServer.AddRequestHandler((byte)18, Request12GetPidProperties);
			}

			public void Add(PID id, Func<UInt48> read_delegate, Action<UInt48> write_delegate)
			{
				if (PidDict.ContainsKey(id))
				{
					throw new InvalidOperationException("PID already exists in LocalDevice.PidServer");
				}
				LocalPid localPid = new LocalPid(id, read_delegate, write_delegate);
				PidDict.Add(id, localPid);
				PidList.Add(localPid);
			}

			public bool ContainsPid(PID id)
			{
				return this[id] != null;
			}

			public UInt48? ReadPidValue(PID id)
			{
				return this[id]?.RawValue;
			}

			private CAN.PAYLOAD? Request10PidReadList(AdapterRxEvent rx)
			{
				if (rx.Count != 2)
				{
					return CAN.PAYLOAD.FromArgs((byte)2);
				}
				int num = rx.GetUINT16(0);
				CAN.PAYLOAD value = CAN.PAYLOAD.FromArgs((ushort)num);
				if (num == 0)
				{
					value.Append((ushort)PidList.Count);
					value.Append((byte)0);
				}
				else
				{
					num = num * 2 - 1;
				}
				while (value.Length < 8)
				{
					LocalPid localPid = null;
					if (num < PidList.Count)
					{
						localPid = PidList[num++];
					}
					value.Append(localPid?.ID ?? ((PID)(ushort)0));
					value.Append(localPid?.Flags ?? 0);
				}
				return value;
			}

			private CAN.PAYLOAD? Request11PidReadWrite(AdapterRxEvent rx)
			{
				if (rx.Count < 2)
				{
					return CAN.PAYLOAD.FromArgs((byte)2);
				}
				PID pID = rx.GetUINT16(0);
				LocalPid localPid = this[pID];
				if (localPid == null)
				{
					return CAN.PAYLOAD.FromArgs((ushort)0, (ushort)pID, (byte)4);
				}
				UInt48 uInt = (byte)0;
				if (rx.Count == 2 || rx.Count == 4)
				{
					if (!localPid.IsReadable)
					{
						return CAN.PAYLOAD.FromArgs((ushort)0, (ushort)pID, (byte)8);
					}
					uInt = localPid.RawValue.Value;
				}
				else
				{
					if (!localPid.IsWritable)
					{
						return CAN.PAYLOAD.FromArgs((ushort)0, (ushort)pID, (byte)7);
					}
					UInt48 uInt2 = (byte)0;
					int num = 2;
					while (num < rx.Count)
					{
						uInt2 <<= 8;
						uInt2 |= (UInt48)rx[num++];
					}
					localPid.RawValue = uInt2;
					uInt = ((!localPid.IsReadable) ? uInt2 : localPid.RawValue.Value);
				}
				CAN.PAYLOAD value = CAN.PAYLOAD.FromArgs((ushort)pID);
				bool flag = false;
				int num2 = 0;
				while (num2 < 6)
				{
					if (num2 == 5)
					{
						flag = true;
					}
					if (num2 == 7)
					{
						flag = true;
					}
					flag |= ((long)uInt & 0xFF0000000000L) != 0;
					if (flag)
					{
						value.Append((byte)(uInt >> 40));
					}
					num2++;
					uInt <<= 8;
				}
				return value;
			}

			private CAN.PAYLOAD? Request12GetPidProperties(AdapterRxEvent rx)
			{
				if (rx.Count != 2)
				{
					return CAN.PAYLOAD.FromArgs((byte)2);
				}
				PID pID = rx.GetUINT16(0);
				LocalPid localPid = this[pID];
				if (localPid == null)
				{
					return CAN.PAYLOAD.FromArgs((ushort)pID, (byte)4);
				}
				SESSION_ID uNKNOWN = SESSION_ID.UNKNOWN;
				return CAN.PAYLOAD.FromArgs((ushort)pID, localPid.Flags, (ushort)uNKNOWN);
			}
		}

		private class RequestServer
		{
			private readonly LocalDevice Device;

			private readonly Func<AdapterRxEvent, CAN.PAYLOAD?>[] RequestDelegateTable = new Func<AdapterRxEvent, CAN.PAYLOAD?>[256];

			public RequestServer(LocalDevice device)
			{
				Device = device;
				AddRequestHandler((byte)0, Request00PartNumberRead);
				AddRequestHandler((byte)3, Request03SoftwareUpdateAuthorization);
			}

			public void AddRequestHandler(REQUEST num, Func<AdapterRxEvent, CAN.PAYLOAD?> handler)
			{
				RequestDelegateTable[(byte)num] = handler;
			}

			public void ProcessRequest(AdapterRxEvent rx)
			{
				REQUEST rEQUEST = rx.MessageData;
				CAN.PAYLOAD? pAYLOAD = null;
				pAYLOAD = ((RequestDelegateTable[(byte)rEQUEST] == null) ? new CAN.PAYLOAD?(CAN.PAYLOAD.FromArgs((byte)1)) : RequestDelegateTable[(byte)rEQUEST]?.Invoke(rx));
				if (pAYLOAD.HasValue)
				{
					ADDRESS address = Device.Address;
					if (address.IsValidDeviceAddress && (!pAYLOAD.HasValue || pAYLOAD.GetValueOrDefault().Length != 1 || pAYLOAD?[0] == 0 || rx.TargetAddress != ADDRESS.BROADCAST))
					{
						Device.Transmit(new CAN_ID((byte)129, address, rx.SourceAddress, rEQUEST), pAYLOAD.Value);
					}
				}
			}

			private CAN.PAYLOAD? Request00PartNumberRead(AdapterRxEvent rx)
			{
				if (rx.Count != 0)
				{
					return CAN.PAYLOAD.FromArgs((byte)2);
				}
				string softwarePartNumber = Device.SoftwarePartNumber;
				CAN.PAYLOAD value = new CAN.PAYLOAD(8);
				for (int i = 0; i < value.Length && i < softwarePartNumber.Length; i++)
				{
					value[i] = (byte)softwarePartNumber[i];
				}
				return value;
			}

			private CAN.PAYLOAD? Request03SoftwareUpdateAuthorization(AdapterRxEvent rx)
			{
				if (rx.Count != 1)
				{
					return CAN.PAYLOAD.FromArgs((byte)2);
				}
				if (Device.Address != Device.Product.Address)
				{
					if (rx.TargetAddress == ADDRESS.BROADCAST)
					{
						return CAN.PAYLOAD.FromArgs((byte)0);
					}
					return CAN.PAYLOAD.FromArgs(rx[0], (byte)6);
				}
				switch (rx[0])
				{
				default:
					return CAN.PAYLOAD.FromArgs(rx[0], (byte)2);
				case 0:
					Device.mLocalProduct.IsSoftwareUpdateAuthorized = false;
					return CAN.PAYLOAD.FromArgs(rx[0], (byte)0);
				case 1:
					if (!Device.mLocalProduct.IsSoftwareUpdateAvailable)
					{
						return CAN.PAYLOAD.FromArgs(rx[0], (byte)9);
					}
					Device.mLocalProduct.IsSoftwareUpdateAuthorized = true;
					return CAN.PAYLOAD.FromArgs(rx[0], (byte)0);
				}
			}
		}

		private class SessionServer : Disposable
		{
			public class LocalSession : Disposable, ISession
			{
				private class ClientEndpoint : IBusEndpoint
				{
					private LocalSession mHostedSession;

					private ADDRESS _address = ADDRESS.INVALID;

					public IAdapter Adapter { get; private set; }

					public ADDRESS Address
					{
						get
						{
							return _address;
						}
						set
						{
							if (_address != value)
							{
								if (_address.IsValidDeviceAddress)
								{
									ushort num = mHostedSession.SessionID;
									mHostedSession.LocalHost.Transmit29((byte)129, 69, this, CAN.PAYLOAD.FromArgs(num, (byte)0));
								}
								_address = value;
							}
						}
					}

					public bool IsOnline
					{
						get
						{
							if (Adapter.IsConnected)
							{
								return Address.IsValidDeviceAddress;
							}
							return false;
						}
					}

					public ClientEndpoint(IAdapter adapter, ADDRESS address, LocalSession sessionHost)
					{
						Adapter = adapter;
						Address = address;
						mHostedSession = sessionHost;
					}
				}

				private static readonly TimeSpan SESSION_TIMEOUT = TimeSpan.FromSeconds(5.0);

				private static readonly TimeSpan SEED_TIMEOUT = TimeSpan.FromSeconds(3.5);

				private readonly ILocalDevice LocalHost;

				private ClientEndpoint mClient;

				private Timer mOpenTime = new Timer();

				private Timer HeartbeatTimeout = new Timer();

				private Timer SeedTimeout = new Timer();

				private ADDRESS AddressSeedWasSentTo;

				private uint Seed;

				public SESSION_ID SessionID { get; private set; }

				public IBusEndpoint Host => LocalHost;

				public IBusEndpoint Client => mClient;

				public bool IsOpen => Client.Address.IsValidDeviceAddress;

				public TimeSpan OpenTime
				{
					get
					{
						if (!IsOpen)
						{
							return TimeSpan.Zero;
						}
						return mOpenTime.ElapsedTime;
					}
				}

				public void CloseSession()
				{
					mClient.Address = ADDRESS.INVALID;
				}

				public LocalSession(LocalDevice localhost, SESSION_ID session)
				{
					SessionID = session;
					LocalHost = localhost;
					mClient = new ClientEndpoint(localhost.Adapter, ADDRESS.INVALID, this);
				}

				public override void Dispose(bool disposing)
				{
					if (disposing)
					{
						mClient.Address = ADDRESS.INVALID;
					}
				}

				public void BackgroundTask()
				{
					if (mClient.Address.IsValidDeviceAddress && HeartbeatTimeout.ElapsedTime >= SESSION_TIMEOUT)
					{
						mClient.Address = ADDRESS.INVALID;
					}
				}

				public CAN.PAYLOAD? ProcessRequest(AdapterRxEvent rx, REQUEST request)
				{
					if (rx.Count < 2)
					{
						return CAN.PAYLOAD.FromArgs((byte)2);
					}
					SESSION_ID sESSION_ID = rx.GetUINT16(0);
					if (SessionID != sESSION_ID)
					{
						return CAN.PAYLOAD.FromArgs((ushort)sESSION_ID, (byte)4);
					}
					switch ((byte)request)
					{
					default:
						return CAN.PAYLOAD.FromArgs((byte)1);
					case 65:
					{
						if (rx.Count != 2)
						{
							return CAN.PAYLOAD.FromArgs((ushort)sESSION_ID, (byte)2);
						}
						ADDRESS address = Client.Address;
						return CAN.PAYLOAD.FromArgs((ushort)sESSION_ID, (byte)(address.IsValidDeviceAddress ? address : ADDRESS.BROADCAST), (uint)OpenTime.TotalMilliseconds);
					}
					case 66:
						if (rx.Count != 2 || !rx.SourceAddress.IsValidDeviceAddress)
						{
							return CAN.PAYLOAD.FromArgs((ushort)sESSION_ID, (byte)2);
						}
						if (IsOpen)
						{
							return CAN.PAYLOAD.FromArgs((ushort)sESSION_ID, (byte)11);
						}
						if ((ushort)SessionID == 4 && LocalHost.NetworkStatus.IsHazardousDevice && (byte)LocalHost.NetworkStatus.InMotionLockoutLevel >= 2)
						{
							return CAN.PAYLOAD.FromArgs((ushort)sESSION_ID, (byte)9);
						}
						Seed = (uint)ThreadLocalRandom.Next();
						AddressSeedWasSentTo = rx.SourceAddress;
						SeedTimeout.Reset();
						return CAN.PAYLOAD.FromArgs((ushort)sESSION_ID, Seed);
					case 67:
					{
						if (rx.Count != 6 || !rx.SourceAddress.IsValidDeviceAddress)
						{
							return CAN.PAYLOAD.FromArgs((ushort)sESSION_ID, (byte)2);
						}
						if (IsOpen)
						{
							return CAN.PAYLOAD.FromArgs((ushort)sESSION_ID, (byte)9);
						}
						if (rx.SourceAddress != AddressSeedWasSentTo)
						{
							return CAN.PAYLOAD.FromArgs((ushort)sESSION_ID, (byte)12);
						}
						AddressSeedWasSentTo = ADDRESS.INVALID;
						if (SeedTimeout.ElapsedTime > SEED_TIMEOUT)
						{
							return CAN.PAYLOAD.FromArgs((ushort)sESSION_ID, (byte)12);
						}
						uint uINT = rx.GetUINT32(2);
						uint num = SessionID.Encrypt(Seed);
						if (uINT != num)
						{
							return CAN.PAYLOAD.FromArgs((ushort)sESSION_ID, (byte)13);
						}
						if ((ushort)SessionID == 4 && LocalHost.NetworkStatus.IsHazardousDevice && (byte)LocalHost.NetworkStatus.InMotionLockoutLevel >= 2)
						{
							return CAN.PAYLOAD.FromArgs((ushort)sESSION_ID, (byte)9);
						}
						mClient.Address = rx.SourceAddress;
						mOpenTime.Reset();
						HeartbeatTimeout.Reset();
						return CAN.PAYLOAD.FromArgs((ushort)sESSION_ID);
					}
					case 68:
						if (rx.Count != 2)
						{
							return CAN.PAYLOAD.FromArgs((ushort)sESSION_ID, (byte)2);
						}
						if (!IsOpen || rx.SourceAddress != Client.Address)
						{
							return CAN.PAYLOAD.FromArgs((ushort)sESSION_ID, (byte)14);
						}
						HeartbeatTimeout.Reset();
						return null;
					case 69:
						if (rx.Count != 2)
						{
							return CAN.PAYLOAD.FromArgs((ushort)sESSION_ID, (byte)2);
						}
						if (!IsOpen || rx.SourceAddress != Client.Address)
						{
							return CAN.PAYLOAD.FromArgs((ushort)sESSION_ID, (byte)14);
						}
						mClient.Address = ADDRESS.INVALID;
						return null;
					}
				}
			}

			private readonly LocalDevice Device;

			private Dictionary<SESSION_ID, LocalSession> SessionDict = new Dictionary<SESSION_ID, LocalSession>();

			private List<SESSION_ID> SessionList = new List<SESSION_ID>();

			public LocalSession this[SESSION_ID id]
			{
				get
				{
					LocalSession value = null;
					SessionDict?.TryGetValue(id, out value);
					return value;
				}
			}

			public bool IsAnySessionOpen
			{
				get
				{
					foreach (LocalSession value in SessionDict.Values)
					{
						if (value.IsOpen)
						{
							return true;
						}
					}
					return false;
				}
			}

			public SessionServer(LocalDevice device)
			{
				Device = device;
				Device.mRequestServer.AddRequestHandler((byte)64, Request41ReadSessionList);
				Device.mRequestServer.AddRequestHandler((byte)65, (AdapterRxEvent rx) => ProcessRequest(rx, (byte)65));
				Device.mRequestServer.AddRequestHandler((byte)66, (AdapterRxEvent rx) => ProcessRequest(rx, (byte)66));
				Device.mRequestServer.AddRequestHandler((byte)67, (AdapterRxEvent rx) => ProcessRequest(rx, (byte)67));
				Device.mRequestServer.AddRequestHandler((byte)68, (AdapterRxEvent rx) => ProcessRequest(rx, (byte)68));
				Device.mRequestServer.AddRequestHandler((byte)69, (AdapterRxEvent rx) => ProcessRequest(rx, (byte)69));
			}

			public override void Dispose(bool disposing)
			{
				if (!disposing)
				{
					return;
				}
				foreach (LocalSession value in SessionDict.Values)
				{
					value.Dispose();
				}
				SessionDict = null;
				SessionList = null;
			}

			public void CloseCommandSessions()
			{
				foreach (LocalSession value in SessionDict.Values)
				{
					if ((ushort)value.SessionID == 4 && value.IsOpen)
					{
						value.CloseSession();
					}
				}
			}

			public ISession AddSessionSupport(SESSION_ID session)
			{
				lock (SessionDict)
				{
					if (!SessionDict.TryGetValue(session, out var value))
					{
						value = new LocalSession(Device, session);
						SessionDict.Add(session, value);
						SessionList.Add(session);
					}
					return value;
				}
			}

			private CAN.PAYLOAD? Request41ReadSessionList(AdapterRxEvent rx)
			{
				if (rx.Count != 2)
				{
					return CAN.PAYLOAD.FromArgs((byte)2);
				}
				int num = rx.GetUINT16(0);
				CAN.PAYLOAD value = CAN.PAYLOAD.FromArgs((ushort)num);
				if (num == 0)
				{
					value.Append((ushort)SessionList.Count);
				}
				else
				{
					num = num * 3 - 1;
				}
				while (value.Length < 8)
				{
					ushort value2 = 0;
					if (num < SessionList.Count)
					{
						value2 = SessionList[num++];
					}
					value.Append(value2);
				}
				return value;
			}

			private CAN.PAYLOAD? ProcessRequest(AdapterRxEvent rx, REQUEST request)
			{
				if (rx.Count < 2)
				{
					return CAN.PAYLOAD.FromArgs((byte)2);
				}
				SESSION_ID sESSION_ID = rx.GetUINT16(0);
				SessionDict.TryGetValue(sESSION_ID, out var value);
				if (value == null)
				{
					return CAN.PAYLOAD.FromArgs((ushort)sESSION_ID, (byte)4);
				}
				return value.ProcessRequest(rx, request);
			}

			public void BackgroundTask()
			{
				foreach (LocalSession value in SessionDict.Values)
				{
					value.BackgroundTask();
				}
			}
		}

		private readonly ResourcePool<ReusableSubscription> ReusableSubscriptionPool = new ResourcePool<ReusableSubscription>();

		private BlockClient mBlockClient;

		private BlockServer mBlockServer;

		private static readonly TimeSpan PRODUCT_STATUS_MESSAGE_PERIOD = TimeSpan.FromSeconds(1.0);

		private static readonly TimeSpan NETWORK_MESSAGE_PERIOD = TimeSpan.FromSeconds(1.0);

		private static readonly TimeSpan CIRCUIT_ID_MESSAGE_PERIOD = TimeSpan.FromSeconds(1.0);

		private static readonly TimeSpan DEVICE_ID_MESSAGE_PERIOD = TimeSpan.FromSeconds(1.0);

		private static readonly TimeSpan ADDRESS_CLAIM_TIMEOUT = TimeSpan.FromSeconds(1.0);

		private static readonly TimeSpan ADDRESS_DETECTED_TIMEOUT = TimeSpan.FromSeconds(5.0);

		private bool _isNotAcceptingCommands;

		private readonly LocalProduct mLocalProduct;

		private byte? mDeviceCapabilities;

		protected readonly SubscriptionManager Subscriptions;

		private AddressClaimManager AddressClaim;

		private MuteManager mMuteManager;

		private InMotionLockoutManager mInMotionLockoutManager;

		private Timer ProductStatusMsgTime = new Timer();

		private Timer NetworkMsgTime = new Timer();

		private Timer CircuitIDMsgTime = new Timer();

		private Timer DeviceIdMsgTime = new Timer();

		private Timer DeviceStatusMsgTime = new Timer();

		private readonly Timer Uptime = new Timer();

		private CAN.PAYLOAD? LastDeviceStatusTx;

		private bool mEnableDevice;

		private int TxBytesSent;

		private int TxMessagesSent;

		private int RxBytesReceived;

		private int RxMessagesReceived;

		private readonly LocalDeviceOnlineEvent LocalDeviceOnlineEvent;

		private readonly LocalDeviceOfflineEvent LocalDeviceOfflineEvent;

		private readonly LocalDeviceRxEvent LocalDeviceRxEvent;

		private PidClient mPidClient;

		private PidServer mPidServer;

		private RequestServer mRequestServer;

		private SessionServer mSessionServer;

		public IAdapter Adapter => Product.Adapter;

		public ADDRESS Address => AddressClaim.Address;

		public IProduct Product => mLocalProduct;

		public MAC MAC => Product.MAC;

		public IDS_CAN_VERSION_NUMBER ProtocolVersion => Product.ProtocolVersion;

		public PRODUCT_ID ProductID => Product.ProductID;

		public byte ProductInstance => Product.ProductInstance;

		public DEVICE_TYPE DeviceType { get; private set; } = (byte)0;


		public int DeviceInstance { get; private set; }

		public FUNCTION_NAME FunctionName { get; private set; } = FUNCTION_NAME.UNKNOWN;


		public int FunctionInstance { get; private set; }

		public CIRCUIT_ID CircuitID { get; private set; }

		public bool IsOnline => Address.IsValidDeviceAddress;

		public string SoftwarePartNumber => mLocalProduct.SoftwarePartNumber;

		public CAN.PAYLOAD DeviceStatus { get; protected set; }

		public LOCAL_DEVICE_OPTIONS Options { get; private set; }

		public LocalTextConsole TextConsole { get; private set; }

		ITextConsole IDevice.TextConsole => TextConsole;

		public byte? DeviceCapabilities
		{
			get
			{
				return mDeviceCapabilities;
			}
			protected set
			{
				if ((byte)ProtocolVersion <= 6)
				{
					if (value.HasValue)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(118, 2);
						defaultInterpolatedStringHandler.AppendLiteral("Failed to set IDS_CAN.LocalDevice().DeviceCapabilities = ");
						defaultInterpolatedStringHandler.AppendFormatted(value);
						defaultInterpolatedStringHandler.AppendLiteral(".  DeviceCapabilities must be set to null when using IDS-CAN ");
						defaultInterpolatedStringHandler.AppendFormatted(ProtocolVersion);
						throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
					}
				}
				else if (!value.HasValue)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(127, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Failed to set IDS_CAN.LocalDevice().DeviceCapabilities = ");
					defaultInterpolatedStringHandler.AppendFormatted(value);
					defaultInterpolatedStringHandler.AppendLiteral(".  DeviceCapabilities must be set to a valid value when using IDS-CAN ");
					defaultInterpolatedStringHandler.AppendFormatted(ProtocolVersion);
					throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				mDeviceCapabilities = value;
			}
		}

		public bool IsNotAcceptingCommands
		{
			get
			{
				return _isNotAcceptingCommands;
			}
			protected set
			{
				if (_isNotAcceptingCommands != value)
				{
					_isNotAcceptingCommands = value;
					if (_isNotAcceptingCommands)
					{
						mSessionServer.CloseCommandSessions();
					}
				}
			}
		}

		public IEventPublisher Events { get; private set; }

		public NETWORK_STATUS NetworkStatus
		{
			get
			{
				byte b = 0;
				if (mSessionServer.IsAnySessionOpen)
				{
					b = (byte)(b | 4u);
				}
				if (mInMotionLockoutManager != null)
				{
					IN_MOTION_LOCKOUT_LEVEL iN_MOTION_LOCKOUT_LEVEL = (mInMotionLockoutManager.IsInContention ? mInMotionLockoutManager.ProposedLevel : mInMotionLockoutManager.LockoutLevel);
					b = (byte)(b | (byte)(((byte)iN_MOTION_LOCKOUT_LEVEL & 3) << 3));
				}
				UInt48? uInt = mPidServer.ReadPidValue(PID.CLOUD_CAPABILITIES);
				if (uInt.HasValue && (long)uInt.Value != 0L)
				{
					b = (byte)(b | 0x40u);
				}
				if (IsHazardousDevice)
				{
					b = (byte)(b | 0x80u);
				}
				return b;
			}
		}

		public bool IsInMotionLockoutInContention => mInMotionLockoutManager?.IsInContention ?? false;

		public bool IsMuted => mMuteManager.IsMuted;

		public bool IsEnabled => mEnableDevice;

		public bool EnableDevice
		{
			get
			{
				return mEnableDevice;
			}
			set
			{
				mEnableDevice = value;
				AddressClaim.Enabled = EnableDevice && !IsMuted;
			}
		}

		protected virtual bool IsHazardousDevice => false;

		protected virtual IN_MOTION_LOCKOUT_LEVEL IsOkToClearInMotionLockout => (byte)0;

		private UInt48 SoftwareBuildDateTime => ((((((((((UInt48)(byte)25 << 8) | (byte)9) << 8) | (byte)12) << 8) | (byte)16) << 8) | (byte)18) << 8) | (byte)9;

		public async Task<Tuple<RESPONSE, CAN.MessageBuffer>> TransmitRequestAsync(AsyncOperation operation, IBusEndpoint target, REQUEST request, CAN.PAYLOAD payload, Func<LocalDeviceRxEvent, RESPONSE?> validator)
		{
			if (!target.IsOnline)
			{
				return Tuple.Create<RESPONSE, CAN.MessageBuffer>(RESPONSE.FAILED, null);
			}
			object mutex = new object();
			TaskCompletionSource<RESPONSE> tcs = new TaskCompletionSource<RESPONSE>();
			CAN.MessageBuffer retained_message = null;
			bool message_sent = false;
			ReusableSubscription rx_listener = ReusableSubscriptionPool.Get();
			rx_listener.SetDelegate(this, delegate(LocalDeviceRxEvent rx)
			{
				if (!message_sent)
				{
					return false;
				}
				if (rx.SourceAddress != target.Address)
				{
					return false;
				}
				if ((byte)rx.MessageType != 129)
				{
					return false;
				}
				if (rx.MessageData != (byte)request)
				{
					return false;
				}
				if (validator != null)
				{
					lock (mutex)
					{
						RESPONSE? rESPONSE = validator?.Invoke(rx);
						if (rESPONSE.HasValue)
						{
							validator = null;
							if (rESPONSE == RESPONSE.SUCCESS)
							{
								CAN.MessageBuffer @object = ResourcePool<CAN.MessageBuffer>.GetObject();
								@object.CopyFrom(rx);
								retained_message = @object;
							}
							tcs.SetResult(rESPONSE.Value);
							return true;
						}
					}
				}
				return false;
			});
			try
			{
				int delay = 500;
				while (IsOnline && target.IsOnline && !operation.IsCancellationRequested && !tcs.Task.IsCompleted)
				{
					if (!Transmit29((byte)128, request, target, payload))
					{
						await Task.Delay(5);
						continue;
					}
					message_sent = true;
					await Task.WhenAny(tcs.Task, Task.Delay(delay, operation.CancellationToken));
				}
			}
			catch (TimeoutException)
			{
			}
			catch (OperationCanceledException)
			{
			}
			finally
			{
				rx_listener.ReturnToPool();
				lock (mutex)
				{
					validator = null;
				}
			}
			if (retained_message != null)
			{
				return Tuple.Create(RESPONSE.SUCCESS, retained_message);
			}
			if (tcs.Task.IsCompleted)
			{
				return Tuple.Create<RESPONSE, CAN.MessageBuffer>(tcs.Task.Result, null);
			}
			if (operation.IsCancellationRequested)
			{
				return Tuple.Create<RESPONSE, CAN.MessageBuffer>(RESPONSE.CANCELLED, null);
			}
			return Tuple.Create<RESPONSE, CAN.MessageBuffer>(RESPONSE.FAILED, null);
		}

		private void InitBlockSupport()
		{
			mBlockClient = new BlockClient(this);
			mBlockServer = new BlockServer(this);
		}

		public async Task<Tuple<RESPONSE, IReadOnlyList<BLOCK_ID>>> ReadBlockListAsync(AsyncOperation operation, IDevice target)
		{
			return await mBlockClient.ReadBlockListAsync(operation, target);
		}

		public async Task<Tuple<RESPONSE, IBlock>> ReadBlockPropertiesAsync(AsyncOperation operation, IDevice target, BLOCK_ID block)
		{
			return await mBlockClient.ReadBlockPropertiesAsync(operation, target, block);
		}

		public async Task<Tuple<RESPONSE, uint?>> RecalculateBlockCrcAsync(AsyncOperation operation, IDevice target, BLOCK_ID block)
		{
			return await mBlockClient.RecalculateBlockCrcAsync(operation, target, block);
		}

		public async Task<Tuple<RESPONSE, IReadOnlyList<byte>>> ReadBlockDataAsync(AsyncOperation operation, IDevice target, BLOCK_ID block, int bulk_xfer_delay_ms, ISessionClient session)
		{
			return await mBlockClient.ReadBlockDataAsync(operation, target, block, bulk_xfer_delay_ms, session);
		}

		public async Task<Tuple<RESPONSE, IReadOnlyList<byte>>> ReadBlockDataAsync(AsyncOperation operation, IBlock block, int bulk_xfer_delay_ms, ISessionClient session)
		{
			return await mBlockClient.ReadBlockDataAsync(operation, block, bulk_xfer_delay_ms, session);
		}

		public async Task<RESPONSE> WriteBlockDataAsync(AsyncOperation operation, IBlock block, IReadOnlyList<byte> data, int bulk_xfer_delay_ms, ISessionClient session)
		{
			return await mBlockClient.WriteBlockDataAsync(operation, block, data, bulk_xfer_delay_ms, session);
		}

		protected void AddBlock(LocalBlock block)
		{
			mBlockServer.Add(block);
		}

		public LocalDevice(LocalProduct product, DEVICE_TYPE device_type, int device_instance, FUNCTION_NAME function_name, int function_instance, byte? capabilties, LOCAL_DEVICE_OPTIONS options)
		{
			mLocalProduct = product;
			Options = options;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
			foreach (ILocalDevice item in product)
			{
				if (item.DeviceType == device_type && item.DeviceInstance == device_instance)
				{
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(78, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Cannon construct LocalDevice(");
					defaultInterpolatedStringHandler.AppendFormatted(device_type);
					defaultInterpolatedStringHandler.AppendLiteral(" #");
					defaultInterpolatedStringHandler.AppendFormatted(device_instance);
					defaultInterpolatedStringHandler.AppendLiteral("), as it already exists within the LocalProduct");
					throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
			}
			if ((byte)ProtocolVersion <= 6)
			{
				if (capabilties.HasValue)
				{
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(144, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Failed to construct IDS_CAN.Devices.LocalDevice(). DEVICE_ID.DeviceCapabilities = ");
					defaultInterpolatedStringHandler.AppendFormatted(capabilties.Value.HexString());
					defaultInterpolatedStringHandler.AppendLiteral("h.  DeviceCapabilities must be set to null when using IDS-CAN ");
					defaultInterpolatedStringHandler.AppendFormatted(ProtocolVersion);
					throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
			}
			else if (!capabilties.HasValue)
			{
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(149, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Failed to construct IDS_CAN.Devices.LocalDevice(). DEVICE_ID.DeviceCapabilities = null.  DeviceCapabilities must be a valid value when using IDS-CAN ");
				defaultInterpolatedStringHandler.AppendFormatted(ProtocolVersion);
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			DeviceType = device_type;
			DeviceInstance = device_instance;
			FunctionName = function_name;
			FunctionInstance = function_instance;
			mDeviceCapabilities = capabilties;
			IsNotAcceptingCommands = false;
			defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(20, 1);
			defaultInterpolatedStringHandler.AppendLiteral("LocalDevice[");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceType);
			defaultInterpolatedStringHandler.AppendLiteral("].Events");
			Events = new EventPublisher(defaultInterpolatedStringHandler.ToStringAndClear());
			AddDisposable(Events);
			LocalDeviceOnlineEvent = new LocalDeviceOnlineEvent(this);
			LocalDeviceOfflineEvent = new LocalDeviceOfflineEvent(this);
			LocalDeviceRxEvent = new LocalDeviceRxEvent(this);
			Subscriptions = new SubscriptionManager();
			AddDisposable(Subscriptions);
			InitRequestServer();
			AddressClaim = new AddressClaimManager(this);
			AddDisposable(AddressClaim);
			mMuteManager = new MuteManager(this);
			InitSessionSupport();
			if (!options.HasFlag(LOCAL_DEVICE_OPTIONS.IGNORE_IN_MOTION_LOCKOUT))
			{
				mInMotionLockoutManager = new InMotionLockoutManager(this);
			}
			InitPidSupport();
			InitBlockSupport();
			mLocalProduct.AddDevice(this);
			Adapter.Events.Subscribe<Comm.AdapterOpenedEvent>(OnAdapterOpened, SubscriptionType.Weak, Subscriptions);
			Adapter.Events.Subscribe<Comm.AdapterClosedEvent>(OnAdapterClosed, SubscriptionType.Weak, Subscriptions);
			Adapter.Events.Subscribe<AdapterRxEvent>(OnAdapterRx, SubscriptionType.Weak, Subscriptions);
			PeriodicTask obj = new PeriodicTask(BackgroundTask, TimeSpan.FromMilliseconds(40.0), TimeSpan.FromMilliseconds(500.0), PeriodicTask.Type.FixedDelay);
			AddDisposable(obj);
		}

		public override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing)
			{
				EnableDevice = false;
				mLocalProduct.RemoveDevice(this);
				TextConsole = null;
			}
		}

		protected LocalTextConsole CreateTextConsole(TEXT_CONSOLE_SIZE size)
		{
			if (base.IsDisposed)
			{
				return null;
			}
			TextConsole = new LocalTextConsole(this, size);
			AddDisposable(TextConsole);
			return TextConsole;
		}

		protected void IncreaseInMotionLockoutLevel(IN_MOTION_LOCKOUT_LEVEL level)
		{
			mInMotionLockoutManager?.IncreaseLockoutLevel(level);
		}

		private void OnAdapterOpened(Comm.AdapterOpenedEvent e)
		{
			if (!base.IsDisposed)
			{
				AddressClaim.OnAdapterOpened(e);
			}
		}

		private void OnAdapterClosed(Comm.AdapterClosedEvent e)
		{
			if (!base.IsDisposed)
			{
				AddressClaim.OnAdapterClosed(e);
			}
		}

		private void OnAdapterRx(AdapterRxEvent rx)
		{
			if (base.IsDisposed)
			{
				return;
			}
			RxMessagesReceived++;
			RxBytesReceived += rx.Count;
			AddressClaim.OnAdapterRx(rx);
			mInMotionLockoutManager?.OnAdapterRx(rx);
			if (rx.TargetAddress == Address || rx.TargetAddress == ADDRESS.BROADCAST)
			{
				OnLocalDeviceRxEvent(rx);
				if ((byte)rx.MessageType == 128)
				{
					mRequestServer.ProcessRequest(rx);
				}
				LocalDeviceRxEvent.Publish(rx);
			}
		}

		protected virtual void OnLocalDeviceRxEvent(AdapterRxEvent rx)
		{
		}

		private void OnAddressChanged(ADDRESS address, ADDRESS prev)
		{
			if (base.IsDisposed)
			{
				return;
			}
			if (address.IsValidDeviceAddress)
			{
				mLocalProduct.SuggestNewProductAddress(address);
				TxBytesSent = 0;
				TxMessagesSent = 0;
				RxBytesReceived = 0;
				RxMessagesReceived = 0;
				LocalDeviceOnlineEvent.Publish();
				Adapter.Events.Publish(LocalDeviceOnlineEvent);
				ProductStatusMsgTime.ElapsedTime = PRODUCT_STATUS_MESSAGE_PERIOD;
				NetworkMsgTime.ElapsedTime = NETWORK_MESSAGE_PERIOD;
				CircuitIDMsgTime.ElapsedTime = CIRCUIT_ID_MESSAGE_PERIOD;
				DeviceIdMsgTime.ElapsedTime = DEVICE_ID_MESSAGE_PERIOD;
				DeviceStatusMsgTime.ElapsedTime = TimeSpan.FromDays(999.0);
			}
			else if (prev.IsValidDeviceAddress)
			{
				if (mLocalProduct.Address == prev)
				{
					mLocalProduct.ChooseNewProductAddress();
				}
				LocalDeviceOfflineEvent.Publish(prev);
				Adapter.Events.Publish(LocalDeviceOfflineEvent);
			}
		}

		private void BackgroundTask()
		{
			if (base.IsDisposed)
			{
				return;
			}
			OnBackgroundTask();
			mInMotionLockoutManager?.BackgroundTask();
			mMuteManager.BackgroundTask();
			AddressClaim.BackgroundTask();
			mSessionServer.BackgroundTask();
			if (!Adapter.IsConnected || !IsOnline)
			{
				return;
			}
			if (Product.ProductInstance == (byte)Address && ProductStatusMsgTime.ElapsedTime >= PRODUCT_STATUS_MESSAGE_PERIOD && Transmit11((byte)6, CAN.PAYLOAD.FromArgs((byte)Product.SoftwareUpdateState)))
			{
				ProductStatusMsgTime.Reset();
			}
			if (NetworkMsgTime.ElapsedTime >= NETWORK_MESSAGE_PERIOD && Transmit11((byte)0, CAN.PAYLOAD.FromArgs((byte)NetworkStatus, (byte)ProtocolVersion, (UInt48)MAC)))
			{
				NetworkMsgTime.Reset();
			}
			if (DeviceIdMsgTime.ElapsedTime >= DEVICE_ID_MESSAGE_PERIOD)
			{
				DEVICE_ID deviceID = this.GetDeviceID();
				if (deviceID.ProductInstance != 0)
				{
					bool flag = false;
					if ((!deviceID.DeviceCapabilities.HasValue) ? Transmit11((byte)2, CAN.PAYLOAD.FromArgs((ushort)deviceID.ProductID, deviceID.ProductInstance, (byte)deviceID.DeviceType, (ushort)deviceID.FunctionName, (byte)((uint)(deviceID.DeviceInstance << 4) | ((uint)deviceID.FunctionInstance & 0xFu)))) : Transmit11((byte)2, CAN.PAYLOAD.FromArgs((ushort)deviceID.ProductID, deviceID.ProductInstance, (byte)deviceID.DeviceType, (ushort)deviceID.FunctionName, (byte)((uint)(deviceID.DeviceInstance << 4) | ((uint)deviceID.FunctionInstance & 0xFu)), deviceID.DeviceCapabilities.Value)))
					{
						DeviceIdMsgTime.Reset();
					}
				}
			}
			if (CircuitIDMsgTime.ElapsedTime >= CIRCUIT_ID_MESSAGE_PERIOD)
			{
				CIRCUIT_ID circuitID = CircuitID;
				if (Transmit11((byte)1, CAN.PAYLOAD.FromArgs((uint)circuitID)))
				{
					CircuitIDMsgTime.Reset();
				}
			}
			CAN.PAYLOAD deviceStatus = DeviceStatus;
			CAN.PAYLOAD? lastDeviceStatusTx = LastDeviceStatusTx;
			TimeSpan timeSpan = ((!(deviceStatus == lastDeviceStatusTx)) ? (mSessionServer.IsAnySessionOpen ? TimeSpan.FromMilliseconds(50.0) : TimeSpan.FromMilliseconds(333.0)) : (mSessionServer.IsAnySessionOpen ? TimeSpan.FromMilliseconds(250.0) : TimeSpan.FromMilliseconds(1000.0)));
			if (DeviceStatusMsgTime.ElapsedTime >= timeSpan)
			{
				CAN.PAYLOAD deviceStatus2 = DeviceStatus;
				if (Transmit11((byte)3, deviceStatus2))
				{
					LastDeviceStatusTx = deviceStatus2;
					DeviceStatusMsgTime.Reset();
				}
			}
		}

		protected virtual void OnBackgroundTask()
		{
		}

		private bool Transmit(CAN.ID id, CAN.PAYLOAD payload = default(CAN.PAYLOAD))
		{
			if (!Adapter.Transmit(id, payload))
			{
				return false;
			}
			TxMessagesSent++;
			TxBytesSent += payload.Length;
			return true;
		}

		public bool Transmit11(MESSAGE_TYPE type, CAN.PAYLOAD payload = default(CAN.PAYLOAD))
		{
			if (!type.IsBroadcast)
			{
				throw new ArgumentException("MESSAGE_TYPE parameter must be 11-bit broadcast type");
			}
			if (!IsOnline)
			{
				return false;
			}
			return Transmit(new CAN_ID(type, Address), payload);
		}

		public bool Transmit29(MESSAGE_TYPE type, byte ext_data, IBusEndpoint target, CAN.PAYLOAD payload = default(CAN.PAYLOAD))
		{
			if (target.Adapter != Adapter)
			{
				throw new InvalidOperationException("Cannot transmit message between two IBusEndpoints on different adapters");
			}
			return Transmit29(type, ext_data, target.Address, payload);
		}

		public bool Transmit29(MESSAGE_TYPE type, byte ext_data, ADDRESS target, CAN.PAYLOAD payload = default(CAN.PAYLOAD))
		{
			if (target == null || !target.IsValidAddress)
			{
				return false;
			}
			if (!type.IsPointToPoint)
			{
				throw new ArgumentException("MESSAGE_TYPE parameter must be 11-bit broadcast type");
			}
			if (!IsOnline)
			{
				return false;
			}
			return Transmit(new CAN_ID(type, Address, target, ext_data), payload);
		}

		private void InitPidSupport()
		{
			mPidClient = new PidClient(this);
			mPidServer = new PidServer(this);
			AddPID(PID.CAN_ADAPTER_MAC, () => MAC);
			AddPID(PID.IDS_CAN_CIRCUIT_ID, () => (uint)CircuitID, delegate(UInt48 arg)
			{
				CircuitID = (uint)arg;
			});
			AddPID(PID.IDS_CAN_FUNCTION_NAME, () => (ushort)FunctionName, delegate(UInt48 arg)
			{
				FunctionName = (ushort)arg;
			});
			AddPID(PID.IDS_CAN_FUNCTION_INSTANCE, () => (byte)FunctionInstance, delegate(UInt48 arg)
			{
				FunctionInstance = (int)(arg & (byte)15);
			});
			AddPID(PID.IDS_CAN_NUM_DEVICES_ON_NETWORK, () => (uint)Adapter.Devices.NumDevicesDetectedOnNetwork);
			AddPID(PID.CAN_BYTES_TX, () => (UInt48)TxBytesSent);
			AddPID(PID.CAN_BYTES_RX, () => (UInt48)RxBytesReceived);
			AddPID(PID.CAN_MESSAGES_TX, () => (UInt48)TxMessagesSent);
			AddPID(PID.CAN_MESSAGES_RX, () => (UInt48)RxMessagesReceived);
			AddPID(PID.SYSTEM_UPTIME_MS, () => (UInt48)Uptime.ElapsedTime.TotalMilliseconds);
			AddPID(PID.TIME_ZONE, () => Adapter.Clock.TIME_ZONE);
			AddPID(PID.RTC_TIME_SEC, () => Adapter.Clock.RTC_TIME_SEC, delegate(UInt48 arg)
			{
				Adapter.Clock.RTC_TIME_SEC = (byte)arg;
			});
			AddPID(PID.RTC_TIME_MIN, () => Adapter.Clock.RTC_TIME_MIN, delegate(UInt48 arg)
			{
				Adapter.Clock.RTC_TIME_MIN = (byte)arg;
			});
			AddPID(PID.RTC_TIME_HOUR, () => Adapter.Clock.RTC_TIME_HOUR, delegate(UInt48 arg)
			{
				Adapter.Clock.RTC_TIME_HOUR = (byte)arg;
			});
			AddPID(PID.RTC_TIME_DAY, () => Adapter.Clock.RTC_TIME_DAY, delegate(UInt48 arg)
			{
				Adapter.Clock.RTC_TIME_DAY = (byte)arg;
			});
			AddPID(PID.RTC_TIME_MONTH, () => Adapter.Clock.RTC_TIME_MONTH, delegate(UInt48 arg)
			{
				Adapter.Clock.RTC_TIME_MONTH = (byte)arg;
			});
			AddPID(PID.RTC_TIME_YEAR, () => Adapter.Clock.RTC_TIME_YEAR, delegate(UInt48 arg)
			{
				Adapter.Clock.RTC_TIME_YEAR = (ushort)arg;
			});
			AddPID(PID.RTC_EPOCH_SEC, () => Adapter.Clock.RTC_EPOCH_SEC, delegate(UInt48 arg)
			{
				Adapter.Clock.RTC_EPOCH_SEC = (uint)arg;
			});
			AddPID(PID.RTC_SET_TIME_SEC, () => Adapter.Clock.RTC_SET_TIME_SEC);
			AddPID(PID.SOFTWARE_BUILD_DATE_TIME, () => SoftwareBuildDateTime);
		}

		public async Task<Tuple<RESPONSE?, PidList>> ReadPidListAsync(AsyncOperation operation, IDevice target)
		{
			return await mPidClient.ReadPidListAsync(operation, target);
		}

		public async Task<Tuple<RESPONSE?, UInt48?>> ReadPidAsync(AsyncOperation operation, IDevice target, PID id)
		{
			return await mPidClient.ReadPidAsync(operation, target, id);
		}

		public async Task<Tuple<RESPONSE?, UInt48?>> ReadPidAsync(AsyncOperation operation, IDevice target, PID id, bool withadd, ushort add)
		{
			return await mPidClient.ReadPidAsync(operation, target, id, withadd, add);
		}

		public async Task<Tuple<RESPONSE?, UInt48?>> ReadPidAsync(AsyncOperation operation, PidInfo pidInfo)
		{
			return await mPidClient.ReadPidAsync(operation, pidInfo);
		}

		public async Task<Tuple<RESPONSE?, UInt48?>> WritePidAsync(AsyncOperation operation, IDevice target, PID id, UInt48 value, ISessionClient session)
		{
			return await mPidClient.WritePidAsync(operation, target, id, value, session);
		}

		public async Task<Tuple<RESPONSE?, Int48?>> WritePidAsync(AsyncOperation operation, IDevice target, PID id, Int48 value, ISessionClient session)
		{
			Tuple<RESPONSE?, UInt48?> tuple = await mPidClient.WritePidAsync(operation, target, id, (UInt48)(long)value, session);
			if (!tuple.Item2.HasValue)
			{
				return Tuple.Create<RESPONSE?, Int48?>(tuple.Item1, null);
			}
			return Tuple.Create(tuple.Item1, (Int48?)(Int48)(ulong)tuple.Item2.Value);
		}

		protected void AddPID(PID id, Func<UInt48> read_delegate)
		{
			mPidServer.Add(id, read_delegate, null);
		}

		protected void AddPID(PID id, Action<UInt48> write_delegate)
		{
			mPidServer.Add(id, null, write_delegate);
		}

		protected void AddPID(PID id, Func<UInt48> read_delgate, Action<UInt48> write_delegate)
		{
			mPidServer.Add(id, read_delgate, write_delegate);
		}

		private void InitRequestServer()
		{
			mRequestServer = new RequestServer(this);
		}

		private void InitSessionSupport()
		{
			mSessionServer = new SessionServer(this);
			AddDisposable(mSessionServer);
			foreach (SESSION_ID item in SESSION_ID.GetEnumerator())
			{
				mSessionServer.AddSessionSupport(item);
			}
		}

		protected ISession AddLocalSessionSupport(SESSION_ID session)
		{
			return mSessionServer.AddSessionSupport(session);
		}

		protected ISession GetLocalSession(SESSION_ID id)
		{
			return mSessionServer[id];
		}

		protected ADDRESS GetLocalSessionClientAddress(SESSION_ID id)
		{
			ISession localSession = GetLocalSession(id);
			if (localSession != null && localSession.IsOpen)
			{
				return localSession.Client.Address;
			}
			return ADDRESS.INVALID;
		}
	}
}
