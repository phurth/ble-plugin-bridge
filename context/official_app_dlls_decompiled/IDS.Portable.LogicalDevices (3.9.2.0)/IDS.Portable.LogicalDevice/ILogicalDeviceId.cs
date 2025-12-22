using System;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceId : IJsonSerializable, IJsonSerializerClass, IComparable, IEquatable<ILogicalDeviceId>
	{
		DEVICE_TYPE DeviceType { get; }

		int DeviceInstance { get; }

		PRODUCT_ID ProductId { get; }

		MAC? ProductMacAddress { get; }

		FUNCTION_NAME FunctionName { get; }

		int FunctionInstance { get; }

		FUNCTION_CLASS FunctionClass { get; }

		[Obsolete("FunctionString is deprecated, please use ToString(LogicalDeviceIdFormat.FunctionNameFull) instead.")]
		string FunctionString { get; }

		DEVICE_ID MakeDeviceId(byte? rawCapability, byte productInstance = 0);

		DEVICE_ID MakeDeviceId<TCapability>(TCapability capability, byte productInstance = 0) where TCapability : ILogicalDeviceCapability;

		bool IsForDeviceId(DEVICE_ID deviceId, MAC withDeviceMacAddress);

		bool IsForDeviceIdIgnoringName(DEVICE_ID deviceId, MAC withDeviceMacAddress, bool ignoreFunctionClass);

		bool IsMatchingPhysicalHardware(PRODUCT_ID withProductId, DEVICE_TYPE withDeviceType, int withDeviceInstance, MAC withDeviceMacAddress);

		bool Rename(FUNCTION_NAME functionName, int functionInstance);

		string MakeImmutableUniqueId(bool isFunctionClassChangeable);

		string ToLdiStringEncoding();

		string ToString(LogicalDeviceIdFormat format);

		ILogicalDeviceId Clone();
	}
}
