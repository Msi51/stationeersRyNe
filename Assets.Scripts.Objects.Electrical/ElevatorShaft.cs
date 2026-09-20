using System;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class ElevatorShaft : Device, ISmartRotatable
{
	private const float ELEVATOR_RENDER_DISTANCE = 40f;

	private const float ELEVATOR_SHADOW_DISTANCE = 20f;

	[ReadOnly]
	public ElevatorShaftNetwork ShaftNetwork;

	public ElevatorCarrage CarragePrefab;

	public bool CanCreateCarrage;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	public static readonly float MinSpeed = 0.5f;

	public override WreckageSize WreckageSize => WreckageSize.Medium;

	public virtual int ShaftLevel { get; set; } = int.MaxValue;

	private Vector3 ThingCenter => ThingTransform.position + Transform.forward;

	public float ElevatorSpeed
	{
		get
		{
			if (ShaftNetwork == null || ShaftNetwork.Carrage == null)
			{
				return 0f;
			}
			return ShaftNetwork.Carrage.ElevatorMode switch
			{
				ElevatorMode.Stationary => 0f, 
				ElevatorMode.Upward => ShaftNetwork.Speed, 
				ElevatorMode.Downward => 0f - ShaftNetwork.Speed, 
				_ => 0f, 
			};
		}
	}

	public float Speed => ShaftNetwork?.Speed ?? 1.5f;

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(40f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(20f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Powered)
		{
			ShaftNetwork?.ShaftPowerUpdated();
		}
	}

	public float GetShaftUsedPower(CableNetwork cableNetwork)
	{
		if (base.PowerCable == null || base.PowerCable.CableNetwork != cableNetwork)
		{
			return 0f;
		}
		if (!OnOff || !base.IsStructureCompleted)
		{
			return 0f;
		}
		return UsedPower;
	}

	private Grid3 GetLargeGrid()
	{
		return ThingCenter.ToGrid();
	}

	public ElevatorShaft ShaftAbove()
	{
		return SmallCell.Get<ElevatorShaft>(GetLargeGrid() + Grid3.Up);
	}

	public ElevatorShaft ShaftBelow()
	{
		return SmallCell.Get<ElevatorShaft>(GetLargeGrid() + Grid3.Down);
	}

	private bool EqualRotation(Quaternion a, Quaternion b)
	{
		return Quaternion.Angle(a, b) < 1f;
	}

	public override CanConstructInfo CanConstruct()
	{
		ElevatorShaft elevatorShaft = ShaftAbove();
		ElevatorShaft elevatorShaft2 = ShaftBelow();
		if ((bool)elevatorShaft && (bool)elevatorShaft2 && (bool)elevatorShaft.ShaftNetwork.Carrage && (bool)elevatorShaft2.ShaftNetwork.Carrage)
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.CannotMergeTwoOperationalElevatorShafts.DisplayString);
		}
		if ((bool)elevatorShaft && !EqualRotation(elevatorShaft.ThingTransform.rotation, ThingTransform.rotation))
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.ElevatorShaftRotationFail.DisplayString);
		}
		if ((bool)elevatorShaft2 && !EqualRotation(elevatorShaft2.ThingTransform.rotation, ThingTransform.rotation))
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.ElevatorShaftRotationFail.DisplayString);
		}
		return base.CanConstruct();
	}

	public override bool IsConnected(Connection otherEnd)
	{
		if (otherEnd.ConnectionType == NetworkType.Elevator)
		{
			Grid3 grid = base.GridController.WorldToLocalGrid(ThingCenter, GridSize, GridOffset);
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
			if (openEnd.ConnectionType == NetworkType.Elevator && (bool)SmallCell.Get<ElevatorShaft>(base.GridController.WorldToLocalGrid(openEnd.Transform.position, GridSize, GridOffset)))
			{
				connBuf[connCount++] = openEnd;
			}
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.SafetyCategory);
	}

	public override string GetStationpediaCategoryKey()
	{
		return StationpediaCategoryStrings.SafetyCategory;
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		if (ShaftNetwork == null)
		{
			ElevatorShaft elevatorShaft = ShaftAbove();
			ElevatorShaft elevatorShaft2 = ShaftBelow();
			if (elevatorShaft2 != null && elevatorShaft2.ShaftNetwork != null && (bool)elevatorShaft && elevatorShaft.ShaftNetwork != null)
			{
				elevatorShaft2.ShaftNetwork.MergeInto(elevatorShaft.ShaftNetwork);
				elevatorShaft.ShaftNetwork.Register(this);
			}
			else if ((bool)elevatorShaft && elevatorShaft.ShaftNetwork != null)
			{
				elevatorShaft.ShaftNetwork.Register(this);
			}
			else if ((bool)elevatorShaft2 && elevatorShaft2.ShaftNetwork != null)
			{
				elevatorShaft2.ShaftNetwork.Register(this);
			}
			if (!elevatorShaft && !elevatorShaft2 && ShaftNetwork == null)
			{
				ShaftNetwork = ((this is ElevatorLevel) ? new ElevatorShaftNetwork(this as ElevatorLevel, CanCreateCarrage) : new ElevatorShaftNetwork(this));
			}
		}
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		if (GameManager.GameState == GameState.Running && ShaftNetwork != null)
		{
			ShaftNetwork.Deregister(this);
		}
	}

	public virtual bool StopCarrage(ElevatorCarrage carrage)
	{
		return false;
	}

	public virtual void CheckCarrageState(ElevatorCarrage carrage)
	{
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

	public new List<Connection> GetOpenEnds()
	{
		return null;
	}

	public override int ConnectedCount()
	{
		return 0;
	}

	public new int GetOpenEndsCount()
	{
		return 0;
	}

	public new float GetGridSize()
	{
		return 2f;
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.ElevatorLevel => true, 
			LogicType.ElevatorSpeed => true, 
			_ => base.CanLogicWrite(logicType), 
		};
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		switch (logicType)
		{
		case LogicType.ElevatorLevel:
			if (ShaftNetwork != null && ShaftNetwork.Carrage != null && (int)value != ShaftNetwork.Carrage.LevelTarget)
			{
				if (GameManager.IsThread)
				{
					SetFromThread((int)value).Forget();
				}
				else
				{
					SetElevatorTarget((int)value);
				}
			}
			break;
		case LogicType.ElevatorSpeed:
			if (ShaftNetwork != null && ShaftNetwork.Carrage != null)
			{
				value = Mathf.Clamp((float)value, MinSpeed, ShaftNetwork.Carrage.MaxMovementSpeed);
				ShaftNetwork.Speed = (float)value;
			}
			break;
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.ElevatorSpeed => true, 
			LogicType.ElevatorLevel => true, 
			_ => base.CanLogicRead(logicType), 
		};
	}

	public override double GetLogicValue(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.ElevatorSpeed:
			return ShaftNetwork?.Speed ?? 0f;
		case LogicType.ElevatorLevel:
			if (ShaftNetwork == null || ShaftNetwork.Carrage == null || ShaftNetwork.Carrage.CurrentShaft == null)
			{
				return -1.0;
			}
			return ShaftNetwork.Carrage.CurrentShaft.ShaftLevel;
		default:
			return base.GetLogicValue(logicType);
		}
	}

	public void SetElevatorTarget(int targetLevel)
	{
		if (ShaftNetwork != null && !(ShaftNetwork.Carrage == null) && targetLevel != ShaftNetwork.Carrage.LevelTarget)
		{
			ShaftNetwork.Carrage.LevelTarget = targetLevel;
			ShaftNetwork.Carrage.RefreshMovement();
		}
	}

	private async UniTaskVoid SetFromThread(int targetLevel)
	{
		await UniTask.SwitchToMainThread();
		SetElevatorTarget(targetLevel);
	}
}
