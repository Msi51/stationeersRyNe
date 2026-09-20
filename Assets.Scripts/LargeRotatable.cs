using System;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts;

public abstract class LargeRotatable : LargeElectrical, IRotatable
{
	[Header("Large Rotatable")]
	public Transform DishTransform;

	public Vector3 DishForward;

	public Transform HorizontalPivot;

	[SerializeField]
	private float SpeedHorizontal = 0.1f;

	[SerializeField]
	private float SpeedVertical = 0.1f;

	protected double _vertical;

	protected double _horizontal;

	public RotatableBehaviour RotatableBehaviour { get; set; }

	public virtual float RotationTolerance => 0.0001f;

	public double MaximumVertical => 90.0;

	public double MaximumHorizontal => 360.0;

	public virtual float MovementSpeedHorizontal => SpeedHorizontal;

	public virtual float MovementSpeedVertical => SpeedVertical;

	public virtual double Vertical
	{
		get
		{
			return _vertical;
		}
		set
		{
			if (Math.Abs(_vertical - value) > 1.401298464324817E-45)
			{
				_vertical = value;
				DishForward = DishTransform.forward;
				SetDishRotation();
			}
		}
	}

	public virtual double Horizontal
	{
		get
		{
			return _horizontal;
		}
		set
		{
			if (Math.Abs(_horizontal - value) > 1.401298464324817E-45)
			{
				_horizontal = value;
				DishForward = DishTransform.forward;
				SetDishRotation();
			}
		}
	}

	public bool CanRotate()
	{
		if (OnOff && Powered)
		{
			return base.IsStructureCompleted;
		}
		return false;
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new SatelliteDishSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is RotatableSaveData rotatableSaveData)
		{
			Horizontal = rotatableSaveData.Horizontal;
			Vertical = rotatableSaveData.Vertical;
			RotatableBehaviour.TargetHorizontal = rotatableSaveData.TargetHorizontal;
			RotatableBehaviour.TargetVertical = rotatableSaveData.TargetVertical;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is RotatableSaveData rotatableSaveData)
		{
			rotatableSaveData.Horizontal = Horizontal;
			rotatableSaveData.Vertical = Vertical;
			rotatableSaveData.TargetHorizontal = RotatableBehaviour?.TargetHorizontal ?? 0.0;
			rotatableSaveData.TargetVertical = RotatableBehaviour?.TargetHorizontal ?? 0.0;
		}
	}

	public override void Awake()
	{
		base.Awake();
		if (RotatableBehaviour == null)
		{
			RotatableBehaviour obj = new RotatableBehaviour(this)
			{
				MaxAudibleSquareDistance = 600f
			};
			RotatableBehaviour rotatableBehaviour = obj;
			RotatableBehaviour = obj;
		}
	}

	public void RunAfterAnimation()
	{
	}

	public abstract UniTaskVoid UpdateAnimator();

	public void SetDishRotation()
	{
		float y = Mathf.Lerp(0f, 360f, (float)_horizontal);
		float x = Mathf.Lerp(-90f, 0f, (float)_vertical);
		HorizontalPivot.localRotation = Quaternion.Euler(0f, y, 0f);
		DishTransform.localRotation = Quaternion.Euler(x, 0f, 0f);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		InteractableType action = interactable.Action;
		if (action == InteractableType.OnOff || action == InteractableType.Powered)
		{
			RotatableBehaviour.MoveToTarget();
		}
	}
}
