using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Sound;
using Assets.Scripts.UI;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Networks;
using Objects.Rockets;
using Objects.Rockets.Log.RocketEvents;
using Trading;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Pipes;

public class Pipe : SmallSingleGrid, ISmartRotatable, IThermal, INetworkedPipe, INetworkedAtmospherics, INetworkedStructure, INetworkMember, IReferencable, IEvaluable, ISmallGrid, ITooltip, IRocketInternals, IRocketComponent, IVolume, IWorkingAtmosphere
{
	public enum ContentType
	{
		Unknown,
		Gas,
		Liquid,
		All
	}

	private PipeBurst _damageRecord;

	private float _damageCoolDown;

	private static readonly System.Random DamageSoundRandom = new System.Random();

	private const float DAMAGE_SOUND_BASE_COOLDOWN = 5f;

	[NonSerialized]
	public float PipeEnergyRadiated;

	[NonSerialized]
	public float PipeEnergyConvected;

	[SerializeField]
	private RocketInternalCellType _rocketInternalCellType;

	[SerializeField]
	[FormerlySerializedAs("Volume")]
	[Header("Pipes")]
	private float volume = 10f;

	public bool IsStraight;

	private PipeBurst _isBurst;

	public Mesh BurstMesh;

	private Mesh _originalMesh;

	private bool _bursting;

	[Tooltip("Pipe insulation type: Normal or Insulated")]
	public Piping.Type PipeType;

	[SerializeField]
	[FormerlySerializedAs("PipeContentType")]
	[Tooltip("What this pipe can hold: Gas or Liquid")]
	private ContentType pipeContentType = ContentType.Gas;

	[Tooltip("Custom offset for scaling labels placed on this pipe. Adjusts how far 'out from' the pipe the label will sit. Offset of 1 is perfect for normal gas pipes.")]
	public float LabelSizeOffset = 1f;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	protected CrewModule _crewModule;

	private static readonly Vector3[] GridDirections = new Vector3[8]
	{
		(Vector3.up - Vector3.right - Vector3.forward) * 0.2f / 1.7320508f,
		(Vector3.up - Vector3.right + Vector3.forward) * 0.2f / 1.7320508f,
		(Vector3.up + Vector3.right - Vector3.forward) * 0.2f / 1.7320508f,
		(Vector3.up + Vector3.right + Vector3.forward) * 0.2f / 1.7320508f,
		(-Vector3.up - Vector3.right - Vector3.forward) * 0.2f / 1.7320508f,
		(-Vector3.up - Vector3.right + Vector3.forward) * 0.2f / 1.7320508f,
		(-Vector3.up + Vector3.right - Vector3.forward) * 0.2f / 1.7320508f,
		(-Vector3.up + Vector3.right + Vector3.forward) * 0.2f / 1.7320508f
	};

