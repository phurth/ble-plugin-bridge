using System;
using System.Collections.Generic;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceStatusPacketMutableExtended : LogicalDeviceDataPacketMutableDoubleBuffer, IDeviceDataPacketMutableExtended, IDeviceDataPacketMutable, IDeviceDataPacket
	{
		public const string LogTag = "LogicalDeviceStatusPacketMutableExtended";

		public DateTime? LastUpdatedTimestamp { get; private set; }

		public virtual byte ExtendedByte { get; protected set; }

		public LogicalDeviceStatusPacketMutableExtended(uint minSize, uint maxSize, byte bufferFill = 0)
			: base(minSize, maxSize, bufferFill)
		{
		}

		public LogicalDeviceStatusPacketMutableExtended(uint minSize)
			: this(minSize, 8u, 0)
		{
		}

		public LogicalDeviceStatusPacketMutableExtended()
			: this(1u, 8u, 0)
		{
		}

		public bool Update(IReadOnlyDictionary<byte, byte[]> inData, DateTime? timeUpdated = null)
		{
			if (inData == null)
			{
				return false;
			}
			bool flag = false;
			DateTime valueOrDefault = timeUpdated.GetValueOrDefault();
			if (!timeUpdated.HasValue)
			{
				valueOrDefault = DateTime.Now;
				timeUpdated = valueOrDefault;
			}
			foreach (KeyValuePair<byte, byte[]> inDatum in inData)
			{
				byte key = inDatum.Key;
				byte[] value = inDatum.Value;
				flag |= Update(value, (uint)value.Length, key, timeUpdated);
			}
			return flag;
		}

		public bool Update(byte[] inData, int length, byte extendedByte, DateTime? timeUpdated = null)
		{
			return Update(inData, (uint)length, extendedByte, timeUpdated);
		}

		public virtual bool Update(IReadOnlyList<byte> inData, uint length, byte extendedByte, DateTime? timeUpdated = null)
		{
			LastUpdatedTimestamp = timeUpdated ?? DateTime.Now;
			bool flag = ExtendedByte != extendedByte;
			ExtendedByte = extendedByte;
			return base.Update(inData, (int)length, flag) != length || flag;
		}
	}
}
