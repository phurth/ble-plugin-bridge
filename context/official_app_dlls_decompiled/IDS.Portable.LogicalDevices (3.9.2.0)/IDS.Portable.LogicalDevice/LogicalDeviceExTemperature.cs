using System;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public abstract class LogicalDeviceExTemperature<TLogicalDevice> : LogicalDeviceExBase<TLogicalDevice>, ILogicalDeviceExTemperature, ILogicalDeviceEx where TLogicalDevice : class, ILogicalDeviceTemperatureMeasurement
	{
		public abstract Task<ITemperatureMeasurement> ReadTemperatureMeasurementAsync(CancellationToken cancellationToken);

		protected virtual bool TemperatureMeasurementSupported(TLogicalDevice logicalDevice)
		{
			return true;
		}

		protected TLogicalDevice? FindPreferredLogicalDevice()
		{
			foreach (TLogicalDevice attachedLogicalDevice in GetAttachedLogicalDevices())
			{
				if (TemperatureMeasurementSupported(attachedLogicalDevice))
				{
					LogicalDeviceActiveConnection activeConnection = attachedLogicalDevice.ActiveConnection;
					if (activeConnection != 0 && (uint)(activeConnection - 1) <= 2u && CanBePreferredDevice(attachedLogicalDevice))
					{
						return attachedLogicalDevice;
					}
				}
			}
			return null;
		}

		protected abstract bool CanBePreferredDevice(TLogicalDevice logicalDevice);

		protected ITemperatureMeasurement GetTemperatureMeasurement(Func<TLogicalDevice, LogicalDeviceExScope> getScope, Func<TLogicalDevice, ITemperatureMeasurement> getMeasurement)
		{
			TLogicalDevice val = FindPreferredLogicalDevice();
			try
			{
				if (val == null || getScope == null || getMeasurement == null)
				{
					throw new TemperatureMeasurementNotSupportedException();
				}
				if (getScope(val) == LogicalDeviceExScope.NotSupported)
				{
					throw new TemperatureMeasurementNotSupportedException();
				}
				if (!TemperatureMeasurementSupported(val))
				{
					throw new TemperatureMeasurementNotSupportedException();
				}
				if (val.ActiveConnection == LogicalDeviceActiveConnection.Offline)
				{
					throw new TemperatureMeasurementUnavailableException();
				}
				ITemperatureMeasurement temperatureMeasurement = getMeasurement(val);
				if (!temperatureMeasurement.IsTemperatureValid)
				{
					throw new TemperatureMeasurementUnavailableException();
				}
				return temperatureMeasurement;
			}
			catch (TemperatureMeasurementUnavailableException)
			{
				throw;
			}
			catch (TemperatureMeasurementNotSupportedException)
			{
				throw;
			}
			catch (TaskCanceledException ex3)
			{
				TaggedLog.Error(LogTag, string.Format("{0}: Canceled for {1}: {2}", "GetTemperatureMeasurement", val, ex3.Message));
				throw new TemperatureMeasurementOperationCanceledException();
			}
			catch (TimeoutException ex4)
			{
				TaggedLog.Error(LogTag, string.Format("{0}: Timeout for {1}: {2}", "GetTemperatureMeasurement", val, ex4.Message));
				throw new TemperatureMeasurementTimeoutException();
			}
			catch (Exception ex5)
			{
				string message = string.Format("{0}: Exception for {1}: {2}", "GetTemperatureMeasurement", val, ex5.Message);
				TaggedLog.Error(LogTag, message);
				throw new TemperatureMeasurementException(message, ex5);
			}
		}

		public override string ToString()
		{
			if (FindPreferredLogicalDevice()?.Product == null)
			{
				return LogTag + " newly created";
			}
			return $"{LogTag} for {FindPreferredLogicalDevice()?.Product} with {GetAttachedLogicalDevices().Count} devices";
		}
	}
}
