namespace IDS.Portable.Common
{
	public interface IEndPointConnectionWithTcpIpPort : IEndPointConnection
	{
		int ConnectionPort { get; }
	}
}