	public PipeBurst DamageRecord
	{
		get
		{
			return _damageRecord;
		}
		set
		{
			_damageRecord = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 512;
			}
			if (GameManager.GameState == GameState.Running && GameManager.RunSimulation && GameManager.GameTime > _damageCoolDown)
			{
				AudioEvent.Create(this, Defines.Sounds.PipeDamageHash);
				_damageCoolDown = GameManager.GameTime + (float)DamageSoundRandom.NextDouble() + (5f - DamageState.TotalRatioClamped * 5f);
			}
		}
	}

	public override float ConvectionFactor => ThermodynamicsScale * 0.1f;

	public override float RadiationFactor => ThermodynamicsScale * 0.005f;

	public RocketInternalCellType InternalCellType => _rocketInternalCellType;

	public bool StrictlyInternal => false;

	public RocketNetwork RocketNetwork { get; set; }

	public override float EnergyRadiated
	{
		get
		{
			return PipeNetwork?.EnergyRadiated ?? 0f;
		}
		set
		{
			PipeEnergyRadiated = value;
		}
	}

	public override float EnergyConvected
	{
		get
		{
			return PipeNetwork?.EnergyConvected ?? 0f;
		}
		set
		{
			PipeEnergyConvected = value;
		}
	}

	public VolumeLitres Volume => new VolumeLitres(volume);

	public PressurekPa MaxPressure
	{
		get
		{
			switch (PipeType)
			{
			case Piping.Type.normal:
			case Piping.Type.Insulated:
				if (pipeContentType != ContentType.Liquid)
				{
					return Chemistry.Limits.MAXPressureGasPipe;
				}
				return Chemistry.Limits.MAXPressureLiquidPipe;
			case Piping.Type.NormalLowVolume:
			case Piping.Type.InsulatedLowVolume:
				if (pipeContentType != ContentType.Liquid)
				{
					return Chemistry.Limits.MAXPressureGasPipe;
				}
				return Chemistry.Limits.MAXPressureLiquidPipe;
			case Piping.Type.Duct:
				return Chemistry.Limits.MAXPressureGasDuct;
			default:
				return Chemistry.Limits.MAXPressureGasPipe;
			}
		}
	}

	public PipeBurst IsBurst
	{
		get
		{
			return _isBurst;
		}
		private set
		{
			if (value != _isBurst)
			{
				_isBurst = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 4096;
				}
				if ((bool)BurstMesh && IsBurst != PipeBurst.None)
				{
					Renderers[0].MeshFilter.mesh = BurstMesh;
					BuildStates[0].Tool.ToolEntry = null;
					PipeLeak.Register(new PipeLeak(this, Transform.TransformVector(BurstEffectLocalPositionOffset)));
					RegisterGridUpdate();
					Achievements.AchieveItsGonnaBlow(this);
				}
			}
		}
	}

	protected virtual Vector3 BurstEffectLocalPositionOffset => Vector3.zero;

	public override bool HasBrokenMesh
	{
		get
		{
			if (!base.HasBrokenMesh)
			{
				return (object)BurstMesh != null;
			}
			return true;
		}
	}

	public ContentType PipeContentType => pipeContentType;

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

	public PipeNetwork PipeNetwork => StructureNetwork as PipeNetwork;

	public List<WorldGrid> CurrentGrids { get; set; } = new List<WorldGrid>();

	public override bool Stressed
	{
		get
		{
			return _stressed;
		}
		set
		{
			if (value != _stressed)
			{
				if (value)
				{
					AtmosphericAudioHandler.Instance.AddStressedPipe(this);
				}
				else
				{
					AtmosphericAudioHandler.Instance.RemoveStressedPipe(this);
				}
				_stressed = value;
			}
		}
	}

	public VolumeLitres GetVolume => Volume;

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.PipesCategory);
	}

	protected override CanConstructInfo CanDeconstruct()
	{
		if (AttachedDevices.Count <= 0)
		{
			return base.CanDeconstruct();
		}
		SmallGrid smallGrid = AttachedDevices[0];
		if ((object)smallGrid != null && !smallGrid.IsBeingDestroyed)
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.InvalidAttachmentsDeconstruct.AsString(smallGrid.ToTooltip()));
		}
		return base.CanDeconstruct();
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}

	public List<INetworkedStructure> ConnectedStructures()
	{
		Span<SmallCellRef> span = stackalloc SmallCellRef[32];
		int count = 0;
		FillConnected<INetworkedPipe>(span, ref count);
		List<INetworkedStructure> list = new List<INetworkedStructure>(count);
		Span<SmallCellRef> span2 = span;
		Span<SmallCellRef> span3 = span2.Slice(0, count);
		for (int i = 0; i < span3.Length; i++)
		{
			SmallCellRef smallCellRef = span3[i];
			if (smallCellRef.TryGet<INetworkedPipe>(out var found) && found.ReferenceId != base.ReferenceId)
			{
				list.Add(found);
			}
		}
		return list;
	}

	public void OnStructureNetworkUpdated()
	{
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteByte((byte)DamageRecord);
		}
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			writer.WriteByte((byte)IsBurst);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			DamageRecord = (PipeBurst)reader.ReadByte();
		}
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			IsBurst = (PipeBurst)reader.ReadByte();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt64(PipeNetwork?.ReferenceId ?? 0);
		writer.WriteByte((byte)DamageRecord);
		writer.WriteByte((byte)IsBurst);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		PipeNetwork pipeNetwork = Referencable.Find<PipeNetwork>(reader.ReadInt64());
		if (GameManager.GameState != GameState.Joining && StructureNetwork.Merge(StructureNetwork.ConnectedNetworks(this), out var mergedNetwork))
		{
			pipeNetwork = mergedNetwork as PipeNetwork;
		}
		pipeNetwork.Add(this);
		DamageRecord = (PipeBurst)reader.ReadByte();
		IsBurst = (PipeBurst)reader.ReadByte();
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new PipeSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public void RegisterGrid(Vector3 position)
	{
		WorldGrid item = new WorldGrid(position);
		if (!CurrentGrids.Contains(item))
		{
			CurrentGrids.Add(item);
		}
	}

	private bool CanMixInWorld()
	{
		if (_crewModule != null)
		{
			return true;
		}
		foreach (WorldGrid currentGrid in CurrentGrids)
		{
			if (base.GridController.CanContainAtmos(currentGrid))
			{
				return true;
			}
		}
		return false;
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		bool flag;
		switch (RocketNetwork?.Rocket?.RocketState)
		{
		case RocketState.Launching:
		case RocketState.Landing:
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			RegisterCurrentGrids();
		}
		if (!_bursting)
		{
			return;
		}
		PipeNetwork.SetNetworkFault(faultState: true);
		if (!CanMixInWorld())
		{
			return;
		}
		SpawnIces().Forget();
		if (_crewModule != null)
		{
			LeakMix(_crewModule.InternalAtmosphere);
			return;
		}
		foreach (WorldGrid currentGrid in CurrentGrids)
		{
			if (base.GridController.CanContainAtmos(currentGrid))
			{
				Atmosphere environmentWrite = base.AtmosphericsController.CloneGlobalAtmosphere(currentGrid, 0L);
				LeakMix(environmentWrite);
			}
		}
	}

	private void LeakMix(Atmosphere environmentWrite)
	{
		PressurekPa pressureGassesAndLiquids = PipeNetwork.Atmosphere.PressureGassesAndLiquids;
		PressurekPa pressurekPa = RocketMath.Abs(environmentWrite.PressureGassesAndLiquids - pressureGassesAndLiquids);
		PressurekPa amountPressureToMove = RocketMath.Max(pressurekPa - MaxPressure, pressurekPa / 10.0 / CurrentGrids.Count);
		double num = (Volume / PipeNetwork.Atmosphere.Volume).ToDouble() * PipeNetwork.Atmosphere.TotalMoles.ToDouble();
		num = ((pressurekPa > MaxPressure) ? 3.4028234663852886E+38 : num);
		VolumeLitres maxVolumeToMove = ((PipeNetwork.Atmosphere.TotalVolumeLiquids > PipeNetwork.Atmosphere.Volume) ? (PipeNetwork.Atmosphere.TotalVolumeLiquids - PipeNetwork.Atmosphere.Volume * 0.99) : Volume);
		AtmosphereHelper.DrainLiquids(PipeNetwork.Atmosphere, environmentWrite, maxVolumeToMove);
		if (pressurekPa > Chemistry.OneAtmosphere)
		{
			double value = num / (double)CurrentGrids.Count;
			AtmosphereHelper.MoveToEqualizeBidirectional(PipeNetwork.Atmosphere, environmentWrite, amountPressureToMove, AtmosphereHelper.MatterState.Gas, new MoleQuantity(value));
		}
		else
		{
			AtmosphereHelper.Mix(PipeNetwork.Atmosphere, environmentWrite, AtmosphereHelper.MatterState.Gas);
		}
	}

	private async UniTaskVoid SpawnIces()
	{
		GasMixture frozenGasMixInPipe = PipeNetwork.Atmosphere.GasMixture.CheckForIceFormation(PipeNetwork.Atmosphere.PressureGasses, new MoleQuantity(50.0));
		if (frozenGasMixInPipe.GetQuantity <= 0f)
		{
			return;
		}
		PipeNetwork.Atmosphere.GasMixture.Remove(frozenGasMixInPipe);
		while (frozenGasMixInPipe.GetQuantity > 0f)
		{
			await UniTask.Delay(25, DelayType.DeltaTime, PlayerLoopTiming.FixedUpdate);
			if (GameManager.GameState != GameState.Running)
			{
				break;
			}
			AtmosphereHelper.SpawnIce(GasMixtureHelper.RemoveStackWorthOfFrozenGas(ref frozenGasMixInPipe), base.Position);
		}
	}

	public override void OnStructureBroken()
	{
		base.OnStructureBroken();
		BurstPipe(PipeBurst.Pressure).Forget();
	}

	public virtual async UniTaskVoid BurstPipe(PipeBurst damageSource)
	{
		await UniTask.SwitchToMainThread();
		if (GameManager.GameState == GameState.Running && GameManager.RunSimulation)
		{
			AudioEvent.Create(this, Defines.Sounds.PipeFailHash);
			Rocket rocket = RocketNetwork?.Rocket;
			rocket?.Report(new PipeFailEvent(rocket, DamageRecord, this));
		}
		IsBurst = damageSource;
		_bursting = true;
		if (GameManager.RunSimulation && base.SmallCell.Device is DevicePipeMounted)
		{
			OnServer.Destroy(base.SmallCell.Device);
		}
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is PipeSaveData { PipeNetworkId: var pipeNetworkId } pipeSaveData)
		{
			(Referencable.Find<PipeNetwork>(pipeNetworkId) ?? new PipeNetwork(pipeNetworkId)).Add(this);
			DamageRecord = pipeSaveData.DamageRecord;
			if (pipeSaveData.IsBurst != PipeBurst.None)
			{
				BurstPipe(pipeSaveData.IsBurst).Forget();
				_bursting = true;
			}
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is PipeSaveData pipeSaveData)
		{
			pipeSaveData.PipeNetworkId = PipeNetwork?.ReferenceId ?? 0;
			pipeSaveData.IsBurst = IsBurst;
			pipeSaveData.DamageRecord = DamageRecord;
		}
	}

	public bool ProhibitConnection(SmallGrid potentialConnection)
	{
		return false;
	}

	public override void OnRegistered(Cell cell)
	{
		if (GameManager.GameState != GameState.Loading && GameManager.RunSimulation)
		{
			StructureNetwork.Merge(StructureNetwork.ConnectedNetworks(this), out var mergedNetwork);
			if (mergedNetwork != null)
			{
				mergedNetwork.Add(this);
			}
			else
			{
				new PipeNetwork(0L).Add(this);
			}
		}
		base.OnRegistered(cell);
		RegisterCurrentGrids();
		AtmosphericsManager.Instance.Register(this);
		_crewModule = (Cell.IsInCrewModule(base.WorldGrid, out var crewModule) ? crewModule : null);
	}

	public Atmosphere GetWorkingAtmosphere()
	{
		if (_crewModule != null)
		{
			return _crewModule.InternalAtmosphere;
		}
		return base.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
	}

	public void CacheWorkingAtmosphere()
	{
		_crewModule = (Cell.IsInCrewModule(base.WorldGrid, out var crewModule) ? crewModule : null);
	}

	protected virtual void RegisterCurrentGrids()
	{
		lock (CurrentGrids)
		{
			CurrentGrids.Clear();
			Vector3[] gridDirections = GridDirections;
			foreach (Vector3 vector in gridDirections)
			{
				RegisterGrid(base.Position + vector);
			}
		}
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
		Span<SmallCellRef> span = stackalloc SmallCellRef[32];
		int count = 0;
		FillConnected<INetworkedPipe>(span, ref count);
		PipeNetwork pipeNetwork = PipeNetwork;
		PipeLeak.DeRegister(this);
		PipeNetwork?.Remove(this);
		if (GameManager.RunSimulation && pipeNetwork != null)
		{
			GasMixture gasMixture = GasMixtureHelper.Create();
			if (pipeNetwork.Atmosphere != null)
			{
				gasMixture.Set(pipeNetwork.Atmosphere.GasMixture);
			}
			List<AtmosphericsNetwork> list = new List<AtmosphericsNetwork>(6);
			Span<SmallCellRef> span2 = span;
			Span<SmallCellRef> span3 = span2.Slice(0, count);
			for (int i = 0; i < span3.Length; i++)
			{
				SmallCellRef smallCellRef = span3[i];
				if (smallCellRef.TryGet<INetworkedPipe>(out var found))
				{
					found.PipeNetwork.RebuildNetworkServer(found);
					list.Add(found.PipeNetwork);
				}
			}
			if (gasMixture.IsValid)
			{
				if (pipeNetwork?.Atmosphere == null || !pipeNetwork.Atmosphere.IsAwaitingEvent)
				{
					NetworkAtmosphereEvent.DivideNetworkAtmosphere(list, gasMixture);
				}
				else
				{
					NetworkAtmosphereEvent.DivideNetworkAtmosphere(list, pipeNetwork.Atmosphere);
				}
			}
		}
		base.OnDestroy();
	}

	public bool IsContentMatch(ContentType inContentType)
	{
		if (inContentType != PipeContentType)
		{
			return PipeContentType == ContentType.All;
		}
		return true;
	}

	public override bool IsConnected(Connection otherEnd)
	{
		if (!(otherEnd.Parent is Pipe pipe))
		{
			return base.IsConnected(otherEnd);
		}
		if (!IsContentMatch(pipe.PipeContentType))
		{
			return false;
		}
		return base.IsConnected(otherEnd);
	}

	public override CanConstructInfo CanConstruct()
	{
		SmallCell smallCell = base.GridController.GetSmallCell(base.ThingTransformPosition);
		if (smallCell != null && smallCell.Device != null)
		{
			DevicePipeMounted devicePipeMounted = smallCell.Device as DevicePipeMounted;
			if (devicePipeMounted != null)
			{
				if (!IsStraight || !devicePipeMounted.IsSameOrientation(this))
				{
					return CanConstructInfo.InvalidPlacement(GameStrings.PlacementBlockedBySmallGrid.AsString(devicePipeMounted.DisplayName));
				}
				if (devicePipeMounted.contentType != pipeContentType)
				{
					return CanConstructInfo.InvalidPlacement(GameStrings.PipeContentTypeIncorrect.AsString(devicePipeMounted.DisplayName));
				}
			}
		}
		Grid3[] array = (Grid3[])GridBounds.GetLocalSmallGrid(base.ThingTransformPosition, base.ThingTransformRotation);
		foreach (Grid3 localGrid in array)
		{
			SmallCell smallCell2 = base.GridController.GetSmallCell(localGrid);
			if (smallCell2 != null && smallCell2.Pipe != null && !(smallCell2.Pipe is Piping))
			{
				return CanConstructInfo.InvalidPlacement(GameStrings.CannotMergeWithSmallGrid.AsString(smallCell2.Pipe.DisplayName));
			}
		}
		if (IsConnectingToUmbilical(out var found))
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementIsNotUmbilicalConnector.AsString(found.AsThing.ToTooltip()));
		}
		return base.CanConstruct();
	}

	public void OnImGuiDraw()
	{
		foreach (Connection openEnd in OpenEnds)
		{
			Vector3 vector = openEnd.Transform.position.GridCenter(SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset);
			Vector3 vector2 = base.transform.position;
			Vector3 normalized = (vector2 - vector).normalized;
			Vector3 worldPos = vector + normalized * (SmallGrid.SmallGridSize * 0.5f);
			switch (PipeType)
			{
			case Piping.Type.normal:
			case Piping.Type.NormalLowVolume:
				ImGuiExtensions.Rendering.DrawClippedLine(ImGuiExtensions.WorldToScreen(vector2), ImGuiExtensions.WorldToScreen(worldPos));
				break;
			case Piping.Type.Insulated:
			case Piping.Type.InsulatedLowVolume:
				ImGuiExtensions.Rendering.DrawClippedDottedLine(vector2, worldPos);
				break;
			}
		}
	}

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = permutation;
	}

	public void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}

	public int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (IsBurst != PipeBurst.None)
		{
			if ((DamageRecord & PipeBurst.Pressure) != PipeBurst.None)
			{
				extendedText.AppendLine(GameStrings.PipeFailModePressure.AsString(ToTooltip()));
			}
			if ((DamageRecord & PipeBurst.Liquid) != PipeBurst.None)
			{
				extendedText.AppendLine(GameStrings.PipeFailModeLiquid.AsString(ToTooltip()));
			}
			if ((DamageRecord & PipeBurst.Solid) != PipeBurst.None)
			{
				extendedText.AppendLine(GameStrings.PipeFailModeFrozen.AsString(ToTooltip()));
			}
		}
		else if (DamageState.TotalRatioClamped > 0f)
		{
			if ((DamageRecord & PipeBurst.Pressure) != PipeBurst.None)
			{
				extendedText.AppendLine(GameStrings.PipeDamageModePressure.AsString(ToTooltip()));
			}
			if ((DamageRecord & PipeBurst.Liquid) != PipeBurst.None)
			{
				extendedText.AppendLine(GameStrings.PipeDamageModeLiquid.AsString(ToTooltip()));
			}
			if ((DamageRecord & PipeBurst.Solid) != PipeBurst.None)
			{
				extendedText.AppendLine(GameStrings.PipeDamageModeFrozen.AsString(ToTooltip()));
			}
		}
		if (IsBurst != PipeBurst.None)
		{
			return extendedText;
		}
		if (PipeContentType == ContentType.Gas && PipeNetwork != null)
		{
			if (PipeNetwork.Atmosphere != null && PipeNetwork.Atmosphere.Condensation && PipeNetwork.Atmosphere.TotalMolesLiquids > Chemistry.MINIMUM_VALID_TOTAL_MOLES)
			{
				extendedText.Append("<color=red>");
				extendedText.Append(GameStrings.CondensationInGasPipe.DisplayString);
				extendedText.AppendLine("</color>");
			}
			else
			{
				Atmosphere atmosphere = PipeNetwork.Atmosphere;
				if (atmosphere != null && atmosphere.TotalMolesLiquids > Chemistry.MINIMUM_VALID_TOTAL_MOLES)
				{
					extendedText.AppendLine(GameStrings.LiquidInGasPipe.DisplayString);
				}
			}
		}
		if (Stressed)
		{
			extendedText.Append("<color=red>");
			extendedText.Append(GameStrings.PipeStressed.DisplayString);
			extendedText.AppendLine("</color>");
		}
		foreach (Connection openEnd in OpenEnds)
		{
			if (openEnd.GetDevice(connected: false) is DeviceInputOutput deviceInputOutput)
			{
				if (deviceInputOutput.InputConnection?.GetINetworkedPipe(connected: false)?.ReferenceId == base.ReferenceId)
				{
					extendedText.AppendLine(InterfaceStrings.ConnectionInput);
					break;
				}
				if (deviceInputOutput.OutputConnection?.GetINetworkedPipe(connected: false)?.ReferenceId == base.ReferenceId)
				{
					extendedText.AppendLine(InterfaceStrings.ConnectionOutput);
					break;
				}
				if (deviceInputOutput.InputConnection2?.GetINetworkedPipe(connected: false)?.ReferenceId == base.ReferenceId)
				{
					extendedText.AppendLine(InterfaceStrings.ConnectionInput2);
					break;
				}
				if (deviceInputOutput.OutputConnection2?.GetINetworkedPipe(connected: false)?.ReferenceId == base.ReferenceId)
				{
					extendedText.AppendLine(InterfaceStrings.ConnectionOutput2);
					break;
				}
			}
		}
		return extendedText;
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip result = base.GetPassiveTooltip(hitCollider);
		if (string.IsNullOrEmpty(result.Title))
		{
			string text = GetExtendedText().ToString();
			if (!string.IsNullOrEmpty(text))
			{
				result = new PassiveTooltip
				{
					Title = DisplayName,
					Extended = text,
					Slider = -1f
				};
			}
		}
		return result;
	}

	public List<ThingRenderer> GetThingRenderers()
	{
		return Renderers;
	}

	protected override void UpgradeStructureServer(CreateStructureInstance instance)
	{
		bool num = PipeNetwork.StructureList.Count == 1;
		GasMixture gasMixture = GasMixtureHelper.Invalid;
		if (num)
		{
			gasMixture = GasMixtureHelper.Create(PipeNetwork.Atmosphere.GasMixture);
		}
		OnServer.Destroy(this);
		if ((bool)PaintableMaterial && (bool)CustomColor.Normal)
		{
			instance.CustomColor = CustomColor.Index;
		}
		Pipe pipe = Constructor.SpawnConstruct(instance) as Pipe;
		if (num && (bool)pipe)
		{
			AtmosphericEventInstance.CreateAdd(pipe.PipeNetwork.Atmosphere, gasMixture);
		}
	}
}
