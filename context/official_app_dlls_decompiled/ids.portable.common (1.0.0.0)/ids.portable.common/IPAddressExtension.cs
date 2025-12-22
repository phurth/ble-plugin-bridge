using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace IDS.Portable.Common
{
	public static class IPAddressExtension
	{
		public static async Task<IPAddress> ParseNameAsync(this string name)
		{
			IPAddress result = default(IPAddress);
			object obj;
			int num;
			try
			{
				result = IPAddress.Parse(name);
				return result;
			}
			catch (Exception ex)
			{
				obj = ex;
				num = 1;
			}
			if (num == 1)
			{
				Exception ipParseException = (Exception)obj;
				try
				{
					if (name == null)
					{
						throw new ArgumentNullException("name");
					}
					return Enumerable.First(await Dns.GetHostAddressesAsync(name));
				}
				catch
				{
					throw ipParseException;
				}
			}
			return result;
		}
	}
}
