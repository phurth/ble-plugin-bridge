using System;

namespace IDS.Portable.Common
{
	public interface IEndPointConnectionWithSerialization : IEndPointConnection, IComparable, IJsonSerializerClass
	{
	}
}
