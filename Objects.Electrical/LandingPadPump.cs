using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Networks;
using Trading;
using UnityEngine;

namespace Objects.Electrical;

public class LandingPadPump : DeviceInputOutput, INetworkedLandingPad, INetworkedAtmospherics, INetworkedStructure, INetworkMember, IReferencable, IEvaluable, INetworkedPad
{
	[SerializeField]
	private bool LightAnimation = true;

	private static readonly List<INetworkedStructure> FoundLandingPads = new List<INetworkedStructure>();

	public bool AnimateLights => LightAnimation;

	public StructureNetwork StructureNetwork { get; set; }

	ReferencableNetwork INetworkMember.Network
	{
		get
		{
			return StructureNetwork;
		}
		set
		{
			StructureNetwork = (StructureNetwork)value;
		}
	}

	public LandingPadNetwork LandingPadNetwork => StructureNetwork as LandingPadNetwork;

	public LandingPadCenter LandingPadCenter => LandingPadNetwork?.LandingPadCenter;

	public PressurekPa MaxPressure => LandingPadNetwork.MaxPressureKpa;

	public PipeBurst DamageRecord { get; set; }

	public int PhaseBucket => -1;

	public PipeBurst IsBurst
	{
		get
		{
			if (!IsBroken)
			{
				return PipeBurst.None;
			}
			return PipeBurst.Pressure;
		}
	}

	public List<WorldGrid> CurrentGrids { get; set; } = new List<WorldGrid>();

	public VolumeLitres Volume => new VolumeLitres(1.0);

	public override float ConvectionFactor => 0f;

	public override float RadiationFactor => 0f;

	public Pipe.ContentType PipeContentType => Pipe.ContentType.All;

	public override bool HasReadableAtmosphere => true;

	public void OnStructureNetworkUpdated()
	{
		if (GameManager.RunSimulation)
		{
			AssessPower(null, LandingPadCenter != null && LandingPadCenter.OnOff);
		}
	}

	protected override void AssessPower(CableNetwork cableNetwork, bool isOn)
	{
		SetPower(cableNetwork, isOn);
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		AssessPower(null, (object)LandingPadCenter != null && LandingPadCenter.OnOff);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		AssessPower(null, (object)LandingPadCenter != null && LandingPadCenter.OnOff);
	}

	protected override void OnBuildStateUpdated(int newState, int previousState)
	{
		base.OnBuildStateUpdated(newState, previousState);
		StructureNetwork?.RefreshNetwork();
	}

	public void FlashLights(bool flash)
	{
	}

	public async UniTaskVoid BurstPipe(PipeBurst damageSource)
	{
	}

	public List<INetworkedStructure> ConnectedStructures()
	{
		FoundLandingPads.Clear();
		foreach (Connection openEnd in OpenEnds)
		{
			Grid3 localGrid = base.GridController.WorldToLocalGrid(openEnd.Transform.position);
			SmallCell smallCell = base.GridController.GetSmallCell(localGrid);
			if (smallCell?.Other != null && smallCell.Other.IsConnected(openEnd) && smallCell.Other is INetworkedLandingPad item)
			{
				FoundLandingPads.Add(item);
			}
			else if (smallCell?.Device != null && smallCell.Device != this && smallCell.Device.IsConnected(openEnd) && smallCell.Device is INetworkedLandingPad item2)
			{
				FoundLandingPads.Add(item2);
			}
		}
		return FoundLandingPads;
	}

	public override bool IsConnected(Connection otherEnd)
	{
		if (otherEnd.ConnectionType == NetworkType.LandingPad)
		{
			Grid3 grid = base.GridController.WorldToLocalGrid(ThingTransform.position, GridSize, GridOffset);
			if (base.GridController.WorldToLocalGrid(otherEnd.Transform.position, GridSize, GridOffset) == grid)
			{
				return true;
			}
		}
		return base.IsConnected(otherEnd);
	}

	public override void WillJoinNetwork(Span<ConnectionRef> connBuf, ref int connCount)
	{
		base.WillJoinNetwork(connBuf, ref connCount);
		foreach (Connection openEnd in OpenEnds)
		{
			Grid3 localGrid = base.GridController.WorldToLocalGrid(openEnd.Transform.position, GridSize, GridOffset);
			SmallCell smallCell = base.GridController.GetSmallCell(localGrid);
			if ((object)smallCell?.Other != null && smallCell.Other != this && smallCell.Other.IsConnected(openEnd) && smallCell.Other is INetworkedPad)
			{
				connBuf[connCount++] = openEnd;
			}
			else if ((object)smallCell?.Device != null && smallCell.Device.IsConnected(openEnd) && smallCell.Device is INetworkedPad)
			{
				connBuf[connCount++] = openEnd;
			}
		}
	}

