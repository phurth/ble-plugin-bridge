using System;
using IDS.Portable.Common.Extensions;

namespace IDS.Portable.Common
{
	public static class Enum<TValue> where TValue : struct, IConvertible
	{
		public static TValue TryConvert(object? fromValue)
		{
			try
			{
				Convert(fromValue, out var toValue, enableDefaultValue: true);
				return toValue;
			}
			catch (Exception ex)
			{
				TaggedLog.Error("EnumExtensions", "TryConvert failed from {0}({1}) to {2}: {3}", fromValue?.GetType().Name, fromValue, typeof(TValue).Name, ex.Message);
				return Default<TValue>.Value;
			}
		}

		public static bool TryConvert(object fromValue, out TValue toValue)
		{
			try
			{
				return Convert(fromValue, out toValue, enableDefaultValue: true);
			}
			catch (Exception ex)
			{
				TaggedLog.Error("EnumExtensions", "TryConvert failed from {0}({1}) to {2}: {3}", fromValue.GetType().Name, fromValue, typeof(TValue).Name, ex.Message);
				toValue = Default<TValue>.Value;
				return false;
			}
		}

		public static bool Convert(object fromValue, out TValue toValue, bool enableDefaultValue = false)
		{
			Type typeFromHandle = typeof(TValue);
			if (!typeFromHandle.IsEnum)
			{
				throw new TypeIsNotAnEnumException(typeFromHandle);
			}
			if (!typeFromHandle.IsDefined(typeof(FlagsAttribute), false))
			{
				if (!(fromValue is string) && fromValue.GetType() != Enum.GetUnderlyingType(typeFromHandle) && fromValue is IConvertible value)
				{
					fromValue = System.Convert.ChangeType(value, Enum.GetUnderlyingType(typeFromHandle));
				}
				if (!Enum.IsDefined(typeFromHandle, fromValue))
				{
					if (!enableDefaultValue)
					{
						throw new ConvertObjectToEnumException(typeFromHandle, fromValue);
					}
					toValue = Default<TValue>.Value;
					return false;
				}
			}
			if (fromValue is string value2)
			{
				toValue = (TValue)Enum.Parse(typeFromHandle, value2);
			}
			else
			{
				toValue = (TValue)Enum.ToObject(typeFromHandle, fromValue);
			}
			return true;
		}
	}
}
