using System;
using Microsoft.Maui.Hosting;

namespace ids.portable.ble
{
	public static class AppBuilder
	{
		public static MauiAppBuilder UseBle(this MauiAppBuilder builder, Action<BleAppBuilder> configure)
		{
			configure(new BleAppBuilder(builder));
			return builder;
		}
	}
}
