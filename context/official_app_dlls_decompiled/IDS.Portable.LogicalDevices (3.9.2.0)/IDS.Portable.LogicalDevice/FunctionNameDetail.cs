namespace IDS.Portable.LogicalDevice
{
	public readonly struct FunctionNameDetail
	{
		public FunctionNameDetailLocation Location { get; }

		public FunctionNameDetailPosition Position { get; }

		public FunctionNameDetailRoom Room { get; }

		public FunctionNameDetailUse Use { get; }

		public FunctionNameDetail(FunctionNameDetailLocation location, FunctionNameDetailPosition position, FunctionNameDetailRoom room, FunctionNameDetailUse use)
		{
			Location = location;
			Position = position;
			Room = room;
			Use = use;
		}
	}
}
