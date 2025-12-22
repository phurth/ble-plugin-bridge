using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace IDS.Portable.Common
{
	public class NotifyPropertyChangedAction<TSource, TDestination> : CommonDisposable
	{
		private const string LogTag = "NotifyPropertyChangedAction";

		private readonly NotifyPropertyChangedProxySource _proxySource;

		private readonly PropertyInfo _sourceProperty;

		private readonly Func<TSource, TDestination>? _sourceToDestinationConverter;

		private readonly Action<TDestination> _destinationAction;

		public NotifyPropertyChangedAction(INotifyPropertyChanged source, string sourcePropertyName, Action<TDestination> destinationAction, Func<TSource, TDestination>? converter = null)
		{
			if (source == null)
			{
				throw new ArgumentException("Invalid source source");
			}
			Type type = source.GetType();
			if (type == null)
			{
				throw new ArgumentException("Invalid sourceType source");
			}
			if (string.IsNullOrEmpty(sourcePropertyName))
			{
				throw new ArgumentException("Invalid or null sourcePropertyName");
			}
			_sourceProperty = type.GetProperty(sourcePropertyName);
			if (_sourceProperty == null)
			{
				throw new ArgumentException("Invalid property sourcePropertyName");
			}
			_sourceToDestinationConverter = converter;
			_destinationAction = destinationAction ?? throw new ArgumentNullException("destinationAction");
			_proxySource = new NotifyPropertyChangedProxySource(source);
			_proxySource.AddInvokeFor(sourcePropertyName, DoSourcePropertyChanged);
			DoSourcePropertyChanged();
		}

		private void DoSourcePropertyChanged()
		{
			try
			{
				if (!base.IsDisposed)
				{
					_destinationAction(GetValue());
				}
			}
			catch (Exception ex)
			{
				TaggedLog.Error("NotifyPropertyChangedAction", "{0} Unable to convert property: {1}", "DoSourcePropertyChanged", ex.Message);
			}
		}

		public TDestination GetValue()
		{
			if (base.IsDisposed)
			{
				throw new ObjectDisposedException(ToString());
			}
			object obj = _sourceProperty?.GetValue(_proxySource.Source, null);
			if (_sourceToDestinationConverter == null)
			{
				if (obj is TDestination)
				{
					return (TDestination)obj;
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(84, 1);
				defaultInterpolatedStringHandler.AppendLiteral("No converter specified and source value can't be auto converted to expected type of ");
				defaultInterpolatedStringHandler.AppendFormatted(typeof(TDestination));
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (!(obj is TSource arg))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(52, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Source value can't be converted to expected type of ");
				defaultInterpolatedStringHandler.AppendFormatted(typeof(TSource));
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return _sourceToDestinationConverter!(arg);
		}

		public override void Dispose(bool disposing)
		{
			_proxySource.TryDispose();
		}
	}
}
