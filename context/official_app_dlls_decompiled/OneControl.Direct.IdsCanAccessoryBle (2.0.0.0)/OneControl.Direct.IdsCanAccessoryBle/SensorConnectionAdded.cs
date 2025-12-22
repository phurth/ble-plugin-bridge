using OneControl.Direct.IdsCanAccessoryBle.Connections;

namespace OneControl.Direct.IdsCanAccessoryBle
{
	public delegate void SensorConnectionAdded(ISensorConnection sensorConnection, bool newlyLinked, bool requestSave);
}
