using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Cysharp.Threading.Tasks;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class MiningDrill : PowerTool, IMiningTool, IReferencable, IEvaluable
{
	[Header("Pickaxe")]
	[Tooltip("Time it takes to mine 1 asteriod voxel")]
	public float MineCompletionTime = 0.1f;

	[Tooltip("The amount that this tool mines at a time")]
	public float MineAmount = 0.2f;

	private bool _drillInUse;

	public string EquipSoundName;

	public string UnEquipSoundName;

	private static readonly int SpeedState = Animator.StringToHash("SpeedState");

	private static readonly string[] _modeStrings = new string[2]
	{
		GameStrings.MiningDrillModeDefault,
		GameStrings.MiningDrillModeFlatten
	};

	public override int EquipSoundHash => Animator.StringToHash(EquipSoundName);

	public override int UnEquipSoundHash => Animator.StringToHash(UnEquipSoundName);

	public override string[] ModeStrings => _modeStrings;

	public CursorVoxelMode CursorVoxelMode => (CursorVoxelMode)Mode;

	public bool IsAvailable()
	{
		if (IsOperable && OnOff)
		{
			return Powered;
		}
		return false;
	}

	public float GetMineCompletionTime()
	{
		return MineCompletionTime;
	}

	public float GetMineAmount()
	{
		return MineAmount;
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Activate)
		{
			BaseAnimator.SetFloat(SpeedState, interactable.State);
		}
	}

	public override void OnUsePrimary(Vector3 targetLocation, Quaternion targetRotation, ulong steamId, bool authoringMode)
	{
		base.OnUsePrimary(targetLocation, targetRotation, steamId, authoringMode);
		AssignVoxelLayerMask();
		if (!_drillInUse && OnOff && Powered)
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
		if ((bool)ore && OnOff && Powered)
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

	public override void Awake()
	{
		base.Awake();
		if (GameManager.GameState != GameState.None)
		{
			AtmosphericsManager.Instance.Register(this);
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		AtmosphericsManager.Instance.Deregister(this);
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
		while (KeyManager.GetMouse("Primary") && !KeyManager.GetButton(KeyMap.SwapHands) && OnOff && Powered)
		{
			await UniTask.NextFrame();
		}
		Thing.Interact(base.InteractActivate, 0);
		_drillInUse = false;
	}
}
