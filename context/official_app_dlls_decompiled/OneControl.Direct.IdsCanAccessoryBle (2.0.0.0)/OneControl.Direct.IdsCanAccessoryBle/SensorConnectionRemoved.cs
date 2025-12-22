using OneControl.Direct.IdsCanAccessoryBle.Connections;

namespace OneControl.Direct.IdsCanAccessoryBle
{
	public delegate void SensorConnectionRemoved(ISensorConnection sensorConnection, bool newRemoval, bool requestSave);
}
