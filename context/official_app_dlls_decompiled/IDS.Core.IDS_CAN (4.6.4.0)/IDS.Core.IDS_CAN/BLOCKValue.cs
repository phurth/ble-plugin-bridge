namespace IDS.Core.IDS_CAN
{
	public struct BLOCKValue
	{
		public IRemoteDevice Device { get; set; }

		public BLOCK_ID ID { get; set; }

		public bool IsValueValid { get; set; }

		public byte PropertyReadWrite { get; set; }

		public ushort PropertySessionRead { get; set; }

		public ushort PropertySessionWrite { get; set; }

		public ulong PropertyBlockCapacity { get; set; }

		public ulong PropertyCurrentBlockSize { get; set; }

		public uint PropertyCRC32 { get; set; }

		public uint PropertyCRC32Verify { get; set; }

		public ulong PropertySetAddress { get; set; }

		public uint BlockOffset { get; set; }

		public ushort ActualBulkTransferSize { get; set; }

		public ushort EndBulkXferOffset { get; set; }

		public uint EndBulkXferCRC32 { get; set; }

		public byte Response { get; set; }

		public byte[] BlockData { get; set; }

		internal BLOCKValue(IRemoteDevice device, BLOCK_ID id, bool Isvaluevalid, byte propertyreadwrite, ushort propertysessionread, ushort propertysessionwrite, ulong propertyblockcapacity, ulong propertycurrentblocksize, uint propertyCRC32, uint propertyCRC32verify, ulong propertySetAddress, uint blockoffset, ushort actualbulktransfersize, ushort endbulkxferoffset, uint endbulkxfercrc32, byte response, byte[] blockdata)
		{
			Device = device;
			ID = id;
			IsValueValid = Isvaluevalid;
			PropertyReadWrite = propertyreadwrite;
			PropertySessionRead = propertysessionread;
			PropertySessionWrite = propertysessionwrite;
			PropertyBlockCapacity = propertyblockcapacity;
			PropertyCurrentBlockSize = propertycurrentblocksize;
			PropertyCRC32 = propertyCRC32;
			PropertyCRC32Verify = propertyCRC32verify;
			PropertySetAddress = propertySetAddress;
			BlockOffset = blockoffset;
			ActualBulkTransferSize = actualbulktransfersize;
			EndBulkXferOffset = endbulkxferoffset;
			EndBulkXferCRC32 = endbulkxfercrc32;
			Response = response;
			BlockData = blockdata;
		}
	}
}
