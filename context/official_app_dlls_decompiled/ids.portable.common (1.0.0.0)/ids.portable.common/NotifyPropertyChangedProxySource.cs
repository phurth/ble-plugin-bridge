using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Utils;

namespace IDS.Portable.Common
{
	public class NotifyPropertyChangedProxySource : CommonDisposable
	{
		private const string LogTag = "NotifyPropertyChangedProxySource";

		private readonly object lockObject = new object();

		private readonly INotifyPropertyChanged _source;

		private readonly ProxyOnPropertyChanged? _destinationOnPropertyChanged;

		private readonly bool _onlyAllowInvokes;

		private readonly Dictionary<string, HashSet<string>> _propertyNameMap = new Dictionary<string, HashSet<string>>();

		private readonly Dictionary<string, HashSet<Action>> _propertyActionDict = new Dictionary<string, HashSet<Action>>();

		private string? _anyDestinationPropertyName;

		private Watchdog? _aggregateNotificationWatchdog;

		private HashSet<string>? _aggregatedPropertyNames;

		private static readonly ObjectPool<HashSet<string>> _aggregateNotificationPool = ObjectPool<HashSet<string>>.MakeObjectPool<HashSet<string>>();

		public INotifyPropertyChanged Source => _source;

		public NotifyPropertyChangedProxySource(INotifyPropertyChanged source, ProxyOnPropertyChanged destinationOnPropertyChanged, List<string>? properyNameList = null)
		{
			_source = source;
			_destinationOnPropertyChanged = destinationOnPropertyChanged;
			_source.PropertyChanged += PropertyChangedEventHandler;
			_onlyAllowInvokes = false;
			if (properyNameList == null)
			{
				return;
			}
			foreach (string item in properyNameList!)
			{
				AddProxyFor(item);
			}
		}

		public NotifyPropertyChangedProxySource(INotifyPropertyChanged source)
		{
			_source = source;
			_destinationOnPropertyChanged = null;
			_source.PropertyChanged += PropertyChangedEventHandler;
			_onlyAllowInvokes = true;
		}

		public NotifyPropertyChangedProxySource(INotifyPropertyChanged source, ProxyOnPropertyChanged destinationOnPropertyChanged, INotifyPropertyChanged destination, string proxyName)
			: this(source, destinationOnPropertyChanged)
		{
			Type type = source.GetType();
			bool flag = HasAutoProxy(destination.GetType(), proxyName);
			foreach (PropertyInfo runtimeProperty in RuntimeReflectionExtensions.GetRuntimeProperties(destination.GetType()))
			{
				if (runtimeProperty == null)
				{
					continue;
				}
				if (flag && type.GetProperty(runtimeProperty.Name) != null)
				{
					bool flag2 = false;
					foreach (NotifyPropertyChangedAutoProxyIgnoreAttribute customAttribute in CustomAttributeExtensions.GetCustomAttributes<NotifyPropertyChangedAutoProxyIgnoreAttribute>(runtimeProperty))
					{
						if (!(customAttribute.ProxyName != proxyName) || !(customAttribute.PropertyName != runtimeProperty.Name))
						{
							flag2 = true;
							TaggedLog.Debug("NotifyPropertyChangedProxySource", "{0} - AUTO property proxy {1} ignored by request!", destination, runtimeProperty.Name);
						}
					}
					if (!flag2)
					{
						TaggedLog.Debug("NotifyPropertyChangedProxySource", "{0} - AUTO adding property proxy {1} as it exists in both the source and destination!", destination, runtimeProperty.Name);
						AddProxyFor(runtimeProperty.Name, runtimeProperty.Name);
					}
				}
				foreach (NotifyPropertyChangedProxyAttribute customAttribute2 in CustomAttributeExtensions.GetCustomAttributes<NotifyPropertyChangedProxyAttribute>(runtimeProperty))
				{
					try
					{
						if (!(customAttribute2.ProxyName != proxyName))
						{
							AddProxyFor(customAttribute2.SourcePropertyName, customAttribute2.DestinationPropertyName);
						}
					}
					catch (Exception ex)
					{
						TaggedLog.Error("NotifyPropertyChangedProxySource", "Ignoring attributed invoke for {0} because {1}", customAttribute2.SourcePropertyName, ex.Message);
					}
				}
			}
			foreach (MethodInfo runtimeMethod in RuntimeReflectionExtensions.GetRuntimeMethods(destination.GetType()))
			{
				foreach (NotifyPropertyChangedInvokeAttribute customAttribute3 in CustomAttributeExtensions.GetCustomAttributes<NotifyPropertyChangedInvokeAttribute>(runtimeMethod))
				{
					try
					{
						if (!(customAttribute3.ProxyName != proxyName))
						{
							AddInvokeFor(customAttribute3.SourcePropertyName, customAttribute3.MakeInvokeMethod(runtimeMethod, destination));
						}
					}
					catch (Exception ex2)
					{
						TaggedLog.Error("NotifyPropertyChangedProxySource", "Ignoring attributed method invoke for {0} because {1}", runtimeMethod.Name, ex2.Message);
					}
				}
			}
			foreach (FieldInfo runtimeField in RuntimeReflectionExtensions.GetRuntimeFields(destination.GetType()))
			{
				foreach (NotifyPropertyChangedInvokeFieldAttribute customAttribute4 in CustomAttributeExtensions.GetCustomAttributes<NotifyPropertyChangedInvokeFieldAttribute>(runtimeField))
				{
					try
					{
						if (!(customAttribute4.ProxyName != proxyName))
						{
							AddInvokeFor(customAttribute4.SourcePropertyName, customAttribute4.MakeInvokeMethod(runtimeField, destination));
						}
					}
					catch (Exception ex3)
					{
						TaggedLog.Error("NotifyPropertyChangedProxySource", "Ignoring attributed field invoke for {0}.{1} because {2}", runtimeField.Name, customAttribute4.MethodName ?? "Unknown", ex3.Message);
					}
				}
			}
		}

