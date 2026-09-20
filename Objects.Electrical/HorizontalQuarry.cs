using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using TerrainSystem;
using TerrainSystem.Lods;
using UnityEngine;

namespace Objects.Electrical;

public class HorizontalQuarry : DeviceImportExport, IPhysical, IProfile, IDensePoolable, IGenerateMinables
{
	private readonly DensePoolReference<IPhysical> _densePoolReference = new DensePoolReference<IPhysical>(Thing.PhysicalPoolActive);

	[Header("Horizontal Quarry")]
	[Tooltip("The Renderer for the track")]
	public Renderer TrackRenderer;

	[Tooltip("The drill car transform itself")]
	public Transform DrillCar;

	[Tooltip("The starting position of the drill")]
	public Transform StartingCarPosition;

	[Tooltip("The end position at which the drill will stop mining")]
	public Transform EndingPositionTransform;

	[Tooltip("The drill bits which mine")]
	public Transform[] DrillBits;

	[Tooltip("Points to check for if the car can mine")]
	public Transform[] VoxelCheckPoints;

	[Tooltip("The bot collider which voxels within this space will clear")]
	public BoxCollider InitClearingVoxelArea;

	[Tooltip("The speed at which the car will move")]
	public float DrillCarMovementSpeed = 0.8f;

	[Tooltip("The multiplier for extracting the ore")]
	public float OreScaleExtractMultiplier = 1f;

	[Tooltip("The mining effect scale")]
	public float OnMineVoxelEffect = 0.5f;

	[Tooltip("The amount of ore which is allowed to be contained at one time")]
	public int CollectedOreLimit = 6;

	public const float RENDER_DISTANCE_QUARRY = 200f;

	private float _drillCarZPosition;

	private bool IsFinished;

	private Queue<QuarryCollectedOre> _collectedOre = new Queue<QuarryCollectedOre>();

	public QuarryState CurrentState;

	private Vector3Int _lastMinePosition;

	private const string _mainTexString = "_MainTex";

	private static readonly int _tracksMainTexture = Shader.PropertyToID("_MainTex");

	private const float _uvOffsetScale = 0.14f;

	private float _lastOffset;

	private readonly Vector3[] _initializationOffsets = new Vector3[48]
	{
		new Vector3(0f, 0f, 0f),
		new Vector3(-1f, 0f, 0f),
		new Vector3(-1f, 0f, -1f),
		new Vector3(0f, 0f, -1f),
		new Vector3(0f, 1f, 0f),
		new Vector3(-1f, 1f, 0f),
		new Vector3(-1f, 1f, -1f),
		new Vector3(0f, 1f, -1f),
		new Vector3(0f, 2f, 0f),
		new Vector3(-1f, 2f, 0f),
		new Vector3(-1f, 2f, -1f),
		new Vector3(0f, 2f, -1f),
		new Vector3(0f, 0f, -1f),
		new Vector3(-1f, 0f, -1f),
		new Vector3(-1f, 0f, -2f),
		new Vector3(0f, 0f, -2f),
		new Vector3(0f, 1f, -1f),
		new Vector3(-1f, 1f, -1f),
		new Vector3(-1f, 1f, -2f),
		new Vector3(0f, 1f, -2f),
		new Vector3(0f, 2f, -1f),
		new Vector3(-1f, 2f, -1f),
		new Vector3(-1f, 2f, -2f),
		new Vector3(0f, 2f, -2f),
		new Vector3(0f, 0f, -2f),
		new Vector3(-1f, 0f, -2f),
		new Vector3(-1f, 0f, -3f),
		new Vector3(0f, 0f, -3f),
		new Vector3(0f, 1f, -2f),
		new Vector3(-1f, 1f, -2f),
		new Vector3(-1f, 1f, -3f),
		new Vector3(0f, 1f, -3f),
		new Vector3(0f, 2f, -2f),
		new Vector3(-1f, 2f, -2f),
		new Vector3(-1f, 2f, -3f),
		new Vector3(0f, 2f, -3f),
		new Vector3(0f, 0f, -3f),
		new Vector3(-1f, 0f, -3f),
		new Vector3(-1f, 0f, -4f),
		new Vector3(0f, 0f, -4f),
		new Vector3(0f, 1f, -3f),
		new Vector3(-1f, 1f, -3f),
		new Vector3(-1f, 1f, -4f),
		new Vector3(0f, 1f, -4f),
		new Vector3(0f, 2f, -3f),
		new Vector3(-1f, 2f, -3f),
		new Vector3(-1f, 2f, -4f),
		new Vector3(0f, 2f, -4f)
	};

