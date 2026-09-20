using UnityEngine;
using UnityEngine.UI;

public class AdaptablePreferred : LayoutElement, ILayoutElement
{
	[SerializeField]
	private RectTransform _contentToGrowWith;

	[SerializeField]
	private RectTransform _rectTransform;

	[SerializeField]
	private bool _usePreferredHeight;

	[SerializeField]
	private float _preferredHeightMax;

	[SerializeField]
	private bool _usePreferredWidth;

	[SerializeField]
	private float _preferredWidthMax;

	private float _preferredHeight;

	public override float preferredHeight => _preferredHeight;

	public override void CalculateLayoutInputVertical()
	{
		if (!(_contentToGrowWith == null))
		{
			if (_usePreferredHeight)
			{
				float num = LayoutUtility.GetPreferredHeight(_contentToGrowWith);
				_preferredHeight = ((_preferredHeightMax > num) ? num : _preferredHeightMax);
			}
			else
			{
				_preferredHeight = -1f;
			}
			if (_rectTransform != null)
			{
				_rectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x, _preferredHeight);
			}
		}
	}
}
