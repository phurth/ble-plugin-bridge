using System;
using System.Threading.Tasks;

namespace IDS.Portable.Common
{
	public interface IWatchdog : ICommonDisposable, IDisposable
	{
		bool AutoStartOnFirstPet { get; }

		TimeSpan PetTimeout { get; }

		TimeSpan MaxTimeUntilTriggered { get; }

		bool Triggered { get; }

		void Monitor();

		void Cancel();

		long Pet(bool autoReset = false);

		long TryPet(bool autoReset = false);

		Task AsTask();
	}
}
