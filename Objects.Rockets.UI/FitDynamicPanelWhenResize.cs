using Assets.Scripts;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Objects.Rockets.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class FitDynamicPanelWhenResize : UIBehaviour
{
	[SerializeField]
	[ReadOnly]
	private RectTransform _rectTransform;

	[SerializeField]
	private RectTransform _transformToFit;

	private Vector2 _extraSize;

	public void SetExtraSize(Vector2 extraSize)
	{
		_extraSize = extraSize;
	}

	protected override void OnRectTransformDimensionsChange()
	{
		_transformToFit.sizeDelta = _rectTransform.sizeDelta + _extraSize;
	}
}
