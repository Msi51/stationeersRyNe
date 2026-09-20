using System.Collections.Generic;

namespace Objects.Rockets.UI.Models;

public struct RocketPanelModel
{
	public RocketModel SelectedRocketModel;

	public List<ConnectedRocketModel> ConnectedRocketModels;
}
