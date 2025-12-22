using IDS.Core.IDS_CAN;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class ProductDtc : CommonDisposableNotifyPropertyChanged
	{
		private bool _isActive;

		private bool _isStored;

		private int _powerCyclesCounter;

		public DTC_ID ID { get; }

		public string Name { get; }

		public bool IsActive
		{
			get
			{
				return _isActive;
			}
			private set
			{
				SetBackingField(ref _isActive, value, "IsActive");
			}
		}

		public bool IsStored
		{
			get
			{
				return _isStored;
			}
			private set
			{
				SetBackingField(ref _isStored, value, "IsStored");
			}
		}

		public int PowerCyclesCounter
		{
			get
			{
				return _powerCyclesCounter;
			}
			private set
			{
				SetBackingField(ref _powerCyclesCounter, value, "PowerCyclesCounter");
			}
		}

		public ProductDtc(DTC_ID id, DtcValue value)
		{
			ID = id;
			Name = id.ToString();
			UpdateState(value);
		}

		public void UpdateState(DtcValue value)
		{
			IsActive = value.IsActive;
			IsStored = value.IsStored;
			PowerCyclesCounter = value.PowerCyclesCounter;
		}
	}
}
