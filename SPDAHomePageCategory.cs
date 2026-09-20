using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SPDAHomePageCategory : MonoBehaviour
{
	public Button ButtonRef;

	public Image ButtonImage;

	public Sprite DefaultImage;

	public TextMeshProUGUI ButtonTitle;

	private SPDAHomePageButtonOverride localData;

	public void SetUp(SPDAHomePageButtonOverride data)
	{
		localData = data;
		data.Prefab = this;
		ButtonRef.onClick.AddListener(delegate
		{
			Stationpedia.OpenAt(data.LinkedPageKey);
		});
		ButtonTitle.text = Stationpedia.Trim(Localization.GetInterface(data.CategoryName));
		Thing thing = Prefab.Find(data.ThingNameForImage);
		Sprite sprite = ((thing != null) ? thing.GetThumbnail() : DefaultImage);
		ButtonImage.sprite = ((sprite != null) ? sprite : DefaultImage);
	}
}