	public override void OnRegistered(Cell cell)
	{
		if (GameManager.GameState != GameState.Loading && GameManager.RunSimulation)
		{
			if (StructureNetwork.Merge(StructureNetwork.ConnectedNetworks(this), out var mergedNetwork))
			{
				mergedNetwork.Add(this);
			}
			else
			{
				new LandingPadNetwork(0L).Add(this);
			}
		}
		base.OnRegistered(cell);
		WorldGrid item = new WorldGrid(ThingTransform.position);
		if (!CurrentGrids.Contains(item))
		{
			CurrentGrids.Add(item);
		}
		AtmosphericsManager.Instance.Register(this);
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		AtmosphericsManager.Instance.Deregister(this);
	}

	public override void OnDestroy()
	{
		if (Singleton<GameManager>.IsQuitting || IsCursor || GameManager.GameState == GameState.None)
		{
			return;
		}
		List<INetworkedStructure> list = new List<INetworkedStructure>(ConnectedStructures());
		LandingPadNetwork landingPadNetwork = LandingPadNetwork;
		StructureNetwork?.Remove(this);
		if (GameManager.RunSimulation && landingPadNetwork != null)
		{
			GasMixture gasMixture = GasMixtureHelper.Create();
			if (landingPadNetwork.Atmosphere != null)
			{
				gasMixture.Set(landingPadNetwork.Atmosphere.GasMixture);
			}
			List<AtmosphericsNetwork> list2 = new List<AtmosphericsNetwork>(6);
			foreach (INetworkedStructure item in list)
			{
				if (item is INetworkedLandingPad networkedLandingPad)
				{
					networkedLandingPad.LandingPadNetwork.RebuildNetworkServer(networkedLandingPad);
					list2.Add(networkedLandingPad.LandingPadNetwork);
				}
			}
			if (gasMixture.IsValid)
			{
				if (landingPadNetwork?.Atmosphere == null || !landingPadNetwork.Atmosphere.IsAwaitingEvent)
				{
					NetworkAtmosphereEvent.DivideNetworkAtmosphere(list2, gasMixture);
				}
				else
				{
					NetworkAtmosphereEvent.DivideNetworkAtmosphere(list2, landingPadNetwork.Atmosphere);
				}
			}
		}
		base.OnDestroy();
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt64(LandingPadNetwork?.ReferenceId ?? 0);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		StructureNetwork structureNetwork = Referencable.Find<StructureNetwork>(reader.ReadInt64());
		StructureNetwork structureNetwork2 = structureNetwork;
		if (GameManager.GameState != GameState.Joining)
		{
			structureNetwork2 = (StructureNetwork.Merge(StructureNetwork.ConnectedNetworks(this), out var mergedNetwork) ? mergedNetwork : structureNetwork);
		}
		structureNetwork2.Add(this);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new LandingPadPumpSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is LandingPadPumpSaveData landingPadPumpSaveData)
		{
			landingPadPumpSaveData.PadNetworkId = LandingPadNetwork?.ReferenceId ?? 0;
		}
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is LandingPadPumpSaveData { PadNetworkId: var padNetworkId })
		{
			(Referencable.Find<LandingPadNetwork>(padNetworkId) ?? new LandingPadNetwork(padNetworkId)).Add(this);
		}
	}

	public override double GasRatio(LogicType logicType)
	{
		return AtmosphereHelper.GasRatio(logicType, LandingPadNetwork?.Atmosphere);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.Combustion:
			if (LandingPadNetwork?.Atmosphere?.Sparked != true)
			{
				return 0.0;
			}
			return 1.0;
		case LogicType.TotalMoles:
			return (LandingPadNetwork?.Atmosphere?.TotalMoles.ToDouble()).GetValueOrDefault();
		case LogicType.Pressure:
			return (LandingPadNetwork?.Atmosphere?.PressureGassesAndLiquids.ToDouble()).GetValueOrDefault();
		case LogicType.Temperature:
			return (LandingPadNetwork?.Atmosphere?.Temperature.ToDouble()).GetValueOrDefault();
		default:
			return base.GetLogicValue(logicType);
		}
	}

	public void OnImGuiDraw()
	{
	}
}
