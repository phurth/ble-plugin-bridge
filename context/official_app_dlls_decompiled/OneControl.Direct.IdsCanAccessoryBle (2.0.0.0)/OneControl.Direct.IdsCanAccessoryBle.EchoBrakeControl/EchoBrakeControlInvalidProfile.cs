using System;
using System.Runtime.CompilerServices;
using OneControl.Devices.EchoBrakeControl;

namespace OneControl.Direct.IdsCanAccessoryBle.EchoBrakeControl
{
	public class EchoBrakeControlInvalidProfile : EchoBrakeControlException
	{
		public EchoBrakeControlInvalidProfile(EchoBrakeControlProfile profile, Exception? innerException = null)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(35, 1);
			defaultInterpolatedStringHandler.AppendLiteral("Invalid Echo Brake Control Profile ");
			defaultInterpolatedStringHandler.AppendFormatted(profile);
			base._002Ector(defaultInterpolatedStringHandler.ToStringAndClear(), innerException);
		}
	}
}
