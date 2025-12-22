using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceProduct : CommonDisposableNotifyPropertyChanged, ILogicalDeviceProduct, ILogicalProductStatus, ICommonDisposable, IDisposable
	{
		private const string LogTag = "LogicalDeviceProduct";

		public LogicalDeviceDataPacketMutableDoubleBuffer ProductStatusBuffer = new LogicalDeviceDataPacketMutableDoubleBuffer(1u, 8u, 0);

		private const byte SoftwareUpdateStateBitMask = 3;

		private string? _softwarePartNumber;

		private readonly Dictionary<Type, ILogicalDeviceEx> _logicalDeviceExDict = new Dictionary<Type, ILogicalDeviceEx>();

		public ILogicalDeviceService DeviceService { get; }

		public PRODUCT_ID ProductId { get; }

		public MAC MacAddress { get; }

		public SoftwareUpdateState SoftwareUpdateStateLastKnown
		{
			get
			{
				if (!ProductStatusBuffer.HasData || ProductStatusBuffer.Size < 1)
				{
					return SoftwareUpdateState.Unknown;
				}
				return (byte)(SOFTWARE_UPDATE_STATE)(byte)(ProductStatusBuffer.Data[0] & 3u) switch
				{
					0 => SoftwareUpdateState.None, 
					1 => SoftwareUpdateState.NeedsAuthorization, 
					2 => SoftwareUpdateState.Authorized, 
					3 => SoftwareUpdateState.InProgress, 
					_ => SoftwareUpdateState.Unknown, 
				};
			}
		}

		public LogicalDeviceProductDtcDataSource DtcDataSource { get; }

		public event LogicalDeviceProductStatusChangedEventHandler? DeviceProductStatusChanged;

		public IEnumerable<ILogicalDevice> FindOnlineDirectLogicalDevices()
		{
			ILogicalDeviceService deviceService = DeviceService;
			object obj;
			if (deviceService == null)
			{
				obj = null;
			}
			else
			{
				ILogicalDeviceProductManager? productManager = deviceService.ProductManager;
				obj = ((productManager != null) ? Enumerable.Where(productManager!.FindLogicalDevices<ILogicalDevice>(ProductId, MacAddress), delegate(ILogicalDevice ld)
				{
					LogicalDeviceActiveConnection activeConnection = ld.ActiveConnection;
					return (activeConnection == LogicalDeviceActiveConnection.Direct || activeConnection == LogicalDeviceActiveConnection.Cloud) ? true : false;
				}) : null);
			}
			if (obj == null)
			{
				IEnumerable<ILogicalDevice> enumerable = Enumerable.Empty<ILogicalDevice>();
				obj = enumerable;
			}
			return (IEnumerable<ILogicalDevice>)obj;
		}

		public LogicalDeviceProduct(PRODUCT_ID productId, MAC macAddress, ILogicalDeviceService deviceService)
		{
			ProductId = productId;
			MacAddress = macAddress;
			DeviceService = deviceService;
			DtcDataSource = new LogicalDeviceProductDtcDataSource(this);
		}

		public override string ToString()
		{
			return $"Product {ProductId} for {MacAddress}";
		}

		public Task<CommandResult> SendSoftwareUpdateAuthorizationAsync(CancellationToken cancelToken)
		{
			ILogicalDevice logicalDevice = Enumerable.FirstOrDefault(FindOnlineDirectLogicalDevices());
			if (logicalDevice == null)
			{
				return Task.FromResult(CommandResult.ErrorDeviceOffline);
			}
			if (!(DeviceService?.GetPrimaryDeviceSourceDirect(logicalDevice) is ILogicalDeviceSourceDirectSoftwareUpdateAuthorization logicalDeviceSourceDirectSoftwareUpdateAuthorization))
			{
				return Task.FromResult(CommandResult.ErrorOther);
			}
			return logicalDeviceSourceDirectSoftwareUpdateAuthorization.SendSoftwareUpdateAuthorizationAsync(logicalDevice, cancelToken);
		}

		public virtual IPidDetail GetPidDetail(Pid pid)
		{
			return pid.GetPidDetailDefault();
		}

		public byte[] CopyRawProductStatus()
		{
			return ProductStatusBuffer.CopyCurrentData();
		}

		public virtual bool UpdateProductStatus(IReadOnlyList<byte> statusData, uint dataLength)
		{
			bool flag = false;
			try
			{
				byte[] data = ProductStatusBuffer.Data;
				flag = ProductStatusBuffer.Update(statusData, (int)dataLength) != dataLength;
				if (flag)
				{
					DebugUpdateProductStatusChanged(data, statusData, dataLength);
					NotifyPropertyChanged("SoftwareUpdateStateLastKnown");
					OnProductStatusChanged();
					return flag;
				}
				return flag;
			}
			catch (Exception ex)
			{
				TaggedLog.Error("LogicalDeviceProduct", string.Format("{0} {1} - Exception updating status {2}: {3}", "LogicalDeviceProduct", this, ex, ex.StackTrace));
				return flag;
			}
		}

		protected virtual void DebugUpdateProductStatusChanged(IReadOnlyList<byte> oldStatusData, IReadOnlyList<byte> statusData, uint dataLength, string optionalText = "")
		{
			TaggedLog.Information("LogicalDeviceProduct", string.Format("{0} {1} - Product Status changed from {2} to {3} {4}{5}", "LogicalDeviceProduct", this, oldStatusData.DebugDump(0, (int)dataLength), statusData.DebugDump(0, (int)dataLength), SoftwareUpdateStateLastKnown, optionalText));
		}

		public virtual void OnProductStatusChanged()
		{
			this.DeviceProductStatusChanged?.Invoke(this);
		}

		public virtual Task<string> GetSoftwarePartNumberAsync(CancellationToken cancelToken)
		{
			throw new NotImplementedException();
		}

		public virtual bool UpdateSoftwarePartNumber(string softwarePartNumber)
		{
			if (string.Equals(softwarePartNumber, _softwarePartNumber))
			{
				return false;
			}
			_softwarePartNumber = softwarePartNumber;
			return true;
		}

		public TLogicalDeviceEx? RegisterProductLogicalDeviceEx<TLogicalDeviceEx>(Func<TLogicalDeviceEx> extensionFactory, bool replaceExisting = false) where TLogicalDeviceEx : class, ILogicalDeviceEx
		{
			Type typeFromHandle = typeof(TLogicalDeviceEx);
			if (_logicalDeviceExDict.TryGetValue(typeFromHandle) is TLogicalDeviceEx val)
			{
				if (!replaceExisting)
				{
					TaggedLog.Debug("LogicalDeviceProduct", string.Format("{0} Using existing {1} for {2}", "RegisterProductLogicalDeviceEx", typeFromHandle, val));
					return val;
				}
				TaggedLog.Warning("LogicalDeviceProduct", string.Format("{0} for {1} is being replaced by a new extension", "RegisterProductLogicalDeviceEx", typeFromHandle));
			}
			if (extensionFactory == null)
			{
				TaggedLog.Error("LogicalDeviceProduct", "RegisterProductLogicalDeviceEx ignored because extension factory is NULL");
				return null;
			}
			TLogicalDeviceEx val2 = extensionFactory();
			_logicalDeviceExDict[typeFromHandle] = val2;
			TaggedLog.Debug("LogicalDeviceProduct", string.Format("{0} Created new {1} for {2}", "RegisterProductLogicalDeviceEx", typeFromHandle, val2));
			return val2;
		}

		public TLogicalDeviceEx? GetProductLogicalDeviceEx<TLogicalDeviceEx>() where TLogicalDeviceEx : class, ILogicalDeviceEx
		{
			lock (_logicalDeviceExDict)
			{
				_logicalDeviceExDict.TryGetValue(typeof(TLogicalDeviceEx), out var value);
				return value as TLogicalDeviceEx;
			}
		}

		public override void Dispose(bool disposing)
		{
			_logicalDeviceExDict.Clear();
			DtcDataSource.Dispose();
			this.DeviceProductStatusChanged = null;
			base.Dispose(disposing);
		}

		public async Task<IReadOnlyDictionary<DTC_ID, DtcValue>> GetProductDtcDictAsync(LogicalDeviceDtcFilter dtcFilter, CancellationToken cancellationToken)
		{
			Dictionary<DTC_ID, DtcValue> result = new Dictionary<DTC_ID, DtcValue>();
			ILogicalDevice directLogicalDevice = Enumerable.FirstOrDefault(FindOnlineDirectLogicalDevices());
			if (directLogicalDevice == null)
			{
				return result;
			}
			if (!(DeviceService.GetPrimaryDeviceSourceDirect(directLogicalDevice) is ILogicalDeviceSourceDirectDtc logicalDeviceSourceDirectDtc))
			{
				return result;
			}
			try
			{
				return await logicalDeviceSourceDirectDtc.GetDtcValuesAsync(directLogicalDevice, dtcFilter, DTC_ID.UNKNOWN, (DTC_ID)65535, cancellationToken);
			}
			catch (Exception ex)
			{
				TaggedLog.Debug("LogicalDeviceProduct", $"GetDtcValuesAsync failed for {ProductId} using {directLogicalDevice}: {ex.Message}");
				throw;
			}
		}
	}
}
