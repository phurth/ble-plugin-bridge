using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.MyRvLink
{
	public abstract class MyRvLinkDevice : IMyRvLinkDevice
	{
		private const string LogTag = "MyRvLinkDevice";

		public const int EncodeHeaderSize = 2;

		protected const int DeviceProtocolIndex = 0;

		protected const int DeviceEntrySizeIndex = 1;

		public MyRvLinkDeviceProtocol Protocol { get; }

		public virtual byte EncodeSize => 2;

		protected MyRvLinkDevice(MyRvLinkDeviceProtocol protocol)
		{
			Protocol = protocol;
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
			defaultInterpolatedStringHandler.AppendFormatted(Protocol);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		public static MyRvLinkDeviceProtocol DecodeDeviceProtocol(IReadOnlyList<byte> decodeBuffer)
		{
			return (MyRvLinkDeviceProtocol)decodeBuffer[0];
		}

		public static int DecodePayloadSize(IReadOnlyList<byte> decodeBuffer)
		{
			return decodeBuffer[1];
		}

		public virtual int EncodeIntoBuffer(byte[] buffer, int offset)
		{
			buffer[offset] = (byte)Protocol;
			buffer[1 + offset] = (byte)(EncodeSize - 2);
			return EncodeSize;
		}

		public static IMyRvLinkDevice TryDecodeFromRawBuffer(IReadOnlyList<byte> buffer)
		{
			MyRvLinkDeviceProtocol myRvLinkDeviceProtocol = MyRvLinkDeviceProtocol.None;
			try
			{
				myRvLinkDeviceProtocol = DecodeDeviceProtocol(buffer);
				return myRvLinkDeviceProtocol switch
				{
					MyRvLinkDeviceProtocol.Host => MyRvLinkDeviceHost.Decode(buffer), 
					MyRvLinkDeviceProtocol.IdsCan => MyRvLinkDeviceIdsCan.Decode(buffer), 
					MyRvLinkDeviceProtocol.None => new MyRvLinkDeviceNone(myRvLinkDeviceProtocol), 
					_ => new MyRvLinkDeviceNone(myRvLinkDeviceProtocol), 
				};
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(61, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Error trying to decode device for ");
				defaultInterpolatedStringHandler.AppendFormatted(myRvLinkDeviceProtocol);
				defaultInterpolatedStringHandler.AppendLiteral(" returning unknown device: ");
				defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
				TaggedLog.Debug("MyRvLinkDevice", defaultInterpolatedStringHandler.ToStringAndClear());
				return new MyRvLinkDeviceNone(myRvLinkDeviceProtocol);
			}
		}

		public IEnumerable<ILogicalDevice> FindLogicalDevicesMatchingPhysicalHardware(ILogicalDeviceService deviceService)
		{
			ILogicalDeviceManager deviceManager = deviceService.DeviceManager;
			if (deviceManager == null || !(this is IMyRvLinkDeviceForLogicalDevice myRvLinkDeviceForLogicalDevice))
			{
				yield break;
			}
			foreach (ILogicalDevice logicalDevice in deviceManager.LogicalDevices)
			{
				if (logicalDevice.LogicalId.IsMatchingPhysicalHardware(myRvLinkDeviceForLogicalDevice.ProductId, myRvLinkDeviceForLogicalDevice.DeviceType, myRvLinkDeviceForLogicalDevice.DeviceInstance, myRvLinkDeviceForLogicalDevice.ProductMacAddress))
				{
					yield return logicalDevice;
				}
			}
		}
	}
}
