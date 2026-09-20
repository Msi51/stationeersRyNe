using System.Text.RegularExpressions;
using Assets.Scripts.Objects.Electrical;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class HelpReference : UserInterfaceAnimated
{
	public TMP_Text Text;

	public TMP_Text Text2;

	public TMP_Text Description;

	public Button Button1;

	public Button Button2;

	public Button Button3;

	public Button Button4;

	public Image SaveType;

	public int ReferenceValue1;

	public int ReferenceValue2;

	public string TextString;

	public string TypeString;

	public string DescString;

	private const string CONSTANT_STRING = "Constant";

	private const string INSTRUCTION_STRING = "Instruction";

	public static readonly int CommandHash = Animator.StringToHash("Constant");

	public static readonly int InstructionHash = Animator.StringToHash("Instruction");

	public bool IsRegexMatch(string pattern)
	{
		if (!Match(pattern, TextString))
		{
			return Match(pattern, DescString);
		}
		return true;
	}

	public bool IsFirstWord(string pattern)
	{
		return Match(pattern, TextString);
	}

	private bool Match(string pattern, string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return false;
		}
		if (value.Length > 255)
		{
			return false;
		}
		return Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
	}

	public async UniTask SearchShow()
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		SetVisible(isVisble: true);
	}

	public void Setup(string title, string description, Sprite defaultItemImage, string referenceType, int ref1 = 0, int ref2 = 0)
	{
		Text.text = title;
		Text2.text = "<color=#808080>" + referenceType + "</color>";
		ReferenceValue1 = ref1;
		ReferenceValue2 = ref2;
		Description.text = description;
		SaveType.sprite = defaultItemImage;
		TextString = ProgrammableChip.StripColorTags(title);
		TypeString = ProgrammableChip.StripColorTags(referenceType);
		DescString = ProgrammableChip.StripColorTags(description);
	}

	public void Setup(ScriptCommand command, Sprite defaultItemImage)
	{
		Setup(ProgrammableChip.GetCommandExample(command), ProgrammableChip.GetCommandDescription(command), defaultItemImage, "Instruction", (int)command, CommandHash);
	}

	public void Setup(ProgrammableChip.Constant constant, Sprite defaultItemImage)
	{
		Setup(constant.GetName(), constant.Description, defaultItemImage, "Constant", constant.Hash, InstructionHash);
	}
}
