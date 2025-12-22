using System;
using IDS.Core.Types;

namespace IDS.Core
{
	public static class CommExtensions
	{
		public static sbyte GetINT8(this Comm.IByteList msg, int index)
		{
			return (sbyte)msg.GetUINT8(index);
		}

		public static byte GetUINT8(this Comm.IByteList msg, int index)
		{
			if (index >= msg.Length)
			{
				throw new IndexOutOfRangeException();
			}
			return msg[index];
		}

		public static short GetINT16(this Comm.IByteList msg, int index)
		{
			return (short)((msg.GetUINT8(index) << 8) + msg.GetUINT8(index + 1));
		}

		public static ushort GetUINT16(this Comm.IByteList msg, int index)
		{
			return (ushort)((msg.GetUINT8(index) << 8) + msg.GetUINT8(index + 1));
		}

		public static Int24 GetINT24(this Comm.IByteList msg, int index)
		{
			return (Int24)((msg.GetUINT16(index) << 8) + msg.GetUINT8(index + 2));
		}

		public static UInt24 GetUINT24(this Comm.IByteList msg, int index)
		{
			return (UInt24)((msg.GetUINT16(index) << 8) + msg.GetUINT8(index + 2));
		}

		public static int GetINT32(this Comm.IByteList msg, int index)
		{
			return (msg.GetUINT16(index) << 16) + msg.GetUINT16(index + 2);
		}

		public static uint GetUINT32(this Comm.IByteList msg, int index)
		{
			return (uint)((msg.GetUINT16(index) << 16) + msg.GetUINT16(index + 2));
		}

		public static Int40 GetINT40(this Comm.IByteList msg, int index)
		{
			return (Int40)(((ulong)msg.GetUINT32(index) << 8) + msg.GetUINT8(index + 4));
		}

		public static UInt40 GetUINT40(this Comm.IByteList msg, int index)
		{
			return (UInt40)(((ulong)msg.GetUINT32(index) << 8) + msg.GetUINT8(index + 4));
		}

		public static Int48 GetINT48(this Comm.IByteList msg, int index)
		{
			return (Int48)(((ulong)msg.GetUINT32(index) << 16) + msg.GetUINT16(index + 4));
		}

		public static UInt48 GetUINT48(this Comm.IByteList msg, int index)
		{
			return (UInt48)(((ulong)msg.GetUINT32(index) << 16) + msg.GetUINT16(index + 4));
		}

		public static Int56 GetINT56(this Comm.IByteList msg, int index)
		{
			return (Int56)(((ulong)msg.GetUINT32(index) << 24) + (ulong)msg.GetUINT24(index + 4));
		}

		public static UInt56 GetUINT56(this Comm.IByteList msg, int index)
		{
			return (UInt56)(((ulong)msg.GetUINT32(index) << 24) + (ulong)msg.GetUINT24(index + 4));
		}

		public static long GetINT64(this Comm.IByteList msg, int index)
		{
			return (long)(((ulong)msg.GetUINT32(index) << 32) + msg.GetUINT32(index + 4));
		}

		public static ulong GetUINT64(this Comm.IByteList msg, int index)
		{
			return ((ulong)msg.GetUINT32(index) << 32) + msg.GetUINT32(index + 4);
		}
	}
}
