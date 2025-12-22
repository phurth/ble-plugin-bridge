using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkDeviceHostMetadata : MyRvLinkDeviceMetadata, IEquatable<object>
	{
		public const MyRvLinkDeviceProtocol DeviceProtocolHost = MyRvLinkDeviceProtocol.Host;

		public const int DefaultProxyFunctionInstance = 15;

		public readonly bool HasIdsCanStyleMetadata;

		private const byte DeviceEntryPayloadSizeWithoutIdsCanMetadata = 0;

		private const byte DeviceEntryPayloadSizeWithIdsCanMetadata = 17;

		public FUNCTION_NAME FunctionName { get; }

		public int FunctionInstance { get; }

		public byte RawDeviceCapability { get; }

		public byte IdsCanVersion { get; }

		public uint CircuitId { get; }

		public string SoftwarePartNumber { get; }

		private byte DeviceEntryPayloadSize
		{
			get
			{
				if (!HasIdsCanStyleMetadata)
				{
					return 0;
				}
				return 17;
			}
		}

		public override byte EncodeSize => (byte)(2 + DeviceEntryPayloadSize);

		public FUNCTION_CLASS PreferredFunctionClass(DEVICE_TYPE deviceType)
		{
			return deviceType.GetPreferredFunctionClass(FunctionName);
		}

		public MyRvLinkDeviceHostMetadata()
			: base(MyRvLinkDeviceProtocol.Host)
		{
			HasIdsCanStyleMetadata = false;
			FunctionName = (ushort)323;
			FunctionInstance = 15;
			RawDeviceCapability = 0;
			IdsCanVersion = IDS_CAN_VERSION_NUMBER.UNKNOWN;
			CircuitId = 0u;
			SoftwarePartNumber = string.Empty;
		}

		private MyRvLinkDeviceHostMetadata(FUNCTION_NAME functionName, int functionInstance, byte rawDeviceCapability, byte idsCanVersion, uint circuitId, string softwarePartnumber)
			: base(MyRvLinkDeviceProtocol.Host)
		{
			HasIdsCanStyleMetadata = true;
			FunctionName = functionName;
			FunctionInstance = functionInstance;
			RawDeviceCapability = rawDeviceCapability;
			IdsCanVersion = idsCanVersion;
			CircuitId = circuitId;
			SoftwarePartNumber = softwarePartnumber;
		}

		public override int EncodeIntoBuffer(byte[] buffer, int offset)
		{
			base.EncodeIntoBuffer(buffer, offset);
			if (HasIdsCanStyleMetadata)
			{
				buffer.SetValueUInt16(FunctionName, 2 + offset);
				buffer[4 + offset] = (byte)FunctionInstance;
				buffer[5 + offset] = RawDeviceCapability;
				buffer[6 + offset] = IdsCanVersion;
				buffer.SetValueUInt32(CircuitId, 7 + offset);
				Array.Clear(buffer, 11 + offset, 8);
				byte[] bytes = Encoding.ASCII.GetBytes(SoftwarePartNumber ?? string.Empty);
				Buffer.BlockCopy(bytes, 0, buffer, 11 + offset, Math.Min(bytes.Length, 8));
			}
			return EncodeSize;
		}

		public static MyRvLinkDeviceHostMetadata Decode(IReadOnlyList<byte> buffer)
		{
			MyRvLinkDeviceProtocol myRvLinkDeviceProtocol = MyRvLinkDeviceMetadata.DecodeDeviceProtocol(buffer);
			if (myRvLinkDeviceProtocol != MyRvLinkDeviceProtocol.Host)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(35, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Invalid device protocol ");
				defaultInterpolatedStringHandler.AppendFormatted(myRvLinkDeviceProtocol);
				defaultInterpolatedStringHandler.AppendLiteral(", expected ");
				defaultInterpolatedStringHandler.AppendFormatted(MyRvLinkDeviceProtocol.Host);
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), "buffer");
			}
			int num = MyRvLinkDeviceMetadata.DecodePayloadSize(buffer);
			switch (num)
			{
			case 0:
				return new MyRvLinkDeviceHostMetadata();
			case 17:
			{
				FUNCTION_NAME functionName = MyRvLinkDeviceIdsCanMetadata.DecodeFunctionName(buffer);
				int functionInstance = MyRvLinkDeviceIdsCanMetadata.DecodeFunctionInstance(buffer);
				byte rawDeviceCapability = MyRvLinkDeviceIdsCanMetadata.DecodeRawDeviceCapability(buffer);
				byte idsCanVersion = MyRvLinkDeviceIdsCanMetadata.DecodeIdsCanVersion(buffer);
				uint circuitId = MyRvLinkDeviceIdsCanMetadata.DecodeCircuitNumber(buffer);
				string softwarePartnumber = MyRvLinkDeviceIdsCanMetadata.DecodeSoftwarePartNumber(buffer);
				return new MyRvLinkDeviceHostMetadata(functionName, functionInstance, rawDeviceCapability, idsCanVersion, circuitId, softwarePartnumber);
			}
			default:
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(44, 4);
				defaultInterpolatedStringHandler.AppendLiteral("Invalid payload size of ");
				defaultInterpolatedStringHandler.AppendFormatted(num);
				defaultInterpolatedStringHandler.AppendLiteral(", expected ");
				defaultInterpolatedStringHandler.AppendFormatted((byte)0);
				defaultInterpolatedStringHandler.AppendLiteral(" or ");
				defaultInterpolatedStringHandler.AppendFormatted((byte)17);
				defaultInterpolatedStringHandler.AppendLiteral(" for ");
				defaultInterpolatedStringHandler.AppendFormatted((byte)17);
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), "buffer");
			}
			}
		}

		public override bool Equals(object obj)
		{
			if (obj is MyRvLinkDeviceIdsCanMetadata myRvLinkDeviceIdsCanMetadata && EqualityComparer<FUNCTION_NAME>.Default.Equals(FunctionName, myRvLinkDeviceIdsCanMetadata.FunctionName) && FunctionInstance == myRvLinkDeviceIdsCanMetadata.FunctionInstance && RawDeviceCapability == myRvLinkDeviceIdsCanMetadata.RawDeviceCapability && IdsCanVersion == myRvLinkDeviceIdsCanMetadata.IdsCanVersion)
			{
				return string.Compare(SoftwarePartNumber, myRvLinkDeviceIdsCanMetadata.SoftwarePartNumber, StringComparison.Ordinal) == 0;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return 17.Hash(FunctionInstance).Hash(FunctionName).Hash(RawDeviceCapability);
		}

		public override string ToString()
		{
			IDS_CAN_VERSION_NUMBER iDS_CAN_VERSION_NUMBER = IdsCanVersion;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(69, 9);
			defaultInterpolatedStringHandler.AppendFormatted(base.ToString());
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendFormatted(FunctionName.Name);
			defaultInterpolatedStringHandler.AppendLiteral("(0x");
			defaultInterpolatedStringHandler.AppendFormatted((int)(ushort)FunctionName, "4X");
			defaultInterpolatedStringHandler.AppendLiteral(") ");
			defaultInterpolatedStringHandler.AppendFormatted(FunctionInstance);
			defaultInterpolatedStringHandler.AppendLiteral(" Capability: ");
			defaultInterpolatedStringHandler.AppendFormatted(RawDeviceCapability, "X1");
			defaultInterpolatedStringHandler.AppendLiteral(" CanVersion:");
			defaultInterpolatedStringHandler.AppendFormatted(iDS_CAN_VERSION_NUMBER);
			defaultInterpolatedStringHandler.AppendLiteral("(");
			defaultInterpolatedStringHandler.AppendFormatted(IdsCanVersion, "X");
			defaultInterpolatedStringHandler.AppendLiteral(") CircuitId: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(CircuitId, "X");
			defaultInterpolatedStringHandler.AppendLiteral(" SoftwarePartNumber:`");
			defaultInterpolatedStringHandler.AppendFormatted(SoftwarePartNumber);
			defaultInterpolatedStringHandler.AppendLiteral("`");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
