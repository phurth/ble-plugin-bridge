using System;
using System.Runtime.CompilerServices;

namespace IDS.Core
{
	public class Statistics
	{
		private double Sum;

		private double SumSquared;

		private double mMean;

		private double mVariance;

		private double mStdev;

		private int CalcSamples;

		public int NumSamples { get; private set; }

		public double Min { get; private set; }

		public double Max { get; private set; }

		public double Mean
		{
			get
			{
				Calculate();
				return mMean;
			}
		}

		public double Variance
		{
			get
			{
				Calculate();
				return mVariance;
			}
		}

		public double Stdev
		{
			get
			{
				Calculate();
				return mStdev;
			}
		}

		public void Reset()
		{
			NumSamples = (CalcSamples = 0);
			Min = 0.0;
			Max = 0.0;
			Sum = 0.0;
			SumSquared = 0.0;
			mMean = 0.0;
			mVariance = 0.0;
			mStdev = 0.0;
		}

		public void AddSample(double value)
		{
			if (NumSamples++ == 0)
			{
				double num3 = (Min = (Max = (mMean = value)));
			}
			if (value < Min)
			{
				Min = value;
			}
			if (value > Max)
			{
				Max = value;
			}
			Sum += value;
			SumSquared += value * value;
		}

		private void Calculate()
		{
			int numSamples = NumSamples;
			if (CalcSamples != numSamples)
			{
				CalcSamples = numSamples;
				mMean = Sum / (double)numSamples;
				mVariance = SumSquared / (double)numSamples - mMean * mMean;
				mStdev = Math.Sqrt(mVariance);
			}
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(46, 6);
			defaultInterpolatedStringHandler.AppendLiteral("samples=");
			defaultInterpolatedStringHandler.AppendFormatted(NumSamples);
			defaultInterpolatedStringHandler.AppendLiteral(", min=");
			defaultInterpolatedStringHandler.AppendFormatted(Min);
			defaultInterpolatedStringHandler.AppendLiteral(", max=");
			defaultInterpolatedStringHandler.AppendFormatted(Max);
			defaultInterpolatedStringHandler.AppendLiteral(", mean=");
			defaultInterpolatedStringHandler.AppendFormatted(Mean);
			defaultInterpolatedStringHandler.AppendLiteral(", variance=");
			defaultInterpolatedStringHandler.AppendFormatted(Variance);
			defaultInterpolatedStringHandler.AppendLiteral(", stdev=");
			defaultInterpolatedStringHandler.AppendFormatted(Stdev);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
