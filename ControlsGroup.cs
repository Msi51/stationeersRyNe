using System.Collections.Generic;
using UnityEngine;

public class ControlsGroup
{
	public static readonly List<ControlsGroup> AllControlGroups = new List<ControlsGroup>();

	public readonly string Name;

	public List<KeyItem> KeyItems = new List<KeyItem>();

	public Transform Transform;

	public ControlsGroup(string name)
	{
		Name = name;
		KeyManager.AddGroupLookup(this);
		AllControlGroups.Add(this);
	}
}
