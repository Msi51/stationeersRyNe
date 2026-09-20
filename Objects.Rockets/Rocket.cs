using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Serialization;
using Assets.Scripts.Sound;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Audio;
using Cysharp.Threading.Tasks;
using ImGuiNET;
using Networks;
using Objects.Electrical;
using Objects.Rockets.Log;
using Objects.Rockets.Log.RocketEvents;
using Objects.Rockets.Scanning;
using Objects.Rockets.UI;
using Sound;
using TerrainSystem;
using Trading;
using UI.ImGuiUi;
using UnityEngine;
using Util;

namespace Objects.Rockets;

public class Rocket : IPhysical, IProfile, IDensePoolable, IReferencable, IEvaluable, IComboable, IAudioParent
{
	private readonly DensePoolReference<IPhysical> _densePoolReference = new DensePoolReference<IPhysical>(Thing.PhysicalPoolActive);

	private Transform _rocketParentTransform;

	private float _currentVelocity;

	public Sprite MapIcon;

	public Sprite MapHighlight;

	public bool MapIconDirty = true;

	public int MapIconColorIndex = -1;

	public int LaunchMountColorIndex = -1;

	[NonSerialized]
	public bool CanPopUpPoiText = true;

	public static List<Rocket> AllRockets = new List<Rocket>();

	public static List<Rocket> NewRockets = new List<Rocket>();

	public static List<long> RemovedRockets = new List<long>();

	public static List<string> DefaultRocketNames = new List<string>(1024);

	public static List<string> RocketNameHistory = new List<string>(128);

	public const float OPTIMAL_LANDING_ALTITUDE = 25000f;

	public const float MEDIUM_LANDING_ALTITUDE = 40000f;

	public const float HIGH_LANDING_ALTITUDE = 70000f;

	public const float VERY_HIGH_LANDING_ALTITUDE = 120000f;

	public static Dictionary<ReEntryProfile, float> ReEntryProfiles = new Dictionary<ReEntryProfile, float>
	{
		{
			ReEntryProfile.None,
			120000f
		},
		{
			ReEntryProfile.Low,
			25000f
		},
		{
			ReEntryProfile.Medium,
			40000f
		},
		{
			ReEntryProfile.High,
			70000f
		},
		{
			ReEntryProfile.Max,
			120000f
		}
	};

	private static StringBuilder nameBuilder = new StringBuilder(128);

	private bool _hasSavedTransform;

	private Vector3 _savedPositionOnParent;

	private Vector3 _savedTransformPosition;

	private Vector3 _savedTargetPosition;

	private Vector2Int _savedRocketParkLocation;

	private float _savedTargetOverShoot;

	private float _savedVelocity;

	private float _savedGravityDeficit;

	private float _savedAcceleration;

	private float _savedMaxRecordedThrust;

	private bool _initialised;

	public List<NodeTransit> AllPaths;

	private RocketState _rocketState;

	private RocketMode _rocketMode;

	private SpaceMapNode _currentNode;

	private NodeTransit _currentTransit;

	private SpaceMapNode _targetNode;

	private float _progress;

	private float _acceleration;

	private float _engineAcceleration;

	private float _distanceToTarget;

	private float _orbitalPosition;

	private string _customName;

	private ushort _estimatedRemainingBurnTime;

	private RocketParkSlot _rocketParkSlot;

	private float _velocity;

	private bool _automatedShutOff = true;

	private bool _automatedLanding = true;

	private float _maxRecordedThrust;

	private Vector3 _targetPosition;

	private ReEntryProfile _reEntryProfile = ReEntryProfile.Low;

	private RocketActionResult _lastReportedResult;

	public RocketAction CurrentAction;

	private readonly List<IRocketActionProgressable> _actionProgressables = new List<IRocketActionProgressable>(16);

	private readonly int RocketFallHash = Animator.StringToHash("RocketFall");

	private PooledAudioSource _airMovementAudio;

	private const float MAX_AUDIO_VELOCITY = -100f;

	private CancellationTokenWrapper _finishMoveToTargetToken = new CancellationTokenWrapper();

	private const float LERP_SPEED = 2f;

	private readonly Collider[] _engineOverlap = new Collider[512];

	private readonly Collider[] _noseConeOverlap = new Collider[512];

	private static readonly int _mask = 1 << LayerMask.NameToLayer("Default");

	private const int COLLISION_STRUCTURE_DAMAGE = 1000;

	public const float SOFTLANDING_VELOCITY = -2f;

	public const float HARD_LANDING_VELOCITY = -4f;

	public const float CRASH_LANDING_VELOCITY = -25f;

	private static readonly int RocketExplodeCloseHash = Animator.StringToHash("RocketExplodeClose");

	private static readonly int RocketExplodeDistantHash = Animator.StringToHash("RocketExplodeDistant");

	private float _minThrottle;

	private float _highestRecordedThrust;

	private const float NORMAL_LANDING_RANGE = 2f;

	public const float LANDING_BEGIN_ALTITUDE = 60f;

	private const float LANDING_ARREST_ALTITUDE = 40f;

	private const float ALTERNATE_2_SAFETY_MARGIN = 2f;

	private const float ALTITUDE_DISCREPANCY_MULTIPLIER = 1f;

	private const float ROCKET_AUDIBLE_ALTITUDE = 1000f;

	private const float MAX_THROTTLE = 100f;

	private const float MIN_LAND_VELOCITY = -0.5f;

	private const float MAX_LAND_VELOCITY = -1f;

	private const float MIN_THROTTLE_TRIM = 0.05f;

	private const float MAX_THROTTLE_TRIM = 10f;

	private const float MAX_LANDING_TTW_VARIANCE = 0.2f;

	private FlightControlRule _flightControlRule;

	private float _lastCalculatedThrust;

	private float _lastCalculatedThrustToWeight;

	private float _lastCalculatedFuelTime;

	private float _lastCalculatedAcceleration;

	public Action OnTargetReached;

	private MoleQuantity _totalFuelMolesLastTick;

	public Vector3 LastParentedPosition;

	public const float LOW_ORBIT_BOTTOM_ALTITUDE = 1500f;

	public const float LOW_ORBIT_HEIGHT = 1000f;

	public const float ORBIT_ANIM_HEIGHT_OFFSET = 1000f;

	public const float ZERO_G_HEIGHT = 1000f;

	public static bool IsDrawingDebug;

	public static Rocket _debugRocket;

	private const float MAX_VELOCITY_FOR_CRASH = 400f;

	private const float MAX_FUEL_FOR_CRASH = 100000f;

	public const float MAX_SHAKE_DISTANCE_SQUARED = 6400f;

	private Vector3 _destroyedLaunchMountPosition;

	public Transform RocketParentTransform
	{
		get
		{
			return _rocketParentTransform;
		}
		private set
		{
			_rocketParentTransform = value;
		}
	}

	public RocketNetwork RocketNetwork { get; }

	public static List<RocketSaveData> GetRocketSaveDatas
	{
		get
		{
			List<RocketSaveData> list = new List<RocketSaveData>();
			foreach (Rocket allRocket in AllRockets)
			{
				if (allRocket != null)
				{
					RocketSaveData item = new RocketSaveData(allRocket);
					list.Add(item);
				}
			}
			return list;
		}
	}

