using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OneControl.Devices.TPMS
{
	public interface ITpms
	{
		Task<IReadOnlyList<TpmsSensorConfiguration>> BlockReadSensorConfigurationsAsync(TpmsGroupId groupId, CancellationToken cancellationToken);

		Task<List<TpmsVehicleConfiguration>> BlockReadAllVehicleConfigurationsAsync(CancellationToken cancellationToken);

		Task<TpmsVehicleConfiguration> BlockReadVehicleConfigurationAsync(TpmsGroupId groupId, CancellationToken cancellationToken);

		Task<byte> BlockReadMaxSensorsSupportedAsync(CancellationToken cancellationToken);

		Task<byte> BlockReadMaxVehicleConfigsSupportedAsync(CancellationToken cancellationToken);

		Task<byte> BlockReadMaxSensorsPerVehicleConfigAsync(CancellationToken cancellationToken);

		Task<byte> BlockReadConfiguredSensorCountAsync(CancellationToken cancellationToken);

		Task<WriteSensorConfigurationResponseCode> WriteSensorConfigurationAsync(uint lowPressureLimit, uint highPressureLimit, int relativeTempLimit, int highTempLimit, byte tireIndex, TpmsGroupId groupId, CancellationToken cancellationToken);

		Task<LearnSensorResponseCode> LearnSensorAsync(LearnSensorOpCode opCode, TpmsGroupId groupId, byte tireIndex, CancellationToken cancellationToken);

		Task<LearnSensorResponseCode> FactoryResetRepeaterAsync(CancellationToken cancellationToken);

		Task<TpmsVehicleConfiguration> ReadVehicleConfigurationAsync(TpmsGroupId groupId, CancellationToken cancellationToken);

		Task WriteVehicleConfigurationAsync(TpmsVehicleConfiguration vehicleConfig, CancellationToken cancellationToken);

		Task<LearnSensorResponseCode> RemoveVehicleConfigurationAsync(LearnSensorOpCode opCode, TpmsGroupId groupId, CancellationToken cancellationToken);

		TpmsGroupId? GetActiveTrailer();

		bool TrySaveActiveTrailer(TpmsGroupId activeTrailer);

		void UpdateSoftwarePartNumber(string softwarePartNumber);

		Task<string> GetRepeaterAssemblyPartNumberAsync(CancellationToken cancellationToken);
	}
}
