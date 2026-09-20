using System;
using System.Collections;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Structures;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Items;

public class Jetpack : Backpack
{
	public enum Emission
	{
		None = 0,
		DownStabilize = 1,
		Forward = 2,
		Backward = 4,
		Left = 8,
		Right = 0x10,
		Up = 0x20,
		Down = 0x40,
		Stabilize = 0x80,
		ClearAll = 0x100
	}

	private const float MOLE_USE_SCALE = 6f;

	[Header("Jetpack")]
	[SerializeField]
	[FormerlySerializedAs("MolesPerEmission")]
	private float molesPerEmission = 0.001f;

	public ushort MaximumHeight = 10;

	public GameBase JetUp;

	public GameBase JetDown;

	public GameBase JetLeft;

	public GameBase JetRight;

	public GameBase JetBack;

	public GameBase JetForward;

	public GameBase JetStabilize;

	public GameBase JetDownStabilize;

	public GameBase[] AudioObjects;

	private int _jetUpStarthash = Animator.StringToHash("JetUpStart");

	private int _jetUpLoophash = Animator.StringToHash("JetUpLoop");

	private int _jetUpOffhash = Animator.StringToHash("JetUpOff");

	private int _jetDownStarthash = Animator.StringToHash("JetDownStart");

	private int _jetDownLoophash = Animator.StringToHash("JetDownLoop");

	private int _jetDownOffhash = Animator.StringToHash("JetDownOff");

	private int _jetLeftStarthash = Animator.StringToHash("JetLeftStart");

	private int _jetLeftLoophash = Animator.StringToHash("JetLeftLoop");

	private int _jetLeftOffhash = Animator.StringToHash("JetLeftOff");

	private int _jetRightStarthash = Animator.StringToHash("JetRightStart");

	private int _jetRightLoophash = Animator.StringToHash("JetRightLoop");

	private int _jetRightOffhash = Animator.StringToHash("JetRightOff");

	private int _jetBackStarthash = Animator.StringToHash("JetBackStart");

	private int _jetBackLoophash = Animator.StringToHash("JetBackLoop");

	private int _jetBackOffhash = Animator.StringToHash("JetBackOff");

	private int _jetForwardStarthash = Animator.StringToHash("JetForwardStart");

	private int _jetForwardLoophash = Animator.StringToHash("JetForwardLoop");

	private int _jetForwardOffhash = Animator.StringToHash("JetForwardOff");

	private int _mainJetsLoopHash = Animator.StringToHash("MainJetsLoop");

	private int _mainJetsStartHash = Animator.StringToHash("MainJetsStart");

	public float JetPackPower;

	public float JetPackSpeed = 10f;

	private bool _jetPackActivate;

	public Slot PropellentSlot;

	private float _outputSetting = 0.5f;

	public static float MinSetting = 0.1f;

	public static float MaxSetting = 2f;

	public static readonly PressurekPa LowPropellantDeltaLevel = new PressurekPa(500.0);

	public static readonly PressurekPa CriticalPropellantDeltaLevel = new PressurekPa(100.0);

	public int TotalActive;

	private static readonly int PropellentHash = Animator.StringToHash("Propellent");

	protected bool noAtmos;

	protected int _currentEmission;

	public int LastFrameEmissions;

	protected bool _hasUpdated;

	protected int _jetpackHash = Animator.StringToHash("JetPack");

	private static readonly int JetPackThrustUpHash = Animator.StringToHash("JetPackThrustUp");

	private static readonly int JetPackThrustDownHash = Animator.StringToHash("JetPackThrustDown");

	private float _heightEfficiency;

	public static readonly float TerrainDetectionRadius = 2f;

	private static Collider[] _overlapColliders = new Collider[8];

	protected bool PropulsionActive => CurrentEmission >= 1;

	protected bool IsStabilizing => CurrentEmission == 1;

	protected bool IsThrusting => CurrentEmission > 1;

	protected bool StabilizerEnabled => OnOff;

	public MoleQuantity MolesPerEmission => new MoleQuantity(molesPerEmission);

	public MoleQuantity MolesToUse => MolesPerEmission * 6.0 * (float)DifficultySetting.Current.JetpackRate;

