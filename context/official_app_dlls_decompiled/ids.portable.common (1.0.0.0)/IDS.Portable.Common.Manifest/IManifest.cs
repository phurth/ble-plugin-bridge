using System.Collections;
using System.Collections.Generic;

namespace IDS.Portable.Common.Manifest
{
	public interface IManifest : IEnumerable<IManifestProduct>, IEnumerable
	{
		IEnumerable<IManifestProduct> Products { get; }

		IMainfestRvConfig RvConfig { get; }

		IManifestProduct AddProduct(IManifestProduct product, bool updateSoftwarePartNumber);

		IManifestProduct? FindProduct(string uniqueProductId);

		string ToJSON();

		string MakeProductBlueprintId();
	}
}
