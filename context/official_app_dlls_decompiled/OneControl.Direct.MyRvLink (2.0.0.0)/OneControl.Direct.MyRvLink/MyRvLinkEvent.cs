using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public abstract class MyRvLinkEvent<TEvent> : IMyRvLinkEvent, IEquatable<TEvent> where TEvent : IMyRvLinkEvent
	{
		protected const int EventTypeIndex = 0;

		public abstract MyRvLinkEventType EventType { get; }

		protected abstract int MinPayloadLength { get; }

		protected abstract byte[] _rawData { get; }

		protected void ValidateEventRawDataBasic(IReadOnlyList<byte> rawData)
		{
			if (rawData == null)
			{
				throw new ArgumentNullException("rawData");
			}
			if (rawData.Count < MinPayloadLength)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(EventType);
				defaultInterpolatedStringHandler.AppendLiteral(" received less then ");
				defaultInterpolatedStringHandler.AppendFormatted(MinPayloadLength);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (EventType != (MyRvLinkEventType)rawData[0])
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(EventType);
				defaultInterpolatedStringHandler.AppendLiteral(" event type doesn't match ");
				defaultInterpolatedStringHandler.AppendFormatted(EventType);
				defaultInterpolatedStringHandler.AppendLiteral(": ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
		}

		public IReadOnlyList<byte> Encode()
		{
			return new ArraySegment<byte>(_rawData, 0, _rawData.Length);
		}

		public bool Equals(TEvent other)
		{
			if (other == null)
			{
				return false;
			}
			if (this == (object)other)
			{
				return true;
			}
			if (!((object)other is MyRvLinkEvent<TEvent> myRvLinkEvent))
			{
				return false;
			}
			if (!Enumerable.SequenceEqual(_rawData, myRvLinkEvent._rawData))
			{
				return false;
			}
			return true;
		}

		public override bool Equals(object? obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (this == obj)
			{
				return true;
			}
			if (obj!.GetType() != typeof(TEvent))
			{
				return false;
			}
			return Equals((TEvent)obj);
		}

		public override int GetHashCode()
		{
			return 17.Hash(_rawData);
		}
	}
}