	public bool JetPackActivate
	{
		get
		{
			return _jetPackActivate;
		}
		set
		{
			if (value && !_jetPackActivate && base.ParentHuman != null)
			{
				Thing.Interact(base.InteractActivate, 1);
			}
			else if (!value && _jetPackActivate)
			{
				Thing.Interact(base.InteractActivate, 0);
			}
			_jetPackActivate = value;
		}
	}

	[ByteArraySync]
	public float OutputSetting
	{
		get
		{
			return _outputSetting;
		}
		set
		{
			if (!RocketMath.Approximately(value, _outputSetting))
			{
				_outputSetting = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 256;
				}
			}
		}
	}

	public virtual bool IsGasPowered => true;

	public virtual bool PropellantLow
	{
		get
		{
			if (!PropellentSlot.Contains<GasCanister>(out var occupant))
			{
				return true;
			}
			if (occupant.InternalAtmosphere != null)
			{
				PressurekPa pressureGassesAndLiquids = occupant.InternalAtmosphere.PressureGassesAndLiquids;
				PressurekPa? obj = base.WorldAtmosphere?.PressureGassesAndLiquids;
				return pressureGassesAndLiquids - obj < LowPropellantDeltaLevel;
			}
			return false;
		}
	}

	public virtual bool PropellantCritical
	{
		get
		{
			if (!PropellentSlot.Contains<GasCanister>(out var occupant))
			{
				return true;
			}
			if (occupant.InternalAtmosphere != null)
			{
				PressurekPa pressureGassesAndLiquids = occupant.InternalAtmosphere.PressureGassesAndLiquids;
				PressurekPa? obj = base.WorldAtmosphere?.PressureGassesAndLiquids;
				return pressureGassesAndLiquids - obj < CriticalPropellantDeltaLevel;
			}
			return false;
		}
	}

	public virtual PressurekPa PropellantDelta
	{
		get
		{
			if (!PropellentSlot.Contains<GasCanister>(out var occupant))
			{
				return PressurekPa.Zero;
			}
			return occupant.InternalAtmosphere.PressureGassesAndLiquids - (base.WorldAtmosphere?.PressureGasses ?? PressurekPa.Zero);
		}
	}

	public virtual bool IsBatteryPowered => false;

	public virtual bool PowerLow => false;

	public virtual bool PowerCritical => false;

	public virtual bool HasPropellent
	{
		get
		{
			if (!PropellentSlot.Contains<GasCanister>(out var occupant))
			{
				return false;
			}
			if (occupant.InternalAtmosphere == null)
			{
				return false;
			}
			MoleQuantity moleQuantity = MoleQuantity.Zero;
			if (base.WorldAtmosphere != null)
			{
				moleQuantity = new MoleQuantity(base.WorldAtmosphere.TotalMoles.ToDouble() * occupant.InternalAtmosphere.Volume.ToDouble() / base.WorldAtmosphere.Volume.ToDouble());
			}
			Atmosphere internalAtmosphere = occupant.InternalAtmosphere;
			return ((internalAtmosphere != null) ? new MoleQuantity?(internalAtmosphere.GasMixture.GetTotalMolesGassesAndLiquids - moleQuantity) : ((MoleQuantity?)null)) >= MolesToUse * OutputSetting;
		}
	}

	public int CurrentEmission
	{
		get
		{
			return _currentEmission;
		}
		set
		{
			int num = ((value != 256) ? value : 0);
			if (_currentEmission != num)
			{
				_currentEmission = num;
				if (base.ParentHuman != null && base.ParentHuman.HasAuthority)
				{
					OnServer.UseJetpack(base.ReferenceId, _currentEmission);
				}
				_hasUpdated = true;
			}
		}
	}

	public bool AnyEmissions => CurrentEmission > 1;

	public bool StabilizeEmission => IsEmission(Emission.DownStabilize);

	public override void OnExitInventory(Thing parent)
	{
		base.OnExitInventory(parent);
		ClearEmissions();
	}

	public void UpdateEmissionAudio(Emission type, bool soundOn)
	{
		if (base.ParentHuman == null)
		{
			if (type <= Emission.Up)
			{
				switch (type)
				{
				case Emission.Forward:
					StopSound(_jetForwardLoophash);
					break;
				case Emission.Backward:
					StopSound(_jetBackLoophash);
					break;
				case Emission.Left:
					StopSound(_jetLeftLoophash);
					break;
				case Emission.Right:
					StopSound(_jetRightLoophash);
					break;
				case Emission.DownStabilize:
					StopSound(_mainJetsLoopHash);
					break;
				case Emission.Up:
					StopSound(_jetUpLoophash);
					break;
				}
			}
			else
			{
				switch (type)
				{
				default:
					_ = 256;
					break;
				case Emission.Down:
					StopSound(_jetDownLoophash);
					break;
				case Emission.Stabilize:
					break;
				}
			}
			return;
		}
		switch (type)
		{
		case Emission.DownStabilize:
			if (soundOn)
			{
				PlaySound(_mainJetsLoopHash);
			}
			else
			{
				StopSound(_mainJetsLoopHash);
			}
			break;
		case Emission.Forward:
			if (soundOn)
			{
				if (base.ParentHuman.VelocityMagnitude < JetPackSpeed)
				{
					PlaySound(_jetForwardStarthash);
				}
				PlaySound(_jetForwardLoophash);
			}
			else
			{
				if (base.ParentHuman.VelocityMagnitude < JetPackSpeed)
				{
					PlaySound(_jetForwardOffhash);
				}
				StopSound(_jetForwardLoophash);
			}
			break;
		case Emission.Backward:
			if (soundOn)
			{
				if (base.ParentHuman.VelocityMagnitude < JetPackSpeed)
				{
					PlaySound(_jetBackStarthash);
				}
				PlaySound(_jetBackLoophash);
			}
			else
			{
				if (base.ParentHuman.VelocityMagnitude < JetPackSpeed)
				{
					PlaySound(_jetBackOffhash);
				}
				StopSound(_jetBackLoophash);
			}
			break;
		case Emission.Left:
			if (soundOn)
			{
				if (base.ParentHuman.VelocityMagnitude < JetPackSpeed)
				{
					PlaySound(_jetLeftStarthash);
				}
				PlaySound(_jetLeftLoophash);
			}
			else
			{
				if (base.ParentHuman.VelocityMagnitude < JetPackSpeed)
				{
					PlaySound(_jetLeftOffhash);
				}
				StopSound(_jetLeftLoophash);
			}
			break;
		case Emission.Right:
			if (soundOn)
			{
				if (base.ParentHuman.VelocityMagnitude < JetPackSpeed)
				{
					PlaySound(_jetRightStarthash);
				}
				PlaySound(_jetRightLoophash);
			}
			else
			{
				if (base.ParentHuman.VelocityMagnitude < JetPackSpeed)
				{
					PlaySound(_jetRightOffhash);
				}
				StopSound(_jetRightLoophash);
			}
			break;
		case Emission.Up:
			if (soundOn)
			{
				if (base.ParentHuman.VelocityMagnitude < JetPackSpeed)
				{
					PlaySound(_jetUpStarthash);
				}
				PlaySound(_jetUpLoophash);
			}
			else
			{
				if (base.ParentHuman.VelocityMagnitude < JetPackSpeed)
				{
					PlaySound(_jetUpOffhash);
				}
				StopSound(_jetUpLoophash);
			}
			break;
		case Emission.Down:
			if (soundOn)
			{
				if (base.ParentHuman.VelocityMagnitude < JetPackSpeed)
				{
					PlaySound(_jetDownStarthash);
				}
				PlaySound(_jetDownLoophash);
			}
			else
			{
				if (base.ParentHuman.VelocityMagnitude < JetPackSpeed)
				{
					PlaySound(_jetDownOffhash);
				}
				StopSound(_jetDownLoophash);
			}
			break;
		case Emission.ClearAll:
			StopSound(_jetBackLoophash);
			StopSound(_jetLeftLoophash);
			StopSound(_jetForwardLoophash);
			StopSound(_jetRightLoophash);
			StopSound(_jetUpLoophash);
			StopSound(_jetDownLoophash);
			StopSound(_mainJetsLoopHash);
			break;
		}
	}

	public override StringBuilder GetSlotTooltip()
	{
		StringBuilder slotTooltip = base.GetSlotTooltip();
		if (base.ParentSlot?.Parent is SuitStorage && PropellentSlot.Contains<GasCanister>(out var occupant))
		{
			slotTooltip.AppendLine(GameStrings.ContainsItemInSlotAtQuantity.AsString(occupant.ToTooltip(), PropellentSlot.ToTooltip(), occupant.GetQuantityText().AsColor("yellow")));
		}
		return slotTooltip;
	}

	public override void Awake()
	{
		base.Awake();
		if (!noAtmos)
		{
			PropellentSlot = Slots.Find((Slot slot) => slot.StringHash == PropellentHash);
			AtmosphericsManager.Instance.Register(this);
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (!noAtmos)
		{
			AtmosphericsManager.Instance?.Deregister(this);
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new JetpackSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is JetpackSaveData jetpackSaveData)
		{
			OutputSetting = jetpackSaveData.OutputSetting;
		}
		StartCoroutine(WaitThenClear());
	}

	private IEnumerator WaitThenClear()
	{
		if (!GameManager.IsTutorial)
		{
			while (GameManager.GameState != GameState.Running)
			{
				yield return Yielders.EndOfFrame;
			}
			if (!base.ParentHuman || !base.ParentHuman.HasAuthority)
			{
				ClearEmissions();
				OnServer.Interact(base.InteractImport, 0);
			}
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		JetpackSaveData jetpackSaveData = savedData as JetpackSaveData;
		if (GameManager.GameState != GameState.None && jetpackSaveData != null)
		{
			jetpackSaveData.OutputSetting = OutputSetting;
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteSingle(OutputSetting);
		}
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteInt32(CurrentEmission);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			float outputSetting = reader.ReadSingle();
			if ((bool)base.ParentHuman && base.ParentHuman.HasAuthority)
			{
				OutputSetting = outputSetting;
			}
		}
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			int currentEmission = reader.ReadInt32();
			if ((bool)base.ParentHuman && !base.ParentHuman.HasAuthority)
			{
				CurrentEmission = currentEmission;
			}
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(OutputSetting);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		OutputSetting = reader.ReadSingle();
	}

	public override void OnStopRender()
	{
		base.OnStopRender();
		DisableJetAll();
	}

	public virtual void DisableJetAll()
	{
		JetForward.SetVisible(isVisble: false);
		JetBack.SetVisible(isVisble: false);
		JetLeft.SetVisible(isVisble: false);
		JetRight.SetVisible(isVisble: false);
		JetUp.SetVisible(isVisble: false);
		JetDown.SetVisible(isVisble: false);
		JetStabilize.SetVisible(isVisble: false);
		JetDownStabilize.SetVisible(isVisble: false);
		JetPackActivate = false;
		CurrentEmission = 0;
		StopAllAudio();
	}

	public void LateUpdate()
	{
		UpdateJetpackEmissions();
	}

	public void ClearEmissions()
	{
		CurrentEmission = 256;
		JetPackActivate = false;
	}

	public override void OnAtmosphericTick()
	{
		Achievements.AchieveJetpackCritical(this);
		if (!HasPropellent || !GameManager.RunSimulation || CurrentEmission <= 0)
		{
			return;
		}
		if (base.ParentHuman != null && base.ParentHuman.MovementController != null)
		{
			MovementController.Mode controlMode = base.ParentHuman.MovementController.ControlMode;
			if (controlMode == MovementController.Mode.Seated || controlMode == MovementController.Mode.LyingDown || controlMode == MovementController.Mode.CryoTube)
			{
				return;
			}
		}
		if (base.WorldAtmosphere == null)
		{
			Atmosphere atmosphere = (base.WorldAtmosphere = base.GridController.AtmosphericsController.CloneGlobalAtmosphere(base.ParentSlot.Parent.WorldGrid, 0L));
		}
		base.WorldAtmosphere.Add(PropellentSlot.Get<GasCanister>().InternalAtmosphere.Remove(MolesToUse * OutputSetting, AtmosphereHelper.MatterState.Gas));
	}

	public virtual void UpdateJetpackEmissions()
	{
		if (LastFrameEmissions != CurrentEmission && _hasUpdated)
		{
			if (!IsOccluded)
			{
				JetForward.SetVisible(IsEmission(Emission.Forward));
				JetBack.SetVisible(IsEmission(Emission.Backward));
				JetLeft.SetVisible(IsEmission(Emission.Left));
				JetRight.SetVisible(IsEmission(Emission.Right));
				JetUp.SetVisible(IsEmission(Emission.Up));
				JetDown.SetVisible(IsEmission(Emission.Down));
				JetStabilize.SetVisible(IsEmission(Emission.Stabilize));
				JetDownStabilize.SetVisible(IsEmission(Emission.DownStabilize));
			}
			LastFrameEmissions = CurrentEmission;
		}
	}

	protected bool IsCurrentlyEmitting(Emission type)
	{
		return ((uint)CurrentEmission & (uint)type) != 0;
	}

	public bool IsEmission(Emission type)
	{
		bool flag = IsCurrentlyEmitting(type);
		UpdateEmissionAudio(type, flag);
		return flag;
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action == InteractableType.OnOff)
		{
			return GameStrings.JetpackStabilizerState.AsString(OnOff ? ActionStrings.Off : ActionStrings.On);
		}
		return base.GetContextualName(interactable);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		float outputSetting = OutputSetting;
		switch (interactable.Action)
		{
		case InteractableType.Button1:
			if (OutputSetting < MaxSetting)
			{
				outputSetting += 0.1f;
				if (!doAction)
				{
					return delayedActionInstance.Succeed();
				}
				if (base.ParentHuman != null && base.ParentHuman.IsLocalPlayer)
				{
					UIAudioManager.Play(JetPackThrustUpHash);
				}
				if (GameManager.RunSimulation)
				{
					OutputSetting = Mathf.Min(outputSetting, MaxSetting);
				}
				return delayedActionInstance.Succeed();
			}
			return delayedActionInstance.Fail(GameStrings.GlobalAlreadyMax);
		case InteractableType.Button2:
			if (OutputSetting > MinSetting)
			{
				outputSetting -= 0.1f;
				if (!doAction)
				{
					return delayedActionInstance.Succeed();
				}
				if (base.ParentHuman != null && base.ParentHuman.IsLocalPlayer)
				{
					UIAudioManager.Play(JetPackThrustDownHash);
				}
				if (GameManager.RunSimulation)
				{
					OutputSetting = Mathf.Max(outputSetting, MinSetting);
				}
				return delayedActionInstance.Succeed();
			}
			return delayedActionInstance.Fail(GameStrings.GlobalAlreadyMin);
		default:
			return base.InteractWith(interactable, interaction, doAction);
		}
	}

	public override void DeserializModData(ThingModData modData)
	{
		base.DeserializModData(modData);
		if (modData is JetPackModData jetPackModData && !float.IsNaN(jetPackModData.MaxSpeed))
		{
			JetPackSpeed = (int)jetPackModData.MaxSpeed;
		}
	}

	public float GetHeightEfficiency()
	{
		return _heightEfficiency;
	}

	public void CalculateHeightEfficiency()
	{
		if ((object)base.ParentHuman == null)
		{
			return;
		}
		bool flag = WorldManager.HasGravityAtHeight(Transform.position.y);
		if ((WorldSetting.Current?.StartConditionData != null && WorldSetting.Current.StartConditionData.IsTerrainEdit) || !flag || base.ParentHuman.Room != null)
		{
			_heightEfficiency = 1f;
			return;
		}
		Vector3 vector = Vector3.up * (TerrainDetectionRadius + 0.5f);
		float num = (float)(int)MaximumHeight + Math.Max(base.ParentHuman.YHeight, 0f);
		if (flag && base.ParentHuman.Room == null)
		{
			Ray ray = new Ray(base.ParentHuman.CenterPosition + vector, Vector3.down);
			int layerMask = (1 << (int)Layers.Default) | (1 << (int)Layers.Terrain);
			RaycastHit hitInfo;
			bool flag2 = Physics.SphereCast(ray, TerrainDetectionRadius, out hitInfo, num * 2f, layerMask);
			int num2 = Physics.OverlapSphereNonAlloc(base.ParentHuman.CenterPosition + vector, TerrainDetectionRadius, _overlapColliders, layerMask);
			float num3 = WorldSetting.Current?.GetLavaHeight(base.ParentHuman.CenterPosition) ?? 0f;
			float b = base.ParentHuman.CenterPosition.y - num3;
			float a = float.MaxValue;
			if (flag2)
			{
				a = hitInfo.distance;
			}
			if (num2 > 0)
			{
				a = 0f;
			}
			a = Mathf.Min(a, b);
			float heightEfficiency = Mathf.Clamp01(Mathf.Lerp(1f, 0f, (a - num) / num));
			_heightEfficiency = heightEfficiency;
		}
	}
}
