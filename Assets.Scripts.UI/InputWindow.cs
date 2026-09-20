using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.UI;

public class InputWindow : InputWindowBase, IModal
{
	public TextMeshProUGUI TitleText;

	public TextMeshProUGUI InputText;

	public UserInterfaceBase TextUI;

	public TextMeshProUGUI InputMultiText;

	public UserInterfaceBase MultiTextUI;

	public TextMeshProUGUI InputType;

	public RectTransform MainPanel;

	[SerializeField]
	private TMP_InputField _singleLineInputField;

	[SerializeField]
	private TMP_InputField _multiLineInputField;

	public int MaxPreviousInput = 10;

	public UserInterfaceBase ImageScrollUp;

	public UserInterfaceBase ImageScrollDown;

	public static InputPanelState InputState = InputPanelState.None;

	private static string InputPanelOutput = string.Empty;

	private static bool OutputRequired = false;

	private static string InputPanelOutput2 = string.Empty;

	private static bool Output2Required = false;

	public static InputWindow Instance;

	private static Dictionary<TMP_InputField.ContentType, PreviousValues> _previousSelections = new Dictionary<TMP_InputField.ContentType, PreviousValues>();

	public static PreviousValues CurrentContent;

	private static bool _cursorDisabled;

	private static EnumCollection<TMP_InputField.ContentType, int> _contentTypes = new EnumCollection<TMP_InputField.ContentType, int>();

	private static Action<string, Thing, Labeller> _onSubmitRename = OnSubmitRename;

	private float _scrollData;

	public static string PreviousString;

	public bool UnlockCursor => true;

	public static event Action<string, string> OnSubmit;

	public static event Action OnCancel;

	public bool HasRequiredInputs()
	{
		if (OutputRequired && string.IsNullOrEmpty(InputPanelOutput))
		{
			return false;
		}
		if (Output2Required && string.IsNullOrEmpty(InputPanelOutput2))
		{
			return false;
		}
		return true;
	}

	public static void Register(TMP_InputField.ContentType contentType)
	{
		_previousSelections.TryGetValue(contentType, out var value);
		if (value == null)
		{
			value = new PreviousValues();
			_previousSelections.Add(contentType, value);
		}
		else
		{
			value.CurrentIndex = -1;
		}
		CurrentContent = value;
	}

	public static void Register(TMP_InputField.ContentType contentType, string value)
	{
		_previousSelections.TryGetValue(contentType, out var value2);
		if (value2 == null)
		{
			value2 = new PreviousValues();
			_previousSelections.Add(contentType, value2);
			return;
		}
		value2.CurrentIndex = -1;
		if (!value2.Values.Contains(value))
		{
			value2.Values.Insert(0, value);
			if (value2.Values.Count > Instance.MaxPreviousInput)
			{
				value2.Values.RemoveRange(Instance.MaxPreviousInput, value2.Values.Count - Instance.MaxPreviousInput);
			}
		}
	}

	public static void SetWidth(int width)
	{
		Instance.MainPanel.sizeDelta = new Vector2(width, Instance.MainPanel.sizeDelta.y);
	}

	public static void OnSubmitRename(string value, Thing thing, Labeller source)
	{
		if (string.IsNullOrEmpty(value))
		{
			value = thing.SourcePrefab.DisplayName;
		}
		if (!string.IsNullOrEmpty(value))
		{
			value = ((value.Length <= 200) ? value : value.Substring(0, 200));
		}
		if (!string.IsNullOrEmpty(value))
		{
			value = value.Replace("<", " ");
			value = value.Replace(">", " ");
			if (GameManager.RunSimulation)
			{
				Thing.RenameThing(thing.ReferenceId, value);
			}
			else
			{
				NetworkClient.RenameThing(thing.ReferenceId, value);
			}
			source.PlaySound(Labeller.LabelConfirmHash);
		}
	}

