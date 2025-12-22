using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace IDS.Portable.Common.Extensions
{
	public static class EnumExtensions
	{
		private const string LogTag = "EnumExtensions";

		public static IEnumerable<TEnum> GetValues<TEnum>() where TEnum : struct, IConvertible
		{
			if (!typeof(TEnum).IsEnum)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(21, 1);
				defaultInterpolatedStringHandler.AppendFormatted(typeof(TEnum));
				defaultInterpolatedStringHandler.AppendLiteral(" must be an enum type");
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return Enumerable.Cast<TEnum>(Enum.GetValues(typeof(TEnum)));
		}

		public static T? GetAttribute<T>(this Enum instance, bool inherit = true) where T : Attribute
		{
			T result = null;
			try
			{
				result = CustomAttributeExtensions.GetCustomAttribute<T>(instance.GetType().GetField(instance.ToString()), inherit);
				return result;
			}
			catch
			{
				return result;
			}
		}

		public static TValue GetAttributeValue<TAttribute, TValue>(this Enum instance, Func<TAttribute, TValue> getValue, bool inherit = true) where TAttribute : Attribute
		{
			TAttribute attribute = instance.GetAttribute<TAttribute>(inherit);
			return getValue(attribute);
		}

		public static TValue GetAttributeValue<TAttribute, TValue>(this Enum instance, TValue defaultValue = default(TValue), bool inherit = true) where TAttribute : Attribute, IAttributeValue<TValue>
		{
			TAttribute attribute = instance.GetAttribute<TAttribute>(inherit);
			if (attribute == null)
			{
				return defaultValue;
			}
			return attribute.Value;
		}

		public static bool TryGetDescription(this Enum instance, out string? description)
		{
			DescriptionAttribute attribute = instance.GetAttribute<DescriptionAttribute>();
			description = attribute?.Description;
			return attribute != null;
		}

		public static string? Description(this Enum instance)
		{
			if (!instance.TryGetDescription(out var description))
			{
				return instance.ToString();
			}
			return description;
		}

		public static int EnumToInt<TEnum>(this TEnum value) where TEnum : struct, IConvertible
		{
			if (!typeof(TEnum).IsEnum)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(21, 1);
				defaultInterpolatedStringHandler.AppendFormatted(typeof(TEnum));
				defaultInterpolatedStringHandler.AppendLiteral(" must be an enum type");
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return Convert.ToInt32(value);
		}

		public static string DebugDumpAsFlags<TEnum>(this TEnum enumFlags) where TEnum : struct, IConvertible
		{
			return Convert.ToUInt32(enumFlags).DebugDumpAsFlags<TEnum>();
		}

		public static string DebugDumpAsFlags<TEnum>(this uint enumFlagsInt) where TEnum : struct, IConvertible
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
			if (!typeof(TEnum).IsEnum)
			{
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(21, 1);
				defaultInterpolatedStringHandler.AppendFormatted(typeof(TEnum));
				defaultInterpolatedStringHandler.AppendLiteral(" must be an enum type");
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			List<string> list = new List<string>();
			foreach (TEnum value in GetValues<TEnum>())
			{
				if ((enumFlagsInt & Convert.ToUInt32(value)) != 0)
				{
					list.Add(value.ToString(CultureInfo.InvariantCulture));
				}
			}
			string text = ((list.Count == 0) ? "none" : string.Join(", ", list));
			defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(5, 2);
			defaultInterpolatedStringHandler.AppendLiteral("0x");
			defaultInterpolatedStringHandler.AppendFormatted(enumFlagsInt, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(" (");
			defaultInterpolatedStringHandler.AppendFormatted(text);
			defaultInterpolatedStringHandler.AppendLiteral(")");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		public static TEnum UpdateFlag<TEnum>(this TEnum enumFlags, TEnum flags, bool setFlags) where TEnum : struct, IConvertible
		{
			if (!setFlags)
			{
				return enumFlags.ClearFlag(flags);
			}
			return enumFlags.SetFlag(flags);
		}

		public static TEnum SetFlag<TEnum>(this TEnum enumFlags, TEnum flags) where TEnum : struct, IConvertible
		{
			if (!typeof(TEnum).IsEnum)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(21, 1);
				defaultInterpolatedStringHandler.AppendFormatted(typeof(TEnum));
				defaultInterpolatedStringHandler.AppendLiteral(" must be an enum type");
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			int num = Convert.ToInt32(enumFlags);
			int num2 = Convert.ToInt32(flags);
			if ((num & num2) != num2)
			{
				num |= num2;
				return (TEnum)Enum.ToObject(typeof(TEnum), num);
			}
			return enumFlags;
		}

		public static TEnum ClearFlag<TEnum>(this TEnum enumFlags, TEnum flag) where TEnum : struct, IConvertible
		{
			if (!typeof(TEnum).IsEnum)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(21, 1);
				defaultInterpolatedStringHandler.AppendFormatted(typeof(TEnum));
				defaultInterpolatedStringHandler.AppendLiteral(" must be an enum type");
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			int num = Convert.ToInt32(enumFlags);
			int num2 = Convert.ToInt32(flag);
			if ((num & num2) != 0)
			{
				num &= ~num2;
				return (TEnum)Enum.ToObject(typeof(TEnum), num);
			}
			return enumFlags;
		}
	}
}
