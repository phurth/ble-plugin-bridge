using System;

namespace OneControl.Devices
{
	[AttributeUsage(AttributeTargets.Field)]
	public class RelayHBridgeDirectionVerboseLabel : Attribute
	{
		public string Label { get; }

		public RelayHBridgeDirectionVerboseLabel(string label)
		{
			Label = label;
		}
	}
}
