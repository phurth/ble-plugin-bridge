using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkDeviceMetadata : IMyRvLinkDeviceMetadata
	{
		private const string LogTag = "MyRvLinkDeviceMetadata";

		public const int EncodeHeaderSize = 2;

		protected const int DeviceProtocolIndex = 0;

		protected const int DeviceEntrySizeIndex = 1;

		public MyRvLinkDeviceProtocol Protocol { get; }

		public virtual byte EncodeSize => 2;

		public MyRvLinkDeviceMetadata(MyRvLinkDeviceProtocol protocol)
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

		public static IMyRvLinkDeviceMetadata TryDecodeFromRawBuffer(IReadOnlyList<byte> buffer)
		{
			MyRvLinkDeviceProtocol myRvLinkDeviceProtocol = MyRvLinkDeviceProtocol.None;
			try
			{
				myRvLinkDeviceProtocol = MyRvLinkDevice.DecodeDeviceProtocol(buffer);
				return myRvLinkDeviceProtocol switch
				{
					MyRvLinkDeviceProtocol.Host => MyRvLinkDeviceHostMetadata.Decode(buffer), 
					MyRvLinkDeviceProtocol.IdsCan => MyRvLinkDeviceIdsCanMetadata.Decode(buffer), 
					MyRvLinkDeviceProtocol.None => new MyRvLinkDeviceMetadata(myRvLinkDeviceProtocol), 
					_ => new MyRvLinkDeviceMetadata(myRvLinkDeviceProtocol), 
				};
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(70, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Error trying to decode device METADATA for ");
				defaultInterpolatedStringHandler.AppendFormatted(myRvLinkDeviceProtocol);
				defaultInterpolatedStringHandler.AppendLiteral(" returning unknown device: ");
				defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
				TaggedLog.Debug("MyRvLinkDeviceMetadata", defaultInterpolatedStringHandler.ToStringAndClear());
				return new MyRvLinkDeviceMetadata(myRvLinkDeviceProtocol);
			}
		}
	}
}
