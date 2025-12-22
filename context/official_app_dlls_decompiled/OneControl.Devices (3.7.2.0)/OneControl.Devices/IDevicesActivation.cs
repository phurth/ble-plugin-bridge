using System.Threading;
using System.Threading.Tasks;

namespace OneControl.Devices
{
	public interface IDevicesActivation
	{
		bool CommandSessionActivated { get; }

		Task ActivateSession(CancellationToken cancelToken);

		void DeactivateSession();
	}
}
