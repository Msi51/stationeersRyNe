using System.Text.RegularExpressions;
using Assets.Scripts;
using Assets.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI;

public class CopyToClipboardHandler : UserInterfaceBase, IPointerClickHandler, IEventSystemHandler
{
	[SerializeField]
	private TextMeshProUGUI _textMesh;

	public void OnPointerClick(PointerEventData eventData)
	{
		if (StripFormatting(_textMesh.text, out var stripped))
		{
			GameManager.Clipboard = stripped;
			if (!Stationpedia.Instance.BaseAnimator.GetBool("Copied"))
			{
				Stationpedia.Instance.BaseAnimator.SetBool("Copied", value: true);
			}
		}
	}

	private bool StripFormatting(string original, out string stripped)
	{
		stripped = string.Empty;
		if (string.IsNullOrWhiteSpace(original))
		{
			return false;
		}
		Regex regex = new Regex("<[^>]*>");
		if (regex.IsMatch(original))
		{
			stripped = regex.Replace(original, string.Empty);
			return true;
		}
		return false;
	}
}
