using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.Common.Manifest;

namespace IDS.Portable.LogicalDevice
{
	public static class ManifestBuilder
	{
		private const string LogTag = "ManifestBuilder";

		public static async Task<IManifest> MakeManifestAsync(IEnumerable<ILogicalDevice> deviceList, CancellationToken cancellationToken)
		{
			try
			{
				IManifest manifest = new Manifest();
				foreach (ILogicalDevice logicalDevice in deviceList)
				{
					IManifest manifest2 = manifest;
					IManifestProduct manifestProduct = manifest2.AddProduct(await MakeManifestProductAsync(logicalDevice, cancellationToken), updateSoftwarePartNumber: false);
					IManifestDevice manifestDevice = MakeManifestDevice(logicalDevice);
					if (manifestDevice != null)
					{
						manifestProduct.AddManifestDevice(manifestDevice);
					}
				}
				return manifest;
			}
			catch (OperationCanceledException ex)
			{
				TaggedLog.Debug("ManifestBuilder", "Unable to build manifest because " + ex.Message);
				throw;
			}
			catch (Exception ex2)
			{
				TaggedLog.Debug("ManifestBuilder", "Unable to build manifest because " + ex2.Message);
				throw new ManifestBuilderException(ex2.Message ?? "", ex2);
			}
		}

		public static IManifestDevice MakeManifestDevice(ILogicalDevice device)
		{
			string name = device.LogicalId.DeviceType.Name;
			byte value = device.LogicalId.DeviceType.Value;
			byte instance = (byte)device.LogicalId.DeviceInstance;
			string name2 = device.LogicalId.FunctionName.Name;
			ushort value2 = device.LogicalId.FunctionName.Value;
			string name3 = device.LogicalId.FunctionClass.GetName();
			byte functionInstance = (byte)device.LogicalId.FunctionInstance;
			byte rawValue = device.DeviceCapabilityBasic.GetRawValue();
			CIRCUIT_ID value3 = device.CircuitId.Value;
			return new ManifestDevice(isOnline: device.ActiveConnection != LogicalDeviceActiveConnection.Offline, name: name, typeID: value, instance: instance, functionName: name2, functionTypeID: value2, functionClass: name3, functionInstance: functionInstance, capabilities: rawValue, circuit: value3);
		}

		public static async Task<IManifestProduct> MakeManifestProductAsync(ILogicalDevice associatedDevice, CancellationToken cancellationToken)
		{
			MAC mAC = associatedDevice.LogicalId.ProductMacAddress ?? new MAC((byte)0);
			string uniqueId = mAC.ToString();
			string name = associatedDevice.LogicalId.ProductId.Name;
			ushort typeId = associatedDevice.LogicalId.ProductId.Value;
			int assemblyPartNumber = associatedDevice.LogicalId.ProductId.AssemblyPartNumber;
			string text = await associatedDevice.GetSoftwarePartNumberAsync(cancellationToken);
			Version protocolVersion = associatedDevice.ProtocolVersion;
			if (string.IsNullOrEmpty(text))
			{
				text = await associatedDevice.GetSoftwarePartNumberAsync(cancellationToken);
				if (string.IsNullOrEmpty(text))
				{
					TaggedLog.Error("ManifestBuilder", "Unable to determine software part number for device named: " + name + ", with uniqueId: " + uniqueId);
				}
				else
				{
					TaggedLog.Information("ManifestBuilder", "Initially failed to determine the software part number for device named: " + name + ". Software part number was found on second try.");
				}
			}
			return new ManifestProduct(uniqueId, name, typeId, assemblyPartNumber, text, protocolVersion);
		}
	}
}
