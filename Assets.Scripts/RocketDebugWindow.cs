using System;
using System.Collections.Generic;
using Assets.Scripts.Objects;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using ImGuiNET;
using Networks;
using Objects.Rockets;
using UI.ImGuiUi;
using UI.ImGuiUi.Debug;
using UnityEngine;

namespace Assets.Scripts;

public static class RocketDebugWindow
{
	public static bool Show;

	private static Action _pending;

	private static Rocket _confirmAbandon;

	private static bool _openAbandonPopup;

	private static readonly HashSet<long> _debugDraw = new HashSet<long>();

	private static readonly Vector4 Dim = new Vector4(0.49f, 0.53f, 0.55f, 1f);

	private static readonly Vector4 Sage = new Vector4(0.53f, 0.77f, 0.43f, 1f);

	private static readonly Vector4 Sky = new Vector4(0.37f, 0.69f, 0.85f, 1f);

	private static readonly Vector4 Amber = new Vector4(0.85f, 0.71f, 0.3f, 1f);

	private static readonly Vector4 Red = new Vector4(0.89f, 0.34f, 0.29f, 1f);

	public static void Toggle()
	{
		Show = !Show;
	}

	public static void Draw()
	{
		ImGui.Begin("Rocket Debug", ref Show, (ImGuiWindowFlags)288);
		ImGui.SetWindowSize(ImguiHelper.StandardResizableScaled, ImGuiCond.Once);
		if (ImGui.BeginTabBar("##rkt_tabs"))
		{
			if (ImGui.BeginTabItem("Rockets"))
			{
				DrawRockets();
				ImGui.EndTabItem();
			}
			if (ImGui.BeginTabItem("Mounts"))
			{
				DrawMounts();
				ImGui.EndTabItem();
			}
			if (ImGui.BeginTabItem("Parks"))
			{
				DrawParks();
				ImGui.EndTabItem();
			}
			ImGui.EndTabBar();
		}
		DrawAbandonModal();
		ImGui.End();
		if (_pending != null)
		{
			Action pending = _pending;
			_pending = null;
			try
			{
				pending();
			}
			catch (Exception arg)
			{
				ConsoleWindow.PrintError($"Rocket debug action failed: {arg}", suppressStacktrace: true);
			}
		}
	}

	private static void DrawAbandonModal()
	{
		if (_openAbandonPopup)
		{
			ImGui.OpenPopup("Abandon Rocket?");
			_openAbandonPopup = false;
		}
		if (!ImGui.BeginPopupModal("Abandon Rocket?", ImGuiWindowFlags.AlwaysAutoResize))
		{
			return;
		}
		ImGui.Text("Abandon '" + _confirmAbandon?.DisplayName + "'? This destroys the rocket.");
		ImGui.Separator();
		if (ImGui.Button("Abandon"))
		{
			Rocket r = _confirmAbandon;
			_pending = delegate
			{
				r?.AbandonRocket();
			};
			_confirmAbandon = null;
			ImGui.CloseCurrentPopup();
		}
		ImGui.SameLine();
		if (ImGui.Button("Cancel"))
		{
			_confirmAbandon = null;
			ImGui.CloseCurrentPopup();
		}
		ImGui.EndPopup();
	}

	private static void DrawRockets()
	{
		ImGui.TextColored(Dim, $"Rockets: {Rocket.AllRockets.Count}");
		ImGui.Separator();
		ImGui.BeginChild("##rkt_list");
		foreach (Rocket allRocket in Rocket.AllRockets)
		{
			if (allRocket?.RocketNetwork != null)
			{
				DrawRocketNode(allRocket);
			}
		}
		ImGui.EndChild();
	}

