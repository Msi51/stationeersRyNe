using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Objects.Electrical;
using UnityEngine;
using Util;

namespace Assets.Scripts.Objects.Items;

public class LiquidVacuum : PowerTool, ISpatial, IPhysical, IProfile, IDensePoolable
{
	private CancellationTokenWrapper _useToolCancellation = new CancellationTokenWrapper();

	private const double INTERNAL_VOLUME = 500.0;

	[SerializeField]
	private ModeDialAnimComponent modeDial;

	[SerializeField]
	protected Transform fan;

	private const float DEGREES_ROTATION_PER_SECOND = 30f;

	private float _degreesRotationPerSecond;

	private const float SPIN_UP_TIME = 2f;

	private const double MAX_VOLUME_PER_TICK = 5.0;

	private const float IS_FULL_RATIO = 0.99f;

	private static VolumeLitres _maxVolumePerTick = new VolumeLitres(5.0);

	public override int EquipSoundHash => Defines.Sounds.EquipHeavyMiningTool;

	public override int UnEquipSoundHash => Defines.Sounds.UnEquipHeavyMiningTool;

	public override string[] ModeStrings => ActiveVent.VentDirectionStrings;

	public override bool PreventStateChange => true;

	public override bool IsOperable
	{
		get
		{
			bool flag = (VentDirection)Mode switch
			{
				VentDirection.Inward => base.InternalAtmosphere.LiquidVolumeRatio < 0.99f, 
				VentDirection.Outward => base.InternalAtmosphere.TotalVolumeLiquids > VolumeLitres.Zero, 
				_ => false, 
			};
			bool flag2 = base.IsOperable && flag;
			if (Error == 0 && !flag2)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			if (Error == 1 && flag2)
			{
				OnServer.Interact(base.InteractError, 0);
			}
			return flag2;
		}
	}

	public override void InitInternalAtmosphere()
	{
		base.InitInternalAtmosphere();
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, new VolumeLitres(500.0), 0L);
		}
	}

	public override void OnAssignedReference()
	{
		base.OnAssignedReference();
		AtmosphericsManager.Instance.Register(this);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (GameManager.GameState != GameState.None)
		{
			AtmosphericsManager.Instance.Deregister(this);
		}
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		if ((object)fan != null && !IsOccluded)
		{
			if (OnOff && Powered && Activate == 1 && Error == 0)
			{
				_degreesRotationPerSecond += 30f * Time.deltaTime * 2f;
			}
			else
			{
				_degreesRotationPerSecond -= 30f * Time.deltaTime * 2f;
			}
			_degreesRotationPerSecond = Mathf.Clamp(_degreesRotationPerSecond, 0f, 30f);
			fan.Rotate(Vector3.right, (Mode == 1) ? (0f - _degreesRotationPerSecond) : (_degreesRotationPerSecond * Time.deltaTime));
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (modeDial != null)
		{
			modeDial.RefreshState();
		}
	}

	public override void OnUsePrimary(Vector3 targetLocation, Quaternion targetRotation, ulong steamId, bool authoringMode)
	{
		base.OnUsePrimary(targetLocation, targetRotation, steamId, authoringMode);
		if (!_useToolCancellation.Initialized && OnOff && Powered && IsOperable)
		{
			_useToolCancellation.Initialize();
			UseVacuum(_useToolCancellation.Token).Forget();
		}
	}

	private async UniTaskVoid UseVacuum(CancellationToken token)
	{
		Thing.Interact(base.InteractActivate, 1);
		while (KeyManager.GetMouse("Primary") && !KeyManager.GetButton(KeyMap.SwapHands) && OnOff && Powered && Error == 0)
		{
			await UniTask.NextFrame(token);
		}
		Thing.Interact(base.InteractActivate, 0);
		_useToolCancellation.Cancel();
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Mode)
		{
			switch ((VentDirection)Mode)
			{
			case VentDirection.Outward:
				if (!doAction || !GameManager.RunSimulation)
				{
					return DelayedActionInstance.Success("Set");
				}
				OnServer.Interact(base.InteractMode, 1);
				break;
			case VentDirection.Inward:
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

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (IsOperable && OnOff && Powered && Activate != 0)
		{
			Atmosphere atmosphere = AtmosphericsController.World.CloneGlobalAtmosphere(new WorldGrid(RootParent.Position), 0L);
			switch ((VentDirection)Mode)
			{
			case VentDirection.Inward:
				AtmosphereHelper.DrainLiquids(atmosphere, base.InternalAtmosphere, _maxVolumePerTick * atmosphere.LiquidWorldVolumeScale);
				break;
			case VentDirection.Outward:
				AtmosphereHelper.DrainLiquids(base.InternalAtmosphere, atmosphere, _maxVolumePerTick);
				break;
			}
		}
	}
}
