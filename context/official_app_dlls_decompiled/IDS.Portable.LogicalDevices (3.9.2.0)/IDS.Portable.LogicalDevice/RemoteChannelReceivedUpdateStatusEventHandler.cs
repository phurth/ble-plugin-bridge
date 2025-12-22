namespace IDS.Portable.LogicalDevice
{
	public delegate void RemoteChannelReceivedUpdateStatusEventHandler<in TValue>(IRemoteChannelDef channel, TValue value);
}
