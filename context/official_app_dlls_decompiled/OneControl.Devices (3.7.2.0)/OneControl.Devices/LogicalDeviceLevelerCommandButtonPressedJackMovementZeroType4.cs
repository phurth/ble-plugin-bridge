using System.Collections.Generic;

namespace OneControl.Devices
{
	public class LogicalDeviceLevelerCommandButtonPressedJackMovementZeroType4 : LogicalDeviceLevelerCommandButtonPressedType4<LogicalDeviceLevelerButtonJackMovementZeroType4>
	{
		protected List<LogicalDeviceLevelerScreenType4> ValidScreens { get; } = new List<LogicalDeviceLevelerScreenType4> { LogicalDeviceLevelerScreenType4.JackMovementZero };


		public bool IsZeroPointSet => (base.ButtonsPressed & LogicalDeviceLevelerButtonJackMovementZeroType4.SetZeroPoint) != 0;

		protected override bool IsScreenSupported(LogicalDeviceLevelerScreenType4 screenSelected)
		{
			return ValidScreens.Contains(screenSelected);
		}

		public LogicalDeviceLevelerCommandButtonPressedJackMovementZeroType4(LogicalDeviceLevelerButtonJackMovementZeroType4 buttonsPressed, int commandResponseTimeMs = 200)
			: base(LogicalDeviceLevelerScreenType4.JackMovementZero, buttonsPressed, commandResponseTimeMs)
		{
		}

		public LogicalDeviceLevelerCommandButtonPressedJackMovementZeroType4(LevelerJackDirection direction, LevelerJackLocation location, bool setZeroPoint, int commandResponseTimeMs = 200)
			: this(LogicalDeviceLevelerJackMovementExtensionType4.MakeJackMovement(direction, location), setZeroPoint, commandResponseTimeMs)
		{
		}

		public LogicalDeviceLevelerCommandButtonPressedJackMovementZeroType4(LevelJackControlCompositeIndividualType4 jackMovementComposite, bool setZeroPoint, int commandResponseTimeMs = 200)
			: this(jackMovementComposite.ToJackMovement(), setZeroPoint, commandResponseTimeMs)
		{
		}

		internal LogicalDeviceLevelerCommandButtonPressedJackMovementZeroType4(LogicalDeviceLevelerJackMovementType4 jackMovement, bool setZeroPoint, int commandResponseTimeMs)
			: this(jackMovement.ToButtonJackMovementZero(setZeroPoint), commandResponseTimeMs)
		{
		}

		public LevelerJackDirection JackDirection(LevelerJackLocation jackLocation)
		{
			return base.ButtonsPressed.ToJackMovement().JackDirection(jackLocation);
		}
	}
}
