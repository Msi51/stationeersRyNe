using Assets.Scripts.Localization2;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class SPDAManufacturer : UserInterfaceBase
{
	public TextMeshProUGUI PrinterNameTitle;

	public TextMeshProUGUI HeaderText;

	public TextMeshProUGUI ContentsText;

	public GameObject DetailsObject;

	public TextMeshProUGUI DetailsText;

	public Button ImageButton;

	public Image PrinterSprite;

	public void SetText(StationBuildCostInsert thingRecipeInfo)
	{
		if (!string.IsNullOrEmpty(thingRecipeInfo.Details))
		{
			DetailsText.text = thingRecipeInfo.Details;
			ContentsText.text = thingRecipeInfo.Description;
			DetailsObject.SetActive(value: true);
		}
		else
		{
			DetailsObject.SetActive(value: false);
		}
		if (!string.IsNullOrEmpty(thingRecipeInfo.Description))
		{
			HeaderText.text = GameStrings.SPDAManufacturerRequirements;
			ContentsText.text = thingRecipeInfo.Description;
			HeaderText.gameObject.SetActive(value: true);
			ContentsText.gameObject.SetActive(value: true);
		}
		else
		{
			HeaderText.gameObject.SetActive(value: false);
			ContentsText.gameObject.SetActive(value: false);
		}
		PrinterSprite.enabled = thingRecipeInfo.PrinterImage != null;
		PrinterSprite.sprite = thingRecipeInfo.PrinterImage;
	}
}
