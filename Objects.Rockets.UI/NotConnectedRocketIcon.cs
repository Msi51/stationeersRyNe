using Assets.Scripts.UI;
using UI.Tooltips;
using UnityEngine;

namespace Objects.Rockets.UI;

public class NotConnectedRocketIcon : UserInterfaceBase
{
	[SerializeField]
	private Transform _imageTransform;

	[SerializeField]
	private UITooltip _tooltip;

	private MapView _mapView;

	private Rocket _rocket;

	public Rocket Rocket => _rocket;

	public void Initialize(Rocket rocket, MapView mapView)
	{
		_rocket = rocket;
		_mapView = mapView;
	}

	public void SetTooltip(string text)
	{
		if ((bool)_tooltip)
		{
			_tooltip.TooltipText = text;
		}
	}

	public void Offset(float x, float y)
	{
		_imageTransform.localPosition = new Vector3(x, y, 0f);
	}
}
