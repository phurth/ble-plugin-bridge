using System;
using System.Collections.Generic;
using System.Text;

namespace IDS.Core.IDS_CAN
{
	internal class RemoteTextConsole : RemoteDevice.ChildNode, ITextConsole
	{
		public struct ROWCOL
		{
			public readonly byte Value;

			public readonly int Row;

			public readonly int Column;

			public ROWCOL(byte value)
			{
				Value = value;
				Row = (value >> 3) & 0x1F;
				Column = (value & 7) * 8;
			}

			public override string ToString()
			{
				return "[" + Row + "," + Column + "]";
			}
		}

		private class FrameScanner
		{
			private enum STATE
			{
				RESYNC,
				GET_SIZE,
				WAIT_EOF
			}

			private ROWCOL mCurrentRowCol = new ROWCOL(0);

			private TEXT_CONSOLE_SIZE mStableSize = new TEXT_CONSOLE_SIZE(0, 0);

			private TEXT_CONSOLE_SIZE mNextFrameSize = new TEXT_CONSOLE_SIZE(0, 0);

			private STATE mState;

			private bool mSizeChanged;

			private Timer mFrameTimeout = new Timer();

			private Timer mMessageTimeout = new Timer();

			private STATE State
			{
				get
				{
					return mState;
				}
				set
				{
					if (mState != value)
					{
						mState = value;
					}
				}
			}

			public TEXT_CONSOLE_SIZE Size => mStableSize;

			public bool SizeChanged
			{
				get
				{
					bool result = mSizeChanged;
					mSizeChanged = false;
					return result;
				}
			}

			public bool CheckForTimeout()
			{
				if (mStableSize.Width <= 0 && mStableSize.Height <= 0)
				{
					return false;
				}
				if (mFrameTimeout.ElapsedTime < FRAME_TIMEOUT)
				{
					return false;
				}
				mStableSize = (mNextFrameSize = new TEXT_CONSOLE_SIZE(0, 0));
				State = STATE.RESYNC;
				mSizeChanged = true;
				return true;
			}

			public bool DetectFrame(ROWCOL rc, int payload_bytes)
			{
				ROWCOL rOWCOL = mCurrentRowCol;
				mCurrentRowCol = rc;
				int num = rc.Row + 1;
				int num2 = rc.Column + payload_bytes;
				if (mMessageTimeout.ElapsedTime > MESSAGE_TIMEOUT)
				{
					State = STATE.RESYNC;
				}
				mMessageTimeout.Reset();
				switch (State)
				{
				case STATE.RESYNC:
					if (rc.Value == 0)
					{
						State = STATE.GET_SIZE;
					}
					return false;
				case STATE.GET_SIZE:
					mNextFrameSize = new TEXT_CONSOLE_SIZE(num2, num);
					State = STATE.WAIT_EOF;
					break;
				case STATE.WAIT_EOF:
					if (num2 > mNextFrameSize.Width || num > mNextFrameSize.Height)
					{
						State = STATE.RESYNC;
						return false;
					}
					if (rc.Value > rOWCOL.Value && (mNextFrameSize.Width != num2 || mNextFrameSize.Height != num))
					{
						State = STATE.RESYNC;
						return false;
					}
					break;
				}
				if (rc.Value > 0)
				{
					return false;
				}
				mFrameTimeout.Reset();
				mSizeChanged |= mStableSize != mNextFrameSize;
				mStableSize = mNextFrameSize;
				State = STATE.GET_SIZE;
				mNextFrameSize = new TEXT_CONSOLE_SIZE(0, 0);
				return true;
			}
		}

		private class LineBuilder
		{
			private readonly byte[] Char;

			private readonly byte[] UTF16;

			private readonly byte[] LatchedUTF16;

			private string mText = "";

			private bool mNeedsRebuild;

			public bool NeedsLatch { get; private set; }

			private int EOL
			{
				get
				{
					for (int i = 0; i < Char.Length; i++)
					{
						if (Char[i] == 0)
						{
							return i;
						}
					}
					return Char.Length - 1;
				}
			}

			public string Text
			{
				get
				{
					if (mNeedsRebuild)
					{
						lock (LatchedUTF16)
						{
							mText = Encoding.Unicode.GetString(LatchedUTF16, 0, EOL * 2);
							mNeedsRebuild = false;
						}
					}
					return mText;
				}
			}

			public byte this[int index]
			{
				get
				{
					return Char[index];
				}
				set
				{
					if (Char[index] != value)
					{
						Char[index] = value;
						ushort num = TEXT_CONSOLE.UnicodeCharset[value & 0xFF];
						UTF16[2 * index] = (byte)num;
						UTF16[2 * index + 1] = (byte)(num >> 8);
						NeedsLatch = true;
					}
				}
			}

			public LineBuilder(int capacity)
			{
				Char = new byte[capacity + 1];
				UTF16 = new byte[Char.Length * 2];
				LatchedUTF16 = new byte[UTF16.Length];
			}

