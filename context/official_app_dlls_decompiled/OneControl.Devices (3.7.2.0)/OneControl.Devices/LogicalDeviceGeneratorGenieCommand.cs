using System;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceGeneratorGenieCommand : LogicalDeviceCommandPacket
	{
		private const byte OffCommandCode = 1;

		private const byte OnCommandCode = 2;

		public bool IsOffCommand => base.Data[0] == 1;

		public bool IsOnCommand => base.Data[0] == 2;

		public GeneratorGenieCommand ToGeneratorGenieCommand
		{
			get
			{
				if (!IsOnCommand)
				{
					return GeneratorGenieCommand.Off;
				}
				return GeneratorGenieCommand.On;
			}
		}

		public LogicalDeviceGeneratorGenieCommand(GeneratorGenieCommand command)
			: base(0, 0, 200)
		{
			switch (command)
			{
			case GeneratorGenieCommand.Off:
				base.Data[0] = 1;
				break;
			case GeneratorGenieCommand.On:
				base.Data[0] = 2;
				break;
			default:
				throw new ArgumentException("Unknown command");
			}
		}

		public static LogicalDeviceGeneratorGenieCommand MakeOnCommand()
		{
			return new LogicalDeviceGeneratorGenieCommand(GeneratorGenieCommand.On);
		}

		public static LogicalDeviceGeneratorGenieCommand MakeOffCommand()
		{
			return new LogicalDeviceGeneratorGenieCommand(GeneratorGenieCommand.Off);
		}

		public LogicalDeviceGeneratorGenieCommand(byte data)
			: base(0, data)
		{
		}
	}
}
