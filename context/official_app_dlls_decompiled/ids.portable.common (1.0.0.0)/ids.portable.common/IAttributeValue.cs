namespace IDS.Portable.Common
{
	public interface IAttributeValue<out TValue>
	{
		TValue Value { get; }
	}
}
