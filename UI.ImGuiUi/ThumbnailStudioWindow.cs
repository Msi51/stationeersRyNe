using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using CharacterCustomisation;
using ImGuiNET;
using ThingImport.Thumbnails;
using UI.ImGuiUi.ImGuiWindows;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.ImGuiUi;

public class ThumbnailStudioWindow : UI.ImGuiUi.ImGuiWindows.ImGuiWindow
{
	private static readonly ThumbnailStudioWindow _instance = new ThumbnailStudioWindow();

	private static readonly Vector2 CellSize = new Vector2(96f, 96f);

	private static readonly Vector4 CurrentTint = new Vector4(0.18f, 0.55f, 0.25f, 1f);

	private static readonly Vector4 LinkTint = new Vector4(0.55f, 0.25f, 0.7f, 1f);

	private static readonly Vector4 LinkDotColor = new Vector4(0.9f, 0.35f, 1f, 1f);

	private static readonly Vector4 LinkTextColor = new Vector4(0.9f, 0.55f, 1f, 1f);

	private static readonly Vector4 WarnColor = new Vector4(1f, 0.7f, 0.2f, 1f);

	private static readonly Vector4 ErrorColor = new Vector4(1f, 0.4f, 0.4f, 1f);

	private static readonly string[] TypeFilterLabels = new string[4] { "All types", "Dynamic things", "Structures", "Other (neither)" };

	private string _search = string.Empty;

	private bool _missingOnly;

	private bool _paintableOnly;

	private int _typeFilter;

	private bool _keepRotation;

	private float _snapDegrees = 15f;

	private string _rotationName = string.Empty;

	private string _rotationSearch = string.Empty;

	private bool _overlayExisting = true;

	private float _overlayOpacity = 0.5f;

	private readonly HashSet<string> _linkSelection = new HashSet<string>();

	private string _status = string.Empty;

	private readonly List<ThumbnailStudio.ExportItem> _exportItems = new List<ThumbnailStudio.ExportItem>();

	private int _exportHighlight = -1;

	private int _exportWidth = 1999;

	private int _exportHeight = 934;

	private int _exportRows = 1;

	private float _exportPadding = 0.08f;

	private bool _exportDropShadow = true;

	private float _exportShadowOpacity = 0.45f;

	private bool _exportGif;

	private int _gifWidth = 900;

	private float _gifSeconds = 6f;

	private int _gifFrames = 60;

	private bool _gifDither;

	private string _exportStatus = string.Empty;

	private string _exportSearch = string.Empty;

	private CharacterConfig _charConfig;

	private bool _charDirty = true;

	private string _charStatus = string.Empty;

	private const int DefaultExportWidth = 1999;

	private const int DefaultExportHeight = 934;

	private string _pendingPoseApply;

	private float _pendingPoseTime;

	private EventSystem _blockedEventSystem;

	private static readonly Vector2 PreviewSize = new Vector2(512f, 512f);

	private static readonly HashSet<string> BinaryStateNames = new HashSet<string> { "Powered", "OnOff", "Open", "Activate", "Lock", "Error", "On", "Off" };

	public static bool IsOpen => _instance.IsShowing;

	public static void Open()
	{
		if (!_instance.IsShowing)
		{
			ImGuiWindowManager.Open(_instance);
		}
	}

	public static void Close()
	{
		if (_instance.IsShowing)
		{
			ImGuiWindowManager.Close(_instance);
		}
	}

	public ThumbnailStudioWindow()
		: base("Thumbnail Studio", new Vector2(1560f, 980f))
	{
	}

	public override void OnOpen()
	{
		_status = string.Empty;
		_linkSelection.Clear();
		_pendingPoseApply = null;
		_charDirty = true;
		ThumbnailStudio.Instance.Open();
	}

	public override void OnClose()
	{
		UnblockUiClickThrough();
		ThumbnailStudio.Instance.Close();
	}

	private void UpdateUiClickBlocking()
	{
		bool wantCaptureMouse = ImGui.GetIO().WantCaptureMouse;
		if (wantCaptureMouse && _blockedEventSystem == null)
		{
			_blockedEventSystem = EventSystem.current;
			if (_blockedEventSystem != null)
			{
				_blockedEventSystem.enabled = false;
			}
		}
		else if (!wantCaptureMouse)
		{
			UnblockUiClickThrough();
		}
	}

	private void UnblockUiClickThrough()
	{
		if (!(_blockedEventSystem == null))
		{
			_blockedEventSystem.enabled = true;
			_blockedEventSystem = null;
		}
	}

	private static int GetTextureId(Texture texture)
	{
		return ImGuiManager.igTextureManager.GetTextureId(texture);
	}

	public override void DrawContent()
	{
		UpdateUiClickBlocking();
		ThumbnailStudio instance = ThumbnailStudio.Instance;
		if (!instance.IsOpen)
		{
			ImGui.TextColored(ErrorColor, "Thumbnail studio failed to initialise (see console).");
			return;
		}
		instance.RenderIfDirty();
		if (_pendingPoseApply != null && Time.unscaledTime - _pendingPoseTime > 0.3f)
		{
			_status = (instance.ApplySavedPose(_pendingPoseApply) ? ("Applied saved rotation/zoom from " + _pendingPoseApply + ".") : (_pendingPoseApply + " has no saved rotation/zoom."));
			_pendingPoseApply = null;
		}
		if (instance.BatchRemaining > 0)
		{
			string text = instance.ProcessBatchStep();
			if (text != null)
			{
				_status = $"[{instance.BatchTotal - instance.BatchRemaining}/{instance.BatchTotal}] {text}";
			}
		}
		if (ImGui.BeginTabBar("StudioTabs"))
		{
			if (ImGui.BeginTabItem("Studio"))
			{
				DrawStudioTab(instance);
				ImGui.EndTabItem();
			}
			if (ImGui.BeginTabItem("Characters"))
			{
				DrawCharactersTab(instance);
				ImGui.EndTabItem();
			}
			if (ImGui.BeginTabItem("Blog Export"))
			{
				DrawExportTab(instance);
				ImGui.EndTabItem();
			}
			ImGui.EndTabBar();
		}
	}

	private void DrawStudioTab(ThumbnailStudio studio)
	{
		ImGui.BeginChild("Gallery", new Vector2(480f, 0f), border: true);
		DrawGallery(studio, exportMode: false);
		ImGui.EndChild();
		ImGui.SameLine();
		ImGui.BeginChild("Editor", new Vector2(0f, 0f), border: true);
		DrawEditor(studio);
		ImGui.EndChild();
	}

