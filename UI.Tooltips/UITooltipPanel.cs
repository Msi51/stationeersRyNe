using Assets.Scripts.UI;
using TMPro;
using UnityEngine;

namespace UI.Tooltips;

public class UITooltipPanel : UserInterfaceBase
{
	[SerializeField]
	private TextMeshProUGUI _textMesh;

	[SerializeField]
	private float _offset;

	[SerializeField]
	private Canvas _canvas;

	private void UpdateLayout()
	{
		Vector3 mousePosition = Input.mousePosition;
		Vector3 quadrant = GetQuadrant(mousePosition);
		Vector3 vector = new Vector3(quadrant.x * (_textMesh.preferredWidth * 0.5f + _offset), quadrant.y * (_textMesh.preferredHeight * 0.5f + _offset));
		Transform.position = mousePosition - vector * _canvas.scaleFactor;
	}

	public void Show(string text)
	{
		_textMesh.text = text;
		SetActive(active: true);
		UpdateLayout();
	}

	public void Show(UITooltip tooltip)
	{
		Show(tooltip.TooltipText);
	}

	public void Hide()
	{
		SetActive(active: false);
		_textMesh.text = string.Empty;
	}

	public static Vector3 GetQuadrant(Vector3 mousePosition)
	{
		Vector3 vector = new Vector3((float)Screen.width * 0.5f, (float)Screen.height * 0.5f, 0f);
		Vector3 vector2 = mousePosition - vector;
		return new Vector3(Mathf.Sign(vector2.x), Mathf.Sign(vector2.y), 0f);
	}
}
