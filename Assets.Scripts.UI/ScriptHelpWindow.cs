using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networking.Transports;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class ScriptHelpWindow : DraggableWindow
{
	public HelpReference ReferencePrefab;

	public RectTransform FunctionTransform;

	public HelpMode HelpMode;

	public TMP_Text Description;

	public Sprite LocalImage;

	public Sprite WorkshopImage;

	private bool _isRebuilt;

	private List<HelpReference> _helpReferences = new List<HelpReference>();

	private bool _isOpaque;

	public Image Background;

	public TMP_InputField SearchField;

	public Sprite DefaultItemImage;

	public Sprite DefaultItemImage2;

	private UniTask searchWaitTask;

	public const float DELAY_AFTER_KEY = 0.25f;

	private float _waitForSearch;

	private UniTask _finishSearchRoutine;

	private CancellationTokenSource _searchRoutineCancel;

	private readonly Regex _searchRegex = new Regex("[^a-zA-Z0-9-]+", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

	public GameObject NoResultsFromSearchText;

	public static ScriptHelpWindow ScriptLibraryWindow;

	private CancellationTokenSource _tokenSource;

	private bool IsOpaque
	{
		get
		{
			return _isOpaque;
		}
		set
		{
			_isOpaque = value;
			if (Background != null)
			{
				Background.color = Background.color.SetAlpha(_isOpaque ? 1f : 0.75f);
			}
		}
	}

	public void ButtonOpacity()
	{
		IsOpaque = !IsOpaque;
	}

	public void Awake()
	{
		if (SearchField != null)
		{
			SearchField.onSubmit.AddListener(delegate
			{
				StartSearchNow();
			});
			SearchField.onSelect.AddListener(SearchBehaviour);
			SearchField.onValueChanged.AddListener(SearchBehaviour);
			SearchField.onSelect.AddListener(delegate
			{
				KeyManager.SetInputState("Stationpedia", KeyInputState.Typing);
			});
			SearchField.onDeselect.AddListener(delegate
			{
				KeyManager.RemoveInputState("Stationpedia");
			});
		}
	}

	private void SearchBehaviour(string inputText)
	{
		if (inputText.Length == 0)
		{
			ClearPreviousSearch();
		}
		else
		{
			StartSearchCountdown();
		}
	}

	private void StartSearchCountdown()
	{
		_waitForSearch = 0.25f;
		if (HelpMode == HelpMode.Functions)
		{
			if (SearchField.text.Length < 1)
			{
				return;
			}
		}
		else if (SearchField.text.Length < 3)
		{
			return;
		}
		if (searchWaitTask.Status != UniTaskStatus.Pending)
		{
			searchWaitTask = WaitStartSearch();
		}
	}

	private void StartSearchNow()
	{
		_waitForSearch = 0f;
		if (searchWaitTask.Status != UniTaskStatus.Pending)
		{
			searchWaitTask = WaitStartSearch();
		}
	}

	private async UniTask WaitStartSearch()
	{
		while (_waitForSearch > 0f)
		{
			_waitForSearch -= Time.deltaTime;
			await UniTask.NextFrame();
		}
		ClearAndStartSearch(SearchField.text);
	}

	private void ForceSearch(string searchText)
	{
		if (string.IsNullOrEmpty(searchText))
		{
			ClearPreviousSearch();
			return;
		}
		bool doExtendedSearch = true;
		switch (HelpMode)
		{
		case HelpMode.Functions:
		{
			if (searchText.Length >= 10)
			{
				break;
			}
			if (Enum.TryParse<ScriptCommand>(searchText, out var result2))
			{
				DoLiteralSearch((int)result2);
				doExtendedSearch = false;
			}
			ProgrammableChip.Constant[] allConstants = ProgrammableChip.AllConstants;
			for (int i = 0; i < allConstants.Length; i++)
			{
				ProgrammableChip.Constant constant = allConstants[i];
				if (constant == searchText)
				{
					DoLiteralSearch(constant.Hash);
					doExtendedSearch = false;
				}
			}
			break;
		}
		case HelpMode.Variables:
		{
			if (searchText.Length >= 24)
			{
				break;
			}
			int num = Animator.StringToHash(searchText);
			foreach (IScriptEnum internalEnum in ProgrammableChip.InternalEnums)
			{
				if (internalEnum.IsHashType(num))
				{
					DoCategorySearch(num);
				}
				if (internalEnum.TryParse(searchText))
				{
					DoLiteralSearch(num);
				}
			}
			break;
		}
		case HelpMode.SlotVariables:
		{
			if (searchText.Length < 24 && Enum.TryParse<LogicSlotType>(searchText, out var result))
			{
				DoLiteralSearch((int)result);
			}
			break;
		}
		}
		if (searchText.Length <= 2)
		{
			return;
		}
		Match match = Regex.Match(searchText, "^\\w+", RegexOptions.IgnorePatternWhitespace);
		string firstWord = string.Empty;
		if (match.Success)
		{
			firstWord = match.Value;
		}
		List<string> list = searchText.Split(',').ToList();
		List<int> list2 = new List<int>(list.Count);
		StringBuilder stringBuilder = new StringBuilder();
		for (int j = 0; j < list.Count; j++)
		{
			string text = list[j];
			if (string.IsNullOrEmpty(text))
			{
				continue;
			}
			if (j > 0)
			{
				stringBuilder.Append("|");
			}
			List<string> list3 = text.Split(' ').ToList();
			for (int num2 = list3.Count - 1; num2 >= 0; num2--)
			{
				list3[num2] = _searchRegex.Replace(list3[num2], string.Empty);
				if (string.IsNullOrEmpty(list3[num2]))
				{
					list3.RemoveAt(num2);
				}
			}
			if (list3.Count > 0 && HelpMode == HelpMode.Variables)
			{
				int num3 = Animator.StringToHash(text);
				bool flag = false;
				foreach (IScriptEnum internalEnum2 in ProgrammableChip.InternalEnums)
				{
					if (internalEnum2.IsHashType(num3))
					{
						list2.Add(num3);
						flag = true;
						break;
					}
				}
				if (flag)
				{
					break;
				}
			}
			for (int k = 0; k < list3.Count; k++)
			{
				stringBuilder.Append("(?=.*" + list3[k] + ")");
			}
		}
		ClearPreviousSearch();
		_searchRoutineCancel = new CancellationTokenSource();
		_finishSearchRoutine = DoSearch(firstWord, stringBuilder.ToString(), list2, _searchRoutineCancel, doExtendedSearch);
	}

	private void DoLiteralSearch(int command)
	{
		ClearPreviousSearch();
		foreach (HelpReference helpReference in _helpReferences)
		{
			helpReference.SetVisible(command == helpReference.ReferenceValue1);
		}
	}

	private void DoCategorySearch(int command)
	{
		ClearPreviousSearch();
		foreach (HelpReference helpReference in _helpReferences)
		{
			helpReference.SetVisible(command == helpReference.ReferenceValue2);
		}
	}

	private async UniTask DoSearch(string firstWord, string pattern, List<int> hashes, CancellationTokenSource cancelToken, bool doExtendedSearch)
	{
		if (string.IsNullOrEmpty(pattern) && hashes.Count == 0)
		{
			ClearPreviousSearch();
			return;
		}
		foreach (HelpReference helpReference in _helpReferences)
		{
			helpReference.SetVisible(isVisble: false);
		}
		await UniTask.SwitchToThreadPool();
		int count = 0;
		foreach (HelpReference helpReference2 in _helpReferences)
		{
			if (cancelToken.IsCancellationRequested)
			{
				return;
			}
			if (hashes.Count > 0)
			{
				bool flag = false;
				foreach (int hash in hashes)
				{
					if (hash != 0 && helpReference2.ReferenceValue2 == hash)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					continue;
				}
			}
			if (helpReference2.IsFirstWord(firstWord))
			{
				count++;
				helpReference2.SearchShow().Forget();
			}
			else if (doExtendedSearch && helpReference2.IsRegexMatch(pattern))
			{
				count++;
				helpReference2.SearchShow().Forget();
			}
		}
		await UniTask.SwitchToMainThread();
		NoResultsFromSearchText.SetActive(count == 0);
	}

	private void ClearAndStartSearch(string searchText)
	{
		ClearPreviousSearch();
		ForceSearch(searchText);
	}

	private void ClearPreviousSearch()
	{
		if (_finishSearchRoutine.Status == UniTaskStatus.Pending)
		{
			_searchRoutineCancel.Cancel();
		}
		NoResultsFromSearchText.SetActive(value: false);
		foreach (HelpReference helpReference in _helpReferences)
		{
			helpReference.SetVisible(isVisble: true);
		}
	}

	private HelpReference MakeHelpReference(string format, string command, string description, string category = "Instruction")
	{
		HelpReference helpReference = UnityEngine.Object.Instantiate(ReferencePrefab, FunctionTransform);
		helpReference.ReferenceValue1 = Animator.StringToHash(command);
		helpReference.Setup(ProgrammableChip.SetString(format, command), description, DefaultItemImage, category, Animator.StringToHash(command), HelpReference.InstructionHash);
		return helpReference;
	}

	public void Initialize()
	{
		IsOpaque = true;
		switch (HelpMode)
		{
		case HelpMode.Functions:
		{
			List<ScriptCommand> list = new List<ScriptCommand>(EnumCollections.ScriptCommands.Values);
			list.Sort((ScriptCommand a, ScriptCommand b) => string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal));
			ScriptCommand[] array = list.ToArray();
			if ((bool)Description)
			{
				Description.text = ProgrammableChip.GetIntroString();
			}
			_helpReferences.Add(MakeHelpReference("{0}{14}", "<color=#20B2AA>$</color>", "any valid hex characters after this will be parsed together as a hex value. You can use underscores to help with readability, but they have no functional use. As an example, $F will parse as 15.", "Macro"));
			_helpReferences.Add(MakeHelpReference("{0}{14}", "<color=#20B2AA>%</color>", "any valid binary numbers (0 or 1) will be parsed together as a binary value. You can use underscores to help with readability, but they have no functional use. As an example, %1111 or %11_11 will parse as 15.", "Macro"));
			_helpReferences.Add(MakeHelpReference("{0}", "<color=#A0A0A0>HASH(</color><color=white>\"...\"</color><color=#A0A0A0>)</color>", "any text inside will be hashed to an integer before processing takes place. Use this to generate integer values for use wherever hashes are required.", "Macro"));
			_helpReferences.Add(MakeHelpReference("{0}", "<color=#A0A0A0>STR(</color><color=white>\"...\"</color><color=#A0A0A0>)</color>", "any text inside will be packed into 53 usable bits of the 64 bit backing number that can be used for drawing text. This limits its use to 6 characters. ", "Macro"));
			ProgrammableChip.Constant[] allConstants = ProgrammableChip.AllConstants;
			foreach (ProgrammableChip.Constant constant in allConstants)
			{
				HelpReference helpReference2 = UnityEngine.Object.Instantiate(ReferencePrefab, FunctionTransform);
				helpReference2.Setup(constant, DefaultItemImage2);
				_helpReferences.Add(helpReference2);
			}
			_helpReferences.Add(MakeHelpReference("{0} {7}", "#", "any characters on line after this ignored"));
			ScriptCommand[] array2 = array;
			foreach (ScriptCommand command in array2)
			{
				if (!LogicBase.IsDeprecated(command))
				{
					HelpReference helpReference3 = UnityEngine.Object.Instantiate(ReferencePrefab, FunctionTransform);
					helpReference3.Setup(command, DefaultItemImage);
					_helpReferences.Add(helpReference3);
				}
			}
			break;
		}
		case HelpMode.Variables:
			foreach (IScriptEnum internalEnum in ProgrammableChip.InternalEnums)
			{
				int num = internalEnum.Count();
				for (int num2 = 0; num2 < num; num2++)
				{
					HelpReference helpReference4 = internalEnum.MakePage(num2, ReferencePrefab, FunctionTransform);
					if (!(helpReference4 == null))
					{
						helpReference4.SaveType.sprite = DefaultItemImage;
						_helpReferences.Add(helpReference4);
					}
				}
			}
			_helpReferences = _helpReferences.OrderBy((HelpReference x) => x.Text2.text).ThenBy((HelpReference y) => y.Text.text).ToList();
			{
				foreach (HelpReference helpReference5 in _helpReferences)
				{
					helpReference5.Transform.SetAsLastSibling();
				}
				break;
			}
		case HelpMode.SlotVariables:
		{
			LogicSlotType[] logicSlotTypes = Logicable.LogicSlotTypes;
			foreach (LogicSlotType logicSlotType in logicSlotTypes)
			{
				if (logicSlotType != LogicSlotType.None && !LogicBase.IsDeprecated(logicSlotType))
				{
					HelpReference helpReference = UnityEngine.Object.Instantiate(ReferencePrefab, FunctionTransform);
					helpReference.Text.text = $"<color=orange>{logicSlotType}</color>";
					helpReference.Text2.text = "<color=#808080>LogicSlotType</color>";
					helpReference.SaveType.sprite = DefaultItemImage;
					helpReference.ReferenceValue1 = (int)logicSlotType;
					string logicDescription = LogicBase.GetLogicDescription(logicSlotType);
					if (string.IsNullOrEmpty(logicDescription))
					{
						helpReference.Description.gameObject.SetActive(value: false);
					}
					else
					{
						helpReference.Description.text = logicDescription;
					}
					_helpReferences.Add(helpReference);
				}
			}
			break;
		}
		case HelpMode.Instructions:
			ReloadFileList();
			ScriptLibraryWindow = this;
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case HelpMode.None:
			break;
		}
	}

	public async void ReloadFileList()
	{
		if (HelpMode != HelpMode.Instructions)
		{
			return;
		}
		foreach (HelpReference helpReference2 in _helpReferences)
		{
			UnityEngine.Object.Destroy(helpReference2.GameObject);
		}
		_helpReferences.Clear();
		if (_tokenSource == null)
		{
			_tokenSource = new CancellationTokenSource();
		}
		var (flag, readOnlyList) = await NetworkManager.GetLocalAndWorkshopItems(SteamTransport.WorkshopType.ICCode).AttachExternalCancellation(_tokenSource.Token).SuppressCancellationThrow();
		if (flag)
		{
			Debug.LogWarning(GameStrings.ScriptEditorInstructionCancelled.DisplayString);
			return;
		}
		foreach (SteamTransport.ItemWrapper item in readOnlyList)
		{
			InstructionData instructionData = InstructionData.GetFromFile(item.FilePathFullName);
			if (instructionData == null)
			{
				continue;
			}
			instructionData.ItemWrapper = item;
			HelpReference helpReference = UnityEngine.Object.Instantiate(ReferencePrefab, FunctionTransform);
			helpReference.Text.text = instructionData.Title;
			helpReference.Description.text = GameStrings.ScriptEditorAuthorInfo.AsString(instructionData.Author, instructionData.GetDescription(128));
			helpReference.Button3.onClick.AddListener(delegate
			{
				PromptPanel.Instance.ShowPrompt(GameStrings.ScriptEditorLoadTitle.DisplayString, GameStrings.ScriptEditorLoadDescription.DisplayString, PromptOverwriteStrings.Button, instructionData.LoadIntoComputer);
			});
			helpReference.Button2.onClick.AddListener(delegate
			{
				PromptPanel.Instance.ShowPrompt(GameStrings.ScriptEditorOverwriteTitle.DisplayString, GameStrings.ScriptEditorOverwriteDescription.DisplayString, PromptOverwriteStrings.Button, instructionData.SaveFromComputer);
			});
			helpReference.Button4.onClick.AddListener(delegate
			{
				PromptPanel.Instance.ShowPrompt(GameStrings.ScriptEditorPublishTitle.DisplayString, GameStrings.ScriptEditorPublishDescription.DisplayString, PromptPublishStrings.Button, delegate
				{
					PublishWorkshopItem(instructionData);
				});
			});
			_helpReferences.Add(helpReference);
			if (item.IsLocal())
			{
				helpReference.Button1.onClick.AddListener(delegate
				{
					PromptPanel.Instance.ShowPrompt(GameStrings.ScriptEditorDeleteTitle.DisplayString, GameStrings.ScriptEditorDeleteDescription.DisplayString, PromptDeleteStrings.Button, instructionData.DeleteFile);
				});
				continue;
			}
			helpReference.SaveType.sprite = WorkshopImage;
			helpReference.Button2.interactable = false;
			helpReference.Button4.interactable = false;
			helpReference.Button1.onClick.AddListener(delegate
			{
				PromptPanel.Instance.ShowPrompt(PromptUnsubscribeStrings.Title, PromptUnsubscribeStrings.Body, PromptUnsubscribeStrings.Button, delegate
				{
					DeleteWorkshopItem(instructionData);
				});
			});
		}
		Rebuild();
	}

	private async void PublishWorkshopItem(InstructionData instructionData)
	{
		var (flag, workshopFileHandle) = await instructionData.PublishToWorkshop();
		if (flag)
		{
			instructionData.WorkshopFileHandle = workshopFileHandle;
			instructionData.SaveToFile(instructionData.DirectoryPath);
			ReloadFileList();
		}
	}

	private async void DeleteWorkshopItem(InstructionData instructionData)
	{
		await instructionData.DeleteWorkshopItem();
		ReloadFileList();
	}

	public override void ToggleVisibility()
	{
		base.ToggleVisibility();
		ReloadFileList();
	}

	public override void SetVisible(bool isVisble)
	{
		base.SetVisible(isVisble);
		if (!isVisble)
		{
			_tokenSource?.Cancel();
			_tokenSource?.Dispose();
			_tokenSource = null;
		}
		if (isVisble && !_isRebuilt)
		{
			Rebuild();
		}
	}

	private void Rebuild()
	{
		bool isVisible = IsVisible;
		RectTransform.ForceUpdateRectTransforms();
		FunctionTransform.ForceUpdateRectTransforms();
		LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);
		LayoutRebuilder.ForceRebuildLayoutImmediate(FunctionTransform);
		GameObject.SetActive(!isVisible);
		GameObject.SetActive(isVisible);
		_isRebuilt = true;
	}
}
