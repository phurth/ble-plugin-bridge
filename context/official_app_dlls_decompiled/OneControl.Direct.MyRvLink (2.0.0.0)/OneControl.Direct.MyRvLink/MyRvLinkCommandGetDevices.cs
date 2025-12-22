using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandGetDevices : MyRvLinkCommand
	{
		private const int MaxPayloadLength = 6;

		private const int DeviceTableIdIndex = 3;

		private const int StartDeviceIdIndex = 4;

		private const int MaxDeviceRequestCountIndex = 5;

		private readonly byte[] _rawData;

		private readonly List<IMyRvLinkDevice> _devices = new List<IMyRvLinkDevice>();

		protected virtual string LogTag { get; } = "MyRvLinkCommandGetDevices";


		protected override int MinPayloadLength => 6;

		public override ushort ClientCommandId => MyRvLinkCommand.DecodeClientCommandId(_rawData);

		public override MyRvLinkCommandType CommandType { get; } = MyRvLinkCommandType.GetDevices;


		public byte DeviceTableId => DecodeDeviceTableId(_rawData);

		public byte StartDeviceId => DecodeStartDeviceId(_rawData);

		public int MaxDeviceRequestCount => DecodeMaxDeviceRequestCount(_rawData);

		public int ResponseReceivedCount { get; private set; } = -1;


		public uint ResponseReceivedDeviceTableCrc { get; private set; }

		public List<IMyRvLinkDevice> Devices
		{
			get
			{
				if (!IsDeviceLoadingComplete(checkForCommandCompleted: true))
				{
					return new List<IMyRvLinkDevice>();
				}
				return _devices;
			}
		}

		public MyRvLinkCommandGetDevices(ushort clientCommandId, byte deviceTableId, byte startDeviceId, int maxDeviceRequestCount)
		{
			_rawData = new byte[6];
			_rawData.SetValueUInt16(clientCommandId, 0);
			_rawData[2] = (byte)CommandType;
			_rawData[3] = deviceTableId;
			_rawData[4] = startDeviceId;
			_rawData[5] = (byte)maxDeviceRequestCount;
			ValidateCommand(_rawData, clientCommandId);
		}

		protected MyRvLinkCommandGetDevices(IReadOnlyList<byte> rawData)
		{
			ValidateCommand(rawData);
			_rawData = rawData.ToNewArray(0, rawData.Count);
		}

		protected bool IsDeviceLoadingComplete(bool checkForCommandCompleted)
		{
			if (checkForCommandCompleted && base.ResponseState != MyRvLinkResponseState.Completed)
			{
				return false;
			}
			if (_devices.Count > MaxDeviceRequestCount)
			{
				return false;
			}
			if (ResponseReceivedCount != _devices.Count)
			{
				return false;
			}
			if (_devices.Count <= 0)
			{
				return false;
			}
			if (StartDeviceId == 0 && !(Enumerable.First(_devices) is MyRvLinkDeviceHost))
			{
				return false;
			}
			return true;
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
			if (!(commandResponse is MyRvLinkCommandGetDevicesResponse myRvLinkCommandGetDevicesResponse))
			{
				if (!(commandResponse is MyRvLinkCommandGetDevicesResponseCompleted myRvLinkCommandGetDevicesResponseCompleted))
				{
					if (commandResponse is MyRvLinkCommandResponseFailure myRvLinkCommandResponseFailure)
					{
						string logTag2 = LogTag;
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(15, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Command failed ");
						defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponseFailure);
						TaggedLog.Debug(logTag2, defaultInterpolatedStringHandler.ToStringAndClear());
						base.ResponseState = MyRvLinkResponseState.Failed;
					}
					else
					{
						string logTag3 = LogTag;
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(29, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Unexpected response received ");
						defaultInterpolatedStringHandler.AppendFormatted(commandResponse);
						TaggedLog.Debug(logTag3, defaultInterpolatedStringHandler.ToStringAndClear());
						base.ResponseState = MyRvLinkResponseState.Failed;
					}
				}
				else
				{
					try
					{
						ResponseReceivedCount = myRvLinkCommandGetDevicesResponseCompleted.DeviceCount;
						ResponseReceivedDeviceTableCrc = myRvLinkCommandGetDevicesResponseCompleted.DeviceTableCrc;
						if (IsDeviceLoadingComplete(checkForCommandCompleted: false))
						{
							base.ResponseState = MyRvLinkResponseState.Completed;
						}
						else
						{
							TaggedLog.Debug(LogTag, "Command completed, ALL devices were not received properly");
							base.ResponseState = MyRvLinkResponseState.Failed;
						}
					}
					catch (Exception ex)
					{
						TaggedLog.Debug(LogTag, "Unexpected response received, expected MyRvLinkCommandGetDevicesResponseCompleted: " + ex.Message);
						base.ResponseState = MyRvLinkResponseState.Failed;
					}
				}
			}
			else
			{
				try
				{
					foreach (IMyRvLinkDevice device in myRvLinkCommandGetDevicesResponse.Devices)
					{
						_devices.Add(device);
					}
				}
				catch (Exception ex2)
				{
					TaggedLog.Debug(LogTag, "Unable to decode response " + ex2.Message);
					base.ResponseState = MyRvLinkResponseState.Failed;
				}
			}
			return base.ResponseState != MyRvLinkResponseState.Pending;
		}

		protected static byte DecodeDeviceTableId(IReadOnlyList<byte> decodeBuffer)
		{
			return decodeBuffer[3];
		}

		protected static byte DecodeStartDeviceId(IReadOnlyList<byte> decodeBuffer)
		{
			return decodeBuffer[4];
		}

		protected static byte DecodeMaxDeviceRequestCount(IReadOnlyList<byte> decodeBuffer)
		{
			return decodeBuffer[5];
		}

		public static MyRvLinkCommandGetDevices Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkCommandGetDevices(rawData);
		}

		public override IReadOnlyList<byte> Encode()
		{
			return new ArraySegment<byte>(_rawData, 0, _rawData.Length);
		}

		public override IMyRvLinkCommandEvent DecodeCommandEvent(IMyRvLinkCommandEvent commandEvent)
		{
			if (commandEvent is MyRvLinkCommandResponseSuccess myRvLinkCommandResponseSuccess)
			{
				if (myRvLinkCommandResponseSuccess.IsCommandCompleted)
				{
					return new MyRvLinkCommandGetDevicesResponseCompleted(myRvLinkCommandResponseSuccess);
				}
				return new MyRvLinkCommandGetDevicesResponse(myRvLinkCommandResponseSuccess);
			}
			return commandEvent;
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(87, 6);
			defaultInterpolatedStringHandler.AppendFormatted(LogTag);
			defaultInterpolatedStringHandler.AppendLiteral("[Client Command Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(", Table Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceTableId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", Start Device Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(StartDeviceId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(" Max Request Device Count: ");
			defaultInterpolatedStringHandler.AppendFormatted(MaxDeviceRequestCount);
			defaultInterpolatedStringHandler.AppendLiteral("]: ");
			defaultInterpolatedStringHandler.AppendFormatted(_rawData.DebugDump());
			StringBuilder stringBuilder = new StringBuilder(defaultInterpolatedStringHandler.ToStringAndClear());
			if (IsDeviceLoadingComplete(checkForCommandCompleted: true))
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder3 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(41, 3, stringBuilder2);
				handler.AppendFormatted(Environment.NewLine);
				handler.AppendLiteral("    Received Device Count ");
				handler.AppendFormatted(ResponseReceivedCount);
				handler.AppendLiteral("  Device CRC 0x");
				handler.AppendFormatted(ResponseReceivedDeviceTableCrc, "X4");
				stringBuilder3.Append(ref handler);
				try
				{
					int startDeviceId = StartDeviceId;
					foreach (IMyRvLinkDevice device in Devices)
					{
						stringBuilder2 = stringBuilder;
						StringBuilder stringBuilder4 = stringBuilder2;
						handler = new StringBuilder.AppendInterpolatedStringHandler(8, 3, stringBuilder2);
						handler.AppendFormatted(Environment.NewLine);
						handler.AppendLiteral("    0x");
						handler.AppendFormatted(startDeviceId++, "X2");
						handler.AppendLiteral(": ");
						handler.AppendFormatted(device.ToString());
						stringBuilder4.Append(ref handler);
					}
				}
				catch (Exception ex)
				{
					stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder5 = stringBuilder2;
					handler = new StringBuilder.AppendInterpolatedStringHandler(31, 2, stringBuilder2);
					handler.AppendFormatted(Environment.NewLine);
					handler.AppendLiteral("    ERROR Trying to Get Device ");
					handler.AppendFormatted(ex.Message);
					stringBuilder5.Append(ref handler);
				}
			}
			else if (base.ResponseState != 0)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder6 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(43, 1, stringBuilder2);
				handler.AppendFormatted(Environment.NewLine);
				handler.AppendLiteral("    --- Device List Not Valid/Complete --- ");
				stringBuilder6.Append(ref handler);
			}
			return stringBuilder.ToString();
		}
	}
}
