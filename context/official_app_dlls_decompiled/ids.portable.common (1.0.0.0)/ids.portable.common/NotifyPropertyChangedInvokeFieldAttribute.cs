using System;
using System.Reflection;

namespace IDS.Portable.Common
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class NotifyPropertyChangedInvokeFieldAttribute : Attribute, INotifyPropertyChangedInvokeAttribute
	{
		private static string LogTag = "NotifyPropertyChangedInvokeFieldAttribute";

		public string ProxyName { get; }

		public string SourcePropertyName { get; }

		public string MethodName { get; }

		public NotifyPropertyChangedInvokeFieldAttribute(string proxyName, string propertyName, string invokeMethodName)
		{
			ProxyName = proxyName;
			SourcePropertyName = propertyName;
			MethodName = invokeMethodName;
		}

		internal Action MakeInvokeMethod(FieldInfo fieldInfo, object destination)
		{
			if (fieldInfo == null)
			{
				throw new ArgumentNullException("fieldInfo");
			}
			if (destination == null)
			{
				throw new ArgumentNullException("destination");
			}
			if (MethodName == null)
			{
				throw new Exception("MethodName is null.");
			}
			return delegate
			{
				object value = fieldInfo.GetValue(destination);
				MethodInfo methodInfo = value?.GetType().GetMethod(MethodName, Type.EmptyTypes);
				if (methodInfo == null)
				{
					TaggedLog.Warning(LogTag, "Invoke Method {0}.{1} failed because method or object is null", fieldInfo.Name, MethodName);
				}
				else
				{
					methodInfo.Invoke(value, null);
				}
			};
		}
	}
}
