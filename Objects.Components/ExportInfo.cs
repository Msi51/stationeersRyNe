using System;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using UnityEngine;

namespace Objects.Components;

[Serializable]
public class ExportInfo
{
	public ExportAnimationComponent AnimationComponent;

	public ConnectionRole ConnectionRole;

	public InteractableType InteractableType;

	public InteractableType SlotType;

	[HideInInspector]
	public Slot Slot;

	[HideInInspector]
	public Connection Connection;

	[HideInInspector]
	public Interactable Interactable;

	[HideInInspector]
	public bool ForceUpdateAnimState;

	[HideInInspector]
	public bool IsCurrentAnimComplete;

	[HideInInspector]
	public BinaryAnimState PreviousAnimState;

	[HideInInspector]
	public int ExportCount;

	[HideInInspector]
	public Chute ExportChute;

	[HideInInspector]
	public DynamicThing ExportingThing;
}
