using System;
using System.Linq;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace IDS.Portable.LogicalDevice
{
	[JsonObject(MemberSerialization.OptIn)]
	public readonly struct PidDetail<TValue> : IPidDetail, IPidDetail<ulong>
	{
		private readonly Func<ulong, TValue> _convertToValueConverter;

		private readonly Func<ulong, IDevicePID?, PidValueCheck> _pidValueChecker;

		[JsonProperty]
		[JsonConverter(typeof(StringEnumConverter))]
		public Pid Pid { get; }

		[JsonProperty]
		[JsonConverter(typeof(StringEnumConverter))]
		public PidCategory Category { get; }

		[JsonProperty]
		[JsonConverter(typeof(StringEnumConverter))]
		public PidUnits Units { get; }

		[JsonProperty]
		public int MinimumBytes { get; }

		[JsonProperty]
		public int DecimalPlacesOfInterest { get; }

		[JsonProperty]
		public bool Deprecated { get; }

		[JsonProperty]
		public bool IsUndefinedPid { get; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		[JsonConverter(typeof(StringEnumConverter))]
		public LogicalDeviceSessionType? WriteSession { get; }

		public string FriendlyName => FriendlyNameDefault(Pid);

		public PidDetail(Pid pid, PidCategory category, PidUnits units, Func<ulong, TValue> toValue, Func<ulong, IDevicePID?, PidValueCheck>? pidValueChecker = null, int minimumBytes = 1, int decimalPlacesOfInterest = 2, bool deprecated = false, bool undefinedPid = false, LogicalDeviceSessionType? writeSession = null)
		{
			Pid = pid;
			Category = category;
			Units = units;
			Deprecated = deprecated;
			_convertToValueConverter = toValue;
			_pidValueChecker = pidValueChecker ?? new Func<ulong, IDevicePID, PidValueCheck>(PidValueCheckExtension.PidCheckValueDefault);
			MinimumBytes = minimumBytes;
			DecimalPlacesOfInterest = decimalPlacesOfInterest;
			IsUndefinedPid = undefinedPid;
			WriteSession = writeSession;
		}

		public TValue Convert(ulong value)
		{
			return _convertToValueConverter(value);
		}

		public PidValueCheck CheckValue(ulong value, IDevicePID? canPid = null)
		{
			return _pidValueChecker(value, canPid);
		}

		public string FormattedValue(ulong rawPidValue, IDevicePID? canPid = null)
		{
			PidValueCheck pidValueCheck = CheckValue(rawPidValue, canPid);
			switch (pidValueCheck)
			{
			case PidValueCheck.NoValue:
			case PidValueCheck.FeatureDisabled:
			case PidValueCheck.Undefined:
				return pidValueCheck.Description() ?? string.Empty;
			default:
			{
				TValue val = Convert(rawPidValue);
				if (val is FunctionName)
				{
					object obj = val;
					FunctionName functionName = (FunctionName)((obj is FunctionName) ? obj : null);
					try
					{
						return functionName.ToFunctionName().Name;
					}
					catch
					{
						return functionName.ToString();
					}
				}
				if (!((object)val is Enum instance))
				{
					if ((object)val is byte[] array)
					{
						return array.DebugDump();
					}
					string? value = Units.Description();
					string text;
					if (val is ulong)
					{
						object obj3 = val;
						ulong num = (ulong)((obj3 is ulong) ? obj3 : null);
						text = ((Units != PidUnits.None && Units != 0 && Units != PidUnits.Mixed) ? val.ToString() : ("0x" + num.ToString($"X{MinimumBytes * 2}")));
					}
					else if (val is uint)
					{
						object obj4 = val;
						uint num2 = (uint)((obj4 is uint) ? obj4 : null);
						text = ((Units != PidUnits.None && Units != 0 && Units != PidUnits.Mixed) ? val.ToString() : ("0x" + num2.ToString($"X{MinimumBytes * 2}")));
					}
					else if (val is float)
					{
						object obj5 = val;
						text = ((float)((obj5 is float) ? obj5 : null)).ToString($"n{DecimalPlacesOfInterest}");
					}
					else
					{
						object obj6 = val?.ToString();
						if (obj6 == null)
						{
							obj6 = string.Empty;
						}
						text = (string)obj6;
					}
					if (string.IsNullOrWhiteSpace(value))
					{
						return text;
					}
					return text + " " + Units.Description();
				}
				return instance.Description() ?? string.Empty;
			}
			}
		}

		private static string FriendlyNameDefault(Pid pid)
		{
			if (pid.TryGetDescription(out var description) && description != null)
			{
				return description;
			}
			description = pid.ToString();
			description = string.Concat(Enumerable.Select(description, (char x) => (!char.IsUpper(x)) ? x.ToString() : (" " + x))).TrimStart(new char[1] { ' ' });
			description = description.TrimEndString(" Count");
			description = description.TrimEndString(" Sec");
			description = description.TrimEndString(" Inches");
			return description.TrimEndString(" Ms");
		}
	}
}
