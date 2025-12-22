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
	public class MyRvLinkDeviceIdsCanMetadata : MyRvLinkDeviceMetadata, IEquatable<object>
	{
		public const MyRvLinkDeviceProtocol DeviceProtocolIdsCan = MyRvLinkDeviceProtocol.IdsCan;

		public const byte MetadataEntryPayloadSize = 17;

		internal const int CanFunctionNameIndex = 2;

		internal const int CanFunctionInstanceIndex = 4;

		internal const int CanRawDeviceCapabilityIndex = 5;

		internal const int CanVersionIndex = 6;

		internal const int CanCircuitIdIndex = 7;

		internal const int CanSoftwarePartNumberIndex = 11;

		internal const int CanSoftwarePartNumberStringLength = 8;

		public FUNCTION_NAME FunctionName { get; }

		public int FunctionInstance { get; }

		public byte RawDeviceCapability { get; }

		public byte IdsCanVersion { get; }

		public uint CircuitId { get; }

		public string SoftwarePartNumber { get; }

		public override byte EncodeSize => 19;

		public MyRvLinkDeviceIdsCanMetadata(FUNCTION_NAME functionName, int functionInstance, byte rawDeviceCapability, byte idsCanVersion, uint circuitId, string softwarePartnumber)
			: base(MyRvLinkDeviceProtocol.IdsCan)
		{
			FunctionName = functionName;
			FunctionInstance = functionInstance;
			RawDeviceCapability = rawDeviceCapability;
			IdsCanVersion = idsCanVersion;
			CircuitId = circuitId;
			SoftwarePartNumber = softwarePartnumber;
		}

		public FUNCTION_CLASS PreferredFunctionClass(DEVICE_TYPE deviceType)
		{
			return deviceType.GetPreferredFunctionClass(FunctionName);
		}

		public FUNCTION_CLASS PreferredFunctionClass(MyRvLinkDeviceIdsCan idsCanPhysical)
		{
			return PreferredFunctionClass(idsCanPhysical.DeviceType);
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

		public override int EncodeIntoBuffer(byte[] buffer, int offset)
		{
			base.EncodeIntoBuffer(buffer, offset);
			buffer.SetValueUInt16(FunctionName, 2 + offset);
			buffer[4 + offset] = (byte)FunctionInstance;
			buffer[5 + offset] = RawDeviceCapability;
			buffer[6 + offset] = IdsCanVersion;
			buffer.SetValueUInt32(CircuitId, 7 + offset);
			Array.Clear(buffer, 11 + offset, 8);
			byte[] bytes = Encoding.ASCII.GetBytes(SoftwarePartNumber ?? string.Empty);
			Buffer.BlockCopy(bytes, 0, buffer, 11 + offset, Math.Min(bytes.Length, 8));
			return EncodeSize;
		}

		public static FUNCTION_NAME DecodeFunctionName(IReadOnlyList<byte> decodeBuffer)
		{
			return decodeBuffer.GetValueUInt16(2);
		}

		public static int DecodeFunctionInstance(IReadOnlyList<byte> decodeBuffer)
		{
			return decodeBuffer[4];
		}

		public static byte DecodeRawDeviceCapability(IReadOnlyList<byte> decodeBuffer)
		{
			return decodeBuffer[5];
		}

		public static byte DecodeIdsCanVersion(IReadOnlyList<byte> decodeBuffer)
		{
			return decodeBuffer[6];
		}

		public static uint DecodeCircuitNumber(IReadOnlyList<byte> decodeBuffer)
		{
			return decodeBuffer.GetValueUInt32(7);
		}

		public static string DecodeSoftwarePartNumber(IReadOnlyList<byte> decodeBuffer)
		{
			byte[] array = new byte[8];
			int i;
			for (i = 11; i < 19 && i < decodeBuffer.Count && decodeBuffer[i] != 0; i++)
			{
				array[i - 11] = decodeBuffer[i];
			}
			int num = i - 11;
			if (num <= 0)
			{
				return string.Empty;
			}
			return Encoding.UTF8.GetString(array, 0, num);
		}

		public static MyRvLinkDeviceIdsCanMetadata Decode(IReadOnlyList<byte> buffer)
		{
			MyRvLinkDeviceProtocol myRvLinkDeviceProtocol = MyRvLinkDeviceMetadata.DecodeDeviceProtocol(buffer);
			if (myRvLinkDeviceProtocol != MyRvLinkDeviceProtocol.IdsCan)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(35, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Invalid device protocol ");
				defaultInterpolatedStringHandler.AppendFormatted(myRvLinkDeviceProtocol);
				defaultInterpolatedStringHandler.AppendLiteral(", expected ");
				defaultInterpolatedStringHandler.AppendFormatted(MyRvLinkDeviceProtocol.IdsCan);
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), "buffer");
			}
			int num = MyRvLinkDeviceMetadata.DecodePayloadSize(buffer);
			if (num != 17)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(40, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Invalid payload size of ");
				defaultInterpolatedStringHandler.AppendFormatted(num);
				defaultInterpolatedStringHandler.AppendLiteral(", expected ");
				defaultInterpolatedStringHandler.AppendFormatted((byte)17);
				defaultInterpolatedStringHandler.AppendLiteral(" for ");
				defaultInterpolatedStringHandler.AppendFormatted(MyRvLinkDeviceProtocol.IdsCan);
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), "buffer");
			}
			FUNCTION_NAME functionName = DecodeFunctionName(buffer);
			int functionInstance = DecodeFunctionInstance(buffer);
			byte rawDeviceCapability = DecodeRawDeviceCapability(buffer);
			byte idsCanVersion = DecodeIdsCanVersion(buffer);
			uint circuitId = DecodeCircuitNumber(buffer);
			string softwarePartnumber = DecodeSoftwarePartNumber(buffer);
			return new MyRvLinkDeviceIdsCanMetadata(functionName, functionInstance, rawDeviceCapability, idsCanVersion, circuitId, softwarePartnumber);
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
