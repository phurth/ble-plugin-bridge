using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;

namespace ids.portable.ble.Platforms.Shared.ManufacturingData
{
	public readonly struct ConnectionInfo : IIdsManufacturerSpecificData, IManufacturerSpecificData, IReadOnlyList<byte>, IEnumerable<byte>, IEnumerable, IReadOnlyCollection<byte>
	{
		private const int RequiredDataSize = 2;

		private readonly byte _rawDataStatus;

		private readonly byte _rawDataPairing;

		public IdsManufacturerSpecificDataType DataType => IdsManufacturerSpecificDataType.ConnectionInfo;

		public bool IsValid => true;

		public BleCapability BleCapability => (BleCapability)_rawDataStatus.GetLowerNibble();

		public bool OOBSupported => _rawDataStatus.IsBitSet(4);

		public bool MITM => _rawDataStatus.IsBitSet(5);

		public bool LESecure => _rawDataStatus.IsBitSet(6);

		public bool BondingSupported => _rawDataStatus.IsBitSet(7);

		public bool PairingSupported => _rawDataPairing.IsBitSet(0);

		public bool PairingAvailableNow => _rawDataPairing.IsBitSet(1);

		public int Count => 2;

		public byte this[int index] => index switch
		{
			0 => _rawDataStatus, 
			1 => _rawDataPairing, 
			_ => throw new ArgumentOutOfRangeException("index"), 
		};

		public ConnectionInfo(IReadOnlyList<byte> data)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			if (data.Count != 2)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(48, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Given data was expected to be ");
				defaultInterpolatedStringHandler.AppendFormatted(2);
				defaultInterpolatedStringHandler.AppendLiteral(" in size, but was ");
				defaultInterpolatedStringHandler.AppendFormatted(data.Count);
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			_rawDataStatus = data[0];
			_rawDataPairing = data[1];
		}

		[IteratorStateMachine(typeof(_003CGetEnumerator_003Ed__22))]
		public IEnumerator<byte> GetEnumerator()
		{
			//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
			return new _003CGetEnumerator_003Ed__22(0)
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