	public static async UniTaskVoid GetText(Assets.Scripts.Localization2.GameString title, Action<string, string> onSubmit, UnityAction<string> onValueChanged = null, string defaultText = "", int characterLimit = 256, TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard, int width = 800)
	{
		if (InputState == InputPanelState.None)
		{
			_cursorDisabled = CursorManager.IsLocked;
			InputState = InputPanelState.Waiting;
			Register(contentType);
			Instance._singleLineInputField.contentType = contentType;
			SetWidth(width);
			OutputRequired = true;
			Output2Required = false;
			MouseModeController.AddModal(Instance);
			Instance.SetVisible(isVisble: true);
			Instance.TextUI.SetVisible(isVisble: true);
			Instance.MultiTextUI.SetVisible(isVisble: false);
			Instance.TitleText.text = title.DisplayString;
			Instance.InputText.text = defaultText ?? string.Empty;
			Instance.InputMultiText.text = string.Empty;
			Instance._singleLineInputField.text = defaultText ?? string.Empty;
			PreviousString = defaultText;
			Instance.InputType.text = _contentTypes.GetName(contentType) + " Input";
			Instance._singleLineInputField.characterLimit = characterLimit;
			Instance._singleLineInputField.onValueChanged.RemoveAllListeners();
			if (onValueChanged != null)
			{
				Instance._singleLineInputField.onValueChanged.AddListener(onValueChanged);
			}
			Instance._singleLineInputField.ActivateInputField();
			Instance._singleLineInputField.Select();
			while (InputState == InputPanelState.Waiting && GameManager.GameState != GameState.None)
			{
				Instance.CheckScroll();
				await UniTask.NextFrame();
			}
			if (onValueChanged != null)
			{
				Instance._singleLineInputField.onValueChanged.RemoveListener(onValueChanged);
			}
			onSubmit?.Invoke(InputPanelOutput, InputPanelOutput2);
			InputState = InputPanelState.None;
		}
	}

	public static void ShowInputPanel(string title, string defaultText, Action<string, string> onConfirm, int width = 600)
	{
		if (InputState == InputPanelState.None)
		{
			_cursorDisabled = CursorManager.IsLocked;
			OnSubmit += onConfirm;
			InputState = InputPanelState.Waiting;
			Register(TMP_InputField.ContentType.Standard);
			Instance._singleLineInputField.contentType = TMP_InputField.ContentType.Standard;
			SetWidth(width);
			OutputRequired = true;
			Output2Required = false;
			Instance.SetVisible(isVisble: true);
			MouseModeController.AddModal(Instance);
			Instance.TextUI.SetVisible(isVisble: true);
			Instance.MultiTextUI.SetVisible(isVisble: false);
			Instance.TitleText.text = title;
			Instance.InputText.text = defaultText ?? string.Empty;
			Instance.InputMultiText.text = string.Empty;
			Instance._singleLineInputField.text = defaultText ?? string.Empty;
			PreviousString = defaultText;
			Instance.InputType.text = string.Empty;
			WaitForInput().Forget();
		}
	}

	[Obsolete("This overload is obsolete. Use ShowInputPanel with input delegate instead.", false)]
	public static bool ShowInputPanel(string title, string defaultText, int characterLimit = 256, TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard, int width = 600)
	{
		if (InputState != InputPanelState.None)
		{
			return false;
		}
		_cursorDisabled = CursorManager.IsLocked;
		InputState = InputPanelState.Waiting;
		Register(contentType);
		Instance._singleLineInputField.contentType = contentType;
		SetWidth(width);
		OutputRequired = true;
		Output2Required = false;
		Instance.SetVisible(isVisble: true);
		MouseModeController.AddModal(Instance);
		Instance.TextUI.SetVisible(isVisble: true);
		Instance.MultiTextUI.SetVisible(isVisble: true);
		Instance.TitleText.text = title;
		Instance.InputText.text = defaultText ?? string.Empty;
		Instance.InputMultiText.text = string.Empty;
		Instance._singleLineInputField.text = defaultText ?? string.Empty;
		PreviousString = defaultText;
		Instance.InputType.text = _contentTypes.GetName(contentType) + " Input";
		WaitForInput().Forget();
		return true;
	}

	[Obsolete("This overload is obsolete. Use ShowInputPanel with input delegate instead.", false)]
	public static bool ShowInputPanel(string title, int width = 600)
	{
		if (InputState != InputPanelState.None)
		{
			return false;
		}
		_cursorDisabled = CursorManager.IsLocked;
		InputState = InputPanelState.Waiting;
		Register(TMP_InputField.ContentType.Standard);
		Instance._singleLineInputField.contentType = TMP_InputField.ContentType.Standard;
		SetWidth(width);
		OutputRequired = false;
		Output2Required = true;
		Instance.SetVisible(isVisble: true);
		MouseModeController.AddModal(Instance);
		Instance.TextUI.SetVisible(isVisble: false);
		Instance.MultiTextUI.SetVisible(isVisble: true);
		Instance.TitleText.text = title;
		Instance.InputMultiText.text = string.Empty;
		Instance.InputType.text = "Standard Input";
		WaitForInput().Forget();
		return true;
	}

