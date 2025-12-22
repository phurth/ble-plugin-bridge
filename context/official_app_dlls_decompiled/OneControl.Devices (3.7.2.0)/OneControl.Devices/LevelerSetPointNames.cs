using OneControl.Devices.Leveler.Type5;

namespace OneControl.Devices
{
	public readonly struct LevelerSetPointNames
	{
		public SetPointType FactorySetPoint { get; }

		public SetPointType SetPoint1 { get; }

		public SetPointType SetPoint2 { get; }

		public SetPointType SetPoint3 { get; }

		public LevelerSetPointNames(SetPointType factorySetPoint, SetPointType setPoint1, SetPointType setPoint2, SetPointType setPoint3)
		{
			FactorySetPoint = factorySetPoint;
			SetPoint1 = setPoint1;
			SetPoint2 = setPoint2;
			SetPoint3 = setPoint3;
		}

		public override string ToString()
		{
			return $"Custom Set Point Names, Factory: {FactorySetPoint}, SetPoint1: {SetPoint1}, SetPoint2: {SetPoint2}, SetPoint3: {SetPoint3}";
		}
	}
}
