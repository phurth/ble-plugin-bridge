namespace IDS.Core.IDS_CAN
{
	internal class ProductDTC : IProductDTC
	{
		public IRemoteProduct Product { get; private set; }

		public bool IsActive => (Status & 0x80) != 0;

		public bool IsStored => Status != 0;

		public int PowerCyclesCounter => Status & 0x7F;

		public DTC_ID ID { get; private set; }

		public string Name => ID.ToString();

		public byte Status { get; private set; }

		public ProductDTC(IRemoteProduct product, DTC_ID id, byte status)
		{
			Product = product;
			ID = id;
			Status = status;
		}

		public override string ToString()
		{
			return Name;
		}

		public static implicit operator DTC_ID(ProductDTC value)
		{
			return value.ID;
		}

		public bool Update(byte status)
		{
			bool isActive = IsActive;
			bool isStored = IsStored;
			int powerCyclesCounter = PowerCyclesCounter;
			Status = status;
			if (IsActive == isActive && IsStored == isStored)
			{
				return PowerCyclesCounter != powerCyclesCounter;
			}
			return true;
		}
	}
}
