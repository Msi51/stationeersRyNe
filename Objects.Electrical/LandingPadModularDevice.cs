using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Networks;
using Trading;
using UnityEngine;

namespace Objects.Electrical;

public class LandingPadModularDevice : Device, INetworkedLandingPad, INetworkedAtmospherics, INetworkedStructure, INetworkMember, IReferencable, IEvaluable, INetworkedPad, ISmartRotatable
{
	[SerializeField]
	private bool _animateLights;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	private static readonly List<INetworkedStructure> FoundLandingPads = new List<INetworkedStructure>();

	public bool AnimateLights => _animateLights;

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

	public virtual int PhaseBucket => -1;

	public List<WorldGrid> CurrentGrids { get; set; } = new List<WorldGrid>();

	public VolumeLitres Volume => new VolumeLitres(1.0);

	public override float ConvectionFactor => 0f;

	public override float RadiationFactor => 0f;

	public Pipe.ContentType PipeContentType => Pipe.ContentType.All;

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

	protected override bool IsOperable
	{
		get
		{
			if (Error == 1)
			{
				if (LandingPadCenter == null)
				{
					return false;
				}
				if (GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractError, 0);
				}
				return true;
			}
			if (LandingPadCenter != null)
			{
				return true;
			}
			if (GameManager.RunSimulation)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			return false;
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

	protected override void OnBuildStateUpdated(int newState, int previousState)
	{
		base.OnBuildStateUpdated(newState, previousState);
		StructureNetwork?.RefreshNetwork();
	}

	public async UniTaskVoid BurstPipe(PipeBurst damageSource)
	{
	}

	public virtual void FlashLights(bool flash)
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
			if (openEnd.ConnectionType == NetworkType.LandingPad)
			{
				Grid3 localGrid = base.GridController.WorldToLocalGrid(openEnd.Transform.position, GridSize, GridOffset);
				SmallCell smallCell = base.GridController.GetSmallCell(localGrid);
				if (smallCell?.Other != null && smallCell.Other as LandingPadModular != this && smallCell.Other.IsConnected(openEnd) && smallCell.Other is INetworkedLandingPad)
				{
					connBuf[connCount++] = openEnd;
				}
				else if (smallCell?.Device != null && smallCell.Device.IsConnected(openEnd) && smallCell.Device is INetworkedLandingPad)
				{
					connBuf[connCount++] = openEnd;
				}
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
	}

	public virtual void OnStructureNetworkUpdated()
	{
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
		ThingSaveData savedData = new LandingPadModularDeviceSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is LandingPadModularDeviceSaveData landingPadModularDeviceSaveData)
		{
			landingPadModularDeviceSaveData.PadNetworkId = LandingPadNetwork?.ReferenceId ?? 0;
		}
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is LandingPadModularDeviceSaveData { PadNetworkId: var padNetworkId })
		{
			(Referencable.Find<LandingPadNetwork>(padNetworkId) ?? new LandingPadNetwork(padNetworkId)).Add(this);
		}
	}

	public void OnImGuiDraw()
	{
	}
}
