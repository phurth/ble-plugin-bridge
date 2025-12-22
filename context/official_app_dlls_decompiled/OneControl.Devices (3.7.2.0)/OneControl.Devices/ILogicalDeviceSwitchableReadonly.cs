namespace OneControl.Devices
{
	public interface ILogicalDeviceSwitchableReadonly
	{
		bool On { get; }

		bool IsCurrentlyOn { get; }

		bool IsMasterSwitchControllable { get; }

		SwitchTransition SwitchInTransition { get; }

		SwitchUsage UsedFor { get; }
	}
}
