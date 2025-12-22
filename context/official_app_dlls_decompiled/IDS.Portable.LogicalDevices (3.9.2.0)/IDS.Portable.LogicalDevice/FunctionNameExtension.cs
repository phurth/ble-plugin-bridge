using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;

namespace IDS.Portable.LogicalDevice
{
	public static class FunctionNameExtension
	{
		public const string ClimateZone = "Climate Zone";

		public const string Tilt = "Tilt";

		public const string Slide = "Slide";

		public const string Awning = "Awning";

		public const string Lights = "Lights";

		public const string LightWithSpace = "Light ";

		public const string Temperature = "Temperature";

		public const string TemperatureWithSpace = "Temperature ";

		public const string Tank = "Tank";

		public const string Battery = "Battery";

		public const string SlideWithSpace = "Slide ";

		public const string Bathroom = "Bathroom";

		public const string Bedroom = "Bedroom";

		public const string Kitchen = "Kitchen";

		public const string Fuel = "Fuel";

		public const string Grey = "Grey";

		public const string Fresh = "Fresh";

		public const string Black = "Black";

		public const string Auxiliary = "Auxiliary";

		public const string Lift = "Lift";

		public const string Leveler = "Leveler";

		public const string LP = "LP";

		private static string LogTag = "FunctionNameExtension";

		private static readonly ConcurrentDictionary<FunctionName, IReadOnlyList<FUNCTION_CLASS>> FunctionClassForFunctionNameDict = new ConcurrentDictionary<FunctionName, IReadOnlyList<FUNCTION_CLASS>>();

		private static readonly ConcurrentDictionary<FunctionName, FunctionNameDetail> FunctionNameDetailForFunctionNameDict = new ConcurrentDictionary<FunctionName, FunctionNameDetail>();

		private static readonly ConcurrentDictionary<FunctionName, string> _functionNameAsStringCache = new ConcurrentDictionary<FunctionName, string>();

		private static readonly ConcurrentDictionary<FunctionName, string> _functionNameShortAsStringCache = new ConcurrentDictionary<FunctionName, string>();

		private static readonly ConcurrentDictionary<FunctionName, string> _functionNameAbbreviatedAsStringCache = new ConcurrentDictionary<FunctionName, string>();

		public const string DefaultUnknownName = "Unknown";

		public static bool ValidateFunctionNamesHaveClasses(bool verbose)
		{
			List<string> list = new List<string>();
			foreach (FUNCTION_NAME item in FUNCTION_NAME.GetEnumerator())
			{
				if (Enum<FunctionName>.TryConvert((ushort)item, out var toValue))
				{
					if (toValue != 0 && Enumerable.First(toValue.GetFunctionClasses()) == FUNCTION_CLASS.UNKNOWN)
					{
						list.Add(string.Format("    {0} doesn't have a known {1}", toValue, "FUNCTION_CLASS"));
					}
				}
				else
				{
					list.Add(string.Format("    {0}/0x{1:X4} not found in {2} enum", item, (ushort)item, "FunctionName"));
				}
			}
			TaggedLog.Debug(LogTag, "**** FUNCTION NAME TEST ****");
			Dictionary<FUNCTION_NAME, FUNCTION_NAME> dictionary = Enumerable.ToDictionary(FUNCTION_NAME.GetEnumerator(), (FUNCTION_NAME name) => name);
			foreach (FunctionName value in Enum.GetValues(typeof(FunctionName)))
			{
				if (value != 0 && !dictionary.ContainsKey(value.ToFunctionName()))
				{
					list.Add(string.Format("    {0}/0x{1:X4} not found in {2} (IDS.Core) enum", value, (ushort)value, "FUNCTION_NAME"));
				}
			}
			if (verbose && list.Count > 0)
			{
				TaggedLog.Error("FunctionNameExtension", "\n**** MISSING FUNCTION CLASS DEFINITION START ****\n");
				foreach (string item2 in list)
				{
					TaggedLog.Error("FunctionNameExtension", item2);
				}
				TaggedLog.Error("FunctionNameExtension", "\n**** MISSING FUNCTION CLASS DEFINITION END ****\n");
			}
			return list.Count == 0;
		}

		public static bool IsSecurityLight(this FUNCTION_NAME functionName)
		{
			ushort num = functionName;
			if (num == 47 || (uint)(num - 146) <= 1u)
			{
				return true;
			}
			return false;
		}

		public static FUNCTION_NAME ToFunctionName(this FunctionName functionName)
		{
			return (ushort)functionName;
		}

		public static FunctionName ToFunctionName(this FUNCTION_NAME functionName)
		{
			return (FunctionName)(ushort)functionName;
		}

		public static IEnumerable<FUNCTION_CLASS> GetFunctionClasses(this FUNCTION_NAME functionName)
		{
			return functionName.ToFunctionName().GetFunctionClasses();
		}

		public static IEnumerable<FUNCTION_CLASS> GetFunctionClasses(this FunctionName functionName)
		{
			if (FunctionClassForFunctionNameDict.TryGetValue(functionName, out var result))
			{
				return result;
			}
			List<FUNCTION_CLASS> list;
			try
			{
				list = Enumerable.ToList((CustomAttributeExtensions.GetCustomAttribute<FunctionClassAttribute>(typeof(FunctionName).GetField(functionName.ToString()), false) ?? throw new Exception(string.Format("No {0} defined for {1}", "FunctionClassAttribute", functionName))).FunctionClasses);
			}
			catch (Exception ex)
			{
				TaggedLog.Error(LogTag, string.Format("Error looking up {0} for {1}: {2}", "FUNCTION_CLASS", functionName, ex.Message));
				list = new List<FUNCTION_CLASS> { FUNCTION_CLASS.UNKNOWN };
			}
			FunctionClassForFunctionNameDict.TryAdd(functionName, list);
			return list;
		}

