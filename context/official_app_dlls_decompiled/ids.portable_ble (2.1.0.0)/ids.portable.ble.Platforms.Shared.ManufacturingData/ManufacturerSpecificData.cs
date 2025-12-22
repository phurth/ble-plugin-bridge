using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ids.portable.ble.Platforms.Shared.ManufacturingData
{
	public readonly struct ManufacturerSpecificData : IManufacturerSpecificData, IReadOnlyList<byte>, IEnumerable<byte>, IEnumerable, IReadOnlyCollection<byte>
	{
		private readonly byte[] _data;

		public int Count => _data.Length;

		public byte this[int index] => _data[index];

		public ManufacturerSpecificData(IReadOnlyList<byte> srcData)
		{
			int count = srcData.Count;
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("srcData", "Source data length can't be negative!");
			}
			_data = new byte[count];
			int num = 0;
			for (int i = 0; i < _data.Length; i++)
			{
				_data[num++] = srcData[i];
			}
		}

		[IteratorStateMachine(typeof(_003CGetEnumerator_003Ed__2))]
		public IEnumerator<byte> GetEnumerator()
		{
			//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
			return new _003CGetEnumerator_003Ed__2(0)
			{
				_003C_003E4__this = this
			};
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
