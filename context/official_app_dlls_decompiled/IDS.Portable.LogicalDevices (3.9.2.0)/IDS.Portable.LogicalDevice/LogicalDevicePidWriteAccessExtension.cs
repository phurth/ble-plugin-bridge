using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public static class LogicalDevicePidWriteAccessExtension
	{
		public static SESSION_ID ToIdsCanSessionId(this LogicalDeviceSessionType access)
		{
			return access switch
			{
				LogicalDeviceSessionType.Manufacturing => (ushort)1, 
				LogicalDeviceSessionType.Diagnostic => (ushort)2, 
				LogicalDeviceSessionType.Reprogramming => (ushort)3, 
				LogicalDeviceSessionType.RemoteControl => (ushort)4, 
				LogicalDeviceSessionType.DataAcquisition => (ushort)5, 
				_ => SESSION_ID.UNKNOWN, 
			};
		}
	}
}
