using System.Runtime.CompilerServices;

namespace ids.portable.ble.Exceptions
{
	public class AccessoryCryptoCrcCheckException : BleServiceException
	{
		public AccessoryCryptoCrcCheckException(byte crc, byte expectedCrc)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(40, 2);
			defaultInterpolatedStringHandler.AppendLiteral("CRC Check Failed expected ");
			defaultInterpolatedStringHandler.AppendFormatted(expectedCrc);
			defaultInterpolatedStringHandler.AppendLiteral(" but received ");
			defaultInterpolatedStringHandler.AppendFormatted(crc);
			base._002Ector(defaultInterpolatedStringHandler.ToStringAndClear());
		}
	}
}
