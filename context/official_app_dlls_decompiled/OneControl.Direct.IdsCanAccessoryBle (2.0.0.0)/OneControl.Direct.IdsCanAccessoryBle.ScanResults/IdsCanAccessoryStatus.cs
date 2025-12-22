using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.IdsCanAccessoryBle.ScanResults
{
	public readonly struct IdsCanAccessoryStatus
	{
		public MAC AccessoryMacAddress { get; }

		public float ConnectionWindowSeconds { get; }

		public PRODUCT_ID ProductId { get; }

		public DEVICE_TYPE DeviceType { get; }

		public FUNCTION_NAME FunctionName { get; }

		public byte FunctionInstance { get; }

		public byte RawCapability { get; }

		public IReadOnlyList<byte> DeviceStatus { get; }

		public byte Crc { get; }

		public uint Key { get; }

		public CIRCUIT_ID CircuitId => (uint)(UInt48)AccessoryMacAddress;

		public IdsCanAccessoryStatus(MAC accessoryMacAddress, float connectionWindowSeconds, PRODUCT_ID productId, DEVICE_TYPE deviceType, FUNCTION_NAME functionName, int functionInstance, byte rawCapability, IReadOnlyList<byte> status, byte crc, uint key)
		{
			AccessoryMacAddress = accessoryMacAddress;
			ConnectionWindowSeconds = connectionWindowSeconds;
			ProductId = productId;
			DeviceType = deviceType;
			FunctionName = functionName;
			FunctionInstance = (byte)((uint)functionInstance & 0xFu);
			RawCapability = rawCapability;
			DeviceStatus = status;
			Crc = crc;
			Key = key;
		}

		public bool IsMatchForLogicalDevice(ILogicalDevice? logicalDevice)
		{
			if (logicalDevice == null || logicalDevice!.IsDisposed || logicalDevice!.LogicalId.ProductMacAddress != AccessoryMacAddress || logicalDevice!.LogicalId.FunctionName != FunctionName || logicalDevice!.LogicalId.FunctionInstance != FunctionInstance || logicalDevice!.LogicalId.DeviceType != DeviceType || logicalDevice!.LogicalId.DeviceInstance != 0 || logicalDevice!.LogicalId.ProductId != ProductId)
			{
				return false;
			}
			return true;
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(35, 7);
			defaultInterpolatedStringHandler.AppendFormatted(ConnectionWindowSeconds, "F2");
			defaultInterpolatedStringHandler.AppendLiteral(", ");
			defaultInterpolatedStringHandler.AppendFormatted(ProductId);
			defaultInterpolatedStringHandler.AppendLiteral(", ");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceType);
			defaultInterpolatedStringHandler.AppendLiteral(", ");
			defaultInterpolatedStringHandler.AppendFormatted(FunctionName);
			defaultInterpolatedStringHandler.AppendLiteral(":");
			defaultInterpolatedStringHandler.AppendFormatted(FunctionInstance);
			defaultInterpolatedStringHandler.AppendLiteral(" Capability: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(RawCapability, "X");
			defaultInterpolatedStringHandler.AppendLiteral(" Raw Status: ");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceStatus.DebugDump(0, DeviceStatus.Count));
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
