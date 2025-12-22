using System;

namespace IDS.Portable.Common
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class NotifyPropertyChangedAutoProxyAttribute : Attribute
	{
		public string ProxyName { get; private set; }

		public NotifyPropertyChangedAutoProxyAttribute(string proxyName)
		{
			ProxyName = proxyName;
		}
	}
}
