using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandGetDevicesMetadata : MyRvLinkCommand
	{
		private const int MaxPayloadLength = 6;

		private const int DeviceTableIdIndex = 3;

		private const int StartDeviceIdIndex = 4;

		private const int MaxDeviceRequestCountIndex = 5;

		private readonly byte[] _rawData;

		private readonly List<IMyRvLinkDeviceMetadata> _devicesMetadata = new List<IMyRvLinkDeviceMetadata>();

		protected virtual string LogTag { get; } = "MyRvLinkCommandGetDevicesMetadata";


		protected override int MinPayloadLength => 6;

		public override ushort ClientCommandId => MyRvLinkCommand.DecodeClientCommandId(_rawData);

		public override MyRvLinkCommandType CommandType { get; } = MyRvLinkCommandType.GetDevicesMetadata;


		public byte DeviceTableId => DecodeDeviceTableId(_rawData);

		public byte StartDeviceId => DecodeStartDeviceId(_rawData);

		public int MaxDeviceRequestCount => DecodeMaxDeviceRequestCount(_rawData);

		public int ResponseReceivedCount { get; private set; } = -1;


		public uint ResponseReceivedMetadataTableCrc { get; private set; }

		public List<IMyRvLinkDeviceMetadata> DevicesMetadata
		{
			get
			{
				if (!IsMetadataLoadingComplete(checkForCommandCompleted: true))
				{
					return new List<IMyRvLinkDeviceMetadata>();
				}
				return _devicesMetadata;
			}
		}

		public MyRvLinkCommandGetDevicesMetadata(ushort clientCommandId, byte deviceTableId, byte startDeviceId, int maxDeviceRequestCount)
		{
			_rawData = new byte[6];
			_rawData.SetValueUInt16(clientCommandId, 0);
			_rawData[2] = (byte)CommandType;
			_rawData[3] = deviceTableId;
			_rawData[4] = startDeviceId;
			_rawData[5] = (byte)maxDeviceRequestCount;
			ValidateCommand(_rawData, clientCommandId);
		}

		protected MyRvLinkCommandGetDevicesMetadata(IReadOnlyList<byte> rawData)
		{
			ValidateCommand(rawData);
			_rawData = rawData.ToNewArray(0, rawData.Count);
		}

		protected bool IsMetadataLoadingComplete(bool checkForCommandCompleted)
		{
			if (checkForCommandCompleted && base.ResponseState != MyRvLinkResponseState.Completed)
			{
				return false;
			}
			if (_devicesMetadata.Count > MaxDeviceRequestCount)
			{
				return false;
			}
			if (ResponseReceivedCount != _devicesMetadata.Count)
			{
				return false;
			}
			if (_devicesMetadata.Count <= 0)
			{
				return false;
			}
			if (StartDeviceId == 0 && !(Enumerable.First(_devicesMetadata) is MyRvLinkDeviceHostMetadata))
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
			if (!(commandResponse is MyRvLinkCommandGetDevicesMetadataResponse myRvLinkCommandGetDevicesMetadataResponse))
			{
				if (!(commandResponse is MyRvLinkCommandGetDevicesMetadataResponseCompleted myRvLinkCommandGetDevicesMetadataResponseCompleted))
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
						ResponseReceivedCount = myRvLinkCommandGetDevicesMetadataResponseCompleted.DeviceCount;
						ResponseReceivedMetadataTableCrc = myRvLinkCommandGetDevicesMetadataResponseCompleted.DeviceMetadataTableCrc;
						if (IsMetadataLoadingComplete(checkForCommandCompleted: false))
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
					foreach (IMyRvLinkDeviceMetadata devicesMetadatum in myRvLinkCommandGetDevicesMetadataResponse.DevicesMetadata)
					{
						_devicesMetadata.Add(devicesMetadatum);
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

		public static MyRvLinkCommandGetDevicesMetadata Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkCommandGetDevicesMetadata(rawData);
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
					return new MyRvLinkCommandGetDevicesMetadataResponseCompleted(myRvLinkCommandResponseSuccess);
				}
				return new MyRvLinkCommandGetDevicesMetadataResponse(myRvLinkCommandResponseSuccess);
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
			if (IsMetadataLoadingComplete(checkForCommandCompleted: true))
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder3 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(51, 2, stringBuilder2);
				handler.AppendLiteral("\n    Received Device Metadata Count ");
				handler.AppendFormatted(ResponseReceivedCount);
				handler.AppendLiteral("  Device CRC 0x");
				handler.AppendFormatted(ResponseReceivedMetadataTableCrc, "X8");
				stringBuilder3.Append(ref handler);
				try
				{
					int startDeviceId = StartDeviceId;
					foreach (IMyRvLinkDeviceMetadata devicesMetadatum in DevicesMetadata)
					{
						stringBuilder2 = stringBuilder;
						StringBuilder stringBuilder4 = stringBuilder2;
						handler = new StringBuilder.AppendInterpolatedStringHandler(9, 2, stringBuilder2);
						handler.AppendLiteral("\n    0x");
						handler.AppendFormatted(startDeviceId++, "X2");
						handler.AppendLiteral(": ");
						handler.AppendFormatted(devicesMetadatum.ToString());
						stringBuilder4.Append(ref handler);
					}
				}
				catch (Exception ex)
				{
					stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder5 = stringBuilder2;
					handler = new StringBuilder.AppendInterpolatedStringHandler(42, 1, stringBuilder2);
					handler.AppendLiteral("\n    ERROR Trying to Get Device Metadata: ");
					handler.AppendFormatted(ex.Message);
					stringBuilder5.Append(ref handler);
				}
			}
			else if (base.ResponseState != 0)
			{
				stringBuilder.Append("\n    --- Device Metadata List Not Valid/Complete --- ");
			}
			return stringBuilder.ToString();
		}
	}
}
