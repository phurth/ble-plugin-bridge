using System;
using System.Collections.Generic;
using System.Linq;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices.TankSensor.Mopeka
{
	public class LPTankName
	{
		public static readonly LPTankName Rv1 = new LPTankName(FunctionName.LpTankRv, 0, isSupportedByOctp: true, isOver100LbTank: false);

		public static readonly LPTankName Rv2 = new LPTankName(FunctionName.LpTankRv, 1, isSupportedByOctp: true, isOver100LbTank: false);

		public static readonly LPTankName Rv3 = new LPTankName(FunctionName.LpTankRv, 2, isSupportedByOctp: true, isOver100LbTank: false);

		public static readonly LPTankName Home1 = new LPTankName(FunctionName.LpTankHome, 0, isSupportedByOctp: false, isOver100LbTank: false);

		public static readonly LPTankName Home2 = new LPTankName(FunctionName.LpTankHome, 1, isSupportedByOctp: false, isOver100LbTank: false);

		public static readonly LPTankName Home3 = new LPTankName(FunctionName.LpTankHome, 2, isSupportedByOctp: false, isOver100LbTank: false);

		public static readonly LPTankName Cabin1 = new LPTankName(FunctionName.LpTankCabin, 0, isSupportedByOctp: false, isOver100LbTank: true);

		public static readonly LPTankName Cabin2 = new LPTankName(FunctionName.LpTankCabin, 1, isSupportedByOctp: false, isOver100LbTank: true);

		public static readonly LPTankName Cabin3 = new LPTankName(FunctionName.LpTankCabin, 2, isSupportedByOctp: false, isOver100LbTank: true);

		public static readonly LPTankName Bbq1 = new LPTankName(FunctionName.LpTankBbq, 0, isSupportedByOctp: true, isOver100LbTank: false);

		public static readonly LPTankName Bbq2 = new LPTankName(FunctionName.LpTankBbq, 1, isSupportedByOctp: true, isOver100LbTank: false);

		public static readonly LPTankName Bbq3 = new LPTankName(FunctionName.LpTankBbq, 2, isSupportedByOctp: true, isOver100LbTank: false);

		public static readonly LPTankName Grill1 = new LPTankName(FunctionName.LpTankGrill, 0, isSupportedByOctp: true, isOver100LbTank: false);

		public static readonly LPTankName Grill2 = new LPTankName(FunctionName.LpTankGrill, 1, isSupportedByOctp: true, isOver100LbTank: false);

		public static readonly LPTankName Grill3 = new LPTankName(FunctionName.LpTankGrill, 2, isSupportedByOctp: true, isOver100LbTank: false);

		public static readonly LPTankName Submarine1 = new LPTankName(FunctionName.LpTankSubmarine, 0, isSupportedByOctp: false, isOver100LbTank: true);

		public static readonly LPTankName Submarine2 = new LPTankName(FunctionName.LpTankSubmarine, 1, isSupportedByOctp: false, isOver100LbTank: true);

		public static readonly LPTankName Submarine3 = new LPTankName(FunctionName.LpTankSubmarine, 2, isSupportedByOctp: false, isOver100LbTank: true);

		public static readonly LPTankName Other1 = new LPTankName(FunctionName.LpTankOther, 0, isSupportedByOctp: true, isOver100LbTank: false);

		public static readonly LPTankName Other2 = new LPTankName(FunctionName.LpTankOther, 1, isSupportedByOctp: true, isOver100LbTank: false);

		public static readonly LPTankName Other3 = new LPTankName(FunctionName.LpTankOther, 2, isSupportedByOctp: true, isOver100LbTank: false);

		private const string LogTag = "LPTankName";

		public static IEnumerable<LPTankName> Values { get; } = new List<LPTankName>
		{
			Rv1, Rv2, Rv3, Home1, Home2, Home3, Cabin1, Cabin2, Cabin3, Bbq1,
			Bbq2, Bbq3, Grill1, Grill2, Grill3, Submarine1, Submarine2, Submarine3, Other1, Other2,
			Other3
		};


		public FunctionName FunctionName { get; }

		public byte FunctionInstance { get; }

		public string Name { get; }

		public bool IsSupportedByOCTP { get; }

		public bool IsOver100LbTank { get; }

		public static LPTankName GetByFunctionNameAndInstance(FunctionName functionName, int functionInstance)
		{
			TaggedLog.Debug("LPTankName", $"LPTankName GetByFunctionNameAndInstance: {functionName}, {functionInstance}");
			try
			{
				return Enumerable.First(Values, delegate(LPTankName x)
				{
					FunctionName functionName2 = x.FunctionName;
					byte functionInstance2 = x.FunctionInstance;
					FunctionName functionName3 = functionName;
					int num = functionInstance;
					return functionName2 == functionName3 && functionInstance2 == num;
				});
			}
			catch (Exception arg)
			{
				TaggedLog.Error("LPTankName", $"Error getting LPTankName: {arg}");
				return Rv1;
			}
		}

		private LPTankName(FunctionName functionName, byte functionInstance, bool isSupportedByOctp, bool isOver100LbTank)
		{
			FunctionName = functionName;
			FunctionInstance = functionInstance;
			Name = LogicalDeviceIdFormatExtension.FormatFunctionNameWithFunctionInstance(functionName.ToFunctionName(), functionInstance, showLinkedToWirelessSwitchInstance: false);
			IsSupportedByOCTP = isSupportedByOctp;
			IsOver100LbTank = isOver100LbTank;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
