using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;

namespace ids.portable.ble.Platforms.Shared.ManufacturingData
{
	public readonly struct PairingInfo : IIdsManufacturerSpecificData, IManufacturerSpecificData, IReadOnlyList<byte>, IEnumerable<byte>, IEnumerable, IReadOnlyCollection<byte>
	{
		private const int RequiredDataSize = 1;

		private readonly byte[] _rawData;

		public IdsManufacturerSpecificDataType DataType => IdsManufacturerSpecificDataType.PairingInfo;

		public bool IsValid => true;

		public bool IsPushToPairButtonPresentOnBus
		{
			get
			{
				if (_rawData != null)
				{
					return _rawData[0].IsBitSet(0);
				}
				return false;
			}
		}

		public int Count => 1;

		public byte this[int index] => _rawData[index];

		public PairingInfo(IReadOnlyList<byte> data)
		{
			_rawData = new byte[1];
			if (data.Count != 1)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(48, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Given data was expected to be ");
				defaultInterpolatedStringHandler.AppendFormatted(1);
				defaultInterpolatedStringHandler.AppendLiteral(" in size, but was ");
				defaultInterpolatedStringHandler.AppendFormatted(data.Count);
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			for (int i = 0; i < 1; i++)
			{
				_rawData[i] = data[i];
			}
		}

		public override string ToString()
		{
			if (!IsValid)
			{
				return "Pairing Info Unknown";
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(26, 1);
			defaultInterpolatedStringHandler.AppendLiteral("IsPushToPairButtonPresent ");
			defaultInterpolatedStringHandler.AppendFormatted(IsPushToPairButtonPresentOnBus);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		public IEnumerator<byte> GetEnumerator()
		{
			return (IEnumerator<byte>)_rawData.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
