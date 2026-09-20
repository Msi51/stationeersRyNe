using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Objects;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class ElectricalInputOutput : Device, ISmartRotatable, ISubmergeable, IPowered, IDensePoolable, IReferencable, IEvaluable
{
	[Header("Electrical I/O")]
	public Connection InputConnection;

	public Connection OutputConnection;

	[ReadOnly]
	public CableNetwork InputNetwork;

	[ReadOnly]
	public CableNetwork OutputNetwork;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	private bool _isSubmerged;

	private const int SUBMERGED_TICKS_BEFORE_BREAK = 60;

	private const float SUBMERGED_BREAK_CHANCE = 0.5f;

	private uint _inputSubmerged;

	private uint _outputSubmerged;

	public override bool IsPowerProvider => true;

	public bool IsSubmerged
	{
		get
		{
			return _isSubmerged;
		}
		set
		{
			if (value != IsSubmerged)
			{
				base.NetworkUpdateFlags |= 512;
				_isSubmerged = value;
			}
		}
	}

	public override bool IsPowerInputOutput => true;

	protected override bool IsOperable
	{
		get
		{
			if (OutputNetwork != null && InputNetwork == OutputNetwork)
			{
				return false;
			}
			return base.IsOperable;
		}
	}

	public virtual float AvailablePower => PotentialLoad;

	public float CurrentLoad
	{
		get
		{
			if (OutputNetwork == null)
			{
				return 0f;
			}
			return OutputNetwork.CurrentLoad;
		}
	}

	public float PotentialLoad
	{
		get
		{
			if (InputNetwork == null)
			{
				return 0f;
			}
			return InputNetwork.PotentialLoad;
		}
	}

	protected virtual bool CanShortOut => OnOff;

	public bool IsValid => !base.IsBeingDestroyed;

	public virtual bool DoSubmergableTick => false;

	public override void InitializeDevice()
	{
		base.InitializeDevice();
		CheckConnections();
	}

	public override void Update100MS(float deltaTime)
	{
		base.Update100MS(deltaTime);
		HandleUnderWaterFX();
	}

	private void HandleUnderWaterFX()
	{
		if (!GameManager.IsBatchMode && CanShortOut && IsSubmerged && RocketMath.Chance(0.25f))
		{
			ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
			{
				position = base.ThingTransformPosition
			};
			WorldManager.Instance.Sparker.Emit(emitParams, Random.Range(200, 350));
			Thing.PlayPooledAudioSound(this, Defines.Sounds.ElectricalFailure, base.transform.forward * 0.1f);
		}
	}

	protected override void CheckConnections()
	{
		Cable cable = InputConnection.GetCable();
		Cable cable2 = OutputConnection.GetCable();
		InputNetwork = (cable ? cable.CableNetwork : null);
		OutputNetwork = (cable2 ? cable2.CableNetwork : null);
	}

	public override void RebuildGridState()
	{
		base.RebuildGridState();
		InputConnection?.SetGrids();
		OutputConnection?.SetGrids();
	}

	public virtual void CheckPower()
	{
		if (GameManager.RunSimulation && InputNetwork == null && Powered)
		{
			OnServer.Interact(base.InteractPowered, 0);
		}
	}

	public override bool IsProviderToDevice(Device device, ref List<long> evaluatedDeviceReferences)
	{
		if (device == null || InputNetwork?.PowerTick?.InputOutputDevices == null)
		{
			return false;
		}
		if (evaluatedDeviceReferences.Contains(base.ReferenceId))
		{
			return false;
		}
		evaluatedDeviceReferences.Add(base.ReferenceId);
		for (int num = InputNetwork.PowerTick.InputOutputDevices.Length - 1; num >= 0; num--)
		{
			if (InputNetwork.PowerTick.InputOutputDevices[num].Device == device)
			{
				return true;
			}
		}
		for (int num2 = InputNetwork.PowerTick.InputOutputDevices.Length - 1; num2 >= 0; num2--)
		{
			PowerProvider powerProvider = InputNetwork.PowerTick.InputOutputDevices[num2];
			if (evaluatedDeviceReferences.Count >= Device.MaxProviderRecursionIterations)
			{
				return false;
			}
			if (powerProvider.Device.IsProviderToDevice(device, ref evaluatedDeviceReferences))
			{
				return true;
			}
		}
		return false;
	}

	public override void OnAddCableNetwork(CableNetwork newNetwork)
	{
		base.OnAddCableNetwork(newNetwork);
		CheckConnections();
		CheckPower();
	}

	public override void OnRemoveCableNetwork(CableNetwork oldNetwork)
	{
		base.OnRemoveCableNetwork(oldNetwork);
		if (oldNetwork == InputNetwork)
		{
			InputNetwork = null;
		}
		if (oldNetwork == OutputNetwork)
		{
			OutputNetwork = null;
		}
		CheckConnections();
		CheckPower();
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		if (InputConnection.ConnectionType != NetworkType.None && hitCollider == InputConnection.Collider)
		{
			PassiveTooltip result = new PassiveTooltip(true);
			result.Title = InterfaceStrings.ConnectionInput;
			return result;
		}
		if (OutputConnection.ConnectionType != NetworkType.None && hitCollider == OutputConnection.Collider)
		{
			PassiveTooltip result = new PassiveTooltip(true);
			result.Title = InterfaceStrings.ConnectionOutput;
			return result;
		}
		return base.GetPassiveTooltip(hitCollider);
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteBoolean(IsSubmerged);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		IsSubmerged = reader.ReadBoolean();
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteBoolean(IsSubmerged);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			IsSubmerged = reader.ReadBoolean();
		}
	}

	private Vector3 SubmergedOffset()
	{
		return Rotation * ((PlacementType == PlacementSnap.FaceMount) ? Vector3.forward : Vector3.up) * 0.001f;
	}

	public void OnSubmergeableTick()
	{
		Cable cable = InputConnection?.GetCable();
		if ((object)cable != null && CanShortOut && AtmosphereHelper.IsSubmerged(InputConnection.LocalGrid.ToVector3() + SubmergedOffset()))
		{
			_inputSubmerged++;
			if (_inputSubmerged > 60 && RocketMath.Chance(0.5f))
			{
				_outputSubmerged = 0u;
				_inputSubmerged = 0u;
				cable.Break();
			}
		}
		else
		{
			_inputSubmerged = 0u;
		}
		Cable cable2 = OutputConnection?.GetCable();
		if ((object)cable2 != null && CanShortOut && AtmosphereHelper.IsSubmerged(OutputConnection.LocalGrid.ToVector3() + SubmergedOffset()))
		{
			_outputSubmerged++;
			if (_outputSubmerged > 60 && RocketMath.Chance(0.5f))
			{
				_outputSubmerged = 0u;
				_inputSubmerged = 0u;
				cable2.Break();
			}
		}
		else
		{
			_outputSubmerged = 0u;
		}
		IsSubmerged = _inputSubmerged != 0 || _outputSubmerged != 0;
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
