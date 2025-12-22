using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.Types;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.Leveler.Type5;

namespace OneControl.Devices
{
	public class LogicalDevicePidProxyLevelerType5CustomSetPointNames : LogicalDevicePidProxy<ILogicalDevicePidArrayEnum<SetPointType>>, ILogicalDevicePidProperty<LevelerSetPointNames>, ILogicalDevicePid<LevelerSetPointNames>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		public new const string LogTag = "LogicalDevicePidProxyLevelerType5CustomSetPointNames";

		private const int FactorySetPointIndex = 5;

		private const int SetPoint1Index = 4;

		private const int SetPoint2Index = 3;

		private const int SetPoint3Index = 2;

		private const int Reserved2Index = 1;

		private const int Reserved1Index = 0;

		private const int ExpectedByteLength = 6;

		private static readonly LevelerSetPointNames defaultValue = new LevelerSetPointNames(SetPointType.Unknown, SetPointType.Unknown, SetPointType.Unknown, SetPointType.Unknown);

		public LevelerSetPointNames SetPointNames
		{
			get
			{
				return Decode(base.ValueRaw);
			}
			set
			{
				base.ValueRaw = (UInt48)EncodeAsULong(value);
			}
		}

		public LogicalDevicePidProxyLevelerType5CustomSetPointNames(ILogicalDevicePidArrayEnum<SetPointType> devicePid = null)
			: base(devicePid)
		{
			base.DevicePid = devicePid;
		}

		public static LevelerSetPointNames Decode(SetPointType[] setPoints)
		{
			if (setPoints.Length != 6)
			{
				TaggedLog.Debug("LogicalDevicePidProxyLevelerType5CustomSetPointNames", $"Unable to Decode SetPoint name as the number of setpoints returned was not long enough. Data length: {setPoints.Length}");
				return defaultValue;
			}
			return new LevelerSetPointNames(setPoints[5], setPoints[4], setPoints[3], setPoints[2]);
		}

		public LevelerSetPointNames Decode(ulong rawData)
		{
			TaggedLog.Debug("LogicalDevicePidProxyLevelerType5CustomSetPointNames", $"Raw Data test: {rawData}");
			ILogicalDevicePidArrayEnum<SetPointType> devicePid = base.DevicePid;
			if (devicePid == null)
			{
				TaggedLog.Debug("LogicalDevicePidProxyLevelerType5CustomSetPointNames", "Unable to Decode SetPoint names, DevicePid is null, returning default");
				return defaultValue;
			}
			return Decode(devicePid.ConvertToEnums(rawData));
		}

		private SetPointType[] FormatSetPointArray(LevelerSetPointNames customNames)
		{
			return new SetPointType[6]
			{
				SetPointType.Reserved,
				SetPointType.Reserved,
				customNames.SetPoint3,
				customNames.SetPoint2,
				customNames.SetPoint1,
				customNames.FactorySetPoint
			};
		}

		public SetPointType[] Encode(LevelerSetPointNames customNames)
		{
			return FormatSetPointArray(customNames);
		}

		public ulong EncodeAsULong(LevelerSetPointNames customNames)
		{
			return Encode(FormatSetPointArray(customNames));
		}

		public LevelerSetPointNames Encode(ulong rawData)
		{
			ILogicalDevicePidArrayEnum<SetPointType> devicePid = base.DevicePid;
			if (devicePid == null)
			{
				TaggedLog.Debug("LogicalDevicePidProxyLevelerType5CustomSetPointNames", "Unable to Encode SetPoint names, DevicePid is null, returning default");
				return defaultValue;
			}
			SetPointType[] array = devicePid.ConvertToEnums(rawData);
			return new LevelerSetPointNames(array[5], array[4], array[3], array[2]);
		}

		public ulong Encode(SetPointType[] enumeratedData)
		{
			ILogicalDevicePidArrayEnum<SetPointType> devicePid = base.DevicePid;
			if (devicePid == null)
			{
				TaggedLog.Debug("LogicalDevicePidProxyLevelerType5CustomSetPointNames", "Unable to Encode SetPoint names, DevicePid is null, returning default");
				return EncodeAsULong(defaultValue);
			}
			return devicePid.ConvertFromEnums(enumeratedData);
		}

		public async Task<LevelerSetPointNames> ReadAsync(CancellationToken cancellationToken)
		{
			return Decode(await ReadValueAsync(cancellationToken));
		}

		public async Task WriteAsync(LevelerSetPointNames customNames, CancellationToken cancellationToken)
		{
			if (base.IsReadOnly)
			{
				throw new LogicalDevicePidValueWriteNotSupportedException(base.PropertyId);
			}
			await WriteValueAsync(Encode(FormatSetPointArray(customNames)), cancellationToken);
		}

		protected override void ValueRawPropertyChanged()
		{
			TaggedLog.Debug("LogicalDevicePidProxyLevelerType5CustomSetPointNames", $"SetPointNamesChanged - {SetPointNames}");
			base.ValueRawPropertyChanged();
			NotifyPropertyChanged("SetPointNames");
		}
	}
}
