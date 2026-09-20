using Assets.Scripts.Objects.Items;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI;

public class InWorldUiAudioComponent : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler
{
	public Button Button;

	public Assets.Scripts.Objects.Items.Motherboard ParentMotherBoard;

	private static readonly int WorldButtonClickHash = Animator.StringToHash("WorldButtonClick");

	private static readonly int WorldButtonHoverHash = Animator.StringToHash("WorldButtonHover");

	private void Start()
	{
		Button.onClick.AddListener(OnClick);
	}

	private void OnClick()
	{
		if (!(ParentMotherBoard == null))
		{
			Singleton<AudioManager>.Instance.PlayAudioClipsData(ParentMotherBoard, WorldButtonClickHash, ParentMotherBoard.UiAudioOffSet);
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (!(ParentMotherBoard == null))
		{
			Singleton<AudioManager>.Instance.PlayAudioClipsData(ParentMotherBoard, WorldButtonHoverHash, ParentMotherBoard.UiAudioOffSet);
		}
	}
}