	private void DrawGallery(ThumbnailStudio studio, bool exportMode)
	{
		if (exportMode)
		{
			ImGui.InputText("Search##Export", ref _exportSearch, 64u);
		}
		else
		{
			ImGui.InputText("Search", ref _search, 64u);
		}
		string text = (exportMode ? _exportSearch : _search);
		ImGui.Checkbox(exportMode ? "Missing thumbnails only##Ex" : "Missing thumbnails only", ref _missingOnly);
		ImGui.SameLine();
		ImGui.Checkbox(exportMode ? "Paintable only##Ex" : "Paintable only", ref _paintableOnly);
		ImGui.SetNextItemWidth(160f);
		ImGui.Combo(exportMode ? "Type##Ex" : "Type", ref _typeFilter, TypeFilterLabels, TypeFilterLabels.Length);
		if (exportMode)
		{
			ImGui.TextDisabled("Click: add to export    Ctrl+Click: remove");
		}
		else
		{
			if (studio.BatchRemaining > 0)
			{
				ImGui.TextColored(WarnColor, $"Re-rendering… {studio.BatchTotal - studio.BatchRemaining}/{studio.BatchTotal}");
				ImGui.SameLine();
				if (ImGui.Button("Cancel##Batch"))
				{
					studio.CancelBatch();
				}
			}
			else if (ImGui.Button("Re-render all saved items"))
			{
				_status = studio.StartBatchRethumbnail();
			}
			if (ImGui.IsItemHovered() && studio.BatchRemaining == 0)
			{
				ImGui.SetTooltip("Regenerates thumbnails for every item with saved pose data, using each item's own saved rotation/zoom. Use after render-pipeline changes.");
			}
			ImGui.TextDisabled("Click: edit    Ctrl+Click: link-select");
		}
		ImGui.Separator();
		ImGui.BeginChild("GalleryScroll");
		IReadOnlyList<Thing> galleryPrefabs = ThumbnailStudio.GalleryPrefabs;
		if (galleryPrefabs.Count == 0)
		{
			ImGui.TextDisabled("No prefabs available (WorldManager not initialised?).");
		}
		int num = Mathf.Max(1, (int)(ImGui.GetContentRegionAvail().x / (CellSize.x + 14f)));
		int num2 = 0;
		foreach (Thing prefab in galleryPrefabs)
		{
			if (prefab == null || string.IsNullOrEmpty(prefab.PrefabName) || (_missingOnly && prefab.Thumbnail != null) || (_paintableOnly && !prefab.IsPaintable))
			{
				continue;
			}
			switch (_typeFilter)
			{
			case 1:
				if (!(prefab is DynamicThing))
				{
					continue;
				}
				break;
			case 2:
				if (!(prefab is Structure))
				{
					continue;
				}
				break;
			case 3:
				if (prefab is DynamicThing || prefab is Structure)
				{
					continue;
				}
				break;
			}
			if (text.Length > 0 && prefab.PrefabName.IndexOf(text, StringComparison.OrdinalIgnoreCase) < 0)
			{
				continue;
			}
			if (num2++ % num != 0)
			{
				ImGui.SameLine();
			}
			ImGui.PushID(prefab.PrefabHash);
			int num3 = 0;
			int num4 = (exportMode ? _exportItems.FindIndex((ThumbnailStudio.ExportItem e) => e.PrefabName == prefab.PrefabName) : (-1));
			if (exportMode)
			{
				if (num4 >= 0 && num4 == _exportHighlight)
				{
					ImGui.PushStyleColor(ImGuiCol.Button, CurrentTint);
					num3++;
				}
				else if (num4 >= 0)
				{
					ImGui.PushStyleColor(ImGuiCol.Button, LinkTint);
					num3++;
				}
			}
			else if (prefab == studio.Current)
			{
				ImGui.PushStyleColor(ImGuiCol.Button, CurrentTint);
				num3++;
			}
			else if (_linkSelection.Contains(prefab.PrefabName))
			{
				ImGui.PushStyleColor(ImGuiCol.Button, LinkTint);
				num3++;
			}
			Texture2D texture2D = ((prefab.Thumbnail != null) ? prefab.Thumbnail.texture : null);
			bool num5 = ((texture2D != null) ? ImGui.ImageButton((IntPtr)GetTextureId(texture2D), CellSize) : ImGui.Button("?", CellSize + new Vector2(6f, 6f)));
			if (num3 > 0)
			{
				ImGui.PopStyleColor(num3);
			}
			if (!exportMode)
			{
				Vector2 itemRectMin = ImGui.GetItemRectMin();
				Vector2 itemRectMax = ImGui.GetItemRectMax();
				bool flag = studio.HasSavedPose(prefab.PrefabName);
				ThumbnailLinkGroup linkGroup = studio.GetLinkGroup(prefab.PrefabName);
				if (flag)
				{
					ImGui.GetWindowDrawList().AddCircleFilled(new Vector2(itemRectMax.x - 8f, itemRectMin.y + 8f), 5f, ImGui.ColorConvertFloat4ToU32(new Vector4(0.25f, 0.95f, 0.3f, 1f)));
				}
				if (linkGroup != null)
				{
					ImGui.GetWindowDrawList().AddCircleFilled(new Vector2(itemRectMax.x - (flag ? 20f : 8f), itemRectMin.y + 8f), 5f, ImGui.ColorConvertFloat4ToU32(LinkDotColor));
				}
				if (ImGui.IsItemHovered())
				{
					string text2 = prefab.PrefabName;
					if (linkGroup != null && linkGroup.Leader != prefab.PrefabName)
					{
						text2 = text2 + "\nLinked to " + linkGroup.Leader;
					}
					else if (linkGroup != null)
					{
						text2 += $"\nLink leader ({linkGroup.Members.Count} items)";
					}
					if (flag)
					{
						text2 += "\nRight-click: apply its saved rotation/zoom here";
					}
					text2 += "\nDouble right-click: stamp current pose onto it & save";
					ImGui.SetTooltip(text2);
				}
				if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Right))
				{
					_pendingPoseApply = null;
					studio.Select(prefab, keepCurrentRotation: true);
					_status = studio.Save();
				}
				else if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
				{
					_pendingPoseApply = prefab.PrefabName;
					_pendingPoseTime = Time.unscaledTime;
				}
			}
			else if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip((num4 >= 0) ? (prefab.PrefabName + "\nIn export list - Ctrl+Click to remove") : (prefab.PrefabName + "\nClick to add to the export"));
			}
			if (num5)
			{
				if (exportMode)
				{
					if (ImGui.GetIO().KeyCtrl)
					{
						if (num4 >= 0)
						{
							RemoveExportItem(num4);
						}
					}
					else if (num4 >= 0)
					{
						HighlightExport(studio, num4);
					}
					else
					{
						_exportItems.Add(new ThumbnailStudio.ExportItem
						{
							PrefabName = prefab.PrefabName
						});
						HighlightExport(studio, _exportItems.Count - 1);
					}
				}
				else if (ImGui.GetIO().KeyCtrl)
				{
					if (!_linkSelection.Add(prefab.PrefabName))
					{
						_linkSelection.Remove(prefab.PrefabName);
					}
				}
				else
				{
					studio.Select(prefab, _keepRotation);
				}
			}
			ImGui.PopID();
		}
		ImGui.EndChild();
	}

	private void DrawEditor(ThumbnailStudio studio)
	{
		Thing current = studio.Current;
		if (current == null)
		{
			ImGui.Text("Select an item from the gallery to begin.");
			return;
		}
		ImGui.Text(current.PrefabName);
		ImGui.SameLine();
		ImGui.TextDisabled((current is Structure) ? "(structure)" : "(dynamic thing)");
		if (!string.IsNullOrEmpty(studio.Warning))
		{
			ImGui.TextColored(WarnColor, studio.Warning);
		}
		ImGui.Separator();
		ThumbnailLinkGroup thumbnailLinkGroup = studio.Links?.GroupFor(current.PrefabName);
		if (thumbnailLinkGroup != null && thumbnailLinkGroup.Leader != current.PrefabName)
		{
			ImGui.Text("Linked to:");
			ImGui.SameLine();
			ImGui.TextColored(LinkTextColor, thumbnailLinkGroup.Leader);
			if (ImGui.IsItemHovered())
			{
				ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
				ImGui.SetTooltip("Click to open");
			}
			if (ImGui.IsItemClicked() && !studio.SelectByName(thumbnailLinkGroup.Leader, _keepRotation))
			{
				_status = "Could not find '" + thumbnailLinkGroup.Leader + "'.";
			}
			ImGui.Spacing();
			if (ImGui.Button("Unlink this item"))
			{
				_status = studio.UnlinkCurrent();
			}
			if (!string.IsNullOrEmpty(_status))
			{
				ImGui.TextWrapped(_status);
			}
			return;
		}
		DrawPreview(studio);
		DrawRotation(studio);
		DrawZoom(studio);
		DrawColor(studio, current);
		DrawBuildState(studio, current);
		DrawThingStates(studio);
		DrawAnimationPose(studio);
		DrawLighting(studio);
		ImGui.Separator();
		DrawLinking(studio, current);
		ImGui.Separator();
		if (ImGui.Button("Save Thumbnail", new Vector2(220f, 36f)))
		{
			_status = studio.Save();
		}
		if (Thing.ThingHasThumbnailVariations(current))
		{
			ImGui.TextDisabled("Save renders every colour variant automatically.");
		}
		if (!string.IsNullOrEmpty(_status))
		{
			ImGui.TextWrapped(_status);
		}
		DrawSavedThumbnails(current);
	}

	private static void DrawSavedThumbnails(Thing current)
	{
		ImGui.Separator();
		ImGui.Text("Current thumbnails:");
		Vector2 size = new Vector2(96f, 96f);
		int shown = 0;
		List<ColorSwatch> list = ((Singleton<GameManager>.Instance != null) ? Singleton<GameManager>.Instance.CustomColors : null);
		Show(current.Thumbnail, "Base");
		if (current.Thumbnails != null)
		{
			for (int i = 0; i < current.Thumbnails.Length; i++)
			{
				string label = ((list != null && i < list.Count && !string.IsNullOrEmpty(list[i]?.Name)) ? list[i].Name : $"#{i}");
				Show(current.Thumbnails[i], label);
			}
		}
		if (current is Structure { BuildStates: not null } structure)
		{
			for (int j = 0; j < structure.BuildStates.Count; j++)
			{
				Show(structure.BuildStates[j]?.Thumbnail, $"State {j}");
			}
		}
		if (shown == 0)
		{
			ImGui.TextDisabled("None saved yet.");
		}
		void Show(Sprite sprite, string fmt)
		{
			if (!(sprite == null) && !(sprite.texture == null))
			{
				if (shown++ % 8 != 0)
				{
					ImGui.SameLine();
				}
				ImGui.BeginGroup();
				ImGui.Image((IntPtr)GetTextureId(sprite.texture), size);
				ImGui.TextDisabled(fmt);
				ImGui.EndGroup();
			}
		}
	}

	private void DrawPreview(ThumbnailStudio studio)
	{
		if (studio.PreviewTexture == null)
		{
			return;
		}
		Vector2 cursorPos = ImGui.GetCursorPos();
		Vector2 cursorScreenPos = ImGui.GetCursorScreenPos();
		ImGui.GetWindowDrawList().AddRectFilled(cursorScreenPos, cursorScreenPos + PreviewSize, ImGui.ColorConvertFloat4ToU32(new Vector4(0.16f, 0.16f, 0.18f, 1f)));
		ImGui.Image((IntPtr)GetTextureId(studio.PreviewTexture), PreviewSize);
		if (_overlayExisting && studio.OriginalThumbnail != null && studio.OriginalThumbnail.texture != null)
		{
			ImGui.SetCursorPos(cursorPos);
			ImGui.Image((IntPtr)GetTextureId(studio.OriginalThumbnail.texture), PreviewSize, Vector2.zero, Vector2.one, new Vector4(1f, 1f, 1f, _overlayOpacity));
		}
		Vector2 itemRectMin = ImGui.GetItemRectMin();
		Vector2 itemRectMax = ImGui.GetItemRectMax();
		ImDrawListPtr windowDrawList = ImGui.GetWindowDrawList();
		Vector4 vector = (studio.LastFillClipped ? new Vector4(1f, 0.25f, 0.25f, 0.95f) : ((studio.LastFillExtent >= 0.88f) ? new Vector4(0.3f, 1f, 0.35f, 0.9f) : new Vector4(1f, 0.65f, 0.15f, 0.9f)));
		windowDrawList.AddRect(itemRectMin, itemRectMax, ImGui.ColorConvertFloat4ToU32(vector), 0f, ImDrawFlags.None, 2f);
		uint col = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 0.12f));
		Vector2 vector2 = (itemRectMin + itemRectMax) * 0.5f;
		windowDrawList.AddLine(new Vector2(vector2.x, itemRectMin.y), new Vector2(vector2.x, itemRectMax.y), col);
		windowDrawList.AddLine(new Vector2(itemRectMin.x, vector2.y), new Vector2(itemRectMax.x, vector2.y), col);
		ImGui.SetCursorPos(cursorPos);
		ImGui.InvisibleButton("PreviewDrag", PreviewSize);
		if (ImGui.IsItemActive())
		{
			Vector2 mouseDelta = ImGui.GetIO().MouseDelta;
			if (mouseDelta.x != 0f || mouseDelta.y != 0f)
			{
				studio.EulerAngles.y -= mouseDelta.x * 0.4f;
				studio.EulerAngles.x += mouseDelta.y * 0.4f;
				studio.MarkDirty();
			}
		}
		ImGui.TextDisabled("Drag to rotate — border: green = fills frame, orange = undersized, red = clipped");
		if (studio.OriginalThumbnail != null)
		{
			ImGui.Checkbox("Overlay existing", ref _overlayExisting);
			ImGui.SameLine();
			ImGui.SetNextItemWidth(100f);
			ImGui.SliderFloat("##OverlayOpacity", ref _overlayOpacity, 0.1f, 0.9f);
			ImGui.SameLine();
			if (ImGui.Button("Auto-align"))
			{
				_status = studio.AutoAlignToOriginal();
			}
			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip("Full search of rotation space for the best silhouette match against the existing thumbnail (takes a few seconds)");
			}
			ImGui.SameLine();
			if (ImGui.Button("Refine from current"))
			{
				_status = studio.AutoAlignToOriginal(fromCurrentOnly: true);
			}
			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip("Roughly align by hand first, then this narrows in from YOUR current rotation only — fast, and it stays in the orientation you chose");
			}
		}
		else
		{
			ImGui.TextDisabled("No existing thumbnail to overlay for this item.");
		}
	}

	private void DrawRotation(ThumbnailStudio studio)
	{
		ImGui.Checkbox("Keep current rotation when selecting items", ref _keepRotation);
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip("Off: clicking an item restores the rotation/zoom it was last saved with.\nOn: the current rotation/zoom is applied to whatever you click.");
		}
		Vector3 eulerAngles = studio.EulerAngles;
		if (ImGui.SliderFloat("Pitch (X)", ref eulerAngles.x, -180f, 180f) | ImGui.SliderFloat("Yaw (Y)", ref eulerAngles.y, -180f, 180f) | ImGui.SliderFloat("Roll (Z)", ref eulerAngles.z, -180f, 180f))
		{
			studio.EulerAngles = eulerAngles;
			studio.MarkDirty();
		}
		ImGui.SetNextItemWidth(120f);
		ImGui.InputFloat("Snap degrees", ref _snapDegrees);
		if (_snapDegrees <= 0f)
		{
			_snapDegrees = 15f;
		}
		ImGui.Text("Snap rotate (world):");
		ImGui.SameLine();
		if (ImGui.Button("X-"))
		{
			Snap(studio, ref studio.EulerAngles.x, -1f);
		}
		ImGui.SameLine();
		if (ImGui.Button("X+"))
		{
			Snap(studio, ref studio.EulerAngles.x, 1f);
		}
		ImGui.SameLine();
		if (ImGui.Button("Y-"))
		{
			Snap(studio, ref studio.EulerAngles.y, -1f);
		}
		ImGui.SameLine();
		if (ImGui.Button("Y+"))
		{
			Snap(studio, ref studio.EulerAngles.y, 1f);
		}
		ImGui.SameLine();
		if (ImGui.Button("Z-"))
		{
			Snap(studio, ref studio.EulerAngles.z, -1f);
		}
		ImGui.SameLine();
		if (ImGui.Button("Z+"))
		{
			Snap(studio, ref studio.EulerAngles.z, 1f);
		}
		ImGui.Text("Rotate (object local):");
		ImGui.SameLine();
		if (ImGui.Button("X-##Local"))
		{
			studio.RotateLocal(Vector3.right, 0f - _snapDegrees);
		}
		ImGui.SameLine();
		if (ImGui.Button("X+##Local"))
		{
			studio.RotateLocal(Vector3.right, _snapDegrees);
		}
		ImGui.SameLine();
		if (ImGui.Button("Y-##Local"))
		{
			studio.RotateLocal(Vector3.up, 0f - _snapDegrees);
		}
		ImGui.SameLine();
		if (ImGui.Button("Y+##Local"))
		{
			studio.RotateLocal(Vector3.up, _snapDegrees);
		}
		ImGui.SameLine();
		if (ImGui.Button("Z-##Local"))
		{
			studio.RotateLocal(Vector3.forward, 0f - _snapDegrees);
		}
		ImGui.SameLine();
		if (ImGui.Button("Z+##Local"))
		{
			studio.RotateLocal(Vector3.forward, _snapDegrees);
		}
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip("Rotates about the object's own axes (follows the model as it turns)");
		}
		ImGui.SameLine();
		if (ImGui.Button("Snap all"))
		{
			studio.EulerAngles.x = Mathf.Round(studio.EulerAngles.x / _snapDegrees) * _snapDegrees;
			studio.EulerAngles.y = Mathf.Round(studio.EulerAngles.y / _snapDegrees) * _snapDegrees;
			studio.EulerAngles.z = Mathf.Round(studio.EulerAngles.z / _snapDegrees) * _snapDegrees;
			studio.MarkDirty();
		}
		ImGui.SameLine();
		if (ImGui.Button("Reset"))
		{
			studio.EulerAngles = Vector3.zero;
			studio.MarkDirty();
		}
		ImGui.SameLine();
		if (ImGui.Button("Load saved pose"))
		{
			_status = studio.LoadPrefabPose();
		}
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip("Restores the rotation/offset baked into the prefab — the pose the SHIPPED thumbnail was rendered with");
		}
		DrawRotationPresets(studio);
	}

	private void DrawRotationPresets(ThumbnailStudio studio)
	{
		ImGui.Spacing();
		if (!ImGui.CollapsingHeader("Saved rotations"))
		{
			return;
		}
		ImGui.SetNextItemWidth(160f);
		ImGui.InputText("##RotationName", ref _rotationName, 48u);
		ImGui.SameLine();
		if (ImGui.Button("Save rotation") && _rotationName.Trim().Length > 0)
		{
			studio.SaveRotationPreset(_rotationName);
		}
		List<ThumbnailRotationPreset> presets = studio.Rotations.Presets;
		if (presets.Count == 0)
		{
			ImGui.TextDisabled("No saved rotations yet — name the current angle and save it.");
			return;
		}
		ImGui.SetNextItemWidth(160f);
		ImGui.InputText("Filter##Rotations", ref _rotationSearch, 48u);
		for (int i = 0; i < presets.Count; i++)
		{
			ThumbnailRotationPreset thumbnailRotationPreset = presets[i];
			if (thumbnailRotationPreset != null && !string.IsNullOrEmpty(thumbnailRotationPreset.Name) && (_rotationSearch.Length <= 0 || thumbnailRotationPreset.Name.IndexOf(_rotationSearch, StringComparison.OrdinalIgnoreCase) >= 0))
			{
				ImGui.PushID(thumbnailRotationPreset.Name);
				if (ImGui.Button("Apply"))
				{
					studio.ApplyRotationPreset(thumbnailRotationPreset);
				}
				ImGui.SameLine();
				bool flag = ImGui.Button("X");
				ImGui.SameLine();
				ImGui.Text($"{thumbnailRotationPreset.Name}   ({thumbnailRotationPreset.X:F0}, {thumbnailRotationPreset.Y:F0}, {thumbnailRotationPreset.Z:F0})  zoom {thumbnailRotationPreset.Zoom:F2}");
				ImGui.PopID();
				if (flag)
				{
					studio.RemoveRotationPreset(thumbnailRotationPreset.Name);
					break;
				}
			}
		}
	}

	private void Snap(ThumbnailStudio studio, ref float axis, float direction)
	{
		axis = Mathf.Round((axis + direction * _snapDegrees) / _snapDegrees) * _snapDegrees;
		studio.MarkDirty();
	}

	private static void DrawThingStates(ThumbnailStudio studio)
	{
		if (!studio.HasPuppet || studio.PuppetParameters.Count == 0)
		{
			return;
		}
		ImGui.Spacing();
		if (!ImGui.CollapsingHeader("States", ImGuiTreeNodeFlags.DefaultOpen))
		{
			return;
		}
		foreach (ThumbnailStudio.PuppetParameter puppetParameter in studio.PuppetParameters)
		{
			ImGui.PushID(puppetParameter.Name);
			switch (puppetParameter.Type)
			{
			case AnimatorControllerParameterType.Bool:
			{
				bool v2 = puppetParameter.Value > 0.5f;
				if (ImGui.Checkbox(puppetParameter.Name, ref v2))
				{
					studio.SetPuppetParameter(puppetParameter, v2 ? 1f : 0f);
				}
				break;
			}
			case AnimatorControllerParameterType.Int:
			{
				if (BinaryStateNames.Contains(puppetParameter.Name))
				{
					bool v3 = puppetParameter.Value > 0.5f;
					if (ImGui.Checkbox(puppetParameter.Name, ref v3))
					{
						studio.SetPuppetParameter(puppetParameter, v3 ? 1f : 0f);
					}
					break;
				}
				int v4 = Mathf.RoundToInt(puppetParameter.Value);
				ImGui.SetNextItemWidth(200f);
				if (ImGui.SliderInt(puppetParameter.Name, ref v4, 0, 10))
				{
					studio.SetPuppetParameter(puppetParameter, v4);
				}
				break;
			}
			default:
			{
				float v = puppetParameter.Value;
				ImGui.SetNextItemWidth(200f);
				if (ImGui.SliderFloat(puppetParameter.Name, ref v, 0f, 1f))
				{
					studio.SetPuppetParameter(puppetParameter, v);
				}
				break;
			}
			}
			ImGui.PopID();
		}
	}

	private static void DrawAnimationPose(ThumbnailStudio studio)
	{
		AnimationClip[] animationClips = studio.AnimationClips;
		if (animationClips == null || animationClips.Length == 0)
		{
			return;
		}
		string[] array = new string[animationClips.Length + 1];
		array[0] = "Default pose";
		for (int i = 0; i < animationClips.Length; i++)
		{
			array[i + 1] = ((animationClips[i] != null) ? animationClips[i].name : $"Clip {i}");
		}
		int current_item = studio.AnimationClipIndex + 1;
		ImGui.SetNextItemWidth(260f);
		if (ImGui.Combo("Animation pose", ref current_item, array, array.Length))
		{
			studio.SetAnimationPose(current_item - 1, studio.AnimationTime);
		}
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip("Samples an animation clip to pose the model (e.g. open/closed states)");
		}
		if (studio.AnimationClipIndex >= 0)
		{
			float v = studio.AnimationTime;
			ImGui.SetNextItemWidth(260f);
			if (ImGui.SliderFloat("Pose time", ref v, 0f, 1f))
			{
				studio.SetAnimationPose(studio.AnimationClipIndex, v);
			}
		}
	}

	private static void DrawLighting(ThumbnailStudio studio)
	{
		ImGui.Spacing();
		if (!ImGui.CollapsingHeader("Lighting"))
		{
			return;
		}
		foreach (ThumbnailStudio.StudioLight light in studio.Lights)
		{
			if (!(light?.Light == null))
			{
				ImGui.PushID(light.Name);
				bool v = light.Light.enabled;
				if (ImGui.Checkbox(light.Name, ref v))
				{
					light.Light.enabled = v;
					studio.MarkDirty();
				}
				float v2 = light.Light.intensity;
				if (ImGui.SliderFloat("Intensity", ref v2, 0f, 2f))
				{
					light.Light.intensity = v2;
					studio.MarkDirty();
				}
				Vector3 euler = light.Euler;
				if (ImGui.SliderFloat("Pitch", ref euler.x, -89f, 89f) | ImGui.SliderFloat("Yaw", ref euler.y, -180f, 180f))
				{
					light.Euler = euler;
					light.Apply();
					studio.MarkDirty();
				}
				ImGui.Spacing();
				ImGui.PopID();
			}
		}
		float v3 = studio.Saturation;
		if (ImGui.SliderFloat("Saturation", ref v3, 0.3f, 1.5f))
		{
			studio.Saturation = v3;
		}
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip("Baked into SAVED thumbnails only (1 = unchanged); the live preview is unaffected");
		}
		if (ImGui.Button("Reset lights to defaults"))
		{
			studio.ResetLights();
		}
	}

	private static void DrawZoom(ThumbnailStudio studio)
	{
		float v = studio.ZoomMultiplier;
		if (ImGui.SliderFloat("Zoom", ref v, 0.4f, 2.5f))
		{
			studio.ZoomMultiplier = v;
			studio.MarkDirty();
		}
		ImGui.SameLine();
		if (ImGui.Button("Reset zoom"))
		{
			studio.ZoomMultiplier = 1f;
			studio.MarkDirty();
		}
		bool v2 = studio.AutoFitFraming;
		if (ImGui.Checkbox("Auto-fit framing", ref v2))
		{
			studio.AutoFitFraming = v2;
			studio.MarkDirty();
		}
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip("ON (default): the scene's fixed camera with the object auto-centred on its silhouette and zoomed to fill the frame.\nOFF: the object sits at its authored ThumbnailOffset — the exact framing of the shipped art.");
		}
	}

	private static void DrawColor(ThumbnailStudio studio, Thing current)
	{
		if (Singleton<GameManager>.Instance == null || !studio.CanColor)
		{
			return;
		}
		List<ColorSwatch> customColors = Singleton<GameManager>.Instance.CustomColors;
		string text = ((studio.ColorIndex >= 0 && studio.ColorIndex < customColors.Count && customColors[studio.ColorIndex] != null) ? customColors[studio.ColorIndex].Name : "Default");
		ImGui.Text("Colour: " + text);
		if (ImGui.Button("Default##Colour"))
		{
			studio.SetColor(-1);
		}
		int num = 1;
		for (int i = 0; i < customColors.Count; i++)
		{
			ColorSwatch colorSwatch = customColors[i];
			if (colorSwatch != null)
			{
				if (num++ % 16 != 0)
				{
					ImGui.SameLine();
				}
				Color color = colorSwatch.Color;
				if (ImGui.ColorButton($"##Swatch{i}", new Vector4(color.r, color.g, color.b, 1f), ImGuiColorEditFlags.NoTooltip, new Vector2(26f, 26f)))
				{
					studio.SetColor(i);
				}
				if (ImGui.IsItemHovered())
				{
					ImGui.SetTooltip(string.IsNullOrEmpty(colorSwatch.Name) ? $"Colour {i}" : colorSwatch.Name);
				}
			}
		}
	}

	private static void DrawBuildState(ThumbnailStudio studio, Thing current)
	{
		if (current is Structure { BuildStates: not null } structure && structure.BuildStates.Count > 1)
		{
			int num = structure.BuildStates.Count - 1;
			int v = Mathf.Clamp(studio.BuildStateIndex, 0, num);
			ImGui.SetNextItemWidth(260f);
			if (ImGui.SliderInt("Build state", ref v, 0, num))
			{
				studio.SetBuildState(v);
			}
			ImGui.SameLine();
			ImGui.TextDisabled((v == num) ? "(complete)" : $"(state {v})");
		}
	}

	private void DrawLinking(ThumbnailStudio studio, Thing current)
	{
		if (!ImGui.CollapsingHeader("Linking (share one thumbnail between items)"))
		{
			return;
		}
		ThumbnailLinkGroup thumbnailLinkGroup = studio.Links?.GroupFor(current.PrefabName);
		if (thumbnailLinkGroup != null)
		{
			ImGui.Text("Linked group — leader: " + thumbnailLinkGroup.Leader);
			foreach (string member in thumbnailLinkGroup.Members)
			{
				ImGui.BulletText(member);
			}
			if (ImGui.Button("Unlink this item"))
			{
				_status = studio.UnlinkCurrent();
			}
		}
		else
		{
			ImGui.TextDisabled("Not linked.");
		}
		if (_linkSelection.Count > 0)
		{
			ImGui.Text($"Link-selection: {_linkSelection.Count} item(s)");
			if (ImGui.Button("Link selected with '" + current.PrefabName + "' as leader"))
			{
				_status = studio.LinkSelected(_linkSelection);
				_linkSelection.Clear();
			}
			ImGui.SameLine();
			if (ImGui.Button("Clear selection"))
			{
				_linkSelection.Clear();
			}
		}
		else
		{
			ImGui.TextDisabled("Ctrl+Click gallery items to build a link group.");
		}
	}

	private void DrawCharactersTab(ThumbnailStudio studio)
	{
		ThumbnailCharacter character = studio.Character;
		if (character == null || !character.Available)
		{
			ImGui.TextColored(WarnColor, "No character avatar found in this scene.");
			ImGui.TextWrapped("Open the Thumbnail Studio from the main menu (which has a character mannequin) or in-game, where a character exists to clone. The avatar is cloned from whatever character is present.");
			return;
		}
		if (_charConfig == null)
		{
			_charConfig = new CharacterConfig();
		}
		CharacterKit kit = character.KitFor(_charConfig);
		if (_charDirty)
		{
			studio.RenderCharacterPreview(_charConfig);
			_charDirty = false;
		}
		ImGui.BeginChild("CharPreview", new Vector2(PreviewSize.x + 22f, 0f), border: true);
		DrawCharacterPreview(studio);
		ImGui.EndChild();
		ImGui.SameLine();
		ImGui.BeginChild("CharControls", new Vector2(0f, 0f), border: true);
		DrawCharacterControls(studio, character, kit);
		ImGui.EndChild();
	}

	private void DrawCharacterPreview(ThumbnailStudio studio)
	{
		if (studio.PreviewTexture == null)
		{
			return;
		}
		Vector2 cursorPos = ImGui.GetCursorPos();
		Vector2 cursorScreenPos = ImGui.GetCursorScreenPos();
		ImGui.GetWindowDrawList().AddRectFilled(cursorScreenPos, cursorScreenPos + PreviewSize, ImGui.ColorConvertFloat4ToU32(new Vector4(0.16f, 0.16f, 0.18f, 1f)));
		ImGui.Image((IntPtr)GetTextureId(studio.PreviewTexture), PreviewSize);
		ImGui.SetCursorPos(cursorPos);
		ImGui.InvisibleButton("CharDrag", PreviewSize);
		if (ImGui.IsItemActive())
		{
			Vector2 mouseDelta = ImGui.GetIO().MouseDelta;
			if (mouseDelta.x != 0f || mouseDelta.y != 0f)
			{
				_charConfig.Euler.y -= mouseDelta.x * 0.4f;
				_charConfig.Euler.x += mouseDelta.y * 0.4f;
				_charDirty = true;
			}
		}
		ImGui.TextDisabled("Drag to rotate");
	}

	private void DrawCharacterControls(ThumbnailStudio studio, ThumbnailCharacter character, CharacterKit kit)
	{
		bool flag = false;
		string[] array = new string[character.Kits.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = ((character.Kits[i] != null) ? character.Kits[i].name : $"Kit {i}");
		}
		if (array.Length != 0)
		{
			int current_item = Mathf.Clamp(_charConfig.KitIndex, 0, array.Length - 1);
			ImGui.SetNextItemWidth(260f);
			if (ImGui.Combo("Kit", ref current_item, array, array.Length))
			{
				_charConfig.KitIndex = current_item;
				flag = true;
			}
		}
		if (ImGui.Button("Randomize face"))
		{
			character.Randomize(_charConfig);
			flag = true;
		}
		ImGui.SameLine();
		if (ImGui.Button("Add to export"))
		{
			_exportItems.Add(new ThumbnailStudio.ExportItem
			{
				PrefabName = ((kit != null) ? ("Character: " + kit.name) : "Character"),
				Character = _charConfig.Clone()
			});
			_charStatus = "Added to the export grid - see the Blog Export tab.";
		}
		if (!string.IsNullOrEmpty(_charStatus))
		{
			ImGui.TextWrapped(_charStatus);
		}
		ImGui.Separator();
		ImGui.Text("Face");
		if (kit != null)
		{
			flag |= FaceSlider("Head", ref _charConfig.Head, kit.Heads.Length);
			flag |= FaceSlider("Eyes", ref _charConfig.Eyes, kit.Eyes.Length);
			flag |= FaceSlider("Eye colour", ref _charConfig.EyeColour, kit.EyeColours.Length);
			flag |= FaceSlider("Skin", ref _charConfig.Skin, kit.SkinColours.Length);
			flag |= FaceSlider("Hair", ref _charConfig.Hair, kit.Hairs.Length);
			flag |= FaceSlider("Hair colour", ref _charConfig.HairColour, kit.HairColours.Length);
			flag |= FaceSlider("Facial hair", ref _charConfig.FacialHair, kit.FacialHairs.Length);
			flag |= FaceSlider("Facial hair colour", ref _charConfig.FacialHairColour, kit.HairColours.Length);
		}
		string[] names = Enum.GetNames(typeof(BlendShapeType));
		int current_item2 = Mathf.Clamp((int)_charConfig.Expression, 0, names.Length - 1);
		ImGui.SetNextItemWidth(200f);
		if (ImGui.Combo("Expression", ref current_item2, names, names.Length))
		{
			_charConfig.Expression = (BlendShapeType)current_item2;
			flag = true;
		}
		ImGui.Separator();
		ImGui.Text("Wearing");
		if (ClothingCombo("Body / suit", character.BodyClothingOptions, ref _charConfig.BodyClothing))
		{
			_charConfig.BodyColor = -1;
			flag = true;
		}
		if (character.IsPaintable(_charConfig.BodyClothing))
		{
			flag |= ColorSwatchRow("Body colour", ref _charConfig.BodyColor);
		}
		if (ClothingCombo("Armor (over)", character.ArmorOptions, ref _charConfig.ArmorClothing))
		{
			_charConfig.ArmorColor = -1;
			flag = true;
		}
		if (character.IsPaintable(_charConfig.ArmorClothing))
		{
			flag |= ColorSwatchRow("Armor colour", ref _charConfig.ArmorColor);
		}
		ImGui.TextDisabled("Body = uniform/overalls or a full suit (space / hard / EVA). Armor layers on top. (Helmets, backpacks and held items are the next step.)");
		ImGui.TextDisabled("Gender: " + (character.IsCurrentFemale(_charConfig) ? "Female" : "Male") + " (from kit) - clothing uses the matching variant automatically.");
		string[] names2 = Enum.GetNames(typeof(HairMode));
		int current_item3 = Mathf.Clamp((int)_charConfig.HairMode, 0, names2.Length - 1);
		ImGui.SetNextItemWidth(200f);
		if (ImGui.Combo("Hair mode", ref current_item3, names2, names2.Length))
		{
			_charConfig.HairMode = (HairMode)current_item3;
			flag = true;
		}
		ImGui.Separator();
		float v = _charConfig.Zoom;
		ImGui.SetNextItemWidth(240f);
		if (ImGui.SliderFloat("Zoom", ref v, 0.4f, 2.5f))
		{
			_charConfig.Zoom = v;
			flag = true;
		}
		if (ImGui.Button("Reset pose"))
		{
			_charConfig.Euler = Vector3.zero;
			_charConfig.Zoom = 1f;
			flag = true;
		}
		if (flag)
		{
			_charDirty = true;
		}
	}

	private static bool FaceSlider(string label, ref int value, int count)
	{
		if (count <= 1)
		{
			return false;
		}
		int v = Mathf.Clamp(value, 0, count - 1);
		ImGui.SetNextItemWidth(240f);
		bool result = ImGui.SliderInt(label, ref v, 0, count - 1);
		value = v;
		return result;
	}

	private static bool ColorSwatchRow(string label, ref int colorIndex)
	{
		if (Singleton<GameManager>.Instance == null)
		{
			return false;
		}
		List<ColorSwatch> customColors = Singleton<GameManager>.Instance.CustomColors;
		bool result = false;
		ImGui.TextDisabled(label);
		if (ImGui.SmallButton("Default##" + label))
		{
			colorIndex = -1;
			result = true;
		}
		int num = 1;
		for (int i = 0; i < customColors.Count; i++)
		{
			ColorSwatch colorSwatch = customColors[i];
			if (colorSwatch != null)
			{
				if (num++ % 14 != 0)
				{
					ImGui.SameLine();
				}
				Color color = colorSwatch.Color;
				if (ImGui.ColorButton($"##{label}{i}", new Vector4(color.r, color.g, color.b, 1f), ImGuiColorEditFlags.NoTooltip, new Vector2(20f, 20f)))
				{
					colorIndex = i;
					result = true;
				}
				if (ImGui.IsItemHovered())
				{
					ImGui.SetTooltip(string.IsNullOrEmpty(colorSwatch.Name) ? $"Colour {i}" : colorSwatch.Name);
				}
			}
		}
		return result;
	}

	private static bool ClothingCombo(string label, List<string> options, ref string current)
	{
		string[] array = new string[options.Count + 1];
		array[0] = "(none)";
		for (int i = 0; i < options.Count; i++)
		{
			array[i + 1] = options[i];
		}
		int current_item = 0;
		if (!string.IsNullOrEmpty(current))
		{
			int num = options.IndexOf(current);
			current_item = ((num >= 0) ? (num + 1) : 0);
		}
		ImGui.SetNextItemWidth(300f);
		if (!ImGui.Combo(label, ref current_item, array, array.Length))
		{
			return false;
		}
		current = ((current_item == 0) ? null : options[current_item - 1]);
		return true;
	}

	private void DrawExportTab(ThumbnailStudio studio)
	{
		ImGui.BeginChild("ExportGallery", new Vector2(480f, 0f), border: true);
		DrawGallery(studio, exportMode: true);
		ImGui.EndChild();
		ImGui.SameLine();
		ImGui.BeginChild("ExportPanel", new Vector2(0f, 0f), border: true);
		DrawExportControls(studio);
		ImGui.EndChild();
	}

	private void HighlightExport(ThumbnailStudio studio, int index)
	{
		_exportHighlight = index;
		if (index >= 0 && index < _exportItems.Count)
		{
			ThumbnailStudio.ExportItem exportItem = _exportItems[index];
			if (exportItem.Character == null && studio.SelectByName(exportItem.PrefabName) && exportItem.UseOverride)
			{
				studio.EulerAngles = exportItem.Euler;
				studio.ZoomMultiplier = exportItem.Zoom;
				studio.SetColor(exportItem.ColorIndex);
				studio.MarkDirty();
			}
		}
	}

	private void RemoveExportItem(int index)
	{
		if (index >= 0 && index < _exportItems.Count)
		{
			_exportItems.RemoveAt(index);
			if (_exportItems.Count == 0)
			{
				_exportHighlight = -1;
			}
			else if (_exportHighlight >= _exportItems.Count)
			{
				_exportHighlight = _exportItems.Count - 1;
			}
			else if (index < _exportHighlight)
			{
				_exportHighlight--;
			}
		}
	}

	private void DrawExportControls(ThumbnailStudio studio)
	{
		ImGui.Text("Blog / hero image export");
		ImGui.TextDisabled("Pick objects from the gallery. They render on a transparent background, evenly spaced and equal in apparent size.");
		ImGui.Separator();
		ImGui.SetNextItemWidth(120f);
		ImGui.InputInt("Width", ref _exportWidth);
		ImGui.SameLine();
		ImGui.SetNextItemWidth(120f);
		ImGui.InputInt("Height", ref _exportHeight);
		ImGui.SameLine();
		if (ImGui.Button($"Reset to {1999}x{934}"))
		{
			_exportWidth = 1999;
			_exportHeight = 934;
		}
		_exportWidth = Mathf.Clamp(_exportWidth, 16, 8192);
		_exportHeight = Mathf.Clamp(_exportHeight, 16, 8192);
		int num = Mathf.Max(1, _exportItems.Count);
		_exportRows = Mathf.Clamp(_exportRows, 1, num);
		ImGui.SetNextItemWidth(200f);
		ImGui.SliderInt("Rows", ref _exportRows, 1, num);
		ImGui.SetNextItemWidth(200f);
		ImGui.SliderFloat("Padding", ref _exportPadding, 0f, 0.45f);
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip("Empty margin around each object within its grid cell.");
		}
		ImGui.Checkbox("Animated GIF (360° turntable)", ref _exportGif);
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip("Export a looping GIF where each subject spins a full 360° about its own centre on a transparent background. No drop shadow (GIF is 1-bit transparent). Keep it under 5 MB for Steam.");
		}
		if (_exportGif)
		{
			ImGui.SetNextItemWidth(200f);
			ImGui.SliderInt("GIF width", ref _gifWidth, 700, 1600);
			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip("Output width in px (height keeps the aspect above). Bigger = sharper but larger file. The status line reports the resulting size.");
			}
			ImGui.SetNextItemWidth(200f);
			ImGui.SliderInt("Frames", ref _gifFrames, 4, 120);
			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip("Exact number of frames. More = smoother + higher fps, but larger file.");
			}
			ImGui.SetNextItemWidth(200f);
			ImGui.SliderFloat("Rotation seconds", ref _gifSeconds, 2f, 15f);
			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip("Time for one full 360° rotation (higher = slower spin). fps = frames / this.");
			}
			ImGui.Checkbox("Dither", ref _gifDither);
			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip("Floyd-Steinberg dithering - smoother colour gradients, at the cost of some speckle and a slightly larger file. Off suits flat-shaded art.");
			}
			ImGui.TextDisabled("Tune width/frames to fill the 5 MB budget; size is shown after export.");
		}
		else
		{
			ImGui.Checkbox("Drop shadow", ref _exportDropShadow);
			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip("A soft shadow under each subject. The grid is inset so it never touches the image edge.");
			}
			if (_exportDropShadow)
			{
				ImGui.SameLine();
				ImGui.SetNextItemWidth(160f);
				ImGui.SliderFloat("Shadow strength", ref _exportShadowOpacity, 0.05f, 0.8f);
			}
		}
		int num2 = Mathf.CeilToInt((float)_exportItems.Count / (float)Mathf.Max(1, _exportRows));
		ImGui.TextDisabled($"{_exportItems.Count} object(s): {num2} col x {_exportRows} row");
		bool num3 = _exportItems.Count > 0;
		if (!num3)
		{
			ImGui.BeginDisabled();
		}
		if (ImGui.Button(_exportGif ? "Export GIF" : "Export PNG", new Vector2(180f, 34f)))
		{
			_exportStatus = (_exportGif ? studio.ExportTurntableGif(_exportItems, _exportWidth, _exportHeight, _exportRows, _exportPadding, _gifWidth, _gifSeconds, _gifFrames, _gifDither) : studio.ExportSheet(_exportItems, _exportWidth, _exportHeight, _exportRows, _exportPadding, _exportDropShadow, _exportShadowOpacity));
		}
		if (!num3)
		{
			ImGui.EndDisabled();
		}
		ImGui.SameLine();
		if (ImGui.Button("Open render folder"))
		{
			Application.OpenURL(ThumbnailPaths.ExportFolder);
		}
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip(ThumbnailPaths.ExportFolder);
		}
		if (!string.IsNullOrEmpty(_exportStatus))
		{
			ImGui.TextWrapped(_exportStatus);
		}
		ImGui.Separator();
		ImGui.Text("Objects in export:");
		if (_exportItems.Count == 0)
		{
			ImGui.TextDisabled("Click items in the gallery to add them.");
		}
		for (int i = 0; i < _exportItems.Count; i++)
		{
			ThumbnailStudio.ExportItem exportItem = _exportItems[i];
			ImGui.PushID(i);
			if (ImGui.Button("Up") && i > 0)
			{
				SwapExportItems(i, i - 1);
			}
			ImGui.SameLine();
			if (ImGui.Button("Dn") && i < _exportItems.Count - 1)
			{
				SwapExportItems(i, i + 1);
			}
			ImGui.SameLine();
			bool num4 = ImGui.Button("X");
			ImGui.SameLine();
			if (ImGui.Selectable(string.Format("{0}. {1}{2}", i + 1, exportItem.PrefabName, exportItem.UseOverride ? "  (custom angle)" : ""), i == _exportHighlight))
			{
				HighlightExport(studio, i);
			}
			ImGui.PopID();
			if (num4)
			{
				RemoveExportItem(i);
				break;
			}
		}
		if (_exportHighlight < 0 || _exportHighlight >= _exportItems.Count)
		{
			return;
		}
		if (_exportItems[_exportHighlight].Character != null)
		{
			ImGui.Separator();
			ImGui.TextDisabled("Character item - edit its look and pose on the Characters tab, then re-add it. (Removing and re-adding replaces it.)");
		}
		else
		{
			if (studio.Current == null)
			{
				return;
			}
			ImGui.Separator();
			ImGui.Text("Editing angle: " + studio.Current.PrefabName);
			bool num5 = ImGui.Button("Reset to saved pose");
			if (num5)
			{
				if (!studio.ApplySavedPose(studio.Current.PrefabName))
				{
					_status = studio.LoadPrefabPose();
				}
				_exportItems[_exportHighlight].UseOverride = false;
			}
			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip("Reverts this object to the pose saved in the Studio tab.");
			}
			DrawPreview(studio);
			DrawRotation(studio);
			DrawZoom(studio);
			DrawColor(studio, studio.Current);
			if (!num5)
			{
				ThumbnailStudio.ExportItem exportItem2 = _exportItems[_exportHighlight];
				exportItem2.UseOverride = true;
				exportItem2.Euler = studio.EulerAngles;
				exportItem2.Zoom = studio.ZoomMultiplier;
				exportItem2.ColorIndex = studio.ColorIndex;
			}
		}
	}

	private void SwapExportItems(int a, int b)
	{
		List<ThumbnailStudio.ExportItem> exportItems = _exportItems;
		List<ThumbnailStudio.ExportItem> exportItems2 = _exportItems;
		ThumbnailStudio.ExportItem exportItem = _exportItems[b];
		ThumbnailStudio.ExportItem exportItem2 = _exportItems[a];
		ThumbnailStudio.ExportItem exportItem3 = (exportItems[a] = exportItem);
		exportItem3 = (exportItems2[b] = exportItem2);
		if (_exportHighlight == a)
		{
			_exportHighlight = b;
		}
		else if (_exportHighlight == b)
		{
			_exportHighlight = a;
		}
	}
}
