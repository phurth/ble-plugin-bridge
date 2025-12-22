using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.Common
{
	public interface IBindableAsyncValue<TValue> : INotifyPropertyChanged, ICommonDisposable, IDisposable
	{
		bool HasValueBeenLoaded { get; }

		bool IsValueInvalid { get; }

		bool IsReading { get; }

		bool IsWriting { get; }

		TValue LastValue { get; }

		TValue Value { get; set; }

		Task LoadAsync(CancellationToken cancellationToken);

		Task SaveAsync(CancellationToken cancellationToken);

		void Dispose(bool isDisposing);
	}
}
