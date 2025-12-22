using IDS.Core.Types;

namespace IDS.Core.IDS_CAN.Devices
{
	public class RELAY_TYPE_2_SIM_INTERFACE : LocalDevice
	{
		public enum PHYSICAL_SWITCH_TYPE : byte
		{
			NO_PHYSICAL_SWITCH,
			DIMMABLE_SWITCH,
			TOGGLE_SWITCH,
			MOMENTARY_SWITCH
		}

		public RELAY_TYPE_2_STATUS_PARAMS mStatusMessageParams;

		private bool _pidsAdded;

		private bool _supports_SoftwareConfigurableFuse;

		private bool _supports_CoarsePosition;

		private bool _supports_FinePosition;

		private bool _supports_Homing;

		private byte _physicalSwitchType;

		private bool _outputDisabledLatch;

		public RELAY_TYPE_2_OUTPUT_STATE OutputState
		{
			get
			{
				return (RELAY_TYPE_2_OUTPUT_STATE)(mStatusMessageParams._OutputState & 0xFu);
			}
			set
			{
				mStatusMessageParams._OutputState &= 240;
				mStatusMessageParams._OutputState |= (byte)(value & (RELAY_TYPE_2_OUTPUT_STATE)15);
				base.DeviceStatus = mStatusMessageParams.GetPayload();
			}
		}

		public bool OnCommandAllowed
		{
			get
			{
				return (mStatusMessageParams._OutputState & 0x80) != 0;
			}
			set
			{
				if (value)
				{
					mStatusMessageParams._OutputState |= 128;
				}
				else
				{
					mStatusMessageParams._OutputState &= 127;
				}
				base.DeviceStatus = mStatusMessageParams.GetPayload();
			}
		}

		public bool ForwardCommandAllowed
		{
			get
			{
				return (mStatusMessageParams._OutputState & 0x80) != 0;
			}
			set
			{
				if (value)
				{
					mStatusMessageParams._OutputState |= 128;
				}
				else
				{
					mStatusMessageParams._OutputState &= 127;
				}
				base.DeviceStatus = mStatusMessageParams.GetPayload();
			}
		}

		public bool ReverseCommandAllowed
		{
			get
			{
				return (mStatusMessageParams._OutputState & 0x40) != 0;
			}
			set
			{
				if (ReverseCommandAllowed != value)
				{
					if (value)
					{
						mStatusMessageParams._OutputState |= 64;
					}
					else
					{
						mStatusMessageParams._OutputState &= 191;
					}
					base.DeviceStatus = mStatusMessageParams.GetPayload();
				}
			}
		}

		public bool UserClearRequired => (mStatusMessageParams._OutputState & 0x20) != 0;

		public byte OutputPositionPct
		{
			get
			{
				return mStatusMessageParams.OutputPositionPct;
			}
			set
			{
				mStatusMessageParams.OutputPositionPct = value;
				base.DeviceStatus = mStatusMessageParams.GetPayload();
			}
		}

		public float CurrentDraw
		{
			get
			{
				return (int)mStatusMessageParams.CurrentDraw;
			}
			set
			{
				mStatusMessageParams.CurrentDraw = (ushort)((double)value * 256.0 + 0.5);
				base.DeviceStatus = mStatusMessageParams.GetPayload();
			}
		}

		public ushort UserMessage
		{
			get
			{
				return mStatusMessageParams.UserMessage;
			}
			set
			{
				mStatusMessageParams.UserMessage = value;
				base.DeviceStatus = mStatusMessageParams.GetPayload();
			}
		}

		public bool Supports_SoftwareConfigurableFuse
		{
			get
			{
				return _supports_SoftwareConfigurableFuse;
			}
			set
			{
				if (_supports_SoftwareConfigurableFuse == value)
				{
					return;
				}
				if (value && !_pidsAdded)
				{
					AddPID(PID.SOFTWARE_FUSE_RATING_AMPS, () => SOFTWARE_FUSE_RATING_AMPS, delegate(UInt48 arg)
					{
						SOFTWARE_FUSE_RATING_AMPS = (uint)arg;
					});
					AddPID(PID.SOFTWARE_FUSE_MAX_RATING_AMPS, () => SOFTWARE_FUSE_MAX_RATING_AMPS, delegate(UInt48 arg)
					{
						SOFTWARE_FUSE_MAX_RATING_AMPS = (uint)arg;
					});
					_pidsAdded = true;
				}
				_supports_SoftwareConfigurableFuse = value;
				UpdateDeviceCapabilities();
			}
		}

		public bool Supports_CoarsePosition
		{
			get
			{
				return _supports_CoarsePosition;
			}
			set
			{
				if (_supports_CoarsePosition != value)
				{
					_supports_CoarsePosition = value;
					UpdateDeviceCapabilities();
				}
			}
		}

		public bool Supports_FinePosition
		{
			get
			{
				return _supports_FinePosition;
			}
			set
			{
				if (_supports_FinePosition != value)
				{
					_supports_FinePosition = value;
					UpdateDeviceCapabilities();
				}
			}
		}

		public bool Supports_Homing
		{
			get
			{
				return _supports_Homing;
			}
			set
			{
				if (_supports_Homing != value)
				{
					_supports_Homing = value;
					UpdateDeviceCapabilities();
				}
			}
		}

		public byte PhysicalSwitchType
		{
			get
			{
				return _physicalSwitchType;
			}
			set
			{
				if (_physicalSwitchType != value)
				{
					_physicalSwitchType = value;
					UpdateDeviceCapabilities();
				}
			}
		}

		public bool OutputDisabledLatch
		{
			get
			{
				return _outputDisabledLatch;
			}
			set
			{
				if (_outputDisabledLatch != value)
				{
					_outputDisabledLatch = value;
				}
			}
		}

		public uint SOFTWARE_FUSE_RATING_AMPS { get; set; }

		public uint SOFTWARE_FUSE_MAX_RATING_AMPS { get; set; }

		public byte EXTENDEDDEVICECAPABILITIES { get; set; }

		public byte CLOUDCAPABILITIES { get; set; }

		public bool NotAcceptingCommands
		{
			get
			{
				return base.IsNotAcceptingCommands;
			}
			set
			{
				base.IsNotAcceptingCommands = value;
			}
		}

		public RELAY_TYPE_2_SIM_INTERFACE(IAdapter adapter, string software_part_number, PRODUCT_ID product_id, IDS_CAN_VERSION_NUMBER version, DEVICE_ID deviceId, LOCAL_DEVICE_OPTIONS options, MAC mac = null)
			: base(new LocalProduct(adapter, mac, product_id, version, software_part_number), deviceId.DeviceType, 0, deviceId.FunctionName, deviceId.FunctionInstance, deviceId.DeviceCapabilities, options)
		{
			mStatusMessageParams = new RELAY_TYPE_2_STATUS_PARAMS();
			base.DeviceStatus = mStatusMessageParams.GetPayload();
		}

		protected virtual void Init()
		{
		}

		protected virtual void UpdateDeviceCapabilities()
		{
		}

		protected virtual void UpdateStatus()
		{
		}
	}
}
