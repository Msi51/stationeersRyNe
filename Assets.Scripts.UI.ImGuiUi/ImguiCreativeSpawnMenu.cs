using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Cysharp.Threading.Tasks;
using ImGuiNET;
using UnityEngine;
using Util;

namespace Assets.Scripts.UI.ImGuiUi;

public static class ImguiCreativeSpawnMenu
{
	private static List<ICreativeSpawnable> Spawnable = new List<ICreativeSpawnable>(1024);

	private static List<ICreativeSpawnable> _focusedThread = new List<ICreativeSpawnable>(1024);

	private static List<ICreativeSpawnable> Focused = new List<ICreativeSpawnable>(1024);

	private static readonly Regex _searchRegex = new Regex("[^a-zA-Z0-9-]+", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

	private static ImGuiModal _dummyModal = new ImGuiModal();

	public static bool Show = false;

	private static bool _showStateVolatile = false;

	private static string _lastInput = string.Empty;

	private static readonly Vector2 DefaultWindowSize = new Vector2(544f, 768f);

	private const int WINDOW_POSITION_BUFFER = 100;

	private static readonly Vector2 ButtonSize = new Vector2(64f, 64f);

	public static readonly Vector2 ChildSize = new Vector2(528f, 72f);

	private const float COLUMN_WIDTH = 96f;

	private const float MIN_WINDOW_WIDTH = 208f;

	private static StringBuilder _toolTipSb = new StringBuilder();

	public const float DELAY_AFTER_KEY = 0.1f;

	private static CancellationTokenWrapper _startSearchCountdownWrapper = new CancellationTokenWrapper();

	private static CancellationTokenWrapper _searchCancellationTokenWrapper = new CancellationTokenWrapper();

	public static void Initialize()
	{
	}

	public static void AddDynamicItem(ICreativeSpawnable spawnable)
	{
		if (!GameManager.IsBatchMode && !(spawnable is Wreckage))
		{
			Spawnable.Add(spawnable);
		}
	}

	public static void ShowMenu(bool show)
	{
		if (!_showStateVolatile)
		{
			Show = show;
			if (Show)
			{
				SetInputEnabled(enabled: true);
				MouseModeController.AddModal(_dummyModal);
				return;
			}
			SetInputEnabled(enabled: false);
			MouseModeController.RemoveModal(_dummyModal);
			CursorManager.Instance?.OnApplicationFocus(focus: true);
			PanelToolTip.Instance.ClearToolTip();
		}
	}

	private static void SetInputEnabled(bool enabled)
	{
		_showStateVolatile = true;
		if (!(InputMouse.Instance == null))
		{
			SetInputKeyState(enabled);
		}
	}

	private static void SetInputKeyState(bool isTyping)
	{
		string key = "InputWindow_ImguiCreativeSpawnMenu";
		if (isTyping)
		{
			KeyManager.SetInputState(key, KeyInputState.Typing);
		}
		else
		{
			KeyManager.RemoveInputState(key);
		}
	}

	public static void Draw()
	{
		if (!Show)
		{
			_showStateVolatile = false;
			MouseModeController.RemoveModal(_dummyModal);
			return;
		}
		ImGuiWindowFlags flags = (ImGuiWindowFlags)3328;
		bool show = Show;
		Localization.PushFont();
		ImGui.Begin(GameStrings.HeaderCreativeSpawnMenu, ref Show, flags);
		if (show && !Show)
		{
			SetInputEnabled(enabled: false);
		}
		if (KeyManager.GetButtonDown(KeyMap.ShowDynamicPanel) && !_showStateVolatile)
		{
			ShowMenu(show: false);
			ImGui.End();
			return;
		}
		if (ImGui.IsWindowFocused() && KeyManager.GetButton(KeyCode.Escape))
		{
			ShowMenu(show: false);
			ImGui.End();
			return;
		}
		_showStateVolatile = false;
		ImGui.SetWindowSize(DefaultWindowSize, ImGuiCond.Once);
		ImGui.SetWindowPos(new Vector2(100f, 100f), ImGuiCond.Once);
		ImGui.Text(GameStrings.ButtonSearch);
		ImGui.SameLine();
		if (string.IsNullOrEmpty(_lastInput))
		{
			Focused.Clear();
			Focused.AddRange(Spawnable);
		}
		if (ImGui.InputText("###SearchInput", ref _lastInput, 196u, (ImGuiInputTextFlags)8208))
		{
			if (!InputMouse.IsMouseControl)
			{
				SetInputEnabled(enabled: true);
			}
			_startSearchCountdownWrapper.CancelAndInitialize();
			ClearAndStartSearch(_lastInput, _startSearchCountdownWrapper.Token).Forget();
		}
		if (ImGui.IsWindowAppearing())
		{
			ImGui.SetKeyboardFocusHere(-1);
		}
		ImGui.Separator();
		Vector2 windowSize = ImGui.GetWindowSize();
		ImGui.BeginChild("PrefabSelectionChild");
		ImGui.Columns(3, "###prefabs", border: false);
		float width = Mathf.Clamp(windowSize.x - 208f, 0f, windowSize.x);
		ImGui.SetColumnWidth(0, 96f);
		ImGui.SetColumnWidth(1, width);
		ImGui.SetColumnWidth(2, 96f);
		ImGui.NewLine();
		ICreativeSpawnable creativeSpawnable = null;
		bool flag = false;
		foreach (ICreativeSpawnable item in Focused)
		{
			Texture2D texture2D = item.GetThumbnail()?.texture;
			if ((bool)texture2D)
			{
				bool flag2 = InventoryManager.SpawnPrefab != null && InventoryManager.SpawnPrefab.SpawnId == item.SpawnId;
				if (ImGui.ImageButton((IntPtr)GetTextureId(texture2D), ButtonSize, flag2 ? new Vector4(1f, 1f, 1f, 0.5f) : new Vector4(1f, 1f, 1f, 0.1f)))
				{
					InventoryManager.SpawnPrefab = item;
					ShowMenu(show: false);
				}
				if (ImGui.IsItemHovered())
				{
					_toolTipSb.Clear();
					item.ToTooltip(_toolTipSb);
					PanelToolTip.Instance.SetUpTooltip(item.DisplayName, _toolTipSb.ToString());
					flag = true;
				}
				ImGui.NextColumn();
				float y = ImGui.GetCursorPos().y;
				float fontSize = ImGui.GetFontSize();
				ImGui.SetCursorPosY(y + ButtonSize.y / 2f + fontSize / 2f);
				ImGui.Text(item.DisplayName);
				ImGui.NextColumn();
				ImGui.NewLine();
				if (ImGui.Button("+###" + item.DisplayName + "Spawn", ButtonSize))
				{
					creativeSpawnable = item;
					InventoryManager.SpawnDynamicThing(item);
				}
				ImGui.NextColumn();
				ImGui.NewLine();
			}
		}
		if (!flag || !Show)
		{
			PanelToolTip.Instance.ClearToolTip();
		}
		ImGui.EndChild();
		if (creativeSpawnable != null)
		{
			InventoryManager.SpawnPrefab = creativeSpawnable;
		}
		if (ImGui.IsWindowFocused() && KeyManager.GetButton(KeyCode.Return))
		{
			if (creativeSpawnable == null || !Focused.Contains(InventoryManager.SpawnPrefab))
			{
				InventoryManager.SpawnPrefab = ((Focused.Count > 0) ? Focused[0] : Spawnable[0]);
			}
			ShowMenu(show: false);
		}
		Localization.PopFont();
		ImGui.End();
	}

	public static int GetTextureId(Texture texture)
	{
		return ImGuiManager.igTextureManager.GetTextureId(texture);
	}

	public static async UniTaskVoid ClearAndStartSearch(string searchText, CancellationToken token)
	{
		ClearPreviousSearch();
		float timer = 0.1f;
		while (timer > 0f)
		{
			timer -= Time.unscaledDeltaTime;
			await UniTask.WaitForEndOfFrame(token);
		}
		if (!token.IsCancellationRequested)
		{
			ForceSearch(searchText);
		}
	}

	private static void ClearPreviousSearch()
	{
		_searchCancellationTokenWrapper.Cancel();
		Focused.Clear();
	}

	private static void ForceSearch(string searchText)
	{
		if (string.IsNullOrEmpty(searchText))
		{
			ClearPreviousSearch();
			return;
		}
		string sanitisedText = Regex.Replace(searchText, "[^0-9-]+", "", RegexOptions.IgnorePatternWhitespace);
		List<string> list = searchText.Split(',').ToList();
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < list.Count; i++)
		{
			string text = list[i];
			if (string.IsNullOrEmpty(text))
			{
				continue;
			}
			if (i > 0)
			{
				stringBuilder.Append("|");
			}
			List<string> list2 = text.Split(' ').ToList();
			for (int num = list2.Count - 1; num >= 0; num--)
			{
				list2[num] = _searchRegex.Replace(list2[num], string.Empty);
				if (string.IsNullOrEmpty(list2[num]))
				{
					list2.RemoveAt(num);
				}
			}
			for (int j = 0; j < list2.Count; j++)
			{
				stringBuilder.Append("(?=.*" + list2[j] + ")");
			}
		}
		ClearPreviousSearch();
		_searchCancellationTokenWrapper.CancelAndInitialize();
		DoSearch(sanitisedText, stringBuilder.ToString(), _searchCancellationTokenWrapper.Token).Forget();
	}

	private static async UniTaskVoid DoSearch(string sanitisedText, string pattern, CancellationToken cancelToken)
	{
		if (string.IsNullOrEmpty(pattern) && string.IsNullOrEmpty(sanitisedText))
		{
			ClearPreviousSearch();
			return;
		}
		_focusedThread.Clear();
		await UniTask.SwitchToThreadPool();
		int num = 0;
		for (int num2 = Spawnable.Count - 1; num2 >= 0; num2--)
		{
			if (cancelToken.IsCancellationRequested)
			{
				return;
			}
			if (IsRegexMatch(Spawnable[num2], sanitisedText, pattern))
			{
				_focusedThread.Add(Spawnable[num2]);
				num++;
			}
		}
		await UniTask.SwitchToMainThread(cancelToken);
		Focused.Clear();
		Focused.AddRange(_focusedThread);
	}

	public static bool IsRegexMatch(ICreativeSpawnable thing, string text, string pattern)
	{
		if (text == thing.SpawnableName)
		{
			return true;
		}
		if (!Match(pattern, thing.SpawnableName))
		{
			return Match(pattern, thing.DisplayName);
		}
		return true;
	}

	private static bool Match(string pattern, string value)
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
}
