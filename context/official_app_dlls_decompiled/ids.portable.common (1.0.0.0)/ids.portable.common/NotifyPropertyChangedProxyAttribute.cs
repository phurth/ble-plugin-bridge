using System;
using System.Runtime.CompilerServices;

namespace IDS.Portable.Common
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class NotifyPropertyChangedProxyAttribute : Attribute
	{
		public string ProxyName { get; private set; }

		public string SourcePropertyName { get; private set; }

		public string? DestinationPropertyName { get; private set; }

		public NotifyPropertyChangedProxyAttribute(string proxyName, [CallerMemberName] string propertyName = "")
			: this(proxyName, propertyName, propertyName)
		{
		}

		public NotifyPropertyChangedProxyAttribute(string proxyName, string sourcePropertyName, [CallerMemberName] string destinationPropertyName = "")
		{
			ProxyName = proxyName;
			SourcePropertyName = sourcePropertyName;
			DestinationPropertyName = destinationPropertyName;
		}
	}
}
