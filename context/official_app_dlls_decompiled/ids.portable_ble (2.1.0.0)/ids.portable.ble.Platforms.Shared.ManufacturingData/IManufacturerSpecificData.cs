using System.Collections;
using System.Collections.Generic;

namespace ids.portable.ble.Platforms.Shared.ManufacturingData
{
	public interface IManufacturerSpecificData : IReadOnlyList<byte>, IEnumerable<byte>, IEnumerable, IReadOnlyCollection<byte>
	{
	}
}
