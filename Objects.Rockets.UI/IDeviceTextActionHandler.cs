namespace Objects.Rockets.UI;

public interface IDeviceTextActionHandler
{
	void DeviceTextClicked(DeviceText deviceText);

	void DeviceTextBeginHover(DeviceText deviceText);

	void DeviceTextEndHover();
}
