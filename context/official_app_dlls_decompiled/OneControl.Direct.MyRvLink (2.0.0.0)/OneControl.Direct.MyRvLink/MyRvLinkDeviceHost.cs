using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkDeviceHost : MyRvLinkDevice, IMyRvLinkDeviceForLogicalDevice, IMyRvLinkDevice
	{
		private const string LogTag = "MyRvLinkDeviceHost";

		public const MyRvLinkDeviceProtocol DeviceProtocolHost = MyRvLinkDeviceProtocol.Host;

		public const int DefaultProxyDeviceIdInstance = 15;

		public const int MacSize = 6;

		public readonly bool HasIdsCanStyleMetadata;

		private static readonly MAC DefaultHostDeviceIdMacLegacy = new MAC((UInt48)863642096980L);

		private MyRvLinkDeviceHostMetadata? _metaData;

		private const byte DeviceEntryPayloadSizeWithoutIdsCanData = 0;

		private const byte DeviceEntryPayloadSizeWithIdsCanData = 10;

		public DEVICE_TYPE DeviceType { get; }

		public int DeviceInstance { get; }

		public PRODUCT_ID ProductId { get; }

		public MAC ProductMacAddress { get; }

		public byte RawDefaultCapability => MetaData?.RawDeviceCapability ?? 0;

		public static MAC DefaultHostDeviceIdMac { get; private set; } = DefaultHostDeviceIdMacLegacy;


		public MyRvLinkDeviceHostMetadata? MetaData
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

		private byte DeviceEntryPayloadSize
		{
			get
			{
				if (!HasIdsCanStyleMetadata)
				{
					return 0;
				}
				return 10;
			}
		}

		public override byte EncodeSize => (byte)(2 + DeviceEntryPayloadSize);

		public static void SetDefaultHostDeviceIdMac(Guid? uniqueId)
		{
			if (!uniqueId.HasValue)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(29, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Using hard coded default MAC ");
				defaultInterpolatedStringHandler.AppendFormatted(DefaultHostDeviceIdMac);
				TaggedLog.Warning("MyRvLinkDeviceHost", defaultInterpolatedStringHandler.ToStringAndClear());
				DefaultHostDeviceIdMac = DefaultHostDeviceIdMacLegacy;
				return;
			}
			byte[] array = uniqueId.Value.ToByteArray();
			if (array.Length != 16)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(44, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Using hard coded default MAC ");
				defaultInterpolatedStringHandler.AppendFormatted(DefaultHostDeviceIdMac);
				defaultInterpolatedStringHandler.AppendLiteral(", invalid Guid ");
				defaultInterpolatedStringHandler.AppendFormatted(uniqueId);
				TaggedLog.Warning("MyRvLinkDeviceHost", defaultInterpolatedStringHandler.ToStringAndClear());
				DefaultHostDeviceIdMac = DefaultHostDeviceIdMacLegacy;
				return;
			}
			try
			{
				MAC mAC = new MAC((UInt48)array.GetValueUInt48(10));
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Host using generated MAC of ");
				defaultInterpolatedStringHandler.AppendFormatted(mAC);
				defaultInterpolatedStringHandler.AppendLiteral(" from ");
				defaultInterpolatedStringHandler.AppendFormatted(uniqueId);
				TaggedLog.Information("MyRvLinkDeviceHost", defaultInterpolatedStringHandler.ToStringAndClear());
				DefaultHostDeviceIdMac = mAC;
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(53, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Using hard coded default MAC ");
				defaultInterpolatedStringHandler.AppendFormatted(DefaultHostDeviceIdMac);
				defaultInterpolatedStringHandler.AppendLiteral(", invalid Guid ");
				defaultInterpolatedStringHandler.AppendFormatted(uniqueId);
				defaultInterpolatedStringHandler.AppendLiteral(" because ");
				defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
				TaggedLog.Warning("MyRvLinkDeviceHost", defaultInterpolatedStringHandler.ToStringAndClear());
				DefaultHostDeviceIdMac = DefaultHostDeviceIdMacLegacy;
			}
		}

		public MyRvLinkDeviceHost()
			: this((byte)36, 15, PRODUCT_ID.BLUETOOTH_GATEWAY_DAUGHTER_BOARD_RVLINK_ESP32_PROGRAMMED_PCBA, DefaultHostDeviceIdMac)
		{
			HasIdsCanStyleMetadata = false;
		}

		private MyRvLinkDeviceHost(DEVICE_TYPE deviceType, int deviceInstance, PRODUCT_ID productId, MAC productMacAddress, MyRvLinkDeviceHostMetadata? metaData = null)
			: base(MyRvLinkDeviceProtocol.Host)
		{
			HasIdsCanStyleMetadata = true;
			DeviceType = deviceType;
			DeviceInstance = deviceInstance;
			ProductId = productId;
			ProductMacAddress = productMacAddress;
			MetaData = metaData;
		}

		public void UpdateMetadata(MyRvLinkDeviceHostMetadata metadata)
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
			if (HasIdsCanStyleMetadata)
			{
				buffer[2 + offset] = DeviceType;
				buffer[3 + offset] = (byte)DeviceInstance;
				buffer.SetValueUInt16(ProductId, 4 + offset);
				buffer.SetValueUInt48(ProductMacAddress, 6 + offset);
			}
			return EncodeSize;
		}

		public static MyRvLinkDeviceHost Decode(IReadOnlyList<byte> buffer)
		{
			MyRvLinkDeviceProtocol myRvLinkDeviceProtocol = MyRvLinkDevice.DecodeDeviceProtocol(buffer);
			if (myRvLinkDeviceProtocol != MyRvLinkDeviceProtocol.Host)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(35, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Invalid device protocol ");
				defaultInterpolatedStringHandler.AppendFormatted(myRvLinkDeviceProtocol);
				defaultInterpolatedStringHandler.AppendLiteral(", expected ");
				defaultInterpolatedStringHandler.AppendFormatted(MyRvLinkDeviceProtocol.Host);
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), "buffer");
			}
			int num = MyRvLinkDevice.DecodePayloadSize(buffer);
			switch (num)
			{
			case 0:
				return new MyRvLinkDeviceHost();
			case 10:
			{
				DEVICE_TYPE deviceType = MyRvLinkDeviceIdsCan.DecodeCanDeviceType(buffer);
				int deviceInstance = MyRvLinkDeviceIdsCan.DecodeDeviceInstance(buffer);
				PRODUCT_ID productId = MyRvLinkDeviceIdsCan.DecodeProductId(buffer);
				MAC productMacAddress = MyRvLinkDeviceIdsCan.DecodeProductMacAddress(buffer);
				return new MyRvLinkDeviceHost(deviceType, deviceInstance, productId, productMacAddress);
			}
			default:
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(44, 4);
				defaultInterpolatedStringHandler.AppendLiteral("Invalid payload size of ");
				defaultInterpolatedStringHandler.AppendFormatted(num);
				defaultInterpolatedStringHandler.AppendLiteral(", expected ");
				defaultInterpolatedStringHandler.AppendFormatted((byte)0);
				defaultInterpolatedStringHandler.AppendLiteral(" or ");
				defaultInterpolatedStringHandler.AppendFormatted((byte)10);
				defaultInterpolatedStringHandler.AppendLiteral(" for ");
				defaultInterpolatedStringHandler.AppendFormatted(MyRvLinkDeviceProtocol.Host);
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), "buffer");
			}
			}
		}

		public override string ToString()
		{
			MyRvLinkDeviceHostMetadata metaData = MetaData;
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
