using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandGetDevicePidListResponse : MyRvLinkCommandResponseSuccess
	{
		public const string LogTag = "MyRvLinkCommandGetDevicePidListResponse";

		private const int PidResultSize = 3;

		private Dictionary<Pid, PidAccess>? _pidDict;

		protected override int MinExtendedDataLength => 3;

		public IReadOnlyDictionary<Pid, PidAccess> PidDict => _pidDict ?? (_pidDict = DecodePidDict());

		public MyRvLinkCommandGetDevicePidListResponse(ushort clientCommandId, IReadOnlyList<(Pid Id, PidAccess Access)> pidList)
			: base(clientCommandId, commandCompleted: false, EncodeExtendedData(pidList))
		{
		}

		public MyRvLinkCommandGetDevicePidListResponse(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public MyRvLinkCommandGetDevicePidListResponse(MyRvLinkCommandResponseSuccess response)
			: base(response.ClientCommandId, response.IsCommandCompleted, response.ExtendedData)
		{
		}

		protected Dictionary<Pid, PidAccess> DecodePidDict()
		{
			Dictionary<Pid, PidAccess> dictionary = new Dictionary<Pid, PidAccess>();
			if (base.ExtendedData == null || base.ExtendedData.Count == 0)
			{
				return new Dictionary<Pid, PidAccess>();
			}
			try
			{
				int num = 0;
				while (num < base.ExtendedData.Count)
				{
					short valueInt = base.ExtendedData.GetValueInt16(num);
					num += 2;
					PidAccess value = (PidAccess)((int)base.ExtendedData[num] & -249);
					num++;
					if (!Enum<Pid>.TryConvert(valueInt, out var toValue))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(26, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Ignoring unknown PID of 0x");
						defaultInterpolatedStringHandler.AppendFormatted(valueInt, "X");
						TaggedLog.Warning("MyRvLinkCommandGetDevicePidListResponse", defaultInterpolatedStringHandler.ToStringAndClear());
					}
					else if (dictionary.ContainsKey(toValue))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(45, 1);
						defaultInterpolatedStringHandler.AppendLiteral("IGNORING: Duplicate PID ");
						defaultInterpolatedStringHandler.AppendFormatted(toValue);
						defaultInterpolatedStringHandler.AppendLiteral(" returned in response");
						TaggedLog.Debug("MyRvLinkCommandGetDevicePidListResponse", defaultInterpolatedStringHandler.ToStringAndClear());
					}
					else
					{
						dictionary.Add(toValue, value);
					}
				}
				return dictionary;
			}
			catch (Exception ex)
			{
				TaggedLog.Error("MyRvLinkCommandGetDevicePidListResponse", "Error getting PID response: " + ex.Message + " extended: " + base.ExtendedData.DebugDump(0, base.ExtendedData.Count));
				return new Dictionary<Pid, PidAccess>();
			}
		}

		private static IReadOnlyList<byte> EncodeExtendedData(IReadOnlyList<(Pid Id, PidAccess Access)> pidList)
		{
			if (pidList.Count == 0)
			{
				throw new ArgumentOutOfRangeException("pidList", "Should have at least 1 PID in the list.");
			}
			if (pidList.Count > 65535)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(53, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Too many ");
				defaultInterpolatedStringHandler.AppendFormatted(pidList.Count);
				defaultInterpolatedStringHandler.AppendLiteral(" devices specified, only ");
				defaultInterpolatedStringHandler.AppendFormatted(ushort.MaxValue);
				defaultInterpolatedStringHandler.AppendLiteral(" devices supported.");
				throw new ArgumentOutOfRangeException("pidList", defaultInterpolatedStringHandler.ToStringAndClear());
			}
			byte[] array = new byte[3 * pidList.Count];
			int num = 0;
			foreach (var pid in pidList)
			{
				array.SetValueUInt16((ushort)pid.Id, num);
				num += 2;
				array[num] = (byte)pid.Access;
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
			defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkCommandGetDevicePidListResponse");
			defaultInterpolatedStringHandler.AppendLiteral(" PID Count: ");
			defaultInterpolatedStringHandler.AppendFormatted(PidDict.Count);
			StringBuilder stringBuilder = new StringBuilder(defaultInterpolatedStringHandler.ToStringAndClear());
			try
			{
				foreach (KeyValuePair<Pid, PidAccess> item in PidDict)
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
				handler.AppendLiteral("\n    ERROR Trying to Get PID ");
				handler.AppendFormatted(ex.Message);
				stringBuilder4.Append(ref handler);
			}
			return stringBuilder.ToString();
		}
	}
}
