namespace IDS.Portable.LogicalDevice.Json
{
	public interface IJsonSerializable
	{
		bool TryJsonSerialize(out string? json);

		string? TryJsonSerialize();
	}
}
