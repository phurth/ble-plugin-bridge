namespace IDS.Portable.Common
{
	public class AsyncValueCachedOperation<TValue>
	{
		public TValue ValueToRevertTo { get; set; }

		public TValue ValueNew { get; }

		public AsyncValueCachedOperation(TValue valueToRevertTo, TValue valueNew)
		{
			ValueToRevertTo = valueToRevertTo;
			ValueNew = valueNew;
		}
	}
}
