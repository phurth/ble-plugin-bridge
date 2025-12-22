using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;

namespace ids.portable.ble.Platforms.Shared.ManufacturingData
{
	public readonly struct BleConnectionCount : IIdsManufacturerSpecificData, IManufacturerSpecificData, IReadOnlyList<byte>, IEnumerable<byte>, IEnumerable, IReadOnlyCollection<byte>
	{
		private const int RequiredDataSize = 1;

		private readonly byte _rawData;

		public IdsManufacturerSpecificDataType DataType => IdsManufacturerSpecificDataType.Connections;

		public bool IsValid => true;

		public int CurrentConnections => _rawData.GetLowerNibble();

		public int MaxConnections => _rawData.GetUpperNibble();

		public int Count => 1;

		public byte this[int index]
		{
			get
			{
				if (index != 0)
				{
					throw new ArgumentOutOfRangeException("index");
				}
				return _rawData;
			}
		}

		public BleConnectionCount(IReadOnlyList<byte> data)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			if (data.Count != 1)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(48, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Given data was expected to be ");
				defaultInterpolatedStringHandler.AppendFormatted(1);
				defaultInterpolatedStringHandler.AppendLiteral(" in size, but was ");
				defaultInterpolatedStringHandler.AppendFormatted(data.Count);
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			_rawData = data[0];
		}

		public override string ToString()
		{
			if (!IsValid)
			{
				return "Connections Unknown";
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(13, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Connections ");
			defaultInterpolatedStringHandler.AppendFormatted(CurrentConnections);
			defaultInterpolatedStringHandler.AppendLiteral("/");
			defaultInterpolatedStringHandler.AppendFormatted(MaxConnections);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		[IteratorStateMachine(typeof(_003CGetEnumerator_003Ed__12))]
		public IEnumerator<byte> GetEnumerator()
		{
			//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
			return new _003CGetEnumerator_003Ed__12(0)
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
