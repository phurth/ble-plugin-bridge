namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceRemoteManager
	{
		void StopRemote();

		bool IsRemoteAccessAvailable(ILogicalDeviceRemote remote);

		bool IsRemoteAccessAvailable(ILogicalDeviceRemote logicalDeviceRemote, IRemoteChannelDef channel);

		TChannelDef? MakeRemoteChannel<TChannelDef>(ILogicalDeviceRemote logicalDevice, string channelId, IRemoteChannelCollection? channelCollection = null) where TChannelDef : class, IRemoteChannelDef;
	}
}
