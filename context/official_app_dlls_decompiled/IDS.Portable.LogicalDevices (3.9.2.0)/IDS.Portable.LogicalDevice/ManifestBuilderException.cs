using System;

namespace IDS.Portable.LogicalDevice
{
	public class ManifestBuilderException : Exception
	{
		public ManifestBuilderException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