	public string DisplayName
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(CustomName))
			{
				return CustomName;
			}
			return CustomName = GameStrings.Rocket.AsString(StringManager.Get(ReferenceId));
		}
	}

	public ushort NetworkUpdateFlags { get; set; }

	public long ReferenceId { get; set; }

	public bool BeingDestroyed { get; set; }

	public RocketState RocketState
	{
		get
		{
			return _rocketState;
		}
		set
		{
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				NetworkUpdateFlags |= 128;
			}
			RocketState rocketState = RocketState;
			_rocketState = value;
			OnRocketStateUpdated(rocketState);
		}
	}

	public RocketMode RocketMode
	{
		get
		{
			return _rocketMode;
		}
		set
		{
			if (NetworkManager.IsServer && NetworkServer.HasClients() && RocketMode != value)
			{
				NetworkUpdateFlags |= 32768;
			}
			RocketMode rocketMode = RocketMode;
			_rocketMode = value;
			OnRocketModeUpdated(rocketMode);
		}
	}

	public SpaceMapNode CurrentNode
	{
		get
		{
			return _currentNode;
		}
		private set
		{
			_currentNode = value;
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				NetworkUpdateFlags |= 32;
			}
		}
	}

	public NodeTransit CurrentTransit
	{
		get
		{
			return _currentTransit;
		}
		set
		{
			_currentTransit = value;
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				NetworkUpdateFlags |= 4;
			}
		}
	}

	public SpaceMapNode TargetNode
	{
		get
		{
			return _targetNode;
		}
		set
		{
			_targetNode = value;
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				NetworkUpdateFlags |= 1;
			}
		}
	}

	public float Progress
	{
		get
		{
			return _progress;
		}
		set
		{
			_progress = value;
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				NetworkUpdateFlags |= 8;
			}
		}
	}

	public float Acceleration
	{
		get
		{
			return _acceleration;
		}
		set
		{
			_acceleration = value;
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				NetworkUpdateFlags |= 16;
			}
		}
	}

	public float EngineAcceleration
	{
		get
		{
			return _engineAcceleration;
		}
		set
		{
			_engineAcceleration = value;
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				NetworkUpdateFlags |= 16;
			}
		}
	}

	public float DistanceToTarget
	{
		get
		{
			return _distanceToTarget;
		}
		set
		{
			_distanceToTarget = value;
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				NetworkUpdateFlags |= 64;
			}
		}
	}

	public float DistanceToNextTarget => (1f - Progress) * CurrentTransit.GetDistance();

	public float OrbitalPosition
	{
		get
		{
			return _orbitalPosition;
		}
		set
		{
			_orbitalPosition = value;
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				NetworkUpdateFlags |= 64;
			}
		}
	}

	public string CustomName
	{
		get
		{
			return _customName;
		}
		set
		{
			_customName = value;
			if (!RocketNameHistory.Contains(value))
			{
				RocketNameHistory.Add(value);
			}
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				NetworkUpdateFlags |= 1024;
			}
		}
	}

	private ushort EstimatedRemainingBurnTime
	{
		get
		{
			return _estimatedRemainingBurnTime;
		}
		set
		{
			_estimatedRemainingBurnTime = value;
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				NetworkUpdateFlags |= 2048;
			}
		}
	}

	public RocketParkSlot RocketParkSlot
	{
		get
		{
			return _rocketParkSlot;
		}
		set
		{
			_rocketParkSlot = value;
			if (RocketParkSlot != null && GameManager.GameState == GameState.Running)
			{
				RocketParentTransform.position = RocketParkSlot.GetWorldPosition();
			}
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				NetworkUpdateFlags |= 256;
			}
		}
	}

	public float Velocity
	{
		get
		{
			return _velocity;
		}
		private set
		{
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				NetworkUpdateFlags |= 16384;
			}
			_velocity = value;
		}
	}

	public bool AutomatedShutOff
	{
		get
		{
			return _automatedShutOff;
		}
		set
		{
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				NetworkUpdateFlags |= 4096;
			}
			_automatedShutOff = value;
		}
	}

	public bool AutomatedLanding
	{
		get
		{
			return _automatedLanding;
		}
		set
		{
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				NetworkUpdateFlags |= 8192;
			}
			_automatedLanding = value;
			if (!GameManager.RunSimulation)
			{
				return;
			}
			FlightControlRule = (AutomatedLanding ? FlightControlRule.Normal : FlightControlRule.None);
			if (AutomatedLanding)
			{
				if (RocketState == RocketState.Landing)
				{
					InitAutomatedLanding();
				}
			}
			else
			{
				TurnOffEngines();
				SetThrottle(100f);
			}
		}
	}

	public float MaxRecordedThrust
	{
		get
		{
			return _maxRecordedThrust;
		}
		set
		{
			if (NetworkManager.IsServer && NetworkServer.HasClients() && !RocketMath.Approximately(MaxRecordedThrust, value, float.Epsilon))
			{
				NetworkUpdateFlags |= 8192;
			}
			_maxRecordedThrust = value;
		}
	}

	public Vector3 TargetPosition
	{
		get
		{
			return _targetPosition;
		}
		set
		{
			if (NetworkManager.IsServer && NetworkServer.HasClients() && !RocketMath.Approximately(TargetPosition, value, float.Epsilon))
			{
				NetworkUpdateFlags |= 512;
			}
			_targetPosition = value;
		}
	}

	public ReEntryProfile ReEntryProfile
	{
		get
		{
			return _reEntryProfile;
		}
		set
		{
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				NetworkUpdateFlags |= 8192;
			}
			_reEntryProfile = value;
		}
	}

	private RocketSize Size
	{
		get
		{
			RocketNetwork rocketNetwork = RocketNetwork;
			if (rocketNetwork == null || rocketNetwork.RocketSize < RocketSize.Medium)
			{
				return RocketSize.Medium;
			}
			return RocketNetwork.RocketSize;
		}
	}

	public bool IsTravelling
	{
		get
		{
			if (TargetNode != null)
			{
				return Progress != 0f;
			}
			return false;
		}
	}

	public bool IsManned
	{
		get
		{
			if (RocketNetwork?.Internals == null)
			{
				return false;
			}
			foreach (IRocketInternals @internal in RocketNetwork.Internals)
			{
				if (@internal is CrewModuleChair crewModuleChair && crewModuleChair.Slots[0].Occupant is Entity)
				{
					return true;
				}
			}
			return false;
		}
	}

	public FlightControlRule FlightControlRule
	{
		get
		{
			return _flightControlRule;
		}
		set
		{
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				NetworkUpdateFlags |= 8192;
			}
			_flightControlRule = value;
		}
	}

	public float EstimatedRemainingBurnTimeSeconds => (CurrentTransit != null) ? EstimatedRemainingBurnTime : 0;

	public static Bounds LowOrbitPlayableBounds => new Bounds(new Vector3(0f, 2000f, 0f), new Vector3(VoxelConstants.Size, 1000f, VoxelConstants.Size));

	public string ProfilerTag => "Rocket";

	public bool RunPhysicsUpdate => true;

	public Vector3 Position => GetWorldPosition();

	public Transform Transform => _rocketParentTransform;

	public WorldGrid WorldGrid => new WorldGrid(Position);

	public bool IsBeingDestroyed => BeingDestroyed;

	public Transform SoundPosition => _rocketParentTransform;

	public bool OnAddToPool(object densePool, int slot)
	{
		if (_densePoolReference.CanAddToPool(densePool))
		{
			return _densePoolReference.AddToPool(densePool, slot);
		}
		return false;
	}

	public void OnRemoveFromPool(object densePool)
	{
		_densePoolReference.OnRemovedFrom(densePool);
	}

	public void MarkIconDirty()
	{
		MapIconDirty = true;
	}

	public Rocket(RocketNetwork rocketNetwork, long referenceId = 0L)
	{
		RocketNetwork = rocketNetwork;
		RocketNetwork.Rocket = this;
		if (referenceId == 0L)
		{
			Referencable.RegisterNew(this);
		}
		else
		{
			Referencable.RegisterAs(this, referenceId);
		}
		GameState gameState = GameManager.GameState;
		if (gameState != GameState.Loading && gameState != GameState.Joining)
		{
			_initialised = true;
		}
	}

	private static string GenerateUniqueRocketName()
	{
		int num = 1;
		string text;
		do
		{
			nameBuilder.Clear();
			nameBuilder.Append(DefaultRocketNames.Pick());
			if (num > 1)
			{
				nameBuilder.Append('-');
				nameBuilder.Append(StringManager.Get(num));
			}
			text = nameBuilder.ToString();
			num++;
		}
		while (RocketNameIsUsed(text));
		return text;
	}

	private static bool RocketNameIsUsed(string name)
	{
		foreach (string item in RocketNameHistory)
		{
			if (name == item)
			{
				return true;
			}
		}
		return false;
	}

	public static void LoadRocket(RocketSaveData saveData)
	{
		RocketNetwork rocketNetwork = Referencable.Find<RocketNetwork>(saveData.RocketNetworkId);
		if (rocketNetwork == null)
		{
			return;
		}
		SpaceMapNode spaceMapNode = Referencable.Find<SpaceMapNode>(saveData.CurrentNodeId);
		if (spaceMapNode == null)
		{
			switch (saveData.RocketState)
			{
			case RocketState.None:
			case RocketState.OnLaunchMount:
			case RocketState.Launching:
				saveData.RocketState = RocketState.None;
				saveData.HasParentTransform = false;
				break;
			case RocketState.InSpace:
			case RocketState.Landing:
				spaceMapNode = SpaceMap.Current.EntryNode;
				saveData.RocketMode = RocketMode.None;
				saveData.RocketState = RocketState.InSpace;
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}
		Rocket rocket = new Rocket(rocketNetwork, saveData.ReferenceId)
		{
			TargetNode = Referencable.Find<SpaceMapNode>(saveData.TargetNodeId),
			CustomName = saveData.CustomName,
			RocketState = saveData.RocketState,
			ReEntryProfile = saveData.ReEntryProfile,
			AutomatedShutOff = saveData.AutoShutOff,
			RocketMode = saveData.RocketMode
		};
		if (rocket.ReEntryProfile == ReEntryProfile.None)
		{
			rocket.ReEntryProfile = ReEntryProfile.Low;
		}
		rocket.SetCurrentNode(spaceMapNode);
		rocket._hasSavedTransform = saveData.HasParentTransform;
		rocket._savedPositionOnParent = saveData.ParentedPosition;
		rocket._savedTransformPosition = saveData.RocketTransformPosition;
		rocket._savedTargetPosition = saveData.TargetPosition;
		rocket._savedRocketParkLocation = saveData.ParkLocation;
		rocket._savedVelocity = saveData.Velocity;
		rocket._savedAcceleration = saveData.Acceleration;
		rocket._savedMaxRecordedThrust = saveData.MaxRecordedThrust;
		if (rocket.RocketState == RocketState.None)
		{
			rocket.RocketNetwork.RefreshRocket();
			rocket.TargetNode = null;
			rocket.RocketState = RocketState.OnLaunchMount;
		}
		else
		{
			rocket.LoadSavedProgressToTarget(saveData);
		}
		rocket.AutomatedLanding = saveData.AutoLand;
		rocket.OrbitalPosition = saveData.OrbitalPosition;
	}

	public static void OnFinishedLoad()
	{
		foreach (Rocket allRocket in AllRockets)
		{
			if (allRocket == null)
			{
				continue;
			}
			if (allRocket._hasSavedTransform)
			{
				allRocket.ReParentRocketParts(allRocket._savedPositionOnParent);
				if (allRocket._savedRocketParkLocation != RocketParkSlot.InvalidLocation)
				{
					RocketParkSlot.ParkRocket(allRocket._savedRocketParkLocation, allRocket);
				}
				else if (allRocket.RocketState == RocketState.InSpace && GameManager.RunSimulation)
				{
					ConsoleWindow.PrintError("Rocket Space Position not serialized correctly");
					RocketParkSlot.ParkRocket(allRocket);
				}
				allRocket.TargetPosition = allRocket._savedTargetPosition;
				allRocket.RocketParentTransform.position = allRocket._savedTransformPosition;
				allRocket.RocketNetwork.UpdateStructurePositions();
			}
			allRocket.Velocity = allRocket._savedVelocity;
			allRocket.Acceleration = allRocket._savedAcceleration;
			allRocket.MaxRecordedThrust = allRocket._savedMaxRecordedThrust;
			if (allRocket.RocketState == RocketState.OnLaunchMount)
			{
				allRocket.DetatchPartsFromGrid();
				RocketParkSlot.RemoveRocket(allRocket);
				allRocket.UnParentRocketParts();
				allRocket.AttachPartsToGrid();
				if (allRocket.CurrentNode == null)
				{
					allRocket.RocketNetwork.RefreshRocket();
				}
				else
				{
					allRocket.SetRocketSizeToLaunchMount(allRocket.CurrentNode.Owner);
				}
				allRocket.OnLanded(immediate: true);
			}
			else if (allRocket.RocketState != RocketState.None)
			{
				allRocket.OnLaunch(immediate: true);
			}
			if (allRocket.RocketState == RocketState.Landing)
			{
				allRocket.SetRocketSizeToLaunchMount(allRocket.CurrentTransit.Destination.Owner);
			}
			RocketState rocketState = allRocket.RocketState;
			if (rocketState != RocketState.InSpace && rocketState != RocketState.Landing)
			{
				RocketParkSlot.RemoveRocket(allRocket);
			}
			Sprite icon = RocketMapIconRenderer.Instance.GetIcon(allRocket, allRocket.LaunchMountColorIndex);
			allRocket.MapIcon = icon;
			allRocket._initialised = true;
		}
	}

	public void AbandonRocket()
	{
		BeingDestroyed = true;
		_finishMoveToTargetToken.Cancel();
		RocketNetwork.DestroyAllInternalsAndStructures();
		RocketState = RocketState.None;
		RocketCanvas.Instance.RocketChangedRefresh();
	}

	private void LoadSavedProgressToTarget(RocketSaveData saveData)
	{
		if (TargetNode != null && SpaceMapPathFinder.GetNextConnection(CurrentNode, TargetNode, out var next, out var overallPath))
		{
			CurrentTransit = next;
			Progress = saveData.Progress;
			AllPaths = overallPath;
		}
	}

	public void OnAssignedReference()
	{
		AllRockets.Add(this);
		Thing.Register(this);
		if (NetworkManager.IsServer && NetworkServer.HasClients())
		{
			NewRockets.Add(this);
		}
	}

	public void DeRegister()
	{
		SetCurrentNode(null);
		RocketParkSlot.RemoveRocket(this);
		AllRockets.Remove(this);
		if (NetworkManager.IsServer && NetworkServer.HasClients())
		{
			RemovedRockets.Add(ReferenceId);
		}
		Referencable.Deregister(this);
		Thing.Deregister(this);
		OnDestroy();
	}

	public void SetCurrentNode(SpaceMapNode node)
	{
		if (CurrentNode != null)
		{
			CurrentNode.RocketsHere.Remove(this);
		}
		CurrentNode = node;
		if (CurrentNode != null)
		{
			CurrentNode.RocketsHere.Add(this);
		}
		RocketCanvas.Instance.RefreshRocketLocation();
	}

	public static void SerializeOnJoin(RocketBinaryWriter writer)
	{
		writer.WriteUInt16((ushort)AllRockets.Count);
		foreach (Rocket allRocket in AllRockets)
		{
			Write(writer, allRocket);
		}
	}

	public static async UniTask DeserializeOnJoin(RocketBinaryReader reader)
	{
		await ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenDeserializingRockets.DisplayString);
		ushort num = reader.ReadUInt16();
		for (int i = 0; i < num; i++)
		{
			Read(reader);
		}
	}

	public static void SerializeNew(RocketBinaryWriter writer)
	{
		bool flag = NewRockets.Count > 0;
		writer.WriteBoolean(flag);
		if (!flag)
		{
			return;
		}
		writer.WriteUInt16((ushort)NewRockets.Count);
		foreach (Rocket newRocket in NewRockets)
		{
			Write(writer, newRocket);
		}
		NewRockets.Clear();
	}

	public static void DeserializeNew(RocketBinaryReader reader)
	{
		if (reader.ReadBoolean())
		{
			ushort num = reader.ReadUInt16();
			for (int i = 0; i < num; i++)
			{
				Read(reader);
			}
		}
	}

	public static void SerializeRemoved(RocketBinaryWriter writer)
	{
		bool flag = RemovedRockets.Count > 0;
		writer.WriteBoolean(flag);
		if (!flag)
		{
			return;
		}
		writer.WriteUInt16((ushort)RemovedRockets.Count);
		foreach (long removedRocket in RemovedRockets)
		{
			writer.WriteInt64(removedRocket);
		}
		RemovedRockets.Clear();
	}

	public static void DeSerializeRemoved(RocketBinaryReader reader)
	{
		if (reader.ReadBoolean())
		{
			ushort num = reader.ReadUInt16();
			for (int i = 0; i < num; i++)
			{
				Referencable.Find<Rocket>(reader.ReadInt64())?.DeRegister();
			}
		}
	}

	public bool IsNetworkUpdate()
	{
		return NetworkUpdateFlags != 0;
	}

	public static void SerializeDeltaState(RocketBinaryWriter writer)
	{
		ushort num = 0;
		int position = writer.Position;
		writer.WriteUInt16(0);
		foreach (Rocket allRocket in AllRockets)
		{
			if (allRocket != null && allRocket.ReferenceId != 0L && !allRocket.BeingDestroyed && allRocket.IsNetworkUpdate())
			{
				BuildUpdate(writer, allRocket);
				allRocket.NetworkUpdateFlags = 0;
				num++;
			}
		}
		writer.Seek(position, SeekOrigin.Begin);
		writer.WriteUInt16(num);
		writer.Seek(0, SeekOrigin.End);
	}

	public static void DeserializeDeltaState(RocketBinaryReader reader)
	{
		ushort num = reader.ReadUInt16();
		for (int i = 0; i < num; i++)
		{
			ProcessUpdate(reader);
		}
	}

	private static void BuildUpdate(RocketBinaryWriter writer, Rocket rocket)
	{
		Network.WritePackedId(writer, rocket);
		ushort networkUpdateFlags = rocket.NetworkUpdateFlags;
		writer.WriteUInt16(networkUpdateFlags);
		if (IsNetworkUpdateRequired(8, networkUpdateFlags))
		{
			writer.WriteFloatHalf(rocket.Progress);
		}
		if (IsNetworkUpdateRequired(4, networkUpdateFlags))
		{
			Network.WriteNullable(writer, rocket.CurrentTransit);
		}
		if (IsNetworkUpdateRequired(1, networkUpdateFlags))
		{
			Network.WritePackedId(writer, rocket.TargetNode?.ReferenceId ?? 0);
		}
		if (IsNetworkUpdateRequired(16, networkUpdateFlags))
		{
			writer.WriteFloatHalf(rocket.Acceleration);
			writer.WriteFloatHalf(rocket.EngineAcceleration);
		}
		if (IsNetworkUpdateRequired(32, networkUpdateFlags))
		{
			Network.WritePackedId(writer, rocket.CurrentNode?.ReferenceId ?? 0);
		}
		if (IsNetworkUpdateRequired(64, networkUpdateFlags))
		{
			writer.WriteSingle(rocket.DistanceToTarget);
			writer.WriteSingle(rocket.OrbitalPosition);
		}
		if (IsNetworkUpdateRequired(512, networkUpdateFlags))
		{
			writer.WriteVector3(rocket.TargetPosition);
		}
		if (IsNetworkUpdateRequired(128, networkUpdateFlags))
		{
			writer.WriteByte((byte)rocket.RocketState);
		}
		if (IsNetworkUpdateRequired(32768, networkUpdateFlags))
		{
			writer.WriteByte((byte)rocket.RocketMode);
		}
		if (IsNetworkUpdateRequired(1024, networkUpdateFlags))
		{
			writer.WriteString(rocket.CustomName);
		}
		if (IsNetworkUpdateRequired(2048, networkUpdateFlags))
		{
			writer.WriteUInt16(rocket.EstimatedRemainingBurnTime);
		}
		if (IsNetworkUpdateRequired(256, networkUpdateFlags))
		{
			writer.WriteInt32(rocket.RocketParkSlot?.Location.x ?? 0);
			writer.WriteInt32(rocket.RocketParkSlot?.Location.y ?? 0);
		}
		if (IsNetworkUpdateRequired(4096, networkUpdateFlags))
		{
			writer.WriteBoolean(rocket.AutomatedShutOff);
		}
		if (IsNetworkUpdateRequired(8192, networkUpdateFlags))
		{
			writer.WriteBoolean(rocket.AutomatedLanding);
			writer.WriteByte((byte)rocket.ReEntryProfile);
			writer.WriteByte((byte)rocket.FlightControlRule);
			writer.WriteFloatHalf(rocket.MaxRecordedThrust);
		}
		if (IsNetworkUpdateRequired(16384, networkUpdateFlags))
		{
			writer.WriteSingle(rocket.Velocity);
		}
	}

	public static bool IsNetworkUpdateRequired(ushort toCheck, ushort networkUpdateType)
	{
		return (toCheck & networkUpdateType) != 0;
	}

	private static void ProcessUpdate(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out var referenceId);
		ushort networkUpdateType = reader.ReadUInt16();
		Rocket rocket = Referencable.Find<Rocket>(referenceId);
		if (IsNetworkUpdateRequired(8, networkUpdateType))
		{
			float progress = reader.ReadFloatHalf();
			if (rocket != null)
			{
				rocket.Progress = progress;
			}
		}
		if (IsNetworkUpdateRequired(4, networkUpdateType))
		{
			NodeTransit value = null;
			Network.ReadNullable(reader, ref value);
			if (rocket != null)
			{
				rocket.CurrentTransit = value;
			}
		}
		if (IsNetworkUpdateRequired(1, networkUpdateType))
		{
			Network.ReadPackedId(reader, out var referenceId2);
			SpaceMapNode targetNode = Referencable.Find<SpaceMapNode>(referenceId2);
			if (rocket != null)
			{
				rocket.TargetNode = targetNode;
			}
		}
		if (IsNetworkUpdateRequired(16, networkUpdateType))
		{
			float acceleration = reader.ReadFloatHalf();
			float engineAcceleration = reader.ReadFloatHalf();
			if (rocket != null)
			{
				rocket.Acceleration = acceleration;
				rocket.EngineAcceleration = engineAcceleration;
			}
		}
		if (IsNetworkUpdateRequired(32, networkUpdateType))
		{
			Network.ReadPackedId(reader, out var referenceId3);
			SpaceMapNode currentNode = Referencable.Find<SpaceMapNode>(referenceId3);
			rocket?.SetCurrentNode(currentNode);
		}
		if (IsNetworkUpdateRequired(64, networkUpdateType))
		{
			float distanceToTarget = reader.ReadSingle();
			float orbitalPosition = reader.ReadSingle();
			if (rocket != null)
			{
				rocket.DistanceToTarget = distanceToTarget;
				rocket.OrbitalPosition = orbitalPosition;
			}
		}
		if (IsNetworkUpdateRequired(512, networkUpdateType))
		{
			Vector3 targetPosition = reader.ReadVector3();
			if (rocket != null)
			{
				rocket.TargetPosition = targetPosition;
			}
		}
		if (IsNetworkUpdateRequired(128, networkUpdateType))
		{
			RocketState rocketState = (RocketState)reader.ReadByte();
			if (rocket != null)
			{
				rocket.RocketState = rocketState;
			}
		}
		if (IsNetworkUpdateRequired(32768, networkUpdateType))
		{
			RocketMode rocketMode = (RocketMode)reader.ReadByte();
			if (rocket != null)
			{
				rocket.RocketMode = rocketMode;
			}
		}
		if (IsNetworkUpdateRequired(1024, networkUpdateType))
		{
			string customName = reader.ReadString();
			if (rocket != null)
			{
				rocket.CustomName = customName;
			}
		}
		if (IsNetworkUpdateRequired(2048, networkUpdateType))
		{
			ushort estimatedRemainingBurnTime = reader.ReadUInt16();
			if (rocket != null)
			{
				rocket.EstimatedRemainingBurnTime = estimatedRemainingBurnTime;
			}
		}
		if (IsNetworkUpdateRequired(256, networkUpdateType))
		{
			int x = reader.ReadInt32();
			int y = reader.ReadInt32();
			Vector2Int parkSlot = new Vector2Int(x, y);
			rocket?.SetParkSlot(parkSlot);
		}
		if (IsNetworkUpdateRequired(4096, networkUpdateType))
		{
			bool automatedShutOff = reader.ReadBoolean();
			if (rocket != null)
			{
				rocket.AutomatedShutOff = automatedShutOff;
			}
		}
		if (IsNetworkUpdateRequired(8192, networkUpdateType))
		{
			bool automatedLanding = reader.ReadBoolean();
			byte reEntryProfile = reader.ReadByte();
			byte flightControlRule = reader.ReadByte();
			float maxRecordedThrust = reader.ReadFloatHalf();
			if (rocket != null)
			{
				rocket.AutomatedLanding = automatedLanding;
				rocket.ReEntryProfile = (ReEntryProfile)reEntryProfile;
				rocket.FlightControlRule = (FlightControlRule)flightControlRule;
				rocket.MaxRecordedThrust = maxRecordedThrust;
			}
		}
		if (IsNetworkUpdateRequired(16384, networkUpdateType))
		{
			float velocity = reader.ReadSingle();
			if (rocket != null)
			{
				rocket.Velocity = velocity;
			}
		}
	}

	private static void Write(RocketBinaryWriter writer, Rocket rocket)
	{
		bool flag = rocket?.RocketNetwork == null || rocket.ReferenceId == 0;
		writer.WriteBoolean(flag);
		if (!flag)
		{
			Network.WritePackedId(writer, rocket);
			Network.WritePackedId(writer, rocket.RocketNetwork);
			writer.WriteFloatHalf(rocket.Progress);
			Network.WriteNullable(writer, rocket.CurrentTransit);
			Network.WritePackedId(writer, rocket.TargetNode?.ReferenceId ?? 0);
			writer.WriteFloatHalf(rocket.Acceleration);
			writer.WriteFloatHalf(rocket.EngineAcceleration);
			Network.WritePackedId(writer, rocket.CurrentNode?.ReferenceId ?? 0);
			writer.WriteSingle(rocket.DistanceToTarget);
			writer.WriteSingle(rocket.OrbitalPosition);
			writer.WriteByte((byte)rocket.RocketState);
			writer.WriteByte((byte)rocket.RocketMode);
			writer.WriteBoolean(rocket.AutomatedShutOff);
			writer.WriteBoolean(rocket.AutomatedLanding);
			writer.WriteByte((byte)rocket.ReEntryProfile);
			writer.WriteByte((byte)rocket.FlightControlRule);
			writer.WriteFloatHalf(rocket.MaxRecordedThrust);
			writer.WriteSingle(rocket.Velocity);
			bool flag2 = rocket.RocketParentTransform != null;
			writer.WriteBoolean(flag2);
			if (flag2)
			{
				writer.WriteVector3(rocket.LastParentedPosition);
				writer.WriteVector3(rocket.RocketParentTransform.position);
				writer.WriteInt32(rocket.RocketParkSlot?.Location.x ?? 0);
				writer.WriteInt32(rocket.RocketParkSlot?.Location.y ?? 0);
				writer.WriteVector3(rocket.TargetPosition);
			}
			writer.WriteString(rocket.CustomName);
			writer.WriteUInt16(rocket.EstimatedRemainingBurnTime);
		}
	}

	private static Rocket Read(RocketBinaryReader reader)
	{
		if (reader.ReadBoolean())
		{
			return null;
		}
		Network.ReadPackedId(reader, out var referenceId);
		Network.ReadPackedId(reader, out var referenceId2);
		float progress = reader.ReadFloatHalf();
		NodeTransit value = null;
		Network.ReadNullable(reader, ref value);
		Network.ReadPackedId(reader, out var referenceId3);
		SpaceMapNode targetNode = Referencable.Find<SpaceMapNode>(referenceId3);
		float num = reader.ReadFloatHalf();
		float engineAcceleration = reader.ReadFloatHalf();
		Network.ReadPackedId(reader, out var referenceId4);
		SpaceMapNode currentNode = Referencable.Find<SpaceMapNode>(referenceId4);
		float distanceToTarget = reader.ReadSingle();
		float orbitalPosition = reader.ReadSingle();
		RocketState rocketState = (RocketState)reader.ReadByte();
		RocketMode rocketMode = (RocketMode)reader.ReadByte();
		bool automatedShutOff = reader.ReadBoolean();
		bool automatedLanding = reader.ReadBoolean();
		ReEntryProfile reEntryProfile = (ReEntryProfile)reader.ReadByte();
		FlightControlRule flightControlRule = (FlightControlRule)reader.ReadByte();
		float maxRecordedThrust = reader.ReadFloatHalf();
		float num2 = reader.ReadSingle();
		bool flag = reader.ReadBoolean();
		Vector3 savedPositionOnParent = Vector3.zero;
		Vector3 savedTransformPosition = Vector3.zero;
		Vector3 savedTargetPosition = Vector3.zero;
		Vector2Int savedRocketParkLocation = Vector2Int.zero;
		if (flag)
		{
			savedPositionOnParent = reader.ReadVector3();
			savedTransformPosition = reader.ReadVector3();
			int x = reader.ReadInt32();
			int y = reader.ReadInt32();
			savedRocketParkLocation = new Vector2Int(x, y);
			savedTargetPosition = reader.ReadVector3();
		}
		string customName = reader.ReadString();
		ushort estimatedRemainingBurnTime = reader.ReadUInt16();
		RocketNetwork rocketNetwork = Referencable.Find<RocketNetwork>(referenceId2);
		if (rocketNetwork == null)
		{
			return null;
		}
		Rocket rocket = new Rocket(rocketNetwork, referenceId)
		{
			Progress = progress,
			CurrentTransit = value,
			TargetNode = targetNode,
			Acceleration = num,
			EngineAcceleration = engineAcceleration,
			DistanceToTarget = distanceToTarget,
			OrbitalPosition = orbitalPosition,
			CustomName = customName,
			RocketState = rocketState,
			_hasSavedTransform = flag,
			EstimatedRemainingBurnTime = estimatedRemainingBurnTime,
			RocketMode = rocketMode,
			AutomatedShutOff = automatedShutOff,
			AutomatedLanding = automatedLanding,
			ReEntryProfile = reEntryProfile,
			FlightControlRule = flightControlRule,
			MaxRecordedThrust = maxRecordedThrust,
			Velocity = num2,
			_savedAcceleration = num,
			_savedVelocity = num2
		};
		rocket.SetCurrentNode(currentNode);
		if (flag)
		{
			rocket._savedPositionOnParent = savedPositionOnParent;
			rocket._savedTransformPosition = savedTransformPosition;
			rocket._savedTargetPosition = savedTargetPosition;
			rocket._savedRocketParkLocation = savedRocketParkLocation;
		}
		return rocket;
	}

	public void SetParkSlot(Vector2Int location)
	{
		if (location == Vector2Int.zero && RocketParkSlot != null)
		{
			RocketParkSlot.RemoveRocket(this);
		}
		else
		{
			RocketParkSlot.ParkRocket(location, this);
		}
	}

	public void SetRocketSizeToLaunchMount(ISpaceMapNodeOwner owner)
	{
		Thing thing = owner as Thing;
		if (thing != null)
		{
			OnServer.Interact(thing.InteractMode, (int)Size);
		}
	}

	public void OnServerTick(float deltaTime)
	{
		IncrementOrbitalMovement(deltaTime);
		ProcessCurrentAction(deltaTime);
	}

	private void OnRocketModeUpdated(RocketMode previous)
	{
		if (!GameManager.RunSimulation || RocketMode == previous || GameManager.GameState != GameState.Running)
		{
			return;
		}
		if (BeingDestroyed || CurrentNode == null)
		{
			RocketMode = RocketMode.Invalid;
			return;
		}
		if (IsTravelling && RocketMode != RocketMode.None)
		{
			RocketMode = RocketMode.None;
		}
		if (CurrentNode == null || !CurrentNode.HasAction(RocketMode))
		{
			RocketMode = RocketMode.Invalid;
		}
	}

	public void OnActionCompleted()
	{
		if (CurrentAction == null)
		{
			RocketMode = RocketMode.None;
		}
	}

	public List<RocketPayloadBay> GetPayloadBays()
	{
		return RocketNetwork?.RocketPayloadBays ?? null;
	}

	public List<IRocketMiner> GetMiners()
	{
		return RocketNetwork?.RocketMiners ?? null;
	}

	public List<RocketScanner> GetScanners()
	{
		return RocketNetwork?.RocketScanners ?? null;
	}

	public List<IUmbilical> GetUmbilicals()
	{
		return RocketNetwork?.RocketUmbilicals ?? null;
	}

	public void Report(RocketActionResult result)
	{
		if (!result.IsSuccess)
		{
			RocketLog.Append(new ActionReportEvent(this, result)).Forget();
			_lastReportedResult = result;
		}
	}

	public void Report(RocketEvent rocketEvent)
	{
		RocketLog.Append(rocketEvent).Forget();
	}

	public List<IRocketActionProgressable> ActionProgressables()
	{
		_actionProgressables.Clear();
		switch (RocketMode)
		{
		case RocketMode.Mine:
			foreach (IRocketMiner miner in GetMiners())
			{
				_actionProgressables.Add(miner);
			}
			break;
		case RocketMode.Survey:
		case RocketMode.Discover:
		case RocketMode.Chart:
		case RocketMode.SurfaceScan:
			foreach (RocketScanner scanner in GetScanners())
			{
				_actionProgressables.Add(scanner);
			}
			break;
		case RocketMode.Deploy:
			foreach (RocketPayloadBay payloadBay in GetPayloadBays())
			{
				_actionProgressables.Add(payloadBay);
			}
			break;
		case RocketMode.Transfer:
			foreach (IUmbilical umbilical in GetUmbilicals())
			{
				if (umbilical is IRocketActionProgressable item)
				{
					_actionProgressables.Add(item);
				}
			}
			break;
		}
		return _actionProgressables;
	}

	public bool GetOrbitalPosition(out float position)
	{
		position = -1f;
		if (!Mathf.Approximately(Progress, 0f))
		{
			return false;
		}
		if (CurrentNode == null)
		{
			return false;
		}
		if (CurrentNode != CurrentNode.SpaceMap.EntryNode)
		{
			return false;
		}
		position = OrbitalPosition;
		return true;
	}

	private void IncrementOrbitalMovement(float deltaTime)
	{
		if (Mathf.Approximately(Progress, 0f) && CurrentNode != null && CurrentNode == CurrentNode.SpaceMap.EntryNode)
		{
			OrbitalPosition += deltaTime;
		}
	}

	private void ProcessCurrentAction(float deltaTime)
	{
		if (CurrentAction != null && (!CurrentAction.IsRocketMode(RocketMode) || Progress != 0f))
		{
			CurrentAction.Close(this, Progress != 0f);
		}
		RocketMode rocketMode = RocketMode;
		if (rocketMode == RocketMode.None || rocketMode == RocketMode.Invalid || CurrentNode == null)
		{
			return;
		}
		if (CurrentAction == null)
		{
			CurrentAction = CurrentNode.GetAction(RocketMode);
			if (CurrentAction == null)
			{
				return;
			}
			if (!CurrentAction.Evaluate(this, out var result))
			{
				Report(result);
				CurrentAction.Close(this);
				return;
			}
			CurrentAction.Start(this);
		}
		if (!CurrentAction.Evaluate(this, out var result2))
		{
			Report(result2);
			CurrentAction.Close(this);
			return;
		}
		if (!CurrentAction.ProgressAction(deltaTime, this, out var result3))
		{
			Report(result3);
		}
		if (CurrentAction.IsCompleted())
		{
			if (!CurrentAction.Complete(this, out var result4))
			{
				Report(result4);
			}
			OnActionCompleted();
		}
	}

	private void OnRocketStateUpdated(RocketState previous)
	{
		if (previous == RocketState.None || previous == RocketState)
		{
			return;
		}
		_finishMoveToTargetToken?.Cancel();
		RocketState rocketState = RocketState;
		if (rocketState != RocketState.InSpace && rocketState != RocketState.Landing)
		{
			RocketParkSlot.RemoveRocket(this);
		}
		switch (RocketState)
		{
		case RocketState.Launching:
			if (previous == RocketState.OnLaunchMount)
			{
				RocketNetwork.RefreshRocket();
				ReParentRocketParts(CurrentNode.Owner.RocketTransformPosition);
				SetRocketSizeToLaunchMount(CurrentNode.Owner);
				OnLaunch();
				Achievements.AchieveBlastOff();
			}
			break;
		case RocketState.InSpace:
			if (previous == RocketState.Launching)
			{
				DetatchPartsFromGrid();
				RocketParkSlot.ParkRocket(this);
				AttachPartsToGrid();
				LastParentedPosition = RocketParentTransform.position;
				Achievements.AssessGoAtThrottleUp(this);
				Achievements.Increment(Achievements.Stat.TotalRocketLaunches, 1);
			}
			break;
		case RocketState.Landing:
		{
			if (previous != RocketState.InSpace)
			{
				break;
			}
			Velocity = 0f - _currentTransit.GetDistance();
			bool isOrbital = CurrentTransit.Destination.Owner.IsOrbital;
			float num = ReEntryProfiles[ReEntryProfile];
			if (isOrbital)
			{
				float minRequiredThrust;
				float autoLandConfidenceRatio = GetAutoLandConfidenceRatio(SpaceMap.Current.DistanceToOrbit * 2f, WorldSetting.Current.Gravity, num, out minRequiredThrust);
				for (int num2 = (int)SpaceMap.Current.DistanceToOrbit - 1; num2 >= 0; num2--)
				{
					float altitude = num * ((float)num2 / SpaceMap.Current.DistanceToOrbit);
					float distance = CurrentTransit.GetDistance();
					if (GetAutoLandConfidenceRatio(distance, GetGravity().value, altitude, out minRequiredThrust) <= autoLandConfidenceRatio)
					{
						num = (float)(num2 + 1) / SpaceMap.Current.DistanceToOrbit * num;
						break;
					}
				}
			}
			Vector3 rocketTransformPosition = CurrentTransit.Destination.Owner.RocketTransformPosition;
			TargetPosition = rocketTransformPosition + new Vector3(0f, num, 0f);
			RocketParentTransform.position = rocketTransformPosition + new Vector3(0f, Mathf.Min(num, 1000f), 0f);
			SetRocketSizeToLaunchMount(CurrentTransit?.Destination?.Owner);
			InitAutomatedLanding();
			break;
		}
		case RocketState.OnLaunchMount:
			if (previous == RocketState.Landing)
			{
				if ((object)RocketParentTransform != null)
				{
					RocketParentTransform.position = TargetPosition;
					RocketNetwork.UpdateStructurePositions();
				}
				DetatchPartsFromGrid();
				SetRocketSizeToLaunchMount(CurrentNode?.Owner);
				UnParentRocketParts();
				AttachPartsToGrid();
				OnLanded();
				Achievements.Increment(Achievements.Stat.TotalRocketLandings, 1);
			}
			break;
		}
	}

	public float EstimatedTimeToTargetSeconds()
	{
		switch (RocketState)
		{
		case RocketState.None:
			return 0f;
		case RocketState.OnLaunchMount:
		case RocketState.Launching:
		case RocketState.InSpace:
			if (!(Acceleration < float.Epsilon))
			{
				return DistanceToTarget / Acceleration;
			}
			return 0f;
		case RocketState.Landing:
			return TimeUntiImpact(GetAltitude(), Velocity, Acceleration);
		default:
			return 0f;
		}
	}

	public float EstimatedTimeToNextTargetSeconds()
	{
		switch (RocketState)
		{
		case RocketState.None:
			return 0f;
		case RocketState.OnLaunchMount:
		case RocketState.Launching:
		case RocketState.InSpace:
			if (!(Acceleration < float.Epsilon))
			{
				return DistanceToNextTarget / Acceleration;
			}
			return 0f;
		case RocketState.Landing:
			return TimeUntiImpact(GetAltitude(), Velocity, Acceleration);
		default:
			return 0f;
		}
	}

	public float GetMapProgress()
	{
		RocketState rocketState = RocketState;
		if ((uint)rocketState > 3u && rocketState == RocketState.Landing)
		{
			return 1f - GetAltitude() / ReEntryProfiles[ReEntryProfile];
		}
		return Progress;
	}

	public float TimeUntiImpact(float altitude, float velocity, float acceleration)
	{
		if (velocity >= 0f)
		{
			return -1f;
		}
		float num = 0.5f * (0f - acceleration);
		float num2 = 0f - velocity;
		float num3 = 0f - altitude;
		float num4 = (0f - num2 + Mathf.Sqrt(Mathf.Pow(num2, 2f) - 4f * num * num3)) / (2f * num);
		if (float.IsNaN(num4))
		{
			return -1f;
		}
		return num4;
	}

	public float CalculateImpactVelocity()
	{
		return ImpactVelocity(GetAltitude(), Velocity, Acceleration);
	}

	private float ImpactVelocity(float altitude, float velocity, float acceleration)
	{
		if (velocity >= 0f)
		{
			return float.NaN;
		}
		if (GetApex(altitude, velocity, acceleration) > 0f)
		{
			return 0f;
		}
		float num = TimeUntiImpact(altitude, velocity, acceleration);
		return velocity + acceleration * num;
	}

	public float TimeToApex()
	{
		return TimeToVerticalArrest(Velocity, Acceleration);
	}

	public static float TimeToVerticalArrest(float velocity, float acceleration)
	{
		if (velocity >= 0f)
		{
			return -1f;
		}
		float num = 0.5f * (0f - acceleration);
		return 0f - (0f - velocity) / (2f * num);
	}

	public float ApexAltitude()
	{
		return GetApex(GetAltitude(), Velocity, Acceleration);
	}

	public static float GetApex(float altitude, float velocity, float acceleration)
	{
		if (velocity > 0f && acceleration > 0f)
		{
			return float.NaN;
		}
		float num = 0.5f * (0f - acceleration);
		float num2 = 0f - velocity;
		float num3 = 0f - altitude;
		float num4 = TimeToVerticalArrest(velocity, acceleration);
		float num5 = Mathf.Pow(num4, 2f) * num + num4 * num2 + num3;
		if (acceleration < 0f)
		{
			return float.NegativeInfinity;
		}
		return 0f - num5;
	}

	public float TargetVelocity()
	{
		switch (RocketState)
		{
		case RocketState.Launching:
		case RocketState.InSpace:
			return CurrentTransit?.GetDistance() ?? 0f;
		default:
			return 0f;
		}
	}

	public int BatteryPercentage()
	{
		float num = 0f;
		float num2 = 0f;
		foreach (Battery battery in RocketNetwork.Batteries)
		{
			num2 += battery.PowerMaximum;
			num += battery.AvailablePower;
		}
		if (num2 == 0f)
		{
			return 0;
		}
		return (int)(num / num2 * 100f);
	}

	private bool CompareNodeTransit(NodeTransit a, NodeTransit b)
	{
		if (a.Destination == b.Destination)
		{
			return a.From == b.From;
		}
		return false;
	}

	public static bool IsValidMannedTarget(SpaceMapNode node)
	{
		if (node != null)
		{
			NodeType nodeType = node.NodeType;
			return nodeType == NodeType.LaunchPad || nodeType == NodeType.LowOrbitLaunchPad;
		}
		return true;
	}

	public void EnforceMannedTargetRestriction()
	{
		if (GameManager.RunSimulation && TargetNode != null && IsManned && !IsValidMannedTarget(TargetNode))
		{
			ChangeTarget(null);
		}
	}

	public void ChangeTarget(SpaceMapNode target)
	{
		if (target == TargetNode)
		{
			return;
		}
		TargetNode = target;
		if (CurrentNode == TargetNode && Progress == 0f && RocketState != RocketState.Landing)
		{
			TargetNode = null;
			CurrentTransit = null;
			AllPaths = null;
			return;
		}
		NodeTransit next;
		List<NodeTransit> overallPath;
		bool nextConnection = SpaceMapPathFinder.GetNextConnection(CurrentNode, TargetNode, out next, out overallPath);
		if (Progress == 0f && RocketState != RocketState.Landing)
		{
			if (nextConnection)
			{
				AllPaths = overallPath;
				CurrentTransit = next;
			}
			else
			{
				TargetNode = null;
				CurrentTransit = null;
				AllPaths = null;
			}
		}
		else
		{
			if (nextConnection && CompareNodeTransit(CurrentTransit, next))
			{
				return;
			}
			if (SpaceMapPathFinder.GetNextConnection(CurrentTransit.Destination, CurrentNode, out var next2, out var overallPath2))
			{
				SetCurrentNode(CurrentTransit.Destination);
				CurrentTransit = next2;
				AllPaths = overallPath2;
				if (RocketState == RocketState.Launching)
				{
					RocketState = RocketState.Landing;
					Progress = 0f;
				}
				else if (RocketState == RocketState.Landing)
				{
					RocketState = RocketState.Launching;
					Progress = Velocity / CurrentTransit.GetDistance();
				}
				else
				{
					Progress = 1f - Progress;
				}
			}
			else
			{
				TargetNode = null;
				CurrentTransit = null;
				AllPaths = null;
			}
		}
	}

	public float GetAltitude()
	{
		return RocketState switch
		{
			RocketState.OnLaunchMount => 0f, 
			RocketState.Launching => (CurrentNode?.Owner != null) ? (TargetPosition.y - CurrentNode.Owner.RocketTransformPosition.y) : TargetPosition.y, 
			RocketState.Landing => (CurrentTransit?.Destination?.Owner != null) ? (TargetPosition.y - CurrentTransit.Destination.Owner.RocketTransformPosition.y) : TargetPosition.y, 
			RocketState.None => -1f, 
			RocketState.InSpace => -1f, 
			_ => -1f, 
		};
	}

	public void PhysicsUpdate()
	{
		if (!_initialised)
		{
			return;
		}
		if (!GameManager.RunSimulation)
		{
			if (!BeingDestroyed)
			{
				MoveToTarget();
			}
			return;
		}
		if (RocketNetwork.Engines.Count == 0)
		{
			MaxRecordedThrust = 0f;
		}
		else
		{
			MaxRecordedThrust = Mathf.Max(GetThrust(), MaxRecordedThrust);
		}
		if (TargetNode == null || CurrentNode == null || BeingDestroyed)
		{
			Acceleration = 0f;
			Progress = 0f;
			_distanceToTarget = 0f;
			EstimatedRemainingBurnTime = 0;
			StopLandingAudio();
			return;
		}
		if (_finishMoveToTargetToken.Initialized)
		{
			StopLandingAudio();
			return;
		}
		EvaluateLaunchOrLandState();
		SetRocketTargetPosition();
		MoveToTarget();
		if (Progress >= 1f)
		{
			NodeType nodeType = CurrentTransit.Destination.NodeType;
			if (nodeType == NodeType.LaunchPad || nodeType == NodeType.LowOrbitLaunchPad)
			{
				if (_finishMoveToTargetToken.Initialized)
				{
					_finishMoveToTargetToken.Cancel();
				}
				_finishMoveToTargetToken.Initialize();
				FinishMovingToTarget(_finishMoveToTargetToken.Token).Forget();
			}
		}
		if (CurrentTransit == null && SpaceMapPathFinder.GetNextConnection(CurrentNode, TargetNode, out var next, out var overallPath))
		{
			CurrentTransit = next;
			AllPaths = overallPath;
		}
		if (Progress >= 1f)
		{
			NodeType nodeType = CurrentNode.NodeType;
			if ((nodeType == NodeType.LaunchPad || nodeType == NodeType.LowOrbitLaunchPad) && RocketState != RocketState.InSpace)
			{
				RocketState = RocketState.InSpace;
			}
		}
		if (Progress >= 1f && CurrentTransit != null)
		{
			SetCurrentNode(CurrentTransit.Destination);
			CurrentTransit = null;
			Progress = 0f;
		}
		if (CurrentNode == TargetNode)
		{
			TargetReached();
			Reset();
		}
		if (CurrentTransit != null && CurrentTransit.GetDistance() > 0f)
		{
			float num = TotalMass();
			float thrust = GetThrust();
			EngineAcceleration = thrust / num;
			float num2 = EngineAcceleration * Time.fixedDeltaTime;
			switch (RocketState)
			{
			case RocketState.None:
				Velocity = 0f;
				break;
			case RocketState.OnLaunchMount:
				num2 += GetGravity().value * Time.fixedDeltaTime;
				Acceleration = num2 / Time.fixedDeltaTime;
				Velocity += num2;
				if (Acceleration < 0f)
				{
					Velocity = 0f;
					Acceleration = 0f;
				}
				else
				{
					Progress += num2 / CurrentTransit.GetDistance();
				}
				break;
			case RocketState.Launching:
			{
				num2 += GetGravity().value * Time.fixedDeltaTime;
				Acceleration = num2 / Time.fixedDeltaTime;
				Velocity += num2;
				float apex = GetApex(GetAltitude(), Velocity, Acceleration);
				CheckForOverlap();
				if ((Velocity < 0f && float.IsNegativeInfinity(apex)) || apex < 0f)
				{
					ChangeTarget(CurrentNode);
					if (AutomatedLanding)
					{
						InitAutomatedLanding();
					}
					return;
				}
				Progress += num2 / CurrentTransit.GetDistance();
				break;
			}
			case RocketState.Landing:
				num2 += GetGravity().value * Time.fixedDeltaTime;
				Acceleration = num2 / Time.fixedDeltaTime;
				Velocity += num2;
				Progress = 0f;
				CheckForOverlap();
				break;
			default:
				Velocity += num2;
				Acceleration = num2 / Time.fixedDeltaTime;
				Progress += num2 / CurrentTransit.GetDistance();
				break;
			}
			DistanceToTarget = GetRemainingDistance();
		}
		else
		{
			Acceleration = 0f;
			DistanceToTarget = 0f;
			Velocity = 0f;
		}
	}

	private void LandingAudio()
	{
		if (GameManager.IsBatchMode)
		{
			return;
		}
		float altitude = GetAltitude();
		float min = 1600f;
		float num = 2500f;
		if (Velocity > -25f || altitude > num || TargetNode?.Owner is LaunchMount { IsOrbital: not false })
		{
			StopLandingAudio();
			return;
		}
		if (!_airMovementAudio)
		{
			_airMovementAudio = Singleton<AudioManager>.Instance.PlayAudioClipsData(this, RocketFallHash, Vector3.zero);
		}
		float num2 = Mathf.Clamp01(Velocity / -100f);
		float num3 = RocketMath.MapToScaleClamp(min, num, 0f, 1f, altitude);
		float num4 = 0.25f + (1f - num3);
		float volumeMultiplier = (0.25f + num2) * num4;
		_airMovementAudio.GameAudioSource.SetVolumeMultiplier(RocketFallHash, volumeMultiplier);
	}

	private void StopLandingAudio(bool immediate = true)
	{
		_airMovementAudio?.Stop(immediate);
		_airMovementAudio = null;
	}

	private void EvaluateLaunchOrLandState()
	{
		bool flag;
		switch (CurrentNode?.NodeType)
		{
		case NodeType.LaunchPad:
		case NodeType.LowOrbitLaunchPad:
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag && Progress > 0f && RocketState != RocketState.Launching)
		{
			RocketState = RocketState.Launching;
			return;
		}
		switch (CurrentTransit?.Destination.NodeType)
		{
		case NodeType.LaunchPad:
		case NodeType.LowOrbitLaunchPad:
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (!flag || !(Progress > 0f) || RocketState == RocketState.Landing)
		{
			return;
		}
		if (AutomatedLanding && RocketState == RocketState.InSpace)
		{
			bool isOrbital = CurrentTransit.Destination.Owner.IsOrbital;
			float distance = CurrentTransit.GetDistance();
			float value = (isOrbital ? (-1f) : WorldSetting.Current.Gravity);
			value = Mathf.Clamp(value, -5.5f, -1f);
			float altitude = ReEntryProfiles[ReEntryProfile];
			if (GetAutoLandConfidenceRatio(distance, value, altitude, out var _) <= 0f)
			{
				ChangeTarget(CurrentTransit.From);
				RocketLog.Append(new RocketLandAbortedEvent(this)).Forget();
				return;
			}
		}
		RocketState = RocketState.Landing;
	}

	private void SnapToLaunchMount()
	{
		RocketParentTransform.position = TargetPosition;
		RocketNetwork.UpdateStructurePositions();
		RocketState = RocketState.OnLaunchMount;
	}

	private async UniTaskVoid FinishMovingToTarget(CancellationToken token)
	{
		while (!RocketMath.Approximately(TargetPosition, RocketParentTransform.position, 0.01f) && !token.IsCancellationRequested)
		{
			await UniTask.WaitForFixedUpdate(token);
			RocketParentTransform.position = Vector3.Lerp(RocketParentTransform.position, TargetPosition, Time.fixedDeltaTime * 3f);
			RocketNetwork.UpdateStructurePositions();
		}
		SnapToLaunchMount();
		_finishMoveToTargetToken.Cancel();
	}

	private void SetRocketTargetPosition()
	{
		switch (RocketState)
		{
		case RocketState.OnLaunchMount:
			TargetPosition = CurrentNode?.Owner?.RocketTransformPosition ?? TargetPosition;
			break;
		case RocketState.Launching:
		{
			Vector3 targetPosition3 = TargetPosition;
			Vector3 targetPosition4 = new Vector3(targetPosition3.x, targetPosition3.y + Velocity * Time.fixedDeltaTime, targetPosition3.z);
			TargetPosition = targetPosition4;
			break;
		}
		case RocketState.InSpace:
			TargetPosition = RocketParkSlot?.GetWorldPosition() ?? TargetPosition;
			break;
		case RocketState.Landing:
		{
			Vector3 targetPosition = TargetPosition;
			Vector3 targetPosition2 = new Vector3(targetPosition.x, targetPosition.y + Velocity * Time.fixedDeltaTime, targetPosition.z);
			TargetPosition = targetPosition2;
			if (GetAltitude() < 0.1f && Velocity > -2f)
			{
				if (TargetNode?.Owner == null)
				{
					ExplodeRocket(targetPosition);
				}
				else
				{
					SoftLanding();
				}
			}
			break;
		}
		case RocketState.None:
			break;
		}
	}

	private void MoveToTarget()
	{
		switch (RocketState)
		{
		case RocketState.None:
			StopLandingAudio();
			break;
		case RocketState.OnLaunchMount:
		case RocketState.InSpace:
			if ((bool)RocketParentTransform)
			{
				RocketParentTransform.position = TargetPosition;
				RocketNetwork.UpdateStructurePositions();
			}
			StopLandingAudio();
			break;
		case RocketState.Launching:
			if ((bool)RocketParentTransform)
			{
				Vector3 vector3 = Vector3.zero;
				if (CurrentNode?.Owner != null)
				{
					vector3 = CurrentNode.Owner.RocketTransformPosition;
				}
				float b2 = vector3.y + 1000f;
				Vector3 position = Vector3.Lerp(b: new Vector3(TargetPosition.x, Mathf.Min(TargetPosition.y, b2), TargetPosition.z), a: RocketParentTransform.position, t: Time.fixedDeltaTime * 2f);
				RocketParentTransform.position = position;
				RocketNetwork.UpdateStructurePositions();
				StopLandingAudio();
			}
			break;
		case RocketState.Landing:
		{
			if (!RocketParentTransform)
			{
				break;
			}
			LandingAudio();
			Vector3 vector = Vector3.zero;
			if (TargetNode?.Owner != null)
			{
				vector = TargetNode.Owner.RocketTransformPosition;
			}
			float b = vector.y + 1000f;
			Vector3 vector2 = Vector3.Lerp(b: new Vector3(TargetPosition.x, Mathf.Min(TargetPosition.y, b), TargetPosition.z), a: RocketParentTransform.position, t: Time.fixedDeltaTime * 2f);
			if (GameManager.RunSimulation)
			{
				bool num = vector2.y < vector.y;
				if (num)
				{
					vector2 = new Vector3(vector2.x, vector.y, vector2.z);
				}
				RocketParentTransform.position = vector2;
				RocketNetwork.UpdateStructurePositions();
				if (num)
				{
					TargetPosition = vector2;
					CrashLanding();
				}
			}
			else
			{
				RocketParentTransform.position = vector2;
				RocketNetwork.UpdateStructurePositions();
			}
			break;
		}
		}
	}

	private void CheckForOverlap()
	{
		StructureFuselage structureFuselage = null;
		StructureFuselage structureFuselage2 = null;
		float num = float.MaxValue;
		float num2 = float.MinValue;
		foreach (INetworkedStructure structure in RocketNetwork.StructureList)
		{
			if (structure.GetAsThing.Position.y > num2)
			{
				structureFuselage = structure as StructureFuselage;
				num2 = structure.GetAsThing.Position.y;
			}
			if (structure.GetAsThing.Position.y < num)
			{
				structureFuselage2 = structure as StructureFuselage;
				num = structure.GetAsThing.Position.y;
			}
		}
		if (!structureFuselage || !structureFuselage2)
		{
			return;
		}
		Vector3 vector = structureFuselage2.Position + Vector3.down;
		Vector3 vector2 = structureFuselage.Position + Vector3.up;
		bool num3 = VoxelTerrain.GetDensityWorldSpace(vector2) > 0.9f;
		bool flag = VoxelTerrain.GetDensityWorldSpace(vector) > 0.9f;
		if (num3 || flag)
		{
			RocketLog.Append(new RocketCrashedEvent(this, null)).Forget();
			ExplodeRocket(RocketParentTransform.position);
		}
		int num4 = Physics.OverlapSphereNonAlloc(vector, 0.9f, _engineOverlap, _mask, QueryTriggerInteraction.Ignore);
		int num5 = Physics.OverlapSphereNonAlloc(vector2, 0.9f, _noseConeOverlap, _mask, QueryTriggerInteraction.Ignore);
		for (int i = 0; i < num4; i++)
		{
			if (IsCollision(_engineOverlap[i], out var collidedStructure))
			{
				RocketLog.Append(new RocketCrashedEvent(this, collidedStructure)).Forget();
				ExplodeRocket(RocketParentTransform.position);
				return;
			}
		}
		for (int j = 0; j < num5; j++)
		{
			if (IsCollision(_noseConeOverlap[j], out var collidedStructure2))
			{
				RocketLog.Append(new RocketCrashedEvent(this, collidedStructure2)).Forget();
				ExplodeRocket(RocketParentTransform.position);
				break;
			}
		}
	}

	private bool IsCollision(Collider collider, out Structure collidedStructure)
	{
		collidedStructure = Thing.Find(collider) as Structure;
		if (!collidedStructure)
		{
			return false;
		}
		if (collidedStructure is LaunchMount)
		{
			return false;
		}
		if (collidedStructure is StructureFuselage structureFuselage)
		{
			Rocket rocket = structureFuselage.RocketNetwork?.Rocket;
			if (rocket != null && rocket != this)
			{
				rocket.ExplodeRocket(structureFuselage.ThingTransformPosition);
				return true;
			}
			return false;
		}
		if (collidedStructure is IRocketInternals rocketInternals)
		{
			Rocket rocket2 = rocketInternals.RocketNetwork?.Rocket;
			if (rocket2 != null && rocket2 != this)
			{
				rocket2.ExplodeRocket(rocketInternals.ThingTransformPosition);
				return true;
			}
			return false;
		}
		collidedStructure.DamageState.Damage(ChangeDamageType.Increment, 1000f, DamageUpdateType.Brute);
		return true;
	}

	private void SoftLanding()
	{
		Progress = 1f;
		Velocity = 0f;
		Vector3 rocketTransformPosition = TargetNode.Owner.RocketTransformPosition;
		TargetPosition = rocketTransformPosition;
	}

	private void CrashLanding()
	{
		Progress = 1f;
		float velocity = Velocity;
		if (!(velocity > -4f))
		{
			if (velocity > -25f)
			{
				DamageRocket(Velocity);
			}
			else
			{
				Explode(RocketParentTransform.position);
				AbandonRocket();
			}
		}
		Velocity = 0f;
	}

	public void ExplodeRocket(Vector3 position)
	{
		if (!BeingDestroyed)
		{
			Explode(position);
			AbandonRocket();
		}
	}

	private void Explode(Vector3 position)
	{
		GasMixture gasMixture = GasMixtureHelper.Create();
		gasMixture.Add(new Mole(Chemistry.GasType.Methane, TotalMolesVolatiles(), MoleEnergy.Zero));
		gasMixture.Add(new Mole(Chemistry.GasType.Oxygen, TotalMolesOxidizer(), MoleEnergy.Zero));
		gasMixture.AddEnergy(IdealGas.Energy(gasMixture.HeatCapacity, new TemperatureKelvin(1000.0)));
		AtmosphericEventInstance.CloneGlobalAddGasMix(new WorldGrid(position), gasMixture, spark: true);
		float num = RocketMath.MapToScaleClamp(0f, 100000f, 0f, 100f, (TotalMolesOxidizer() + TotalMolesVolatiles()).ToFloat());
		float value = Mathf.Abs(Velocity);
		float num2 = RocketMath.MapToScaleClamp(0f, 400f, 0f, 100f, value);
		float force = (num + num2) / 2f * 100f;
		float radius = RocketMath.MapToScaleClamp(-25f, -50f, 3f, 6f, Velocity);
		Explosion.Explode(force, position, radius, float.MaxValue, mineTerrain: true);
		AudioEvent.Create(position, RocketExplodeCloseHash);
		AudioEvent.Create(position, RocketExplodeDistantHash);
	}

	private void DamageRocket(float crashVelocity)
	{
		for (int num = RocketNetwork.Engines.Count - 1; num >= 0; num--)
		{
			((Thing)RocketNetwork.Engines[num])?.DamageState?.Damage(ChangeDamageType.Increment, crashVelocity * 10f, DamageUpdateType.Brute);
		}
	}

	public void TeleportToEndOfPath()
	{
	}

	public float GetRemainingDistance()
	{
		if (AllPaths == null || AllPaths.Count == 0)
		{
			return 0f;
		}
		List<NodeTransit> allPaths = AllPaths;
		float num = allPaths[allPaths.Count - 1].GetDistance() * (1f - Progress);
		if (AllPaths.Count > 1)
		{
			for (int i = 0; i < AllPaths.Count - 1; i++)
			{
				num += AllPaths[i].GetDistance();
			}
		}
		return num;
	}

	public void Reset()
	{
		TargetNode = null;
		CurrentTransit = null;
		Progress = 0f;
		Velocity = 0f;
		Acceleration = 0f;
		EngineAcceleration = 0f;
	}

	public float GetAutoLandConfidenceRatio(float deltaV, float gravity, float altitude, out float minRequiredThrust)
	{
		minRequiredThrust = float.PositiveInfinity;
		if (!AutomatedLanding)
		{
			return 0f;
		}
		float velocity = 0f - deltaV;
		float maxExpectedThrust = GetMaxExpectedThrust();
		float num = TotalMass();
		float num2 = Mathf.Clamp(gravity, -5.5f, -1f);
		int num3 = 50;
		int num4 = 0;
		for (int num5 = 100; num5 >= num3; num5--)
		{
			float num6 = (float)num5 / 100f;
			float acceleration = maxExpectedThrust * num6 / num + num2;
			float apex = GetApex(altitude, velocity, acceleration);
			if (float.IsNaN(apex) || float.IsNegativeInfinity(apex) || !(apex > 60f))
			{
				break;
			}
			num4++;
			minRequiredThrust = maxExpectedThrust * num6;
		}
		return (float)num4 / (float)num3;
	}

	private void InitAutomatedLanding()
	{
		if (AutomatedLanding)
		{
			_highestRecordedThrust = 0f;
			TurnOnEngines();
			if (Velocity <= 0f - CurrentTransit.GetDistance())
			{
				TrimThrottle(100f);
			}
		}
	}

	private void HandleAutomatedLanding()
	{
		if (FlightControlRule != FlightControlRule.None)
		{
			float altitude = GetAltitude();
			_highestRecordedThrust = Mathf.Max(_highestRecordedThrust, GetThrust());
			FlightControlRule = GetFlightControlRule(out var decentApex);
			switch (FlightControlRule)
			{
			case FlightControlRule.None:
				break;
			case FlightControlRule.Normal:
				NormalRuleThrottle(decentApex, TargetApex(altitude), altitude);
				break;
			case FlightControlRule.Alternate:
				AlternateThrottleControl();
				break;
			case FlightControlRule.Alternate2:
				Alternate2ThrottleControl(2f, altitude);
				break;
			case FlightControlRule.FinalApproach:
				FinalDecent(decentApex, altitude, Velocity);
				break;
			default:
				throw new NotImplementedException($"Flight Control Rule {FlightControlRule} not implemented!");
			}
		}
	}

	private float TargetApex(float currentAltitude)
	{
		return Mathf.Max(currentAltitude / 50f, 40f);
	}

	private FlightControlRule GetFlightControlRule(out float decentApex)
	{
		float velocity = Velocity;
		float acceleration = Acceleration;
		float altitude = GetAltitude();
		decentApex = GetApex(altitude, velocity, acceleration);
		if (altitude < 60f && Velocity > -4f)
		{
			return FlightControlRule.FinalApproach;
		}
		if (!float.IsNaN(decentApex) && !float.IsNegativeInfinity(decentApex) && (TargetApex(altitude) - decentApex < altitude / 2f || (decentApex > 0f && velocity < (0f - CurrentTransit.GetDistance()) / 2f)))
		{
			return FlightControlRule.Normal;
		}
		if ((!float.IsNegativeInfinity(decentApex) && !float.IsNaN(decentApex)) || (!float.IsNegativeInfinity(decentApex) && decentApex < 0f - altitude))
		{
			return FlightControlRule.Alternate;
		}
		return FlightControlRule.Alternate2;
	}

	private void NormalRuleThrottle(float arrestAltitude, float targetAltitude, float altitude)
	{
		TurnOnEngines();
		float thrust = GetThrust();
		float throttle = GetThrottle();
		float num = 1f;
		float a = 1f;
		if (throttle >= 1f && thrust >= 1f)
		{
			float num2 = GetThrust() / GetThrottle();
			float num3 = Mathf.Abs(Weight());
			num = 0.25f * num3 / num2;
			a = num / 200f;
		}
		_highestRecordedThrust = Mathf.Max(_highestRecordedThrust, GetThrust());
		float t = Mathf.Abs((arrestAltitude - targetAltitude) / altitude) * 1f;
		float num4 = Mathf.Lerp(a, num, t);
		TrimThrottle((arrestAltitude < targetAltitude) ? num4 : (0f - num4));
	}

	private void FinalDecent(float arrestAltitude, float altitude, float velocity)
	{
		TurnOnEngines();
		float t = altitude / 60f;
		float num = Mathf.Lerp(-1f, -4f, t);
		float num2 = Mathf.Lerp(-0.5f, -2f, t);
		float num3 = GetThrust() / GetThrottle();
		float num4 = Mathf.Abs(Weight());
		float num5 = 0.999f;
		if (arrestAltitude < -2f || velocity < num)
		{
			num5 = Mathf.Lerp(1f, 1.2f, (velocity - num) / Mathf.Clamp(num, -4f, -1f));
		}
		else if (arrestAltitude > -0.1f || velocity > num2)
		{
			num5 = Mathf.Lerp(0.799f, 0.999f, (velocity - num2) / Mathf.Clamp(num2, -2f, -1f));
		}
		if (altitude < 0.5f && velocity > -2f)
		{
			num5 = 1f;
		}
		float throttle = num5 * num4 / num3;
		SetThrottle(throttle);
	}

	private void AlternateThrottleControl()
	{
		TurnOnEngines();
		TrimThrottle(10f);
	}

	private void Alternate2ThrottleControl(float safetyMargin, float currentAltitude)
	{
		TurnOnEngines();
		float thrust = GetThrust();
		float throttle = GetThrottle();
		float b = 1f;
		if (thrust > 1f && throttle > 1f)
		{
			float num = thrust / throttle;
			b = Mathf.Abs(Weight()) * 0.25f / num;
		}
		float num2 = Mathf.Min(GetMaxExpectedThrust(), _highestRecordedThrust);
		num2 /= safetyMargin;
		float num3 = TargetDecentVelocity(num2, TargetApex(currentAltitude));
		float num4 = Mathf.Abs(num3) * safetyMargin;
		if (Velocity > 0f)
		{
			TrimThrottle(-100f);
			return;
		}
		if (GetThrottle() < 1f)
		{
			SetThrottle(1f);
			return;
		}
		if (Velocity > num3)
		{
			float t = (Velocity - num3) / num4;
			float num5 = Mathf.Lerp(0.05f, b, t);
			TrimThrottle(0f - num5);
		}
		if (Velocity < num3)
		{
			float num6 = (num3 - Velocity) / num4;
			float throttleAmount = Mathf.Lerp(0.05f, b, num6 * 2f);
			TrimThrottle(throttleAmount);
		}
	}

	private void TurnOnEngines()
	{
		foreach (IRocketEngine engine in RocketNetwork.Engines)
		{
			if (engine is Thing { OnOff: false })
			{
				engine.SetLogicValue(LogicType.On, 1.0);
			}
		}
	}

	private void TurnOffEngines()
	{
		foreach (IRocketEngine engine in RocketNetwork.Engines)
		{
			if (engine is Thing { OnOff: not false })
			{
				engine.SetLogicValue(LogicType.On, 0.0);
			}
		}
	}

	public float GetMaxExpectedThrust()
	{
		float maxRecordedThrust = RocketNetwork.Rocket.MaxRecordedThrust;
		foreach (IRocketEngine engine in RocketNetwork.Engines)
		{
			if (engine != null)
			{
				return Mathf.Max(maxRecordedThrust, engine.MaxThrust);
			}
		}
		return 0f;
	}

	private void TrimThrottle(float throttleAmount)
	{
		foreach (IRocketEngine engine in RocketNetwork.Engines)
		{
			double logicValue = engine.GetLogicValue(LogicType.Throttle);
			engine.SetLogicValue(LogicType.Throttle, logicValue + (double)throttleAmount);
		}
	}

	private float GetThrottle()
	{
		using (List<IRocketEngine>.Enumerator enumerator = RocketNetwork.Engines.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				return (float)enumerator.Current.GetLogicValue(LogicType.Throttle);
			}
		}
		return 0f;
	}

	private void SetThrottle(float value)
	{
		foreach (IRocketEngine engine in RocketNetwork.Engines)
		{
			engine.SetLogicValue(LogicType.Throttle, value);
		}
	}

	private float TargetDecentVelocity(float thrust, float altitude)
	{
		float num = Weight();
		float num2 = Mathf.Max(1.01f, thrust / num);
		float num3 = num * num2 - num;
		return 0f - Mathf.Sqrt(altitude / (TotalMass() / (2f * num3)));
	}

	public float GetLastCalculatedThrust()
	{
		AtLeastOneEngineIsOn();
		_lastCalculatedThrust = GetThrust();
		return _lastCalculatedThrust;
	}

	public float GetLastCalculatedThrustToWeight()
	{
		if (AtLeastOneEngineIsOn())
		{
			_lastCalculatedThrustToWeight = ThrustToWeightRatio();
		}
		return _lastCalculatedThrustToWeight;
	}

	public float GetLastCalculatedFuelTime()
	{
		if (AtLeastOneEngineIsOn())
		{
			_lastCalculatedFuelTime = EstimatedRemainingBurnTimeSeconds;
		}
		return _lastCalculatedFuelTime;
	}

	public float GetLastCalculatedAcceleration()
	{
		if (AtLeastOneEngineIsOn())
		{
			_lastCalculatedAcceleration = Acceleration;
		}
		return _lastCalculatedAcceleration;
	}

	private bool AtLeastOneEngineIsOn()
	{
		for (int num = RocketNetwork.Engines.Count - 1; num >= 0; num--)
		{
			IRocketEngine rocketEngine = RocketNetwork.Engines[num];
			if (rocketEngine != null && ((RocketEngineBase)rocketEngine).OnOff)
			{
				return true;
			}
		}
		return false;
	}

	public float GetThrust()
	{
		float num = 0f;
		for (int num2 = RocketNetwork.Engines.Count - 1; num2 >= 0; num2--)
		{
			IRocketEngine rocketEngine = RocketNetwork.Engines[num2];
			if (rocketEngine != null)
			{
				num += rocketEngine.Force;
			}
		}
		return num;
	}

	private void TargetReached()
	{
		RocketLog.Append(new ReachedDestinationEvent(this, CurrentNode)).Forget();
		OnTargetReached?.Invoke();
		CurrentNode?.Data?.AchievementReached?.Execute();
		RocketMode = RocketMode.None;
		StopLandingAudio();
		if (!AutomatedShutOff)
		{
			return;
		}
		NodeType nodeType = CurrentNode.NodeType;
		if (nodeType == NodeType.LaunchPad || nodeType == NodeType.LowOrbitLaunchPad)
		{
			return;
		}
		foreach (IRocketInternals @internal in RocketNetwork.Internals)
		{
			if (@internal is RocketAvionicsDevice rocketAvionicsDevice)
			{
				rocketAvionicsDevice.RunAutoShutOff();
			}
		}
	}

	public static void RocketAtmospherics()
	{
		for (int num = AllRockets.Count - 1; num >= 0; num--)
		{
			AllRockets[num]?.OnPreAtmosphere();
		}
	}

	public static void RocketAtmosphericsClient()
	{
		for (int num = AllRockets.Count - 1; num >= 0; num--)
		{
			AllRockets[num]?.OnAtmosphereClient();
		}
	}

	private void OnAtmosphereClient()
	{
		RocketNetwork?.CalculateGasMass();
	}

	private void OnPreAtmosphere()
	{
		try
		{
			RocketNetwork.CalculateGasMass();
			MoleQuantity moleQuantity = TotalMolesOxidizer();
			MoleQuantity moleQuantity2 = TotalMolesVolatiles();
			SendEventsToLog(RocketMath.Min(moleQuantity, moleQuantity2).ToFloat());
			MoleQuantity moleQuantity3 = moleQuantity + moleQuantity2;
			if (RocketState == RocketState.Landing && AutomatedLanding)
			{
				HandleAutomatedLanding();
			}
			if (CurrentTransit != null && CurrentTransit.GetDistance() > 0f)
			{
				if (!RocketMath.Approximately(_totalFuelMolesLastTick, moleQuantity3))
				{
					MoleQuantity moleQuantity4 = _totalFuelMolesLastTick - moleQuantity3;
					if (moleQuantity4 < Chemistry.MINIMUM_QUANTITY_MOLES)
					{
						EstimatedRemainingBurnTime = 0;
					}
					else
					{
						float num = (moleQuantity3 / moleQuantity4).ToFloat();
						EstimatedRemainingBurnTime = (ushort)(num / 2f);
					}
				}
				else
				{
					EstimatedRemainingBurnTime = 0;
				}
				_totalFuelMolesLastTick = moleQuantity3;
			}
			else
			{
				EstimatedRemainingBurnTime = 0;
			}
		}
		catch
		{
			EstimatedRemainingBurnTime = 0;
		}
	}

	private void SendEventsToLog(float minReactionMoles)
	{
		if (minReactionMoles == 0f && minReactionMoles > -1f)
		{
			RocketLog.Append(new FuelDepletedEvent(this)).Forget();
		}
		if (BatteryPercentage() == 0 && RocketNetwork.Batteries.Count > 0)
		{
			RocketLog.Append(new BatteryDepletedEvent(this)).Forget();
		}
	}

	public MoleQuantity TotalMolesVolatiles()
	{
		MoleQuantity zero = MoleQuantity.Zero;
		try
		{
			if (RocketNetwork.RocketAtmospheres.Count == 0)
			{
				return MoleQuantity.Zero;
			}
			foreach (Atmosphere rocketAtmosphere in RocketNetwork.RocketAtmospheres)
			{
				zero += rocketAtmosphere.GasMixture.TotalFuel;
				zero += rocketAtmosphere.GasMixture.TotalHypergolics / 2.0;
			}
			return zero;
		}
		catch
		{
			return zero;
		}
	}

	public MoleQuantity TotalMolesOxidizer()
	{
		MoleQuantity zero = MoleQuantity.Zero;
		try
		{
			if (RocketNetwork.RocketAtmospheres.Count == 0)
			{
				return MoleQuantity.Zero;
			}
			foreach (Atmosphere rocketAtmosphere in RocketNetwork.RocketAtmospheres)
			{
				zero += rocketAtmosphere.GasMixture.TotalOxidiser;
				zero += rocketAtmosphere.GasMixture.TotalHypergolics / 2.0;
			}
			return zero;
		}
		catch
		{
			return zero;
		}
	}

	public void Clear()
	{
		_finishMoveToTargetToken.Cancel();
	}

	public static void ClearAll()
	{
		foreach (Rocket allRocket in AllRockets)
		{
			allRocket?.Clear();
		}
		AllRockets.Clear();
	}

	private void OnLaunch(bool immediate = false)
	{
		foreach (IRocketInternals @internal in RocketNetwork.Internals)
		{
			@internal?.OnLaunch(immediate);
		}
		RocketNetwork.CrewModule?.OnLaunch(immediate);
	}

	private void OnLanded(bool immediate = false)
	{
		if (!immediate)
		{
			FlightControlRule = (AutomatedLanding ? FlightControlRule.Normal : FlightControlRule.None);
		}
		if (AutomatedLanding && GameManager.RunSimulation && !immediate)
		{
			TrimThrottle(100f);
		}
		if (AutomatedShutOff && GameManager.RunSimulation && !immediate)
		{
			foreach (IRocketInternals @internal in RocketNetwork.Internals)
			{
				if (@internal is RocketAvionicsDevice rocketAvionicsDevice)
				{
					rocketAvionicsDevice.RunAutoShutOff();
				}
			}
		}
		foreach (IRocketInternals internal2 in RocketNetwork.Internals)
		{
			internal2?.OnLanded(immediate);
		}
		RocketNetwork.CrewModule?.OnLanded(immediate);
		CanPopUpPoiText = true;
	}

	private void DetatchPartsFromGrid()
	{
		RocketNetwork.DetatchAll();
	}

	private void AttachPartsToGrid()
	{
		RocketNetwork.AttachAll();
		RocketNetwork.RebuildAllGridState();
	}

	private void ReParentRocketParts(Vector3 parentTransformPosition)
	{
		RocketParentTransform = new GameObject("Rocket " + RocketNetwork.ReferenceId).transform;
		RocketParentTransform.gameObject.AddComponent<Rigidbody>().isKinematic = true;
		RocketParentTransform.position = parentTransformPosition;
		LastParentedPosition = parentTransformPosition;
		foreach (IRocketInternals @internal in RocketNetwork.Internals)
		{
			@internal.Transform.SetParent(RocketParentTransform);
			foreach (Connection accessOpenEnd in @internal.AccessOpenEnds)
			{
				accessOpenEnd.Initialize();
			}
		}
		foreach (INetworkedStructure structure in RocketNetwork.StructureList)
		{
			structure.GetAsThing.Transform.SetParent(RocketParentTransform);
		}
	}

	private void UnParentRocketParts()
	{
		if (RocketParentTransform != null)
		{
			RocketParentTransform.DetachChildren();
			UnityEngine.Object.Destroy(_rocketParentTransform.gameObject);
		}
	}

	public float GetDryMass()
	{
		return RocketNetwork.DryMass;
	}

	public float TotalMass()
	{
		return RocketNetwork.CombinedMass();
	}

	public float Weight()
	{
		return Mathf.Abs(TotalMass() * GetGravity().value);
	}

	public float ThrustToWeightRatio()
	{
		float thrust = GetThrust();
		float num = Weight();
		if (num != 0f && thrust != 0f)
		{
			return thrust / num;
		}
		return -1f;
	}

	public (float value, string description) GetGravity()
	{
		return RocketState switch
		{
			RocketState.Landing => CheckOrbital(CurrentTransit?.Destination?.Owner?.IsOrbital == true), 
			RocketState.OnLaunchMount => CheckOrbital(CurrentNode?.Owner?.IsOrbital == true), 
			RocketState.Launching => CheckOrbital(CurrentTransit?.From?.Owner?.IsOrbital == true), 
			RocketState.InSpace => (value: 0f, description: GameStrings.SpaceName.DisplayString), 
			_ => (value: 0f, description: "error"), 
		};
		static (float value, string description) CheckOrbital(bool orbital)
		{
			float item = Mathf.Clamp(orbital ? (-1f) : WorldSetting.Current.Gravity, -5.5f, -1f);
			string item2 = (orbital ? ((string)GameStrings.RocketLowOrbit) : WorldManager.CurrentWorldName);
			return (value: item, description: item2);
		}
	}

	public void PrintDebugInfo(bool verbose = false)
	{
		throw new NotImplementedException();
	}

	public static void DrawDebug()
	{
		if (AllRockets.Count != 0 && GameManager.GameState == GameState.Running && IsDrawingDebug)
		{
			ImGui.Begin("RocketDebug", (ImGuiWindowFlags)799685);
			ImGui.SetWindowPos(new Vector2(10f, 10f), ImGuiCond.Always);
			if (_debugRocket != null)
			{
				ImguiHelper.DrawText("Name: " + _debugRocket.DisplayName);
				ImguiHelper.DrawText("CurrentLocation: " + (_debugRocket.CurrentNode?.DisplayName ?? "None"));
				ImguiHelper.DrawText("Destination: " + (_debugRocket.TargetNode?.DisplayName ?? "None"));
				ImguiHelper.DrawText("Eta: " + StringManager.Get(_debugRocket.EstimatedTimeToTargetSeconds()) + "s");
				ImguiHelper.DrawText("Burn Time Remaining: " + StringManager.Get(_debugRocket.EstimatedRemainingBurnTime) + "s");
				float apex = GetApex(_debugRocket.GetAltitude(), _debugRocket.Velocity, _debugRocket.Acceleration);
				ImguiHelper.DrawText($"Decent Arrest: {apex}m");
				ImGui.NewLine();
				ImguiHelper.DrawText("Acceleration: " + StringManager.Get(_debugRocket.Acceleration));
				ImguiHelper.DrawText("Velocity: " + StringManager.Get(_debugRocket.Velocity));
				ImguiHelper.DrawText("TargetVelocity: " + StringManager.Get(_debugRocket.TargetVelocity()));
				ImguiHelper.DrawText("Altitude: " + StringManager.Get(_debugRocket.GetAltitude()));
				ImGui.NewLine();
				ImguiHelper.DrawText($"Rule: {_debugRocket.FlightControlRule}");
				ImguiHelper.DrawText("Mass: " + StringManager.Get(_debugRocket.TotalMass()) + " kg");
				ImguiHelper.DrawText("Dry Mass: " + StringManager.Get(_debugRocket.GetDryMass()) + "kg");
				ImguiHelper.DrawText("Thrust: " + StringManager.Get(_debugRocket.GetThrust() / 1000f) + "kN");
				ImguiHelper.DrawText("Weight: " + StringManager.Get(_debugRocket.Weight() / 1000f) + "kN (" + (WorldManager.CurrentWorldName ?? string.Empty) + ")");
				ImguiHelper.DrawText("Thrust To Weight Ratio: " + StringManager.Get(_debugRocket.ThrustToWeightRatio()) + " (" + (WorldManager.CurrentWorldName ?? string.Empty) + ")");
				ImGui.NewLine();
				ImguiHelper.DrawText("Total Oxidizer: " + StringManager.Get(_debugRocket.TotalMolesOxidizer().ToFloat()) + "mol");
				ImguiHelper.DrawText("Total Volatiles: " + StringManager.Get(_debugRocket.TotalMolesVolatiles().ToFloat()) + " mol");
				ImguiHelper.DrawText("Battery: " + StringManager.Get(_debugRocket.BatteryPercentage()) + "%");
				ImGui.End();
			}
		}
	}

	public string GetKey()
	{
		return StringManager.Get(ReferenceId);
	}

	public string GetName()
	{
		return DisplayName;
	}

	public Vector3 GetWorldPosition()
	{
		foreach (IRocketEngine engine in RocketNetwork.Engines)
		{
			if (engine is Thing thing)
			{
				return thing.Position;
			}
		}
		return Vector3.zero;
	}

	public static string AutoLandConfidenceString(float confidenceRatio)
	{
		Assets.Scripts.Localization2.GameString gameString = ((confidenceRatio >= 0.3f) ? ((confidenceRatio >= 0.8f) ? GameStrings.VeryHighConfidence : ((!(confidenceRatio >= 0.6f)) ? GameStrings.ModerateConfidence : GameStrings.HighConfidence)) : ((!(confidenceRatio > 0f)) ? GameStrings.NoConfidence : GameStrings.LowConfidence));
		return gameString;
	}

	public static string AutoLandConfidenceColor(float confidenceRatio)
	{
		if (confidenceRatio >= 0.3f)
		{
			if (!(confidenceRatio >= 0.8f))
			{
				if (confidenceRatio >= 0.6f)
				{
					return "yellow";
				}
				return "orange";
			}
			return "green";
		}
		if (confidenceRatio > 0f)
		{
			return "red";
		}
		return "red";
	}

	public void HandleLaunchMountNodeBroken(SpaceMapNode removedNode, Vector3 rocketMountPosition)
	{
		switch (RocketState)
		{
		case RocketState.None:
		case RocketState.OnLaunchMount:
			if (CurrentNode == removedNode)
			{
				ExplodeRocket(removedNode.Owner.RocketTransformPosition);
			}
			break;
		case RocketState.Launching:
			_destroyedLaunchMountPosition = rocketMountPosition;
			break;
		case RocketState.InSpace:
			if (TargetNode == removedNode)
			{
				ChangeTarget(SpaceMap.Current.EntryNode);
			}
			break;
		case RocketState.Landing:
			if (TargetNode == removedNode)
			{
				ChangeTarget(CurrentTransit.From);
				RocketLog.Append(new RocketLandAbortedEvent(this)).Forget();
			}
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	public string ToTooltip()
	{
		return "<color=lightblue>" + DisplayName + "</color>";
	}

	public void Add(PooledAudioSource pooledAudio)
	{
	}

	public void Remove(PooledAudioSource pooledAudio)
	{
		if (_airMovementAudio == pooledAudio)
		{
			_airMovementAudio = null;
		}
	}

	public void OnDestroy()
	{
		StopLandingAudio();
	}

	public bool IsSoundLocal()
	{
		if (InventoryManager.Parent?.ParentSlot?.Parent is IRocketInternals rocketInternals)
		{
			return rocketInternals.RocketNetwork == RocketNetwork;
		}
		return false;
	}
}
