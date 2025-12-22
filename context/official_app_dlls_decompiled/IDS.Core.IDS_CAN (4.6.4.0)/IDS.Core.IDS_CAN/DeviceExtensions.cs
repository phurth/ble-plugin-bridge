using System.Runtime.CompilerServices;
using IDS.Core.Types;

namespace IDS.Core.IDS_CAN
{
	public static class DeviceExtensions
	{
		public static DEVICE_ID GetDeviceID(this IDevice device)
		{
			return new DEVICE_ID(device.ProductID, device.ProductInstance, device.DeviceType, device.DeviceInstance, device.FunctionName, device.FunctionInstance, device.DeviceCapabilities);
		}

		public static ulong GetDeviceUniqueID(this IUniqueDeviceInfo device)
		{
			return ((((((((((((((((ulong)device.MAC[0] << 8) | device.MAC[1]) << 8) | device.MAC[2]) << 8) | device.MAC[3]) << 8) | device.MAC[4]) << 8) | device.MAC[5]) << 4) | (byte)((ushort)device.ProductID & 0xF)) << 8) | (byte)device.DeviceType) << 4) | (byte)((uint)device.DeviceInstance & 0xFu);
		}

		public static UInt128 GetFeatureUniqueID(this IDevice device)
		{
			return ((((UInt128)device.GetDeviceUniqueID() << 16) | (ushort)device.FunctionName) << 4) | (byte)((uint)device.FunctionInstance & 0xFu);
		}

		public static string FriendlyName(this IDevice device)
		{
			FUNCTION_NAME fUNCTION_NAME = device.FunctionName ?? FUNCTION_NAME.UNKNOWN;
			if (device.FunctionInstance > 0)
			{
				if (fUNCTION_NAME.Name.Contains("{0}"))
				{
					return fUNCTION_NAME.Name.Replace("{0}", device.FunctionInstance.ToString()) ?? "";
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(1, 2);
				defaultInterpolatedStringHandler.AppendFormatted(fUNCTION_NAME);
				defaultInterpolatedStringHandler.AppendLiteral(" ");
				defaultInterpolatedStringHandler.AppendFormatted(device.FunctionInstance);
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
			return fUNCTION_NAME.ToString();
		}

		public static string ToShortString(this IDevice device, bool show_address)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
			if (show_address)
			{
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(5, 3);
				defaultInterpolatedStringHandler.AppendFormatted(device.FriendlyName());
				defaultInterpolatedStringHandler.AppendLiteral(" (");
				defaultInterpolatedStringHandler.AppendFormatted(device.DeviceType);
				defaultInterpolatedStringHandler.AppendLiteral(" @");
				defaultInterpolatedStringHandler.AppendFormatted(device.Address);
				defaultInterpolatedStringHandler.AppendLiteral(")");
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
			defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 2);
			defaultInterpolatedStringHandler.AppendFormatted(device.FriendlyName());
			defaultInterpolatedStringHandler.AppendLiteral(" (");
			defaultInterpolatedStringHandler.AppendFormatted(device.DeviceType);
			defaultInterpolatedStringHandler.AppendLiteral(")");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
