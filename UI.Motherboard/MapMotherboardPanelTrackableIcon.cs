using Assets.Scripts.UI;
using TerrainSystem;
using UI.ElementCollection;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Motherboard;

public class MapMotherboardPanelTrackableIcon : UserInterfaceBase, IUIElement<MapMotherboardPanelTrackableIconData>
{
	[SerializeField]
	private Image _image;

	public long Id { get; set; }

	public bool ShowingThisFrame { get; set; }

	public void DoUpdate(MapMotherboardPanelTrackableIconData data)
	{
		Vector3 worldPosition = data.WorldPosition;
		float num = 285f;
		float x = worldPosition.x / (float)VoxelConstants.Offset * num;
		float y = worldPosition.z / (float)VoxelConstants.Offset * num;
		Transform.localPosition = new Vector3(x, y, 0f);
		_image.color = data.Color;
		Transform.localScale = Vector3.one * (1f / data.Scale);
	}
}
