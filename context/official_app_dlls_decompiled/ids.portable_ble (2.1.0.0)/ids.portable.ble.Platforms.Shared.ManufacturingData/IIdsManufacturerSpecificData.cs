using System.Collections;
using System.Collections.Generic;

namespace ids.portable.ble.Platforms.Shared.ManufacturingData
{
	public interface IIdsManufacturerSpecificData : IManufacturerSpecificData, IReadOnlyList<byte>, IEnumerable<byte>, IEnumerable, IReadOnlyCollection<byte>
	{
		IdsManufacturerSpecificDataType DataType { get; }

		bool IsValid { get; }
	}
}
