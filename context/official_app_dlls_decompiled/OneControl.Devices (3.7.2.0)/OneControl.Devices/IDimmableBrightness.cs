using System.Threading.Tasks;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface IDimmableBrightness
	{
		byte DimmableBrightness { get; }

		bool IsDimmableBrightnessEnabled { get; }

		Task<CommandResult> SendDimmableBrightnessCommandAsync(byte brightness);
	}
}
