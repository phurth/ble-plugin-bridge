namespace IDS.Core.IDS_CAN
{
	public struct BLOCKPropertyValue
	{
		public ulong PropertyValue { get; set; }

		public bool IsValueValid { get; set; }

		internal BLOCKPropertyValue(ulong Propertyvalue, bool Isvaluevalid)
		{
			PropertyValue = Propertyvalue;
			IsValueValid = Isvaluevalid;
		}
	}
}
