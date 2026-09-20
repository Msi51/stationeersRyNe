using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Serialization;
using Assets.Scripts.Sound;
using Assets.Scripts.UI.HelperHints.Extensions;
using Assets.Scripts.Util;
using TerrainSystem;
using TerrainSystem.Lods;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class DeepMiner : DeviceInputOutputImportExportCircuit, IGenerateMinables
{
	[Header("Deep Miner")]
	[SerializeField]
	private Transform _drillShaftBase;

	[SerializeField]
	private Transform _shaftEndPosition;

	[SerializeField]
	private Transform _drillShaft;

	[SerializeField]
	private Transform _drillHead;

	[SerializeField]
	private Transform _drillBit;

	[Tooltip("Points where the drill will mine voxels")]
	[SerializeField]
	private Transform[] _drillPoints;

	[SerializeField]
	private Gear[] _gears;

	private static DirtyOre _spawnOrePrefab;

	private const float DRILL_DOWN_SPEED = 0.125f;

	private bool _isReachedBedRock;

	private Thing _thingInTheWay;

	private byte _processing;

	private const float MIN_VOXEL_DENSITY = 6000f;

	private static StringBuilder _sb = new StringBuilder();

	private Vector3[] offsets = new Vector3[4]
	{
		new Vector3(0f, 0f, 0f),
		new Vector3(-1f, 0f, 0f),
		new Vector3(-1f, 0f, -1f),
		new Vector3(0f, 0f, -1f)
	};

	private Vector3Int _lastMinePosition;

	private const float _drillHeadRatio = 0.25f;

	protected GameAudioEvent _gear1Audio;

	protected GameAudioEvent _gear3Audio;

	private GameAudioEvent _drillHeadAudio;

	private GameAudioEvent _drillShaftAudio;

	protected const float WOBBLE_FACTOR = 0.003f;

	private const float WOBBLE_SPEED = 10f;

	private DeepMinablesGenerationData _deepMinables;

	public const float STANDARD_RPM = 200f;

	protected float _currentProgress;

	public float OreSpawnTime { get; private set; }

	private Thing ThingInTheWay
	{
		get
		{
			return _thingInTheWay;
		}
		set
		{
			_thingInTheWay = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
		}
	}

	public byte Processing
	{
		get
		{
			return _processing;
		}
		set
		{
			if (value != Processing && NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 512;
			}
			_processing = value;
		}
	}

	protected override bool IsOperable => !ThingInTheWay;

	public override Slot ImportSlot => null;

	public override Slot ExportSlot
	{
		get
		{
			List<Slot> slots = Slots;
			if (slots == null || slots.Count <= 0)
			{
				return null;
			}
			return Slots[0];
		}
	}

	public virtual float ShaftWobbleFactor => 0.003f;

	public virtual float GearsWobbleFactor => 0f;

	public virtual float Rpm => 200f;

	public Vector3Int MinablesGenerationRange { get; }

	public Vector3 PreviousMinableRequestPosition { get; set; }

	public bool ShouldGenerate => true;

	public Vector3 GeneratePosition { get; set; }

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(20f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}

	public override void Awake()
	{
		base.Awake();
		_gear1Audio = GetAudioEvent(Animator.StringToHash("GearOne"));
		_gear3Audio = GetAudioEvent(Animator.StringToHash("GearThree"));
		_drillHeadAudio = GetAudioEvent(Animator.StringToHash("DrillHead"));
		_drillShaftAudio = GetAudioEvent(Animator.StringToHash("DrillShaft"));
	}

	public override CanConstructInfo CanConstruct()
	{
		if (AtmosphereHelper.GetWorldVolume(base.ThingTransformPosition) > new VolumeLitres(6000.0))
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementMustBeOnTerrain.DisplayString);
		}
		return base.CanConstruct();
	}

	protected override string GetInfoPanelOperationText()
	{
		_sb.Clear();
		string value = GameStrings.None;
		GeographicRegionData geographicRegionData = WorldSetting.Current.Data.GeographicRegionData;
		if (geographicRegionData != null)
		{
			value = geographicRegionData.DefaultRegionName;
			if (RegionManager.TryGetRegionAtWorldPosition(geographicRegionData.RegionSet, base.Position, out var region))
			{
				value = region.Name;
			}
		}
		_sb.AppendLine(GameStrings.RegionName.AsString(value.AsColor("lightblue")));
		foreach (DeepMinablesGenerationData deepMinablesDatum in WorldSetting.Current.Data.DeepMinablesData)
		{
			if (deepMinablesDatum.Evaluate(this))
			{
				deepMinablesDatum.ReagentAction.ToolTip(_sb, 0);
				break;
			}
		}
		_sb.AppendLine(Rpm.ToStringPrefix("RPM", "yellow"));
		float value2 = Mathf.Max(0f, _drillBit.transform.position.y);
		_sb.Append(GameStrings.DistanceToBedRock).Append(' ').Append(value2.ToStringPrefix("m", "yellow"));
		_sb.Newline();
		_sb.Append(GameStrings.Progress).Append(' ').Append(((float)(int)Processing).ToStringPercent("yellow"));
		return _sb.ToString();
	}

	public override void Start()
	{
		base.Start();
		if ((object)_spawnOrePrefab == null)
		{
			_spawnOrePrefab = Prefab.Find<DirtyOre>("ItemDirtyOre");
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is DeepMinerSaveData deepMinerSaveData)
		{
			deepMinerSaveData.IsReachedBedRock = _isReachedBedRock;
			deepMinerSaveData.DrillPosition = _drillBit.position;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new DeepMinerSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is DeepMinerSaveData deepMinerSaveData)
		{
			_isReachedBedRock = deepMinerSaveData.IsReachedBedRock;
			_drillBit.position = deepMinerSaveData.DrillPosition;
			UpdateDrillShaftScale();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteByte(Processing);
		writer.WriteVector3(_drillBit.position);
		Network.WritePackedId(writer, ThingInTheWay);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Processing = reader.ReadByte();
		_drillBit.position = reader.ReadVector3();
		Network.ReadPackedId(reader, out var referenceId);
		ThingInTheWay = Thing.Find<Thing>(referenceId);
		UpdateDrillShaftScale();
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteVector3(_drillBit.position);
			Network.WritePackedId(writer, ThingInTheWay);
		}
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteByte(Processing);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			_drillBit.position = reader.ReadVector3();
			UpdateDrillShaftScale();
			Network.ReadPackedId(reader, out var referenceId);
			ThingInTheWay = Thing.Find<Thing>(referenceId);
		}
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			Processing = reader.ReadByte();
		}
	}

	public override void AssessError()
	{
		if (GameManager.RunSimulation)
		{
			if (Error == 0 && !CanMine())
			{
				OnServer.Interact(base.InteractError, 1);
			}
			else if (Error == 1 && CanMine())
			{
				OnServer.Interact(base.InteractError, 0);
			}
		}
	}

	public bool CanMine()
	{
		if (!ThingInTheWay)
		{
			return _deepMinables != null;
		}
		return false;
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = base.InteractWith(interactable, interaction, doAction);
		if (interactable.Action == InteractableType.OnOff && OnOff)
		{
			if (!Powered)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
			}
			if (Error == 1 && (bool)ThingInTheWay)
			{
				return delayedActionInstance.Fail(GameStrings.DeepMinerSomethingInTheWay, ThingInTheWay.ToTooltip());
			}
		}
		return delayedActionInstance;
	}

	private void DrillDownAnimation()
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		AssessError();
		Vector3 vector = _drillBit.position;
		Vector3Int vector3Int = _drillBit.position.FloorToInt();
		if (_lastMinePosition != vector3Int)
		{
			for (int i = 0; i < offsets.Length; i++)
			{
				Vector3 vector2 = vector3Int + offsets[i];
				VoxelTerrain.SetDensityWorldSpace(vector2, 0f, RoomChangeSource.VoxelRemove, dirtyLods: false);
				Vein.GetVeinAtPosition(vector2)?.TryRemoveServer(vector2.FloorToInt());
			}
			_lastMinePosition = vector3Int;
			LodManager.Instance.DirtyLods(vector3Int, dirtyNeighbours: true);
		}
		_isReachedBedRock = vector.y <= 0f;
		vector += Vector3.down * (Time.deltaTime * 0.125f * RpmNormalised(Rpm));
		_drillBit.position = vector;
		UpdateDrillShaftScale();
		CheckThingInWay();
		base.NetworkUpdateFlags |= 256;
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		if (!_isReachedBedRock && OnOff && Powered && Error == 0)
		{
			DrillDownAnimation();
		}
		GeneratePosition = _drillBit.position;
		VoxelTerrain.GenerateMinables(this);
		if (!IsOccluded && base.IsStructureCompleted && !GameManager.IsBatchMode && !(Rpm < 1f))
		{
			float num = Rpm / 60f;
			Gear[] gears = _gears;
			for (int i = 0; i < gears.Length; i++)
			{
				Gear gear = gears[i];
				gear.GearTransform.Rotate(gear.RotateAxis * (360f * num * Time.deltaTime * gear.Ratio), Space.Self);
				float x = GearsWobbleFactor * Mathf.Sin(GameManager.GameTime * 10f * 1.1f);
				float z = GearsWobbleFactor * Mathf.Sin(GameManager.GameTime * 10f * 1.2f);
				float y = GearsWobbleFactor * Mathf.Sin(GameManager.GameTime * 10f * 1.3f);
				gear.GearTransform.localPosition = gear.localPosition + new Vector3(x, y, z);
			}
			_drillHead.Rotate(Vector3.up * (360f * num * Time.deltaTime * 0.25f), Space.Self);
			_drillShaft.Rotate(Vector3.up * (360f * num * Time.deltaTime * 0.25f), Space.Self);
		}
	}

	public override void UpdateAudio(float deltaTime)
	{
		base.UpdateAudio(deltaTime);
		UpdateGearAudio();
	}

	protected void UpdateGearAudio()
	{
		if (IsOccluded || Rpm < 1f || !base.IsStructureCompleted)
		{
			_gear1Audio?.UpdatePlayState(shouldPlay: false);
			_gear3Audio?.UpdatePlayState(shouldPlay: false);
			_drillShaftAudio?.UpdatePlayState(shouldPlay: false);
			_drillHeadAudio?.UpdatePlayState(shouldPlay: false);
			return;
		}
		float volumeMultiplier = RpmToAudioVolume();
		float num = RpmToAudioPitch();
		GameAudioEvent gear1Audio = _gear1Audio;
		if (gear1Audio != null && !gear1Audio.IsPlaying)
		{
			_gear1Audio.Trigger(volumeMultiplier, num);
		}
		gear1Audio = _gear3Audio;
		if (gear1Audio != null && !gear1Audio.IsPlaying)
		{
			_gear3Audio.Trigger(volumeMultiplier, num);
		}
		gear1Audio = _drillShaftAudio;
		if (gear1Audio != null && !gear1Audio.IsPlaying)
		{
			_drillShaftAudio.Trigger(volumeMultiplier, num);
		}
		gear1Audio = _drillHeadAudio;
		if (gear1Audio != null && !gear1Audio.IsPlaying)
		{
			_drillHeadAudio.Trigger(volumeMultiplier, num);
		}
		_gear1Audio?.SetVolumeAndPitch(volumeMultiplier, num);
		_gear3Audio?.SetVolumeAndPitch(volumeMultiplier, num);
		_drillShaftAudio?.SetVolumeAndPitch(volumeMultiplier, num);
		_drillHeadAudio?.SetVolumeAndPitch(volumeMultiplier, num);
	}

	protected float RpmToAudioPitch()
	{
		return RocketMath.MapToScaleClamp(0f, 1000f, 0.4f, 1.8f, Rpm);
	}

	protected float RpmToAudioVolume()
	{
		if (Rpm < 100f)
		{
			return RocketMath.MapToScaleClamp(1f, 50f, 0f, 1f, Rpm);
		}
		return RocketMath.MapToScaleClamp(50f, 1000f, 1f, 1.5f, Rpm);
	}

	private void UpdateDrillShaftScale()
	{
		Vector3 vector = _drillShaftBase.position;
		Vector3 b = _shaftEndPosition.position;
		float y = Vector3.Distance(vector, b);
		Vector3 localScale = _drillShaft.localScale;
		_drillShaft.localScale = new Vector3(localScale.x, y, localScale.z);
		float x = ShaftWobbleFactor * Mathf.Sin(GameManager.GameTime * 10f * 1.1f);
		float z = ShaftWobbleFactor * Mathf.Sin(GameManager.GameTime * 10f * 1.2f);
		_drillShaft.position = vector + new Vector3(x, 0f, z);
	}

	private bool CheckThingInWay()
	{
		if (base.IsBeingDestroyed)
		{
			ThingInTheWay = null;
			return false;
		}
		Structure structure = (Structure)(ThingInTheWay = base.GridController.Get<Structure>(_drillBit.position, StructureElement.Center));
		if (structure != null)
		{
			return structure.StructureCollisionType == CollisionType.BlockGrid;
		}
		return false;
	}

	protected override void OnServerExportTick(float deltaTime)
	{
		if (!OnOff || !Powered || !_isReachedBedRock || Error != 0 || !base.IsStructureCompleted)
		{
			return;
		}
		ProgressProcessing(deltaTime);
		if (IsSpawnTime() && !base.IsExportChuteBlocked)
		{
			ResetSpawnTime();
			SpawnOre();
			if (CanBeginExport)
			{
				OnServer.Interact(base.InteractExport, 1);
			}
		}
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		foreach (DeepMinablesGenerationData deepMinablesDatum in WorldSetting.Current.Data.DeepMinablesData)
		{
			if (deepMinablesDatum.Evaluate(this))
			{
				_deepMinables = deepMinablesDatum;
				break;
			}
		}
		ResetSpawnTime();
	}

	private bool IsSpawnTime()
	{
		return _currentProgress > OreSpawnTime;
	}

	private void ResetSpawnTime()
	{
		_currentProgress = 0f;
		OreSpawnTime = _deepMinables.GetTimeToMine();
	}

	private void SpawnOre()
	{
		DirtyOre dirtyOre = Thing.Create<DirtyOre>(_spawnOrePrefab, ExportSlot.Location);
		_deepMinables.SetValues(dirtyOre);
		dirtyOre.ParentSlot = null;
		OnServer.MoveToSlot(dirtyOre, ExportSlot);
	}

	public static float RpmNormalised(float rpm)
	{
		return rpm / 200f;
	}

	protected void ProgressProcessing(float deltaTime)
	{
		if (OnOff && Powered)
		{
			_currentProgress += deltaTime * RpmNormalised(Rpm);
			Processing = (byte)(Mathf.Clamp01(_currentProgress / OreSpawnTime) * 100f);
		}
	}
}