	private static void DrawRocketNode(Rocket rocket)
	{
		RocketNetwork rocketNetwork = rocket.RocketNetwork;
		ImGui.PushID((int)rocket.ReferenceId);
		bool flag = ImGui.TreeNodeEx($"{rocket.DisplayName}  #{StringManager.Get(rocket.ReferenceId)}  [{rocket.RocketState}]");
		RocketContextMenu(rocket);
		int count = rocketNetwork.Internals.Count;
		int num = 0;
		foreach (IRocketInternals @internal in rocketNetwork.Internals)
		{
			if (@internal is Structure { RocketData: null })
			{
				num++;
			}
		}
		ImGui.SameLine();
		if (num == 0)
		{
			ImGui.TextColored(Sage, $"· {count} internals");
		}
		else
		{
			ImGui.TextColored(Amber, $"· {count} internals, {num} unrecorded");
		}
		if (flag)
		{
			ImGui.TextColored(Dim, $"Network #{StringManager.Get(rocketNetwork.ReferenceId)}   dry {rocketNetwork.DryMass:0}kg · gas {rocketNetwork.GasMass:0}kg");
			string text = ((rocketNetwork.Anchor != null) ? (rocketNetwork.Anchor.DisplayName + " #" + StringManager.Get(rocketNetwork.Anchor.ReferenceId)) : "<none>");
			ImGui.TextColored((rocketNetwork.Anchor != null) ? Dim : Red, "Anchor: " + text);
			if (ImGui.TreeNodeEx($"Structures ({rocketNetwork.StructureList.Count})##s"))
			{
				DrawPartTable("##stbl", rocketNetwork.StructureList);
				ImGui.TreePop();
			}
			if (ImGui.TreeNodeEx($"Internals ({rocketNetwork.Internals.Count})##i"))
			{
				DrawPartTable("##itbl", rocketNetwork.Internals);
				ImGui.TreePop();
			}
			ImGui.TreePop();
		}
		ImGui.PopID();
	}

