using System;
using System.Threading;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Trading;
using UnityEngine;

namespace Objects.Items;

public class PneumaticMiningDrill : Tool, IMiningTool, IReferencable, IEvaluable
{
	[Space(15f)]
	[Header("PneumaticMiningDrill")]
	[SerializeField]
	private float _mineAmount = 0.4f;

	[SerializeField]
	private float _minOperationPressureDifferential;

	[SerializeField]
	private float _maxPressureDifferential;

	[SerializeField]
	private float _minMolesPerTick;

	[SerializeField]
	private float _maxMolesPerTick = 0.2f;

	[SerializeField]
	private float _idleUsageMolesPerTick = 0.05f;

	[SerializeField]
	private float _fastestMineTime = 0.08f;

	[SerializeField]
	private float _slowestMineTime = 0.2f;

	[SerializeField]
	private GasCanister _fuelTank;

	private bool _drillInUse;

	private static readonly string[] _modeStrings = Enum.GetNames(typeof(CursorVoxelMode));

	private float _mineTime;

	public override int EquipSoundHash => Animator.StringToHash("EquipMiningTool");

	public override int UnEquipSoundHash => Animator.StringToHash("UnEquipMiningTool");

	public override string[] ModeStrings => _modeStrings;

	public override bool IsOperable
	{
		get
		{
			if (OnOff && !IsEmpty)
			{
				return PressureDifferential() > new PressurekPa(_minOperationPressureDifferential);
			}
			return false;
		}
	}

	private bool IsEmpty
	{
		get
		{
			if (_fuelTank?.InternalAtmosphere != null)
			{
				return _fuelTank.InternalAtmosphere.TotalMolesGases <= Chemistry.MINIMUM_VALID_TOTAL_MOLES;
			}
			return true;
		}
	}

	public override bool Powered => IsOperable;

