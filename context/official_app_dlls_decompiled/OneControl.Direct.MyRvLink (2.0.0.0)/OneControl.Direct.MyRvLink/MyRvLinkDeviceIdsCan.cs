using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkDeviceIdsCan : MyRvLinkDevice, IMyRvLinkDeviceForLogicalDevice, IMyRvLinkDevice
	{
		private const string LogTag = "MyRvLinkDeviceIdsCan";

		public const MyRvLinkDeviceProtocol DeviceProtocolIdsCan = MyRvLinkDeviceProtocol.IdsCan;

		private MyRvLinkDeviceIdsCanMetadata? _metaData;

		public const byte DeviceEntryPayloadSize = 10;

		internal const int CanDeviceTypeIndex = 2;

		internal const int CanDeviceInstanceIndex = 3;

		internal const int CanProductIdIndex = 4;

		internal const int CanMacIndex = 6;

		public DEVICE_TYPE DeviceType { get; }

		public int DeviceInstance { get; }

		public PRODUCT_ID ProductId { get; }

		public MAC ProductMacAddress { get; }

		public byte RawDefaultCapability => MetaData?.RawDeviceCapability ?? 0;

		public MyRvLinkDeviceIdsCanMetadata? MetaData
		{
			get
			{
				return _metaData;
			}
			private set
			{
				_metaData = value;
				UpdateLogicalDeviceId();
			}
		}

		public ILogicalDeviceId? LogicalDeviceId { get; private set; }

		public override byte EncodeSize => 12;

		public MyRvLinkDeviceIdsCan(DEVICE_TYPE deviceType, int deviceInstance, PRODUCT_ID productId, MAC productMacAddress, MyRvLinkDeviceIdsCanMetadata? metaData = null)
			: base(MyRvLinkDeviceProtocol.IdsCan)
		{
			DeviceType = deviceType;
			DeviceInstance = deviceInstance;
			ProductId = productId;
			ProductMacAddress = productMacAddress;
			MetaData = metaData;
		}

		public void UpdateMetadata(MyRvLinkDeviceIdsCanMetadata metadata)
		{
			MetaData = metadata;
		}

		private void UpdateLogicalDeviceId()
		{
			LogicalDeviceId = ((MetaData == null) ? null : new LogicalDeviceId(MakeDeviceId(), ProductMacAddress));
		}

		private DEVICE_ID MakeDeviceId()
		{
			return new DEVICE_ID(ProductId, 0, DeviceType, DeviceInstance, MetaData?.FunctionName ?? FUNCTION_NAME.UNKNOWN, MetaData?.FunctionInstance ?? 0, RawDefaultCapability);
		}

		public override int EncodeIntoBuffer(byte[] buffer, int offset)
		{
			base.EncodeIntoBuffer(buffer, offset);
			buffer[2 + offset] = DeviceType;
			buffer[3 + offset] = (byte)DeviceInstance;
			buffer.SetValueUInt16(ProductId, 4 + offset);
			buffer.SetValueUInt48(ProductMacAddress, 6 + offset);
			return EncodeSize;
		}

		public static DEVICE_TYPE DecodeCanDeviceType(IReadOnlyList<byte> decodeBuffer)
		{
			return decodeBuffer[2];
		}

		public static int DecodeDeviceInstance(IReadOnlyList<byte> decodeBuffer)
		{
			return decodeBuffer[3];
		}

		public static PRODUCT_ID DecodeProductId(IReadOnlyList<byte> decodeBuffer)
		{
			return decodeBuffer.GetValueUInt16(4);
		}

		public static MAC DecodeProductMacAddress(IReadOnlyList<byte> decodeBuffer)
		{
			return new MAC((UInt48)decodeBuffer.GetValueUInt48(6));
		}

		public static MyRvLinkDeviceIdsCan Decode(IReadOnlyList<byte> buffer)
		{
			MyRvLinkDeviceProtocol myRvLinkDeviceProtocol = MyRvLinkDevice.DecodeDeviceProtocol(buffer);
			if (myRvLinkDeviceProtocol != MyRvLinkDeviceProtocol.IdsCan)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(35, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Invalid device protocol ");
				defaultInterpolatedStringHandler.AppendFormatted(myRvLinkDeviceProtocol);
				defaultInterpolatedStringHandler.AppendLiteral(", expected ");
				defaultInterpolatedStringHandler.AppendFormatted(MyRvLinkDeviceProtocol.IdsCan);
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), "buffer");
			}
			int num = MyRvLinkDevice.DecodePayloadSize(buffer);
			if (num != 10)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(40, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Invalid payload size of ");
				defaultInterpolatedStringHandler.AppendFormatted(num);
				defaultInterpolatedStringHandler.AppendLiteral(", expected ");
				defaultInterpolatedStringHandler.AppendFormatted((byte)10);
				defaultInterpolatedStringHandler.AppendLiteral(" for ");
				defaultInterpolatedStringHandler.AppendFormatted(MyRvLinkDeviceProtocol.IdsCan);
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), "buffer");
			}
			DEVICE_TYPE deviceType = DecodeCanDeviceType(buffer);
			int deviceInstance = DecodeDeviceInstance(buffer);
			PRODUCT_ID productId = DecodeProductId(buffer);
			MAC productMacAddress = DecodeProductMacAddress(buffer);
			return new MyRvLinkDeviceIdsCan(deviceType, deviceInstance, productId, productMacAddress);
		}

		public override string ToString()
		{
			MyRvLinkDeviceIdsCanMetadata metaData = MetaData;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(5, 6);
			defaultInterpolatedStringHandler.AppendFormatted(base.ToString());
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceType);
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceInstance);
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendFormatted(ProductId);
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendFormatted(ProductMacAddress);
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendFormatted(metaData?.ToString() ?? "NO META DATA");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
