namespace IDS.Portable.LogicalDevice
{
	public static class CommandResultEx
	{
		public static void ThrowOnError(this CommandResult commandResult, bool treatCancelAsError = true)
		{
			if (commandResult == CommandResult.Completed || (!treatCancelAsError && (commandResult == CommandResult.Canceled || commandResult == CommandResult.CanceledWithSameCommand)))
			{
				return;
			}
			throw new CommandResultException(commandResult);
		}
	}
}