	private float MineTime
	{
		get
		{
			return _mineTime;
		}
		set
		{
			_mineTime = value;
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 256;
			}
		}
	}

	public CursorVoxelMode CursorVoxelMode => (CursorVoxelMode)Mode;

	public override void Awake()
	{
		base.Awake();
		if (GameManager.GameState != GameState.None)
		{
			AtmosphericsManager.Instance.Register(this);
			MineTime = _slowestMineTime;
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		AtmosphericsManager.Instance.Deregister(this);
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, new VolumeLitres(1.0), 0L);
		}
	}

	public override bool CheckTogglePower()
	{
		if (base.CanTogglePower)
		{
			return !IsEmpty;
		}
		return false;
	}

	public override void OnUsePrimary(Vector3 targetLocation, Quaternion targetRotation, ulong steamId, bool authoringMode)
	{
		base.OnUsePrimary(targetLocation, targetRotation, steamId, authoringMode);
		AssignVoxelLayerMask();
		if (!_drillInUse && IsOperable)
		{
			UseDrill().Forget();
		}
	}

	private void AssignVoxelLayerMask()
	{
		if (RootParent.HasAuthority && base.ParentSlot == InventoryManager.ActiveHandSlot)
		{
			CursorManager instance = CursorManager.Instance;
			instance.CursorHitMask = (int)instance.CursorHitMask | (int)LayerMasks.CursorVoxel;
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		Ore ore = interaction.DestinationThing as Ore;
		if ((bool)ore && IsOperable)
		{
			if (!doAction)
			{
				return DelayedActionInstance.Success(ActionStrings.Collect);
			}
			if (GameManager.RunSimulation)
			{
				OnMinedOre(ore);
			}
			return DelayedActionInstance.Success(ActionStrings.Collect);
		}
		if (interactable.Action == InteractableType.Mode)
		{
			switch ((CursorVoxelMode)Mode)
			{
			case CursorVoxelMode.Default:
				if (!doAction || !GameManager.RunSimulation)
				{
					return DelayedActionInstance.Success("Set");
				}
				OnServer.Interact(base.InteractMode, 1);
				break;
			case CursorVoxelMode.Flatten:
				if (!doAction || !GameManager.RunSimulation)
				{
					return DelayedActionInstance.Success("Set");
				}
				OnServer.Interact(base.InteractMode, 0);
				break;
			}
			return DelayedActionInstance.Success("set");
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.OnOff)
		{
			if (interactable.State == 1)
			{
				PlayPooledAudioSound(Defines.Sounds.SwitchOn, Vector3.zero);
			}
			else
			{
				PlayPooledAudioSound(Defines.Sounds.SwitchOff, Vector3.zero);
			}
		}
	}

	private async UniTaskVoid WaitForRunning(Thing parent)
	{
		float waitTime = 30f;
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		while (GameManager.GameState != GameState.Running && waitTime > 0f)
		{
			waitTime -= Time.deltaTime;
			await UniTask.NextFrame(cancelToken);
		}
		if (!cancelToken.IsCancellationRequested)
		{
			AssignVoxelLayerMask();
		}
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		if (GameManager.GameState != GameState.Running)
		{
			WaitForRunning(parent).Forget();
		}
		else
		{
			AssignVoxelLayerMask();
		}
	}

	public override void OnExitInventory(Thing oldParent)
	{
		base.OnExitInventory(oldParent);
		if (oldParent.HasAuthority)
		{
			CursorManager instance = CursorManager.Instance;
			instance.CursorHitMask = (int)instance.CursorHitMask & ~(int)LayerMasks.CursorVoxel;
		}
	}

	private async UniTaskVoid UseDrill()
	{
		_drillInUse = true;
		Thing.Interact(base.InteractActivate, 1);
		while (KeyManager.GetMouse("Primary") && !KeyManager.GetButton(KeyMap.SwapHands) && IsOperable)
		{
			await UniTask.NextFrame();
		}
		Thing.Interact(base.InteractActivate, 0);
		_drillInUse = false;
	}

	private PressurekPa PressureDifferential()
	{
		PressurekPa pressurekPa = ((base.WorldAtmosphere != null && base.WorldAtmosphere.IsValid()) ? base.WorldAtmosphere.PressureGassesAndLiquids : PressurekPa.Zero);
		PressurekPa pressureGassesAndLiquids = _fuelTank.InternalAtmosphere.PressureGassesAndLiquids;
		return RocketMath.Max(PressurekPa.Zero, pressureGassesAndLiquids - pressurekPa);
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		EjectInternalAtmosphere();
		if (IsOperable)
		{
			PressurekPa pressurekPa = PressureDifferential();
			float outMax = ((Activate == 1) ? _maxMolesPerTick : _idleUsageMolesPerTick);
			MoleQuantity transferMoles = new MoleQuantity(RocketMath.MapToScale(_minOperationPressureDifferential, _maxPressureDifferential, _minMolesPerTick, outMax, pressurekPa.ToFloat()));
			GasMixture gasMixture = _fuelTank.InternalAtmosphere.Remove(transferMoles, AtmosphereHelper.MatterState.All);
			base.InternalAtmosphere.Add(gasMixture);
			if (Activate == 1)
			{
				float value = RocketMath.MapToScale(_minOperationPressureDifferential, _maxPressureDifferential, _slowestMineTime, _fastestMineTime, pressurekPa.ToFloat());
				MineTime = Mathf.Clamp(value, _fastestMineTime, _slowestMineTime);
			}
		}
	}

	private void EjectInternalAtmosphere()
	{
		if (base.WorldAtmosphere == null || base.WorldAtmosphere.Mode == AtmosphereHelper.AtmosphereMode.Global)
		{
			base.WorldAtmosphere = base.GridController.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
		}
		base.WorldAtmosphere.Add(base.InternalAtmosphere.GasMixture);
		base.InternalAtmosphere.GasMixture.Reset();
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		_fuelTank = newChild as GasCanister;
		MineTime = _slowestMineTime;
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		_fuelTank = null;
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(MineTime);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		MineTime = reader.ReadSingle();
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteSingle(MineTime);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			MineTime = reader.ReadSingle();
		}
	}

	public bool IsAvailable()
	{
		return IsOperable;
	}

	public float GetMineCompletionTime()
	{
		return MineTime;
	}

	public float GetMineAmount()
	{
		return _mineAmount;
	}
}
