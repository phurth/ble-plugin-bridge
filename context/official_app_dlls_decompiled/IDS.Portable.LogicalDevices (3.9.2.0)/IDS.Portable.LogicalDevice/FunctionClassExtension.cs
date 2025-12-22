using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;

namespace IDS.Portable.LogicalDevice
{
	public static class FunctionClassExtension
	{
		private static readonly IReadOnlyDictionary<FUNCTION_CLASS, string> FunctionClassDescriptionDict;

		private static readonly IReadOnlyDictionary<FUNCTION_CLASS, IReadOnlyCollection<DEVICE_TYPE>> FunctionClassDevicesDict;

		private static readonly IReadOnlyDictionary<DEVICE_TYPE, FUNCTION_CLASS> ExclusiveFunctionClass;

		private static readonly IReadOnlyDictionary<DEVICE_TYPE, FUNCTION_CLASS> DefaultFunctionClass;

		private static readonly Dictionary<FUNCTION_CLASS, ReadOnlyCollection<FUNCTION_CLASS>> SingleFunctionClassCache;

		static FunctionClassExtension()
		{
			SingleFunctionClassCache = new Dictionary<FUNCTION_CLASS, ReadOnlyCollection<FUNCTION_CLASS>>();
			Dictionary<FUNCTION_CLASS, string> dictionary = new Dictionary<FUNCTION_CLASS, string>();
			Dictionary<FUNCTION_CLASS, IReadOnlyCollection<DEVICE_TYPE>> dictionary2 = new Dictionary<FUNCTION_CLASS, IReadOnlyCollection<DEVICE_TYPE>>();
			Dictionary<DEVICE_TYPE, FUNCTION_CLASS> dictionary3 = new Dictionary<DEVICE_TYPE, FUNCTION_CLASS>();
			Dictionary<DEVICE_TYPE, FUNCTION_CLASS> dictionary4 = new Dictionary<DEVICE_TYPE, FUNCTION_CLASS>();
			foreach (FUNCTION_CLASS value5 in EnumExtensions.GetValues<FUNCTION_CLASS>())
			{
				FunctionClassDescriptionAttribute attribute = value5.GetAttribute<FunctionClassDescriptionAttribute>(inherit: false);
				if (attribute != null)
				{
					dictionary.Add(value5, attribute.Description);
				}
				FunctionClassDevicesAttribute attribute2 = value5.GetAttribute<FunctionClassDevicesAttribute>(inherit: false);
				if (attribute2 != null)
				{
					dictionary2.Add(value5, attribute2.DeviceTypes);
				}
				FunctionClassExclusiveDevicesAttribute attribute3 = value5.GetAttribute<FunctionClassExclusiveDevicesAttribute>(inherit: false);
				if (attribute3 != null)
				{
					if (!dictionary2.TryGetValue(value5, out var value))
					{
						TaggedLog.Warning("FunctionClassExtension", string.Format("{0}.{1} missing {2}", "FUNCTION_CLASS", value5, "FunctionClassDevicesAttribute"));
					}
					else
					{
						foreach (DEVICE_TYPE deviceType in attribute3.DeviceTypes)
						{
							FUNCTION_CLASS value2;
							if (!Enumerable.Contains(value, deviceType))
							{
								TaggedLog.Error("FunctionClassExtension", string.Format("{0}.{1} has EXCLUSIVE device type of `{2}` but that device type isn't shown as supported by this {3} ", "FUNCTION_CLASS", value5, deviceType, "FUNCTION_CLASS"));
							}
							else if (dictionary3.TryGetValue(deviceType, out value2))
							{
								TaggedLog.Error("FunctionClassExtension", string.Format("`{0}` defined as having a EXCLUSIVE device class of {1}.{2} but was also defined for {3}.{4}", deviceType, "FUNCTION_CLASS", value2, "FUNCTION_CLASS", value5));
							}
							else
							{
								dictionary3[deviceType] = value5;
							}
						}
					}
				}
				FunctionClassDefaultDevicesAttribute attribute4 = value5.GetAttribute<FunctionClassDefaultDevicesAttribute>(inherit: false);
				if (attribute4 == null)
				{
					continue;
				}
				if (!dictionary2.TryGetValue(value5, out var value3))
				{
					TaggedLog.Warning("FunctionClassExtension", string.Format("{0}.{1} missing {2}", "FUNCTION_CLASS", value5, "FunctionClassDevicesAttribute"));
					continue;
				}
				foreach (DEVICE_TYPE deviceType2 in attribute4.DeviceTypes)
				{
					FUNCTION_CLASS value4;
					if (!Enumerable.Contains(value3, deviceType2))
					{
						TaggedLog.Error("FunctionClassExtension", string.Format("{0}.{1} has DEFAULT device type of `{2}` but that device type isn't shown as supported by this {3} ", "FUNCTION_CLASS", value5, deviceType2, "FUNCTION_CLASS"));
					}
					else if (dictionary4.TryGetValue(deviceType2, out value4))
					{
						TaggedLog.Error("FunctionClassExtension", string.Format("`{0}` defined as having a DEFAULT device class of {1}.{2} but was also defined for {3}.{4}", deviceType2, "FUNCTION_CLASS", value4, "FUNCTION_CLASS", value5));
					}
					else
					{
						dictionary4[deviceType2] = value5;
					}
				}
			}
			FunctionClassDescriptionDict = dictionary;
			FunctionClassDevicesDict = dictionary2;
			ExclusiveFunctionClass = dictionary3;
			DefaultFunctionClass = dictionary4;
		}

