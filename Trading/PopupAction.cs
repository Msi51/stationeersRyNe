using System;
using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UI;

namespace Trading;

public class PopupAction : ActionData
{
	public const string DELAY_ATTRIBUTE = "Delay";

	[XmlAttribute("Delay")]
	public float Delay;

	public const string POP_UP_TITLE_ELEMENT = "Title";

	[XmlElement("Title")]
	public LocalizedStringReference PopupTitle;

	public const string POP_UP_TEXT_ELEMENT = "Text";

	[XmlElement("Text")]
	public LocalizedStringReference PopupText;

	public override string XElementName { get; }

	public override bool Execute<T>(T t, Entity player)
	{
		WaitThenPopup((int)(Delay * 1000f)).Forget();
		return true;
	}

	private async UniTaskVoid WaitThenPopup(int delayMs)
	{
		await UniTask.Delay(delayMs);
		if (GameManager.GameState != GameState.None)
		{
			Singleton<ConfirmationPanel>.Instance.Show(PopupTitle.Key, PopupText.Key, "OkayConfirmation");
		}
	}

	public override bool Execute(ref GasMixture tradable, int totalQuantitySold)
	{
		throw new NotImplementedException();
	}

	public override int GetChecksum()
	{
		return (PopupTitle.GetChecksum() * 41) ^ (PopupText.GetChecksum() * 41);
	}

	public override void Add(ref XElement parentElement, string actionElementName)
	{
		throw new NotImplementedException();
	}
}
