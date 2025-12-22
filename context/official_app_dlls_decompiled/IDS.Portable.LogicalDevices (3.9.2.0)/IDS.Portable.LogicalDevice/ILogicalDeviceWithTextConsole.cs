using System;
using System.ComponentModel;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceWithTextConsole : ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		string TextConsoleMessage { get; }

		int TextConsoleWidth { get; }

		int TextConsoleHeight { get; }

		void UpdateTextConsole(ITextConsole textConsole);

		void OnDeviceTextConsoleMessageChanged();
	}
}
