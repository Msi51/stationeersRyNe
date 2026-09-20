using Assets.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Objects.Rockets.UI;

public class MapSprite : UserInterfaceBase
{
	[SerializeField]
	private Image _image;

	public void Initialize(SpaceMapSpriteData data)
	{
		_image.sprite = data.MapDisplay.Icon.IconSprite;
		RectTransform.sizeDelta = Vector2.one * (MapLocation.DefaultSize * data.MapDisplay.Icon.Size);
		base.transform.localPosition = data.MapDisplay.Position;
	}
}