			public void Latch()
			{
				if (NeedsLatch)
				{
					lock (LatchedUTF16)
					{
						Array.Copy(UTF16, LatchedUTF16, UTF16.Length);
						NeedsLatch = false;
						mNeedsRebuild = true;
					}
				}
			}

			public void Clear()
			{
				lock (LatchedUTF16)
				{
					Array.Clear(Char, 0, Char.Length);
					Array.Clear(UTF16, 0, UTF16.Length);
					Array.Clear(LatchedUTF16, 0, LatchedUTF16.Length);
					NeedsLatch = false;
					mText = "";
					mNeedsRebuild = false;
				}
			}
		}

		private static readonly TimeSpan FRAME_TIMEOUT = TimeSpan.FromSeconds(4.0);

		private static readonly TimeSpan MESSAGE_TIMEOUT = TimeSpan.FromSeconds(1.75);

		private static readonly string[] Empty = new string[0];

		private readonly TextConsoleSizeChangedEvent SizeChangedEvent;

		private readonly TextConsoleTextChangedEvent TextChangedEvent;

		private FrameScanner FrameScan = new FrameScanner();

		private LineBuilder[] Line = new LineBuilder[32];

		private string[] Strings = Empty;

		private bool RebuildStrings;

		IDevice ITextConsole.Device => base.Device;

		public bool IsDetected
		{
			get
			{
				TEXT_CONSOLE_SIZE size = Size;
				if (size.Width > 0)
				{
					return size.Height > 0;
				}
				return false;
			}
		}

		public IReadOnlyList<string> Lines
		{
			get
			{
				if (RebuildStrings)
				{
					lock (Line)
					{
						RebuildStrings = false;
						int height = Size.Height;
						if (Strings.Length != height)
						{
							Strings = new string[height];
						}
						for (int i = 0; i < Strings.Length; i++)
						{
							Strings[i] = Line[i].Text;
						}
					}
				}
				return Strings;
			}
		}

		public TEXT_CONSOLE_SIZE Size => FrameScan?.Size ?? default(TEXT_CONSOLE_SIZE);

		private bool AnyTextChanged
		{
			get
			{
				bool flag = false;
				int height = Size.Height;
				for (int i = 0; i < height; i++)
				{
					flag |= Line[i].NeedsLatch;
				}
				return flag;
			}
		}

		public RemoteTextConsole(RemoteDevice device)
			: base(device)
		{
			SizeChangedEvent = new TextConsoleSizeChangedEvent(device);
			TextChangedEvent = new TextConsoleTextChangedEvent(device);
			for (int i = 0; i < Line.Length; i++)
			{
				Line[i] = new LineBuilder(64);
			}
			TreeNode.RemoveFromParent();
			base.Text = "Text console: not detected";
			base.Icon = IDS.Core.IDS_CAN.Adapter.ICON.CROSS;
		}

		public override void Dispose(bool disposing)
		{
			if (disposing)
			{
				FrameScan = null;
				Strings = null;
				Line = null;
			}
		}

		private void RaiseSizeChanged()
		{
			if (Size.Height > 0)
			{
				base.Text = "Text console: " + Size.ToString();
				base.Icon = IDS.Core.IDS_CAN.Adapter.ICON.CHECKMARK;
				if (TreeNode.Parent == null)
				{
					base.Device.TreeNode.AddChild(TreeNode);
				}
			}
			else
			{
				TreeNode.RemoveFromParent();
			}
			SizeChangedEvent.Publish();
			base.Adapter.Events.Publish(SizeChangedEvent);
		}

		private void RaiseTextChanged()
		{
			TextChangedEvent.Publish();
			base.Adapter.Events.Publish(TextChangedEvent);
		}

		public override void BackgroundTask()
		{
			if (!FrameScan.CheckForTimeout())
			{
				return;
			}
			lock (Line)
			{
				RebuildStrings = false;
				for (int i = 0; i < Line.Length; i++)
				{
					Line[i].Clear();
				}
				Strings = Empty;
			}
			RaiseSizeChanged();
			RaiseTextChanged();
		}

		public override void OnDeviceTx(AdapterRxEvent tx)
		{
			if ((byte)tx.MessageType != 132)
			{
				return;
			}
			ROWCOL rc = new ROWCOL(tx.MessageData);
			LineBuilder lineBuilder = Line[rc.Row];
			for (int i = 0; i < tx.Count; i++)
			{
				lineBuilder[rc.Column + i] = tx[i];
			}
			if (tx.Count < 8)
			{
				lineBuilder[rc.Column + tx.Count] = 0;
			}
			if (!FrameScan.DetectFrame(rc, tx.Count))
			{
				return;
			}
			bool sizeChanged = FrameScan.SizeChanged;
			bool flag = AnyTextChanged;
			if (sizeChanged)
			{
				RaiseSizeChanged();
				flag = true;
			}
			if (!flag)
			{
				return;
			}
			lock (Line)
			{
				int height = Size.Height;
				for (int j = 0; j < height; j++)
				{
					Line[j].Latch();
				}
				RebuildStrings = true;
			}
			RaiseTextChanged();
		}
	}
}
