using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class PowerTransmitter : WirelessPower
{
	private float _linkedReceiverDistance;

	private static readonly float _MaxTransmitterDistance = 500f;

	public static float MaxPowerTransmission = 5000f;

	public AnimationCurve PowerLossOverDistance = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f), new Keyframe(2f, 1f));

	private long _savedRecieverId;

	private PowerReceiver _linkedReceiver;

	private int _tryTargetCount;

	private static readonly int ReTargetWait = 10;

	private float _powerProvided;

	private long _savedNetworkId;

	public WirelessNetwork WirelessOutputNetwork => OutputNetwork as WirelessNetwork;

	public PowerReceiver LinkedReceiver
	{
		get
		{
			return _linkedReceiver;
		}
		set
		{
			if (value != _linkedReceiver)
			{
				if (_linkedReceiver != null)
				{
					WirelessOutputNetwork.RemoveDevice(_linkedReceiver);
				}
				if (value != null)
				{
					WirelessOutputNetwork.AddDevice(value);
				}
				if (GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractMode, (value != null) ? 1 : 0);
				}
				_linkedReceiver = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= NetworkUpdateType.Thing.WirelessPower.Receiver;
				}
				CheckError();
			}
		}
	}

	public override void SetVisualizerIntensity(float intensity)
	{
		base.SetVisualizerIntensity(intensity);
		if ((object)LinkedReceiver != null)
		{
			LinkedReceiver.VisualizerIntensity = intensity;
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(NetworkUpdateType.Thing.WirelessPower.Receiver, networkUpdateType))
		{
			writer.WriteInt64(LinkedReceiver?.ReferenceId ?? 0);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(NetworkUpdateType.Thing.WirelessPower.Receiver, networkUpdateType))
		{
			LinkedReceiver = Thing.Find<PowerReceiver>(reader.ReadInt64());
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt64(OutputNetwork.ReferenceId);
		writer.WriteInt64(LinkedReceiver?.ReferenceId ?? 0);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		OutputNetwork = Referencable.Find<WirelessNetwork>(reader.ReadInt64());
		OutputNetwork.AddDevice(this);
		_savedRecieverId = reader.ReadInt64();
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.OnOff)
		{
			if (OnOff)
			{
				TryContactReceiver();
			}
			else
			{
				base.VisualizerIntensity = 0f;
			}
		}
		if (interactable.Action == InteractableType.Powered && !Powered)
		{
			base.VisualizerIntensity = 0f;
		}
	}

	public override void OnDestroy()
	{
		if ((bool)LinkedReceiver)
		{
			LinkedReceiver.LinkedPowerTransmitter = null;
		}
		base.OnDestroy();
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		if (!(LinkedReceiver == null))
		{
			return;
		}
		base.VisualizerIntensity = 0f;
		if (OnOff)
		{
			_tryTargetCount++;
			if (_tryTargetCount > ReTargetWait)
			{
				_tryTargetCount = 0;
				WaitTryContactReceiverMainThread().Forget();
			}
		}
	}

	public override bool AllowSetPower(CableNetwork cableNetwork)
	{
		if (InputNetwork == cableNetwork)
		{
			return true;
		}
		return false;
	}

	public override void UsePower(CableNetwork cableNetwork, float powerUsed)
	{
		if (Error != 1 && OnOff && cableNetwork == WirelessOutputNetwork)
		{
			_powerProvided += powerUsed;
		}
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		if (InputNetwork == null || cableNetwork == InputNetwork)
		{
			if (!OnOff || InputNetwork == null)
			{
				base.VisualizerIntensity = 0f;
				return;
			}
			base.VisualizerIntensity = RocketMath.MapToScale(0f, MaxPowerTransmission, 0f, 1f, powerAdded);
			_powerProvided -= powerAdded;
		}
	}

	public override float GetUsedPower([NotNull] CableNetwork cableNetwork)
	{
		if (InputNetwork == null)
		{
			base.VisualizerIntensity = 0f;
		}
		if (InputNetwork == null || cableNetwork != InputNetwork)
		{
			return 0f;
		}
		if (Error == 1)
		{
			base.VisualizerIntensity = 0f;
			if (!OnOff)
			{
				return 0f;
			}
			return UsedPower;
		}
		if (!OnOff)
		{
			return 0f;
		}
		return Mathf.Min(MaxPowerTransmission, _powerProvided);
	}

	public override float GetGeneratedPower(CableNetwork cableNetwork)
	{
		if (OutputNetwork == null || Error == 1 || cableNetwork != OutputNetwork)
		{
			return 0f;
		}
		float num = PowerLossOverDistance.Evaluate(Mathf.Clamp01(_linkedReceiverDistance / _MaxTransmitterDistance)) * MaxPowerTransmission;
		if (!OnOff || InputNetwork == null)
		{
			return 0f;
		}
		return Mathf.Min(MaxPowerTransmission, InputNetwork.PotentialLoad) - num;
	}

	private async UniTask WaitTryContactReceiverMainThread()
	{
		await UniTask.SwitchToMainThread();
		TryContactReceiver();
	}

	public void TryContactReceiver()
	{
		if (GameManager.RunSimulation && GameManager.GameState == GameState.Running)
		{
			if (LinkedReceiver != null)
			{
				LinkedReceiver.LinkedPowerTransmitter = null;
			}
			LinkedReceiver = null;
			if (Physics.Raycast(RayTransform.transform.position, RayTransform.transform.TransformDirection(Vector3.forward), out var hitInfo, float.PositiveInfinity) && !(hitInfo.transform == null) && Thing._colliderLookup.TryGetValue(hitInfo.collider, out var value) && value is PowerReceiver powerReceiver && hitInfo.transform == powerReceiver.DishTarget && RocketMath.Approximately(Vector3.Angle(RayTransform.forward, powerReceiver.RayTransform.forward), 180f, 7f) && RocketMath.Approximately(Vector3.Angle(RayTransform.right, powerReceiver.RayTransform.right), 180f, 7f))
			{
				LinkedReceiver = powerReceiver;
				LinkedReceiver.LinkedPowerTransmitter = this;
				_linkedReceiverDistance = hitInfo.distance;
			}
		}
	}

	protected override void CheckConnections()
	{
		Cable cable = InputConnection.GetCable();
		InputNetwork = (cable ? cable.CableNetwork : null);
	}

	public override void RunAfterAnimation()
	{
		TryContactReceiver();
	}

	public override void OnRegistered(Cell cell)
	{
		if (GameManager.GameState != GameState.Loading && GameManager.RunSimulation)
		{
			OutputNetwork = WirelessNetwork.GetWirelessNetwork(this);
			OutputNetwork.AddDevice(this);
		}
		base.OnRegistered(cell);
		if (GameManager.GameState == GameState.Running)
		{
			base.Horizontal = 0.0;
			base.Vertical = 1.0;
			base.RotatableBehaviour.TargetHorizontal = base.Horizontal;
			base.RotatableBehaviour.TargetVertical = base.Vertical;
		}
		if (GameManager.GameState != GameState.Loading)
		{
			TryContactReceiver();
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		if (GameManager.RunSimulation)
		{
			OutputNetwork = Referencable.Find<WirelessNetwork>(_savedNetworkId) ?? new WirelessNetwork(this);
			OutputNetwork.AddDevice(this);
			TryContactReceiver();
		}
		if (!GameManager.RunSimulation && _savedRecieverId != 0L)
		{
			LinkedReceiver = Thing.Find<PowerReceiver>(_savedRecieverId);
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new PowerTransmitterSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is PowerTransmitterSaveData powerTransmitterSaveData)
		{
			_savedNetworkId = powerTransmitterSaveData.OutputNetworkReferenceId;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is PowerTransmitterSaveData powerTransmitterSaveData)
		{
			powerTransmitterSaveData.OutputNetworkReferenceId = OutputNetwork?.ReferenceId ?? 0;
		}
	}
}
