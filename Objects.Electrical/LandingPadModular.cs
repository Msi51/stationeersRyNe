using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Networks;
using Objects.Structures;
using Trading;
using UnityEngine;

namespace Objects.Electrical;

public class LandingPadModular : ModularStructure<LandingPadModular, INetworkedLandingPad>, INetworkedLandingPad, INetworkedAtmospherics, INetworkedStructure, INetworkMember, IReferencable, IEvaluable, INetworkedPad, ITrading
{
	[SerializeField]
	private bool _animateLights;

	protected bool _flash;

	private int _phaseBucket;

	public bool AnimateLights => _animateLights;

	public LandingPadNetwork LandingPadNetwork => base.StructureNetwork as LandingPadNetwork;

	ReferencableNetwork INetworkMember.Network
	{
		get
		{
			return base.StructureNetwork;
		}
		set
		{
			base.StructureNetwork = (StructureNetwork)value;
		}
	}

	public override NetworkType NetworkConnectionType => NetworkType.LandingPad;

	public LandingPadCenter LandingPadCenter => LandingPadNetwork.LandingPadCenter;

	public virtual VolumeLitres Volume => new VolumeLitres(1.0);

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

	public PressurekPa MaxPressure => LandingPadNetwork.MaxPressureKpa;

	public PipeBurst DamageRecord { get; set; }

	public List<WorldGrid> CurrentGrids { get; set; } = new List<WorldGrid>();

	public int PhaseBucket => _phaseBucket;

	public async UniTaskVoid BurstPipe(PipeBurst damageSource)
	{
	}

	public override CanConstructInfo CanConstruct()
	{
		Vector3 worldPosition = base.ThingTransformPosition - ThingTransform.up * GridSize;
		Structure structure = base.GridController.Get<Structure>(worldPosition, StructureElement.Center);
		if ((base.RequiresFrame && !structure) || ((bool)structure && !structure.AllowMounting))
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrame.DisplayString);
		}
		Structure structure2 = IsAnyCollidingStructurual(base.GridController.GetCell(base.ThingTransformPosition));
		if ((object)structure2 != null)
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementBlockedByStructure.AsString(structure2.DisplayName));
		}
		return base.CanConstruct();
	}

	private static Structure IsAnyCollidingStructurual(Cell cell)
	{
		if (cell == null)
		{
			return null;
		}
		foreach (Structure allStructure in cell.AllStructures)
		{
			if (allStructure is INetworkedLandingPad || allStructure is Frame || allStructure is Wall)
			{
				return allStructure;
			}
		}
		return null;
	}

	protected override void OnBuildStateUpdated(int newState, int previousState)
	{
		base.OnBuildStateUpdated(newState, previousState);
		base.StructureNetwork?.RefreshNetwork();
	}

	public virtual void OnStructureNetworkUpdated()
	{
		CheckPadPower();
		if (LandingPadCenter == null)
		{
			_phaseBucket = -1;
			return;
		}
		Vector3 vector = LandingPadCenter.ShuttlePosition - base.Position;
		float a = Mathf.Abs(vector.x) / GridSize;
		float b = Mathf.Abs(vector.z) / GridSize;
		_phaseBucket = Mathf.FloorToInt(Mathf.Max(a, b));
	}

	public void CheckPadPower()
	{
		if (GameManager.RunSimulation)
		{
			OnServer.Interact(base.InteractOnOff, (LandingPadCenter != null) ? (LandingPadCenter.OnOff ? 1 : 0) : 0);
		}
	}

	public override void SetCustomColor(int index, bool emissive = false)
	{
		base.SetCustomColor(index, OnOff && _flash);
		if (GameManager.IsValidColor(index))
		{
			SetLightsCustomColor();
		}
	}

	public virtual void FlashLights(bool flash)
	{
		_flash = flash;
		SetCustomColor(OnOff && _flash);
	}

	public virtual void SetLightsCustomColor()
	{
		foreach (ThingLight light in Lights)
		{
			light.Light.color = CustomColor.Light;
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.OnOff)
		{
			SetCustomColor(OnOff);
		}
	}

	public override void OnDestroy()
	{
		if (Singleton<GameManager>.IsQuitting || IsCursor || GameManager.GameState == GameState.None)
		{
			return;
		}
		List<INetworkedStructure> list = new List<INetworkedStructure>(ConnectedStructures());
		LandingPadNetwork landingPadNetwork = LandingPadNetwork;
		base.OnDestroy();
		if (!GameManager.RunSimulation || landingPadNetwork == null)
		{
			return;
		}
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
		ReLinkWaypoints(list2);
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

	private void ReLinkWaypoints(List<AtmosphericsNetwork> newNetworks)
	{
		foreach (AtmosphericsNetwork newNetwork in newNetworks)
		{
			if (newNetwork is LandingPadNetwork landingPadNetwork)
			{
				landingPadNetwork.LinkTaxiWaypoints();
			}
		}
	}

	public override StructureNetwork CreateNewNetwork()
	{
		return new LandingPadNetwork(0L);
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		WorldGrid item = new WorldGrid(ThingTransform.position);
		if (!CurrentGrids.Contains(item))
		{
			CurrentGrids.Add(item);
		}
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
		ThingSaveData savedData = new LandingPadModularSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is LandingPadModularSaveData landingPadModularSaveData)
		{
			landingPadModularSaveData.PadNetworkId = LandingPadNetwork?.ReferenceId ?? 0;
		}
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is LandingPadModularSaveData landingPadModularSaveData)
		{
			Referencable.Find<LandingPadNetwork>(landingPadModularSaveData.PadNetworkId)?.Add(this);
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		CheckPadPower();
	}

	public void OnImGuiDraw()
	{
	}
}
