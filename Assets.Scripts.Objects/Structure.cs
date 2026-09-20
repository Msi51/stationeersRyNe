using System;
using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Effects;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Structures;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Networks;
using Objects.Rockets;
using Rendering;
using Rooms;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects;

public class Structure : Thing
{
	public delegate void StructureEvent(Structure structure);

	public static Structure LastCreatedStructure;

	public static readonly List<Structure> AllStructurePrefabs = new List<Structure>();

	public Grid3 LocalGrid;

	public RocketData RocketData;

	[SerializeField]
	private bool alwaysCastShadows;

	private readonly DensePoolReference<Structure> _structuresDensePool = new DensePoolReference<Structure>(GridController.AllStructuresPool);

	private readonly DensePoolReference<Structure> _serverTickDensePool = new DensePoolReference<Structure>(GridController.AllServerTickStructures);

	public static bool IsCursorCreating;

	[FormerlySerializedAs("MaxPressureDelta")]
	[SerializeField]
	public float maxPressureDelta = -1f;

	[Header("Construction Settings")]
	public string BuildingTip = "";

	public List<BuildState> BuildStates;

	public List<BrokenBuildState> BrokenBuildStates;

	public ToolBasic RepairTools;

	public UpgradePrefab UpgradePrefab;

	[Tooltip("The grid that the object will use")]
	public float GridSize = 2f;

	[Tooltip("How much to offset the grid by")]
	public float GridOffset;

	[Header("Axis independent grid snapping")]
	[SerializeField]
	[Tooltip("Toggle to determine if the structure can snap to different grid sizes by axis")]
	private bool _useAxisIndependentGridSnapping;

	[SerializeField]
	[Tooltip("Grid size the structure will snap to based on axis")]
	private Vector3 _gridSize3d;

	[SerializeField]
	[Tooltip("Grid offset the structure will use based on axis")]
	private Vector3 _gridOffset3d;

	[Space(15f)]
	[Tooltip("Method of grid placement")]
	public PlacementSnap PlacementType;

	[Tooltip("Method of highlighting during placement")]
	public SelectionHighlightMethod SelectionDisplay;

	[Tooltip("Controls how collision detection within grids is handled")]
	public CollisionType StructureCollisionType = CollisionType.BlockCustom;

	[Tooltip("If set to true, allows mounting of things - such as wall lights. Typically only frames have this.")]
	public bool AllowMounting;

	[Tooltip("Uses the cell position as its position when registering to that given cell")]
	public bool UseCellPosition;

	[Tooltip("Ratio that bounds it to be used when calculating grid size")]
	public float BoundsGridRatio = 0.9f;

	[Tooltip("Shift the grids forward or back")]
	[Range(-5f, 5f)]
	public float BoundsGridShiftForward;

	[Tooltip("Shift the grids Left or Right")]
	[Range(-5f, 5f)]
	public float BoundsGridShiftSide;

	[Tooltip("Extra height added to bounds calculation")]
	public float BoundsGridExtraHeight;

	[Tooltip("Extra height added to bounds calculation")]
	public float BoundsGridAddHeight;

	[Tooltip("Extra bottom added to bounds calculation")]
	public float BoundsGridAddBottom;

	[Range(0f, 5f)]
	[Tooltip("Extra width added to bounds calculation on each side")]
	public float BoundsGridExtraWidth;

	[Range(0f, 5f)]
	[Tooltip("Extra width added to bounds calculation on each side")]
	public float BoundsGridExtraForward;

	[FormerlySerializedAs("BoundsGridExtraPositiveForward")]
	[Tooltip("Adjust forward for bounds calculation")]
	public float BoundsForward;

	[Tooltip("How make to scale the large grid compared to small grid for things that dual register")]
	[Range(0f, 5f)]
	public float DualRegisterGridScale = 1f;

	public GridBounds GridBounds = new GridBounds();

	public RotationAxis RotationAxis = RotationAxis.All;

	public AllowedRotations AllowedRotations = AllowedRotations.All;

	[Tooltip("If you don't want to use the base mesh filter on the Game object assign a different one here for navigation mapping")]
	public MeshFilter CustomNavMeshFilter;

	private WorldGrid[] _neighbourPositions = Array.Empty<WorldGrid>();

	[SerializeField]
	protected Vector3[] blockingGrids;

	[ByteArraySync]
	[ReadOnly]
	public Grid3 RegisteredLocalGrid;

	[ReadOnly]
	public Vector3 Up;

	[ReadOnly]
	public float Temperature;

	[ReadOnly]
	public List<SmallGrid> AttachedDevices = new List<SmallGrid>();

	public bool RenderUsingTerrainLod;

	public List<Structure> NeighborStructures = new List<Structure>();

	[ReadOnly]
	public Quaternion Direction;

	private int _currentBuildState;

	public bool HasLight;

	private static readonly Vector3 BuildStateSoundLocalPlayerOffset = new Vector3(-0.5f, 0.6f);

	[SerializeField]
	public StructureRenderMode structureRenderMode;

	private MaterialPropertyBlock _materialPropertyBlock;

	public List<Renderer> MPBRenderers = new List<Renderer>();

	private BuildState _oldBuildState;

	private static readonly string _brokenStateColorMaterialName = "Color";

	[HideInInspector]
	[ReadOnly]
	public bool IsUpdateOnServerTick;

	private Matrix4x4 _matrix4X4;

	public List<Grid3> ForceGridBounds = new List<Grid3>();

	public float BoundsExpand;

	private static readonly Quaternion RotateXtoZ = Quaternion.AngleAxis(-90f, Vector3.up);

	private static readonly Quaternion RotateZtoX = Quaternion.AngleAxis(90f, Vector3.up);

	private static readonly Quaternion RotateYtoZ = Quaternion.AngleAxis(90f, Vector3.right);

	private static readonly Quaternion RotateZtoY = Quaternion.AngleAxis(-90f, Vector3.right);

	public float GetCursorOffset => Bounds.extents.magnitude;

	public override bool OccludeAudio => CurrentBuildState.BlockAir;

	public PressurekPa MaxPressureDelta => new PressurekPa(maxPressureDelta);

	public virtual bool HasBrokenMesh
	{
		get
		{
			List<BrokenBuildState> brokenBuildStates = BrokenBuildStates;
			if (brokenBuildStates == null)
			{
				return false;
			}
			return brokenBuildStates.Count > 0;
		}
	}

	public bool HasSpawnedWreckage { get; set; }

	public override bool IsBroken
	{
		get
		{
			if (!base.IsBroken)
			{
				return CurrentBuildStateIndex < 0;
			}
			return true;
		}
	}

	public Grid3 OriginInWorldGridSpace => BlockingGrids[0];

	public Grid3[] BlockingGrids { get; set; }

	public float BuildPlacementTime
	{
		get
		{
			if (BuildStates.Count > 0)
			{
				return BuildStates[0].Tool.EntryTime;
			}
			return 0f;
		}
	}

	public float TemperatureRadiationFactor
	{
		get
		{
			if (CurrentBuildState == null)
			{
				return 0f;
			}
			return CurrentBuildState.TemperatureRadiationFactor;
		}
	}

	public bool IsDoor
	{
		get
		{
			if (!(GetType() == typeof(Door)))
			{
				return GetType().IsSubclassOf(typeof(Door));
			}
			return true;
		}
	}

	public bool IsStairs
	{
		get
		{
			if (!(GetType() == typeof(Stairs)))
			{
				return GetType().IsSubclassOf(typeof(Stairs));
			}
			return true;
		}
	}

	public bool IsStructureCompleted
	{
		get
		{
			if (CurrentBuildStateIndex != BuildStates.Count - 1)
			{
				if (CurrentBuildStateIndex > 0 && CurrentBuildStateIndex < BuildStates.Count)
				{
					return BuildStates[CurrentBuildStateIndex].CanManufacture;
				}
				return false;
			}
			return true;
		}
	}

	public virtual bool CanAirPass
	{
		get
		{
			if (CurrentBuildStateIndex >= 0)
			{
				return !CurrentBuildState.BlockAir;
			}
			return true;
		}
	}

	protected bool NeverAirPass
	{
		get
		{
			if (CurrentBuildStateIndex >= 0)
			{
				return CurrentBuildState.AlwaysBlockAir;
			}
			return false;
		}
	}

	public bool CanGravityPass
	{
		get
		{
			if (CurrentBuildStateIndex >= 0)
			{
				return !CurrentBuildState.BlockGravity;
			}
			return true;
		}
	}

	public virtual bool CanLightPass
	{
		get
		{
			if (CurrentBuildStateIndex >= 0)
			{
				return !CurrentBuildState.BlockLight;
			}
			return true;
		}
	}

