using System;
using System.Collections.Generic;

namespace ids.portable.ble.Platforms.Shared.BleScanner
{
	public interface IBleScanResultFactoryManufacturerId : IBleScanResultFactory<ushort>
	{
		IEnumerable<Guid>? PrimaryServiceUuids { get; }
	}
}
