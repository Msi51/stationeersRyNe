using System;
using System.Collections.Generic;
using Assets.Scripts.Events;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using UnityEngine;

namespace Objects.Components;

[Serializable]
public class ImportInfo
{
	public GenericAssignableAnimComponent AnimationComponent;

	public MachineInputTrigger Trigger;

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
	public List<DynamicThing> ImportQueue = new List<DynamicThing>();

	[HideInInspector]
	public int ImportCount;

	[HideInInspector]
	public Chute ImportChute;

	[HideInInspector]
	public DynamicThing ImportingThing;
}
