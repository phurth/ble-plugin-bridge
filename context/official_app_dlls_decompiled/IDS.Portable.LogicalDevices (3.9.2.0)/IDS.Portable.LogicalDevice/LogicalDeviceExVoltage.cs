using System;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public abstract class LogicalDeviceExVoltage<TLogicalDevice> : LogicalDeviceExBase<TLogicalDevice>, ILogicalDeviceExBatteryVoltage, ILogicalDeviceEx, IReadVoltageMeasurement where TLogicalDevice : class, ILogicalDeviceVoltageMeasurement
	{
		public abstract Task<float> ReadVoltageMeasurementAsync(CancellationToken cancellationToken);

		protected virtual bool VoltageMeasurementSupported(TLogicalDevice logicalDevice)
		{
			return true;
		}

		public TLogicalDevice? FindPreferredLogicalDevice()
		{
			foreach (TLogicalDevice attachedLogicalDevice in GetAttachedLogicalDevices())
			{
				if (VoltageMeasurementSupported(attachedLogicalDevice))
				{
					LogicalDeviceActiveConnection activeConnection = attachedLogicalDevice.ActiveConnection;
					if (activeConnection != 0 && (uint)(activeConnection - 1) <= 2u)
					{
						return attachedLogicalDevice;
					}
				}
			}
			return null;
		}

		protected async Task<float> GetVoltageMeasurementAsync(CancellationToken cancellationToken, Func<TLogicalDevice, LogicalDeviceExScope> getScope, Func<TLogicalDevice, CancellationToken, Task<float>> getMeasurementAsync)
		{
			TLogicalDevice logicalDevice = FindPreferredLogicalDevice() ?? throw new VoltageMeasurementUnavailableException("No preferred device found");
			try
			{
				if (logicalDevice == null || getScope == null || getMeasurementAsync == null)
				{
					throw new VoltageMeasurementNotSupportedException();
				}
				if (getScope(logicalDevice) == LogicalDeviceExScope.NotSupported)
				{
					throw new VoltageMeasurementNotSupportedException();
				}
				if (!VoltageMeasurementSupported(logicalDevice))
				{
					throw new VoltageMeasurementNotSupportedException();
				}
				if (getScope(logicalDevice) == LogicalDeviceExScope.NotSupported)
				{
					throw new TemperatureMeasurementNotSupportedException();
				}
				return await getMeasurementAsync(logicalDevice, cancellationToken);
			}
			catch (VoltageMeasurementNotSupportedException)
			{
				throw;
			}
			catch (PhysicalDeviceNotFoundException)
			{
				throw;
			}
			catch (TaskCanceledException ex3)
			{
				TaggedLog.Error(LogTag, string.Format("{0}: Canceled for {1}: {2}", "GetVoltageMeasurementAsync", logicalDevice, ex3.Message));
				throw new VoltageMeasurementOperationCanceledException();
			}
			catch (TimeoutException ex4)
			{
				TaggedLog.Error(LogTag, string.Format("{0}: Timeout for {1}: {2}", "GetVoltageMeasurementAsync", logicalDevice, ex4.Message));
				throw new VoltageMeasurementTimeoutException();
			}
			catch (Exception ex5)
			{
				string message = string.Format("{0}: Exception for {1}: {2}", "GetVoltageMeasurementAsync", logicalDevice, ex5.Message);
				TaggedLog.Error(LogTag, message);
				throw new VoltageMeasurementException(message, ex5);
			}
		}

		public override string ToString()
		{
			if (FindPreferredLogicalDevice()?.Product == null)
			{
				return "LogicalDeviceExVoltageBattery newly created";
			}
			return string.Format("{0} for {1} with {2} devices", "LogicalDeviceExVoltageBattery", FindPreferredLogicalDevice()?.Product, GetAttachedLogicalDevices().Count);
		}
	}
}
