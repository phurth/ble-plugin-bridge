using System;
using System.Collections.Generic;
using System.Text;
using IDS.Core.Tasks;

namespace IDS.Core.IDS_CAN
{
	public class LocalTextConsole : Disposable, ITextConsole
	{
		private static readonly TimeSpan FRAME_TRANSMIT_PERIOD = TimeSpan.FromSeconds(1.0);

		private PeriodicTask mBackgroundTask;

		private readonly string[] _strings;

		private readonly byte[][] _bytes;

		private readonly int _maxColIndex = -1;

		private readonly int _maxRowIndex;

		private bool _restart = true;

		private Timer TimeSinceLastFrameTx = new Timer();

		private int txRow = -1;

		private int txCol = -1;

		public bool IsDetected => true;

		IDevice ITextConsole.Device => Device;

		public ILocalDevice Device { get; private set; }

		public TEXT_CONSOLE_SIZE Size { get; private set; }

		public IReadOnlyList<string> Lines
		{
			get
			{
				return _strings;
			}
			set
			{
				for (int i = 0; i < _strings.Length; i++)
				{
					string text = string.Empty;
					if (i < value?.Count)
					{
						text = value[i];
					}
					if (text == null)
					{
						text = string.Empty;
					}
					if (text != _strings[i])
					{
						lock (_strings)
						{
							_strings[i] = text;
							_bytes[i] = null;
							_restart = true;
						}
					}
				}
			}
		}

		public LocalTextConsole(ILocalDevice localDevice, TEXT_CONSOLE_SIZE size)
		{
			Device = localDevice;
			Size = size;
			_maxColIndex = size.Width / 8 - 1;
			if (size.Width % 8 > 0)
			{
				_maxColIndex++;
			}
			_maxRowIndex = size.Height - 1;
			_strings = new string[size.Height];
			_bytes = new byte[size.Height][];
			for (int i = 0; i < _strings.Length; i++)
			{
				_strings[i] = string.Empty;
			}
			mBackgroundTask = new PeriodicTask(BackgroundTask, TimeSpan.FromMilliseconds(5.0), TimeSpan.FromMilliseconds(500.0), PeriodicTask.Type.FixedDelay);
		}

		public override void Dispose(bool disposing)
		{
			if (disposing)
			{
				mBackgroundTask.Dispose();
			}
		}

		private void BackgroundTask()
		{
			if (!Device.IsOnline)
			{
				return;
			}
			if (_restart || TimeSinceLastFrameTx.ElapsedTime >= FRAME_TRANSMIT_PERIOD)
			{
				txRow = _maxRowIndex;
				txCol = _maxColIndex;
				_restart = false;
				TimeSinceLastFrameTx.Reset();
			}
			if (txRow >= 0 && TransmitTextMessage(txRow, txCol))
			{
				if (txCol > 0)
				{
					txCol--;
					return;
				}
				txCol = _maxColIndex;
				txRow--;
			}
		}

		private byte ByteAt(int x, int y)
		{
			if (y >= _strings.Length)
			{
				return 32;
			}
			if (_strings[y] == null)
			{
				return 32;
			}
			if (_strings[y] == string.Empty)
			{
				return 32;
			}
			byte[] array = _bytes[y];
			if (array == null)
			{
				array = (_bytes[y] = Encoding.UTF8.GetBytes(_strings[y]));
			}
			if (x >= array.Length)
			{
				return 32;
			}
			return array[x];
		}

		private bool TransmitTextMessage(int txRow, int txCol)
		{
			lock (_strings)
			{
				int num = txCol * 8;
				int num2 = (txCol + 1) * 8;
				int length = 8;
				if (num2 > Size.Width)
				{
					length = 8 - (num2 - Size.Width);
				}
				CAN.PAYLOAD payload = new CAN.PAYLOAD(length);
				for (int i = 0; i < payload.Length; i++)
				{
					payload[i] = ByteAt(num + i, txRow);
				}
				byte ext_data = (byte)((txRow << 3) | txCol);
				return Device.Transmit29((byte)132, ext_data, ADDRESS.BROADCAST, payload);
			}
		}
	}
}
