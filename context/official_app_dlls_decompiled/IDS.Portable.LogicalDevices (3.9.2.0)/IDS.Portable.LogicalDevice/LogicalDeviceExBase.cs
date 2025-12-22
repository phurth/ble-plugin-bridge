using System;
using System.Collections.Generic;
using System.Linq;
using IDS.Portable.Common;
using ids.portable.common.Collection;

namespace IDS.Portable.LogicalDevice
{
	public abstract class LogicalDeviceExBase<TLogicalDevice> : ILogicalDeviceExOnline, ILogicalDeviceEx where TLogicalDevice : class, ILogicalDevice
	{
		protected readonly ConcurrentHashSet<TLogicalDevice> AttachedLogicalDevices = new ConcurrentHashSet<TLogicalDevice>();

		private static Dictionary<Type, ILogicalDeviceEx>? _sharedExtensionDict;

		private static readonly object _sharedExtensionLock = new object();

		protected abstract string LogTag { get; }

		protected List<TLogicalDevice> GetAttachedLogicalDevices()
		{
			return Enumerable.ToList(AttachedLogicalDevices);
		}

		protected static TExtension? GetSharedExtension<TExtension>(bool autoCreate) where TExtension : class, ILogicalDeviceEx, new()
		{
			lock (_sharedExtensionLock)
			{
				if (_sharedExtensionDict == null && !autoCreate)
				{
					return null;
				}
				if (_sharedExtensionDict == null)
				{
					_sharedExtensionDict = new Dictionary<Type, ILogicalDeviceEx>();
				}
				if (_sharedExtensionDict!.TryGetValue(typeof(TExtension), out var value))
				{
					return value as TExtension;
				}
				if (!autoCreate)
				{
					return null;
				}
				TExtension val = new TExtension();
				_sharedExtensionDict![typeof(TExtension)] = val;
				return val;
			}
		}

		protected static ILogicalDeviceEx? LogicalDeviceExFactory<TExtension>(ILogicalDevice logicalDevice, Func<TLogicalDevice, LogicalDeviceExScope> getScope) where TExtension : class, ILogicalDeviceEx, new()
		{
			if (!(logicalDevice is TLogicalDevice val))
			{
				return null;
			}
			switch (getScope?.Invoke(val) ?? LogicalDeviceExScope.NotSupported)
			{
			case LogicalDeviceExScope.NotSupported:
				return null;
			case LogicalDeviceExScope.Device:
				return new TExtension();
			case LogicalDeviceExScope.Product:
			{
				if (val.Product == null)
				{
					TaggedLog.Debug("LogicalDeviceExBase", $"Unable to register PRODUCT extension {typeof(TExtension)} as there is no product, this is expected for simulated devices, defaulting to device level registration for {logicalDevice}");
					return new TExtension();
				}
				ILogicalDeviceProduct? product = val.Product;
				return (product != null) ? product!.RegisterProductLogicalDeviceEx(() => new TExtension()) : null;
			}
			case LogicalDeviceExScope.Shared:
				return GetSharedExtension<TExtension>(autoCreate: true);
			default:
				return null;
			}
		}

		public virtual void LogicalDeviceAttached(ILogicalDevice logicalDevice)
		{
			if (!(logicalDevice is TLogicalDevice item))
			{
				TaggedLog.Error(LogTag, $"LogicalDeviceAttached attempting to attach to a Logical Device that doesn't implement {typeof(TLogicalDevice)}: {logicalDevice}");
			}
			else
			{
				AttachedLogicalDevices.Add(item);
			}
		}

		public virtual void LogicalDeviceDetached(ILogicalDevice logicalDevice)
		{
			if (!(logicalDevice is TLogicalDevice item))
			{
				TaggedLog.Error(LogTag, $"LogicalDeviceDetached attempting to detach from a Logical Device that doesn't implement {typeof(TLogicalDevice)}: {logicalDevice}");
			}
			else
			{
				AttachedLogicalDevices.TryRemove(item);
			}
		}

		public virtual void LogicalDeviceOnlineChanged(ILogicalDevice logicalDevice)
		{
		}
	}
}
