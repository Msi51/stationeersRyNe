using System.Threading;
using System.Threading.Tasks;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Util;

namespace Assets.Scripts.Objects.Weapons;

public class ProjectileLauncher : Tool
{
	[Header("ProjectileLauncher")]
	public float FirePower = 256f;

	public Vector3 MuzzlePosition = new Vector3(-0.4f, 0.05f, 0f);

	public Transform MuzzleFlashTransform;

	public float CooldownSeconds = 0.64f;

	private bool _supressOnSlotEnter;

	private CancellationTokenWrapper _startCooldownCoolDownCancellation = new CancellationTokenWrapper();

	private CancellationTokenWrapper _doEffectsCancellation = new CancellationTokenWrapper();

	private Stackable Magazine => Slots[0]?.Occupant as Stackable;

	private Slot Chamber => Slots[1];

	private bool IsLoaded => Magazine != null;

	public override bool IsOperable
	{
		get
		{
			if (base.IsOperable)
			{
				return IsLoaded;
			}
			return false;
		}
	}

	public bool SupressFire
	{
		get
		{
			if (Activate != 1)
			{
				return _supressOnSlotEnter;
			}
			return true;
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.ManualTools);
	}

	public Vector3 MuzzleInWorld()
	{
		return base.ThingTransformPosition + ThingTransform.rotation * MuzzlePosition;
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		_startCooldownCoolDownCancellation.CancelAndInitialize();
		StartCooldown(_startCooldownCoolDownCancellation.Token).Forget();
	}

	public override void OnUsePrimary(Vector3 targetLocation, Quaternion targetRotation, ulong steamId, bool authoringMode)
	{
		base.OnUsePrimary(targetLocation, targetRotation, steamId, authoringMode);
		if (IsOperable && Activate != 1)
		{
			Vector3 vector = MuzzleInWorld();
			Vector3 normalized = (RootParentHuman.AimIk.position - vector).normalized;
			Vector3 velocity = normalized * FirePower;
			if (GameManager.RunSimulation)
			{
				Fire(vector, velocity);
				return;
			}
			NetworkMessages.FireProjectileMessage fireProjectileMessage = new NetworkMessages.FireProjectileMessage();
			fireProjectileMessage.LauncherReferenceId = base.ReferenceId;
			fireProjectileMessage.Origin = vector;
			fireProjectileMessage.Velocity = velocity;
			fireProjectileMessage.SendToServer();
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Activate && interactable.State == 1 && !GameManager.IsBatchMode)
		{
			_doEffectsCancellation.CancelAndInitialize();
			DoEffects(_doEffectsCancellation.Token).Forget();
		}
	}

	public void Fire(Vector3 origin, Vector3 velocity)
	{
		if (IsOperable && !SupressFire)
		{
			Quaternion rotation = Quaternion.LookRotation(velocity, Vector3.up);
			Chamber.Type = Slot.Class.Flare;
			Stackable stackable = Magazine.SplitStack(1, Chamber);
			OnServer.MoveToWorld(stackable, origin, rotation, velocity, Vector3.zero);
			Chamber.Type = Slot.Class.Blocked;
			(stackable as IProjectile)?.OnProjectileLaunched(origin, velocity);
			OnServer.Interact(base.InteractActivate, 1);
			_startCooldownCoolDownCancellation.CancelAndInitialize();
			StartCooldown(_startCooldownCoolDownCancellation.Token).Forget();
		}
	}

	private async UniTaskVoid StartCooldown(CancellationToken token)
	{
		_supressOnSlotEnter = true;
		await Task.Delay((int)(CooldownSeconds * 1000f), token);
		OnServer.Interact(base.InteractActivate, 0);
		_supressOnSlotEnter = false;
	}

	private async UniTaskVoid DoEffects(CancellationToken token)
	{
		MuzzleFlashTransform.Rotate(Vector3.left, 100f);
		MuzzleFlashTransform.gameObject.SetActive(value: true);
		await UniTask.Delay(40, ignoreTimeScale: false, PlayerLoopTiming.Update, token);
		MuzzleFlashTransform.gameObject.SetActive(value: false);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		_doEffectsCancellation.Cancel();
		_startCooldownCoolDownCancellation.Cancel();
	}
}
