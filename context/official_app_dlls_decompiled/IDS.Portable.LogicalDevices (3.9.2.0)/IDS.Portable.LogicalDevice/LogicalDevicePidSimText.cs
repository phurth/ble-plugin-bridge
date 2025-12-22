using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidSimText : LogicalDevicePidSim, ILogicalDevicePidText, ILogicalDevicePidProperty<string>, ILogicalDevicePid<string>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		public string ValueText
		{
			get
			{
				return LogicalDevicePidText.ConvertToString(base.ValueRaw);
			}
			set
			{
				base.ValueRaw = (UInt48)LogicalDevicePidText.ConvertFromString(value);
			}
		}

		public LogicalDevicePidSimText(PID pid, string value)
			: base(pid, LogicalDevicePidText.ConvertFromString(value))
		{
		}

		public async Task<string> ReadAsync(CancellationToken cancellationToken)
		{
			return LogicalDevicePidText.ConvertToString(await ReadValueAsync(cancellationToken));
		}

		public async Task WriteAsync(string value, CancellationToken cancellationToken)
		{
			ulong value2 = LogicalDevicePidText.ConvertFromString(value);
			await WriteValueAsync(value2, cancellationToken);
		}

		public async Task<string> ReadTextAsync(CancellationToken cancellationToken)
		{
			return await ReadAsync(cancellationToken);
		}

		public async Task WriteTextAsync(string value, CancellationToken cancellationToken)
		{
			await WriteAsync(value, cancellationToken);
		}

		protected override void ValueRawPropertyChanged()
		{
			base.ValueRawPropertyChanged();
			NotifyPropertyChanged("ValueText");
		}
	}
}
