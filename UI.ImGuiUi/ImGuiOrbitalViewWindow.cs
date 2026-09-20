using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Assets.Scripts;
using Assets.Scripts.UI.ImGuiUi;
using ImGuiNET;
using UI.ImGuiUi.ImGuiWindows;
using UnityEngine;

namespace UI.ImGuiUi;

public class ImGuiOrbitalViewWindow : UI.ImGuiUi.ImGuiWindows.ImGuiWindow
{
	public static ImGuiOrbitalViewWindow Window = new ImGuiOrbitalViewWindow();

	private const float DEFAULT_SURFACE_HEIGHT = 120f;

	private const float DEFAULT_CURVATURE = 0.5f;

	private bool _overrideSurfaceHeight;

	private float _surfaceHeight = 120f;

	private bool _overrideAtmosphereDepth;

	private float _atmosphereDepth = 800f;

	private bool _overrideCurvature;

	private float _curvature = 0.5f;

	private bool _overrideFalloff;

	private float _falloff = 0.25f;

	private bool _overrideLift;

	private float _lift = 500f;

	private bool _overrideColour;

	private Vector3 _colour = new Vector3(0.4f, 0.62f, 1f);

	private string _status = "";

	public ImGuiOrbitalViewWindow()
		: base("Orbital View Tuning", new Vector2(440f, 420f))
	{
	}

	public override void OnOpen()
	{
		_status = "";
		OrbitalViewData orbitalViewData = WorldSetting.Current?.Data?.OrbitalView;
		_overrideSurfaceHeight = orbitalViewData != null && orbitalViewData.SurfaceHeight > 0f;
		_surfaceHeight = (_overrideSurfaceHeight ? orbitalViewData.SurfaceHeight : 120f);
		_overrideAtmosphereDepth = orbitalViewData != null && orbitalViewData.AtmosphereDepth > 0f;
		_atmosphereDepth = (_overrideAtmosphereDepth ? orbitalViewData.AtmosphereDepth : Mathf.Max(OrbitalViewController.AutoAtmosphereDepth, 100f));
		_overrideCurvature = orbitalViewData != null && orbitalViewData.Curvature > 0f;
		_curvature = (_overrideCurvature ? orbitalViewData.Curvature : 0.5f);
		_overrideFalloff = orbitalViewData != null && orbitalViewData.AtmosphereFalloff > 0f;
		_falloff = (_overrideFalloff ? orbitalViewData.AtmosphereFalloff : 0.25f);
		_overrideLift = orbitalViewData != null && orbitalViewData.PlanetLift > 0f;
		_lift = (_overrideLift ? orbitalViewData.PlanetLift : 500f);
		_overrideColour = orbitalViewData != null && orbitalViewData.AtmosphereColor.a > 0f;
		Color color = (_overrideColour ? orbitalViewData.AtmosphereColor : OrbitalViewController.DerivedAtmosphereColour);
		_colour = new Vector3(color.r, color.g, color.b);
	}

	public override void OnClose()
	{
	}

	public override void DrawContent()
	{
		WorldSetting current = WorldSetting.Current;
		if (current?.Data == null)
		{
			ImGui.TextDisabled("No world loaded.");
			return;
		}
		ImGui.Text("World: ");
		ImGui.SameLine();
		ImGui.TextColored(ImGuiColor.Float4.Green, current.Id);
		ImGui.TextDisabled("Tick Override to take control of a value; un-ticked values stay automatic.");
		ImGui.Separator();
		DrawFloatSetting("Surface Height", "m, terrain height the atmosphere sphere fits to", ref _overrideSurfaceHeight, ref _surfaceHeight, 1f, 0f, 1500f, $"default {120f:0}");
		DrawFloatSetting("Atmosphere Depth", "m above surface; should clear the tallest mountains", ref _overrideAtmosphereDepth, ref _atmosphereDepth, 5f, 50f, 5000f, $"auto {OrbitalViewController.AutoAtmosphereDepth:0} (from pressure)");
		DrawFloatSetting("Sphere Amount", "orbital curvature; higher = rounder ball", ref _overrideCurvature, ref _curvature, 0.005f, 0.05f, 1f, $"default {0.5f:0.00}");
		DrawFloatSetting("Atmosphere Falloff", "scale height as a fraction of depth; low = dense surface layer, high = soft glow filling the shell", ref _overrideFalloff, ref _falloff, 0.005f, 0.05f, 1f, $"default {0.25f:0.00}");
		DrawFloatSetting("Planet Lift", "m the whole planet rises toward the camera in space, making it bigger in the sky", ref _overrideLift, ref _lift, 5f, 0f, 1800f, "default 0 (no lift)");
		ImGui.Checkbox("Override##Colour", ref _overrideColour);
		ImGui.SameLine();
		if (_overrideColour)
		{
			ImGui.ColorEdit3("Atmosphere Colour", ref _colour);
		}
		else
		{
			Color derivedAtmosphereColour = OrbitalViewController.DerivedAtmosphereColour;
			ImGui.TextDisabled("Atmosphere Colour: derived from gas composition");
			ImGui.SameLine();
			ImGui.ColorButton("##DerivedColour", new Vector4(derivedAtmosphereColour.r, derivedAtmosphereColour.g, derivedAtmosphereColour.b, 1f));
		}
		ApplyToWorldData(current);
		ImGui.Separator();
		if (ImGui.Button("Save Overrides To World XML"))
		{
			SaveOverrides(current);
		}
		ImGui.SameLine();
		if (ImGui.Button("Clear All Overrides"))
		{
			_overrideSurfaceHeight = false;
			_overrideAtmosphereDepth = false;
			_overrideCurvature = false;
			_overrideFalloff = false;
			_overrideLift = false;
			_overrideColour = false;
		}
		ImGui.TextDisabled("Save writes only the overridden values; with none overridden it removes the block.");
		if (!string.IsNullOrEmpty(_status))
		{
			ImGui.TextWrapped(_status);
		}
	}

