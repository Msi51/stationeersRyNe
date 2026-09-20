using System;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Structures;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Items;

public class WeldingTorch : Tool, IWelder, IConstructor, IUsed, IThermal
{
	private static readonly Vector3 LeftHandRot = new Vector3(-75f, 127f, -126f);

	private static readonly Vector3 LeftHandPos = new Vector3(-0.152f, 0.076f, 0.044f);

	private static readonly Vector3 RightHandRot = new Vector3(67f, -119f, -128f);

	private static readonly Vector3 RightHandPos = new Vector3(-0.155f, 0.079f, -0.019f);

	[Header("Welding Torch")]
	public GameObject Flame;

	public ParticleSystem Sparks;

	public Light FlameLight;

	[SerializeField]
	[FormerlySerializedAs("MolesPerTick")]
	private float molesPerTick = 0.01f;

	[NonSerialized]
	[ReadOnly]
	public GasCanister FuelTank;

	public static readonly int EquipWelderHash = Animator.StringToHash("EquipWelder");

	public static readonly int UnEquipWelderHash = Animator.StringToHash("UnEquipWelder");

	private float _usedQuantity;

	private GasCanister _batteryPrefab;

	private Color _colorTorch;

	protected bool ToolSpeedAdditionActive;

	public override bool IsOperable
	{
		get
		{
			if (!IsEmpty && OnOff)
			{
				return Inflamed;
			}
			return false;
		}
	}

	public bool IsEmpty
	{
		get
		{
			if (FuelTank?.InternalAtmosphere != null)
			{
				if (FuelTank.InternalAtmosphere.GasMixture.TotalFuel <= MoleQuantity.Zero)
				{
					return FuelTank.InternalAtmosphere.GasMixture.TotalHypergolics <= MoleQuantity.Zero;
				}
				return false;
			}
			return true;
		}
	}

	public MoleQuantity MolesPerTick => new MoleQuantity(molesPerTick);

	public override int EquipSoundHash => EquipWelderHash;

	public override int UnEquipSoundHash => UnEquipWelderHash;

	public bool Inflamed => base.InternalAtmosphere?.Inflamed ?? false;

	public override bool HasReadableAtmosphere => true;

	public override bool CheckTogglePower()
	{
		if (base.CanTogglePower)
		{
			return !IsEmpty;
		}
		return false;
	}

	public override void SetHandPosition(bool leftHand)
	{
		if (leftHand)
		{
			LocalRotationInHand = LeftHandRot;
			LocalOffSetInHand = LeftHandPos;
		}
		else
		{
			LocalRotationInHand = RightHandRot;
			LocalOffSetInHand = RightHandPos;
		}
		base.ThingTransformLocalPosition = LocalOffSetInHand;
		base.ThingTransformLocalRotationEuler = LocalRotationInHand;
	}

	public void SendStartWelding()
	{
		OnServer.WeldEffect(base.ReferenceId, CursorManager.CursorHit.point, isEnabled: true);
		SetSparks(CursorManager.CursorHit.point);
	}

	public void SendStopWelding()
	{
		OnServer.WeldEffect(base.ReferenceId, default(Vector3), isEnabled: false);
		ResetSparks();
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.OnOff)
		{
			if (IsEmpty)
			{
				return DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.WeldingTorchNoFuel);
			}
			if (!doAction)
			{
				return DelayedActionInstance.Success(interactable.ContextualName);
			}
			OnServer.Interact(interactable, (interactable.State != 1) ? 1 : 0);
			return DelayedActionInstance.Success(interactable.ContextualName);
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override bool OnUseItem(float quantity, Thing onUseThing)
	{
		if (!OnOff || !GameManager.RunSimulation || FuelTank == null)
		{
			return true;
		}
		if (quantity <= 0f)
		{
			quantity = 100f;
		}
		_usedQuantity += quantity;
		return true;
	}

