namespace IDS.Core.Events
{
	public interface IEventSender
	{
		IEventPublisher Events { get; }
	}
}
