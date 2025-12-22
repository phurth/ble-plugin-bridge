namespace ids.portable.ble.Platforms.Shared.BleScanner
{
	public interface IBleScanResultFactoryRegistry
	{
		void Register<TKey>(IBleScanResultFactory<TKey> scanResultFactory) where TKey : notnull;

		void UnRegister<TKey>(IBleScanResultFactory<TKey> scanResultFactory) where TKey : notnull;
	}
}
