using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.Common;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandGetDevicesResponse : MyRvLinkCommandResponseSuccess
	{
		public const string LogTag = "MyRvLinkCommandGetDevicesResponse";

		private List<IMyRvLinkDevice>? _devices;

		protected const int ExtendedDataHeaderSize = 3;

		protected const int DeviceTableIdIndex = 0;

		protected const int StartingDeviceIdIndex = 1;

		protected const int DeviceCountIndex = 2;

		protected override int MinExtendedDataLength => 3;

		public byte DeviceTableId => base.ExtendedData[0];

		public byte StartDeviceId => base.ExtendedData[1];

		public byte DeviceCount => base.ExtendedData[2];

		public IReadOnlyList<IMyRvLinkDevice> Devices => _devices ?? (_devices = DecodeDevices());

		public MyRvLinkCommandGetDevicesResponse(ushort clientCommandId, byte deviceTableId, byte startingDeviceId, IReadOnlyList<IMyRvLinkDevice> devices)
			: base(clientCommandId, commandCompleted: false, EncodeExtendedData(deviceTableId, startingDeviceId, devices))
		{
		}

		public MyRvLinkCommandGetDevicesResponse(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public MyRvLinkCommandGetDevicesResponse(MyRvLinkCommandResponseSuccess response)
			: base(response.ClientCommandId, response.IsCommandCompleted, response.ExtendedData)
		{
		}

		protected List<IMyRvLinkDevice> DecodeDevices()
		{
			List<IMyRvLinkDevice> devices = new List<IMyRvLinkDevice>();
			if (base.ExtendedData == null)
			{
				return new List<IMyRvLinkDevice>();
			}
			int num = 3;
			while (num < base.ExtendedData.Count)
			{
				int item = DecodeDevice(GetExtendedData(num), ref devices).bytesRead;
				num += item;
				if (item == 0)
				{
					throw new MyRvLinkDecoderException("Unable to decode device, no bytes were read.");
				}
			}
			if (DeviceCount != devices.Count)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(48, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode devices, expected ");
				defaultInterpolatedStringHandler.AppendFormatted(DeviceCount);
				defaultInterpolatedStringHandler.AppendLiteral(" but decoded ");
				defaultInterpolatedStringHandler.AppendFormatted(devices.Count);
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return devices;
		}

		public static (int bytesRead, IMyRvLinkDevice device) DecodeDevice(IReadOnlyList<byte> buffer, ref List<IMyRvLinkDevice> devices)
		{
			if (devices == null)
			{
				devices = new List<IMyRvLinkDevice>();
			}
			int num = MyRvLinkDevice.DecodePayloadSize(buffer);
			IMyRvLinkDevice myRvLinkDevice = MyRvLinkDevice.TryDecodeFromRawBuffer(buffer);
			if (myRvLinkDevice is MyRvLinkDeviceNone)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(24, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Added an unknown device ");
				defaultInterpolatedStringHandler.AppendFormatted(myRvLinkDevice);
				TaggedLog.Warning("MyRvLinkCommandGetDevicesResponse", defaultInterpolatedStringHandler.ToStringAndClear());
			}
			devices.Add(myRvLinkDevice);
			return (num + 2, myRvLinkDevice);
		}

		private static IReadOnlyList<byte> EncodeExtendedData(byte deviceTableId, byte startingDeviceId, IReadOnlyList<IMyRvLinkDevice> devices)
		{
			if (devices.Count > 255)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(53, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Too many ");
				defaultInterpolatedStringHandler.AppendFormatted(devices.Count);
				defaultInterpolatedStringHandler.AppendLiteral(" devices specified, only ");
				defaultInterpolatedStringHandler.AppendFormatted(1);
				defaultInterpolatedStringHandler.AppendLiteral(" devices supported.");
				throw new ArgumentOutOfRangeException("devices", defaultInterpolatedStringHandler.ToStringAndClear());
			}
			byte b = (byte)devices.Count;
			int num = 0;
			foreach (IMyRvLinkDevice device in devices)
			{
				num += device.EncodeSize;
			}
			byte[] array = new byte[3 + num];
			array[0] = deviceTableId;
			array[1] = startingDeviceId;
			array[2] = b;
			int num2 = 3;
			foreach (IMyRvLinkDevice device2 in devices)
			{
				int num3 = device2.EncodeIntoBuffer(array, num2);
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
			defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkCommandGetDevicesResponse");
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
				foreach (IMyRvLinkDevice device in Devices)
				{
					StringBuilder stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder3 = stringBuilder2;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 2, stringBuilder2);
					handler.AppendLiteral("\n    0x");
					handler.AppendFormatted(startDeviceId++, "X2");
					handler.AppendLiteral(": ");
					handler.AppendFormatted(device.ToString());
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