		public static FUNCTION_CLASS GetPreferredFunctionClass(this DEVICE_TYPE deviceType, FUNCTION_NAME functionName)
		{
			return Get(deviceType, functionName);
		}

		public static FUNCTION_CLASS GetPreferredFunctionClass(this FUNCTION_NAME functionName, DEVICE_TYPE deviceType)
		{
			return Get(deviceType, functionName);
		}

		public static IReadOnlyCollection<DEVICE_TYPE> GetDeviceTypeList(this FUNCTION_CLASS functionClass)
		{
			if (!FunctionClassDevicesDict.TryGetValue(functionClass, out var result))
			{
				return (IReadOnlyCollection<DEVICE_TYPE>)(object)Array.Empty<DEVICE_TYPE>();
			}
			return result;
		}

		public static string GetName(this FUNCTION_CLASS functionClass)
		{
			if (!FunctionClassDescriptionDict.TryGetValue(functionClass, out var result))
			{
				return "UNKNOWN";
			}
			return result;
		}

		public static FUNCTION_CLASS Get(DEVICE_TYPE deviceType, FUNCTION_NAME functionName)
		{
			if ((byte)deviceType == 1 && (ushort)functionName == 262)
			{
				return FUNCTION_CLASS.Text;
			}
			if ((ushort)functionName == 384)
			{
				return FUNCTION_CLASS.NETWORK_BRIDGE;
			}
			if (ExclusiveFunctionClass.TryGetValue(deviceType, out var result))
			{
				return result;
			}
			foreach (FUNCTION_CLASS functionClass in functionName.GetFunctionClasses())
			{
				if (Enumerable.Contains<DEVICE_TYPE>(functionClass.GetDeviceTypeList(), deviceType))
				{
					return functionClass;
				}
			}
			return GetFirstFunctionClass(deviceType);
		}

		private static FUNCTION_CLASS GetFirstFunctionClass(DEVICE_TYPE deviceType)
		{
			if (ExclusiveFunctionClass.TryGetValue(deviceType, out var result))
			{
				return result;
			}
			if (DefaultFunctionClass.TryGetValue(deviceType, out var result2))
			{
				return result2;
			}
			foreach (KeyValuePair<FUNCTION_CLASS, IReadOnlyCollection<DEVICE_TYPE>> item in FunctionClassDevicesDict)
			{
				if (Enumerable.Contains<DEVICE_TYPE>(item.Value, deviceType))
				{
					return item.Key;
				}
			}
			return FUNCTION_CLASS.UNKNOWN;
		}

		public static IEnumerable<FUNCTION_CLASS> GetEnumerator()
		{
			return FunctionClassDescriptionDict.Keys;
		}

		public static ReadOnlyCollection<FUNCTION_CLASS> ToCollection(this FUNCTION_CLASS functionClass)
		{
			lock (SingleFunctionClassCache)
			{
				SingleFunctionClassCache.TryGetValue(functionClass, out var value);
				if (value == null)
				{
					value = new ReadOnlyCollection<FUNCTION_CLASS>(new FUNCTION_CLASS[1] { functionClass });
					SingleFunctionClassCache[functionClass] = value;
				}
				return value;
			}
		}

		public static ReadOnlyCollection<FUNCTION_CLASS> ToCollection(this FUNCTION_CLASS functionClass, params FUNCTION_CLASS[] additionalFunctionClasses)
		{
			List<FUNCTION_CLASS> list = new List<FUNCTION_CLASS> { functionClass };
			if (additionalFunctionClasses != null && additionalFunctionClasses.Length != 0)
			{
				foreach (FUNCTION_CLASS fUNCTION_CLASS in additionalFunctionClasses)
				{
					if (!list.Contains(fUNCTION_CLASS))
					{
						list.Add(fUNCTION_CLASS);
					}
				}
			}
			return new ReadOnlyCollection<FUNCTION_CLASS>(list);
		}
	}
}
