using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ids.portable.ble.ScanResults;

namespace ids.portable.ble.Platforms.Shared.ManufacturingData
{
	public readonly struct BleCanGatewayProtocolVersion : IIdsManufacturerSpecificData, IManufacturerSpecificData, IReadOnlyList<byte>, IEnumerable<byte>, IEnumerable, IReadOnlyCollection<byte>
	{
		private readonly byte _rawData;

		private const int RequiredDataSize = 1;

		public IdsManufacturerSpecificDataType DataType => IdsManufacturerSpecificDataType.BleCanGatewayProtocolVersion;

		public bool IsValid => true;

		public BleGatewayInfo.GatewayVersion GatewayVersion
		{
			get
			{
				if (_rawData < 68)
				{
					return BleGatewayInfo.GatewayVersion.Unknown;
				}
				return BleGatewayInfo.GatewayVersion.V2_D;
			}
		}

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

		public BleCanGatewayProtocolVersion(IReadOnlyList<byte> data)
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

		[IteratorStateMachine(typeof(_003CGetEnumerator_003Ed__9))]
		public IEnumerator<byte> GetEnumerator()
		{
			//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
			return new _003CGetEnumerator_003Ed__9(0)
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
