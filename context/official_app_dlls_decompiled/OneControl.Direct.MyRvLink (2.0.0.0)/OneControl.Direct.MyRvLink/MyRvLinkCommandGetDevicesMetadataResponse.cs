using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandGetDevicesMetadataResponse : MyRvLinkCommandResponseSuccess
	{
		public const string LogTag = "MyRvLinkCommandGetDevicesMetadataResponse";

		private List<IMyRvLinkDeviceMetadata>? _devicesMetadata;

		protected const int ExtendedDataHeaderSize = 3;

		protected const int DeviceTableIdIndex = 0;

		protected const int StartingDeviceIdIndex = 1;

		protected const int DeviceCountIndex = 2;

		protected override int MinExtendedDataLength => 3;

		public byte DeviceTableId => base.ExtendedData[0];

		public byte StartDeviceId => base.ExtendedData[1];

		public byte DeviceCount => base.ExtendedData[2];

		public IReadOnlyList<IMyRvLinkDeviceMetadata> DevicesMetadata => _devicesMetadata ?? (_devicesMetadata = DecodeDevicesMetadata());

		public MyRvLinkCommandGetDevicesMetadataResponse(ushort clientCommandId, byte deviceTableId, byte startingDeviceId, IReadOnlyList<IMyRvLinkDeviceMetadata> devicesMetadata)
			: base(clientCommandId, commandCompleted: false, EncodeExtendedData(deviceTableId, startingDeviceId, devicesMetadata))
		{
		}

		public MyRvLinkCommandGetDevicesMetadataResponse(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public MyRvLinkCommandGetDevicesMetadataResponse(MyRvLinkCommandResponseSuccess response)
			: base(response.ClientCommandId, response.IsCommandCompleted, response.ExtendedData)
		{
		}

		protected List<IMyRvLinkDeviceMetadata> DecodeDevicesMetadata()
		{
			List<IMyRvLinkDeviceMetadata> devicesMetadata = new List<IMyRvLinkDeviceMetadata>();
			if (base.ExtendedData == null)
			{
				return new List<IMyRvLinkDeviceMetadata>();
			}
			int num = 3;
			while (num < base.ExtendedData.Count)
			{
				int num2 = DecodeMetadata(GetExtendedData(num), ref devicesMetadata);
				num += num2;
				if (num2 == 0)
				{
					throw new MyRvLinkDecoderException("Unable to decode device metadata, no bytes were read.");
				}
			}
			if (DeviceCount != devicesMetadata.Count)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(57, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode devices metadata, expected ");
				defaultInterpolatedStringHandler.AppendFormatted(DeviceCount);
				defaultInterpolatedStringHandler.AppendLiteral(" but decoded ");
				defaultInterpolatedStringHandler.AppendFormatted(devicesMetadata.Count);
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return devicesMetadata;
		}

		public static int DecodeMetadata(IReadOnlyList<byte> buffer, ref List<IMyRvLinkDeviceMetadata> devicesMetadata)
		{
			if (devicesMetadata == null)
			{
				devicesMetadata = new List<IMyRvLinkDeviceMetadata>();
			}
			int num = MyRvLinkDevice.DecodePayloadSize(buffer);
			IMyRvLinkDeviceMetadata item = MyRvLinkDeviceMetadata.TryDecodeFromRawBuffer(buffer);
			devicesMetadata.Add(item);
			return num + 2;
		}

		private static IReadOnlyList<byte> EncodeExtendedData(byte deviceTableId, byte startingDeviceId, IReadOnlyList<IMyRvLinkDeviceMetadata> devicesMetadata)
		{
			if (devicesMetadata.Count > 255)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(53, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Too many ");
				defaultInterpolatedStringHandler.AppendFormatted(devicesMetadata.Count);
				defaultInterpolatedStringHandler.AppendLiteral(" devices specified, only ");
				defaultInterpolatedStringHandler.AppendFormatted(1);
				defaultInterpolatedStringHandler.AppendLiteral(" devices supported.");
				throw new ArgumentOutOfRangeException("devicesMetadata", defaultInterpolatedStringHandler.ToStringAndClear());
			}
			byte b = (byte)devicesMetadata.Count;
			int num = 0;
			foreach (IMyRvLinkDeviceMetadata devicesMetadatum in devicesMetadata)
			{
				num += devicesMetadatum.EncodeSize;
			}
			byte[] array = new byte[3 + num];
			array[0] = deviceTableId;
			array[1] = startingDeviceId;
			array[2] = b;
			int num2 = 3;
			foreach (IMyRvLinkDeviceMetadata devicesMetadatum2 in devicesMetadata)
			{
				int num3 = devicesMetadatum2.EncodeIntoBuffer(array, num2);
				num2 += num3;
			}
			return new ArraySegment<byte>(array, 0, num2);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(68, 5);
			defaultInterpolatedStringHandler.AppendLiteral("Command(0x");
			defaultInterpolatedStringHandler.AppendFormatted(base.ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(") Response ");
			defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkCommandGetDevicesMetadataResponse");
			defaultInterpolatedStringHandler.AppendLiteral(" DeviceTableId: ");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceTableId);
			defaultInterpolatedStringHandler.AppendLiteral(" DeviceStartId: ");
			defaultInterpolatedStringHandler.AppendFormatted(StartDeviceId);
			defaultInterpolatedStringHandler.AppendLiteral(" Device Count: ");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceCount);
			StringBuilder stringBuilder = new StringBuilder(defaultInterpolatedStringHandler.ToStringAndClear());
			try
			{
				int startDeviceId = StartDeviceId;
				foreach (IMyRvLinkDeviceMetadata devicesMetadatum in DevicesMetadata)
				{
					StringBuilder stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder3 = stringBuilder2;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 2, stringBuilder2);
					handler.AppendLiteral("\n    0x");
					handler.AppendFormatted(startDeviceId++, "X2");
					handler.AppendLiteral(": ");
					handler.AppendFormatted(devicesMetadatum.ToString());
					stringBuilder3.Append(ref handler);
				}
			}
			catch (Exception ex)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder4 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(32, 1, stringBuilder2);
				handler.AppendLiteral("\n    ERROR Trying to Get Device ");
				handler.AppendFormatted(ex.Message);
				stringBuilder4.Append(ref handler);
			}
			return stringBuilder.ToString();
		}
	}
}