	[Obsolete("This overload is obsolete. Use ShowInputPanel with input delegate instead.", false)]
	public static bool ShowInputPanel(string title, string defaultText, Thing target, int characterLimit = 256, TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard, int width = 600)
	{
		if (InputState != InputPanelState.None)
		{
			return false;
		}
		_cursorDisabled = CursorManager.IsLocked;
		Instance.SetVisible(isVisble: true);
		MouseModeController.AddModal(Instance);
		Instance.TextUI.SetVisible(isVisble: true);
		Instance.MultiTextUI.SetVisible(isVisble: false);
		InputState = InputPanelState.Waiting;
		Register(contentType);
		Instance._singleLineInputField.contentType = contentType;
		SetWidth(width);
		OutputRequired = true;
		Output2Required = false;
		Instance.TitleText.text = title;
		Instance.InputText.text = defaultText ?? string.Empty;
		Instance._singleLineInputField.text = defaultText ?? string.Empty;
		Instance.InputMultiText.text = string.Empty;
		PreviousString = defaultText;
		Instance.InputType.text = _contentTypes.GetName(contentType) + " Input";
		WaitForInput(target).Forget();
		return true;
	}

	[Obsolete("This overload is obsolete. Use ShowInputPanel with input delegate instead.", false)]
	public static bool ShowInputPanel(string title, string defaultText, Thing target, DynamicThing source, int characterLimit = 256, TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard, int width = 600)
	{
		if (InputState != InputPanelState.None)
		{
			return false;
		}
		_cursorDisabled = CursorManager.IsLocked;
		Instance.SetVisible(isVisble: true);
		MouseModeController.AddModal(Instance);
		if (target is Sign)
		{
			Instance.TextUI.SetVisible(isVisble: false);
			Instance.MultiTextUI.SetVisible(isVisble: true);
			Instance._singleLineInputField.text = defaultText ?? string.Empty;
			Instance._multiLineInputField.text = defaultText ?? string.Empty;
		}
		else
		{
			Instance.TextUI.SetVisible(isVisble: true);
			Instance.MultiTextUI.SetVisible(isVisble: false);
			Instance._singleLineInputField.text = defaultText ?? string.Empty;
			Instance._multiLineInputField.text = defaultText ?? string.Empty;
		}
		InputState = InputPanelState.Waiting;
		Register(contentType);
		Instance._singleLineInputField.contentType = contentType;
		SetWidth(width);
		Instance.TitleText.text = title;
		PreviousString = defaultText;
		Instance._singleLineInputField.Select();
		Instance.InputType.text = _contentTypes.GetName(contentType) + " Input";
		WaitForInput(target, source).Forget();
		return true;
	}

	[Obsolete("This overload is obsolete. Use ShowInputPanel with input delegate instead.", false)]
	public static bool ShowInputPanel(string title, string defaultText, ISetable target, DynamicThing source, int characterLimit = 256, TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard, int width = 600)
	{
		if (InputState != InputPanelState.None)
		{
			return false;
		}
		_cursorDisabled = CursorManager.IsLocked;
		Instance.SetVisible(isVisble: true);
		MouseModeController.AddModal(Instance);
		Instance.TextUI.SetVisible(isVisble: true);
		Instance.MultiTextUI.SetVisible(isVisble: false);
		InputState = InputPanelState.Waiting;
		Register(contentType);
		Instance._singleLineInputField.contentType = contentType;
		SetWidth(width);
		Instance.TitleText.text = title;
		Instance.InputText.text = defaultText ?? string.Empty;
		Instance._singleLineInputField.text = defaultText ?? string.Empty;
		Instance.InputMultiText.text = string.Empty;
		PreviousString = defaultText;
		Instance.InputType.text = _contentTypes.GetName(contentType) + " Input";
		WaitForInput(target, source).Forget();
		return true;
	}

