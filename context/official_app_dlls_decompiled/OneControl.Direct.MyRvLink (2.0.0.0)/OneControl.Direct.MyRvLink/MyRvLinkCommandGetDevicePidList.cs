using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandGetDevicePidList : MyRvLinkCommand
	{
		private const int MaxPayloadLength = 9;

		private const int DeviceTableIdIndex = 3;

		private const int DeviceIdIndex = 4;

		private const int StartPidIndex = 5;

		private const int EndPidIndex = 7;

		private readonly byte[] _rawData;

		private readonly Dictionary<Pid, PidAccess> _pidDict = new Dictionary<Pid, PidAccess>();

		protected virtual string LogTag { get; } = "MyRvLinkCommandGetDevicePidList";


		public override MyRvLinkCommandType CommandType { get; } = MyRvLinkCommandType.GetDevicePidList;


		protected override int MinPayloadLength => 9;

		public override ushort ClientCommandId => MyRvLinkCommand.DecodeClientCommandId(_rawData);

		public byte DeviceTableId => _rawData[3];

		public byte DeviceId => _rawData[4];

		public Pid StartPidId => (Pid)_rawData.GetValueUInt16(5);

		public Pid EndPidId => (Pid)_rawData.GetValueUInt16(7);

		public bool IsDeviceLoadingCompleted { get; private set; }

		public Dictionary<Pid, PidAccess> PidDict
		{
			get
			{
				if (!IsDeviceLoadingCompleted)
				{
					return new Dictionary<Pid, PidAccess>();
				}
				return _pidDict;
			}
		}

		public MyRvLinkCommandGetDevicePidList(ushort clientCommandId, byte deviceTableId, byte deviceId, Pid startPidId, Pid endPidId = Pid.Unknown)
		{
			_rawData = new byte[9];
			_rawData.SetValueUInt16(clientCommandId, 0);
			_rawData[2] = (byte)CommandType;
			_rawData[3] = deviceTableId;
			_rawData[4] = deviceId;
			_rawData.SetValueUInt16((ushort)startPidId, 5);
			_rawData.SetValueUInt16((ushort)endPidId, 7);
			ValidateCommand(_rawData, clientCommandId);
		}

		protected MyRvLinkCommandGetDevicePidList(IReadOnlyList<byte> rawData)
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
			if (!(commandResponse is MyRvLinkCommandGetDevicePidListResponse myRvLinkCommandGetDevicePidListResponse))
			{
				if (!(commandResponse is MyRvLinkCommandGetDevicePidListResponseCompleted myRvLinkCommandGetDevicePidListResponseCompleted))
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
						if (myRvLinkCommandGetDevicePidListResponseCompleted.PidCount != _pidDict.Count)
						{
							string logTag4 = LogTag;
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(92, 2);
							defaultInterpolatedStringHandler.AppendLiteral("Command failed didn't receive expected number of DTCs. Response received ");
							defaultInterpolatedStringHandler.AppendFormatted(_pidDict.Count);
							defaultInterpolatedStringHandler.AppendLiteral(" PIDs and expected ");
							defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandGetDevicePidListResponseCompleted.PidCount);
							TaggedLog.Debug(logTag4, defaultInterpolatedStringHandler.ToStringAndClear());
							_pidDict.Clear();
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
						TaggedLog.Debug(LogTag, "Command completed, ALL PIDs were not received properly: " + ex.Message);
						base.ResponseState = MyRvLinkResponseState.Failed;
					}
				}
			}
			else
			{
				try
				{
					foreach (KeyValuePair<Pid, PidAccess> item in myRvLinkCommandGetDevicePidListResponse.PidDict)
					{
						if (_pidDict.ContainsKey(item.Key))
						{
							string logTag5 = LogTag;
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(68, 1);
							defaultInterpolatedStringHandler.AppendLiteral("IGNORING: Duplicate PID ");
							defaultInterpolatedStringHandler.AppendFormatted(item.Key);
							defaultInterpolatedStringHandler.AppendLiteral(", value already returned in another response");
							TaggedLog.Debug(logTag5, defaultInterpolatedStringHandler.ToStringAndClear());
						}
						else
						{
							_pidDict.Add(item.Key, item.Value);
						}
					}
				}
				catch (Exception ex2)
				{
					TaggedLog.Debug(LogTag, "Command completed, ALL PIDs were not received properly: " + ex2.Message);
					base.ResponseState = MyRvLinkResponseState.Failed;
				}
			}
			return base.ResponseState != MyRvLinkResponseState.Pending;
		}

		public static MyRvLinkCommandGetDevicePidList Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkCommandGetDevicePidList(rawData);
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
					return new MyRvLinkCommandGetDevicePidListResponseCompleted(myRvLinkCommandResponseSuccess);
				}
				return new MyRvLinkCommandGetDevicePidListResponse(myRvLinkCommandResponseSuccess);
			}
			return commandEvent;
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(77, 7);
			defaultInterpolatedStringHandler.AppendFormatted(LogTag);
			defaultInterpolatedStringHandler.AppendLiteral("[Client Command Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(", Table Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceTableId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", Start Device Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", Start: ");
			defaultInterpolatedStringHandler.AppendFormatted(StartPidId);
			defaultInterpolatedStringHandler.AppendLiteral(", End: ");
			defaultInterpolatedStringHandler.AppendFormatted(EndPidId);
			defaultInterpolatedStringHandler.AppendLiteral(" ]: ");
			defaultInterpolatedStringHandler.AppendFormatted(_rawData.DebugDump());
			StringBuilder stringBuilder = new StringBuilder(defaultInterpolatedStringHandler.ToStringAndClear());
			if (IsDeviceLoadingCompleted)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder3 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(23, 2, stringBuilder2);
				handler.AppendFormatted(Environment.NewLine);
				handler.AppendLiteral("    Received PID Count ");
				handler.AppendFormatted(PidDict.Count);
				stringBuilder3.Append(ref handler);
				try
				{
					foreach (KeyValuePair<Pid, PidAccess> item in PidDict)
					{
						stringBuilder2 = stringBuilder;
						StringBuilder stringBuilder4 = stringBuilder2;
						handler = new StringBuilder.AppendInterpolatedStringHandler(6, 3, stringBuilder2);
						handler.AppendFormatted(Environment.NewLine);
						handler.AppendLiteral("    ");
						handler.AppendFormatted(item.Key);
						handler.AppendLiteral(": ");
						handler.AppendFormatted(item.Value.DebugDumpAsFlags());
						stringBuilder4.Append(ref handler);
					}
				}
				catch (Exception ex)
				{
					stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder5 = stringBuilder2;
					handler = new StringBuilder.AppendInterpolatedStringHandler(28, 2, stringBuilder2);
					handler.AppendFormatted(Environment.NewLine);
					handler.AppendLiteral("    ERROR Trying to Get PID ");
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
				handler.AppendLiteral("    --- PID List Not Valid/Complete --- ");
				stringBuilder6.Append(ref handler);
			}
			return stringBuilder.ToString();
		}
	}
}
