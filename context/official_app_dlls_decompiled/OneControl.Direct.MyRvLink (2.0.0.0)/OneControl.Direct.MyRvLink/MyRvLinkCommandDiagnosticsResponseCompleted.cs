using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandDiagnosticsResponseCompleted : MyRvLinkCommandResponseSuccess
	{
		public const string LogTag = "MyRvLinkCommandGetDevicePidResponseCompleted";

		public const byte Separator = byte.MaxValue;

		protected override int MinExtendedDataLength => 1;

		public IReadOnlyList<MyRvLinkCommandType> EnabledDiagnosticCommands => DecodeEnabledDiagnosticCommands();

		public IReadOnlyList<MyRvLinkEventType> EnabledDiagnosticEvents => DecodeEnabledDiagnosticEvents();

		public MyRvLinkCommandDiagnosticsResponseCompleted(ushort clientCommandId, IReadOnlyCollection<MyRvLinkCommandType> commands, IReadOnlyCollection<MyRvLinkEventType> events)
			: base(clientCommandId, commandCompleted: true, EncodeExtendedData(commands, events))
		{
		}

		public MyRvLinkCommandDiagnosticsResponseCompleted(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public MyRvLinkCommandDiagnosticsResponseCompleted(MyRvLinkCommandResponseSuccess response)
			: base(response.ClientCommandId, response.IsCommandCompleted, response.ExtendedData)
		{
		}

		protected List<MyRvLinkCommandType> DecodeEnabledDiagnosticCommands()
		{
			List<MyRvLinkCommandType> list = new List<MyRvLinkCommandType>();
			if (base.ExtendedData == null)
			{
				return list;
			}
			int num = 0;
			while (num < base.ExtendedData.Count)
			{
				byte b = base.ExtendedData[num++];
				if (b == byte.MaxValue)
				{
					break;
				}
				list.Add((MyRvLinkCommandType)b);
			}
			return list;
		}

		protected List<MyRvLinkEventType> DecodeEnabledDiagnosticEvents()
		{
			List<MyRvLinkEventType> list = new List<MyRvLinkEventType>();
			if (base.ExtendedData == null)
			{
				return list;
			}
			int num = 0;
			bool flag = false;
			while (num < base.ExtendedData.Count)
			{
				byte b = base.ExtendedData[num++];
				if (b == byte.MaxValue)
				{
					if (flag)
					{
						TaggedLog.Warning("MyRvLinkCommandGetDevicePidResponseCompleted", "Invalid response, found multiple separators");
						break;
					}
					flag = true;
				}
				else if (flag)
				{
					list.Add((MyRvLinkEventType)b);
				}
			}
			return list;
		}

		private static IReadOnlyList<byte> EncodeExtendedData(IReadOnlyCollection<MyRvLinkCommandType> commands, IReadOnlyCollection<MyRvLinkEventType> events)
		{
			byte[] array = new byte[commands.Count + 1 + events.Count];
			int num = 0;
			foreach (MyRvLinkCommandType command in commands)
			{
				if ((byte)command == byte.MaxValue)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Invalid command value of 0x");
					defaultInterpolatedStringHandler.AppendFormatted(byte.MaxValue, "X");
					throw new ArgumentException("commands", defaultInterpolatedStringHandler.ToStringAndClear());
				}
				array[num++] = (byte)command;
			}
			array[num++] = byte.MaxValue;
			foreach (MyRvLinkEventType @event in events)
			{
				if ((byte)@event == byte.MaxValue)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(25, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Invalid event value of 0x");
					defaultInterpolatedStringHandler.AppendFormatted(byte.MaxValue, "X");
					throw new ArgumentException("commands", defaultInterpolatedStringHandler.ToStringAndClear());
				}
				array[num++] = (byte)@event;
			}
			return new ArraySegment<byte>(array, 0, num);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(21, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Command(0x");
			defaultInterpolatedStringHandler.AppendFormatted(base.ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(") Response ");
			defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkCommandDiagnosticsResponseCompleted");
			StringBuilder stringBuilder = new StringBuilder(defaultInterpolatedStringHandler.ToStringAndClear());
			IReadOnlyList<MyRvLinkCommandType> enabledDiagnosticCommands = EnabledDiagnosticCommands;
			StringBuilder stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler;
			if (enabledDiagnosticCommands.Count == 0)
			{
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder3 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(34, 1, stringBuilder2);
				handler.AppendFormatted(Environment.NewLine);
				handler.AppendLiteral("    NO Commands In Diagnostic Mode");
				stringBuilder3.Append(ref handler);
			}
			else
			{
				foreach (MyRvLinkCommandType item in enabledDiagnosticCommands)
				{
					stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder4 = stringBuilder2;
					handler = new StringBuilder.AppendInterpolatedStringHandler(38, 3, stringBuilder2);
					handler.AppendFormatted(Environment.NewLine);
					handler.AppendLiteral("    ");
					handler.AppendFormatted(item);
					handler.AppendLiteral("(0x");
					handler.AppendFormatted((int)item, "X");
					handler.AppendLiteral(") - Command Diagnostics Enabled");
					stringBuilder4.Append(ref handler);
				}
			}
			IReadOnlyList<MyRvLinkEventType> enabledDiagnosticEvents = EnabledDiagnosticEvents;
			if (enabledDiagnosticEvents.Count == 0)
			{
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder5 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(32, 1, stringBuilder2);
				handler.AppendFormatted(Environment.NewLine);
				handler.AppendLiteral("    NO Events In Diagnostic Mode");
				stringBuilder5.Append(ref handler);
			}
			else
			{
				foreach (MyRvLinkEventType item2 in enabledDiagnosticEvents)
				{
					stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder6 = stringBuilder2;
					handler = new StringBuilder.AppendInterpolatedStringHandler(36, 3, stringBuilder2);
					handler.AppendFormatted(Environment.NewLine);
					handler.AppendLiteral("    ");
					handler.AppendFormatted(item2);
					handler.AppendLiteral("(0x");
					handler.AppendFormatted((int)item2, "X");
					handler.AppendLiteral(") - Event Diagnostics Enabled");
					stringBuilder6.Append(ref handler);
				}
			}
			IReadOnlyList<byte> readOnlyList = Encode();
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder7 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(14, 2, stringBuilder2);
			handler.AppendFormatted(Environment.NewLine);
			handler.AppendLiteral("    Raw Data: ");
			handler.AppendFormatted(readOnlyList.DebugDump(0, readOnlyList.Count));
			stringBuilder7.Append(ref handler);
			return stringBuilder.ToString();
		}
	}
}
