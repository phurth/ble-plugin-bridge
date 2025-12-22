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
	public class MyRvLinkCommandGetProductDtcValuesResponse : MyRvLinkCommandResponseSuccess
	{
		public const string LogTag = "MyRvLinkCommandGetProductDtcValuesResponse";

		private const int DtcResultSize = 3;

		private Dictionary<DTC_ID, DtcValue>? _dtcDict;

		protected override int MinExtendedDataLength => 3;

		public IReadOnlyDictionary<DTC_ID, DtcValue> DtcDict => _dtcDict ?? (_dtcDict = DecodeDtcDict());

		public MyRvLinkCommandGetProductDtcValuesResponse(ushort clientCommandId, IReadOnlyList<(DTC_ID Id, DtcValue Value)> dtcList)
			: base(clientCommandId, commandCompleted: false, EncodeExtendedData(dtcList))
		{
		}

		public MyRvLinkCommandGetProductDtcValuesResponse(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public MyRvLinkCommandGetProductDtcValuesResponse(MyRvLinkCommandResponseSuccess response)
			: base(response.ClientCommandId, response.IsCommandCompleted, response.ExtendedData)
		{
		}

		protected Dictionary<DTC_ID, DtcValue> DecodeDtcDict()
		{
			Dictionary<DTC_ID, DtcValue> dictionary = new Dictionary<DTC_ID, DtcValue>();
			if (base.ExtendedData == null || base.ExtendedData.Count == 0)
			{
				return new Dictionary<DTC_ID, DtcValue>();
			}
			try
			{
				int num = 0;
				while (num < base.ExtendedData.Count)
				{
					short valueInt = base.ExtendedData.GetValueInt16(num);
					num += 2;
					byte rawDtcValue = base.ExtendedData[num];
					num++;
					if (!Enum<DTC_ID>.TryConvert(valueInt, out var toValue))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(26, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Ignoring unknown PID of 0x");
						defaultInterpolatedStringHandler.AppendFormatted(valueInt, "X");
						TaggedLog.Warning("MyRvLinkCommandGetProductDtcValuesResponse", defaultInterpolatedStringHandler.ToStringAndClear());
					}
					else if (dictionary.ContainsKey(toValue))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(45, 1);
						defaultInterpolatedStringHandler.AppendLiteral("IGNORING: Duplicate DTC ");
						defaultInterpolatedStringHandler.AppendFormatted(toValue);
						defaultInterpolatedStringHandler.AppendLiteral(" returned in response");
						TaggedLog.Debug("MyRvLinkCommandGetProductDtcValuesResponse", defaultInterpolatedStringHandler.ToStringAndClear());
					}
					else
					{
						dictionary.Add(toValue, new DtcValue(rawDtcValue));
					}
				}
				return dictionary;
			}
			catch (Exception ex)
			{
				TaggedLog.Error("MyRvLinkCommandGetProductDtcValuesResponse", "Error getting DTC response: " + ex.Message + " extended: " + base.ExtendedData.DebugDump(0, base.ExtendedData.Count));
				return new Dictionary<DTC_ID, DtcValue>();
			}
		}

		private static IReadOnlyList<byte> EncodeExtendedData(IReadOnlyList<(DTC_ID Id, DtcValue Value)> dtcList)
		{
			if (dtcList.Count == 0)
			{
				throw new ArgumentOutOfRangeException("dtcList", "Should have at least 1 DTC in the list.");
			}
			if (dtcList.Count > 65535)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(53, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Too many ");
				defaultInterpolatedStringHandler.AppendFormatted(dtcList.Count);
				defaultInterpolatedStringHandler.AppendLiteral(" devices specified, only ");
				defaultInterpolatedStringHandler.AppendFormatted(ushort.MaxValue);
				defaultInterpolatedStringHandler.AppendLiteral(" devices supported.");
				throw new ArgumentOutOfRangeException("dtcList", defaultInterpolatedStringHandler.ToStringAndClear());
			}
			byte[] array = new byte[3 * dtcList.Count];
			int num = 0;
			foreach (var dtc in dtcList)
			{
				array.SetValueUInt16((ushort)dtc.Id, num);
				num += 2;
				array[num] = dtc.Value.ToRawValue();
				num++;
			}
			return new ArraySegment<byte>(array, 0, num);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(33, 3);
			defaultInterpolatedStringHandler.AppendLiteral("Command(0x");
			defaultInterpolatedStringHandler.AppendFormatted(base.ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(") Response ");
			defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkCommandGetProductDtcValuesResponse");
			defaultInterpolatedStringHandler.AppendLiteral(" DTC Count: ");
			defaultInterpolatedStringHandler.AppendFormatted(DtcDict.Count);
			StringBuilder stringBuilder = new StringBuilder(defaultInterpolatedStringHandler.ToStringAndClear());
			try
			{
				foreach (KeyValuePair<DTC_ID, DtcValue> item in DtcDict)
				{
					StringBuilder stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder3 = stringBuilder2;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(7, 2, stringBuilder2);
					handler.AppendLiteral("\n    ");
					handler.AppendFormatted(item.Key);
					handler.AppendLiteral(": ");
					handler.AppendFormatted(item.Value);
					stringBuilder3.Append(ref handler);
				}
			}
			catch (Exception ex)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder4 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(29, 1, stringBuilder2);
				handler.AppendLiteral("\n    ERROR Trying to Get DTC ");
				handler.AppendFormatted(ex.Message);
				stringBuilder4.Append(ref handler);
			}
			return stringBuilder.ToString();
		}
	}
}
