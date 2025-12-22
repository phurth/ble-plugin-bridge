using System.Runtime.CompilerServices;

namespace IDS.Core.IDS_CAN.Devices
{
	public class RELAY_TYPE_2_STATUS_PARAMS : IDeviceStatusParams
	{
		private CAN.PAYLOAD payload;

		private byte mOutputState;

		private byte mOutputPositionPct;

		private ushort mCurrentDraw;

		private ushort mUserMessage;

		[DeviceDisplay("Output State")]
		public string OutputDisplay
		{
			get
			{
				string text = ((RELAY_TYPE_2_OUTPUT_STATE)(mOutputState & 0xFu)).ToString();
				if ((mOutputState & 0x20u) != 0)
				{
					text += "\nUSER_CLEAR_REQUIRED";
				}
				if ((mOutputState & 0x40) == 0)
				{
					text += "\nREVERSE_COMMAND_NOT_ALLOWED";
				}
				if ((mOutputState & 0x80) == 0)
				{
					text += "\nFORWARD/ON_COMMAND_NOT_ALLOWED";
				}
				return text ?? "";
			}
		}

		public byte _OutputState
		{
			get
			{
				return mOutputState;
			}
			set
			{
				if (mOutputState != value)
				{
					mOutputState = value;
					payload[0] = value;
				}
			}
		}

		[DeviceDisplay("Output Position")]
		public string OutputPositionDisplay
		{
			get
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(1, 1);
				defaultInterpolatedStringHandler.AppendFormatted(mOutputPositionPct);
				defaultInterpolatedStringHandler.AppendLiteral("%");
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
		}

		public byte OutputPositionPct
		{
			get
			{
				return mOutputPositionPct;
			}
			set
			{
				mOutputPositionPct = value;
				payload[1] = value;
			}
		}

		[DeviceDisplay("Current Draw")]
		public string CurrentDrawDisplay
		{
			get
			{
				float num = (float)(int)mCurrentDraw / 256f;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 1);
				defaultInterpolatedStringHandler.AppendFormatted(num, "0.##");
				defaultInterpolatedStringHandler.AppendLiteral(" A");
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
		}

		public ushort CurrentDraw
		{
			get
			{
				return mCurrentDraw;
			}
			set
			{
				mCurrentDraw = value;
				payload[2] = (byte)(value >> 8);
				payload[3] = (byte)(value & 0xFFu);
			}
		}

		[DeviceDisplay("User Message")]
		public string UserMessageDisplay
		{
			get
			{
				DTC_ID dTC_ID = (DTC_ID)mUserMessage;
				return dTC_ID.ToString();
			}
		}

		public ushort UserMessage
		{
			get
			{
				return mUserMessage;
			}
			set
			{
				mUserMessage = value;
				payload[4] = (byte)(value >> 8);
				payload[5] = (byte)(value & 0xFFu);
			}
		}

		public RELAY_TYPE_2_STATUS_PARAMS()
		{
			payload = new CAN.PAYLOAD(6);
			_OutputState = 0;
			OutputPositionPct = 0;
			CurrentDraw = 0;
			UserMessage = 0;
		}

		public CAN.PAYLOAD GetPayload()
		{
			return payload;
		}

		public void SetPayload(CAN.PAYLOAD pl)
		{
			_OutputState = pl[0];
			OutputPositionPct = pl[1];
			CurrentDraw = (ushort)((pl[2] << 8) + pl[3]);
			UserMessage = (ushort)((pl[4] << 8) + pl[5]);
		}
	}
}