	private static void DrawFloatSetting(string label, string tooltip, ref bool overrideFlag, ref float value, float speed, float min, float max, string defaultText)
	{
		ImGui.Checkbox("Override##" + label, ref overrideFlag);
		ImGui.SameLine();
		if (overrideFlag)
		{
			ImGui.DragFloat(label, ref value, speed, min, max);
			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip(tooltip);
			}
		}
		else
		{
			ImGui.TextDisabled(label + ": " + defaultText);
		}
	}

	private void ApplyToWorldData(WorldSetting worldSetting)
	{
		WorldSettingData data = worldSetting.Data;
		OrbitalViewData obj = data.OrbitalView ?? (data.OrbitalView = new OrbitalViewData());
		obj.SurfaceHeight = (_overrideSurfaceHeight ? _surfaceHeight : 0f);
		obj.AtmosphereDepth = (_overrideAtmosphereDepth ? _atmosphereDepth : 0f);
		obj.Curvature = (_overrideCurvature ? _curvature : 0f);
		obj.AtmosphereFalloff = (_overrideFalloff ? _falloff : 0f);
		obj.PlanetLift = (_overrideLift ? _lift : 0f);
		obj.AtmosphereColor = (_overrideColour ? new Color(_colour.x, _colour.y, _colour.z, 1f) : Color.clear);
	}

	private void SaveOverrides(WorldSetting worldSetting)
	{
		string text = FindWorldXmlPath(worldSetting.Id);
		if (text == null)
		{
			_status = "Could not find a world XML containing World Id=\"" + worldSetting.Id + "\" under StreamingAssets/Worlds.";
			return;
		}
		try
		{
			XDocument xDocument = XDocument.Load(text);
			XElement xElement = xDocument.Descendants("World").First((XElement w) => (string)w.Attribute("Id") == worldSetting.Id);
			xElement.Element("OrbitalView")?.Remove();
			XElement xElement2 = BuildOverrideElement();
			if (xElement2 != null)
			{
				XElement xElement3 = xElement.Element("AtmosphericScatteringData");
				if (xElement3 != null)
				{
					xElement3.AddBeforeSelf(xElement2);
				}
				else
				{
					xElement.Add(xElement2);
				}
			}
			xDocument.Save(text);
			_status = ((xElement2 != null) ? ("Saved overrides to " + text) : ("No overrides set — removed <OrbitalView> from " + text));
		}
		catch (Exception ex)
		{
			_status = "Save failed: " + ex.Message;
		}
	}

	private XElement BuildOverrideElement()
	{
		if (!_overrideSurfaceHeight && !_overrideAtmosphereDepth && !_overrideCurvature && !_overrideFalloff && !_overrideLift && !_overrideColour)
		{
			return null;
		}
		XElement xElement = new XElement("OrbitalView");
		if (_overrideSurfaceHeight)
		{
			xElement.Add(new XElement("SurfaceHeight", _surfaceHeight));
		}
		if (_overrideAtmosphereDepth)
		{
			xElement.Add(new XElement("AtmosphereDepth", _atmosphereDepth));
		}
		if (_overrideCurvature)
		{
			xElement.Add(new XElement("Curvature", _curvature));
		}
		if (_overrideFalloff)
		{
			xElement.Add(new XElement("AtmosphereFalloff", _falloff));
		}
		if (_overrideLift)
		{
			xElement.Add(new XElement("PlanetLift", _lift));
		}
		if (_overrideColour)
		{
			xElement.Add(new XElement("AtmosphereColor", new XElement("r", _colour.x), new XElement("g", _colour.y), new XElement("b", _colour.z), new XElement("a", 1f)));
		}
		return xElement;
	}

	private static string FindWorldXmlPath(string worldId)
	{
		string path = Path.Combine(Application.streamingAssetsPath, "Worlds");
		if (!Directory.Exists(path))
		{
			return null;
		}
		string[] files = Directory.GetFiles(path, "*.xml", SearchOption.AllDirectories);
		foreach (string text in files)
		{
			try
			{
				if (XDocument.Load(text).Descendants("World").Any((XElement w) => (string)w.Attribute("Id") == worldId))
				{
					return text;
				}
			}
			catch
			{
			}
		}
		return null;
	}
}
