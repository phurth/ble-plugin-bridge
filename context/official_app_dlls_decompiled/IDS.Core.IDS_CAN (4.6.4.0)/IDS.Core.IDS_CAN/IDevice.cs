namespace IDS.Core.IDS_CAN
{
	public interface IDevice : IBusEndpoint, IUniqueDeviceInfo, IUniqueProductInfo
	{
		IProduct Product { get; }

		IDS_CAN_VERSION_NUMBER ProtocolVersion { get; }

		NETWORK_STATUS NetworkStatus { get; }

		CIRCUIT_ID CircuitID { get; }

		CAN.PAYLOAD DeviceStatus { get; }

		byte ProductInstance { get; }

		FUNCTION_NAME FunctionName { get; }

		int FunctionInstance { get; }

		byte? DeviceCapabilities { get; }

		string SoftwarePartNumber { get; }

		ITextConsole TextConsole { get; }
	}
}
