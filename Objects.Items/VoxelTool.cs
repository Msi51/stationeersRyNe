using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using TerrainSystem;
using UnityEngine;

namespace Objects.Items;

public class VoxelTool : PowerTool
{
	[Tooltip("How forced cooldown length between voxel additions")]
	public float UsageCooldown = 0.25f;

	[Tooltip("The density of the voxel")]
	public byte VoxelPaintDensity = byte.MaxValue;

	[Tooltip("The amount of power to use each voxel placement")]
	public byte UsageEachVoxelPlacement = 1;

	[Tooltip("Blueprint for visualizing")]
	public GameObject VoxelBlueprint;

	public Wireframe CubeWireFrame;

	private Color _cantPlaceColor = new Color(1f, 0f, 0f, 0.3f);

	private Color _canPlaceColor = new Color(0f, 1f, 0f, 0.3f);

	public bool DoVisualize;

	private float _usageCoolDown = 0.25f;

	private const float VOXEL_MAX_DENSITY_FOR_PLACEMENT = 40f / 51f;

	public static readonly int InsertCanisterHash = Animator.StringToHash("InsertCanister");

	private bool _voxelToolInUse;

	public Slot DirtCanisterSlot => Slots[1];

	public virtual DirtCanister DirtCanister => DirtCanisterSlot?.Occupant as DirtCanister;

	public override void Awake()
	{
		base.Awake();
		VoxelBlueprint.transform.parent = null;
	}

	public override void UpdateEachFrame()
	{
		if (!WorldManager.IsGamePaused)
		{
			base.UpdateEachFrame();
			if (_usageCoolDown > 0f)
			{
				_usageCoolDown -= GameManager.DeltaTime;
			}
		}
	}

	public void LateUpdate()
	{
		base.LateUpdateEachFrame();
		if (OnOff && Powered && InventoryManager.ActiveHandSlot?.Occupant?.ReferenceId == base.ReferenceId)
		{
			if (!VoxelBlueprint.gameObject.activeSelf)
			{
				VoxelBlueprint.gameObject.SetActive(value: true);
			}
			Vector3 vector = (AllowForwardCursor ? CursorManager.CursorPositionForward : CursorManager.CursorPlanePosition).ToGrid(1f).ToVector3();
			if (!(base.transform.position == vector))
			{
				float densityWorldSpace = VoxelTerrain.GetDensityWorldSpace(vector);
				CubeWireFrame.BlueprintRenderer.material.color = (((bool)DirtCanister && DirtCanister.GetCurrentCollectedDirtAmount() > 0f && densityWorldSpace <= 40f / 51f) ? _canPlaceColor : _cantPlaceColor);
				VoxelBlueprint.transform.position = vector;
			}
		}
		else if (VoxelBlueprint.gameObject.activeSelf)
		{
			VoxelBlueprint.gameObject.SetActive(value: false);
		}
	}

	public override void OnUsePrimary(Vector3 targetLocation, Quaternion targetRotation, ulong steamId, bool authoringMode)
	{
		base.OnUsePrimary(targetLocation, targetRotation, steamId, authoringMode);
		if (!_voxelToolInUse && OnOff && Powered && CanRemoveDirt())
		{
			UseTool().Forget();
		}
		if (!(_usageCoolDown > 0f) && Powered && CanRemoveDirt() && !(VoxelTerrain.GetDensityWorldSpace(targetLocation) > 40f / 51f))
		{
			OnServer.PlaceVoxelAtWorldPosition(base.ReferenceId, targetLocation, VoxelNodeType.Dirt);
			_usageCoolDown = UsageCooldown;
		}
	}

	public bool CanRemoveDirt()
	{
		if ((bool)DirtCanister)
		{
			return DirtCanister.GetCurrentCollectedDirtAmount() >= (float)(int)UsageEachVoxelPlacement;
		}
		return false;
	}

	public void RemoveDirtFromDirtBag()
	{
		if ((bool)DirtCanister)
		{
			DirtCanister.RemoveDirt((int)UsageEachVoxelPlacement);
		}
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		VoxelBlueprint.transform.rotation = Quaternion.identity;
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new VoxelToolSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		if (newChild is DirtCanister)
		{
			PlaySound(InsertCanisterHash);
		}
		base.OnChildEnterInventory(newChild);
	}

	private async UniTaskVoid UseTool()
	{
		_voxelToolInUse = true;
		Interact(InteractableType.Activate, 1);
		while (KeyManager.GetMouse("Primary") && !KeyManager.GetButton(KeyMap.SwapHands) && OnOff && Powered && CanRemoveDirt())
		{
			await UniTask.Yield();
		}
		Interact(InteractableType.Activate, 0);
		_voxelToolInUse = false;
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		Object.Destroy(VoxelBlueprint);
	}
}
