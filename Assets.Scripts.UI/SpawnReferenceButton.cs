using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.UI;

public class SpawnReferenceButton : UserInterfaceAnimated
{
	[SerializeField]
	private SpawnReference _spawnReference;

	public override void OnPointerEnter(PointerEventData eventData)
	{
		_spawnReference.ShowTooltip();
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		_spawnReference.HideTooltip();
	}

	public void OnClick()
	{
		_spawnReference.ToggleSize();
	}
}
