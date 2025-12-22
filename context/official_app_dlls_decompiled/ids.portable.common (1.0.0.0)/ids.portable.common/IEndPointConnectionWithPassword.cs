namespace IDS.Portable.Common
{
	public interface IEndPointConnectionWithPassword : IEndPointConnection
	{
		string ConnectionPassword { get; }
	}
}
