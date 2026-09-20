using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Objects.Electrical;
using UnityEngine;
using Util;

namespace Assets.Scripts.Objects.Items;

public class FireExtinguisher : ItemRenamable, ISpatial, IPhysical, IProfile, IDensePoolable
{
	[SerializeField]
	private Transform particleEmitPosition;

	private const int NUMBER_OF_PARTICLES = 3;

	public CancellationTokenWrapper ParticleTask = new CancellationTokenWrapper();

	public CancellationTokenWrapper ActiveTask = new CancellationTokenWrapper();

	private const float LITERS_PER_SECOND = 0.25f;

	private static readonly VolumeLitres LitresPerSecond = new VolumeLitres(0.25);

	private const float PRESSURE_PER_SECOND = 200f;

	private static readonly PressurekPa PressurePerSecond = new PressurekPa(200.0);

	public static MoleEnergy ExtraEnergyToRemove = new MoleEnergy(20000.0);

	public int SupressTicks = 10;

	public Transform ParticleEmitPosition => particleEmitPosition;

	public bool IsSuppressingFire { get; private set; }

	private GasCanister Canister => Slots[0].Get<GasCanister>();

	public override bool UseDefaultUiUsingSounds()
	{
		return false;
	}

	public override void OnUsePrimary(Vector3 targetLocation, Quaternion targetRotation, ulong steamId, bool authoringMode)
	{
		base.OnUsePrimary(targetLocation, targetRotation, steamId, authoringMode);
		if (!ActiveTask.Initialized)
		{
			ActiveTask.Initialize();
			UseExtinguisher(ActiveTask.Token).Forget();
		}
	}

	private async UniTaskVoid UseExtinguisher(CancellationToken cancellationToken)
	{
		while (KeyManager.GetMouse("Primary") && !KeyManager.GetButton(KeyMap.SwapHands) && GameManager.GameState == GameState.Running && !base.BeingDestroyed)
		{
			bool flag = Canister != null && (Canister.InternalAtmosphere.TotalMolesLiquids > MoleQuantity.One || Canister.InternalAtmosphere.PressureGasses > base.WorldAtmosphere.PressureGasses + PressurePerSecond * GameManager.GameTickSpeedSeconds);
			if (flag && Activate == 0)
			{
				Thing.Interact(base.InteractActivate, 1);
			}
			else if (!flag && Activate == 1)
			{
				Thing.Interact(base.InteractActivate, 0);
			}
			await UniTask.NextFrame(cancellationToken);
		}
		Thing.Interact(base.InteractActivate, 0);
		ActiveTask.Cancel();
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		Atmosphere atmosphere = base.GridController.AtmosphericsController.CloneGlobalAtmosphere(new WorldGrid(RootParent.Position), 0L);
		if (Activate == 1 && Canister != null)
		{
			IsSuppressingFire = InternalsCanSuppressFire(atmosphere);
			AtmosphereHelper.MoveLiquidVolume(Canister.InternalAtmosphere, atmosphere, LitresPerSecond * GameManager.GameTickSpeedSeconds);
			AtmosphereHelper.MoveToEqualize(Canister.InternalAtmosphere, atmosphere, PressurePerSecond * GameManager.GameTickSpeedSeconds, AtmosphereHelper.MatterState.Gas);
			if (IsSuppressingFire)
			{
				atmosphere.Extinguish(SupressTicks, ExtraEnergyToRemove);
			}
		}
		else
		{
			IsSuppressingFire = false;
		}
	}

	private bool InternalsCanSuppressFire(Atmosphere worldAtmosphere)
	{
		PressurekPa pressurekPa = worldAtmosphere?.PressureGasses ?? PressurekPa.Zero;
		if ((object)Canister == null || (Canister.InternalAtmosphere.TotalMolesLiquids < MoleQuantity.One && Canister.InternalAtmosphere.PressureGasses < pressurekPa + PressurePerSecond * GameManager.GameTickSpeedSeconds))
		{
			return false;
		}
		return Canister.InternalAtmosphere.TotalInertMoles > Canister.InternalAtmosphere.TotalMoles * 0.9900000095367432;
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action != InteractableType.Activate || GameManager.IsBatchMode)
		{
			return;
		}
		switch (Activate)
		{
		case 1:
			if (!ParticleTask.Initialized)
			{
				ParticleTask.Initialize();
				EmitParticles(ParticleTask.Token).Forget();
			}
			break;
		case 0:
			if (ParticleTask.Initialized)
			{
				ParticleTask.Cancel();
			}
			break;
		}
	}

	private async UniTaskVoid EmitParticles(CancellationToken cancellationToken)
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread(cancellationToken);
		}
		while (GameManager.GameState == GameState.Running && !ParticleTask.Token.IsCancellationRequested && !base.BeingDestroyed)
		{
			if (ShouldEmitParticles())
			{
				AtmosphericsManager.Instance.ExtinguisherParticleSystem.EmitExtinguisherParticles(ParticleEmitPosition.position, ParticleEmitPosition.rotation, 3);
			}
			await UniTask.NextFrame(PlayerLoopTiming.FixedUpdate, cancellationToken);
		}
	}

	private bool ShouldEmitParticles()
	{
		if (!Canister || !(Canister.InternalAtmosphere.TotalMolesLiquids > MoleQuantity.One))
		{
			if (base.WorldAtmosphere != null)
			{
				return Canister.InternalAtmosphere.PressureGasses > base.WorldAtmosphere.PressureGasses;
			}
			return false;
		}
		return true;
	}

	public override void OnAssignedReference()
	{
		base.OnAssignedReference();
		AtmosphericsManager.Instance.Register(this);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		ActiveTask.Cancel();
		ParticleTask.Cancel();
		if (GameManager.GameState != GameState.None)
		{
			AtmosphericsManager.Instance.Deregister(this);
		}
	}

	public override void OnDisplayInPlayerWindow()
	{
		base.OnDisplayInPlayerWindow();
		base.ParentSlot.RefreshQuantity();
	}

	public override string GetQuantityText()
	{
		if (Canister == null)
		{
			return string.Empty;
		}
		return Canister.GetQuantityText();
	}
}
