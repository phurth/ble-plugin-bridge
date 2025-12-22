using OneControl.Devices.EchoBrakeControl;

namespace OneControl.Direct.IdsCanAccessoryBle.EchoBrakeControl
{
	public static class EchoBrakeControlProfileExtension
	{
		public static int GetEchoBrakeProfileIndex(this EchoBrakeControlProfile profile)
		{
			switch (profile)
			{
			case EchoBrakeControlProfile.Profile1:
			case EchoBrakeControlProfile.Profile2:
			case EchoBrakeControlProfile.Profile3:
			case EchoBrakeControlProfile.Profile4:
			case EchoBrakeControlProfile.Profile5:
				return (int)profile;
			default:
				throw new EchoBrakeControlInvalidProfile(profile);
			}
		}
	}
}
