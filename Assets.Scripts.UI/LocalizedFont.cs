using TMPro;

namespace Assets.Scripts.UI;

public class LocalizedFont : Localized
{
	public TMP_FontAsset DefaultFont;

	public TMP_Text TextMesh;

	private void Awake()
	{
		DefaultFont = TextMesh.font;
		Localization.Register(this);
		Refresh();
	}

	private void OnDestroy()
	{
		Localization.Deregister(this);
	}

	public void Refresh()
	{
		if (!(TextMesh == null))
		{
			TextMesh.font = (Localization.CurrentFont ? Localization.CurrentFont : DefaultFont);
		}
	}
}
