using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandActionSwitch : MyRvLinkCommand
	{
		private const int DeviceTableIdIndex = 3;

		private const int DeviceStateIndex = 4;

		private const int FirstDeviceIdIndex = 5;

		private readonly byte[] _rawData;

		private readonly HashSet<byte> _successList = new HashSet<byte>();

		private readonly ConcurrentDictionary<byte, MyRvLinkCommandActionSwitchResponseFailure> _failureDict = new ConcurrentDictionary<byte, MyRvLinkCommandActionSwitchResponseFailure>();

		protected virtual string LogTag { get; } = "MyRvLinkCommandActionSwitch";


		protected override int MinPayloadLength => 5;

		public override ushort ClientCommandId => MyRvLinkCommand.DecodeClientCommandId(_rawData);

		public override MyRvLinkCommandType CommandType { get; } = MyRvLinkCommandType.ActionSwitch;


		public byte DeviceTableId => _rawData[3];

		public int DeviceCount => GetDeviceCount(_rawData);

		public MyRvLinkCommandActionSwitchState SwitchState => MyRvLinkCommandActionSwitchStateExtension.Decode(_rawData[4]);

		public bool IsCommandCompleted => base.ResponseState != MyRvLinkResponseState.Pending;

		private int MaxPayloadLength(int deviceCount)
		{
			return MinPayloadLength + deviceCount;
		}

		private static int GetDeviceCount(IReadOnlyList<byte> data)
		{
			if (data.Count > 5)
			{
				return data.Count - 5;
			}
			return 0;
		}

		public MyRvLinkCommandActionSwitch(ushort clientCommandId, byte deviceTableId, MyRvLinkCommandActionSwitchState switchState, params byte[] switchDeviceIdList)
		{
			int num = switchDeviceIdList.Length;
			if (num > 255)
			{
				throw new ArgumentOutOfRangeException("switchDeviceIdList", "Too Many Switches Specified");
			}
			if (num == 0)
			{
				throw new ArgumentOutOfRangeException("switchDeviceIdList", "Must specify at least 1 device");
			}
			_rawData = new byte[MaxPayloadLength(num)];
			_rawData.SetValueUInt16(clientCommandId, 0);
			_rawData[2] = (byte)CommandType;
			_rawData[3] = deviceTableId;
			_rawData[4] = switchState.Encode();
			int num2 = 5;
			foreach (byte b in switchDeviceIdList)
			{
				_rawData[num2] = b;
				num2++;
			}
			ValidateCommand(_rawData, clientCommandId);
		}

		protected MyRvLinkCommandActionSwitch(IReadOnlyList<byte> rawData)
		{
			ValidateCommand(rawData);
			int deviceCount = GetDeviceCount(rawData);
			if (deviceCount <= 0)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(65, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(CommandType);
				defaultInterpolatedStringHandler.AppendLiteral(" must contain at least 1 device bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			int num = MaxPayloadLength(deviceCount);
			if (rawData.Count > num)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(CommandType);
				defaultInterpolatedStringHandler.AppendLiteral(" received more then ");
				defaultInterpolatedStringHandler.AppendFormatted(num);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			_rawData = rawData.ToNewArray(0, rawData.Count);
		}

		public IEnumerable<byte> GetDeviceIds()
		{
			if (DeviceCount > 0)
			{
				for (int index = 0; index < DeviceCount; index++)
				{
					yield return _rawData[index + 5];
				}
			}
		}

		public IEnumerable<byte> GetSuccessDeviceIds()
		{
			return Enumerable.AsEnumerable(_successList);
		}

		public IEnumerable<MyRvLinkCommandActionSwitchResponseFailure> GetFailedDeviceIds2()
		{
			return Enumerable.AsEnumerable(_failureDict.Values);
		}

		public static MyRvLinkCommandActionSwitch Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkCommandActionSwitch(rawData);
		}

		public override IReadOnlyList<byte> Encode()
		{
			return new ArraySegment<byte>(_rawData, 0, _rawData.Length);
		}

		public override bool ProcessResponse(IMyRvLinkCommandResponse commandResponse)
		{
			if (base.ResponseState != 0)
			{
				string logTag = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(55, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Ignoring Process Command Response because command is ");
				defaultInterpolatedStringHandler.AppendFormatted(base.ResponseState);
				defaultInterpolatedStringHandler.AppendLiteral(": ");
				defaultInterpolatedStringHandler.AppendFormatted(commandResponse);
				TaggedLog.Debug(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
				return true;
			}
			if (!(commandResponse is MyRvLinkCommandActionSwitchResponseSuccess myRvLinkCommandActionSwitchResponseSuccess))
			{
				if (!(commandResponse is MyRvLinkCommandActionSwitchResponseFailure myRvLinkCommandActionSwitchResponseFailure))
				{
					if (!(commandResponse is MyRvLinkCommandResponseSuccess myRvLinkCommandResponseSuccess))
					{
						if (commandResponse is MyRvLinkCommandResponseFailure myRvLinkCommandResponseFailure)
						{
							string logTag2 = LogTag;
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(37, 1);
							defaultInterpolatedStringHandler.AppendLiteral("Command failed (BUT UNEXPECTED TYPE) ");
							defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponseFailure);
							TaggedLog.Warning(logTag2, defaultInterpolatedStringHandler.ToStringAndClear());
							if (myRvLinkCommandResponseFailure.IsCommandCompleted)
							{
								base.ResponseState = MyRvLinkResponseState.Failed;
							}
						}
						else
						{
							string logTag3 = LogTag;
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(29, 1);
							defaultInterpolatedStringHandler.AppendLiteral("Unexpected response received ");
							defaultInterpolatedStringHandler.AppendFormatted(commandResponse);
							TaggedLog.Debug(logTag3, defaultInterpolatedStringHandler.ToStringAndClear());
							if (commandResponse.IsCommandCompleted)
							{
								base.ResponseState = MyRvLinkResponseState.Failed;
							}
						}
					}
					else
					{
						string logTag4 = LogTag;
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(38, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Command success (BUT UNEXPECTED TYPE) ");
						defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponseSuccess);
						TaggedLog.Warning(logTag4, defaultInterpolatedStringHandler.ToStringAndClear());
						if (myRvLinkCommandResponseSuccess.IsCommandCompleted)
						{
							base.ResponseState = MyRvLinkResponseState.Completed;
						}
					}
				}
				else
				{
					try
					{
						byte? deviceId = myRvLinkCommandActionSwitchResponseFailure.DeviceId;
						if (deviceId.HasValue)
						{
							if (_successList.Contains(deviceId.Value))
							{
								DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(50, 1);
								defaultInterpolatedStringHandler.AppendLiteral("Device Id 0x");
								defaultInterpolatedStringHandler.AppendFormatted(deviceId, "X2");
								defaultInterpolatedStringHandler.AppendLiteral(" was previously reported as successful");
								throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
							}
							if (_failureDict.TryGetValue(deviceId.Value, out var myRvLinkCommandActionSwitchResponseFailure2))
							{
								DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 2);
								defaultInterpolatedStringHandler.AppendLiteral("Device Id 0x");
								defaultInterpolatedStringHandler.AppendFormatted(deviceId, "X2");
								defaultInterpolatedStringHandler.AppendLiteral(" was previously reported with a failure (");
								defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandActionSwitchResponseFailure2);
								defaultInterpolatedStringHandler.AppendLiteral(")");
								throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
							}
							_failureDict[deviceId.Value] = myRvLinkCommandActionSwitchResponseFailure;
						}
					}
					catch (Exception ex)
					{
						string logTag5 = LogTag;
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 2);
						defaultInterpolatedStringHandler.AppendLiteral("Invalid Failure response ");
						defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandActionSwitchResponseFailure);
						defaultInterpolatedStringHandler.AppendLiteral(": ");
						defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
						TaggedLog.Debug(logTag5, defaultInterpolatedStringHandler.ToStringAndClear());
					}
					if (myRvLinkCommandActionSwitchResponseFailure.IsCommandCompleted)
					{
						base.ResponseState = MyRvLinkResponseState.Failed;
					}
				}
			}
			else
			{
				try
				{
					if (!myRvLinkCommandActionSwitchResponseSuccess.HasDeviceId)
					{
						throw new MyRvLinkException("Missing Device Id");
					}
					byte deviceId2 = myRvLinkCommandActionSwitchResponseSuccess.DeviceId;
					if (_successList.Contains(deviceId2))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(50, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Device Id 0x");
						defaultInterpolatedStringHandler.AppendFormatted(deviceId2, "X2");
						defaultInterpolatedStringHandler.AppendLiteral(" was previously reported as successful");
						throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					if (_failureDict.TryGetValue(deviceId2, out var myRvLinkCommandActionSwitchResponseFailure3))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(53, 2);
						defaultInterpolatedStringHandler.AppendLiteral("Device Id was 0x");
						defaultInterpolatedStringHandler.AppendFormatted(deviceId2, "X2");
						defaultInterpolatedStringHandler.AppendLiteral("  previously reported with a failure ");
						defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandActionSwitchResponseFailure3);
						throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					_successList.Add(deviceId2);
				}
				catch (Exception ex2)
				{
					string logTag6 = LogTag;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Invalid Success response ");
					defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandActionSwitchResponseSuccess);
					defaultInterpolatedStringHandler.AppendLiteral(": ");
					defaultInterpolatedStringHandler.AppendFormatted(ex2.Message);
					TaggedLog.Debug(logTag6, defaultInterpolatedStringHandler.ToStringAndClear());
				}
				if (myRvLinkCommandActionSwitchResponseSuccess.IsCommandCompleted)
				{
					base.ResponseState = MyRvLinkResponseState.Completed;
				}
			}
			if (commandResponse.IsCommandCompleted && !DidGetResponseForAllDevices())
			{
				TaggedLog.Warning(LogTag, "Command completed, but didn't receive responses from all devices\n" + ToString());
			}
			return base.ResponseState != MyRvLinkResponseState.Pending;
		}

		public bool DidGetResponseForAllDevices()
		{
			if (!IsCommandCompleted)
			{
				return false;
			}
			return _successList.Count + _failureDict.Count == DeviceCount;
		}

		public override IMyRvLinkCommandEvent DecodeCommandEvent(IMyRvLinkCommandEvent commandEvent)
		{
			if (!(commandEvent is MyRvLinkCommandResponseSuccess response))
			{
				if (commandEvent is MyRvLinkCommandResponseFailure response2)
				{
					return new MyRvLinkCommandActionSwitchResponseFailure(response2);
				}
				string logTag = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(45, 1);
				defaultInterpolatedStringHandler.AppendLiteral("DecodeCommandEvent unexpected event type for ");
				defaultInterpolatedStringHandler.AppendFormatted(commandEvent);
				TaggedLog.Warning(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
				return commandEvent;
			}
			return new MyRvLinkCommandActionSwitchResponseSuccess(response);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(68, 5);
			defaultInterpolatedStringHandler.AppendFormatted(LogTag);
			defaultInterpolatedStringHandler.AppendLiteral("[Client Command Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(", Table Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceTableId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", Device Count: ");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceCount);
			defaultInterpolatedStringHandler.AppendLiteral(", Set State: ");
			defaultInterpolatedStringHandler.AppendFormatted(SwitchState.DebugDumpAsFlags());
			defaultInterpolatedStringHandler.AppendLiteral("]: ");
			StringBuilder stringBuilder = new StringBuilder(defaultInterpolatedStringHandler.ToStringAndClear());
			if (DeviceCount <= 0)
			{
				stringBuilder.Append("No Devices");
			}
			else
			{
				byte[] array = Enumerable.ToArray(GetDeviceIds());
				foreach (byte b in array)
				{
					MyRvLinkCommandActionSwitchResponseFailure myRvLinkCommandActionSwitchResponseFailure;
					if (!IsCommandCompleted)
					{
						StringBuilder stringBuilder2 = stringBuilder;
						StringBuilder stringBuilder3 = stringBuilder2;
						StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(30, 2, stringBuilder2);
						handler.AppendFormatted(Environment.NewLine);
						handler.AppendLiteral("    Device ID 0x");
						handler.AppendFormatted(b, "X2");
						handler.AppendLiteral(" Not Completed");
						stringBuilder3.Append(ref handler);
					}
					else if (_successList.Contains(b))
					{
						StringBuilder stringBuilder2 = stringBuilder;
						StringBuilder stringBuilder4 = stringBuilder2;
						StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(24, 2, stringBuilder2);
						handler.AppendFormatted(Environment.NewLine);
						handler.AppendLiteral("    Device ID 0x");
						handler.AppendFormatted(b, "X2");
						handler.AppendLiteral(" Success");
						stringBuilder4.Append(ref handler);
					}
					else if (_failureDict.TryGetValue(b, out myRvLinkCommandActionSwitchResponseFailure))
					{
						StringBuilder stringBuilder2 = stringBuilder;
						StringBuilder stringBuilder5 = stringBuilder2;
						StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(17, 3, stringBuilder2);
						handler.AppendFormatted(Environment.NewLine);
						handler.AppendLiteral("    Device ID 0x");
						handler.AppendFormatted(b, "X2");
						handler.AppendLiteral(" ");
						handler.AppendFormatted(myRvLinkCommandActionSwitchResponseFailure);
						stringBuilder5.Append(ref handler);
					}
					else
					{
						StringBuilder stringBuilder2 = stringBuilder;
						StringBuilder stringBuilder6 = stringBuilder2;
						StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(67, 2, stringBuilder2);
						handler.AppendFormatted(Environment.NewLine);
						handler.AppendLiteral("    Device ID 0x");
						handler.AppendFormatted(b, "X2");
						handler.AppendLiteral(" Completed but didn't report success/failure status");
						stringBuilder6.Append(ref handler);
					}
				}
				if (IsCommandCompleted && !DidGetResponseForAllDevices())
				{
					StringBuilder stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder7 = stringBuilder2;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(44, 1, stringBuilder2);
					handler.AppendFormatted(Environment.NewLine);
					handler.AppendLiteral("    DIDN'T RECEIVE RESPONSES FOR ALL DEVICES");
					stringBuilder7.Append(ref handler);
				}
			}
			return stringBuilder.ToString();
		}
	}
}
