using System;
using System.Reflection;

namespace IDS.Portable.Common
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public class NotifyPropertyChangedInvokeAttribute : Attribute, INotifyPropertyChangedInvokeAttribute
	{
		public string ProxyName { get; private set; }

		public string SourcePropertyName { get; private set; }

		public NotifyPropertyChangedInvokeAttribute(string proxyName, string propertyName)
		{
			ProxyName = proxyName;
			SourcePropertyName = propertyName;
		}

		internal Action MakeInvokeMethod(MethodInfo methodInfo, object destination)
		{
			if (methodInfo == null)
			{
				throw new ArgumentNullException("methodInfo");
			}
			if (destination == null)
			{
				throw new ArgumentNullException("destination");
			}
			if (methodInfo.GetParameters().Length != 0)
			{
				throw new Exception("Given methodInfo must take zero arguments to be invoked");
			}
			return delegate
			{
				methodInfo.Invoke(destination, null);
			};
		}
	}
}
