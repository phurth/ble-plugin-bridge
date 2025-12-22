using System;

namespace IDS.Portable.Common
{
	public interface ICommonDisposable : IDisposable
	{
		bool IsDisposed { get; }

		void TryDispose();
	}
}
