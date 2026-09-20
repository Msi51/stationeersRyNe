using Assets.Scripts.Localization2;
using Assets.Scripts.Util;
using Effects;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class Flashlight : PowerTool, IWearableLight, IReferencable, IEvaluable
{
	[SerializeField]
	private MaterialChanger _materialChanger;

	private bool IsHeldByLocalPlayer
	{
		get
		{
			if (RootParentHuman != null)
			{
				return RootParentHuman.IsLocalPlayer;
			}
			return false;
		}
	}

	private bool IsInHandSlot
	{
		get
		{
			if (RootParentHuman != null)
			{
				if (base.ParentSlot != RootParentHuman.LeftHandSlot)
				{
					return base.ParentSlot == RootParentHuman.RightHandSlot;
				}
				return true;
			}
			return false;
		}
	}

	private bool IsInLeftHand
	{
		get
		{
			if (RootParentHuman != null)
			{
				return base.ParentSlot == RootParentHuman.LeftHandSlot;
			}
			return false;
		}
	}

	private bool LightIsActive
	{
		get
		{
			if ((base.ParentSlot == null || IsInHandSlot) && OnOff)
			{
				return IsOperable;
			}
			return false;
		}
	}

	private bool IsLowPower => Mode == 0;

	private bool IsHighPower => Mode == 1;

	public override string[] ModeStrings { get; } = new string[2]
	{
		GameStrings.FlashLightModeLowPower,
		GameStrings.FlashLightModeHighPower
	};

	public override void OnStartRender()
	{
		base.OnStartRender();
		UpdateBeamStates();
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		InteractableType action = interactable.Action;
		if (action == InteractableType.Mode || action == InteractableType.OnOff || action == InteractableType.Powered)
		{
			UpdateBeamStates();
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		UpdateBeamStates();
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Mode)
		{
			if (doAction)
			{
				OnServer.Interact(base.InteractMode, (Mode + 1) % 2);
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		if (OnOff && Powered && (object)base.Battery != null)
		{
			base.Battery.PowerStored -= (IsHighPower ? UsedPowerActive : (IsLowPower ? UsedPowerPassive : 0f));
		}
	}

	public override void SetLightVisibility(bool isVisible, bool hideOnPlayer = false)
	{
		UpdateBeamStates();
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		UpdateBeamStates();
	}

	public override void OnExitInventory(Thing parent)
	{
		base.OnExitInventory(parent);
		UpdateBeamStates();
	}

	public override bool MoveToWorld(Vector3 worldPosition, Quaternion worldRotation, Vector3 velocity, Vector3 angularVelocity, float force = 0f)
	{
		UpdateBeamStates();
		return base.MoveToWorld(worldPosition, worldRotation, velocity, angularVelocity, force);
	}

	public override void Awake()
	{
		base.Awake();
		if (!IsCursor)
		{
			Thing.AllIWearableLights.Add(this);
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (!Singleton<GameManager>.IsQuitting)
		{
			Thing.AllIWearableLights.Remove(this);
		}
	}

	private void UpdateBeamStates()
	{
		if (!GameManager.IsBatchMode)
		{
			bool flag = IsLowPower && LightIsActive;
			bool flag2 = IsHighPower && LightIsActive;
			Lights[0].SetVisible(flag, flag && !IsHeldByLocalPlayer);
			Lights[1].SetVisible(flag2, flag2 && !IsHeldByLocalPlayer);
			if (!flag)
			{
				Lights[0].Light.cullingMask = ThingLight.BlankMask;
			}
			if (!flag2)
			{
				Lights[1].Light.cullingMask = ThingLight.BlankMask;
			}
			if (IsInHandSlot)
			{
				ParentBeamsToPlayer();
			}
			else
			{
				ParentBeamsToTorch();
			}
			_materialChanger.ChangeState(LightIsActive ? Defines.Animator.OnPowered : Defines.Animator.Off);
		}
	}

	private void ParentBeamsToPlayer()
	{
		Transform obj = Lights[0].Light.transform;
		Transform parent = (Lights[1].Light.transform.parent = RootParentHuman.HeadBone.transform);
		obj.parent = parent;
		Transform obj2 = Lights[0].Light.transform;
		Vector3 localPosition = (Lights[1].Light.transform.localPosition = new Vector3(0.25f, 0.25f, IsInLeftHand ? 0.25f : (-0.25f)));
		obj2.localPosition = localPosition;
		Transform obj3 = Lights[0].Light.transform;
		Quaternion localRotation = (Lights[1].Light.transform.localRotation = Quaternion.Euler(-45f, -90f, 0f));
		obj3.localRotation = localRotation;
	}

	private void ParentBeamsToTorch()
	{
		Transform obj = Lights[0].Light.transform;
		Transform parent = (Lights[1].Light.transform.parent = base.transform);
		obj.parent = parent;
		Transform obj2 = Lights[0].Light.transform;
		Vector3 localPosition = (Lights[1].Light.transform.localPosition = new Vector3(-0.25f, -0.023f, -0.005f));
		obj2.localPosition = localPosition;
		Transform obj3 = Lights[0].Light.transform;
		Quaternion localRotation = (Lights[1].Light.transform.localRotation = Quaternion.Euler(0f, -90f, 0f));
		obj3.localRotation = localRotation;
	}
}
