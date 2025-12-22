using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceProduct : ILogicalProductStatus, ICommonDisposable, IDisposable
	{
		ILogicalDeviceService DeviceService { get; }

		PRODUCT_ID ProductId { get; }

		MAC MacAddress { get; }

		LogicalDeviceProductDtcDataSource DtcDataSource { get; }

		Task<CommandResult> SendSoftwareUpdateAuthorizationAsync(CancellationToken cancelToken);

		TLogicalDeviceEx? RegisterProductLogicalDeviceEx<TLogicalDeviceEx>(Func<TLogicalDeviceEx> extensionFactory, bool replaceExisting = false) where TLogicalDeviceEx : class, ILogicalDeviceEx;

		TLogicalDeviceEx? GetProductLogicalDeviceEx<TLogicalDeviceEx>() where TLogicalDeviceEx : class, ILogicalDeviceEx;

		IPidDetail GetPidDetail(Pid canPid);

		Task<IReadOnlyDictionary<DTC_ID, DtcValue>> GetProductDtcDictAsync(LogicalDeviceDtcFilter dtcFilter, CancellationToken cancellationToken);
	}
}
