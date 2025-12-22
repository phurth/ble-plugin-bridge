namespace ids.portable.ble.ScanResults
{
	public readonly struct BleGatewayInfo
	{
		public enum GatewayVersion
		{
			Unknown,
			V1,
			V2,
			V2_D
		}

		public int PartNumber { get; }

		public char MinRev { get; }

		public GatewayVersion Version { get; }

		public BleGatewayInfo(int partNumber, char minRev, GatewayVersion version)
		{
			PartNumber = partNumber;
			MinRev = minRev;
			Version = version;
		}
	}
}
