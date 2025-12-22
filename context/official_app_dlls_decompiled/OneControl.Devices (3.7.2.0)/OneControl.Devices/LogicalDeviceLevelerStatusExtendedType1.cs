using System;
using System.Text;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceLevelerStatusExtendedType1 : LogicalDeviceStatusPacketMutableExtended
	{
		private const int MinimumStatusPacketSize = 8;

		private const int MaxFragments = 4;

		private const int FragmentWaitingForFrameStart = -1;

		private readonly string[] _textFragments = new string[4];

		private string _text;

		public string Text
		{
			get
			{
				return _text;
			}
			set
			{
				SetBackingField(ref _text, value, "Text");
			}
		}

		public LogicalDeviceLevelerStatusExtendedType1()
			: base(8u)
		{
		}

		public override void DidUpdateData()
		{
			try
			{
				int extendedByte = ExtendedByte;
				if (extendedByte < 0 || extendedByte >= 4)
				{
					throw new ArgumentOutOfRangeException($"Unexpected/Unknown Extended byte (text fragment) {ExtendedByte}");
				}
				string @string = Encoding.ASCII.GetString(base.Data);
				if (string.CompareOrdinal(@string, _textFragments[extendedByte]) != 0)
				{
					_textFragments[extendedByte] = @string;
					Text = _textFragments[0] + _textFragments[1] + "\n" + _textFragments[2] + _textFragments[3];
				}
			}
			catch (Exception ex)
			{
				TaggedLog.Error("LogicalDeviceStatusPacketMutableExtended", "Unable to process extended message: " + ex.Message);
			}
			finally
			{
				base.DidUpdateData();
			}
		}
	}
}
