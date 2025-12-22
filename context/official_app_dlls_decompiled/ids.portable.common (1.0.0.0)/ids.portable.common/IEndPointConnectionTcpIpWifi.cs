namespace IDS.Portable.Common
{
	public interface IEndPointConnectionTcpIpWifi : IEndPointConnectionTcpIp, IEndPointConnection, IEndPointConnectionWithPassword
	{
		string ConnectionSsid { get; }
	}
}
