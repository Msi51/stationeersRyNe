using System;
using System.Text;
using System.Text.RegularExpressions;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using TMPro;
using UI.Tooltips;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.UI;

public class EditorLineOfCode : UserInterfaceBase, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IComparable<EditorLineOfCode>
{
	private const string _cr = "\n";

	public TMP_InputField InputField;

	public TextMeshProUGUI InputText;

	public TextMeshProUGUI FormattedText;

	public TextMeshProUGUI LineNumber;

	public bool IsUsingReference;

	public bool IsVoidLine;

	public bool WaitToClear;

	private static EditorLineOfCode _currentLine;

	public InputSourceCode Parent;

	public string Text
	{
		get
		{
			return InputField.text;
		}
		set
		{
			InputField.text = value;
		}
	}

	public static EditorLineOfCode CurrentLine
	{
		get
		{
			return _currentLine;
		}
		set
		{
			if (!(_currentLine == value))
			{
				_currentLine = value;
				if (!(_currentLine == null))
				{
					_currentLine.InputField.ActivateInputField();
				}
			}
		}
	}

	private void Awake()
	{
		InputField.onValueChanged.AddListener(TextChanged);
	}

	public void TextChanged(string text)
	{
		ReformatText();
	}

	public void ReformatText()
	{
		string text = InputField.text;
		text = text.Replace("\n", string.Empty);
		InputText.text = text;
		ReformatText(text);
		Parent.UpdateFileSize();
	}

	public void ReformatText(string inputString)
	{
		bool num = Localization.ParseDefines(Parent.LinesOfCode, ref Parent.AcceptedStrings, ref Parent.AcceptedJumps);
		if (string.IsNullOrEmpty(inputString.TrimEnd()))
		{
			FormattedText.text = string.Empty;
		}
		else
		{
			string input = inputString.TrimEnd();
			input = Regex.Replace(input, "([<>])", "<noparse>$1</noparse>");
			FormattedText.text = Localization.ParseScript(input, ref Parent.AcceptedStrings, ref Parent.AcceptedJumps, this);
			IsVoidLine = false;
		}
		if (!num)
		{
			return;
		}
		foreach (EditorLineOfCode item in Parent.LinesOfCode)
		{
			if (!(item.LineNumber == LineNumber))
			{
				item.FormattedText.text = Localization.ParseScript(item.Text, ref Parent.AcceptedStrings, ref Parent.AcceptedJumps, item);
			}
		}
	}

	public int GetIndex()
	{
		return RectTransform.GetSiblingIndex();
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		base.OnPointerEnter(eventData);
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		base.OnPointerExit(eventData);
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		CurrentLine = this;
	}

	public int CompareTo(EditorLineOfCode other)
	{
		if (other.RectTransform.GetSiblingIndex() < RectTransform.GetSiblingIndex())
		{
			return 1;
		}
		if (other.RectTransform.GetSiblingIndex() > RectTransform.GetSiblingIndex())
		{
			return -1;
		}
		return 0;
	}

	public void Activate()
	{
		InputField.ActivateInputField();
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		CurrentLine = this;
	}

	public void OnPointerUp(PointerEventData eventData)
	{
	}

	public void HandleUpdate()
	{
		Vector3 mousePosition = Input.mousePosition;
		if (!RectTransformUtility.RectangleContainsScreenPoint(FormattedText.rectTransform, mousePosition))
		{
			return;
		}
		int num = TMP_TextUtilities.FindIntersectingWord(FormattedText, Input.mousePosition, null);
		if (num == -1)
		{
			return;
		}
		string word = FormattedText.textInfo.wordInfo[num].GetWord();
		if (num != 0)
		{
			return;
		}
		ScriptCommand[] values = EnumCollections.ScriptCommands.Values;
		foreach (ScriptCommand scriptCommand in values)
		{
			if (!LogicBase.IsDeprecated(scriptCommand))
			{
				string value = EnumCollections.ScriptCommands.GetName(scriptCommand);
				if (word.Equals(value, StringComparison.InvariantCultureIgnoreCase))
				{
					StringBuilder stringBuilder = new StringBuilder();
					string commandExample = ProgrammableChip.GetCommandExample(scriptCommand);
					stringBuilder.Append("<color=white>").Append("<b>").Append("Instruction")
						.AppendLine("</b></color>");
					stringBuilder.Append("<i>").Append(commandExample).AppendLine("</i>");
					StringManager.WrapLineLength(stringBuilder, ProgrammableChip.GetCommandDescription(scriptCommand), 70, "grey");
					UITooltipManager.SetTooltip(stringBuilder);
					break;
				}
			}
		}
	}
}
