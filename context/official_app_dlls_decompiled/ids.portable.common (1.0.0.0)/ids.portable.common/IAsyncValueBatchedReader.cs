using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.Common
{
	public interface IAsyncValueBatchedReader<TValue>
	{
		Task<TValue> ReadValueAsync(CancellationToken cancellationToken, bool forceUpdate);
	}
}
