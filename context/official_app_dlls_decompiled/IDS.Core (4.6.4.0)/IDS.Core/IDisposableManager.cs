using System;

namespace IDS.Core
{
	public interface IDisposableManager : IDisposable, System.IDisposable
	{
		void AddDisposable(IDisposable obj);

		void RemoveDisposable(IDisposable obj);
	}
}
