namespace IDS.Portable.Common.Manifest
{
	public interface IManifestDTC
	{
		ushort TypeID { get; }

		string Name { get; }

		bool IsActive { get; }

		bool IsStored { get; }

		int PowerCyclesCounter { get; }
	}
}