		~NotifyPropertyChangedProxySource()
		{
			try
			{
				_source.PropertyChanged -= PropertyChangedEventHandler;
			}
			catch
			{
			}
		}

		private static bool HasAutoProxy(Type destinationType, string proxyName)
		{
			foreach (NotifyPropertyChangedAutoProxyAttribute customAttribute in CustomAttributeExtensions.GetCustomAttributes<NotifyPropertyChangedAutoProxyAttribute>(destinationType))
			{
				if (customAttribute != null && !(customAttribute.ProxyName != proxyName))
				{
					return true;
				}
			}
			return false;
		}

		public void AddProxyFor(string propertyName)
		{
			AddProxyFor(propertyName, propertyName);
		}

		public void AddProxyFor(string sourcePropertyName, string destinationPropertyName)
		{
			if (string.IsNullOrEmpty(sourcePropertyName) || string.IsNullOrEmpty(destinationPropertyName))
			{
				throw new ArgumentException("Invalid source property name of method");
			}
			if (_onlyAllowInvokes || _destinationOnPropertyChanged == null)
			{
				TaggedLog.Error("NotifyPropertyChangedProxySource", "AddProxyFor ignored as NotifyPropertyChangedProxySource isn't configured to handle property proxies", string.Empty);
			}
			else
			{
				HashSet<string> hashSet = _propertyNameMap.TryGetValue(sourcePropertyName) ?? new HashSet<string>();
				hashSet.Add(destinationPropertyName);
				_propertyNameMap[sourcePropertyName] = hashSet;
			}
		}

		public void AddInvokeFor(string sourcePropertyName, Action method)
		{
			if (string.IsNullOrEmpty(sourcePropertyName))
			{
				throw new ArgumentException("Source property name is null or empty");
			}
			if (method == null)
			{
				throw new ArgumentException("Action method is null");
			}
			if (!_propertyActionDict.TryGetValue(sourcePropertyName, out var value))
			{
				value = new HashSet<Action>();
				_propertyActionDict[sourcePropertyName] = value;
			}
			value.Add(method);
		}

		public void SetDestinationProxyForAnySourceProperty(string destinationPropertyName)
		{
			if (_onlyAllowInvokes || _destinationOnPropertyChanged == null)
			{
				TaggedLog.Error("NotifyPropertyChangedProxySource", "SetDestinationProxyForAnySourceProperty ignored as NotifyPropertyChangedProxySource isn't configured to handle property proxies", string.Empty);
			}
			else
			{
				_anyDestinationPropertyName = destinationPropertyName;
			}
		}

		private void PropertyChangedEventHandler(object sender, PropertyChangedEventArgs eventArgs)
		{
			if (base.IsDisposed)
			{
				TaggedLog.Debug("NotifyPropertyChangedProxySource", "NotifyPropertyChangedProxySource proxy trigger for {0} IGNORED as we are disposed!", eventArgs.PropertyName);
				return;
			}
			if (_propertyActionDict.TryGetValue(eventArgs.PropertyName, out var value))
			{
				foreach (Action item in value)
				{
					item?.Invoke();
				}
			}
			if (_onlyAllowInvokes || _destinationOnPropertyChanged == null)
			{
				if (!_onlyAllowInvokes)
				{
					TaggedLog.Debug("NotifyPropertyChangedProxySource", "NotifyPropertyChangedProxySource proxy trigger for {0} IGNORED as destination is NULL", eventArgs.PropertyName);
				}
				return;
			}
			HashSet<string> hashSet = _propertyNameMap.TryGetValue(eventArgs.PropertyName);
			if (hashSet != null)
			{
				foreach (string item2 in hashSet)
				{
					SendDestinationOnPropertyChanged(item2);
				}
			}
			if (_anyDestinationPropertyName != null)
			{
				SendDestinationOnPropertyChanged(_anyDestinationPropertyName);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void SendDestinationOnPropertyChanged(string propertyName)
		{
			_destinationOnPropertyChanged?.Invoke(propertyName);
		}

		public override void Dispose(bool disposing)
		{
			try
			{
				_source.PropertyChanged -= PropertyChangedEventHandler;
			}
			catch
			{
			}
			_propertyNameMap.Clear();
			_propertyActionDict.Clear();
			lock (lockObject)
			{
				_aggregateNotificationWatchdog?.Dispose();
				_aggregateNotificationWatchdog = null;
				if (_aggregatedPropertyNames != null)
				{
					_aggregatedPropertyNames!.Clear();
					_aggregateNotificationPool.PutObject(_aggregatedPropertyNames);
					_aggregatedPropertyNames = null;
				}
			}
		}
	}
}