		public static FunctionNameDetail GetFunctionNameDetail(this FunctionName functionName)
		{
			if (FunctionNameDetailForFunctionNameDict.TryGetValue(functionName, out var result))
			{
				return result;
			}
			FunctionNameDetail functionNameDetail;
			try
			{
				functionNameDetail = (CustomAttributeExtensions.GetCustomAttribute<FunctionNameDetailAttribute>(typeof(FunctionName).GetField(functionName.ToString()), false) ?? throw new Exception(string.Format("No {0} defined for {1}", "FunctionNameDetailAttribute", functionName))).Detail;
			}
			catch (Exception ex)
			{
				TaggedLog.Error(LogTag, $"Error looking up Function Name Detail for {functionName}: {ex.Message}");
				functionNameDetail = new FunctionNameDetail(FunctionNameDetailLocation.Unknown, FunctionNameDetailPosition.Unknown, FunctionNameDetailRoom.Unknown, FunctionNameDetailUse.Unknown);
			}
			FunctionNameDetailForFunctionNameDict.TryAdd(functionName, functionNameDetail);
			return functionNameDetail;
		}

		public static IEnumerable<FunctionName> GetFunctionNamesForUse(FunctionNameDetailUse use)
		{
			return Enumerable.Where(EnumExtensions.GetValues<FunctionName>(), (FunctionName functionName) => functionName.GetFunctionNameDetail().Use.HasFlag(use));
		}

		public static IEnumerable<FunctionName> GetFunctionNamesForFunctionClass(FUNCTION_CLASS functionClass)
		{
			return Enumerable.Where(EnumExtensions.GetValues<FunctionName>(), (FunctionName functionName) => Enumerable.Contains(functionName.GetFunctionClasses(), functionClass));
		}

		public static string GetName(this FunctionName functionName)
		{
			if (_functionNameAsStringCache.TryGetValue(functionName, out var result))
			{
				return result;
			}
			try
			{
				result = functionName.ToFunctionName().Name;
			}
			catch (Exception ex)
			{
				TaggedLog.Error(LogTag, string.Format("Unable to Convert {0} to {1} in order to lookup name: {2}", functionName, "FUNCTION_NAME", ex.Message));
				return "Unknown";
			}
			try
			{
				foreach (FunctionNameOverrideDisplayNameAttribute customAttribute in CustomAttributeExtensions.GetCustomAttributes<FunctionNameOverrideDisplayNameAttribute>(typeof(FunctionName).GetField(functionName.ToString()), false))
				{
					result = customAttribute.Transform(result);
				}
			}
			catch (Exception ex2)
			{
				TaggedLog.Warning(LogTag, $"Unable to convert apply name overrides for {functionName}: {ex2.Message}");
			}
			result = result.Trim();
			_functionNameAsStringCache.TryAdd(functionName, result);
			return result;
		}

		public static string GetNameShort(this FunctionName functionName)
		{
			if (_functionNameShortAsStringCache.TryGetValue(functionName, out var result))
			{
				return result;
			}
			result = functionName.GetName();
			try
			{
				bool flag = false;
				FUNCTION_CLASS functionClass = Enumerable.FirstOrDefault(functionName.GetFunctionClasses());
				foreach (FunctionNameOverrideDisplayNameShortAttribute customAttribute in CustomAttributeExtensions.GetCustomAttributes<FunctionNameOverrideDisplayNameShortAttribute>(typeof(FunctionName).GetField(functionName.ToString()), false))
				{
					flag = true;
					result = customAttribute.Transform(functionClass, result);
				}
				if (!flag)
				{
					result = FunctionNameOverrideDisplayNameShortAttribute.DefaultRulesTransformer(functionClass, result);
				}
			}
			catch (Exception ex)
			{
				TaggedLog.Warning(LogTag, $"Unable to convert apply name short overrides for {functionName}: {ex.Message}");
			}
			result = result.Trim();
			_functionNameShortAsStringCache.TryAdd(functionName, result);
			return result;
		}

		public static string GetNameShortAbbreviated(this FunctionName functionName)
		{
			if (_functionNameAbbreviatedAsStringCache.TryGetValue(functionName, out var result))
			{
				return result;
			}
			result = functionName.GetNameShort();
			try
			{
				bool flag = false;
				foreach (FunctionNameOverrideDisplayNameShortAbbreviatedAttribute customAttribute in CustomAttributeExtensions.GetCustomAttributes<FunctionNameOverrideDisplayNameShortAbbreviatedAttribute>(typeof(FunctionName).GetField(functionName.ToString()), false))
				{
					flag = true;
					result = customAttribute.Transform(result);
				}
				if (!flag)
				{
					result = FunctionNameOverrideDisplayNameShortAbbreviatedAttribute.DefaultRulesTransformer(result);
				}
			}
			catch (Exception ex)
			{
				TaggedLog.Warning(LogTag, $"Unable to convert apply name short overrides for {functionName}: {ex.Message}");
			}
			result = result.Trim();
			_functionNameAbbreviatedAsStringCache.TryAdd(functionName, result);
			return result;
		}
	}
}
