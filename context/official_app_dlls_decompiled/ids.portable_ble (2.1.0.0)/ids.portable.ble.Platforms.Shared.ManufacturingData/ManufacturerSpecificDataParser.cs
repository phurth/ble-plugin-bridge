using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ids.portable.ble.Platforms.Shared.ManufacturingData
{
	public static class ManufacturerSpecificDataParser
	{
		private const int CompanyIdentifierNumBytes = 2;

		private const int PayloadSizeNumBytes = 1;

		private const int PayloadTypeNumBytes = 1;

		private const int PayloadDataStartIndex = 2;

		private const int MinimumLippertAdvertisementLength = 7;

		[IteratorStateMachine(typeof(_003CParseLciManufacturerSpecificData_003Ed__5))]
		public static IEnumerable<IManufacturerSpecificData> ParseLciManufacturerSpecificData(this byte[] manufacturerSpecificData)
		{
			//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
			return new _003CParseLciManufacturerSpecificData_003Ed__5(-2)
			{
				_003C_003E3__manufacturerSpecificData = manufacturerSpecificData
			};
		}
	}
}