	protected override void CloseOnClientConnected(bool isPaused, string message)
	{
		if (IsVisible && isPaused)
		{
			CancelInput();
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

	public void ButtonInputCancel()
	{
		Instance.MultiTextUI.SetVisible(isVisble: false);
		InputPanelOutput = string.Empty;
		InputPanelOutput2 = string.Empty;
		InputState = InputPanelState.Cancelled;
		Instance.SetVisible(isVisble: false);
		MouseModeController.RemoveModal(Instance);
		InputWindow.OnCancel?.Invoke();
		InputWindow.OnCancel = null;
		InputWindow.OnSubmit = null;
		InputState = InputPanelState.None;
	}

	public void ButtonInputSubmit()
	{
		Instance.MultiTextUI.SetVisible(isVisble: false);
		InputPanelOutput = _singleLineInputField.text;
		InputPanelOutput2 = InputMultiText.text;
		if (!string.IsNullOrEmpty(InputPanelOutput))
		{
			char trimChar = InputPanelOutput.ToCharArray()[InputPanelOutput.Length - 1];
			if (InputPanelOutput.ToCharArray()[InputPanelOutput.Length - 1] == '\u200b')
			{
				InputPanelOutput = InputPanelOutput.TrimEnd(trimChar);
			}
		}
		InputState = InputPanelState.Submitted;
		Instance.SetVisible(isVisble: false);
		MouseModeController.RemoveModal(Instance);
		if (!HasRequiredInputs())
		{
			InputWindow.OnCancel?.Invoke();
		}
		else
		{
			Register(Instance._singleLineInputField.contentType, InputPanelOutput);
			if (InputWindow.OnSubmit != null)
			{
				InputWindow.OnSubmit(InputPanelOutput, InputPanelOutput2);
			}
		}
		InputWindow.OnCancel = null;
		InputWindow.OnSubmit = null;
		Singleton<GameManager>.Instance.StartCoroutine(WaitSetInputState());
	}

	private IEnumerator WaitSetInputState()
	{
		yield return Yielders.EndOfFrame;
		InputState = InputPanelState.None;
	}

	private static async UniTaskVoid WaitForInput(Thing target, DynamicThing source)
	{
		while (InputState == InputPanelState.Waiting)
		{
			if (source.RootParent != InventoryManager.Parent || target == null)
			{
				CancelInput();
				return;
			}
			Instance.CheckScroll();
			await UniTask.NextFrame();
		}
		InputState = InputPanelState.None;
	}

	private static async UniTaskVoid WaitForInput(ISetable target, DynamicThing source)
	{
		while (InputState == InputPanelState.Waiting)
		{
			if (source.RootParent != InventoryManager.Parent || target == null)
			{
				CancelInput();
				return;
			}
			Instance.CheckScroll();
			await UniTask.NextFrame();
		}
		InputState = InputPanelState.None;
	}

	private static async UniTaskVoid WaitForInput(Thing target)
	{
		while (InputState == InputPanelState.Waiting)
		{
			if (target == null)
			{
				CancelInput();
				return;
			}
			Instance.CheckScroll();
			await UniTask.NextFrame();
		}
		InputState = InputPanelState.None;
	}

	private static async UniTaskVoid WaitForInput()
	{
		while (InputState == InputPanelState.Waiting)
		{
			Instance.CheckScroll();
			await UniTask.NextFrame();
		}
		InputState = InputPanelState.None;
	}

	private void CheckScroll()
	{
		_scrollData = Input.mouseScrollDelta.y / 10f;
		if (CurrentContent.CurrentIndex == -1)
		{
			PreviousString = _singleLineInputField.text;
		}
		if (_scrollData < 0f && CurrentContent.Next())
		{
			Instance.InputText.text = CurrentContent.Text();
			Instance._singleLineInputField.text = CurrentContent.Text();
		}
		else if (_scrollData > 0f && CurrentContent.Previous())
		{
			Instance.InputText.text = CurrentContent.Text();
			Instance._singleLineInputField.text = CurrentContent.Text();
		}
		Instance.ImageScrollUp.SetVisible(!CurrentContent.IsAtMin() && CurrentContent.HasText());
		Instance.ImageScrollDown.SetVisible(!CurrentContent.IsAtMax() && CurrentContent.HasText());
	}

	public override void Initialize()
	{
		base.Initialize();
		Instance = this;
		SetVisible(isVisble: false);
	}

	public static bool IsMultiLine()
	{
		return Instance.MultiTextUI.IsVisible;
	}
}
