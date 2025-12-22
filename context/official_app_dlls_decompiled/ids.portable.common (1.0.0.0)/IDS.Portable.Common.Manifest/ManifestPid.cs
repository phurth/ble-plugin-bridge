namespace IDS.Portable.Common.Manifest
{
	public struct ManifestPid : IManifestPid
	{
		public string Name { get; set; }

		public ulong Value { get; set; }
	}
}
