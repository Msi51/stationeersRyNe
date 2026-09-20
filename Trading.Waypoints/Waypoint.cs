using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Effects;
using Objects.Electrical;
using UnityEngine;

namespace Trading.Waypoints;

public class Waypoint : Device, IWaypoint, ISmartRotatable, ISetable, ILogicable, IReferencable, IEvaluable
{
	[Header("Waypoint")]
	[SerializeField]
	private Transform _directionVisualiser;

	[SerializeField]
	private MaterialChanger[] _lightMaterialChangers;

	private long _targetWaypointReferenceId;

	private int _heightSetting = 3;

	private bool _visualiserOn;

	private float _visualiserOffTime = -1f;

	private IWaypoint _targetWaypoint;

	private static readonly int PoweredState = Animator.StringToHash("powered");

	private static readonly int UnPoweredState = Animator.StringToHash("unpowered");

	private UniTask VisualizerDisableTimer;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	private IWaypoint _nextWaypoint;

	private IWaypoint _previousWaypoint;

	private double _setting;

	public IWaypoint TargetWaypoint
	{
		get
		{
			return _targetWaypoint;
		}
		set
		{
			_targetWaypoint = value;
			ValidateWaypoint();
			UpdateVisualiser();
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 4096;
			}
		}
	}

	private int HeightSetting
	{
		get
		{
			return _heightSetting;
		}
		set
		{
			value = Mathf.Clamp(value, -100, 100);
			_heightSetting = value;
			UpdateVisualiser();
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
		}
	}

	private bool VisualiserOn
	{
		get
		{
			return _visualiserOn;
		}
		set
		{
			_visualiserOn = value;
			UpdateVisualiser();
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
		}
	}

	public Vector3 WaypointPosition => base.Position + Vector3.up * _heightSetting;

	public Vector3 WaypointForward => Transform.forward;

	public string WaypointName => DisplayName;

	public WaypointType WaypointType => WaypointType.Beacon;

	public IWaypoint NextWaypoint
	{
		get
		{
			return _nextWaypoint;
		}
		set
		{
			_nextWaypoint = value;
		}
	}

	public IWaypoint PreviousWaypoint
	{
		get
		{
			return _previousWaypoint;
		}
		set
		{
			_previousWaypoint = value;
		}
	}

	public double Setting
	{
		get
		{
			return HeightSetting;
		}
		set
		{
			HeightSetting = (int)value;
		}
	}

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		switch (interactable.Action)
		{
		case InteractableType.Button1:
		{
			Labeller labeller = interaction.SourceSlot.Occupant as Labeller;
			if ((bool)labeller)
			{
				delayedActionInstance.ActionMessage = ActionStrings.Set;
				delayedActionInstance.AppendStateMessage(GameStrings.DeviceManualHeightWindow);
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
					return delayedActionInstance.Succeed();
				}
				labeller.Set(this);
				return delayedActionInstance.Succeed();
			}
			if (!UsingScrewdriver(interaction))
			{
				return delayedActionInstance.Fail(GameStrings.RequiresScrewdriver);
			}
			delayedActionInstance.ActionMessage = GameStrings.CycleWaypointHeight.DisplayString;
			delayedActionInstance.AppendStateMessage(GameStrings.CurrentHeight, StringManager.Get(HeightSetting));
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			if (GameManager.RunSimulation)
			{
				HeightSetting += ((!interaction.AltKey) ? 1 : (-1));
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		}
		case InteractableType.Button2:
			if (!UsingScrewdriver(interaction))
			{
				return delayedActionInstance.Fail(GameStrings.RequiresScrewdriver);
			}
			delayedActionInstance.ActionMessage = GameStrings.CycleWaypoint.DisplayString;
			delayedActionInstance.AppendStateMessage(GameStrings.CurrentWaypoint, TargetWaypoint?.WaypointName ?? GameStrings.OperatorNone.DisplayString);
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			if (GameManager.RunSimulation)
			{
				IWaypoint targetWaypoint = WaypointManager.CycleWaypoint(this, TargetWaypoint);
				TargetWaypoint = targetWaypoint;
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button3:
			if (!UsingScrewdriver(interaction))
			{
				return delayedActionInstance.Fail(GameStrings.RequiresScrewdriver);
			}
			delayedActionInstance.ActionMessage = GameStrings.ToggleVisualiser.DisplayString;
			delayedActionInstance.AppendStateMessage(GameStrings.VisualiserState, _visualiserOn ? "On" : "Off");
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			if (GameManager.RunSimulation)
			{
				VisualiserOn = !VisualiserOn;
				_visualiserOffTime = GameManager.GameTime + 15f;
				if (VisualizerDisableTimer.Status != UniTaskStatus.Pending)
				{
					VisualizerDisableTimer = AsyncVisualiserDisable();
				}
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		default:
			return base.InteractWith(interactable, interaction, doAction);
		}
	}

	private bool UsingScrewdriver(Interaction interaction)
	{
		return interaction.SourceSlot.Occupant is Screwdriver;
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		InteractableType action = interactable.Action;
		if (action == InteractableType.OnOff || action == InteractableType.Powered)
		{
			ValidateWaypoint();
			UpdateVisualiser();
			int id = ((Powered && OnOff) ? PoweredState : UnPoweredState);
			MaterialChanger[] lightMaterialChangers = _lightMaterialChangers;
			for (int i = 0; i < lightMaterialChangers.Length; i++)
			{
				lightMaterialChangers[i].ChangeState(id);
			}
		}
	}

	public override void OnPowerTick()
	{
		CheckTargetExists();
		ValidateWaypoint();
		CheckErrorState();
		UpdateVisualiser();
	}

	private void UpdateVisualiser()
	{
		if (GameManager.IsThread)
		{
			RefreshVisualiserOnMainThread().Forget();
		}
		else
		{
			RefreshVisualiser();
		}
	}

	private void RefreshVisualiser()
	{
		if (!VisualiserOn || NextWaypoint == null || !OnOff || !Powered)
		{
			_directionVisualiser.gameObject.SetActive(value: false);
			return;
		}
		_directionVisualiser.gameObject.SetActive(value: true);
		Vector3 vector = WaypointPosition - NextWaypoint.WaypointPosition;
		Vector3 vector2 = Vector3.ProjectOnPlane(vector, Vector3.up);
		float x = Vector3.Angle(vector, vector2) * Mathf.Sign(0f - vector.y);
		_directionVisualiser.rotation = Quaternion.LookRotation(vector2, Vector3.up) * Quaternion.Euler(x, 0f, 0f);
		_directionVisualiser.localPosition = Vector3.up * HeightSetting;
	}

	private async UniTask AsyncVisualiserDisable()
	{
		await UniTask.WaitUntil(() => GameManager.GameTime >= _visualiserOffTime || _visualiserOffTime <= 0f);
		VisualiserOn = false;
	}

	private async UniTaskVoid RefreshVisualiserOnMainThread()
	{
		await UniTask.SwitchToMainThread();
		RefreshVisualiser();
	}

	private void CheckErrorState()
	{
		if (Error == 0 && NextWaypoint == null)
		{
			OnServer.Interact(base.InteractError, 1);
		}
		else if (Error == 1 && NextWaypoint != null)
		{
			OnServer.Interact(base.InteractError, 0);
		}
	}

	private void CheckTargetExists()
	{
		if (TargetWaypoint != null && !WaypointManager.AllWaypoints.Contains(TargetWaypoint))
		{
			TargetWaypoint = null;
		}
	}

	private void ValidateWaypoint()
	{
		if (NextWaypoint != null)
		{
			NextWaypoint.PreviousWaypoint = null;
			NextWaypoint = null;
		}
		if (!OnOff || !Powered || TargetWaypoint == null)
		{
			return;
		}
		if (TargetWaypoint is LandingPadCenter landingPadCenter)
		{
			if (!landingPadCenter.GetTaxiThresholdPiece(out var threshold))
			{
				threshold = landingPadCenter;
			}
			if (threshold.PreviousWaypoint == null)
			{
				NextWaypoint = threshold;
				threshold.PreviousWaypoint = this;
			}
		}
		else if (TargetWaypoint.PreviousWaypoint == null)
		{
			NextWaypoint = TargetWaypoint;
			NextWaypoint.PreviousWaypoint = this;
		}
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		WaypointManager.Register(this);
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		WaypointManager.Deregister(this);
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt32(HeightSetting);
		writer.WriteBoolean(VisualiserOn);
		writer.WriteInt64((TargetWaypoint as Thing)?.ReferenceId ?? 0);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		HeightSetting = reader.ReadInt32();
		VisualiserOn = reader.ReadBoolean();
		_targetWaypointReferenceId = reader.ReadInt64();
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			HeightSetting = reader.ReadInt32();
			VisualiserOn = reader.ReadBoolean();
		}
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			TargetWaypoint = Referencable.Find<Thing>(reader.ReadInt64()) as IWaypoint;
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteInt32(HeightSetting);
			writer.WriteBoolean(VisualiserOn);
		}
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			writer.WriteInt64((TargetWaypoint as Thing)?.ReferenceId ?? 0);
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new WaypointSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is WaypointSaveData waypointSaveData)
		{
			waypointSaveData.TargetWaypointReferenceId = (TargetWaypoint as Thing)?.ReferenceId ?? 0;
			waypointSaveData.WaypointHeight = HeightSetting;
		}
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is WaypointSaveData waypointSaveData)
		{
			_targetWaypointReferenceId = waypointSaveData.TargetWaypointReferenceId;
			HeightSetting = waypointSaveData.WaypointHeight;
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		TargetWaypoint = Referencable.Find<Thing>(_targetWaypointReferenceId) as IWaypoint;
	}

	public bool IsLinkedToCenter()
	{
		if (NextWaypoint is LandingPadCenter)
		{
			return true;
		}
		if (NextWaypoint == null)
		{
			return false;
		}
		return NextWaypoint.IsLinkedToCenter();
	}

	public void OnDeregister()
	{
		if (NextWaypoint != null)
		{
			NextWaypoint.PreviousWaypoint = null;
		}
		if (PreviousWaypoint != null)
		{
			PreviousWaypoint.NextWaypoint = null;
			if (PreviousWaypoint is Waypoint waypoint)
			{
				waypoint.TargetWaypoint = null;
			}
		}
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
}
