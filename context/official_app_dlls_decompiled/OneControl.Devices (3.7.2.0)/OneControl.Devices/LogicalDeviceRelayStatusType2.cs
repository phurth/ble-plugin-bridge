using System;
using System.Collections.Generic;
using System.Text;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public abstract class LogicalDeviceRelayStatusType2<TOutputState> : LogicalDeviceStatusPacketMutable where TOutputState : struct, IConvertible
	{
		private const int MinimumStatusPacketSize = 6;

		public const ushort CurrentDrawAmpsNotSupportedValue = ushort.MaxValue;

		public const int StatusByteIndex = 0;

		public const int PositionStartingIndex = 1;

		public const int CurrentDrawAmpsStartingIndex = 2;

		public const int DtcStartingIndex = 4;

		public const byte OutputStateBitMask = 15;

		public const BasicBitMask UserClearRequiredLatchBitMask = BasicBitMask.BitMask0X20;

		protected byte RawOutputState => (byte)(base.Data[0] & 0xFu);

		public abstract TOutputState State { get; }

		public bool IsValid => base.HasData;

		public bool IsFaulted
		{
			get
			{
				if (UserClearRequired)
				{
					return UserMessageDtc != DTC_ID.UNKNOWN;
				}
				return false;
			}
		}

		public bool UserClearRequired => GetBit(BasicBitMask.BitMask0X20, 0);

		public DTC_ID UserMessageDtc => (DTC_ID)((base.Data != null && base.Data.Length >= 6) ? GetUInt16(4u) : 0);

		public bool IsPositionKnown
		{
			get
			{
				if (IsValid)
				{
					return Position <= 100;
				}
				return false;
			}
		}

		public byte Position => base.Data[1];

		public bool IsCurrentDrawAmpsKnown
		{
			get
			{
				if (IsValid)
				{
					return FixedPointUnsignedBigEndian8X8.ToFixedPoint(base.Data, 2u) != ushort.MaxValue;
				}
				return false;
			}
		}

		public float CurrentDrawAmps => FixedPointUnsignedBigEndian8X8.ToFloat(base.Data, 2u);

		public void SetState(TOutputState state)
		{
			SetByte(15, Convert.ToByte(state), 0);
		}

		public bool SetFault(bool isFaulted)
		{
			return false;
		}

		public bool SetUserClearRequired(bool disabled)
		{
			SetBit(BasicBitMask.BitMask0X20, disabled, 0);
			return true;
		}

		public bool SetUserMessageDtc(DTC_ID dtc)
		{
			SetUInt16((ushort)dtc, 4);
			return true;
		}

		public bool SetPosition(byte position)
		{
			base.Data[1] = position;
			return true;
		}

		public bool SetCurrentDrawAmps(float voltage)
		{
			SetFixedPoint(FixedPointType.UnsignedBigEndian8x8, voltage, 2u);
			return true;
		}

		protected LogicalDeviceRelayStatusType2(uint minSize = 6u)
			: base(minSize)
		{
		}

		public static bool IsSignificantlyDifferent(IReadOnlyList<byte> statusData1, IReadOnlyList<byte> statusData2)
		{
			try
			{
				if (statusData1[0] != statusData2[0] || statusData1[1] != statusData2[1] || statusData1[4] != statusData2[4] || statusData1[4] != statusData2[4])
				{
					return true;
				}
			}
			catch
			{
				return true;
			}
			return false;
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder($"Relay Status: {State}");
			if (UserMessageDtc != 0)
			{
				stringBuilder.Append($", UserMessageDTC: {UserMessageDtc}");
			}
			if (UserClearRequired)
			{
				stringBuilder.Append(IsFaulted ? ", UserClearRequired & Faulted" : ", UserClearRequired");
			}
			if (IsPositionKnown)
			{
				stringBuilder.Append($", Position: {Position}%");
			}
			if (IsCurrentDrawAmpsKnown)
			{
				stringBuilder.Append($", CurrentDraw {CurrentDrawAmps:F2}A");
			}
			stringBuilder.Append(": " + base.Data.DebugDump());
			return stringBuilder.ToString();
		}
	}
}
