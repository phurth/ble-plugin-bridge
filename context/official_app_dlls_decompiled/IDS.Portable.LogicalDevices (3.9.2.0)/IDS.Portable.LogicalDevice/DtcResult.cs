using System.Collections.Generic;
using IDS.Portable.Common.Manifest;

namespace IDS.Portable.LogicalDevice
{
	internal struct DtcResult
	{
		public readonly List<IManifestDTC> DtcList;

		public readonly DtcListType ListType;

		public DtcResult(List<IManifestDTC> dtcList, DtcListType listType)
		{
			DtcList = dtcList;
			ListType = listType;
		}
	}
}
