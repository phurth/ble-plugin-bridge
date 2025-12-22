using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Core;
using IDS.Portable.Common.Extensions;

namespace IDS.Portable.LogicalDevice
{
	public readonly struct ByteList : Comm.IByteList, IReadOnlyList<byte>, IEnumerable<byte>, IEnumerable, IReadOnlyCollection<byte>
	{
		private readonly byte[] _data;

		public int Count { get; }

		public byte this[int index] => _data[index];

		[Obsolete("Deprecated, should use Count")]
		public int Length => Count;

		public ByteList(byte[] inData, int length)
		{
			if (inData.Length < length)
			{
				throw new LogicalDeviceDataPacketException($"Data Update not possible because inData size {inData.Length} is less then what was specified as the length {length}.");
			}
			_data = inData;
			Count = length;
		}

		[IteratorStateMachine(typeof(_003CGetEnumerator_003Ed__5))]
		public IEnumerator<byte> GetEnumerator()
		{
			//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
			return new _003CGetEnumerator_003Ed__5(0)
			{
				_003C_003E4__this = this
			};
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void CopyTo(byte[] array, int index)
		{
			Array.Copy(_data, 0, array, index, Count);
		}

		public void CopyRangeTo(int sourceIndex, int count, byte[] array, int destIndex)
		{
			Array.Copy(_data, sourceIndex, array, destIndex, count);
		}

		public string ToString(bool dataOnly)
		{
			return _data.DebugDump();
		}

		public override string ToString()
		{
			return _data.DebugDump();
		}
	}
}