	public override void OnPrimaryUseStart()
	{
		base.OnPrimaryUseStart();
		if (Activate != 1)
		{
			SendStartWelding();
			Thing.Interact(base.InteractActivate, 1);
			GasCanister fuelTank = FuelTank;
			if ((object)fuelTank != null && fuelTank.InternalAtmosphere?.GasMixture.GetGasTypeRatio(Chemistry.GasType.NitrousOxide) > 0.1f)
			{
				Achievements.Achieve(Achievements.Kind.AchievementFastAndFurious);
			}
		}
	}

	public override void OnPrimaryUseEnd()
	{
		base.OnPrimaryUseEnd();
		SendStopWelding();
		Thing.Interact(base.InteractActivate, 0);
	}

	public void ResetSparks()
	{
		Sparks.transform.parent = ThingTransform;
		Sparks.transform.localPosition = Vector3.zero;
		Sparks.gameObject.SetActive(value: false);
	}

	public void SetSparks(Vector3 position)
	{
		Sparks.transform.parent = null;
		Sparks.transform.position = position;
		Sparks.gameObject.SetActive(value: true);
	}

	public override void OnAnimationStop()
	{
		base.OnAnimationStop();
		if (OnOff && base.InternalAtmosphere != null)
		{
			base.InternalAtmosphere.Sparked = true;
		}
	}

	public override void UpdateEachFrame()
	{
		if (WorldManager.IsGamePaused)
		{
			return;
		}
		base.UpdateEachFrame();
		if (!IsOccluded)
		{
			if (Inflamed && !Flame.gameObject.activeSelf)
			{
				Flame.gameObject.SetActive(value: true);
			}
			if (!Inflamed && Flame.gameObject.activeSelf)
			{
				Flame.gameObject.SetActive(value: false);
			}
			_ = Flame.gameObject.activeSelf;
		}
	}

	private void EjectInternalAtmosphere()
	{
		_ = base.InternalAtmosphere.Inflamed;
		if (base.WorldAtmosphere == null || base.WorldAtmosphere.Mode == AtmosphereHelper.AtmosphereMode.Global)
		{
			base.WorldAtmosphere = base.GridController.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
		}
		base.WorldAtmosphere.Add(base.InternalAtmosphere.GasMixture);
		if (base.WorldAtmosphere != null)
		{
			base.WorldAtmosphere.Sparked = true;
		}
		base.InternalAtmosphere.GasMixture.Reset();
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (!OnOff)
		{
			if (base.InternalAtmosphere.TotalMoles > Chemistry.MINIMUM_VALID_TOTAL_MOLES)
			{
				EjectInternalAtmosphere();
			}
			return;
		}
		if (OnOff && (!FuelTank || IsEmpty))
		{
			OnServer.Interact(base.InteractOnOff, 0);
			return;
		}
		EjectInternalAtmosphere();
		GasMixture gasMixture = FuelTank.InternalAtmosphere.Remove(MolesPerTick + new MoleQuantity(_usedQuantity) * MolesPerTick, AtmosphereHelper.MatterState.All);
		base.InternalAtmosphere.Add(gasMixture);
		base.InternalAtmosphere.Sparked = true;
		base.InternalAtmosphere.TryCombust(1.0, force: true);
		ToolSpeedAdditionActive = base.InternalAtmosphere.Temperature > new TemperatureKelvin(3000.0) + Chemistry.Temperature.ZeroDegrees;
		_usedQuantity = 0f;
	}

	public override float getToolSpeed()
	{
		return base.getToolSpeed() + (ToolSpeedAdditionActive ? 0.5f : 0.2f);
	}

	public override void Awake()
	{
		base.Awake();
		if (GameManager.GameState != GameState.None)
		{
			AtmosphericsManager.Instance.Register(this);
		}
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, new VolumeLitres(1.0), 0L);
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		AtmosphericsManager.Instance?.Deregister(this);
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		FuelTank = newChild as GasCanister;
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		FuelTank = null;
		if (GameManager.RunSimulation)
		{
			Thing.Interact(base.InteractOnOff, (FuelTank != null) ? 1 : 0);
		}
	}

	public void OnTankExploded()
	{
		if (base.ParentSlot?.Parent is Locker)
		{
			Achievements.AchieveDidYouForgetSomething();
		}
	}
}
