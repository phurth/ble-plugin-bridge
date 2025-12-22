using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IDS.Core.Tasks;

namespace IDS.Core.IDS_CAN
{
	public interface IBlockClient
	{
		Task<Tuple<RESPONSE, IReadOnlyList<BLOCK_ID>>> ReadBlockListAsync(AsyncOperation operation, IDevice target);

		Task<Tuple<RESPONSE, IBlock>> ReadBlockPropertiesAsync(AsyncOperation operation, IDevice target, BLOCK_ID block);

		Task<Tuple<RESPONSE, uint?>> RecalculateBlockCrcAsync(AsyncOperation operation, IDevice target, BLOCK_ID block);

		Task<Tuple<RESPONSE, IReadOnlyList<byte>>> ReadBlockDataAsync(AsyncOperation operation, IDevice target, BLOCK_ID block, int bulk_xfer_delay_ms, ISessionClient session);

		Task<Tuple<RESPONSE, IReadOnlyList<byte>>> ReadBlockDataAsync(AsyncOperation operation, IBlock block, int bulk_xfer_delay_ms, ISessionClient session);

		Task<RESPONSE> WriteBlockDataAsync(AsyncOperation operation, IBlock block, IReadOnlyList<byte> data, int bulk_xfer_delay_ms, ISessionClient session);
	}
}
