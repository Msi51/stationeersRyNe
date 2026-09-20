using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI;

public class LocalizedText : Localized
{
	public string StringKey;

	[ReadOnly]
	public int StringHash;

	public TMP_Text TextMesh;

	public TMP_FontAsset DefaultFont;

	public bool Trim;

	private bool _initialized;

	private string _defaultText;

	public void Initialize()
	{
		if (!_initialized)
		{
			_defaultText = TextMesh.text;
			DefaultFont = TextMesh.font;
			Localization.Register(this);
			Refresh();
			_initialized = true;
		}
	}

	public string GetDefaultText()
	{
		return _defaultText;
	}

	private void Awake()
	{
		Initialize();
	}

	private void OnDestroy()
	{
		Localization.Deregister(this);
	}

	public void Refresh()
	{
		if (TextMesh == null || string.IsNullOrEmpty(StringKey))
		{
			return;
		}
		StringHash = ((!string.IsNullOrEmpty(StringKey)) ? Animator.StringToHash(StringKey) : 0);
		if (!Localization.InterfaceExists(StringHash))
		{
			string text = Localization.GetFallbackInterface(StringHash);
			if (string.IsNullOrEmpty(text))
			{
				text = StringKey;
			}
			TextMesh.SetText(text);
			TextMesh.font = (Localization.CurrentFont ? Localization.CurrentFont : DefaultFont);
		}
		else
		{
			string text2 = Localization.GetInterface(this);
			if (Trim)
			{
				text2 = Stationpedia.Trim(text2);
			}
			TextMesh.text = text2;
			TextMesh.font = (Localization.CurrentFont ? Localization.CurrentFont : DefaultFont);
		}
	}
}
