using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Entities;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class StackableLight : Stackable
{
	[Header("Road Flare")]
	public Light FlareLight;

	public float UpOffset;

	protected const float UP_SCALE = 0.1f;

	private new const float RENDER_DISTANCE = 100f;

	private float _actualLifetime;

	public const float LIFETIME = 180f;

	public float ActualLifetime
	{
		get
		{
			return _actualLifetime;
		}
		set
		{
			_actualLifetime = value;
		}
	}

	protected override float GetRenderMaxDistanceSquared()
	{
		if (base.ParentSlot != null)
		{
			return base.GetRenderMaxDistanceSquared();
		}
		return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	public override bool CanStack(IMergeable targetStack)
	{
		if (base.CanStack(targetStack) && !OnOff)
		{
			if (targetStack is RoadFlare roadFlare)
			{
				return !roadFlare.OnOff;
			}
			return false;
		}
		return false;
	}

	public override void Awake()
	{
		base.Awake();
		_actualLifetime = 180f + (float)Random.Range(0, 5);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new RoadflareSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is RoadflareSaveData roadflareSaveData)
		{
			_actualLifetime = roadflareSaveData.Lifetime;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is RoadflareSaveData roadflareSaveData)
		{
			roadflareSaveData.Lifetime = _actualLifetime;
		}
	}

	public override DelayedActionInstance OnUseSecondary(bool doAction = false, float actionCompletedRatio = 1f)
	{
		if (OnOff || ActualLifetime <= 0f || actionCompletedRatio < 1f)
		{
			return null;
		}
		DelayedActionInstance result = new DelayedActionInstance
		{
			Duration = 0.5f,
			ActionMessage = "Light Flare"
		};
		if (!doAction)
		{
			return result;
		}
		StackableLight stackableLight = this;
		if (base.Quantity > 1)
		{
			Quaternion rotation = Rotation;
			rotation.SetLookRotation(Vector3.forward);
			stackableLight = OnServer.Create<StackableLight>(base.SourcePrefab, GetSafeDropPosition(base.Position, RootParentHuman.CharacterRotationY.rotation * Vector3.forward, 0.5f), rotation);
			if (RootParent is Human human)
			{
				if (human.RightHandSlot.IsEmpty())
				{
					OnServer.MoveToSlotOrWorld(stackableLight, human.RightHandSlot);
				}
				else if (human.LeftHandSlot.IsEmpty())
				{
					OnServer.MoveToSlotOrWorld(stackableLight, human.LeftHandSlot);
				}
			}
			stackableLight?.SetCustomColor(CustomColor);
			DecrementQuantity();
		}
		stackableLight?.WaitThenBurn().Forget();
		return result;
	}

	public async UniTaskVoid WaitThenBurn()
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		while (GameManager.GameState != GameState.Running)
		{
			await UniTask.NextFrame();
		}
		await UniTask.NextFrame();
		Thing.Interact(base.InteractOnOff, 1);
	}

	private void HandleInventoryChange(Thing parent)
	{
		if (parent is Human { IsLocalPlayer: not false })
		{
			FlareLight.cullingMask &= ~((1 << (int)Layers.Player) | (int)Layers.PlayerInvisible);
		}
		else
		{
			FlareLight.cullingMask |= (1 << (int)Layers.Player) | (int)Layers.PlayerInvisible;
		}
	}

	public override void OnParentLocalityChange()
	{
		base.OnParentLocalityChange();
		Thing parent = ((base.ParentSlot?.Parent != null) ? base.ParentSlot.Parent.RootParent : null);
		HandleInventoryChange(parent);
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		HandleInventoryChange(parent.RootParent);
	}

	public override void OnExitInventory(Thing oldParent)
	{
		base.OnExitInventory(oldParent);
		Thing parent = ((base.ParentSlot?.Parent != null) ? base.ParentSlot.Parent.RootParent : null);
		HandleInventoryChange(parent);
	}

	public override void SetCustomColor(int index, bool emissive = false)
	{
		base.SetCustomColor(index, emissive);
		foreach (ThingLight light in Lights)
		{
			light.Light.color = CustomColor.Light;
		}
		FlareLight.color = CustomColor.Light;
		SetCustomColor(OnOff && Powered);
	}
}
