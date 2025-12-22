using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public static class DtcIdExtension
	{
		public static string? TryGetUserVisibleTextEnglish(this DTC_ID dtcId)
		{
			switch (dtcId)
			{
			case DTC_ID.IGNITION_NOT_ACTIVE:
				return "Ignition must be active to operate this device";
			case DTC_ID.PARK_BRAKE_NOT_ENGAGED:
				return "Park brake must be set to operate this device.";
			case DTC_ID.BATTERY_TOO_LOW_TO_OPERATE:
				return "Voltage is too low to operate this device.";
			case DTC_ID.SWITCH_ACTIVE_ONLY_VIA_HARDWARE:
				return "Device unavailable, physical switches are active.";
			case DTC_ID.SURESHADE_GENERAL_FAULT:
				return "General Fault.";
			case DTC_ID.WIND_EVENT_AUTO_OPERATION_IN_PROGRESS:
				return "Your Lippert One Wind Sensor has retracted your awning due to a wind event.  If this is unexpected, consider decreasing the sensitivity level.";
			case DTC_ID.WIND_EVENT_AUTO_OPERATION_COMPLETE:
				return "Your Lippert One Wind Sensor has retracted your awning due to a wind event.\u00a0 If this is unexpected, consider decreasing the sensitivity level.";
			case DTC_ID.AUTO_OPERATION_CANCELED:
				return "Your awning's automatic operation was stopped by a user.";
			case DTC_ID.WIND_SENSOR_1_COMM_FAILURE:
			case DTC_ID.WIND_SENSOR_2_COMM_FAILURE:
			case DTC_ID.WIND_SENSOR_3_COMM_FAILURE:
			case DTC_ID.WIND_SENSOR_4_COMM_FAILURE:
			case DTC_ID.WIND_SENSOR_5_COMM_FAILURE:
			case DTC_ID.WIND_SENSOR_6_COMM_FAILURE:
			case DTC_ID.WIND_SENSOR_7_COMM_FAILURE:
			case DTC_ID.WIND_SENSOR_8_COMM_FAILURE:
			case DTC_ID.WIND_SENSOR_9_COMM_FAILURE:
			case DTC_ID.WIND_SENSOR_10_COMM_FAILURE:
			case DTC_ID.WIND_SENSOR_11_COMM_FAILURE:
			case DTC_ID.WIND_SENSOR_12_COMM_FAILURE:
			case DTC_ID.WIND_SENSOR_13_COMM_FAILURE:
			case DTC_ID.WIND_SENSOR_14_COMM_FAILURE:
			case DTC_ID.WIND_SENSOR_15_COMM_FAILURE:
			case DTC_ID.WIND_SENSOR_16_COMM_FAILURE:
				return "Problem communicating with Lippert One Wind Sensor. Check your connection if problem persists.";
			default:
				return null;
			}
		}

		public static bool IsWhitelisted(this DTC_ID dtcId)
		{
			if (dtcId.TryGetUserVisibleTextEnglish() != null)
			{
				return true;
			}
			switch (dtcId)
			{
			case DTC_ID.NVM_FAILURE:
			case DTC_ID.CONFIGURATION_FAILURE:
			case DTC_ID.BATTERY_VOLTAGE_LOW:
			case DTC_ID.BATTERY_VOLTAGE_HIGH:
			case DTC_ID.LEVELER_ZERO_POINT_NOT_CONFIGURED:
			case DTC_ID.REMOTE_SENSOR_COMM_FAILURE:
			case DTC_ID.REMOTE_SENSOR_POWER_SHORT_TO_GROUND:
			case DTC_ID.REMOTE_SENSOR_FAILURE:
			case DTC_ID.FRONT_REMOTE_SENSOR_COMM_FAILURE:
			case DTC_ID.REAR_REMOTE_SENSOR_COMM_FAILURE:
			case DTC_ID.HALL_EFFECT_POWER_PLUS_SHORT_TO_GND:
			case DTC_ID.HALL_EFFECT_POWER_MINUS_SHORT_TO_BATT:
			case DTC_ID.JACK_LF_OPEN:
			case DTC_ID.JACK_LF_OVER_CURRENT:
			case DTC_ID.JACK_LF_HALL_EFFECT_SIGNAL_LOST:
			case DTC_ID.JACK_LF_POSITION_LOST:
			case DTC_ID.JACK_LM_OPEN:
			case DTC_ID.JACK_LM_OVER_CURRENT:
			case DTC_ID.JACK_LM_HALL_EFFECT_SIGNAL_LOST:
			case DTC_ID.JACK_LM_POSITION_LOST:
			case DTC_ID.JACK_LR_OPEN:
			case DTC_ID.JACK_LR_OVER_CURRENT:
			case DTC_ID.JACK_LR_HALL_EFFECT_SIGNAL_LOST:
			case DTC_ID.JACK_LR_POSITION_LOST:
			case DTC_ID.JACK_RF_OPEN:
			case DTC_ID.JACK_RF_OVER_CURRENT:
			case DTC_ID.JACK_RF_HALL_EFFECT_SIGNAL_LOST:
			case DTC_ID.JACK_RF_POSITION_LOST:
			case DTC_ID.JACK_RM_OPEN:
			case DTC_ID.JACK_RM_OVER_CURRENT:
			case DTC_ID.JACK_RM_HALL_EFFECT_SIGNAL_LOST:
			case DTC_ID.JACK_RM_POSITION_LOST:
			case DTC_ID.JACK_RR_OPEN:
			case DTC_ID.JACK_RR_OVER_CURRENT:
			case DTC_ID.JACK_RR_HALL_EFFECT_SIGNAL_LOST:
			case DTC_ID.JACK_RR_POSITION_LOST:
			case DTC_ID.TOUCH_PAD_POWER_PLUS_SHORT_TO_GND:
			case DTC_ID.TOUCH_PAD_POWER_MINUS_SHORT_TO_BATT:
			case DTC_ID.WATER_INTRUSION_DETECTED_IN_TOUCHPAD:
			case DTC_ID.OPERATING_VOLTAGE_DROPOUT:
				return true;
			default:
				return false;
			}
		}
	}
}