	public int CurrentBuildStateIndex
	{
		get
		{
			return _currentBuildState;
		}
		set
		{
			if (value > BuildStates.Count - 1)
			{
				value = 0;
			}
			if (value < _currentBuildState)
			{
				UpdateSoundsOnBuildState(construct: false);
			}
			int currentBuildState = _currentBuildState;
			_currentBuildState = value;
			if (this.OnBuildState != null)
			{
				this.OnBuildState();
			}
			if (_currentBuildState > currentBuildState)
			{
				UpdateSoundsOnBuildState(construct: true);
			}
			base.GridController?.UpdateAirState(this);
			OnBuildStateUpdated(value, currentBuildState);
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 64;
			}
		}
	}

	public BuildState CurrentBuildState
	{
		get
		{
			if (IsBroken && CurrentBuildStateIndex < 0 && BrokenBuildStates.Count > 0)
			{
				int num = Math.Abs(CurrentBuildStateIndex + 1);
				DamageState.HealAll();
				if (num < BrokenBuildStates.Count)
				{
					return BrokenBuildStates[num].BuildState;
				}
				return BrokenBuildStates[0].BuildState;
			}
			if (CurrentBuildStateIndex < BuildStates.Count && CurrentBuildStateIndex >= 0)
			{
				return BuildStates[CurrentBuildStateIndex];
			}
			return null;
		}
	}

	private bool HasDeconstructTool
	{
		get
		{
			if (!IsBroken || BrokenBuildStates.Count <= 0)
			{
				return CurrentBuildState != null;
			}
			return true;
		}
	}

	public BuildState NextBuildState
	{
		get
		{
			if (CurrentBuildStateIndex + 1 >= BuildStates.Count || CurrentBuildStateIndex < 0)
			{
				return null;
			}
			return BuildStates[CurrentBuildStateIndex + 1];
		}
	}

	protected AtmosphericsController AtmosphericsController => base.GridController?.AtmosphericsController;

	public override bool IsBurnable => false;

	public static event StructureEvent OnAnyConstructed;

	public event Event OnBuildState;

	public override ShadowCastingMode GetShadowCastingMode()
	{
		if (!alwaysCastShadows)
		{
			return base.GetShadowCastingMode();
		}
		return ShadowCastingMode.On;
	}

	public override void SnapTransform(Vector3 transformPosition, Quaternion transformRotation)
	{
		LocalGrid = base.GridController.WorldToLocalGrid(CenterPosition);
	}

	public override bool OnAddToPool(object densePool, int slot)
	{
		if (_structuresDensePool.CanAddToPool(densePool))
		{
			return _structuresDensePool.AddToPool(densePool, slot);
		}
		if (_serverTickDensePool.CanAddToPool(densePool))
		{
			return _serverTickDensePool.AddToPool(densePool, slot);
		}
		return base.OnAddToPool(densePool, slot);
	}

	public override void OnRemoveFromPool(object densePool)
	{
		base.OnRemoveFromPool(densePool);
		_structuresDensePool.OnRemovedFrom(densePool);
		_serverTickDensePool.OnRemovedFrom(densePool);
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(64u, networkUpdateType))
		{
			writer.WriteSByte((sbyte)CurrentBuildStateIndex);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(64u, networkUpdateType))
		{
			int buildStateIndex = reader.ReadSByte();
			UpdateBuildStateAndVisualizer(buildStateIndex);
		}
	}

	protected override void WriteTransform(RocketBinaryWriter writer)
	{
		writer.WriteVector3(base.RegisteredPosition);
		writer.WriteQuaternion(base.RegisteredRotation);
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteByte((byte)CurrentBuildStateIndex);
		writer.WriteGrid3(RegisteredLocalGrid);
		writer.WriteQuaternion(Direction);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		byte buildStateIndex = reader.ReadByte();
		RegisteredLocalGrid = reader.ReadGrid3();
		Direction = reader.ReadQuaternion();
		UpdateBuildStateAndVisualizer(buildStateIndex);
	}

	public override void OnAssignedReference()
	{
		base.OnAssignedReference();
		Direction = ThingTransform.rotation;
		RegisteredLocalGrid = new Grid3(base.ThingTransformPosition);
		base.ThingTransformPosition = RegisteredLocalGrid.ToVector3();
		if (blockingGrids == null || blockingGrids.Length == 0)
		{
			BlockingGrids = new Grid3[1]
			{
				new Grid3(base.ThingTransformPosition)
			};
		}
		else
		{
			BlockingGrids = new Grid3[blockingGrids.Length];
			for (int i = 0; i < blockingGrids.Length; i++)
			{
				Vector3 worldPosition = blockingGrids[i].RotateAround(Vector3.zero, ThingTransform.rotation) + base.transform.position;
				BlockingGrids[i] = new Grid3(worldPosition);
			}
		}
		GridController.World.Register(this);
	}

	public virtual void DetatchFromGrid()
	{
		base.GridController.Detatch(this);
	}

	public virtual void AttachToGrid()
	{
		base.GridController.Attach(this);
	}

	public virtual void RebuildGridState()
	{
		Direction = ThingTransform.rotation;
		RegisteredLocalGrid = new Grid3(base.ThingTransformPosition);
		base.ThingTransformPosition = RegisteredLocalGrid.ToVector3();
		base.RegisteredPosition = base.ThingTransformPosition;
		base.RegisteredRotation = base.ThingTransformRotation;
		if (blockingGrids == null || blockingGrids.Length == 0)
		{
			BlockingGrids = new Grid3[1]
			{
				new Grid3(base.ThingTransformPosition)
			};
			return;
		}
		if (BlockingGrids.Length != blockingGrids.Length)
		{
			BlockingGrids = new Grid3[blockingGrids.Length];
		}
		for (int i = 0; i < blockingGrids.Length; i++)
		{
			Vector3 worldPosition = blockingGrids[i].RotateAround(Vector3.zero, ThingTransform.rotation) + base.transform.position;
			BlockingGrids[i] = new Grid3(worldPosition);
		}
	}

	public new static void ClearAll()
	{
		LastCreatedStructure = null;
	}

	public void SpawnWreckage()
	{
		if (this is IWreckage iWreckage && !HasSpawnedWreckage)
		{
			WreckageManager.SpawnWreckage(iWreckage);
		}
	}

	public override void SetPrefab(Thing prefab)
	{
		base.SetPrefab(prefab);
		if (prefab is Structure structure)
		{
			GridBounds = structure.GridBounds;
		}
	}

	public override void CheckBounds()
	{
		base.CheckBounds();
		if (GridBounds == null || !GridBounds.IsValid())
		{
			CachePrefabBounds();
		}
	}

	public override void PlayAnimatorSound(AnimationEvent animationEvent)
	{
		if (IsStructureCompleted)
		{
			base.PlayAnimatorSound(animationEvent);
		}
	}

	protected virtual void OnBuildStateUpdated(int newState, int previousState)
	{
	}

	public virtual UniTaskVoid OnHasSunlight()
	{
		return default(UniTaskVoid);
	}

	public virtual UniTaskVoid OnLostSunlight()
	{
		return default(UniTaskVoid);
	}

	protected virtual void UpdateSoundsOnBuildState(bool construct)
	{
		if (!XmlSaveLoad.IsReadyToPlayWorldAudio)
		{
			return;
		}
		if (construct)
		{
			if (CurrentBuildState?.Tool?.ToolEntry != null && CurrentBuildState.Tool.ToolEntry.FinishedConstructingSoundHash != 0)
			{
				if (CursorManager.CursorThing != null && CursorManager.CursorThing.AsStructure == this)
				{
					Thing.PlayPooledAudioSound(InventoryManager.ParentHuman.OrganBrain, CurrentBuildState.Tool.ToolEntry.FinishedConstructingSoundHash, BuildStateSoundLocalPlayerOffset);
				}
				else
				{
					PlayPooledAudioSound(CurrentBuildState.Tool.ToolEntry.FinishedConstructingSoundHash, Vector3.zero);
				}
			}
			if (CurrentBuildState?.Tool?.ToolEntry2 != null && CurrentBuildState.Tool.ToolEntry2.FinishedConstructingSoundHash != 0)
			{
				if (CursorManager.CursorThing != null && CursorManager.CursorThing.AsStructure == this)
				{
					Thing.PlayPooledAudioSound(InventoryManager.ParentHuman.OrganBrain, CurrentBuildState.Tool.ToolEntry2.FinishedConstructingSoundHash, BuildStateSoundLocalPlayerOffset);
				}
				else
				{
					PlayPooledAudioSound(CurrentBuildState.Tool.ToolEntry2.FinishedConstructingSoundHash, Vector3.zero);
				}
			}
		}
		else if (CurrentBuildState?.Tool.ToolExit != null && CurrentBuildState.Tool.ToolExit.FinishedDeconstructingSoundHash != 0)
		{
			if (CursorManager.CursorThing != null && CursorManager.CursorThing.AsStructure == this)
			{
				Thing.PlayPooledAudioSound(InventoryManager.ParentHuman.OrganBrain, CurrentBuildState.Tool.ToolExit.FinishedDeconstructingSoundHash, BuildStateSoundLocalPlayerOffset);
			}
			else
			{
				PlayPooledAudioSound(CurrentBuildState.Tool.ToolExit.FinishedDeconstructingSoundHash, Vector3.zero);
			}
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new StructureSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is StructureSaveData structureSaveData)
		{
			CurrentBuildStateIndex = structureSaveData.CurrentBuildState;
			HasSpawnedWreckage = structureSaveData.HasSpawnedWreckage;
			if (this is IRocketInternals rocketInternals)
			{
				RocketRecordData rocketRecord = structureSaveData.RocketRecord;
				if (rocketRecord != null && rocketRecord.RocketNetworkId != 0L)
				{
					RocketNetwork rocketNetwork = Referencable.Find<RocketNetwork>(rocketRecord.RocketNetworkId) ?? new RocketNetwork(rocketRecord.RocketNetworkId);
					RocketData = new RocketData(rocketNetwork, rocketRecord.Offset);
					rocketNetwork.AdoptFromLoad(rocketInternals);
				}
			}
		}
		UpdateStateVisualizer();
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is StructureSaveData structureSaveData)
		{
			structureSaveData.CurrentBuildState = CurrentBuildStateIndex;
			structureSaveData.HasSpawnedWreckage = HasSpawnedWreckage;
			structureSaveData.RegisteredWorldPosition = base.RegisteredPosition;
			structureSaveData.RegisteredWorldRotation = base.RegisteredRotation;
			if (RocketData?.Network != null)
			{
				structureSaveData.RocketRecord = new RocketRecordData
				{
					RocketNetworkId = RocketData.Network.ReferenceId,
					Offset = RocketData.Offset
				};
			}
		}
	}

	private bool ShowBuildTooltip()
	{
		bool flag = IsUpgradeState();
		if (Settings.CurrentData.ExtendedTooltips && (flag || !IsStructureCompleted) && NextBuildState != null)
		{
			if (!flag)
			{
				Slot activeHandSlot = InventoryManager.ActiveHandSlot;
				if (activeHandSlot != null)
				{
					return activeHandSlot.Occupant is IShowBuildStateTooltip;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	private bool ShowDeconstructTooltip()
	{
		if (Settings.CurrentData.ExtendedTooltips && (bool)InventoryManager.Parent)
		{
			Slot activeHandSlot = InventoryManager.ActiveHandSlot;
			if (activeHandSlot != null && activeHandSlot.Occupant is IShowBuildStateTooltip)
			{
				return HasDeconstructTool;
			}
		}
		return false;
	}

	private bool ShowRepairTooltip()
	{
		if (Settings.CurrentData.ExtendedTooltips && RepairTools.IsValid())
		{
			return DamageState.Total > 0f;
		}
		return false;
	}

	private bool IsUpgradeState()
	{
		ToolUseType? toolUseType = NextBuildState?.Tool?.ToolUseType;
		if (toolUseType.HasValue)
		{
			return toolUseType == ToolUseType.Upgrade;
		}
		return false;
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		bool flag = ShowBuildTooltip();
		bool flag2 = ShowDeconstructTooltip();
		bool flag3 = ShowRepairTooltip();
		if (DamageState.Total <= 0f && !flag && !flag2 && !flag3)
		{
			return base.GetPassiveTooltip(hitCollider);
		}
		PassiveTooltip passiveTooltip = new PassiveTooltip(true);
		passiveTooltip.Title = DisplayName;
		passiveTooltip.Extended = GetExtendedText().ToString();
		PassiveTooltip result = passiveTooltip;
		if (flag)
		{
			result.ConstructString = NextBuildState.Tool.GetToolsAsString();
		}
		if (IsBroken && BrokenBuildStates.Count > 0)
		{
			int index = Mathf.Clamp(Mathf.Abs(CurrentBuildStateIndex) - 1, 0, BrokenBuildStates.Count - 1);
			result.DeconstructString = BrokenBuildStates[index].BuildState.Tool.GetExitToolAsString();
		}
		else if (CurrentBuildState != null)
		{
			result.DeconstructString = CurrentBuildState.Tool.GetExitToolAsString();
		}
		if (flag3)
		{
			result.RepairString = RepairTools.GetRepairsAsString();
		}
		return result;
	}

	public virtual CanMountResult CanMountOnWall()
	{
		return CanMountResult.BasicValid;
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		CacheAllAnimatorInteractableVariables();
		base.GridController.UpdateAirState(this);
	}

	protected async void AddRocketRenderer()
	{
		await UniTask.WaitUntil(() => RocketRendererManager.instance);
		if (GameManager.IsBatchMode)
		{
			return;
		}
		foreach (ThingRenderer renderer in Renderers)
		{
			if (renderer?.sharedMaterials != null && renderer.sharedMaterials[0].shader == RocketRendererManager.instance.StandardInstancedShader && !IsCursor && !IsCursorCreating)
			{
				RocketRendererInstance rocketRendererInstance = renderer.GetRendererGameObject().AddComponent<RocketRendererInstance>();
				rocketRendererInstance.Materials = renderer.sharedMaterials;
				renderer.RocketRenderer = rocketRendererInstance;
				RocketRenderers.Add(rocketRendererInstance);
			}
		}
	}

	private void UpdateAllMaterialPropertyBlocks()
	{
		if (MPBRenderers.Count == 0 || MPBRenderers[0] == null || IsCursor || IsCursorCreating)
		{
			return;
		}
		MPBRenderers[0].GetPropertyBlock(_materialPropertyBlock);
		_materialPropertyBlock.SetFloat(Thing.DiffuseIndexPropertyID, DiffuseIndex);
		_materialPropertyBlock.SetColor(Thing.EMISSION_COLOR, EmissionColor);
		_materialPropertyBlock.SetFloat(Thing.SmoothnessIndexPropertyID, SmoothnessIndex);
		foreach (Renderer mPBRenderer in MPBRenderers)
		{
			mPBRenderer.SetPropertyBlock(_materialPropertyBlock);
		}
	}

	public override void SetCustomColor(int index, bool emissive = false)
	{
		if (structureRenderMode == StructureRenderMode.Standard)
		{
			base.SetCustomColor(index, emissive);
			return;
		}
		throw new NotImplementedException();
	}

	public override void Awake()
	{
		base.Awake();
		if (structureRenderMode == StructureRenderMode.Batched)
		{
			_materialPropertyBlock = new MaterialPropertyBlock();
			MeshRenderer component = GetComponent<MeshRenderer>();
			if ((object)component != null)
			{
				UnityEngine.Object.Destroy(component);
			}
		}
		if (BuildStates.Count > 0 && BuildStates[0].Visualizer == null)
		{
			BuildStates[0].Visualizer = GetComponent<MeshRenderer>();
		}
		if (!IsCursorCreating && !IsCursor)
		{
			int colorIndex = GameManager.GetColorIndex(PaintableMaterial);
			foreach (BuildState buildState in BuildStates)
			{
				DrawData initialDrawData = buildState.InitialDrawData;
				ThingRenderer thingRenderer = new ThingRenderer(this, buildState.InitialDrawData);
				for (int i = 0; i < initialDrawData.materials.Length; i++)
				{
					Material material = initialDrawData.materials[i];
					if (material == PaintableMaterial || material.mainTexture is Texture2DArray)
					{
						_customMaterials.Add(new CustomColorMapping(thingRenderer, i, colorIndex));
					}
				}
				if (thingRenderer.HasRenderer())
				{
					Renderers.Add(thingRenderer);
					buildState.RendererInstance = thingRenderer;
				}
			}
		}
		foreach (ThingRenderer renderer in Renderers)
		{
			foreach (BuildState buildState2 in BuildStates)
			{
				PopulateBuildStateRenderers(buildState2, renderer);
			}
			foreach (BrokenBuildState brokenBuildState in BrokenBuildStates)
			{
				PopulateBuildStateRenderers(brokenBuildState.BuildState, renderer);
			}
		}
		for (int j = 0; j < BrokenBuildStates.Count; j++)
		{
			if (BuildStates.Count > 0 && BuildStates[0].Tool != null && !(BuildStates[0].Tool.ToolEntry == null) && Recycler.RecycleRecipes.TryGetValue(BuildStates[0].Tool.ToolEntry.PrefabHash, out var value))
			{
				BrokenBuildState value2 = BrokenBuildStates[j];
				value2.TotalReagentMixture = value;
				BrokenBuildStates[j] = value2;
			}
		}
		UpdateStateVisualizer(visualOnly: true);
	}

	private static void PopulateBuildStateRenderers(BuildState buildState, ThingRenderer thingRenderer)
	{
		if (!(buildState.Visualizer == null) && thingRenderer.IsCurrentRenderer(buildState.Visualizer))
		{
			buildState.RendererInstance = thingRenderer;
		}
	}

	public override bool CanCacheRenderer(Renderer selectedRenderer)
	{
		if (!selectedRenderer.GetComponent<TextMesh>())
		{
			return base.CanCacheRenderer(selectedRenderer);
		}
		return false;
	}

	public void UpdateBuildStateAndVisualizer(int buildStateIndex, int numberOfParticles = 0)
	{
		CurrentBuildStateIndex = buildStateIndex;
		UpdateStateVisualizer();
		if (numberOfParticles > 0)
		{
			EffectManager.CreateDeconstructionEffect(this, numberOfParticles);
		}
	}

	public virtual void OnStructureBroken()
	{
		base.IsBurning = false;
		if (this is IWreckage wreckage)
		{
			wreckage.SpawnWreckage();
		}
	}

	public virtual void UpdateStateVisualizer(bool visualOnly = false)
	{
		if (IsCursor)
		{
			return;
		}
		if (structureRenderMode == StructureRenderMode.Batched && _oldBuildState != null)
		{
			RenderBatch.Deregister(_oldBuildState.RendererInstance);
		}
		int num = Mathf.Abs(CurrentBuildStateIndex + 1);
		if (CurrentBuildStateIndex < 0 && (IsBroken || num < BrokenBuildStates.Count))
		{
			foreach (Transform item2 in base.transform)
			{
				if (!(item2 == base.transform))
				{
					item2.gameObject.SetActive(value: false);
				}
			}
			foreach (BuildState buildState2 in BuildStates)
			{
				if (buildState2 != null && !(buildState2.Visualizer == null))
				{
					buildState2.Visualizer.enabled = false;
				}
			}
			foreach (ThingRenderer renderer in Renderers)
			{
				if (renderer != null)
				{
					renderer.Enabled = false;
				}
			}
			MeshRenderer component = GetComponent<MeshRenderer>();
			if (component != null)
			{
				component.enabled = false;
			}
			IsOccluded = true;
			base.AllowInteraction = false;
			if (BrokenBuildStates.Count > 0)
			{
				foreach (GameObject linkedGameObject in BrokenBuildStates[num].BuildState.LinkedGameObjects)
				{
					if (linkedGameObject == null)
					{
						Debug.LogError($"{DisplayName} has a null LinkedGameObject in BrokenBuildState_{num}");
						continue;
					}
					linkedGameObject.SetActive(value: true);
					Renderer component2 = linkedGameObject.GetComponent<Renderer>();
					if (component2 == null)
					{
						continue;
					}
					Material[] sharedMaterials = component2.sharedMaterials;
					for (int i = 0; i < sharedMaterials.Length; i++)
					{
						if (sharedMaterials[i] == null)
						{
							Debug.LogError($"{DisplayName} has a null material in BrokenBuildState_{num}");
						}
						else if (sharedMaterials[i].name.ToLower().Contains(_brokenStateColorMaterialName.ToLower()))
						{
							sharedMaterials[i] = CustomColor.Normal;
						}
					}
					component2.sharedMaterials = sharedMaterials;
					component2.enabled = true;
				}
				foreach (Collider collider in BrokenBuildStates[num].BuildState.Colliders)
				{
					if (!(collider == null))
					{
						collider.enabled = true;
					}
				}
				BrokenBuildState value = BrokenBuildStates[num];
				if (!value.HasBroken)
				{
					value.HasBroken = true;
					BrokenBuildStates[num] = value;
				}
			}
		}
		else
		{
			List<GameObject> list = new List<GameObject>();
			for (int j = 0; j < BuildStates.Count; j++)
			{
				BuildState buildState = BuildStates[j];
				bool flag = j == CurrentBuildStateIndex;
				foreach (GameObject linkedGameObject2 in buildState.LinkedGameObjects)
				{
					bool flag2 = list.Contains(linkedGameObject2) && !flag;
					if ((bool)linkedGameObject2 && !flag2)
					{
						linkedGameObject2.SetActive(flag);
						list.Add(linkedGameObject2);
					}
				}
				if ((buildState.RendererInstance == null || !buildState.RendererInstance.HasRenderer()) && !buildState.InitialDrawData.IsValid())
				{
					continue;
				}
				bool flag3 = false;
				switch (buildState.RenderMode)
				{
				case BuildStateRenderMode.OnMineAndPreviousStates:
					flag3 = j <= CurrentBuildStateIndex;
					break;
				case BuildStateRenderMode.OnMyState:
					flag3 = j == CurrentBuildStateIndex;
					break;
				}
				if (flag3)
				{
					SimpleFabricatorBase simpleFabricatorBase = this as SimpleFabricatorBase;
					if ((bool)simpleFabricatorBase)
					{
						simpleFabricatorBase.NeedsValidate = true;
					}
				}
				if (j == CurrentBuildStateIndex && buildState.Interactables != null && buildState.Interactables.Count > 0)
				{
					Interactables = buildState.Interactables;
					if (_interactableColliderLookup == null)
					{
						_interactableColliderLookup = new Dictionary<Collider, Interactable>();
					}
					_interactableColliderLookup.Clear();
					foreach (Interactable interactable in Interactables)
					{
						interactable.Initialize();
						interactable.SetState();
						if ((bool)interactable.Collider && !_interactableColliderLookup.ContainsKey(interactable.Collider))
						{
							_interactableColliderLookup.Add(interactable.Collider, interactable);
						}
					}
					if (Interactables != null)
					{
						foreach (Interactable interactable2 in Interactables)
						{
							interactable2.CacheBounds();
						}
					}
					CacheStates(cacheInteractables: true);
				}
				buildState.RendererInstance.Enabled = flag3;
				if (!WorldParticleEffect.GridsInUse.Contains(base.WorldGrid))
				{
					WorldParticleEffect.GridsInUse.Add(base.WorldGrid);
				}
				WorldGrid[] neighbourPositions = _neighbourPositions;
				foreach (WorldGrid item in neighbourPositions)
				{
					if (!WorldParticleEffect.GridsInUse.Contains(item))
					{
						WorldParticleEffect.GridsInUse.Add(item);
					}
				}
				foreach (Collider collider2 in BuildStates[j].Colliders)
				{
					if (!(collider2 == null))
					{
						collider2.enabled = flag;
					}
				}
			}
		}
		if (!visualOnly)
		{
			if (StructureCollisionType == CollisionType.BlockGrid || StructureCollisionType == CollisionType.BlockFace)
			{
				WorldChangeChecks();
			}
			if (!IsOccluded)
			{
				OnStartRender();
			}
			else
			{
				OnStopRender();
			}
		}
		BuildState currentBuildState = CurrentBuildState;
		if (currentBuildState == null)
		{
			return;
		}
		if (currentBuildState.RendererInstance != null)
		{
			currentBuildState.RendererInstance.Enabled = true;
			if (currentBuildState.StateMeshes.Count > 0)
			{
				currentBuildState.RendererInstance.SharedMesh = currentBuildState.StateMeshes.Pick();
			}
			foreach (Collider collider3 in currentBuildState.Colliders)
			{
				if ((bool)collider3)
				{
					collider3.enabled = true;
				}
			}
		}
		if (structureRenderMode == StructureRenderMode.Batched)
		{
			if (CurrentBuildState != null)
			{
				RenderBatch.Register(CurrentBuildState.RendererInstance);
			}
			_oldBuildState = CurrentBuildState;
		}
	}

	public virtual Grid3 GetLocalGrid()
	{
		if (!IsCursor)
		{
			return base.GridController.WorldToLocalGrid(CenterPosition, GridSize, GridOffset);
		}
		base.Position = base.ThingTransformPosition;
		Rotation = base.ThingTransformRotation;
		return base.GridController.WorldToLocalGrid(CenterPosition, GridSize, GridOffset);
	}

	public Vector3 GetWorldGrid(Vector3 position)
	{
		if (_useAxisIndependentGridSnapping)
		{
			return base.GridController.ClampWorld(position, _gridSize3d, _gridOffset3d);
		}
		return base.GridController.ClampWorld(position, GridSize, GridOffset);
	}

	protected Room GetRoom()
	{
		return base.GridController?.RoomController?.GetRoom(GridPosition);
	}

	protected Vector3 GetGrid()
	{
		switch (PlacementType)
		{
		case PlacementSnap.Grid:
			return base.ThingTransformPosition.GridCenter(GridSize, GridOffset);
		case PlacementSnap.Face:
		case PlacementSnap.FaceMount:
			return (base.ThingTransformPosition + ThingTransform.forward * 0.1f).GridCenter(GridSize, GridOffset);
		default:
			return base.ThingTransformPosition.GridCenter();
		}
	}

	public Vector3 GetGridPosition()
	{
		return base.ThingTransformPosition;
	}

	protected virtual CanConstructInfo CanConstructCell(Cell cell, Vector3 position)
	{
		if (cell == null)
		{
			return CanConstructInfo.ValidPlacement;
		}
		if (StructureCollisionType == CollisionType.BlockGrid)
		{
			Structure first = cell.GetFirst();
			if ((object)first != null)
			{
				return CanConstructInfo.InvalidPlacement(GameStrings.PlacementBlockedByStructure.AsString(first.DisplayName));
			}
		}
		foreach (Structure allStructure in cell.AllStructures)
		{
			switch (allStructure.StructureCollisionType)
			{
			case CollisionType.BlockGrid:
				return new CanConstructInfo(canConstruct: false, GameStrings.GridBlockedByStructure.AsString(allStructure.DisplayName));
			case CollisionType.BlockFace:
				if (Vector3.SqrMagnitude(allStructure.GetGridPosition() - position) < 0.01f)
				{
					return new CanConstructInfo(canConstruct: false, GameStrings.FaceBlockedByStructure.AsString(allStructure.DisplayName));
				}
				break;
			}
		}
		return CanConstructInfo.ValidPlacement;
	}

	public virtual CanConstructInfo CanConstruct()
	{
		Cell cell;
		if (PlacementType == PlacementSnap.Grid)
		{
			Span<Grid3> span = stackalloc Grid3[GridBounds._grids.Length];
			GridBounds.GetLocalGrids(base.ThingTransformPosition, base.ThingTransformRotation, span);
			Span<Grid3> span2 = span;
			for (int i = 0; i < span2.Length; i++)
			{
				Grid3 localGrid = span2[i];
				cell = base.GridController.GetCell(localGrid);
				CanConstructInfo result = CanConstructCell(cell, base.ThingTransformPosition);
				if (!result.CanConstruct)
				{
					return result;
				}
			}
			return CanConstructInfo.ValidPlacement;
		}
		cell = base.GridController.GetCell(GetLocalGrid());
		return CanConstructCell(cell, base.ThingTransformPosition);
	}

	private DelayedActionInstance SetNewCreativeSpawnItem(ref DelayedActionInstance contextMessage)
	{
		Constructor constructor = BuildStates[0].Tool.ToolEntry as Constructor;
		MultiConstructor multiConstructor = BuildStates[0].Tool.ToolEntry as MultiConstructor;
		if (!constructor && !multiConstructor && BuildStates[0].Tool.ToolEntry2 != null)
		{
			constructor = BuildStates[0].Tool.ToolEntry2 as Constructor;
			multiConstructor = BuildStates[0].Tool.ToolEntry2 as MultiConstructor;
		}
		if (!constructor && !multiConstructor)
		{
			return contextMessage.Fail();
		}
		if ((object)constructor != null)
		{
			InventoryManager.SpawnPrefab = Prefab.Find<DynamicThing>(constructor.PrefabHash);
		}
		if ((object)multiConstructor != null)
		{
			InventoryManager.SpawnPrefab = Prefab.Find<DynamicThing>(multiConstructor.PrefabHash);
		}
		return contextMessage;
	}

	public override void PrintDebugInfo(bool verbose = false)
	{
		base.PrintDebugInfo(verbose);
		ConsoleWindow.Print($"BuildState: {CurrentBuildStateIndex}");
		ConsoleWindow.Print($"Is Broken: {IsBroken}");
	}

	private DelayedActionInstance InoperableToolResult(DelayedActionInstance contextMessage, Tool tool, bool doAction)
	{
		contextMessage.IsDisabled = true;
		PowerTool powerTool = tool as PowerTool;
		if ((bool)powerTool && !powerTool.Battery)
		{
			return contextMessage.Fail(GameStrings.ToolDoesNotHaveAnythingInSlot, tool.ToTooltip(), powerTool.BatterySlot.ToTooltip());
		}
		if ((bool)powerTool && (bool)powerTool.Battery && powerTool.Battery.IsEmpty)
		{
			return contextMessage.Fail(GameStrings.ToolDoesNotHaveEnoughCharge, tool.ToTooltip(), powerTool.Battery.ToTooltip());
		}
		contextMessage.AppendStateMessage(GameStrings.ToolCanNotCompleteTask, tool.ToTooltip());
		if (!doAction)
		{
			return contextMessage;
		}
		return null;
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		DynamicThing sourceItem = attack.SourceItem;
		if (!sourceItem)
		{
			return base.AttackWith(attack, doAction);
		}
		AuthoringTool authoringTool = sourceItem as AuthoringTool;
		if ((bool)authoringTool)
		{
			DelayedActionInstance contextMessage = new DelayedActionInstance
			{
				Duration = 0.5f,
				ActionMessage = ((CurrentBuildStateIndex == BuildStates.Count - 1) ? ActionStrings.Deconstruct : ActionStrings.Construct)
			};
			if (attack.IsDestroy)
			{
				contextMessage.ActionMessage = "Delete";
				AddReferenceIDToContextualMessage(ref contextMessage);
				if (KeyManager.GetButtonDown(KeyMap.PrimaryAction))
				{
					bool num = StructureCollisionType == CollisionType.BlockGrid && CurrentBuildState.BlockAir;
					WorldGrid worldGrid = base.WorldGrid;
					Delete(attack.SourceItem);
					if (num)
					{
						AtmosphericEventInstance.StructureReleaseGrid(worldGrid);
					}
				}
				return contextMessage;
			}
			if (attack.IsCopy)
			{
				contextMessage.ActionMessage = "Copy";
				AddReferenceIDToContextualMessage(ref contextMessage);
				if (!doAction)
				{
					return contextMessage;
				}
				if (BuildStates.Count > 0 && (bool)BuildStates[0].Tool.ToolEntry)
				{
					SetNewCreativeSpawnItem(ref contextMessage);
				}
				return contextMessage;
			}
		}
		Item item = sourceItem as Item;
		Tool tool = item as Tool;
		float value = ((tool != null) ? tool.getToolSpeed() : 1f);
		value = Mathf.Clamp(value, 0.2f, 5f);
		if (IsBroken && BrokenBuildStates.Count > 0)
		{
			int index = Mathf.Clamp(Mathf.Abs(CurrentBuildStateIndex) - 1, 0, BrokenBuildStates.Count - 1);
			BrokenBuildState brokenBuildState = BrokenBuildStates[index];
			BuildState buildState = brokenBuildState.BuildState;
			if (brokenBuildState.HasBroken && buildState.Tool.ToolExit != null && (bool)buildState.Tool.ToolExit && buildState.Tool.IsToolExit(item))
			{
				DelayedActionInstance delayedActionInstance = new DelayedActionInstance
				{
					Duration = buildState.Tool.ExitTime,
					ActionMessage = ActionStrings.Deconstruct
				};
				Tool tool2 = sourceItem as Tool;
				if ((bool)tool2 && !tool2.IsOperable)
				{
					return InoperableToolResult(delayedActionInstance, tool2, doAction);
				}
				if (!doAction)
				{
					return delayedActionInstance;
				}
				if ((bool)tool2 && !tool2.OnUseItem(buildState.Tool.ExitQuantity, this))
				{
					return null;
				}
				ConstructionEventInstance eventInstance = new ConstructionEventInstance
				{
					Parent = this,
					Position = attack.Position,
					Rotation = ThingTransform.rotation,
					SteamId = base.OwnerClientId,
					OtherHandSlot = attack.OtherHand
				};
				StructureDestroyed(eventInstance, destroyedFromDamage: true);
				OnServer.Destroy(this);
				return delayedActionInstance;
			}
		}
		if (!base.Indestructable && RepairTools.IsValid() && DamageState.Total > 0f)
		{
			ToolBasic repairTools = RepairTools;
			if (repairTools != null && (bool)repairTools.ToolEntry && repairTools.IsToolEntry(item))
			{
				Item toolEntry = repairTools.ToolEntry2;
				DelayedActionInstance contextMessage2 = new DelayedActionInstance
				{
					Duration = 0.5f,
					ActionMessage = ActionStrings.Repair,
					ActionSoundHash = ((item != null) ? item.ConstructingSoundHash : (-1)),
					ActionCompleteSoundHash = ((item != null) ? item.FinishedConstructingSoundHash : (-1))
				};
				contextMessage2 = HandleToolUse(contextMessage2, sourceItem, attack, toolEntry, repairTools, doAction);
				if (contextMessage2 == null)
				{
					return null;
				}
				if (!doAction)
				{
					return contextMessage2;
				}
				DamageState.HealAll();
				return contextMessage2;
			}
		}
		if (UpgradePrefab?.Prefab != null && UpgradePrefab.UpgradeTools.IsValid())
		{
			ToolBasic upgradeTools = UpgradePrefab.UpgradeTools;
			if (upgradeTools != null && (bool)upgradeTools.ToolEntry && upgradeTools.IsToolEntry(item))
			{
				Item toolEntry2 = upgradeTools.ToolEntry2;
				DelayedActionInstance contextMessage3 = new DelayedActionInstance
				{
					Duration = 0.5f,
					ActionMessage = GameStrings.UpgradeConstructionAction.AsString(UpgradePrefab.Prefab.DisplayName),
					ActionSoundHash = ((item != null) ? item.ConstructingSoundHash : (-1)),
					ActionCompleteSoundHash = ((item != null) ? item.FinishedConstructingSoundHash : (-1))
				};
				contextMessage3 = HandleToolUse(contextMessage3, sourceItem, attack, toolEntry2, upgradeTools, doAction);
				if (contextMessage3 == null)
				{
					return null;
				}
				if (!doAction)
				{
					return contextMessage3;
				}
				ConstructionEventInstance eventInstance2 = new ConstructionEventInstance
				{
					Parent = this,
					Position = attack.Position,
					Rotation = ThingTransform.rotation,
					SteamId = base.OwnerClientId,
					OtherHandSlot = attack.OtherHand
				};
				StructureDestroyed(eventInstance2, destroyedFromDamage: true);
				if (GameManager.RunSimulation)
				{
					CreateStructureInstance instance = new CreateStructureInstance(UpgradePrefab.Prefab, this);
					UpgradeStructureServer(instance);
				}
				return contextMessage3;
			}
		}
		if (!base.Indestructable && BuildStates.Count > 0)
		{
			BuildState nextBuildState = NextBuildState;
			if ((bool)authoringTool && BuildStates.Count > 1)
			{
				bool flag = CurrentBuildStateIndex + 1 >= BuildStates.Count;
				float duration = (flag ? 0.8f : 0.3f);
				DelayedActionInstance result = new DelayedActionInstance
				{
					Duration = duration,
					ActionMessage = ((CurrentBuildStateIndex == BuildStates.Count - 1) ? ActionStrings.Deconstruct : ActionStrings.Construct),
					ExtendedMessage = "Reference ID: " + base.ReferenceId
				};
				if (!doAction)
				{
					return result;
				}
				if (flag)
				{
					CurrentBuildStateIndex = 0;
				}
				else
				{
					CurrentBuildStateIndex++;
				}
				UpdateStateVisualizer();
				return result;
			}
			if (nextBuildState != null && (bool)nextBuildState.Tool.ToolEntry && nextBuildState.Tool.IsToolEntry(item))
			{
				Item toolEntry3 = nextBuildState.Tool.ToolEntry2;
				DelayedActionInstance delayedActionInstance2 = new DelayedActionInstance
				{
					Duration = nextBuildState.Tool.EntryTime / value,
					ActionMessage = ActionStrings.Construct,
					ActionSoundHash = ((item != null) ? item.ConstructingSoundHash : (-1)),
					ActionCompleteSoundHash = ((item != null) ? item.FinishedConstructingSoundHash : (-1))
				};
				if (DamageState.Total > 0f)
				{
					return delayedActionInstance2.Fail(GameStrings.CannotConstructWhenDamaged, ToString());
				}
				delayedActionInstance2 = HandleToolUse(delayedActionInstance2, sourceItem, attack, toolEntry3, nextBuildState.Tool, doAction);
				if (delayedActionInstance2 == null)
				{
					return null;
				}
				if (delayedActionInstance2.IsDisabled)
				{
					return delayedActionInstance2;
				}
				if (!doAction)
				{
					return delayedActionInstance2;
				}
				CurrentBuildStateIndex++;
				UpdateStateVisualizer();
			}
			else if (CurrentBuildStateIndex >= 0 && CurrentBuildStateIndex < BuildStates.Count)
			{
				BuildState buildState2 = BuildStates[CurrentBuildStateIndex];
				DelayedActionInstance delayedActionInstance3 = new DelayedActionInstance
				{
					Duration = buildState2.Tool.ExitTime / value,
					ActionMessage = ActionStrings.Deconstruct
				};
				Tool tool3 = sourceItem as Tool;
				if ((bool)buildState2.Tool.ToolExit && buildState2.Tool.IsToolExit(item))
				{
					BuildState buildState3 = BuildStates[CurrentBuildStateIndex];
					if ((bool)tool3 && !tool3.IsOperable)
					{
						return InoperableToolResult(delayedActionInstance3, tool3, doAction);
					}
					CanConstructInfo canConstructInfo = CanDeconstruct();
					if (tool3 != null && !canConstructInfo.CanConstruct)
					{
						return delayedActionInstance3.Fail(canConstructInfo.ErrorMessage);
					}
					if (!doAction)
					{
						return delayedActionInstance3;
					}
					if (CurrentBuildStateIndex > 0 && GameManager.RunSimulation)
					{
						ConstructionEventInstance eventInstance3 = new ConstructionEventInstance
						{
							Parent = this,
							Position = attack.Position,
							Rotation = ThingTransform.rotation,
							SteamId = base.OwnerClientId,
							OtherHandSlot = attack.OtherHand
						};
						buildState3.Tool.Deconstruct(eventInstance3);
					}
					if ((bool)tool3 && !tool3.OnUseItem(buildState2.Tool.ExitQuantity, this))
					{
						return null;
					}
					CurrentBuildStateIndex--;
					if (CurrentBuildStateIndex < 0)
					{
						ConstructionEventInstance eventInstance4 = new ConstructionEventInstance
						{
							Parent = this,
							Position = attack.Position,
							Rotation = ThingTransform.rotation,
							SteamId = base.OwnerClientId,
							OtherHandSlot = attack.OtherHand
						};
						StructureDestroyed(eventInstance4);
						OnServer.Destroy(this);
						return delayedActionInstance3;
					}
					UpdateStateVisualizer();
				}
			}
			else if (CurrentBuildStateIndex < 0 && BrokenBuildStates.Count > 0)
			{
				int num2 = Mathf.Abs(CurrentBuildStateIndex);
				int index2 = ((num2 < BrokenBuildStates.Count) ? num2 : (BrokenBuildStates.Count - 1));
				BrokenBuildState brokenBuildState2 = BrokenBuildStates[index2];
				DelayedActionInstance delayedActionInstance4 = new DelayedActionInstance
				{
					Duration = brokenBuildState2.BuildState.Tool.ExitTime,
					ActionMessage = ActionStrings.Deconstruct
				};
				Tool tool4 = sourceItem as Tool;
				if (!(tool4 is IConstructor))
				{
					return null;
				}
				if ((bool)tool4 && !tool4.IsOperable)
				{
					return InoperableToolResult(delayedActionInstance4, tool4, doAction);
				}
				ConstructionEventInstance eventInstance5 = new ConstructionEventInstance
				{
					Parent = this,
					Position = attack.Position,
					Rotation = ThingTransform.rotation,
					SteamId = base.OwnerClientId,
					OtherHandSlot = attack.OtherHand
				};
				if (!doAction)
				{
					return delayedActionInstance4;
				}
				if ((bool)tool4 && !tool4.OnUseItem(brokenBuildState2.BuildState.Tool.ExitQuantity, this))
				{
					return null;
				}
				StructureDestroyed(eventInstance5, destroyedFromDamage: true);
				OnServer.Destroy(this);
				return delayedActionInstance4;
			}
		}
		return base.AttackWith(attack, doAction);
	}

	protected virtual void UpgradeStructureServer(CreateStructureInstance instance)
	{
		OnServer.Destroy(this);
		if ((bool)PaintableMaterial && (bool)CustomColor.Normal)
		{
			instance.CustomColor = CustomColor.Index;
		}
		Constructor.SpawnConstruct(instance);
	}

	private DelayedActionInstance HandleToolUse(DelayedActionInstance contextMessage, DynamicThing weapon, Attack attack, Item secondHandItem, ToolBasic tools, bool doAction)
	{
		Stackable stackable = attack.OtherHand.Occupant as Stackable;
		if ((bool)secondHandItem)
		{
			if (attack.OtherHand == null || attack.OtherHand.Occupant == null || !tools.IsToolEntry2(attack.OtherHand.Occupant as Item))
			{
				contextMessage.AppendStateMessage(GameStrings.StructureYouRequireToCompleteTask, secondHandItem.ToTooltip(), attack.OtherHand.ToTooltip());
				contextMessage.IsDisabled = true;
				return contextMessage;
			}
			if ((bool)stackable && !stackable.CanUseStack(tools.EntryQuantity2, ref contextMessage))
			{
				return contextMessage;
			}
		}
		Stackable stackable2 = weapon as Stackable;
		if ((bool)stackable2 && !stackable2.CanUseStack(tools.EntryQuantity, ref contextMessage))
		{
			return contextMessage;
		}
		Tool tool = weapon as Tool;
		if ((bool)tool && !tool.CanUseTool(ref contextMessage))
		{
			return contextMessage;
		}
		if (!doAction)
		{
			return contextMessage;
		}
		if (tools.ToolEntry2 != null && tools.ToolEntry2 is Stackable && (stackable == null || !stackable.OnUseItem(tools.EntryQuantity2, this)))
		{
			return null;
		}
		if ((bool)secondHandItem && !(secondHandItem is Stackable) && (bool)attack.OtherHandOccupant() && tools.IsToolEntry2(attack.OtherHandOccupant() as Item) && tools.EntryQuantity2 > 0 && GameManager.RunSimulation)
		{
			OnServer.Destroy(attack.OtherHandOccupant());
		}
		if ((bool)stackable2 && !stackable2.OnUseItem(tools.EntryQuantity, this))
		{
			return null;
		}
		if ((bool)tool && !tool.OnUseItem(tools.EntryQuantity, this))
		{
			return null;
		}
		return contextMessage;
	}

	protected virtual CanConstructInfo CanDeconstruct()
	{
		return CanConstructInfo.ValidPlacement;
	}

	private int GetBrokenState(int buildStateIndex)
	{
		int num = Mathf.Abs(buildStateIndex);
		if (BrokenBuildStates.Count == 0)
		{
			return 0;
		}
		if (num >= BrokenBuildStates.Count)
		{
			return -BrokenBuildStates.Count;
		}
		return -(num + 1);
	}

	public int GetBrokenState()
	{
		return GetBrokenState(CurrentBuildStateIndex);
	}

	protected void StructureDestroyed(ConstructionEventInstance eventInstance, bool destroyedFromDamage = false)
	{
		WorldGrid[] neighbourPositions;
		if (WorldParticleEffect.GridsInUse.Contains(base.WorldGrid))
		{
			bool flag = true;
			neighbourPositions = _neighbourPositions;
			for (int i = 0; i < neighbourPositions.Length; i++)
			{
				_ = ref neighbourPositions[i];
				Structure structure = base.GridController.Get<Structure>(base.WorldGrid);
				if (structure != null && structure != this)
				{
					flag = false;
				}
			}
			if (flag)
			{
				WorldParticleEffect.GridsInUse.Remove(base.WorldGrid);
			}
		}
		Span<WorldGrid> span = stackalloc WorldGrid[26];
		int count = 0;
		neighbourPositions = _neighbourPositions;
		foreach (WorldGrid worldGrid in neighbourPositions)
		{
			if (!WorldParticleEffect.GridsInUse.Contains(worldGrid))
			{
				continue;
			}
			WorldGrid.PopulateAllGridNeighours(span, ref count, worldGrid);
			bool flag2 = true;
			Span<WorldGrid> span2 = span;
			Span<WorldGrid> span3 = span2.Slice(0, count);
			for (int j = 0; j < span3.Length; j++)
			{
				WorldGrid worldGrid2 = span3[j];
				Structure structure2 = base.GridController.Get<Structure>(worldGrid2);
				if (structure2 != null && structure2 != this)
				{
					flag2 = false;
				}
			}
			if (flag2)
			{
				WorldParticleEffect.GridsInUse.Remove(worldGrid);
			}
		}
		if (destroyedFromDamage && BrokenBuildStates.Count > 0)
		{
			int index = Mathf.Clamp(Mathf.Abs(CurrentBuildStateIndex) - 1, 0, BrokenBuildStates.Count - 1);
			BrokenBuildState value = BrokenBuildStates[index];
			if (!value.HasBroken)
			{
				value.HasBroken = true;
				BrokenBuildStates[index] = value;
			}
		}
		else if ((bool)BuildStates[0].Tool.ToolEntry && !destroyedFromDamage)
		{
			try
			{
				BuildStates[0].Tool.Deconstruct(eventInstance);
			}
			catch (Exception ex)
			{
				ConsoleWindow.PrintError($"{DisplayName} #{base.ReferenceId} failed to return its parts on deconstruct: {ex.Message}");
			}
			try
			{
				if (Slots.Count > 0)
				{
					foreach (Slot slot in Slots)
					{
						if ((bool)slot.Occupant && GameManager.RunSimulation)
						{
							OnServer.MoveToWorld(slot.Occupant);
						}
					}
				}
			}
			catch (Exception ex2)
			{
				ConsoleWindow.PrintError($"{DisplayName} #{base.ReferenceId} failed to empty its slots on deconstruct: {ex2.Message}");
			}
		}
		if (AttachedDevices == null || AttachedDevices.Count <= 0)
		{
			return;
		}
		foreach (SmallGrid attachedDevice in AttachedDevices)
		{
			if (attachedDevice.ConnectedCount() > 0)
			{
				break;
			}
			if (attachedDevice.BuildStates.Count > 0)
			{
				try
				{
					for (int num = attachedDevice.CurrentBuildStateIndex; num >= 0; num--)
					{
						attachedDevice.BuildStates[num].Tool.Deconstruct(new ConstructionEventInstance(attachedDevice));
					}
				}
				catch (Exception ex3)
				{
					ConsoleWindow.PrintError($"{attachedDevice.DisplayName} #{attachedDevice.ReferenceId} failed to return its parts on deconstruct: {ex3.Message}");
				}
				try
				{
					if (attachedDevice.Slots.Count > 0)
					{
						foreach (Slot slot2 in attachedDevice.Slots)
						{
							if ((bool)slot2.Occupant && GameManager.RunSimulation)
							{
								OnServer.MoveToWorld(slot2.Occupant);
							}
						}
					}
				}
				catch (Exception ex4)
				{
					ConsoleWindow.PrintError($"{attachedDevice.DisplayName} #{attachedDevice.ReferenceId} failed to empty its slots on deconstruct: {ex4.Message}");
				}
			}
			OnServer.Destroy(attachedDevice);
		}
	}

	private bool IsOverrideMethod(string methodName)
	{
		return GetType().GetMethod(methodName)?.DeclaringType != typeof(Structure);
	}

	public override void OnPrefabLoad()
	{
		base.OnPrefabLoad();
		IsUpdateOnServerTick = IsOverrideMethod("OnServerTick");
	}

	public virtual void OnServerTick(float deltaTime)
	{
	}

	public Matrix4x4 GetBatchMatrix()
	{
		return _matrix4X4;
	}

	public override void OnRegistered(Cell cell)
	{
		if (IsCursor)
		{
			return;
		}
		Forward = ThingTransform.forward;
		Up = ThingTransform.up;
		base.OnRegistered(cell);
		LocalGrid = base.GridController.WorldToLocalGrid(CenterPosition);
		Rotation = base.ThingTransformRotation;
		CollisionType structureCollisionType = StructureCollisionType;
		if (structureCollisionType == CollisionType.BlockGrid || structureCollisionType == CollisionType.BlockFace)
		{
			Span<Grid3> span = stackalloc Grid3[32];
			int count = 0;
			base.GridController.GetNeighborCells(base.ThingTransformPosition, span, ref count);
			Span<Grid3> span2 = span;
			Span<Grid3> span3 = span2.Slice(0, count);
			for (int i = 0; i < span3.Length; i++)
			{
				Grid3 localGrid = span3[i];
				Cell cell2 = base.GridController.GetCell(localGrid);
				if (cell2 == null)
				{
					continue;
				}
				StructuralArray.Enumerator enumerator = cell2.Lookup.GetEnumerator();
				while (enumerator.MoveNext())
				{
					Structure current = enumerator.Current;
					if (current.StructureCollisionType == CollisionType.BlockGrid)
					{
						current.NeighborStructures.Add(this);
						NeighborStructures.Add(current);
					}
				}
			}
		}
		Span<WorldGrid> span4 = stackalloc WorldGrid[26];
		int count2 = 0;
		WorldGrid.PopulateAllGridNeighours(span4, ref count2, base.WorldGrid);
		Span<WorldGrid> span5 = span4;
		_neighbourPositions = span5.Slice(0, count2).ToArray();
		_matrix4X4 = Matrix4x4.TRS(base.Position, Rotation, Vector3.one);
		UpdateStateVisualizer();
		WorldChangeChecks();
		LastCreatedStructure = this;
	}

	protected Atmosphere GetWorldAtmosphere()
	{
		return AtmosphericsController.GetAtmosphereLocal(base.WorldGrid);
	}

	public bool IsExposedToGlobal()
	{
		Atmosphere worldAtmosphere = GetWorldAtmosphere();
		if (worldAtmosphere == null)
		{
			return GetRoom() == null;
		}
		if (!worldAtmosphere.IsGlobalAtmosphere)
		{
			return worldAtmosphere.IsCloseToGlobal(new PressurekPa(1.0));
		}
		return true;
	}

	private void WorldChangeChecks()
	{
		if (GameManager.GameState == GameState.None || AtmosphericsController == null)
		{
			return;
		}
		GameState gameState = GameManager.GameState;
		if (gameState == GameState.Loading || gameState == GameState.Joining)
		{
			return;
		}
		Span<Grid3> span = stackalloc Grid3[32];
		switch (StructureCollisionType)
		{
		case CollisionType.BlockGrid:
		{
			Span<Grid3> span2 = stackalloc Grid3[GridBounds._grids.Length];
			GridBounds.GetLocalGrids(base.ThingTransformPosition, base.ThingTransformRotation, span2);
			Span<Grid3> span3 = span2;
			for (int i = 0; i < span3.Length; i++)
			{
				Grid3 localGrid = span3[i];
				if (GameManager.RunSimulation)
				{
					if (!CanAirPass)
					{
						AtmosphericEventInstance.StructureBlockingGrid(new WorldGrid(localGrid));
					}
					else
					{
						AtmosphericEventInstance.StructureReleaseGrid(new WorldGrid(localGrid));
					}
				}
				RoomEvaluator.Instance.CheckFrame(new WorldGrid(localGrid));
				int count = 0;
				GridController.PopulateGridNeighbours(span, ref count, localGrid);
				Span<Grid3> span4 = span;
				Span<Grid3> span5 = span4.Slice(0, count);
				for (int j = 0; j < span5.Length; j++)
				{
					Grid3 grid3 = span5[j];
					WorldGrid worldGrid3 = new WorldGrid(grid3);
					AtmosphericsController.CheckAtmosphereConnections(worldGrid3);
				}
			}
			break;
		}
		case CollisionType.BlockFace:
		{
			Grid3[] array = BlockingGrids;
			foreach (Grid3 grid in array)
			{
				WorldGrid worldGrid = new WorldGrid(grid);
				Grid3 grid2 = grid - worldGrid.Value;
				WorldGrid worldGrid2 = new WorldGrid(grid + grid2);
				RoomEvaluator.Instance.CheckWall(worldGrid, worldGrid2);
				AtmosphericsController.CheckAtmosphereConnections(worldGrid);
				AtmosphericsController.CheckAtmosphereConnections(worldGrid2);
			}
			break;
		}
		}
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		if (StructureCollisionType == CollisionType.BlockGrid || StructureCollisionType == CollisionType.BlockFace)
		{
			Span<Grid3> span = stackalloc Grid3[32];
			int count = 0;
			base.GridController.GetNeighborCells(base.ThingTransformPosition, span, ref count);
			Span<Grid3> span2 = span;
			Span<Grid3> span3 = span2.Slice(0, count);
			for (int i = 0; i < span3.Length; i++)
			{
				Grid3 localGrid = span3[i];
				Cell cell = base.GridController.GetCell(localGrid);
				if (cell == null)
				{
					continue;
				}
				StructuralArray.Enumerator enumerator = cell.Lookup.GetEnumerator();
				while (enumerator.MoveNext())
				{
					Structure current = enumerator.Current;
					if (current.StructureCollisionType == CollisionType.BlockGrid)
					{
						current.NeighborStructures.Remove(this);
						NeighborStructures.Remove(current);
					}
				}
			}
		}
		WorldChangeChecks();
	}

	public void RemoveFromNeighbouringGridCells(Vector3 worldPosition)
	{
		if (StructureCollisionType != CollisionType.BlockGrid && StructureCollisionType != CollisionType.BlockFace)
		{
			return;
		}
		Span<Grid3> span = stackalloc Grid3[32];
		int count = 0;
		base.GridController.GetNeighborCells(worldPosition, span, ref count);
		Span<Grid3> span2 = span;
		Span<Grid3> span3 = span2.Slice(0, count);
		for (int i = 0; i < span3.Length; i++)
		{
			Grid3 localGrid = span3[i];
			Cell cell = base.GridController.GetCell(localGrid);
			if (cell == null)
			{
				continue;
			}
			StructuralArray.Enumerator enumerator = cell.Lookup.GetEnumerator();
			while (enumerator.MoveNext())
			{
				Structure current = enumerator.Current;
				if (current.StructureCollisionType == CollisionType.BlockGrid)
				{
					current.NeighborStructures.Remove(this);
					NeighborStructures.Remove(current);
				}
			}
		}
	}

	public override void Start()
	{
		base.Start();
		if (structureRenderMode == StructureRenderMode.Batched)
		{
			SetCustomColor(CustomColor);
		}
	}

	public override void OnDamageDestroyed()
	{
		base.OnDamageDestroyed();
		if (GameManager.RunSimulation && !base.Indestructable)
		{
			ConstructionEventInstance eventInstance = new ConstructionEventInstance
			{
				Parent = this,
				Position = base.ThingTransformPosition,
				Rotation = ThingTransform.rotation,
				SteamId = base.OwnerClientId,
				OtherHandSlot = null
			};
			StructureDestroyed(eventInstance, destroyedFromDamage: true);
			if (this is IWreckage wreckage)
			{
				wreckage.SpawnWreckage();
			}
			OnServer.Destroy(this);
		}
	}

	public override void OnDestroy()
	{
		if (Singleton<GameManager>.IsQuitting)
		{
			return;
		}
		if (MyGraphicsRaycaster.CameraInfo.CameraTransform != null)
		{
			MyGraphicsRaycaster.CameraInfo.CameraTransform.SetParent(null);
		}
		base.OnDestroy();
		if (CustomNavMeshFilter != null)
		{
			UnityEngine.Object.Destroy(CustomNavMeshFilter.mesh);
			UnityEngine.Object.Destroy(CustomNavMeshFilter);
		}
		if (GameManager.GameState != GameState.None && !IsCursor)
		{
			base.GridController.Deregister(this);
			GridController.AllServerTickStructures.Remove(this);
			if (RocketData?.Network != null && this is IRocketInternals rocketInternals)
			{
				RocketData.Network.RemoveInternal(rocketInternals);
			}
			if (structureRenderMode == StructureRenderMode.Batched && CurrentBuildState != null)
			{
				RenderBatch.Deregister(CurrentBuildState.RendererInstance);
			}
		}
	}

	public virtual void WillJoinNetwork(Span<ConnectionRef> connBuf, ref int connCount)
	{
	}

	public override void CachePrefabBounds()
	{
		base.CachePrefabBounds();
		if (BuildStates != null)
		{
			foreach (BuildState buildState in BuildStates)
			{
				if (buildState.InitialDrawData.IsValid())
				{
					Bounds.Encapsulate(buildState.InitialDrawData.mesh.bounds);
				}
			}
		}
		GridBounds = new GridBounds(this);
	}

	public virtual object GetLocalGridBounds()
	{
		if (ForceGridBounds.Count > 0)
		{
			return ForceGridBounds.ToArray();
		}
		Vector3 worldPosition = Bounds.min * (BoundsGridRatio * DualRegisterGridScale);
		worldPosition.y += BoundsGridAddBottom;
		worldPosition.x += worldPosition.x * BoundsGridExtraWidth;
		worldPosition.z += worldPosition.z * BoundsGridExtraForward;
		Vector3 worldPosition2 = Bounds.max * (BoundsGridRatio * DualRegisterGridScale);
		worldPosition2.y += BoundsGridAddHeight;
		worldPosition2.y += worldPosition2.y * BoundsGridExtraHeight;
		worldPosition2.x += worldPosition2.x * BoundsGridExtraWidth;
		worldPosition2.z += worldPosition2.z * BoundsGridExtraForward + worldPosition2.z * BoundsForward;
		Grid3 grid = worldPosition.ToGrid();
		Grid3 grid2 = worldPosition2.ToGrid();
		grid += (GridSize * 0.5f * Vector3.one).ToGridPosition();
		Grid3 grid3 = grid2 - (GridSize * 0.5f * Vector3.one).ToGridPosition();
		float num = (float)Math.Abs(grid3.x - grid.x) / GridSize * 0.1f;
		float num2 = (float)Math.Abs(grid3.y - grid.y) / GridSize * 0.1f;
		float num3 = (float)Math.Abs(grid3.z - grid.z) / GridSize * 0.1f;
		int num4 = 0;
		Grid3[] array = new Grid3[((int)num + 1) * ((int)num2 + 1) * ((int)num3 + 1)];
		for (int i = 0; (float)i <= num; i++)
		{
			for (int j = 0; (float)j <= num2; j++)
			{
				for (int k = 0; (float)k <= num3; k++)
				{
					Grid3 grid4 = new Grid3((float)i * GridSize * 10f, (float)j * GridSize * 10f, (float)k * GridSize * 10f);
					grid4 += grid;
					array[num4++] = grid4;
				}
			}
		}
		return array;
	}

	public virtual Bounds GetSmallGridBounds()
	{
		Bounds result = new Bounds(Bounds.center, Bounds.size);
		result.Expand(BoundsExpand);
		Vector3 worldPosition = result.min * BoundsGridRatio;
		worldPosition.y += BoundsGridAddBottom;
		worldPosition.x += worldPosition.x * BoundsGridExtraWidth;
		worldPosition.z += worldPosition.z * BoundsGridExtraForward;
		Vector3 worldPosition2 = result.max * BoundsGridRatio;
		worldPosition2.y += BoundsGridAddHeight;
		worldPosition2.y += worldPosition2.y * BoundsGridExtraHeight;
		worldPosition2.x += worldPosition2.x * BoundsGridExtraWidth;
		worldPosition2.z += worldPosition2.z * BoundsGridExtraForward + worldPosition2.z * BoundsForward + BoundsGridShiftForward;
		result.min = worldPosition.ToGrid(SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset).ToVector3();
		result.min -= GridSize * 0.5f * Vector3.one;
		result.max = worldPosition2.ToGrid(SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset).ToVector3();
		result.max += GridSize * 0.5f * Vector3.one;
		return result;
	}

	public virtual object GetLocalSmallGridBounds()
	{
		Bounds bounds = Bounds;
		bounds.Expand(BoundsExpand);
		Vector3 worldPosition = bounds.min * BoundsGridRatio;
		worldPosition.y += BoundsGridAddBottom;
		worldPosition.x += worldPosition.x * BoundsGridExtraWidth;
		worldPosition.z += worldPosition.z * BoundsGridExtraForward + BoundsGridShiftForward;
		Vector3 worldPosition2 = bounds.max * BoundsGridRatio;
		worldPosition2.y += BoundsGridAddHeight;
		worldPosition2.y += worldPosition2.y * BoundsGridExtraHeight;
		worldPosition2.x += worldPosition2.x * BoundsGridExtraWidth;
		worldPosition2.z += worldPosition2.z * BoundsGridExtraForward + worldPosition2.z * BoundsForward + BoundsGridShiftForward;
		Grid3 grid = worldPosition.ToGridPosition();
		Grid3 grid2 = worldPosition2.ToGridPosition();
		float num = (float)Math.Abs(grid2.x - grid.x) / 0.5f * 0.1f;
		float num2 = (float)Math.Abs(grid2.y - grid.y) / 0.5f * 0.1f;
		float num3 = (float)Math.Abs(grid2.z - grid.z) / 0.5f * 0.1f;
		int num4 = 0;
		Grid3[] array = new Grid3[((int)num + 1) * ((int)num2 + 1) * ((int)num3 + 1)];
		for (int i = 0; (float)i <= num; i++)
		{
			for (int j = 0; (float)j <= num2; j++)
			{
				for (int k = 0; (float)k <= num3; k++)
				{
					Grid3 grid3 = new Grid3((float)i * 0.5f * 10f, (float)j * 0.5f * 10f, (float)k * 0.5f * 10f);
					grid3 += grid;
					array[num4++] = grid3;
				}
			}
		}
		return array;
	}

	public override void OnFireTick()
	{
		if (!IsBurnable)
		{
			return;
		}
		Atmosphere atmosphere = AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid);
		if (atmosphere != null && (Cell == null || (Cell != null && Cell.IsOpenAir(LocalGrid, allowCrewModules: true))) && !base.IsBurning && ShouldIgnite(atmosphere))
		{
			base.IsBurning = true;
			OnFireStart();
		}
		if (!base.IsBurning)
		{
			return;
		}
		float num = 100f * AtmosphericsManager.Instance.TickSpeedSeconds / BurnTime;
		float heatEnergyReleased = EnergyReleasedWhenBurned * num / 100f;
		DamageState.Damage(ChangeDamageType.Increment, num, DamageUpdateType.Burn);
		Atmosphere atmosphere2 = AtmosphericsManager.CloneGlobalAtmosphereThreadSafe(base.WorldGrid);
		lock (atmosphere2)
		{
			if (OnFireConsume(atmosphere2, heatEnergyReleased))
			{
				atmosphere2.Sparked = true;
				return;
			}
			atmosphere2.Sparked = false;
			Extinguish();
		}
	}

	public PlacementSnap GetPlacementType()
	{
		return PlacementType;
	}

	public RotationAxis GetRotationAxis()
	{
		return RotationAxis;
	}

	public AllowedRotations GetAllowedRotations()
	{
		return AllowedRotations;
	}

	public void Rotate(Vector3 axis, float angle, Quaternion offset, Vector3 centerOfRotation)
	{
		switch (PlacementType)
		{
		case PlacementSnap.Grid:
		case PlacementSnap.Face:
			switch (RotationAxis)
			{
			case RotationAxis.XY:
			case RotationAxis.ZX:
			case RotationAxis.ZY:
			case RotationAxis.All:
				if (AllowedRotations != AllowedRotations.Floor)
				{
					Quaternion quaternion3 = Quaternion.AngleAxis(angle, axis);
					(offset * quaternion3 * Quaternion.Inverse(offset)).ToAngleAxis(out angle, out axis);
					ThingTransform.RotateAround(centerOfRotation, axis, angle);
					break;
				}
				goto case RotationAxis.Y;
			case RotationAxis.X:
			{
				Quaternion quaternion2 = Quaternion.AngleAxis(angle, axis);
				(offset * RotateZtoX * quaternion2 * RotateXtoZ * Quaternion.Inverse(offset)).ToAngleAxis(out angle, out axis);
				ThingTransform.RotateAround(centerOfRotation, axis, angle);
				break;
			}
			case RotationAxis.Y:
			{
				Quaternion quaternion4 = Quaternion.AngleAxis(angle, axis);
				(offset * RotateZtoY * quaternion4 * RotateYtoZ * Quaternion.Inverse(offset)).ToAngleAxis(out angle, out axis);
				ThingTransform.RotateAround(centerOfRotation, axis, angle);
				break;
			}
			case RotationAxis.Z:
			{
				Quaternion quaternion = Quaternion.AngleAxis(angle, axis);
				(offset * quaternion * Quaternion.Inverse(offset)).ToAngleAxis(out angle, out axis);
				ThingTransform.RotateAround(centerOfRotation, axis, angle);
				break;
			}
			}
			break;
		case PlacementSnap.FaceMount:
			ThingTransform.Rotate(axis, angle, Space.Self);
			break;
		default:
			ThingTransform.Rotate(axis, angle, Space.Self);
			break;
		}
	}

	public void SetStructureData(Quaternion localRotation, ulong ownerClientId, Grid3 localGrid, int customColourIndex)
	{
		Direction = localRotation;
		base.OwnerClientId = ownerClientId;
		RegisteredLocalGrid = localGrid;
		if (customColourIndex >= 0 && PaintableMaterial != null && customColourIndex != CustomColor.Index)
		{
			SetCustomColor(customColourIndex);
		}
	}

	public bool AlwaysInstanceWorldAtmosphere()
	{
		BuildState currentBuildState = CurrentBuildState;
		if (currentBuildState != null && currentBuildState.BlockGravity)
		{
			return !currentBuildState.BlockAir;
		}
		return false;
	}
}
