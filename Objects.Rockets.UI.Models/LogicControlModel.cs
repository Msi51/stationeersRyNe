using System.Collections.Generic;

namespace Objects.Rockets.UI.Models;

public struct LogicControlModel
{
	public long SelectedDeviceReferenceId;

	public List<DeviceModel> Devices;

	public List<LogicValueModel> LogicValues;
}
