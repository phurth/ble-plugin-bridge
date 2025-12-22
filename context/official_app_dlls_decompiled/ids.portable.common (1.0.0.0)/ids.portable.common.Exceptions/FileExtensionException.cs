using System;

namespace ids.portable.common.Exceptions
{
	public class FileExtensionException : Exception
	{
		public FileExtensionException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
