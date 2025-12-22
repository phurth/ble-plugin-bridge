using System;

namespace IDS.Portable.LogicalDevice
{
	public class CommandResultException : Exception
	{
		public CommandResult CommandResult { get; }

		public CommandResultException(CommandResult commandResult, Exception? innerException = null)
			: base($"Command error {commandResult}", innerException)
		{
			CommandResult = commandResult;
		}
	}
}
