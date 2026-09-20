using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using LeTai.Asset.TranslucentImage;
using TMPro;
using UI.Tooltips;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class InputSourceCode : InputWindowBase, IModal
{
	public delegate void InputEvent(string result);

	public static InputSourceCode Instance;

	public TextMeshProUGUI SizeText;

	public TextMeshProUGUI TitleText;

	public TextMeshProUGUI InputType;

	public Canvas Canvas;

	public Image BackGroundFadeImage;

	public EditorLineOfCode LineOfCodePrefab;

	public RectTransform LineParent;

	public TranslucentImage Background;

	[SerializeField]
	private UiComponentRenderer CodeInputWindow;

	public static InputPanelState InputState;

	public RectTransform Window;

	public Button SubmitButton;

	public ProgrammableChipMotherboard PCM;

	public List<ScriptHelpWindow> HelpWindows = new List<ScriptHelpWindow>();

	public List<EditorLineOfCode> LinesOfCode;

	public const int MAX_FILE_SIZE = 4096;

	public const int MAX_LINES = 128;

	public const int LINE_LENGTH_LIMIT = 90;

	public List<string> AcceptedStrings = new List<string>();

	public List<string> AcceptedJumps = new List<string>();

	private float _defaultWidth;

	private bool _isOpaque = true;

	private int _acceptedStringsCount;

	private int _previousCaretPosition;

	private const char CHAR_ZERO_WIDTH_SPACE = '\u200b';

	private const char CHAR_SPACE = ' ';

	private int _fileSize;

	private bool _editorIsFocused;

	public override bool IsVisible => Canvas.enabled;

	private EditorLineOfCode CurrentLine
	{
		get
		{
			return EditorLineOfCode.CurrentLine;
		}
		set
		{
			EditorLineOfCode.CurrentLine = value;
		}
	}

	private int CaretPosition
	{
		get
		{
			if (!(CurrentLine == null))
			{
				return CurrentLine.InputField.caretPosition;
			}
			return 0;
		}
		set
		{
			if (CurrentLine != null)
			{
				CurrentLine.InputField.caretPosition = value;
			}
		}
	}

	private bool IsOpaque
	{
		get
		{
			return _isOpaque;
		}
		set
		{
			_isOpaque = value;
			Background.color = Background.color.SetAlpha(_isOpaque ? 1f : 0.75f);
			Background.spriteBlending = (_isOpaque ? 1f : 0.65f);
		}
	}

	public bool UnlockCursor => true;

	public static event InputEvent OnSubmit;

	public static event Event OnCancel;

	public void Awake()
	{
		_defaultWidth = Window.sizeDelta.x;
	}

	private void Start()
	{
		WorldManager.OnPaused += WorldManagerOnPaused;
	}

	protected override void CloseOnClientConnected(bool isPaused, string message)
	{
		if (isPaused && IsVisible)
		{
			Instance.ButtonInputSubmit();
		}
	}

	private void PauseGameToggle(bool pauseGame)
	{
		if (!NetworkManager.IsClient && NetworkBase.Clients.Count == 0)
		{
			WorldManager.SetGamePause(pauseGame);
		}
	}

	private void WorldManagerOnPaused(bool isOn)
	{
	}

	public override void SetVisible(bool isVisble)
	{
		base.SetVisible(isVisble);
		Canvas.enabled = isVisble;
		ClampWindowSize();
	}

	private void ClampWindowSize()
	{
		float num = Mathf.Min(Screen.width, _defaultWidth);
		if (num < 800f)
		{
			num = 800f;
		}
		Window.sizeDelta = new Vector2(num, Window.sizeDelta.y);
	}

	public override void SetActive(bool active)
	{
		base.SetActive(active);
		Canvas.enabled = active;
	}

	private static void SortLines()
	{
		Instance.LinesOfCode.Sort((EditorLineOfCode x, EditorLineOfCode y) => x.CompareTo(y));
		for (int num = 0; num < Instance.LinesOfCode.Count; num++)
		{
			Instance.LinesOfCode[num].LineNumber.text = $"{num}.";
		}
	}

	public override void Initialize()
	{
		base.Initialize();
		CodeInputWindow.gameObject.SetActive(value: true);
		Instance = this;
		BackGroundFadeImage.enabled = true;
		LinesOfCode = new List<EditorLineOfCode>(128);
		for (int i = 0; i < 128; i++)
		{
			EditorLineOfCode editorLineOfCode = UnityEngine.Object.Instantiate(LineOfCodePrefab, LineParent);
			editorLineOfCode.Parent = this;
			editorLineOfCode.LineNumber.text = $"{i}.";
			editorLineOfCode.name = $"~LineOfCode_{i}";
			LinesOfCode.Add(editorLineOfCode);
			editorLineOfCode.GetComponent<TMP_InputField>().characterLimit = 90;
		}
		if (LinesOfCode.Count > 0)
		{
			CurrentLine = LinesOfCode[0];
		}
		SortLines();
		UpdateFileSize();
		SetVisible(isVisble: false);
		foreach (ScriptHelpWindow helpWindow in HelpWindows)
		{
			helpWindow.Initialize();
		}
	}

	public void OnSave()
	{
		if (!(Instance.PCM == null))
		{
			Instance.PCM.SetSourceCode(Copy());
			Instance.SetVisible(isVisble: false);
			MouseModeController.RemoveModal(Instance);
		}
	}

	public static void CancelInput()
	{
		Instance.ButtonInputCancel();
	}

	public static void SubmitInput()
	{
		Instance.ButtonInputSubmit();
	}

	public void ButtonCopyToClipboard()
	{
		GameManager.Clipboard = Copy();
	}

	public void ButtonPasteFromClipboard()
	{
		Paste(GameManager.Clipboard);
	}

	public static void Paste(string value)
	{
		Instance.AcceptedStrings = new List<string>();
		if (string.IsNullOrEmpty(value))
		{
			value = string.Empty;
		}
		value = value.TrimEnd();
		string[] array = value.Split('\n');
		for (int i = 0; i < Instance.LinesOfCode.Count; i++)
		{
			EditorLineOfCode editorLineOfCode = Instance.LinesOfCode[i];
			string text = ((i >= array.Length) ? string.Empty : array[i].TrimEnd());
			text = AsciiString.ParseLine(text, 90);
			editorLineOfCode.InputField.text = text;
			editorLineOfCode.ReformatText(text);
		}
		Instance.UpdateFileSize();
	}

	public static string Copy()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (EditorLineOfCode item in Instance.LinesOfCode)
		{
			stringBuilder.AppendLine(AsciiString.ParseLine(item.InputField.text, 90));
		}
		string text = stringBuilder.ToString();
		if (text.Length > 4096)
		{
			text = text.Substring(0, 4096);
		}
		return text.TrimEnd();
	}

	public void ButtonSaveNew()
	{
		if (InputWindow.ShowInputPanel("Enter Save Name", string.Empty, 32))
		{
			InputWindow.OnSubmit += SaveNewWithName;
		}
	}

	public void ButtonOpacity()
	{
		IsOpaque = !IsOpaque;
	}

	public static void DeleteInstruction(string directoryName)
	{
		string text = Path.Combine(StationSaveUtils.GetSavePathScriptsSubDir().FullName, directoryName);
		if (!Directory.Exists(text))
		{
			return;
		}
		List<string> list = Directory.GetDirectories(text).ToList();
		list.Add(text);
		foreach (string item in list)
		{
			string[] files = Directory.GetFiles(item);
			for (int i = 0; i < files.Length; i++)
			{
				File.Delete(files[i]);
			}
			Directory.Delete(item);
		}
		ScriptHelpWindow.ScriptLibraryWindow.ReloadFileList();
	}

	private void SaveNewWithName(string filename, string description)
	{
		description = Regexes.CleanInvalidXmlChars(description);
		filename = Regexes.CleanInvalidXmlChars(filename);
		string path = filename.SanitizeFilename();
		string path2 = Path.Combine(StationSaveUtils.GetSavePathScriptsSubDir().FullName, path);
		if (Directory.Exists(path2))
		{
			DirectoryInfo instructionDirectory = new DirectoryInfo(path2);
			PromptPanel.Instance.ShowPrompt(PromptOverwriteStrings.Title, PromptOverwriteStrings.Body, PromptOverwriteStrings.Button, delegate
			{
				SaveNew(instructionDirectory, description);
			});
		}
		else
		{
			DirectoryInfo instructionDirectory2 = Directory.CreateDirectory(path2);
			SaveNew(instructionDirectory2, description);
		}
	}

	private void SaveNew(DirectoryInfo instructionDirectory, string description)
	{
		InstructionData instructionData = new InstructionData();
		instructionData.Title = instructionDirectory.Name;
		instructionData.Description = description;
		instructionData.Author = (NetworkManager.CurrentTransport.IsInitialised ? NetworkManager.Username : "Unknown");
		instructionData.Instructions = Copy();
		instructionData.SaveToFile(instructionDirectory);
		ScriptHelpWindow.ScriptLibraryWindow.ReloadFileList();
	}

	public void ButtonClear()
	{
		Paste(string.Empty);
	}

	public void ButtonInputCancel()
	{
		InputState = InputPanelState.Cancelled;
		Instance.SetVisible(isVisble: false);
		MouseModeController.RemoveModal(Instance);
		if (InputSourceCode.OnCancel != null)
		{
			InputSourceCode.OnCancel = null;
		}
		InputSourceCode.OnCancel = null;
		InputSourceCode.OnSubmit = null;
		Instance.PCM = null;
		InputState = InputPanelState.None;
	}

	public void ButtonInputSubmit()
	{
		InputState = InputPanelState.Submitted;
		Instance.SetVisible(isVisble: false);
		MouseModeController.RemoveModal(Instance);
		InputSourceCode.OnSubmit?.Invoke(Copy());
		InputSourceCode.OnCancel = null;
		InputSourceCode.OnSubmit = null;
		Instance.PCM = null;
		InputState = InputPanelState.None;
	}

	public static string GetLineText(string sourceCode)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < CountLines(sourceCode); i++)
		{
			stringBuilder.Append(StringManager.Get(i));
			stringBuilder.Append(".\n");
		}
		return stringBuilder.ToString();
	}

	private static int CountLines(string str)
	{
		if (str == null)
		{
			throw new ArgumentNullException("str");
		}
		if (str == string.Empty)
		{
			return 0;
		}
		int num = -1;
		int num2 = 0;
		while (-1 != (num = str.IndexOf("\n", num + 1, StringComparison.Ordinal)))
		{
			num2++;
		}
		return num2 + 1;
	}

	public static bool ShowInputPanel(string title, string defaultText, TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard)
	{
		if (InputState != InputPanelState.None)
		{
			return false;
		}
		Instance.SetVisible(isVisble: true);
		MouseModeController.AddModal(Instance);
		Instance.CodeInputWindow.SetVisible(isVisble: true);
		InputState = InputPanelState.Waiting;
		Instance.TitleText.text = title;
		Paste(defaultText);
		Instance.InputType.text = contentType.ToString().ToProper() + " Input";
		WaitForInput().Forget();
		return true;
	}

	public override void OnDisable()
	{
		base.OnDisable();
		UITooltipCanvas.Instance?.DoUpdate();
	}

	private void HandleInput()
	{
		EditorLineOfCode currentLine = CurrentLine;
		if (Input.GetKeyDown(KeyCode.Home))
		{
			CaretPosition = 0;
		}
		if (Input.GetKeyDown(KeyCode.End))
		{
			CaretPosition = CurrentLine.Text.Length;
		}
		if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
		{
			CaretPosition = _previousCaretPosition;
			int caretPosition = CaretPosition;
			string text = CurrentLine.Text;
			if (caretPosition < 0)
			{
				return;
			}
			string text2 = text.Substring(0, caretPosition);
			string text3 = text.Substring(caretPosition);
			text3 = text3.Replace('\u200b', ' ');
			int siblingIndex = currentLine.GetIndex() + 1;
			EditorLineOfCode editorLineOfCode = LinesOfCode[127];
			editorLineOfCode.RectTransform.SetSiblingIndex(siblingIndex);
			CurrentLine.Text = text2;
			CurrentLine.ReformatText();
			CurrentLine = editorLineOfCode;
			CurrentLine.Text = text3;
			UpdateFileSize();
			SortLines();
		}
		if (Input.GetKeyDown(KeyCode.Backspace) && _previousCaretPosition == 0)
		{
			int index = CurrentLine.GetIndex();
			if (index <= 0)
			{
				return;
			}
			CurrentLine = LinesOfCode[index - 1];
			CaretPosition = CurrentLine.Text.Length;
			index = CurrentLine.GetIndex();
			CurrentLine.Text += LinesOfCode[index + 1].Text;
			if (CurrentLine.Text.Length > 90)
			{
				CurrentLine.Text = CurrentLine.Text.Substring(0, 90);
			}
			LinesOfCode[index + 1].Text = null;
			RemoveLine(1);
		}
		if (Input.GetKeyDown(KeyCode.Delete) && CaretPosition >= CurrentLine.Text.Length - 1)
		{
			int index2 = CurrentLine.GetIndex();
			if (index2 >= 127)
			{
				return;
			}
			CurrentLine.Text += LinesOfCode[index2 + 1].Text;
			if (CurrentLine.Text.Length > 90)
			{
				CurrentLine.Text = CurrentLine.Text.Substring(0, 90);
			}
			LinesOfCode[index2 + 1].Text = null;
			RemoveLine(1);
		}
		if (Input.GetKeyDown(KeyCode.DownArrow))
		{
			int index3 = CurrentLine.GetIndex();
			index3++;
			if (index3 < LinesOfCode.Count)
			{
				CurrentLine = LinesOfCode[index3];
				CaretPosition = _previousCaretPosition;
				return;
			}
		}
		if (Input.GetKeyDown(KeyCode.UpArrow))
		{
			int index4 = CurrentLine.GetIndex();
			index4--;
			if (index4 >= 0)
			{
				CurrentLine = LinesOfCode[index4];
				CaretPosition = _previousCaretPosition;
				return;
			}
		}
		if (AcceptedStrings.Count != _acceptedStringsCount)
		{
			_acceptedStringsCount = AcceptedStrings.Count;
			ForceRefresh();
		}
		_previousCaretPosition = CaretPosition;
		foreach (EditorLineOfCode item in LinesOfCode)
		{
			item.HandleUpdate();
		}
	}

	public void UpdateFileSize()
	{
		_fileSize = 0;
		int num = 0;
		for (int num2 = LinesOfCode.Count - 1; num2 >= 0; num2--)
		{
			num = num2;
			if (LinesOfCode[num2].Text.Length > 0)
			{
				break;
			}
		}
		for (int i = 0; i < LinesOfCode.Count; i++)
		{
			EditorLineOfCode editorLineOfCode = LinesOfCode[i];
			_fileSize += editorLineOfCode.Text.Length;
			if (i < LinesOfCode.Count - 1 && i < num)
			{
				_fileSize++;
				_fileSize++;
			}
		}
		SizeText.text = GameStrings.CodeEditorFileSize.AsString(StringManager.Get(_fileSize), StringManager.Get(4096));
		SizeText.color = ((_fileSize > 4096) ? Color.red : Color.white);
		SubmitButton.interactable = _fileSize <= 4096;
	}

	public void Update()
	{
		if (InputState == InputPanelState.Waiting && _editorIsFocused)
		{
			HandleInput();
		}
		_editorIsFocused = CurrentLine.InputField.isFocused;
		UITooltipCanvas.Instance.DoUpdate();
		if ((float)Screen.width < Window.sizeDelta.x)
		{
			ClampWindowSize();
		}
	}

	private void RemoveLine(int direction)
	{
		EditorLineOfCode currentLine = EditorLineOfCode.CurrentLine;
		if (currentLine == null)
		{
			return;
		}
		int num = currentLine.GetIndex() + direction;
		if (num >= 0 && num < 128)
		{
			EditorLineOfCode editorLineOfCode = LinesOfCode[num];
			if (!(editorLineOfCode == null) && string.IsNullOrEmpty(editorLineOfCode.InputField.text.TrimEnd()))
			{
				editorLineOfCode.RectTransform.SetAsLastSibling();
				UpdateFileSize();
				SortLines();
			}
		}
	}

	private static async UniTask WaitForInput()
	{
		while (InputState == InputPanelState.Waiting)
		{
			await UniTask.NextFrame();
		}
		InputState = InputPanelState.None;
	}

	private void ForceRefresh()
	{
		AcceptedStrings.Clear();
		AcceptedJumps.Clear();
		Localization.ParseDefines(LinesOfCode, ref AcceptedStrings, ref AcceptedJumps);
		foreach (EditorLineOfCode item in LinesOfCode)
		{
			if (item.IsUsingReference)
			{
				string text = item.InputField.text;
				text = Regex.Replace(text, "([<>])", "<noparse>$1</noparse>");
				item.FormattedText.text = Localization.ParseScript(text, ref AcceptedStrings, ref AcceptedJumps);
			}
		}
	}
}
