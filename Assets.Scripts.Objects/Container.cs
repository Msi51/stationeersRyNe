using System.Collections.Generic;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Structures;
using Assets.Scripts.Util;
using Assets.Scripts.Vehicles;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class Container : DraggableThing, IUnfastenable, IReferencable, IEvaluable
{
	[Header("Container")]
	public CrateType CrateContents;

	[ReadOnly]
	public CrateMount CrateMount;

	private List<Item> _buildingSupplies;

	private Vector3 ChildRotation = new Vector3(45f, 90f, 90f);

	[SerializeField]
	private ContainerOpenAnimComponent openAnimComponent;

	private bool _obscuredDrag;

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		SetContentsVisibility(IsOpen);
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (openAnimComponent != null)
		{
			openAnimComponent.RefreshState(skipAnimation);
		}
	}

	public override void SetSlotOccupantTransformData(DynamicThing newChild)
	{
		if ((object)newChild != null)
		{
			newChild.ThingTransformLocalRotation = Quaternion.Euler(ChildRotation + newChild.ChildSlotOffset);
			newChild.ScaleToSlot(0.9f);
			newChild.ThingTransformLocalPosition = newChild.ChildSlotOffsetPosition;
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		previousChild.ThingTransform.localScale = Vector3.one;
	}

	public void InitContainer()
	{
		switch (CrateContents)
		{
		case CrateType.Empty:
			return;
		case CrateType.BuildingSupplies:
			_buildingSupplies = new List<Item>
			{
				Prefab.Find<Item>("ItemKitArcFurnace"),
				Prefab.Find<Item>("ItemKitAutolathe"),
				Prefab.Find<Item>("ItemIronFrames"),
				Prefab.Find<Item>("ItemIronSheets"),
				Prefab.Find<Item>("ItemAreaPowerControl"),
				Prefab.Find<Item>("ItemBatteryCellLarge"),
				Prefab.Find<Item>("ItemKitSolidGenerator"),
				Prefab.Find<Item>("ItemKitWallIron"),
				Prefab.Find<Item>("ItemKitSolarPanel"),
				Prefab.Find<Item>("ItemGlassSheets")
			};
			break;
		case CrateType.PipeSupplies:
			_buildingSupplies = new List<Item>
			{
				Prefab.Find<Item>("ItemPortablesConnector"),
				Prefab.Find<Item>("ItemActiveVent"),
				Prefab.Find<Item>("ItemKitConsole"),
				Prefab.Find<Item>("CircuitboardAirlockControl"),
				Prefab.Find<Item>("ItemKitAirlock"),
				Prefab.Find<Item>("ItemKitAirlock"),
				Prefab.Find<Item>("ItemPipeValve"),
				Prefab.Find<Item>("ItemKitPipe"),
				Prefab.Find<Item>("ItemKitSensor"),
				Prefab.Find<Item>("ItemBatteryCharger")
			};
			break;
		case CrateType.CableSupplies:
			_buildingSupplies = new List<Item>
			{
				Prefab.Find<Item>("ItemWireCutters"),
				Prefab.Find<Item>("ItemKitConsole"),
				Prefab.Find<Item>("ItemDataDisk"),
				Prefab.Find<Item>("ItemCableCoil"),
				Prefab.Find<Item>("CircuitboardAirControl"),
				Prefab.Find<Item>("CircuitboardPowerControl"),
				Prefab.Find<Item>("CircuitboardModeControl"),
				Prefab.Find<Item>("CircuitboardAirlockControl"),
				Prefab.Find<Item>("CircuitboardGasDisplay"),
				Prefab.Find<Item>("CircuitboardDoorControl")
			};
			break;
		case CrateType.ConveyorSupplies:
			_buildingSupplies = new List<Item>
			{
				Prefab.Find<Item>("ItemCrowbar"),
				Prefab.Find<Item>("ItemWrench"),
				Prefab.Find<Item>("ItemDrill"),
				Prefab.Find<Item>("ItemKitConsole"),
				Prefab.Find<Item>("ItemDataDisk"),
				Prefab.Find<Item>("ItemCableCoil"),
				Prefab.Find<Item>("ItemKitConveyor"),
				Prefab.Find<Item>("ItemKitConveyor"),
				Prefab.Find<Item>("ItemKitChute"),
				Prefab.Find<Item>("ItemKitFurnace")
			};
			break;
		case CrateType.Eggs:
			_buildingSupplies = new List<Item>
			{
				Prefab.Find<Item>("ItemEgg"),
				Prefab.Find<Item>("ItemEgg"),
				Prefab.Find<Item>("ItemEgg"),
				Prefab.Find<Item>("ItemEgg"),
				Prefab.Find<Item>("ItemEgg"),
				Prefab.Find<Item>("ItemEgg")
			};
			break;
		}
		for (int i = 0; i < Slots.Count; i++)
		{
			if (i < _buildingSupplies.Count)
			{
				Item item = Thing.Create<Item>(_buildingSupplies[i]);
				item.RigidBody.useGravity = false;
				if (item is Stackable stackable)
				{
					stackable.SetQuantity(stackable.MaxQuantity);
				}
				OnServer.MoveToSlot(item, Slots[i]);
			}
		}
	}

	public void SetContentsVisibility(bool isVisible)
	{
		foreach (Interactable interactable in Interactables)
		{
			if (interactable.Action != InteractableType.Open && interactable.Action != InteractableType.Button1 && (bool)interactable.Collider)
			{
				interactable.Collider.enabled = isVisible;
			}
		}
		foreach (Slot slot in Slots)
		{
			if ((bool)slot.Occupant)
			{
				slot.Occupant.SetVisibility(isVisible);
			}
		}
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action == InteractableType.Button1 && _obscuredDrag)
		{
			return base.GetContextualName(interactable) + "\n" + GameStrings.DeviceCannotDragCollision.AsString();
		}
		return base.GetContextualName(interactable);
	}

	public override void OnDestroy()
	{
		if (!Singleton<GameManager>.IsQuitting)
		{
			base.OnDestroy();
		}
	}

	public IContainerMount GetStructuralMount()
	{
		return (GridController.GetController(CenterPosition).GetOther(CenterPosition) as IContainerMount) ?? (GridController.GetController(CenterPosition).GetSmallCell(CenterPosition)?.Device as IContainerMount);
	}

	public IContainerMount GetRoverMount()
	{
		return Rover.IsNearby(this);
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if (base.Joint != null || !(attack.SourceItem as Wrench))
		{
			return base.AttackWith(attack, doAction);
		}
		IContainerMount structuralMount = GetStructuralMount();
		Rover rover = GetRoverMount() as Rover;
		Lander lander = null;
		if (base.ParentSlot != null)
		{
			lander = base.ParentSlot.Parent as Lander;
		}
		if (structuralMount == null && rover == null && lander == null)
		{
			return base.AttackWith(attack, doAction);
		}
		DelayedActionInstance result = new DelayedActionInstance
		{
			Duration = 1f,
			ActionMessage = ((base.ParentSlot != null) ? ActionStrings.Disconnect : ActionStrings.Connect)
		};
		if (!doAction)
		{
			return result;
		}
		if (GameManager.RunSimulation)
		{
			if (base.ParentSlot != null)
			{
				OnServer.MoveToWorld(this);
			}
			else if (rover != null)
			{
				rover.Attach(this);
			}
			else
			{
				if (structuralMount?.ContainerSlot == null)
				{
					return result;
				}
				OnServer.MoveToSlot(this, structuralMount.ContainerSlot);
			}
		}
		return result;
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		CrateMount crateMount = parent as CrateMount;
		if (crateMount != null)
		{
			CrateMount = crateMount;
			if ((bool)BaseAnimator)
			{
				BaseAnimator.SetBool(DraggableThing.AnchoredState, value: true);
			}
		}
	}

	public override void OnExitInventory(Thing parent)
	{
		base.OnExitInventory(parent);
		if (CrateMount == parent)
		{
			CrateMount = null;
			if ((bool)BaseAnimator)
			{
				BaseAnimator.SetBool(DraggableThing.AnchoredState, value: false);
			}
		}
	}

	public override bool MoveToWorld(float force = 0f)
	{
		bool result = base.MoveToWorld(force);
		if (GameManager.RunSimulation)
		{
			RigidBody.AddForce(Vector3.up * 3f);
			RigidBody.AddTorque(Random.insideUnitCircle);
		}
		return result;
	}
}