	private readonly Vector3[] OriginOffsets = new Vector3[4]
	{
		new Vector3(0f, 0f, 0f),
		new Vector3(0f, 0f, -1f),
		new Vector3(-1f, 0f, -1f),
		new Vector3(-1f, 0f, 0f)
	};

	[ByteArraySync]
	private float DrillCarZPosition
	{
		get
		{
			return _drillCarZPosition;
		}
		set
		{
			if (NetworkManager.IsServer && !RocketMath.Approximately(_drillCarZPosition, value, 0.001f))
			{
				base.NetworkUpdateFlags |= 512;
			}
			_drillCarZPosition = value;
			if (!GameManager.RunSimulation)
			{
				DrillCar.localPosition = new Vector3(DrillCar.localPosition.x, DrillCar.localPosition.y, value);
			}
		}
	}

	public bool IsDrillFull => _collectedOre.Count >= CollectedOreLimit;

	public bool IsRunning
	{
		get
		{
			if (base.IsStructureCompleted && OnOff)
			{
				return Powered;
			}
			return false;
		}
	}

	public bool RunPhysicsUpdate => true;

	public Vector3Int MinablesGenerationRange => GameConstants.MINABLES_GENERATION_RANGE_DEEP_MINER;

	public Vector3 GeneratePosition { get; set; }

	public Vector3 PreviousMinableRequestPosition { get; set; }

	public bool ShouldGenerate => true;

	public override bool OnAddToPool(object densePool, int slot)
	{
		if (_densePoolReference.CanAddToPool(densePool))
		{
			return _densePoolReference.AddToPool(densePool, slot);
		}
		return base.OnAddToPool(densePool, slot);
	}

