using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidText : LogicalDevicePid, ILogicalDevicePidText, ILogicalDevicePidProperty<string>, ILogicalDevicePid<string>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		public const int MaxStringSize = 6;

		public LogicalDevicePidTextEncoding Encoding { get; }

		public string ValueText
		{
			get
			{
				return ConvertToString(base.ValueRaw);
			}
			set
			{
				base.ValueRaw = (UInt48)ConvertFromString(value);
			}
		}

		public LogicalDevicePidText(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, LogicalDevicePidTextEncoding encoding = LogicalDevicePidTextEncoding.Ascii, Func<ulong, bool>? validityCheck = null)
			: base(logicalDevice, pid, writeAccess, validityCheck)
		{
			Encoding = encoding;
		}

		public static string ConvertToString(ulong value)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (byte item in Enumerable.Reverse(BitConverter.GetBytes(value)))
			{
				if (item != 0)
				{
					stringBuilder.Append(Convert.ToChar(item));
				}
			}
			return stringBuilder.ToString();
		}

		public static ulong ConvertFromString(string value)
		{
			ulong num = 0uL;
			byte[] bytes = System.Text.Encoding.ASCII.GetBytes(value);
			if (bytes.Length > 6)
			{
				throw new ArgumentOutOfRangeException("value", $"String must not exceed {6} bytes");
			}
			for (int i = 0; i < bytes.Length; i++)
			{
				num <<= 8;
				num |= bytes[i];
			}
			return num;
		}

		public Task<string> ReadAsync(CancellationToken cancellationToken)
		{
			return ReadTextAsync(cancellationToken);
		}

		public Task WriteAsync(string value, CancellationToken cancellationToken)
		{
			return WriteTextAsync(value, cancellationToken);
		}

		public async Task<string> ReadTextAsync(CancellationToken cancellationToken)
		{
			return ConvertToString(await ReadValueAsync(cancellationToken));
		}

		public Task WriteTextAsync(string value, CancellationToken cancellationToken)
		{
			try
			{
				ulong value2 = ConvertFromString(value);
				return WriteValueAsync(value2, cancellationToken);
			}
			catch (ArgumentOutOfRangeException innerException)
			{
				throw new LogicalDevicePidInvalidValueException<string>(base.PropertyId, LogicalDevice, value, innerException);
			}
		}

		protected override void ValueRawPropertyChanged()
		{
			base.ValueRawPropertyChanged();
			OnPropertyChanged("ValueText");
		}
	}
}
