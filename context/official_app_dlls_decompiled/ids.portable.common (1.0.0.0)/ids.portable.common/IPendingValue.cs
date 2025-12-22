namespace IDS.Portable.Common
{
	public interface IPendingValue<TValue>
	{
		TValue Value { get; set; }

		bool IsValuePending { get; }

		void SetPendingValue(TValue value);

		void TryCancelPendingValue();
	}
}
