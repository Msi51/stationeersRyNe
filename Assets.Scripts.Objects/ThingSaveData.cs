using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Objects.Electrical;
using Reagents;
using UnityEngine;

namespace Assets.Scripts.Objects;

[XmlRoot]
public class ThingSaveData : ReferencableSaveData
{
	[XmlElement]
	public string PrefabName;

	[XmlElement]
	public string CustomName;

	[XmlElement]
	public Vector3 WorldPosition;

	[XmlElement]
	public Quaternion WorldRotation;

	[XmlArray("States")]
	[XmlArrayItem("State")]
	public List<InteractableState> States = new List<InteractableState>();

	[XmlElement]
	public bool IsCustomName;

	[XmlElement]
	public int CustomColorIndex = -1;

	[XmlElement]
	public ulong OwnerSteamId;

	[XmlArray("Reagents")]
	[XmlArrayItem("Reagent")]
	public List<ReagentSaveData> Reagents = new List<ReagentSaveData>();

	[XmlElement]
	public bool Indestructable;

	[XmlElement]
	public DamageUpdate DamageState;

	[XmlElement("LogicStack")]
	public LogicStackData LogicStack;

	public virtual bool IsValidData()
	{
		bool result = true;
		if (ReferenceId < 0)
		{
			result = false;
			ConsoleWindow.PrintError($"ReferenceId cannot be negative: {ReferenceId} - {PrefabName}");
		}
		if (string.IsNullOrEmpty(PrefabName))
		{
			result = false;
			ConsoleWindow.PrintError($"Prefab name cannot be null or empty: {ReferenceId}");
		}
		if (float.IsNaN(WorldPosition.x))
		{
			result = false;
			ConsoleWindow.PrintError($"World position is NaN on thing with reference id: {ReferenceId}");
		}
		if (float.IsNaN(WorldPosition.x))
		{
			result = false;
			ConsoleWindow.PrintError($"Thing save data with reference id: {ReferenceId} is not valid.");
		}
		return result;
	}
}
