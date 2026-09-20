using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Util;
using ImGuiNET;
using UnityEngine;

namespace Assets.Scripts.UI.ImGuiUi;

public class ImGuiProfiler
{
	public static bool Enabled;

	private static Dictionary<string, DebugGroup> _groups = new Dictionary<string, DebugGroup>(10);

	private static readonly Vector4 _colorGrey = new Vector4(0.7f, 0.7f, 0.7f, 1f);

	private static readonly Vector4 _colorYellow = new Vector4(1f, 0.8f, 0.3f, 1f);

	private static readonly Vector4 _colorRed = new Vector4(1f, 0.4f, 0.4f, 1f);

	private static readonly Vector4 _colorRowBg = new Vector4(0.1f, 0.1f, 0.1f, 0.8f);

	private static readonly Vector4 _colorRowBgAlt = new Vector4(0.2f, 0.2f, 0.2f, 0.8f);

	private static bool IsEnabled
	{
		get
		{
			if (Enabled && GameManager.RunSimulation)
			{
				return GameManager.GameState == GameState.Running;
			}
			return false;
		}
	}

	public static void DrawDebug()
	{
		if (IsEnabled)
		{
			Draw();
		}
	}

	private static void Draw()
	{
		ImGui.Begin("ImGuiProfilerWindow", (ImGuiWindowFlags)12741);
		ImGui.SetWindowPos(Vector2.zero, ImGuiCond.Always);
		ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(10f, 0f));
		ImGui.PushStyleColor(ImGuiCol.TableRowBg, _colorRowBg);
		ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, _colorRowBgAlt);
		if (ImGui.BeginTabBar("###ImGuiProfilerTabBar", (ImGuiTabBarFlags)40))
		{
			int num = 0;
			foreach (KeyValuePair<string, DebugGroup> group in _groups)
			{
				DrawTab(num, group.Key, group.Value);
				num++;
			}
			ImGui.EndTabBar();
		}
		ImGui.PopStyleVar();
		ImGui.PopStyleColor();
		ImGui.PopStyleColor();
		ImGui.End();
	}

	private static void DrawTab(int tabId, string groupKey, DebugGroup group)
	{
		if (!ImGui.BeginTabItem($"{groupKey}###ImGuiProfilerTab{tabId}"))
		{
			return;
		}
		if (ImGui.BeginTable($"ImGuiProfilerTable{tabId}", 3, (ImGuiTableFlags)10320))
		{
			ImGui.TableSetupColumn("Name");
			ImGui.TableSetupColumn("Cur ms", 100f);
			ImGui.TableSetupColumn("Avg ms", 100f);
			ImGui.TableHeadersRow();
			foreach (KeyValuePair<string, DebugLine> line in group.Lines)
			{
				ImGui.TableNextRow();
				ImGui.TableSetColumnIndex(0);
				ImGui.TextUnformatted(line.Key);
				ImGui.TableSetColumnIndex(1);
				DrawValue(line.Value.GetCurrent(), "0");
				ImGui.TableSetColumnIndex(2);
				DrawValue(line.Value.GetAverage(), "0.00");
			}
			ImGui.EndTable();
		}
		ImGui.EndTabItem();
	}

	private static void DrawValue(float value, string format)
	{
		float num = 100f;
		float num2 = 500f;
		ImGui.TextColored((value > num2) ? _colorRed : ((value > num) ? _colorYellow : _colorGrey), value.ToString(format));
	}

	private static void PrintToConsole()
	{
		foreach (KeyValuePair<string, DebugGroup> group in _groups)
		{
			ConsoleWindow.Print(group.Key);
			foreach (KeyValuePair<string, DebugLine> line in group.Value.Lines)
			{
				ConsoleWindow.Print(line.Key + ": " + StringManager.Get(line.Value.GetCurrent()));
			}
		}
	}

	private static DebugGroup GetGroup(string groupKey)
	{
		if (!_groups.TryGetValue(groupKey, out var value))
		{
			value = new DebugGroup();
			_groups.Add(groupKey, value);
		}
		return value;
	}

	public static void Begin(string groupKey)
	{
		if (Enabled)
		{
			GetGroup(groupKey).Begin();
		}
	}

	public static void End(string groupKey)
	{
		if (Enabled)
		{
			GetGroup(groupKey).End();
			if (GameManager.IsBatchMode)
			{
				PrintToConsole();
				Enabled = false;
				Clear();
			}
		}
	}

	public static void Reset(string groupKey)
	{
		if (Enabled)
		{
			GetGroup(groupKey).Reset();
		}
	}

	public static void Clear()
	{
		_groups.Clear();
	}

	public static void Update(string groupKey, string key)
	{
		if (Enabled)
		{
			GetGroup(groupKey).Update(key);
		}
	}
}
