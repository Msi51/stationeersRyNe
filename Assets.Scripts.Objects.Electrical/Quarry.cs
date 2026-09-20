using System;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Objects.Electrical;
using TerrainSystem;
using TerrainSystem.Lods;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class Quarry : DeviceImportExport, IPhysical, IProfile, IDensePoolable, IGenerateMinables
{
	private readonly DensePoolReference<IPhysical> _densePoolReference = new DensePoolReference<IPhysical>(Thing.PhysicalPoolActive);

	[Header("Auto Miner")]
	[HideInInspector]
	public List<Quarry> AllQuarrys = new List<Quarry>();

	public Transform DrillHead;

	private Vector3 PrevDrillPosition;

	public Transform DrillCables;

	public Transform DrillCarriage;

	public Transform DrillRail;

	public Transform QuarryDigPosition;

	[Tooltip("The drill bits which mine")]
	public Transform[] DrillBits;

	[Tooltip("The area to quarry")]
	public BoxCollider QuarryArea;

	[Tooltip("The speed at which the drill moves")]
	public float DrillMovementSpeed = 1.3f;

	[Tooltip("The area to quarry")]
	public float OreScaleExtract = 1f;

	[Tooltip("The mining effect scale")]
	public float OnMineVoxelEffect = 0.5f;

	[Tooltip("The amount of ore which is allowed to be contained at one time")]
	public int CollectedOreLimit = 6;

	public Vector3 DefaultHeadPosition;

	public Vector3 DefaultDrillCarriagePosition;

	public Vector3 DefaultRailingPosition;

	private Vector3 MovementAmount = new Vector3(1f, 1f, 1f);

	private Vector3 _halfQuarrySize;

	private Vector3 _drillEndSegmentX;

	private Vector3 _drillEndSegmentY;

	private Vector3 _drillEndSegmentZ;

	private int _collectedOreCount;

	[SerializeField]
	private LayerMask layerMask;

	private Queue<QuarryCollectedOre> _collectedOre = new Queue<QuarryCollectedOre>();

	private bool _drillFinished;

	private bool _isFinishing;

	private bool _drillIsPlaced;

	private bool _transportingOre;

	private bool _isDeliveringOre;

	private Vector3 _savedDrillEndSegmentX;

	private Vector3 _savedDrillEndSegmentY;

	private Vector3 _savedDrillEndSegmentZ;

	private bool _forwardDirection;

	private static readonly string _deliveringOreStr = "DeliveringOre";

	private static readonly string _transportingOreStr = "TransportingOre";

	private static readonly int _bedRockOffset = 2;

	private readonly int _yModuloOffsetTerrainGeneration = 5;

	private readonly int _railStartHash = Animator.StringToHash("RailStart");

	private readonly int _railsHash = Animator.StringToHash("Rails");

	private bool _railSound;

	private readonly int _carriageHash = Animator.StringToHash("Carriage");

	private bool _carriageSound;

	private readonly int _cableHash = Animator.StringToHash("Cable");

	private readonly int _cableStopHash = Animator.StringToHash("CableStop");

	private bool _cableSound;

	private readonly int _cablesPositionOffset = 2;

	private Vector3Int _lastMinePosition;

	private Thing ThingInTheWay { get; set; }

	[ByteArraySync]
	private int _CollectedOreCount
	{
		get
		{
			return _collectedOreCount;
		}
		set
		{
			_collectedOreCount = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 512;
			}
		}
	}

	public bool RunPhysicsUpdate => true;

	private bool RailSound
	{
		get
		{
			return _railSound;
		}
		set
		{
			if (value != _railSound)
			{
				if (value)
				{
					PlaySound(_railStartHash);
					PlaySound(_railsHash);
					CarriageSound = false;
				}
				else
				{
					StopSound(_railsHash);
				}
				_railSound = value;
			}
		}
	}

	private bool CarriageSound
	{
		get
		{
			return _carriageSound;
		}
		set
		{
			if (value != _carriageSound)
			{
				if (value)
				{
					PlaySound(_carriageHash);
				}
				else
				{
					StopSound(_carriageHash);
				}
				_carriageSound = value;
			}
		}
	}

	private bool CableSound
	{
		get
		{
			return _cableSound;
		}
		set
		{
			if (value != _cableSound)
			{
				if (value)
				{
					PlaySound(_cableHash);
					RailSound = false;
					CarriageSound = false;
				}
				else
				{
					StopSound(_cableHash);
					PlaySound(_cableStopHash);
				}
				_cableSound = value;
			}
		}
	}

	private int CollectedOreCount
	{
		get
		{
			if (!GameManager.RunSimulation)
			{
				return _CollectedOreCount;
			}
			return _collectedOre.Count;
		}
	}

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

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		if (base.IsStructureCompleted && Error != 1 && _drillIsPlaced && OnOff && Activate == 1 && Powered)
		{
			GeneratePosition = DrillBits[0].position;
			VoxelTerrain.GenerateMinables(this);
			if (Exporting == 0 && ExportingThing == null && !_drillFinished && ExportSlot.Occupant == null)
			{
				RunDrillBit();
			}
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteInt32(_CollectedOreCount);
			writer.WriteVector3(_drillEndSegmentX);
			writer.WriteVector3(_drillEndSegmentY);
			writer.WriteVector3(_drillEndSegmentZ);
			writer.WriteVector3(DrillRail.localPosition);
			writer.WriteVector3(DrillCarriage.localPosition);
			writer.WriteVector3(MovementAmount);
			writer.WriteBoolean(_forwardDirection);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			_CollectedOreCount = reader.ReadInt32();
			_drillEndSegmentX = reader.ReadVector3();
			_drillEndSegmentY = reader.ReadVector3();
			_drillEndSegmentZ = reader.ReadVector3();
			DrillRail.localPosition = reader.ReadVector3();
			DrillCarriage.localPosition = reader.ReadVector3();
			MovementAmount = reader.ReadVector3();
			_forwardDirection = reader.ReadBoolean();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt32(_CollectedOreCount);
		writer.WriteVector3(_drillEndSegmentX);
		writer.WriteVector3(_drillEndSegmentY);
		writer.WriteVector3(_drillEndSegmentZ);
		writer.WriteVector3(DrillRail.localPosition);
		writer.WriteVector3(DrillCarriage.localPosition);
		writer.WriteVector3(MovementAmount);
		writer.WriteBoolean(_forwardDirection);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		_CollectedOreCount = reader.ReadInt32();
		_drillEndSegmentX = reader.ReadVector3();
		_drillEndSegmentY = reader.ReadVector3();
		_drillEndSegmentZ = reader.ReadVector3();
		DrillRail.localPosition = reader.ReadVector3();
		DrillCarriage.localPosition = reader.ReadVector3();
		MovementAmount = reader.ReadVector3();
		_forwardDirection = reader.ReadBoolean();
	}

	protected override void OnServerExportTick()
	{
		if (!base.IsStructureCompleted || Error == 1 || !_drillIsPlaced || !OnOff || Activate != 1 || !Powered)
		{
			return;
		}
		if (_isDeliveringOre && IsNextExportReady && CollectedOreCount > 0)
		{
			QuarryCollectedOre quarryCollectedOre = _collectedOre.Dequeue();
			_CollectedOreCount = _collectedOre.Count;
			Ore ore = OnServer.CreateOld(quarryCollectedOre.OreName, DrillHead.position, UnityEngine.Random.rotation) as Ore;
			if (ore != null)
			{
				ore.SetQuantity(Mathf.CeilToInt(Mathf.Clamp(quarryCollectedOre.Quantity, 1f, ore.MaxQuantity)));
				OnServer.MoveToSlot(ore, ExportSlot);
			}
		}
		if (CanBeginExport)
		{
			OnServer.Interact(base.InteractExport, 1);
		}
	}

	public void PhysicsUpdate()
	{
		if (!base.IsStructureCompleted || Error == 1 || !_drillIsPlaced || !OnOff || Activate != 1 || !Powered)
		{
			CarriageSound = false;
			RailSound = false;
			CableSound = false;
		}
		else if (Exporting == 0 && ExportingThing == null && !_drillFinished && ExportSlot.Occupant == null)
		{
			HandleMovement();
		}
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
			if (Error == 1 && (object)ThingInTheWay != null)
			{
				return delayedActionInstance.Fail(GameStrings.DeepMinerSomethingInTheWay, ThingInTheWay.ToTooltip());
			}
		}
		if (interactable.Action != InteractableType.Button1)
		{
			return delayedActionInstance;
		}
		DelayedActionInstance delayedActionInstance2 = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		delayedActionInstance2.ActionMessage = ((Activate == 0) ? ActionStrings.On : ActionStrings.Off);
		if (!Powered)
		{
			return delayedActionInstance2.Fail(GameStrings.DeviceNoPower);
		}
		if (!doAction || !GameManager.RunSimulation)
		{
			return delayedActionInstance2.Succeed();
		}
		if (_drillFinished)
		{
			SetupDrillHead();
			OnServer.Interact(base.InteractError, 0);
			_isFinishing = false;
			_drillFinished = false;
		}
		OnServer.Interact(base.InteractActivate, 1);
		OnServer.Interact(base.InteractButton1, (Button1 != 1) ? 1 : 0);
		if (Button1 == 0 && !_drillFinished && Error != 1)
		{
			_isFinishing = true;
			SetDrillStartingPosition();
		}
		UpdateCablesScale();
		IsThingInWay();
		AssessError();
		return DelayedActionInstance.Success(interactable.ContextualName);
	}

	private void SetUpQuarry()
	{
		_halfQuarrySize = QuarryArea.size / 2f;
		SetupDrillHead();
		OnServer.Interact(base.InteractError, 0);
		_drillFinished = false;
		_isFinishing = false;
		_drillIsPlaced = true;
	}

	private void SetupDrillHead()
	{
		DrillHead.localPosition = DefaultHeadPosition;
		PrevDrillPosition = DrillHead.position;
		DrillCarriage.localPosition = DefaultDrillCarriagePosition;
		DrillRail.localPosition = DefaultRailingPosition;
		_drillEndSegmentX = DefaultDrillCarriagePosition;
		_drillEndSegmentY = DefaultHeadPosition;
		_drillEndSegmentZ = DefaultRailingPosition;
	}

	private void RunDrillBit()
	{
		HandleOutsideBounds();
		if (_transportingOre && DrillCarriage.localPosition == _drillEndSegmentX && DrillRail.localPosition == _drillEndSegmentZ && DrillHead.localPosition == _drillEndSegmentY)
		{
			StartTransportOre();
			if (!_transportingOre && _isFinishing)
			{
				SetDrillFinished();
			}
		}
		else if (IsThingInWay())
		{
			AssessError();
		}
		else
		{
			if (!(DrillCarriage.localPosition == _drillEndSegmentX))
			{
				return;
			}
			if (CheckShouldTransportOre())
			{
				SetDrillStartingPosition();
				return;
			}
			float num = (_forwardDirection ? (0f - MovementAmount.x) : MovementAmount.x);
			_drillEndSegmentX.x += num;
			HandleMining();
			if (DrillHead.position.y <= (float)_bedRockOffset)
			{
				_isFinishing = true;
				SetDrillStartingPosition();
			}
		}
	}

	private void AssessError()
	{
		if (GameManager.RunSimulation && HasErrorState)
		{
			if (Error == 0 && (object)ThingInTheWay != null)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			else if (Error == 1 && ThingInTheWay == null)
			{
				OnServer.Interact(base.InteractError, 0);
			}
		}
	}

	private bool IsThingInWay()
	{
		if (base.IsBeingDestroyed)
		{
			ThingInTheWay = null;
			return false;
		}
		Vector3 vector = DrillHead.position - PrevDrillPosition;
		if (vector != Vector3.zero)
		{
			if (Math.Abs(vector.x) >= Math.Abs(vector.y) && Math.Abs(vector.x) >= Math.Abs(vector.z))
			{
				vector.Set(vector.x, 0f, 0f);
			}
			else if (Math.Abs(vector.y) >= Math.Abs(vector.x) && Math.Abs(vector.y) >= Math.Abs(vector.z))
			{
				vector.Set(0f, vector.y, 0f);
			}
			else
			{
				vector.Set(0f, 0f, vector.z);
			}
			vector.Normalize();
		}
		Vector3 vector2 = DrillHead.position;
		Structure structure = base.GridController.Get<Structure>(vector2 + vector * 0.5f, StructureElement.Center);
		if ((object)structure == null)
		{
			structure = base.GridController.GetFaceStructure(vector2 + vector * 0.5f, vector * 0.5f);
		}
		Structure faceStructure = base.GridController.GetFaceStructure(vector2 + vector * 0.5f, -vector * 0.5f);
		if ((object)structure != null || (object)faceStructure != null)
		{
			Physics.Raycast(vector2, vector, out var hitInfo, 0.5f, layerMask);
			if ((object)hitInfo.collider != null)
			{
				if (Thing.Find(hitInfo.collider) is Structure structure2)
				{
					structure = (Structure)(ThingInTheWay = structure2);
				}
			}
			else
			{
				structure = null;
			}
		}
		return (object)structure != null;
	}

	private void SetDrillFinished()
	{
		_drillFinished = true;
		_isFinishing = false;
		OnServer.Interact(base.InteractError, 1);
		OnServer.Interact(base.InteractActivate, 0);
		OnServer.Interact(base.InteractButton1, 0);
	}

	private void SetDrillStartingPosition()
	{
		_savedDrillEndSegmentX = _drillEndSegmentX;
		_savedDrillEndSegmentY = _drillEndSegmentY;
		_savedDrillEndSegmentZ = _drillEndSegmentZ;
		_drillEndSegmentX = DefaultDrillCarriagePosition;
		_drillEndSegmentY = DefaultHeadPosition;
		_drillEndSegmentZ = DefaultRailingPosition;
		_transportingOre = true;
		BaseAnimator.SetBool(_transportingOreStr, value: true);
	}

	private void StartTransportOre()
	{
		if (CollectedOreCount == 0)
		{
			_drillEndSegmentX = _savedDrillEndSegmentX;
			_drillEndSegmentY = _savedDrillEndSegmentY;
			_drillEndSegmentZ = _savedDrillEndSegmentZ;
			_isDeliveringOre = false;
			_transportingOre = false;
			BaseAnimator.SetBool(_deliveringOreStr, value: false);
			BaseAnimator.SetBool(_transportingOreStr, value: false);
			if (Button1 == 0)
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
		}
		else
		{
			_isDeliveringOre = true;
			BaseAnimator.SetBool(_deliveringOreStr, value: true);
		}
	}

	private void HandleOutsideBounds()
	{
		if (_drillEndSegmentZ.z > QuarryDigPosition.localPosition.z + _halfQuarrySize.z)
		{
			MovementAmount.z = Mathf.Abs(MovementAmount.z);
			_drillEndSegmentZ.z -= MovementAmount.z;
			_drillEndSegmentX.z -= MovementAmount.z;
			_drillEndSegmentY.y -= MovementAmount.y;
		}
		else if (_drillEndSegmentZ.z < QuarryDigPosition.localPosition.z - _halfQuarrySize.z)
		{
			MovementAmount.z = 0f - MovementAmount.z;
			_drillEndSegmentZ.z -= MovementAmount.z;
			_drillEndSegmentX.z -= MovementAmount.z;
			_drillEndSegmentY.y -= MovementAmount.y;
		}
		if (_drillEndSegmentX.x > QuarryDigPosition.localPosition.x + _halfQuarrySize.x)
		{
			_drillEndSegmentX.x -= MovementAmount.x;
			_drillEndSegmentZ.z -= MovementAmount.z;
			_drillEndSegmentX.z -= MovementAmount.z;
			_forwardDirection = true;
		}
		else if (_drillEndSegmentX.x < QuarryDigPosition.localPosition.x - _halfQuarrySize.x)
		{
			_drillEndSegmentX.x += MovementAmount.x;
			_drillEndSegmentZ.z -= MovementAmount.z;
			_drillEndSegmentX.z -= MovementAmount.z;
			_forwardDirection = false;
		}
	}

	private void HandleMovement()
	{
		PrevDrillPosition = DrillHead.position;
		if (DrillHead.localPosition == _drillEndSegmentY)
		{
			DrillRail.localPosition = Vector3.MoveTowards(DrillRail.localPosition, _drillEndSegmentZ, Time.deltaTime * DrillMovementSpeed);
			RailSound = (DrillRail.localPosition.z < _drillEndSegmentZ.z || DrillRail.localPosition.z > _drillEndSegmentZ.z) && !CableSound;
			DrillCarriage.localPosition = Vector3.MoveTowards(DrillCarriage.localPosition, _drillEndSegmentX, Time.deltaTime * DrillMovementSpeed);
			CarriageSound = (DrillCarriage.localPosition.x < _drillEndSegmentX.x || DrillCarriage.localPosition.x > _drillEndSegmentX.x) && !RailSound && !CableSound;
		}
		else
		{
			UpdateCablesScale();
			DrillHead.localPosition = Vector3.MoveTowards(DrillHead.localPosition, _drillEndSegmentY, Time.deltaTime * DrillMovementSpeed);
			CableSound = DrillHead.localPosition.y < _drillEndSegmentY.y || DrillHead.localPosition.y > _drillEndSegmentY.y;
		}
	}

	private void UpdateCablesScale()
	{
		float y = Mathf.Abs(DrillHead.localPosition.y) * (float)_cablesPositionOffset - Mathf.Abs(DefaultHeadPosition.y);
		DrillCables.transform.localScale = new Vector3(DrillCables.transform.localScale.x, y, DrillCables.transform.localScale.z);
	}

	private bool CheckShouldTransportOre()
	{
		return CollectedOreCount >= CollectedOreLimit;
	}

	private void HandleMining()
	{
		if (GameManager.RunSimulation)
		{
			Transform[] drillBits = DrillBits;
			for (int i = 0; i < drillBits.Length; i++)
			{
				_ = drillBits[i];
				MineTerrain();
			}
		}
	}

	public void MineTerrain()
	{
		Vector3 vector = new Vector3(0f, -1f, 0f);
		Vector3Int vector3Int = DrillBits[0].position.FloorToInt();
		if (!(_lastMinePosition != vector3Int))
		{
			return;
		}
		_lastMinePosition = vector3Int;
		for (int i = 0; i < DrillBits.Length; i++)
		{
			Vector3 vector2 = DrillBits[i].position + vector;
			VoxelTerrain.SetDensityWorldSpace(vector2, 0f, RoomChangeSource.VoxelRemove, dirtyLods: false);
			Vein veinAtPosition = Vein.GetVeinAtPosition(vector2);
			if (veinAtPosition != null)
			{
				veinAtPosition.TryMineServer(vector2.FloorToInt(), out Ore createdOre, vector2);
				if ((object)createdOre != null)
				{
					createdOre.Quantity = Mathf.RoundToInt(createdOre.GetQuantity * OreScaleExtract);
					AddOre(createdOre);
				}
			}
		}
		LodManager.Instance.DirtyLods(vector3Int, dirtyNeighbours: true);
	}

	private void AddOre(Ore ore)
	{
		_collectedOre.Enqueue(new QuarryCollectedOre
		{
			OreName = ore.PrefabName,
			Quantity = ore.Quantity
		});
		OnServer.Destroy(ore);
	}

	public override void OnRegistered(Cell cell)
	{
		SetUpQuarry();
		AllQuarrys.Add(this);
		base.OnRegistered(cell);
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		AllQuarrys.Remove(this);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new QuarrySaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (!IsCursor && savedData is QuarrySaveData quarrySaveData)
		{
			_drillEndSegmentX = quarrySaveData.DrillEndSgementX;
			_drillEndSegmentY = quarrySaveData.DrillEndSgementY;
			_drillEndSegmentZ = quarrySaveData.DrillEndSgementZ;
			_savedDrillEndSegmentX = quarrySaveData.SavedDrillEndSegmentX;
			_savedDrillEndSegmentY = quarrySaveData.SavedDrillEndSegmentY;
			_savedDrillEndSegmentZ = quarrySaveData.SavedDrillEndSegmentZ;
			DrillRail.localPosition = quarrySaveData.RailingPosition;
			DrillCarriage.localPosition = quarrySaveData.CarriagePosition;
			DrillHead.localPosition = quarrySaveData.HeadPosition;
			MovementAmount = quarrySaveData.MovementAmount;
			_forwardDirection = quarrySaveData.ForwardDirection;
			_drillFinished = quarrySaveData.DrillFinished;
			_isFinishing = quarrySaveData.IsFinishing;
			_transportingOre = quarrySaveData.TransportingOre;
			if (quarrySaveData.CollectedOre != null)
			{
				_collectedOre = new Queue<QuarryCollectedOre>(quarrySaveData.CollectedOre);
				_CollectedOreCount = _collectedOre.Count;
			}
			UpdateCablesScale();
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is QuarrySaveData quarrySaveData && !IsCursor && !(DrillRail == null))
		{
			quarrySaveData.DrillEndSgementX = _drillEndSegmentX;
			quarrySaveData.DrillEndSgementY = _drillEndSegmentY;
			quarrySaveData.DrillEndSgementZ = _drillEndSegmentZ;
			quarrySaveData.SavedDrillEndSegmentX = _savedDrillEndSegmentX;
			quarrySaveData.SavedDrillEndSegmentY = _savedDrillEndSegmentY;
			quarrySaveData.SavedDrillEndSegmentZ = _savedDrillEndSegmentZ;
			quarrySaveData.RailingPosition = DrillRail.localPosition;
			quarrySaveData.CarriagePosition = DrillCarriage.localPosition;
			quarrySaveData.HeadPosition = DrillHead.localPosition;
			quarrySaveData.MovementAmount = MovementAmount;
			quarrySaveData.ForwardDirection = _forwardDirection;
			quarrySaveData.DrillFinished = _drillFinished;
			quarrySaveData.IsFinishing = _isFinishing;
			quarrySaveData.TransportingOre = _transportingOre;
			if (_collectedOre.Count > 0)
			{
				quarrySaveData.CollectedOre = _collectedOre.ToArray();
			}
		}
	}
}
