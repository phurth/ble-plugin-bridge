using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common.Extensions;
using OneControl.Devices.Leveler.Type5;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandLeveler5ResponseFailure : MyRvLinkCommandResponseFailure
	{
		public const string LogTag = "MyRvLinkCommandActionSwitchResponseFailure";

		public const int DeviceIdExtendedIndex = 0;

		public const int OptionalExtendedDataSize = 2;

		protected override int MinExtendedDataLength => 0;

		public bool HasUserMessageDtc
		{
			get
			{
				IReadOnlyList<byte> extendedData = base.ExtendedData;
				if (extendedData == null)
				{
					return false;
				}
				return extendedData.Count == 2;
			}
		}

		public LevelerFaultType5? LevelerFault => DecodeUserMessageDtc();

		public MyRvLinkCommandLeveler5ResponseFailure(ushort clientCommandId, MyRvLinkCommandResponseFailureCode failureCode)
			: base(clientCommandId, commandCompleted: true, failureCode)
		{
		}

		public MyRvLinkCommandLeveler5ResponseFailure(ushort clientCommandId, bool commandComplete, MyRvLinkCommandResponseFailureCode failureCode, DTC_ID userMessageDtc)
			: base(clientCommandId, commandComplete, failureCode, EncodeExtendedData(userMessageDtc))
		{
		}

		public MyRvLinkCommandLeveler5ResponseFailure(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public MyRvLinkCommandLeveler5ResponseFailure(MyRvLinkCommandResponseFailure response)
			: base(response.ClientCommandId, response.IsCommandCompleted, response.FailureCode, response.ExtendedData)
		{
		}

		protected LevelerFaultType5? DecodeUserMessageDtc()
		{
			if (!HasUserMessageDtc)
			{
				return null;
			}
			return new LevelerFaultType5(base.ExtendedData.GetValueUInt16(0));
		}

		private static IReadOnlyList<byte> EncodeExtendedData(DTC_ID userMessageDtc)
		{
			byte[] array = new byte[2];
			array.SetValueUInt16((ushort)userMessageDtc, 0);
			return new ArraySegment<byte>(array, 0, 2);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(39, 4);
			defaultInterpolatedStringHandler.AppendLiteral("Command(0x");
			defaultInterpolatedStringHandler.AppendFormatted(base.ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(") Response ");
			defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkCommandActionSwitchResponseFailure");
			defaultInterpolatedStringHandler.AppendLiteral(" Failure Code ");
			defaultInterpolatedStringHandler.AppendFormatted(base.FailureCode);
			defaultInterpolatedStringHandler.AppendLiteral("(0x");
			defaultInterpolatedStringHandler.AppendFormatted((int)base.FailureCode, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(")");
			StringBuilder stringBuilder = new StringBuilder(defaultInterpolatedStringHandler.ToStringAndClear());
			try
			{
				LevelerFaultType5? levelerFault = LevelerFault;
				if (!levelerFault.HasValue)
				{
					stringBuilder.Append(" User Message: none");
				}
				else
				{
					StringBuilder stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder3 = stringBuilder2;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
					handler.AppendLiteral(" User Message: ");
					handler.AppendFormatted(levelerFault?.ToDebugString());
					handler.AppendLiteral(")");
					stringBuilder3.Append(ref handler);
				}
			}
			catch (Exception ex)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder4 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(22, 1, stringBuilder2);
				handler.AppendLiteral(" User Message: none (");
				handler.AppendFormatted(ex.Message);
				handler.AppendLiteral(")");
				stringBuilder4.Append(ref handler);
			}
			return stringBuilder.ToString();
		}
	}
}
