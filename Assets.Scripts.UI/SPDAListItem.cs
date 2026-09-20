using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class SPDAListItem : UserInterfaceBase
{
	public Image BackImage;

	public Button InsertsButton;

	public Image InsertImage;

	public TextMeshProUGUI InsertTitle;

	public UserInterfaceBase SubObject;

	public TextMeshProUGUI SubText;

	public TextMeshProUGUI SubValue;

	public Sprite NormalButton;

	public Sprite SpecialButton;

	public void Apply(string title)
	{
		InsertTitle.text = Stationpedia.Trim(title);
		if (SubObject != null)
		{
			SubObject.SetVisible(isVisble: false);
		}
	}

	public void SetSpecial()
	{
		BackImage.sprite = SpecialButton;
	}

	public void SetNormal()
	{
		BackImage.sprite = NormalButton;
	}
}
