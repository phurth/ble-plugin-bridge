using System;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;

namespace OneControl.Devices
{
	public interface IRvKind : IEquatable<IRvKind>, IJsonSerializable, IJsonSerializerClass
	{
		RvDetail<int>? Year { get; }

		RvDetail<string>? Make { get; }

		RvDetail<string>? Model { get; }

		RvDetail<string>? FloorPlan { get; }

		bool IsHardwareRead { get; }

		string? Vin { get; }

		bool IsHardwareReadVin { get; }

		bool LciIdsAreSupplied();

		bool IsAllInformationSupplied();

		bool ValuesAreSupplied();
	}
}
