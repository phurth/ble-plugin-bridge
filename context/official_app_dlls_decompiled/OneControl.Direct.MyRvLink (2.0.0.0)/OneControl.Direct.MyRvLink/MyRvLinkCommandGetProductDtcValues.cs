using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandGetProductDtcValues : MyRvLinkCommand
	{
		private const byte DtcFilterMask = 7;

		private const int MaxPayloadLength = 10;

		private const int DeviceTableIdIndex = 3;

		private const int DeviceIdIndex = 4;

		private const int OptionIndex = 5;

		private const int StartDtcIndex = 6;

		private const int EndDtcIndex = 8;

		private readonly byte[] _rawData;

		private readonly Dictionary<DTC_ID, DtcValue> _dtcDict = new Dictionary<DTC_ID, DtcValue>();

		protected virtual string LogTag { get; } = "MyRvLinkCommandGetProductDtcValues";


		public override MyRvLinkCommandType CommandType { get; } = MyRvLinkCommandType.GetProductDtcValues;


		protected override int MinPayloadLength => 10;

		public override ushort ClientCommandId => MyRvLinkCommand.DecodeClientCommandId(_rawData);

		public byte DeviceTableId => _rawData[3];

		public byte DeviceId => _rawData[4];

		public LogicalDeviceDtcFilter DtcFilter => (LogicalDeviceDtcFilter)(_rawData[5] & 7);

		public DTC_ID StartDtcId => (DTC_ID)_rawData.GetValueUInt16(6);

		public DTC_ID EndDtcId => (DTC_ID)_rawData.GetValueUInt16(8);

		public bool IsDeviceLoadingCompleted { get; private set; }

		public Dictionary<DTC_ID, DtcValue> DtcDict
		{
			get
			{
				if (!IsDeviceLoadingCompleted)
				{
					return new Dictionary<DTC_ID, DtcValue>();
				}
				return _dtcDict;
			}
		}

		public MyRvLinkCommandGetProductDtcValues(ushort clientCommandId, byte deviceTableId, byte deviceId, LogicalDeviceDtcFilter dtcFilter, DTC_ID startDtcId, DTC_ID endDtcId)
		{
			_rawData = new byte[10];
			_rawData.SetValueUInt16(clientCommandId, 0);
			_rawData[2] = (byte)CommandType;
			_rawData[3] = deviceTableId;
			_rawData[4] = deviceId;
			_rawData[5] = (byte)((byte)dtcFilter & 7u);
			_rawData.SetValueUInt16((ushort)startDtcId, 6);
			_rawData.SetValueUInt16((ushort)endDtcId, 8);
			ValidateCommand(_rawData, clientCommandId);
		}

		protected MyRvLinkCommandGetProductDtcValues(IReadOnlyList<byte> rawData)
		{
			ValidateCommand(rawData);
			_rawData = rawData.ToNewArray(0, rawData.Count);
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
			if (!(commandResponse is MyRvLinkCommandGetProductDtcValuesResponse myRvLinkCommandGetProductDtcValuesResponse))
			{
				if (!(commandResponse is MyRvLinkCommandGetProductDtcValuesResponseCompleted myRvLinkCommandGetProductDtcValuesResponseCompleted))
				{
					if (commandResponse is MyRvLinkCommandResponseFailure myRvLinkCommandResponseFailure)
					{
						string logTag2 = LogTag;
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(15, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Command failed ");
						defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponseFailure);
						TaggedLog.Debug(logTag2, defaultInterpolatedStringHandler.ToStringAndClear());
						IsDeviceLoadingCompleted = false;
						base.ResponseState = MyRvLinkResponseState.Failed;
					}
					else
					{
						string logTag3 = LogTag;
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(29, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Unexpected response received ");
						defaultInterpolatedStringHandler.AppendFormatted(commandResponse);
						TaggedLog.Debug(logTag3, defaultInterpolatedStringHandler.ToStringAndClear());
						IsDeviceLoadingCompleted = false;
						base.ResponseState = MyRvLinkResponseState.Failed;
					}
				}
				else
				{
					try
					{
						if (myRvLinkCommandGetProductDtcValuesResponseCompleted.DtcCount != _dtcDict.Count)
						{
							IReadOnlyList<byte> readOnlyList = myRvLinkCommandGetProductDtcValuesResponseCompleted.Encode();
							string logTag4 = LogTag;
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(67, 3);
							defaultInterpolatedStringHandler.AppendLiteral("Should have received ");
							defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandGetProductDtcValuesResponseCompleted.DtcCount);
							defaultInterpolatedStringHandler.AppendLiteral(" DTCs, but received ");
							defaultInterpolatedStringHandler.AppendFormatted(_dtcDict.Count);
							defaultInterpolatedStringHandler.AppendLiteral(" DTCs: Response Complete: ");
							defaultInterpolatedStringHandler.AppendFormatted(readOnlyList.DebugDump(0, readOnlyList.Count));
							TaggedLog.Debug(logTag4, defaultInterpolatedStringHandler.ToStringAndClear());
							_dtcDict.Clear();
							IsDeviceLoadingCompleted = false;
							base.ResponseState = MyRvLinkResponseState.Failed;
						}
						else
						{
							IsDeviceLoadingCompleted = true;
							base.ResponseState = MyRvLinkResponseState.Completed;
						}
					}
					catch (Exception ex)
					{
						TaggedLog.Debug(LogTag, "Command completed, ALL DTCs were not received properly: " + ex.Message);
						base.ResponseState = MyRvLinkResponseState.Failed;
					}
				}
			}
			else
			{
				try
				{
					foreach (KeyValuePair<DTC_ID, DtcValue> item in myRvLinkCommandGetProductDtcValuesResponse.DtcDict)
					{
						if (_dtcDict.ContainsKey(item.Key))
						{
							string logTag5 = LogTag;
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(68, 1);
							defaultInterpolatedStringHandler.AppendLiteral("IGNORING: Duplicate DTC ");
							defaultInterpolatedStringHandler.AppendFormatted(item.Key);
							defaultInterpolatedStringHandler.AppendLiteral(", value already returned in another response");
							TaggedLog.Debug(logTag5, defaultInterpolatedStringHandler.ToStringAndClear());
						}
						else
						{
							_dtcDict.Add(item.Key, item.Value);
						}
					}
				}
				catch (Exception ex2)
				{
					TaggedLog.Debug(LogTag, "Command completed, ALL DTCs were not received properly: " + ex2.Message);
					base.ResponseState = MyRvLinkResponseState.Failed;
				}
			}
			return base.ResponseState != MyRvLinkResponseState.Pending;
		}

		public static MyRvLinkCommandGetProductDtcValues Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkCommandGetProductDtcValues(rawData);
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
					return new MyRvLinkCommandGetProductDtcValuesResponseCompleted(myRvLinkCommandResponseSuccess);
				}
				return new MyRvLinkCommandGetProductDtcValuesResponse(myRvLinkCommandResponseSuccess);
			}
			return commandEvent;
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(85, 8);
			defaultInterpolatedStringHandler.AppendFormatted(LogTag);
			defaultInterpolatedStringHandler.AppendLiteral("[Client Command Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(", Table Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceTableId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", Device Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", Filter: ");
			defaultInterpolatedStringHandler.AppendFormatted(DtcFilter);
			defaultInterpolatedStringHandler.AppendLiteral(", Start: 0x");
			defaultInterpolatedStringHandler.AppendFormatted((ushort)StartDtcId, "X");
			defaultInterpolatedStringHandler.AppendLiteral(", End: 0x");
			defaultInterpolatedStringHandler.AppendFormatted((ushort)EndDtcId, "X");
			defaultInterpolatedStringHandler.AppendLiteral(" ]: ");
			defaultInterpolatedStringHandler.AppendFormatted(_rawData.DebugDump());
			StringBuilder stringBuilder = new StringBuilder(defaultInterpolatedStringHandler.ToStringAndClear());
			if (IsDeviceLoadingCompleted)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder3 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(23, 2, stringBuilder2);
				handler.AppendFormatted(Environment.NewLine);
				handler.AppendLiteral("    Received DTC Count ");
				handler.AppendFormatted(DtcDict.Count);
				stringBuilder3.Append(ref handler);
				try
				{
					foreach (KeyValuePair<DTC_ID, DtcValue> item in DtcDict)
					{
						stringBuilder2 = stringBuilder;
						StringBuilder stringBuilder4 = stringBuilder2;
						handler = new StringBuilder.AppendInterpolatedStringHandler(25, 5, stringBuilder2);
						handler.AppendFormatted(Environment.NewLine);
						handler.AppendLiteral("    ");
						handler.AppendFormatted(item.Key);
						handler.AppendLiteral(": ");
						handler.AppendFormatted(item.Value.PowerCyclesCounter);
						handler.AppendLiteral(", Active: ");
						handler.AppendFormatted(item.Value.IsActive);
						handler.AppendLiteral(" Stored: ");
						handler.AppendFormatted(item.Value.IsStored);
						stringBuilder4.Append(ref handler);
					}
				}
				catch (Exception ex)
				{
					stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder5 = stringBuilder2;
					handler = new StringBuilder.AppendInterpolatedStringHandler(28, 2, stringBuilder2);
					handler.AppendFormatted(Environment.NewLine);
					handler.AppendLiteral("    ERROR Trying to Get DTC ");
					handler.AppendFormatted(ex.Message);
					stringBuilder5.Append(ref handler);
				}
			}
			else if (base.ResponseState != 0)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder6 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(40, 1, stringBuilder2);
				handler.AppendFormatted(Environment.NewLine);
				handler.AppendLiteral("    --- DTC List Not Valid/Complete --- ");
				stringBuilder6.Append(ref handler);
			}
			return stringBuilder.ToString();
		}
	}
}