	public override void OnRemoveFromPool(object densePool)
	{
		base.OnRemoveFromPool(densePool);
		_densePoolReference.OnRemovedFrom(densePool);
	}

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(200f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	public void PhysicsUpdate()
	{
		GeneratePosition = DrillCar.transform.position;
		VoxelTerrain.GenerateMinables(this);
		if (IsRunning)
		{
			HandleMovement();
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteSingle(DrillCarZPosition);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			DrillCarZPosition = reader.ReadSingle();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(DrillCarZPosition);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		DrillCarZPosition = reader.ReadSingle();
	}

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		if (IsRunning)
		{
			RunMinerOnServer();
		}
	}

	protected override void OnServerExportTick()
	{
		if (!IsRunning)
		{
			return;
		}
		if (CurrentState == QuarryState.DepositingOre && IsNextExportReady && _collectedOre.Count > 0)
		{
			QuarryCollectedOre quarryCollectedOre = _collectedOre.Dequeue();
			Ore ore = OnServer.CreateOld(quarryCollectedOre.OreName, DrillCar.position, UnityEngine.Random.rotation) as Ore;
			if (ore != null)
			{
				ore.SetQuantity(Mathf.CeilToInt(quarryCollectedOre.Quantity));
				OnServer.MoveToSlot(ore, ExportSlot);
			}
		}
		if (CanBeginExport)
		{
			OnServer.Interact(base.InteractExport, 1);
		}
	}

	private void RunMinerOnServer()
	{
		switch (CurrentState)
		{
		case QuarryState.Idle:
			if (Activate == 1)
			{
				CurrentState = QuarryState.Mining;
			}
			break;
		case QuarryState.Mining:
			if (DrillCar.localPosition == EndingPositionTransform.localPosition || !CheckDrillCarCanMove() || CheckStructureObstructingDrillCar())
			{
				CurrentState = QuarryState.Returning;
				IsFinished = true;
			}
			else if (Activate == 0 || IsDrillFull || IsFinished)
			{
				CurrentState = ((!(DrillCar.localPosition == StartingCarPosition.localPosition)) ? QuarryState.Returning : QuarryState.Idle);
				Activate = ((!(DrillCar.localPosition == StartingCarPosition.localPosition)) ? 1 : 0);
			}
			else
			{
				HandleMining();
			}
			break;
		case QuarryState.Returning:
			if (DrillCar.localPosition == StartingCarPosition.localPosition)
			{
				if (_collectedOre.Count > 0)
				{
					CurrentState = QuarryState.DepositingOre;
					break;
				}
				CurrentState = QuarryState.Idle;
				Activate = 0;
			}
			break;
		case QuarryState.DepositingOre:
			if (_collectedOre.Count == 0)
			{
				if (Activate == 0 || IsFinished)
				{
					CurrentState = QuarryState.Finished;
					IsFinished = true;
				}
				else
				{
					CurrentState = QuarryState.Mining;
				}
			}
			break;
		case QuarryState.Finished:
			if (!IsFinished)
			{
				CurrentState = QuarryState.Idle;
			}
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		if (Math.Abs(DrillCarZPosition - DrillCar.localPosition.z) > 0.1f)
		{
			DrillCarZPosition = DrillCar.localPosition.z;
		}
		if (CurrentState != (QuarryState)Mode)
		{
			OnServer.Interact(base.InteractMode, (int)CurrentState);
		}
	}

	public void HandleMovement()
	{
		switch (CurrentState)
		{
		case QuarryState.Mining:
			MoveTowards(EndingPositionTransform);
			AnimateTracks();
			break;
		case QuarryState.Returning:
			MoveTowards(StartingCarPosition);
			AnimateTracks();
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case QuarryState.Idle:
		case QuarryState.DepositingOre:
		case QuarryState.Finished:
			break;
		}
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		SetCurrentState();
	}

	public void SetCurrentState()
	{
		if (Mode != (int)CurrentState)
		{
			CurrentState = (QuarryState)Mathf.Min(Mode, 4);
		}
	}

	public void MoveTowards(Transform targetTransform)
	{
		DrillCar.localPosition = Vector3.MoveTowards(DrillCar.localPosition, targetTransform.localPosition, Time.deltaTime * DrillCarMovementSpeed);
	}

	public void HandleMining()
	{
		Vector3Int vector3Int = DrillBits[0].position.FloorToInt();
		if (_lastMinePosition != vector3Int)
		{
			_lastMinePosition = vector3Int;
			for (int i = 0; i < DrillBits.Length; i++)
			{
				MineVoxelTerrain(DrillBits[i].position.GridCenter(1f), OreScaleExtractMultiplier);
			}
			LodManager.Instance.DirtyLods(vector3Int, dirtyNeighbours: true);
		}
	}

	public bool CheckDrillCarCanMove()
	{
		int num = 0;
		Transform[] voxelCheckPoints = VoxelCheckPoints;
		foreach (Transform transform in voxelCheckPoints)
		{
			if (IsVoxelEmpty(transform.position))
			{
				num++;
			}
		}
		return num <= 2;
	}

	public bool IsVoxelEmpty(Vector3 worldPosition)
	{
		return VoxelTerrain.GetDensityWorldSpace(worldPosition) <= 0.49803922f;
	}

	public bool CheckStructureObstructingDrillCar()
	{
		Vector3 worldPosition = DrillCar.position + -(DrillCar.forward * 3f);
		return base.GridController.Get<Structure>(worldPosition, StructureElement.Center) != null;
	}

	private void MineVoxelTerrain(Vector3 worldVoxelPosition, float scaleExtracted)
	{
		VoxelTerrain.SetDensityWorldSpace(worldVoxelPosition, 0f, RoomChangeSource.VoxelRemove, dirtyLods: false);
		Vein veinAtPosition = Vein.GetVeinAtPosition(worldVoxelPosition);
		if (veinAtPosition != null)
		{
			veinAtPosition.TryMineServer(worldVoxelPosition.FloorToInt(), out Ore createdOre, worldVoxelPosition);
			if ((object)createdOre != null)
			{
				createdOre.SetQuantity(Mathf.RoundToInt(createdOre.GetQuantity * scaleExtracted));
				AddOre(createdOre);
			}
		}
	}

	private static bool CanMergeOreInto(Ore ore, Stackable target)
	{
		if ((object)target != null && target.PrefabHash == ore.PrefabHash)
		{
			return !target.IsStackFull;
		}
		return false;
	}

	public void AddOre(Ore ore)
	{
		_collectedOre.Enqueue(new QuarryCollectedOre
		{
			OreName = ore.PrefabName,
			Quantity = ore.Quantity
		});
		OnServer.Destroy(ore);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action != InteractableType.Activate)
		{
			return base.InteractWith(interactable, interaction, doAction);
		}
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (doAction)
		{
			PlaySound(Animator.StringToHash("ActivateButton"));
		}
		if (!Powered)
		{
			return delayedActionInstance.Fail();
		}
		if (!doAction || !GameManager.RunSimulation)
		{
			return delayedActionInstance.Succeed();
		}
		switch (CurrentState)
		{
		case QuarryState.Idle:
		case QuarryState.Finished:
			OnServer.Interact(base.InteractActivate, 1);
			IsFinished = false;
			break;
		case QuarryState.Mining:
		case QuarryState.Returning:
		case QuarryState.DepositingOre:
			OnServer.Interact(base.InteractActivate, 0);
			IsFinished = true;
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		return DelayedActionInstance.Success(interactable.ContextualName);
	}

	public void AnimateTracks()
	{
		if ((bool)TrackRenderer)
		{
			Material material = TrackRenderer.material;
			if ((bool)material)
			{
				float num = ((CurrentState != QuarryState.Mining) ? (-0.14f) : 0.14f);
				_lastOffset = (RocketMath.Approximately(num, 0f) ? 0f : (_lastOffset + num * Time.deltaTime));
				material.SetTextureOffset(_tracksMainTexture, new Vector2(0f, 0f - _lastOffset));
			}
		}
	}

	protected override void OnBuildStateUpdated(int newState, int previousState)
	{
		base.OnBuildStateUpdated(newState, previousState);
		if (base.IsStructureCompleted && GameManager.RunSimulation)
		{
			ClearInitVoxels();
		}
	}

	public void ClearInitVoxels()
	{
		Quaternion rotation = base.transform.rotation;
		Vector3 vector = OriginOffsets[Math.Clamp(Mathf.RoundToInt(rotation.eulerAngles.y) / 90, 0, 3)];
		for (int i = 0; i < _initializationOffsets.Length; i++)
		{
			Vector3 vector2 = _initializationOffsets[i];
			vector2 = rotation * vector2;
			Vector3 vector3 = base.Position + vector + vector2;
			VoxelTerrain.SetDensityWorldSpace(vector3, 0f);
			Vein.GetVeinAtPosition(vector3)?.TryRemoveServer(vector3.FloorToInt());
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new HorizontalQuarrySaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (!IsCursor && savedData is HorizontalQuarrySaveData horizontalQuarrySaveData)
		{
			DrillCar.position = horizontalQuarrySaveData.DrillCarPosition;
			if (horizontalQuarrySaveData.CollectedOre != null)
			{
				_collectedOre = new Queue<QuarryCollectedOre>(horizontalQuarrySaveData.CollectedOre);
			}
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is HorizontalQuarrySaveData horizontalQuarrySaveData && !IsCursor)
		{
			horizontalQuarrySaveData.DrillCarPosition = DrillCar.position;
			if (_collectedOre.Count > 0)
			{
				horizontalQuarrySaveData.CollectedOre = _collectedOre.ToArray();
			}
		}
	}

	protected bool Equals(HorizontalQuarry other)
	{
		if (base.Equals((object)other))
		{
			return _lastOffset.Equals(other._lastOffset);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (this == obj)
		{
			return true;
		}
		if (obj.GetType() != GetType())
		{
			return false;
		}
		return Equals((HorizontalQuarry)obj);
	}

	public override int GetHashCode()
	{
		return (base.GetHashCode() * 397) ^ _lastOffset.GetHashCode();
	}
}
