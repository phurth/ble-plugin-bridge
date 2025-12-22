using System;
using System.Runtime.CompilerServices;

namespace IDS.Portable.Common
{
	public class ConvertObjectToEnumException : Exception
	{
		public ConvertObjectToEnumException(Type type, object fromValue)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(33, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Unable to convert ");
			defaultInterpolatedStringHandler.AppendFormatted<object>(fromValue);
			defaultInterpolatedStringHandler.AppendLiteral(" into the enum ");
			defaultInterpolatedStringHandler.AppendFormatted(type.Name);
			base._002Ector(defaultInterpolatedStringHandler.ToStringAndClear());
		}
	}
}
