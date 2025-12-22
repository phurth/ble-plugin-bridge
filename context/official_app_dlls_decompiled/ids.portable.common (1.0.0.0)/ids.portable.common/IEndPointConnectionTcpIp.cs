namespace IDS.Portable.Common
{
	public interface IEndPointConnectionTcpIp : IEndPointConnection
	{
		string ConnectionIpAddress { get; }
	}
}
