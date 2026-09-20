using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Objects.Rockets.UI.Models;
using UnityEngine;

namespace Objects.Rockets.UI;

public class LogicControlPanel : UserInterfaceBase
{
	[SerializeField]
	private DeviceListPanel _deviceListPanel;

	[SerializeField]
	private LogicValueListPanel _logicValueListPanel;

	public void Initialize()
	{
		_deviceListPanel.Initialize();
		_logicValueListPanel.Initialize();
	}

	public void Show(RocketMotherboard motherboard)
	{
		_logicValueListPanel.Show(motherboard);
		_deviceListPanel.Show(motherboard);
	}

	public void Refresh(RocketUIModel model)
	{
		_deviceListPanel.Refresh(model);
		_logicValueListPanel.Refresh(model);
	}
}
