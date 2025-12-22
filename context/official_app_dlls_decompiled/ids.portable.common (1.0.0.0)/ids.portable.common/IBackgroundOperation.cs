namespace IDS.Portable.Common
{
	public interface IBackgroundOperation
	{
		void Start();

		void Stop();
	}
	public interface IBackgroundOperation<in TBackgroundArg1>
	{
		void Start(TBackgroundArg1 arg1);

		void Stop();
	}
	public interface IBackgroundOperation<in TBackgroundArg1, in TBackgroundArg2>
	{
		void Start(TBackgroundArg1 arg1, TBackgroundArg2 arg2);

		void Stop();
	}
}
