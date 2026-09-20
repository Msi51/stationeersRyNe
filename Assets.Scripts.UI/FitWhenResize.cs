using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class FitWhenResize : UIBehaviour
{
	[SerializeField]
	[ReadOnly]
	private RectTransform _rectTransform;

	[SerializeField]
	private RectTransform _transformToFit;

	protected override void OnRectTransformDimensionsChange()
	{
		_transformToFit.sizeDelta = _rectTransform.sizeDelta;
	}
}
