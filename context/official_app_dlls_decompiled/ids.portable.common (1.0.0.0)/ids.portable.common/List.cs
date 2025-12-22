using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;

namespace IDS.Portable.Common
{
	public static class List
	{
		private static readonly byte[] ConversionBuffer = new byte[16];

		public static List<TValue> Remove<TValue>(this List<TValue> list, Func<TValue, bool> removeItemFilter)
		{
			List<TValue> list2 = new List<TValue>();
			if (removeItemFilter == null)
			{
				return list2;
			}
			foreach (TValue item in list)
			{
				if (removeItemFilter(item))
				{
					list2.Add(item);
				}
			}
			list.Remove(list2);
			return list2;
		}

		public static void Remove<TValue>(this List<TValue> list, IEnumerable<TValue> itemsToRemove)
		{
			if (itemsToRemove == null)
			{
				return;
			}
			foreach (TValue item in itemsToRemove)
			{
				try
				{
					list.Remove(item);
				}
				catch
				{
				}
			}
		}

		public static IEnumerable<TValue> RemoveDuplicates<TValue>(this List<TValue> list)
		{
			if (list == null)
			{
				yield break;
			}
			HashSet<TValue> returned = new HashSet<TValue>();
			foreach (TValue item in list)
			{
				if (!returned.Contains(item))
				{
					returned.Add(item);
					yield return item;
				}
			}
		}

		public static int BinarySearch<T>(this IList<T> collection, T? value)
		{
			return collection.BinarySearch(value, Comparer<T>.Default);
		}

		public static int BinarySearch<T>(this IList<T> collection, T? value, IComparer<T>? comparer)
		{
			if (comparer == null)
			{
				comparer = Comparer<T>.Default;
			}
			int num = 0;
			int num2 = collection.Count - 1;
			while (num <= num2)
			{
				int num3 = num + (num2 - num >> 1);
				int num4 = comparer!.Compare(collection[num3], value);
				if (num4 >= 0)
				{
					if (num4 == 0)
					{
						return num3;
					}
					num2 = num3 - 1;
				}
				else
				{
					num = num3 + 1;
				}
			}
			return ~num;
		}

		public static List<byte> AppendValueByte(this List<byte> list, byte value)
		{
			list.Add(value);
			return list;
		}

		public static List<byte> AppendValueUInt16(this List<byte> list, ushort value)
		{
			lock (ConversionBuffer)
			{
				ConversionBuffer.SetValueUInt16(value, 0);
				list.Add(ConversionBuffer[0]);
				list.Add(ConversionBuffer[1]);
				return list;
			}
		}

		public static List<byte> AppendValueUInt24(this List<byte> list, uint value)
		{
			lock (ConversionBuffer)
			{
				ConversionBuffer.SetValueUInt24(value, 0);
				list.Add(ConversionBuffer[0]);
				list.Add(ConversionBuffer[1]);
				list.Add(ConversionBuffer[3]);
				return list;
			}
		}

		public static List<byte> AppendValueUInt32(this List<byte> list, uint value)
		{
			lock (ConversionBuffer)
			{
				ConversionBuffer.SetValueUInt32(value, 0);
				list.Add(ConversionBuffer[0]);
				list.Add(ConversionBuffer[1]);
				list.Add(ConversionBuffer[2]);
				list.Add(ConversionBuffer[3]);
				return list;
			}
		}

		public static List<byte> AppendValueUInt48(this List<byte> list, ulong value)
		{
			lock (ConversionBuffer)
			{
				ConversionBuffer.SetValueUInt48(value, 0);
				list.Add(ConversionBuffer[0]);
				list.Add(ConversionBuffer[1]);
				list.Add(ConversionBuffer[2]);
				list.Add(ConversionBuffer[3]);
				list.Add(ConversionBuffer[4]);
				list.Add(ConversionBuffer[5]);
				return list;
			}
		}

		public static List<byte> AppendValueFixedPointFloat(this List<byte> list, float value, FixedPointType fixedPoint)
		{
			lock (ConversionBuffer)
			{
				ConversionBuffer.SetFixedPointFloat(value, 0u, fixedPoint);
				switch (fixedPoint)
				{
				case FixedPointType.UnsignedBigEndian8x8:
				case FixedPointType.SignedBigEndian8x8:
					list.Add(ConversionBuffer[0]);
					list.Add(ConversionBuffer[1]);
					return list;
				case FixedPointType.UnsignedBigEndian16x16:
				case FixedPointType.SignedBigEndian16x16:
					list.Add(ConversionBuffer[0]);
					list.Add(ConversionBuffer[1]);
					list.Add(ConversionBuffer[3]);
					list.Add(ConversionBuffer[4]);
					return list;
				default:
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(12, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Unknown ");
					defaultInterpolatedStringHandler.AppendFormatted("FixedPointType");
					defaultInterpolatedStringHandler.AppendLiteral(" of ");
					defaultInterpolatedStringHandler.AppendFormatted(fixedPoint);
					throw new NotImplementedException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				}
			}
		}
	}
}
