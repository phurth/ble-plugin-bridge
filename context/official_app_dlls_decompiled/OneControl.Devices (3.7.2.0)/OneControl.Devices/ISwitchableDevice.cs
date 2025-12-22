using System.Threading.Tasks;

namespace OneControl.Devices
{
	public interface ISwitchableDevice : ILogicalDeviceSwitchableReadonly
	{
		Task<bool> ToggleAsync(bool restore);

		Task<bool> TurnOnAsync(bool restore);

		Task<bool> TurnOffAsync();
	}
}
