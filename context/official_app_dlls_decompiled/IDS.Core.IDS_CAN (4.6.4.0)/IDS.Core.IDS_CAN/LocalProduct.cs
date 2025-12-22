using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace IDS.Core.IDS_CAN
{
	public class LocalProduct : Disposable, IProduct, IBusEndpoint, IUniqueProductInfo, IEnumerable<IDevice>, IEnumerable, IEnumerable<ILocalDevice>
	{
		private static readonly TimeSpan SOFTWARE_UPDATE_TIMEOUT = TimeSpan.FromSeconds(60.0);

		private ConcurrentDictionary<ulong, LocalDevice> Devices = new ConcurrentDictionary<ulong, LocalDevice>();

		public bool IsSoftwareUpdateAvailable;

		public readonly Timer SoftwareUpdateAuthorizeTimer = new Timer();

		public string Name { get; private set; }

		public string UniqueName { get; private set; }

		public IAdapter Adapter { get; private set; }

		public PRODUCT_ID ProductID { get; private set; }

		public IDS_CAN_VERSION_NUMBER ProtocolVersion { get; private set; }

		public byte ProductInstance => Address;

		public ADDRESS Address { get; private set; } = ADDRESS.BROADCAST;


		public int AssemblyPartNumber => ProductID.AssemblyPartNumber;

		public MAC MAC { get; private set; }

		public int DeviceCount => Devices.Count;

		public bool IsOnline => ProductInstance != 0;

		public string SoftwarePartNumber { get; private set; }

		public SOFTWARE_UPDATE_STATE SoftwareUpdateState
		{
			get
			{
				if (IsSoftwareUpdateAvailable)
				{
					return (byte)((!IsSoftwareUpdateAuthorized) ? 1 : 2);
				}
				return (byte)0;
			}
		}

		public bool IsSoftwareUpdateAuthorized
		{
			get
			{
				if (SoftwareUpdateAuthorizeTimer.IsRunning)
				{
					if (SoftwareUpdateAuthorizeTimer.ElapsedTime <= SOFTWARE_UPDATE_TIMEOUT)
					{
						return true;
					}
					SoftwareUpdateAuthorizeTimer.Stop();
				}
				return false;
			}
			set
			{
				if (value)
				{
					SoftwareUpdateAuthorizeTimer.Reset();
				}
				else
				{
					SoftwareUpdateAuthorizeTimer.Stop();
				}
			}
		}

		public IEnumerator<ILocalDevice> GetEnumerator()
		{
			return Devices.Values.GetEnumerator();
		}

		IEnumerator<IDevice> IEnumerable<IDevice>.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public LocalProduct(IAdapter adapter, MAC mac, PRODUCT_ID product_id, IDS_CAN_VERSION_NUMBER protocol_version, string software_part_number)
		{
			if (mac == null)
			{
				mac = new MAC();
				mac.SetRandomMACValue();
			}
			Adapter = adapter;
			MAC = mac;
			ProductID = product_id;
			ProtocolVersion = protocol_version;
			SoftwarePartNumber = software_part_number;
			Name = ProductID.ToString();
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(6, 2);
			defaultInterpolatedStringHandler.AppendFormatted(ProductID);
			defaultInterpolatedStringHandler.AppendLiteral(" MAC[");
			defaultInterpolatedStringHandler.AppendFormatted(MAC);
			defaultInterpolatedStringHandler.AppendLiteral("]");
			UniqueName = defaultInterpolatedStringHandler.ToStringAndClear();
			SoftwareUpdateAuthorizeTimer.Stop();
			if (adapter.LocalProducts is LocalProductManager localProductManager)
			{
				localProductManager.Add(this);
			}
		}

		public override void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}
			if (Adapter?.LocalProducts is LocalProductManager localProductManager)
			{
				localProductManager.Remove(this);
			}
			using (IEnumerator<ILocalDevice> enumerator = GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					enumerator.Current?.Dispose();
				}
			}
			Devices.Clear();
			Devices = null;
			Adapter = null;
		}

		public LocalDevice CreateDevice(DEVICE_TYPE device_type, int device_instance, FUNCTION_NAME function_name, int function_instance, byte? capabilties, LOCAL_DEVICE_OPTIONS options)
		{
			return new LocalDevice(this, device_type, device_instance, function_name, function_instance, capabilties, options);
		}

		public override string ToString()
		{
			return Name;
		}

		public void EnableAllDevices()
		{
			foreach (LocalDevice value in Devices.Values)
			{
				value.EnableDevice = true;
			}
		}

		public void DisableAllDevices()
		{
			foreach (LocalDevice value in Devices.Values)
			{
				value.EnableDevice = false;
			}
		}

		public void AddDevice(LocalDevice device)
		{
			Devices.TryAdd(device.GetDeviceUniqueID(), device);
		}

		public void RemoveDevice(LocalDevice device)
		{
			Devices.TryRemove(device.GetDeviceUniqueID(), out var _);
		}

		internal void ChooseNewProductAddress()
		{
			using (IEnumerator<ILocalDevice> enumerator = GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ADDRESS address = enumerator.Current.Address;
					if (address.IsValidDeviceAddress)
					{
						Address = address;
						return;
					}
				}
			}
			Address = ADDRESS.BROADCAST;
		}

		internal void SuggestNewProductAddress(ADDRESS address)
		{
			if (address.IsValidDeviceAddress && !Address.IsValidDeviceAddress)
			{
				Address = address;
			}
		}
	}
}
