using System;
using System.Collections.Generic;

namespace IDS.Portable.Common.Manifest
{
	public interface IManifestDevice : IComparable<IManifestDevice>
	{
		string Name { get; }

		ushort TypeID { get; }

		byte Instance { get; }

		string FunctionName { get; }

		ushort FunctionTypeID { get; }

		string FunctionClass { get; }

		byte FunctionInstance { get; }

		int Capabilities { get; }

		uint Circuit { get; }

		bool IsOnline { get; }

		Dictionary<string, string> CustomAttribute { get; set; }

		Dictionary<ushort, IManifestPid>? Pids { get; set; }

		void SetCustomAttribute(string attribute, string value);

		string? TryGetCustomAttribute(string attribute);
	}
}
