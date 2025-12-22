namespace IDS.Portable.Common.Color
{
	public struct NativeColor
	{
		public HsvColor AsHsvColor { get; }

		public RgbColor AsRgbColor { get; }

		public NativeColor(HsvColor hsvColor)
		{
			AsHsvColor = hsvColor;
			AsRgbColor = hsvColor.ToRgb();
		}

		public NativeColor(RgbColor rgbColor)
		{
			AsHsvColor = rgbColor.ToHsv();
			AsRgbColor = rgbColor;
		}
	}
}
