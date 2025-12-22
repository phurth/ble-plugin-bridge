using System;
using IDS.Portable.Common;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;

namespace OneControl.Direct.IdsCanAccessoryBle
{
	public interface IAccessoryBleDeviceDriver : ICommonDisposable, IDisposable
	{
		bool IsOnline { get; }

		void Update(IdsCanAccessoryScanResult accessoryScanResult);
	}
}
