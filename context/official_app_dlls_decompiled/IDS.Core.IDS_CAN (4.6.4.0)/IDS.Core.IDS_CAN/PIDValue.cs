namespace IDS.Core.IDS_CAN
{
	public struct PIDValue
	{
		public IRemoteDevice Device { get; private set; }

		public PID ID { get; private set; }

		public bool IsValueValid { get; private set; }

		public ulong Value { get; private set; }

		public uint Data { get; private set; }

		public ushort Address { get; private set; }

		public string ValueString
		{
			get
			{
				if (!IsValueValid)
				{
					return "UNKNOWN";
				}
				return ID.FormatValue(Value);
			}
		}

		internal PIDValue(IRemoteDevice device, PID id)
		{
			Device = device;
			ID = id;
			IsValueValid = false;
			Value = 0uL;
			Address = 0;
			Data = 0u;
		}

		internal PIDValue(IRemoteDevice device, PID id, ulong value, ushort address, uint data)
		{
			Device = device;
			ID = id;
			IsValueValid = true;
			Value = value;
			Address = address;
			Data = data;
		}
	}
}
