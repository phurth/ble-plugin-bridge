using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkHostDebug : MyRvLinkEvent<MyRvLinkHostDebug>
	{
		public override MyRvLinkEventType EventType => MyRvLinkEventType.HostDebug;

		protected override int MinPayloadLength => 0;

		protected override byte[] _rawData { get; }

		protected MyRvLinkHostDebug(IReadOnlyList<byte> rawData)
		{
			if (rawData == null)
			{
				_rawData = Array.Empty<byte>();
				return;
			}
			int count = rawData.Count;
			_rawData = new byte[count];
			for (int i = 0; i < count; i++)
			{
				_rawData[i] = rawData[i];
			}
		}

		public static MyRvLinkHostDebug Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkHostDebug(rawData);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(1, 2);
			defaultInterpolatedStringHandler.AppendFormatted(EventType);
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendFormatted(_rawData.DebugDump());
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
