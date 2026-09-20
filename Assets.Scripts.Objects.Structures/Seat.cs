using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Structures;

public class Seat : Device, ISmartRotatable, IExitable
{
	public Transform CameraPoint;

	private Transform _cameraRig;

	[SerializeField]
	private Transform _ExitTransform;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	private static readonly float _radiusCheck = 0.05f;

	public virtual Vector3 ExitPosition => _ExitTransform.position;

	public Slot SeatSlot => Slots[0];

	public bool FreeLook => true;

	public virtual void Exit(Human parent)
	{
		Vector3 worldPosition = ExitPosition;
		if (Physics.CheckSphere(worldPosition, _radiusCheck))
		{
			Vector3 entityForward = parent.EntityForward;
			float z = _ExitTransform.localPosition.z;
			Vector3 vector = _ExitTransform.position - entityForward * z;
			worldPosition = vector - entityForward * z;
			if (Physics.CheckSphere(worldPosition, _radiusCheck))
			{
				Vector3 right = ThingTransform.right;
				worldPosition = vector + right * z;
				if (Physics.CheckSphere(worldPosition, _radiusCheck))
				{
					worldPosition = vector - right * z;
				}
			}
		}
		parent.MoveToWorld(worldPosition, parent.ParentSlot.Parent.Rotation, Vector3.zero, Vector3.zero);
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.ChairTableCategory);
	}

	public override string GetStationpediaCategoryKey()
	{
		return StationpediaCategoryStrings.ChairTableCategory;
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Slot1)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 2f,
				ActionMessage = ActionStrings.GetIn
			};
			DynamicThing occupant = interaction.SourceSlot.Occupant;
			if ((bool)interactable.Slot.Occupant || (bool)occupant)
			{
				return HandleSwitch(interaction, interactable.Slot.SlotIndex, delayedActionInstance, doAction);
			}
			if (GameManager.RunSimulation && doAction)
			{
				OnServer.MoveToSlot(interaction.SourceThing.AsDynamicThing, interactable.Slot);
			}
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = (int[])permutation.Clone();
	}

	public void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}

	public int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}

	public Vector3 GetExitPosition(Entity entity)
	{
		return ExitPosition;
	}

	public Transform GetCameraPoint(Entity entity)
	{
		return CameraPoint;
	}
}
