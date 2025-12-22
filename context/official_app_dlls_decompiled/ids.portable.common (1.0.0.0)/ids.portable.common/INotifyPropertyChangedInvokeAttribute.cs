namespace IDS.Portable.Common
{
	public interface INotifyPropertyChangedInvokeAttribute
	{
		string ProxyName { get; }

		string SourcePropertyName { get; }
	}
}
