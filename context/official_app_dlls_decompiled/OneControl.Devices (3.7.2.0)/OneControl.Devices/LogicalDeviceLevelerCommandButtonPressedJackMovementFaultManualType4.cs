using System.Collections.Generic;

namespace OneControl.Devices
{
	public class LogicalDeviceLevelerCommandButtonPressedJackMovementFaultManualType4 : LogicalDeviceLevelerCommandButtonPressedType4<LogicalDeviceLevelerButtonJackMovementFaultManualType4>
	{
		protected List<LogicalDeviceLevelerScreenType4> ValidScreens { get; } = new List<LogicalDeviceLevelerScreenType4>
		{
			LogicalDeviceLevelerScreenType4.JackMovementFaultManual,
			LogicalDeviceLevelerScreenType4.JackMovementFaultManualConsole
		};


		public bool IsAutoRetractSet => (base.ButtonsPressed & LogicalDeviceLevelerButtonJackMovementFaultManualType4.AutoRetract) != 0;

		protected static LogicalDeviceLevelerScreenType4 MakeScreenType(bool withConsole)
		{
			if (!withConsole)
			{
				return LogicalDeviceLevelerScreenType4.JackMovementFaultManual;
			}
			return LogicalDeviceLevelerScreenType4.JackMovementFaultManualConsole;
		}

		protected override bool IsScreenSupported(LogicalDeviceLevelerScreenType4 screenSelected)
		{
			return ValidScreens.Contains(screenSelected);
		}

		public LogicalDeviceLevelerCommandButtonPressedJackMovementFaultManualType4(LogicalDeviceLevelerButtonJackMovementFaultManualType4 buttonsPressed, bool withConsole, int commandResponseTimeMs = 200)
			: base(MakeScreenType(withConsole), buttonsPressed, commandResponseTimeMs)
		{
		}

		public LogicalDeviceLevelerCommandButtonPressedJackMovementFaultManualType4(LevelerJackDirection direction, LevelerJackLocation location, bool setAutoRetract, bool withConsole, int commandResponseTimeMs = 200)
			: this(LogicalDeviceLevelerJackMovementExtensionType4.MakeJackMovement(direction, location), setAutoRetract, withConsole, commandResponseTimeMs)
		{
		}

		public LogicalDeviceLevelerCommandButtonPressedJackMovementFaultManualType4(LevelJackControlCompositeIndividualType4 jackMovementComposite, bool setAutoRetract, bool withConsole, int commandResponseTimeMs = 200)
			: this(jackMovementComposite.ToJackMovement(), setAutoRetract, withConsole, commandResponseTimeMs)
		{
		}

		public LogicalDeviceLevelerCommandButtonPressedJackMovementFaultManualType4(LevelJackControlCompositeIndividualAll jackMovementComposite, bool setAutoRetract, bool withConsole, int commandResponseTimeMs = 200)
			: this(jackMovementComposite.ToJackMovement(), setAutoRetract, withConsole, commandResponseTimeMs)
		{
		}

		internal LogicalDeviceLevelerCommandButtonPressedJackMovementFaultManualType4(LogicalDeviceLevelerJackMovementType4 jackMovement, bool setAutoRetract, bool withConsole, int commandResponseTimeMs)
			: this(jackMovement.ToButtonJackMovementFaultManual(setAutoRetract), withConsole, commandResponseTimeMs)
		{
		}

		public LevelerJackDirection JackDirection(LevelerJackLocation jackLocation)
		{
			return base.ButtonsPressed.ToJackMovement().JackDirection(jackLocation);
		}
	}
}