	private static bool BeginStyledTable(string id, int columns)
	{
		ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(2f, 2f) * ImguiHelper.UIScale);
		ImGui.PushStyleColor(ImGuiCol.TableHeaderBg, new Vector4(0.13f, 0.14f, 0.17f, 1f));
		ImGui.PushStyleColor(ImGuiCol.TableRowBg, new Vector4(0.06f, 0.07f, 0.09f, 1f));
		ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, new Vector4(0.1f, 0.11f, 0.13f, 1f));
		ImGui.PushStyleColor(ImGuiCol.TableBorderStrong, new Vector4(0.2f, 0.22f, 0.26f, 1f));
		ImGui.PushStyleColor(ImGuiCol.TableBorderLight, new Vector4(0.13f, 0.14f, 0.17f, 1f));
		if (ImGui.BeginTable(id, columns, (ImGuiTableFlags)1984))
		{
			return true;
		}
		ImGui.PopStyleColor(5);
		ImGui.PopStyleVar();
		return false;
	}

	private static void EndStyledTable()
	{
		ImGui.EndTable();
		ImGui.PopStyleColor(5);
		ImGui.PopStyleVar();
	}

	private static void DrawPartTable(string id, List<INetworkedStructure> parts)
	{
		if (!BeginStyledTable(id, 4))
		{
			return;
		}
		ImGui.TableSetupColumn("Part");
		ImGui.TableSetupColumn("Type");
		ImGui.TableSetupColumn("Offset");
		ImGui.TableSetupColumn("ReferenceId");
		ImGui.TableHeadersRow();
		foreach (INetworkedStructure part in parts)
		{
			if (part is Thing thing)
			{
				RocketData rocketData = (thing as Structure)?.RocketData;
				ImGui.TableNextRow();
				ImGui.TableNextColumn();
				ImGui.Text(thing.DisplayName);
				ImGui.TableNextColumn();
				ImGui.Text(thing.GetType().Name);
				ImGui.TableNextColumn();
				ImGui.Text((rocketData != null) ? $"{rocketData.Offset.x:0.#}, {rocketData.Offset.y:0.#}, {rocketData.Offset.z:0.#}" : "—");
				ImGui.TableNextColumn();
				ImGui.Text(StringManager.Get(thing.ReferenceId));
			}
		}
		EndStyledTable();
	}

	private static void DrawPartTable(string id, List<IRocketInternals> parts)
	{
		if (!BeginStyledTable(id, 4))
		{
			return;
		}
		ImGui.TableSetupColumn("Part");
		ImGui.TableSetupColumn("Type");
		ImGui.TableSetupColumn("Offset");
		ImGui.TableSetupColumn("ReferenceId");
		ImGui.TableHeadersRow();
		foreach (IRocketInternals part in parts)
		{
			if (part is Thing thing)
			{
				RocketData rocketData = (thing as Structure)?.RocketData;
				ImGui.TableNextRow();
				ImGui.TableNextColumn();
				ImGui.Text(thing.DisplayName);
				ImGui.TableNextColumn();
				ImGui.Text(thing.GetType().Name);
				ImGui.TableNextColumn();
				ImGui.Text((rocketData != null) ? $"{rocketData.Offset.x:0.#}, {rocketData.Offset.y:0.#}, {rocketData.Offset.z:0.#}" : "—");
				ImGui.TableNextColumn();
				ImGui.Text(StringManager.Get(thing.ReferenceId));
			}
		}
		EndStyledTable();
	}

	private static void DrawMounts()
	{
		if (!BeginStyledTable("##mtbl", 4))
		{
			return;
		}
		ImGui.TableSetupColumn("Mount");
		ImGui.TableSetupColumn("Kind");
		ImGui.TableSetupColumn("Node");
		ImGui.TableSetupColumn("RefId");
		ImGui.TableHeadersRow();
		int num = 0;
		foreach (SpaceMapNode allSpaceMapNode in SpaceMapNode.AllSpaceMapNodes)
		{
			if (!(allSpaceMapNode?.Owner is LaunchMount launchMount))
			{
				continue;
			}
			num++;
			bool num2 = allSpaceMapNode.RocketsHere != null && allSpaceMapNode.RocketsHere.Count > 0;
			ImGui.PushID((int)launchMount.ReferenceId);
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGuiTreeNodeFlags imGuiTreeNodeFlags = (ImGuiTreeNodeFlags)4128;
			if (!num2)
			{
				imGuiTreeNodeFlags |= (ImGuiTreeNodeFlags)264;
			}
			bool flag = ImGui.TreeNodeEx(launchMount.DisplayName, imGuiTreeNodeFlags);
			ImGui.TableNextColumn();
			ImGui.TextColored(launchMount.IsOrbital ? Sky : Sage, launchMount.IsOrbital ? "Orbital" : "Ground");
			ImGui.TableNextColumn();
			ImGui.TextColored(Dim, "#" + StringManager.Get(allSpaceMapNode.ReferenceId));
			ImGui.TableNextColumn();
			ImGui.TextColored(Dim, "#" + StringManager.Get(launchMount.ReferenceId));
			if (num2 && flag)
			{
				foreach (Rocket item in allSpaceMapNode.RocketsHere)
				{
					ImGui.TableNextRow();
					ImGui.TableNextColumn();
					ImGui.TreeNodeEx($"{item.DisplayName}  #{StringManager.Get(item.ReferenceId)}  [{item.RocketState}]", (ImGuiTreeNodeFlags)4872);
					ImGui.TableNextColumn();
					ImGui.TableNextColumn();
				}
				ImGui.TreePop();
			}
			ImGui.PopID();
		}
		EndStyledTable();
		if (num == 0)
		{
			ImGui.TextColored(Dim, "No launch mounts found.");
		}
	}

	private static void DrawParks()
	{
		List<RocketParkSlot> slots = RocketParkSlot.Slots;
		if (slots == null || slots.Count == 0)
		{
			ImGui.TextColored(Dim, "Rocket park not initialised (no slots).");
			return;
		}
		int num = 0;
		foreach (RocketParkSlot item in slots)
		{
			if (item.AssignedRocket != null && item.AssignedRocket.RocketParkSlot == item)
			{
				num++;
			}
		}
		ImGui.TextColored(Dim, $"Parks: {slots.Count}  ({num} occupied, {slots.Count - num} free)");
		ImGui.ProgressBar((slots.Count > 0) ? ((float)num / (float)slots.Count) : 0f, new Vector2(-1f, 0f), $"{num} / {slots.Count}");
		ImGui.Separator();
		ImGui.BeginChild("##parks");
		if (ImGui.TreeNodeEx($"Occupied ({num})", ImGuiTreeNodeFlags.DefaultOpen))
		{
			if (BeginStyledTable("##parktbl", 3))
			{
				ImGui.TableSetupColumn("Park");
				ImGui.TableSetupColumn("Position");
				ImGui.TableSetupColumn("Occupant");
				ImGui.TableHeadersRow();
				foreach (RocketParkSlot item2 in slots)
				{
					Rocket assignedRocket = item2.AssignedRocket;
					if (assignedRocket != null && assignedRocket.RocketParkSlot == item2)
					{
						Vector3 worldPosition = item2.GetWorldPosition();
						ImGui.TableNextRow();
						ImGui.TableNextColumn();
						ImGui.Text($"({item2.Location.x}, {item2.Location.y})");
						ImGui.TableNextColumn();
						ImGui.Text($"{worldPosition.x:0}, {worldPosition.y:0}, {worldPosition.z:0}");
						ImGui.TableNextColumn();
						ImGui.Text($"{assignedRocket.DisplayName}  #{StringManager.Get(assignedRocket.ReferenceId)}  [{assignedRocket.RocketState}]");
					}
				}
				EndStyledTable();
			}
			ImGui.TreePop();
		}
		ImGui.Text($"Free & empty: {slots.Count - num}");
		List<IRocketInternals> list = new List<IRocketInternals>();
		DensePool<Thing>.ActiveEnumerable.Enumerator enumerator2 = OcclusionManager.AllThings.Active().GetEnumerator();
		while (enumerator2.MoveNext())
		{
			if (enumerator2.Current is IRocketInternals { StrictlyInternal: not false, RocketNetwork: null } rocketInternals)
			{
				list.Add(rocketInternals);
			}
		}
		if (list.Count > 0)
		{
			ImGui.PushStyleColor(ImGuiCol.Text, Red);
		}
		bool num2 = ImGui.TreeNodeEx($"Loose orphans ({list.Count})", (list.Count > 0) ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None);
		if (list.Count > 0)
		{
			ImGui.PopStyleColor();
		}
		if (num2)
		{
			if (list.Count > 0 && BeginStyledTable("##orphantbl", 3))
			{
				ImGui.TableSetupColumn("Orphan");
				ImGui.TableSetupColumn("Type");
				ImGui.TableSetupColumn("Position");
				ImGui.TableHeadersRow();
				foreach (IRocketInternals item3 in list)
				{
					Thing thing = item3 as Thing;
					Vector3 vector = ((thing != null) ? thing.Transform.position : Vector3.zero);
					ImGui.TableNextRow();
					ImGui.TableNextColumn();
					ImGui.Text((thing != null) ? (thing.DisplayName + "  #" + StringManager.Get(thing.ReferenceId)) : item3.GetType().Name);
					ImGui.TableNextColumn();
					ImGui.Text(item3.GetType().Name);
					ImGui.TableNextColumn();
					ImGui.Text($"{vector.x:0}, {vector.y:0}, {vector.z:0}");
				}
				EndStyledTable();
			}
			ImGui.TreePop();
		}
		ImGui.EndChild();
	}

	private static void RocketContextMenu(Rocket rocket)
	{
		if (!ImGui.BeginPopupContextItem($"##ctx{rocket.ReferenceId}"))
		{
			return;
		}
		ImGui.TextDisabled(rocket.DisplayName + "  #" + StringManager.Get(rocket.ReferenceId));
		ImGui.Separator();
		bool flag = _debugDraw.Contains(rocket.ReferenceId);
		if (ImGui.MenuItem("Debug Draw", flag))
		{
			if (flag)
			{
				_debugDraw.Remove(rocket.ReferenceId);
			}
			else
			{
				_debugDraw.Add(rocket.ReferenceId);
			}
		}
		ImGui.Separator();
		ImGui.BeginDisabled(!GameManager.RunSimulation);
		if (ImGui.BeginMenu("Clone to"))
		{
			bool flag2 = false;
			foreach (LaunchMount item in EmptyMounts())
			{
				flag2 = true;
				if (ImGui.MenuItem($"{item.DisplayName}##clone{item.ReferenceId}"))
				{
					Rocket r = rocket;
					LaunchMount m = item;
					_pending = delegate
					{
						RocketCloner.Clone(r, m);
					};
				}
			}
			if (!flag2)
			{
				ImGui.TextDisabled("(no empty mounts)");
			}
			ImGui.EndMenu();
		}
		bool flag3 = rocket.RocketState == RocketState.InSpace;
		if (ImGui.BeginMenu("Move to"))
		{
			if (!flag3)
			{
				ImGui.TextDisabled("(in-space rockets only)");
			}
			ImGui.BeginDisabled(!flag3);
			RocketParkSlot freeSlot = RocketParkSlot.GetFreeSlot();
			if (ImGui.MenuItem((freeSlot != null) ? $"Free park ({freeSlot.Location.x},{freeSlot.Location.y})" : "Free park (full)") && freeSlot != null)
			{
				Rocket r2 = rocket;
				RocketParkSlot s = freeSlot;
				_pending = delegate
				{
					r2.SetParkSlot(s.Location);
				};
			}
			ImGui.Separator();
			bool flag4 = false;
			foreach (LaunchMount item2 in EmptyMounts())
			{
				flag4 = true;
				if (ImGui.MenuItem($"{item2.DisplayName}##move{item2.ReferenceId}"))
				{
					Rocket r3 = rocket;
					LaunchMount m2 = item2;
					_pending = delegate
					{
						r3.ChangeTarget(m2.SpaceMapNode);
						r3.Progress = 1f;
					};
				}
			}
			if (!flag4)
			{
				ImGui.TextDisabled("(no empty mounts)");
			}
			ImGui.EndDisabled();
			ImGui.EndMenu();
		}
		if (ImGui.MenuItem("Abandon…"))
		{
			_confirmAbandon = rocket;
			_openAbandonPopup = true;
		}
		ImGui.EndDisabled();
		ImGui.EndPopup();
	}

	private static IEnumerable<LaunchMount> EmptyMounts()
	{
		foreach (SpaceMapNode allSpaceMapNode in SpaceMapNode.AllSpaceMapNodes)
		{
			if (allSpaceMapNode?.Owner is LaunchMount launchMount && (allSpaceMapNode.RocketsHere == null || allSpaceMapNode.RocketsHere.Count == 0))
			{
				yield return launchMount;
			}
		}
	}

	public static void DrawDebugOverlays()
	{
		if (_debugDraw.Count == 0)
		{
			return;
		}
		foreach (Rocket allRocket in Rocket.AllRockets)
		{
			if (allRocket?.RocketNetwork == null || !_debugDraw.Contains(allRocket.ReferenceId))
			{
				continue;
			}
			RocketNetwork rocketNetwork = allRocket.RocketNetwork;
			if (rocketNetwork.Anchor != null)
			{
				ImGuiDebugHelper.DrawCube(rocketNetwork.Anchor.ThingTransformPosition, Vector3.one * 0.8f, ImGuiColor.Integer.Yellow);
			}
			foreach (INetworkedStructure structure in rocketNetwork.StructureList)
			{
				if (structure is Thing thing)
				{
					ImGuiDebugHelper.DrawWireSphere(thing.ThingTransformPosition, 0.6f, ImGuiColor.Integer.BlueTransparent);
				}
			}
			foreach (IRocketInternals @internal in rocketNetwork.Internals)
			{
				if (@internal is Thing thing2)
				{
					bool flag = (@internal as Structure)?.RocketData != null;
					ImGuiDebugHelper.DrawWireSphere(thing2.ThingTransformPosition, 0.35f, flag ? ImGuiColor.Integer.GreenTransparent : ImGuiColor.Integer.LightRedTransparent);
				}
			}
		}
	}
}
