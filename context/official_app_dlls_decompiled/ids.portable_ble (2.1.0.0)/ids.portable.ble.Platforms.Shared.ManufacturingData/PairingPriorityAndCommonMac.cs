using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ids.portable.ble.Platforms.Shared.ManufacturingData
{
	public readonly struct PairingPriorityAndCommonMac : IIdsManufacturerSpecificData, IManufacturerSpecificData, IReadOnlyList<byte>, IEnumerable<byte>, IEnumerable, IReadOnlyCollection<byte>
	{
		private const int RequiredDataSize = 7;

		private const int NumberOfBytesBeforeCommonMac = 1;

		private const int CommonMacSize = 6;

		private readonly bool _isSet;

		private readonly byte[] _rawData;

		private IReadOnlyList<byte> RawData
		{
			get
			{
				if (_isSet)
				{
					return _rawData;
				}
				_rawData[0] = 254;
				byte[] array = Guid.NewGuid().ToByteArray();
				for (int i = 1; i < 7; i++)
				{
					_rawData[i] = array[i];
				}
				return _rawData;
			}
		}

		public IdsManufacturerSpecificDataType DataType => IdsManufacturerSpecificDataType.PairingPriorityAndCommonMac;

		public bool IsValid => true;

		public byte PairingPriority => RawData[0];

		public byte[] CommonMac => Enumerable.ToArray(Enumerable.Take(Enumerable.Skip(RawData, 1), 6));

		public int Count => 7;

		public byte this[int index] => RawData[index];

		public PairingPriorityAndCommonMac(IReadOnlyList<byte> data)
		{
			_rawData = new byte[7];
			_isSet = data.Count == 7;
			if (!_isSet)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(48, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Given data was expected to be ");
				defaultInterpolatedStringHandler.AppendFormatted(7);
				defaultInterpolatedStringHandler.AppendLiteral(" in size, but was ");
				defaultInterpolatedStringHandler.AppendFormatted(data.Count);
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			for (int i = 0; i < 7; i++)
			{
				_rawData[i] = data[i];
			}
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(32, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Pairing Priority: ");
			defaultInterpolatedStringHandler.AppendFormatted(PairingPriority);
			defaultInterpolatedStringHandler.AppendLiteral(", ");
			defaultInterpolatedStringHandler.AppendLiteral("Common MAC: ");
			defaultInterpolatedStringHandler.AppendFormatted(CommonMac);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		public IEnumerator<byte> GetEnumerator()
		{
			return RawData.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
