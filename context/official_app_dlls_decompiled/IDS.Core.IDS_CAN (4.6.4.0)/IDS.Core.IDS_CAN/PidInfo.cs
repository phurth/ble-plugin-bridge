namespace IDS.Core.IDS_CAN
{
	public class PidInfo
	{
		public readonly IDevice Device;

		public readonly PID ID;

		public ushort PID_Address;

		public readonly byte Flags;

		public bool IsReadable => (Flags & 1) != 0;

		public bool IsWritable => (Flags & 2) != 0;

		public bool IsNonVolatile => (Flags & 4) != 0;

		public bool IsWithAddress => (Flags & 8) != 0;

		public string Name => ID.Name;

		public PidInfo(IDevice device, PID id, byte flags)
		{
			Device = device;
			ID = id;
			Flags = flags;
		}
	}
}
