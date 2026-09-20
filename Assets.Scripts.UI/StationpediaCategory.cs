using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class StationpediaCategory : UserInterfaceBase
{
	public RectTransform Contents;

	public RectTransform SecondContents;

	public Image CollapseImage;

	public TextMeshProUGUI Title;

	public Sprite VisibleImage;

	public Sprite NotVisibleImage;

	private static readonly int ExpandHash = Animator.StringToHash("SP_Expand");

	private static readonly int CollapseHash = Animator.StringToHash("SP_Collapse");

	public void ToggleContentVisibility()
	{
		if ((bool)Contents)
		{
			UIAudioManager.Play(Contents.gameObject.activeSelf ? CollapseHash : ExpandHash);
			Contents.gameObject.SetActive(!Contents.gameObject.activeSelf);
			if ((bool)SecondContents)
			{
				SecondContents.gameObject.SetActive(Contents.gameObject.activeSelf);
			}
			CollapseImage.sprite = ((!Contents.gameObject.activeSelf) ? NotVisibleImage : VisibleImage);
		}
	}

	public int GetChildCount()
	{
		return Contents.childCount;
	}

	public void ClearChildInserts()
	{
		int childCount = GetChildCount();
		if (childCount > 0)
		{
			for (int i = 0; i < childCount; i++)
			{
				Object.Destroy(Contents.GetChild(i).gameObject);
			}
		}
	}
}
