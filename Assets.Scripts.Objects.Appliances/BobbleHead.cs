using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Structures;
using Assets.Scripts.Util;
using Effects;
using UnityEngine;

namespace Assets.Scripts.Objects.Appliances;

public class BobbleHead : Appliance
{
	[Header("Bobble Head")]
	[SerializeField]
	private MaterialChanger _baseLightMaterialChanger;

	[SerializeField]
	private MaterialChanger _helmetLightMaterialChanger;

	[SerializeField]
	private MeshRenderer _helmetMeshRenderer;

	[SerializeField]
	private MeshRenderer _bodyMeshRenderer;

	[SerializeField]
	private GameObject _leftCord;

	[SerializeField]
	private GameObject _rightCord;

	[SerializeField]
	private Rigidbody _headRigidbody;

	[SerializeField]
	private float _pokeForceMultiplier;

	[SerializeField]
	private Transform _pokeForcePoint;

	private static readonly int _poweredState = Animator.StringToHash("Powered");

	private static readonly int _unPoweredState = Animator.StringToHash("UnPowered");

	private static readonly int MASK_COLOR = Shader.PropertyToID("_MaskColor");

	protected override bool HasPaintableMaskMaterial => true;

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Activate)
		{
			if (doAction)
			{
				_headRigidbody.AddForceAtPosition(Random.insideUnitSphere * _pokeForceMultiplier, _pokeForcePoint.position, ForceMode.Impulse);
				if (GameManager.RunSimulation)
				{
					AudioEvent.Create(this, Defines.Sounds.BobbleHead);
				}
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override void BenchPowerStateChanged(bool receivingPower)
	{
		ChangeLightMaterials(receivingPower);
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		if (parent is Bench bench)
		{
			if (bench.Slots[0].Occupant == this)
			{
				ShowPowerCord(left: true);
			}
			else if (bench.Slots[1].Occupant == this)
			{
				ShowPowerCord(left: false);
			}
			bool powered = bench.Powered && bench.OnOff;
			ChangeLightMaterials(powered);
		}
	}

	public override void OnExitInventory(Thing oldParent)
	{
		base.OnExitInventory(oldParent);
		if (oldParent is Bench)
		{
			HidePowerCord();
			ChangeLightMaterials(powered: false);
		}
	}

	private void ChangeLightMaterials(bool powered)
	{
		int id = (powered ? _poweredState : _unPoweredState);
		_baseLightMaterialChanger.ChangeState(id);
		_helmetLightMaterialChanger.ChangeState(id);
		SetCustomColor(CustomColor.Index);
	}

	private void ShowPowerCord(bool left)
	{
		_leftCord.SetActive(!left);
		_rightCord.SetActive(left);
	}

	private void HidePowerCord()
	{
		_leftCord.SetActive(value: false);
		_rightCord.SetActive(value: false);
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		DynamicThing sourceItem = attack.SourceItem;
		if (!sourceItem)
		{
			return null;
		}
		if (!(sourceItem is Labeller labeller))
		{
			return base.AttackWith(attack, doAction);
		}
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = ActionStrings.Rename
		};
		if (!labeller.OnOff)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
		}
		if (!labeller.IsOperable)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
		}
		if (!doAction)
		{
			return delayedActionInstance;
		}
		labeller.Rename(this);
		return delayedActionInstance;
	}

	public override void SetCustomColor(int index, bool emissive = false)
	{
		base.SetCustomColor(index, emissive);
		if (CustomColor != null)
		{
			_helmetMeshRenderer.material.SetColor(MASK_COLOR, CustomColor.Color);
			_bodyMeshRenderer.material.SetColor(MASK_COLOR, CustomColor.Color);
		}
	}
}
