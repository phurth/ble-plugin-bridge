using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.Common.COBS
{
	public class CobsStream : Stream, IAsyncValueStream
	{
		private const string LogTag = "CobsStream";

		private readonly object _locker = new object();

		private bool _isDisposed;

		private readonly Stream _stream;

		private readonly CobsEncoder _cobsEncoder;

		private readonly CobsDecoder _cobsDecoder;

		private int _readBufferOffset;

		private int _readBufferSize;

		private readonly byte[] _readBuffer = new byte[255];

		private readonly byte[] _writeBuffer = new byte[382];

		public override bool CanRead
		{
			get
			{
				if (_cobsDecoder != null)
				{
					return _stream.CanRead;
				}
				return false;
			}
		}

		public override bool CanWrite
		{
			get
			{
				if (_cobsEncoder != null)
				{
					return _stream.CanWrite;
				}
				return false;
			}
		}

		public override bool CanSeek => false;

		public override long Length
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public override long Position
		{
			get
			{
				throw new NotSupportedException();
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		public CobsStream(Stream stream, CobsEncoder encoder, CobsDecoder decoder)
		{
			_stream = stream;
			_cobsEncoder = encoder;
			_cobsDecoder = decoder;
		}

		public override void Close()
		{
			_stream.Close();
		}

		protected override void Dispose(bool disposing)
		{
			lock (_locker)
			{
				Close();
				_isDisposed = true;
			}
			base.Dispose(disposing);
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return ReadAsync(buffer, offset, count, cancellationToken, null).AsTask();
		}

		[AsyncStateMachine(typeof(_003CReadAsync_003Ed__13))]
		public ValueTask<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken, TimeSpan? readTimeout)
		{
			_003CReadAsync_003Ed__13 _003CReadAsync_003Ed__ = default(_003CReadAsync_003Ed__13);
			_003CReadAsync_003Ed__._003C_003Et__builder = AsyncValueTaskMethodBuilder<int>.Create();
			_003CReadAsync_003Ed__._003C_003E4__this = this;
			_003CReadAsync_003Ed__.buffer = buffer;
			_003CReadAsync_003Ed__.offset = offset;
			_003CReadAsync_003Ed__.count = count;
			_003CReadAsync_003Ed__.cancellationToken = cancellationToken;
			_003CReadAsync_003Ed__.readTimeout = readTimeout;
			_003CReadAsync_003Ed__._003C_003E1__state = -1;
			_003CReadAsync_003Ed__._003C_003Et__builder.Start(ref _003CReadAsync_003Ed__);
			return _003CReadAsync_003Ed__._003C_003Et__builder.Task;
		}

		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset");
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			if (_isDisposed)
			{
				throw new ObjectDisposedException("CobsStream");
			}
			ArraySegment<byte> arraySegment = new ArraySegment<byte>(buffer, offset, count);
			IReadOnlyList<byte> readOnlyList = _cobsEncoder.Encode(arraySegment);
			for (int i = 0; i < readOnlyList.Count; i++)
			{
				_writeBuffer[i] = readOnlyList[i];
			}
			await _stream.WriteAsync(_writeBuffer, 0, readOnlyList.Count, cancellationToken);
		}

		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			Flush();
			return Task.FromResult(true);
		}

		public override void Flush()
		{
			lock (_locker)
			{
				_readBufferOffset = 0;
				_readBufferSize = 0;
			}
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException("Use ReadAsync");
		}

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			throw new NotSupportedException("Use ReadAsync");
		}

		public override int EndRead(IAsyncResult asyncResult)
		{
			throw new NotSupportedException("Use ReadAsync");
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException("Use WriteAsync");
		}

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			throw new NotSupportedException("Use WriteAsync");
		}

		public override void EndWrite(IAsyncResult asyncResult)
		{
			throw new NotSupportedException("Use WriteAsync");
		}
	}
}
