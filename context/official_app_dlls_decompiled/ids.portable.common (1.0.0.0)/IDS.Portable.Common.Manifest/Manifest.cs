using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;

namespace IDS.Portable.Common.Manifest
{
	[JsonObject(MemberSerialization.OptIn)]
	public class Manifest : IManifest, IEnumerable<IManifestProduct>, IEnumerable
	{
		[JsonProperty(PropertyName = "ProductList")]
		private List<IManifestProduct> _productList;

		[JsonProperty(PropertyName = "RVConfig")]
		public IMainfestRvConfig RvConfig { get; }

		public IEnumerable<IManifestProduct> Products => _productList;

		public Manifest()
		{
			_productList = new List<IManifestProduct>();
			RvConfig = new MainfestRvConfig();
		}

		[JsonConstructor]
		public Manifest(List<ManifestProduct> productList, MainfestRvConfig rvConfig)
			: this()
		{
			_productList.AddRange(productList);
			RvConfig = rvConfig;
		}

		public IManifestProduct AddProduct(IManifestProduct product, bool updateSoftwarePartNumber)
		{
			IManifestProduct manifestProduct = FindProduct(product.UniqueID);
			if (manifestProduct != null)
			{
				if (updateSoftwarePartNumber && string.IsNullOrEmpty(manifestProduct.SoftwarePartNumber) && !string.IsNullOrEmpty(product.SoftwarePartNumber))
				{
					manifestProduct.SoftwarePartNumber = product.SoftwarePartNumber;
				}
				return manifestProduct;
			}
			_productList.Add(product);
			return product;
		}

		public IManifestProduct? FindProduct(string uniqueProductId)
		{
			if (uniqueProductId == null)
			{
				return null;
			}
			foreach (IManifestProduct product in _productList)
			{
				if (product.UniqueID == uniqueProductId)
				{
					return product;
				}
			}
			return null;
		}

		public string ToJSON()
		{
			return JsonConvert.SerializeObject(this, Formatting.Indented);
		}

		public static Manifest MakeManifestFromJSON(string json)
		{
			try
			{
				return JsonConvert.DeserializeObject<Manifest>(json);
			}
			catch (Exception ex)
			{
				throw new Exception("MakeManifestFromJSON error processing JSON", ex);
			}
		}

		public override string ToString()
		{
			try
			{
				return ToJSON();
			}
			catch
			{
				return base.ToString();
			}
		}

		public string MakeProductBlueprintId()
		{
			Dictionary<ushort, byte> dictionary = new Dictionary<ushort, byte>();
			foreach (IManifestProduct product in _productList)
			{
				byte b = dictionary.TryGetValue(product.TypeID);
				b = (byte)(b + 1);
				dictionary[product.TypeID] = b;
			}
			StringBuilder stringBuilder = new StringBuilder();
			foreach (KeyValuePair<ushort, byte> item in Enumerable.OrderBy(dictionary, (KeyValuePair<ushort, byte> i) => i.Key))
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(0, 2, stringBuilder2);
				handler.AppendFormatted(item.Value, "X2");
				handler.AppendFormatted(item.Key, "X2");
				stringBuilder2.Append(ref handler);
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 1);
			defaultInterpolatedStringHandler.AppendLiteral("BL");
			defaultInterpolatedStringHandler.AppendFormatted(stringBuilder);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		public IEnumerator<IManifestProduct> GetEnumerator()
		{
			return _productList.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
