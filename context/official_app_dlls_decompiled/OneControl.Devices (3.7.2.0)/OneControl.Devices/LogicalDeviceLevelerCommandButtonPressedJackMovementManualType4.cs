using System.Collections.Generic;

namespace OneControl.Devices
{
	public class LogicalDeviceLevelerCommandButtonPressedJackMovementManualType4 : LogicalDeviceLevelerCommandButtonPressedType4<LogicalDeviceLevelerButtonJackMovementManualType4>
	{
		protected List<LogicalDeviceLevelerScreenType4> ValidScreens { get; } = new List<LogicalDeviceLevelerScreenType4>
		{
			LogicalDeviceLevelerScreenType4.JackMovementManual,
			LogicalDeviceLevelerScreenType4.JackMovementManualConsole
		};


		protected static LogicalDeviceLevelerScreenType4 MakeScreenType(bool withConsole)
		{
			if (!withConsole)
			{
				return LogicalDeviceLevelerScreenType4.JackMovementManual;
			}
			return LogicalDeviceLevelerScreenType4.JackMovementManualConsole;
		}

		protected override bool IsScreenSupported(LogicalDeviceLevelerScreenType4 screenSelected)
		{
			return ValidScreens.Contains(screenSelected);
		}

		public LogicalDeviceLevelerCommandButtonPressedJackMovementManualType4(LogicalDeviceLevelerButtonJackMovementManualType4 buttonsPressed, bool withConsole, int commandResponseTimeMs = 200)
			: base(MakeScreenType(withConsole), buttonsPressed, commandResponseTimeMs)
		{
		}

		public LogicalDeviceLevelerCommandButtonPressedJackMovementManualType4(LevelerJackDirection direction, LevelerJackLocation location, bool withConsole, int commandResponseTimeMs = 200)
			: this(LogicalDeviceLevelerJackMovementExtensionType4.MakeJackMovement(direction, location), withConsole, commandResponseTimeMs)
		{
		}

		public LogicalDeviceLevelerCommandButtonPressedJackMovementManualType4(LevelJackControlCompositeIndividualType4 jackMovementComposite, bool withConsole, int commandResponseTimeMs = 200)
			: this(jackMovementComposite.ToJackMovement(), withConsole, commandResponseTimeMs)
		{
		}

		internal LogicalDeviceLevelerCommandButtonPressedJackMovementManualType4(LogicalDeviceLevelerJackMovementType4 jackMovement, bool withConsole, int commandResponseTimeMs)
			: this(jackMovement.ToButtonJackMovementManual(), withConsole, commandResponseTimeMs)
		{
		}

		public LevelerJackDirection JackDirection(LevelerJackLocation jackLocation)
		{
			return base.ButtonsPressed.ToJackMovement().JackDirection(jackLocation);
		}
	}
}
