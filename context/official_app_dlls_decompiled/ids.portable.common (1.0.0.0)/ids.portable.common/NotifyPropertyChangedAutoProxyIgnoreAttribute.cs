using System;
using System.Runtime.CompilerServices;

namespace IDS.Portable.Common
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class NotifyPropertyChangedAutoProxyIgnoreAttribute : Attribute
	{
		public string ProxyName { get; private set; }

		public string PropertyName { get; private set; }

		public NotifyPropertyChangedAutoProxyIgnoreAttribute(string proxyName, [CallerMemberName] string propertyName = "")
		{
			ProxyName = proxyName;
			PropertyName = propertyName;
		}
	}
}
