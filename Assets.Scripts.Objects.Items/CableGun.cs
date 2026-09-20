using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UI;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class CableGun : PowerTool
{
	private readonly struct RunSegment(int startIndex, int optionIndex)
	{
		public readonly int StartIndex = startIndex;

		public readonly int OptionIndex = optionIndex;
	}

	private static readonly int EquipDrillHash = Animator.StringToHash("EquipDrill");

	private static readonly int UnEquipDrillHash = Animator.StringToHash("UnEquipDrill");

	private static readonly int MaxPreviewSegments = 256;

	private static readonly float ArmDelay = 0.2f;

	private static readonly int _smallGridStep = (int)(SmallGrid.SmallGridSize * 10f);

	private static readonly int CableAmmoHash = Animator.StringToHash("CableAmmo");

	private readonly Grid3[] _previewCells = new Grid3[MaxPreviewSegments];

	private readonly bool[] _previewCellValid = new bool[MaxPreviewSegments];

	private int _previewCellCount;

	private int _invalidCellIndex = -1;

	private int _runCost;

	private Quaternion _runRotation;

	private CanConstructInfo _placementInfo = CanConstructInfo.ValidPlacement;

	private Grid3 _lastLineOrigin;

	private Grid3 _lastLineEnd;

	private int _lastLineAxis;

	private int _lastLineSign;

	private int _lastLineCount = -1;

	private long _lastLineCoilId;

	private static readonly string PreviewPoolName = "~CableGunPreviewPool";

	private static readonly string PreviewPieceName = "~CableGunPreview";

	private readonly Wireframe[] _previewPool = new Wireframe[MaxPreviewSegments];

	private Wireframe _previewSourceWireframe;

	private Transform _previewPoolParent;

	private int _previewActiveCount;

	private readonly List<(int length, int optionIndex, int cost)> _segmentKinds = new List<(int, int, int)>();

	private readonly List<RunSegment> _runSegments = new List<RunSegment>();

	private bool _placing;

	private Vector3 _runOrigin;

	private bool _arming;

	private float _armStartTime;

	public override int EquipSoundHash => EquipDrillHash;

	public override int UnEquipSoundHash => UnEquipDrillHash;

	public override int FinishedConstructingSoundHash => Animator.StringToHash("ScrewdriverFinished");

	public override int FinishedDeconstructingSoundHash => Animator.StringToHash("ScrewdriverFinished");

	public Slot CableSlot { get; private set; }

	private Transform PreviewPoolParent
	{
		get
		{
			if (!_previewPoolParent)
			{
				_previewPoolParent = new GameObject(PreviewPoolName).transform;
			}
			return _previewPoolParent;
		}
	}

	private MultiConstructor GetCoil()
	{
		return CableSlot?.Occupant as MultiConstructor;
	}

	private CableToolBelt FindCableBelt()
	{
		if (!(RootParent is Human human))
		{
			return null;
		}
		for (int i = 0; i < human.Slots.Count; i++)
		{
			Slot slot = human.Slots[i];
			if (slot != null && slot.Type == Slot.Class.Belt && slot.Occupant is CableToolBelt result)
			{
				return result;
			}
		}
		return null;
	}

	private MultiConstructor FindBeltCoil(int prefabHash)
	{
		return FindCableBelt()?.FindMatchingCoil(prefabHash);
	}

	private void SetErrorState(bool on)
	{
		if (HasErrorState)
		{
			int num = (on ? 1 : 0);
			if (Error != num)
			{
				OnServer.Interact(base.InteractError, num);
			}
		}
	}

	private void MoveCoilIntoGun(MultiConstructor coil)
	{
		if ((object)coil == null || CableSlot == null || coil.ParentSlot == CableSlot)
		{
			return;
		}
		MultiConstructor multiConstructor = CableSlot.Occupant as MultiConstructor;
		if (multiConstructor != null)
		{
			if (multiConstructor.Quantity > 0)
			{
				return;
			}
			CableSlot.Empty();
			multiConstructor.ParentSlot = null;
			OnServer.Destroy(multiConstructor);
		}
		OnServer.MoveToSlot(coil, CableSlot);
	}

	public override void Awake()
	{
		base.Awake();
		CableSlot = Slots.Find((Slot s) => s.StringHash == CableAmmoHash);
		InventoryManager.OnActiveHandChanged += OnActiveHandChangedHandler;
	}

	public override void OnDestroy()
	{
		InventoryManager.OnActiveHandChanged -= OnActiveHandChangedHandler;
		base.OnDestroy();
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable == base.InteractOnOff && !OnOff)
		{
			ClearActivePlacement();
		}
	}

	public override void OnExitInventory(Thing oldParent)
	{
		base.OnExitInventory(oldParent);
		SetErrorState(on: false);
		ClearActivePlacement();
	}

	private void OnActiveHandChangedHandler()
	{
		if ((_placing || _previewActiveCount > 0) && InventoryManager.ActiveHandSlot?.Occupant != this)
		{
			ClearActivePlacement();
		}
	}

	public void TickPlacement()
	{
		if (!IsOperable)
		{
			SetErrorState(on: false);
			Thing.Interact(base.InteractOnOff, 0);
			ClearActivePlacement();
			return;
		}
		if ((object)GetCoil() == null)
		{
			SetErrorState(on: true);
			ClearActivePlacement();
			return;
		}
		SetErrorState(on: false);
		if (!_placing)
		{
			UpdateIdlePreview();
			HandleStartInput();
		}
		else
		{
			UpdatePlacingPreview();
			HandleCommitInput();
		}
	}

	private static Vector3 GetCursorPoint()
	{
		Vector3 normal;
		return InputHelpers.GetCameraForwardGrid(0.6f, 0f, out normal);
	}

	private void UpdateIdlePreview()
	{
		Vector3 cursorPoint = GetCursorPoint();
		TryGetCellLine(cursorPoint, cursorPoint);
		UpdatePreview();
	}

	private void HandleStartInput()
	{
		if (_arming)
		{
			if (!KeyManager.GetMouse("Primary"))
			{
				CancelArming();
				return;
			}
			float num = (Time.time - _armStartTime) / ArmDelay;
			if (num >= 1f)
			{
				CancelArming();
				_placing = true;
				_runOrigin = GetCursorPoint();
				_previewCellCount = 0;
				_invalidCellIndex = -1;
				_placementInfo = CanConstructInfo.ValidPlacement;
				_lastLineCount = -1;
			}
			else
			{
				ShowArmProgress(num);
			}
		}
		else if (KeyManager.GetMouseDown("Primary"))
		{
			_arming = true;
			_armStartTime = Time.time;
		}
	}

	private void UpdatePlacingPreview()
	{
		Vector3 cursorPoint = GetCursorPoint();
		TryGetCellLine(_runOrigin, cursorPoint);
		UpdatePreview();
	}

	private void HandleCommitInput()
	{
		if (_arming)
		{
			if (!KeyManager.GetMouse("Primary"))
			{
				CancelArming();
				return;
			}
			float num = (Time.time - _armStartTime) / ArmDelay;
			if (num >= 1f)
			{
				CancelArming();
				CommitPlacement();
			}
			else
			{
				ShowArmProgress(num);
			}
		}
		else if (KeyManager.GetMouseDown("Primary"))
		{
			_arming = true;
			_armStartTime = Time.time;
		}
	}

	private void CommitPlacement()
	{
		_placing = false;
		HidePreview();
		MultiConstructor coil = GetCoil();
		if ((object)coil != null && _previewCellCount > 0 && _invalidCellIndex == -1)
		{
			ulong referenceId = (ulong)InventoryManager.ParentBrain.ReferenceId;
			if (GameManager.RunSimulation)
			{
				PlaceCableRun(coil, _lastLineOrigin, _lastLineEnd, referenceId);
				return;
			}
			NetworkClient.SendToServer(new PlaceCableRunMessage
			{
				CableGunId = base.ReferenceId,
				CoilId = coil.ReferenceId,
				Origin = _lastLineOrigin,
				End = _lastLineEnd,
				CreatorSteamId = referenceId
			});
		}
	}

	public void PlaceCableRun(MultiConstructor coil, Grid3 origin, Grid3 end, ulong steamId)
	{
		if (!GameManager.RunSimulation || !coil)
		{
			return;
		}
		int straightIndex = GetStraightIndex(coil);
		if (straightIndex < 0)
		{
			return;
		}
		ResolveRunAxis(origin, end, out var axis, out var sign, out var count);
		if (count <= 0 || count > MaxPreviewSegments)
		{
			return;
		}
		GetLinearGrid3(origin, axis, sign, count, _previewCells);
		_runRotation = RotationForRun(axis, sign);
		if (FindFirstInvalidCell(coil, straightIndex, count, _previewCells) != -1)
		{
			return;
		}
		int num = BuildRunSegments(coil, count);
		if (num < 0 || coil.Quantity < num || !base.Battery || base.Battery.PowerStored < (float)(_runSegments.Count * BasePowerUsage))
		{
			return;
		}
		int prefabHash = coil.PrefabHash;
		int num2 = 0;
		foreach (RunSegment segment in _runSegments)
		{
			if (!OnUseItem(1f, null))
			{
				break;
			}
			coil.Construct(_previewCells[segment.StartIndex], _runRotation, segment.OptionIndex, null, authoringMode: true, steamId);
			num2 += _segmentKinds.Find(((int length, int optionIndex, int cost) k) => k.optionIndex == segment.OptionIndex).cost;
		}
		ConsumeCablesAfterRun(prefabHash, num2);
	}

	private void ConsumeCablesAfterRun(int prefabHash, int totalCost)
	{
		int num = totalCost;
		MultiConstructor coil = GetCoil();
		if (coil != null && coil.PrefabHash == prefabHash && coil.Quantity > 0)
		{
			int num2 = Mathf.Min(num, coil.Quantity);
			coil.OnUseItem(num2, null);
			num -= num2;
		}
		MultiConstructor coil2 = GetCoil();
		if ((object)coil2 == null || coil2.Quantity <= 0)
		{
			MultiConstructor multiConstructor = FindBeltCoil(prefabHash);
			if (multiConstructor != null)
			{
				MoveCoilIntoGun(multiConstructor);
			}
		}
	}

	private static void ResolveRunAxis(Grid3 origin, Grid3 end, out int axis, out int sign, out int count)
	{
		int num = end.x - origin.x;
		int num2 = end.y - origin.y;
		int num3 = end.z - origin.z;
		int num4 = Mathf.Abs(num);
		int num5 = Mathf.Abs(num2);
		int num6 = Mathf.Abs(num3);
		int num7;
		if (num4 >= num5 && num4 >= num6)
		{
			axis = 0;
			num7 = num;
		}
		else if (num5 >= num6)
		{
			axis = 1;
			num7 = num2;
		}
		else
		{
			axis = 2;
			num7 = num3;
		}
		sign = ((num7 >= 0) ? 1 : (-1));
		count = Mathf.Abs(num7) / _smallGridStep + 1;
	}

	private void ShowArmProgress(float progress)
	{
		UIProgressionBar uIProgressionBar = InventoryManager.Instance?.UIProgressionBar;
		if ((bool)uIProgressionBar)
		{
			uIProgressionBar.SetProgress(progress, ActionStrings.Build, GetCoil()?.DisplayName ?? DisplayName);
			uIProgressionBar.SetActive(active: true);
		}
	}

	private void CancelArming()
	{
		if (_arming)
		{
			_arming = false;
			InventoryManager.Instance?.UIProgressionBar?.SetActive(active: false);
		}
	}

	private void ClearActivePlacement()
	{
		CancelArming();
		_placing = false;
		_previewCellCount = 0;
		_invalidCellIndex = -1;
		_runCost = 0;
		_placementInfo = CanConstructInfo.ValidPlacement;
		_lastLineCount = -1;
		HidePreview();
	}

	public PassiveTooltip BuildTooltip()
	{
		PassiveTooltip passiveTooltip = new PassiveTooltip(true);
		passiveTooltip.Action = ActionStrings.Build;
		PassiveTooltip result = passiveTooltip;
		MultiConstructor coil = GetCoil();
		if (!coil)
		{
			result.ConstructString = GameStrings.CableRunLength.AsString("0", DisplayName, "0", GameStrings.CableRunNoCoil.DisplayString);
			return result;
		}
		int straightIndex = GetStraightIndex(coil);
		Structure structure = ((straightIndex >= 0) ? coil.Constructables[straightIndex] : null);
		result.Title = (((object)structure != null) ? structure.DisplayName : DisplayName);
		if (!_placementInfo.CanConstruct)
		{
			result.color = Color.red;
			result.State = "<color=red>" + _placementInfo.ErrorMessage + "</color>";
		}
		else if (_previewCellCount > 0 && (bool)structure)
		{
			result.color = Color.green;
			result.ConstructString = GameStrings.CableRunLength.AsString(StringManager.Get(_previewCellCount), structure.DisplayName, StringManager.Get(_runCost), coil.DisplayName);
		}
		return result;
	}

	private static int GetStraightIndex(MultiConstructor coil)
	{
		for (int i = 0; i < coil.Constructables.Count; i++)
		{
			if (coil.Constructables[i] is Cable { IsStraight: not false, StraightUnitLength: 1 })
			{
				return i;
			}
		}
		return -1;
	}

	private void CollectSegmentKinds(MultiConstructor coil)
	{
		_segmentKinds.Clear();
		for (int i = 0; i < coil.Constructables.Count; i++)
		{
			if (coil.Constructables[i] is Cable { IsStraight: not false, StraightUnitLength: >0 } cable)
			{
				_segmentKinds.Add((cable.StraightUnitLength, i, cable.BuildStates[0].Tool.EntryQuantity));
			}
		}
		_segmentKinds.Sort(((int length, int optionIndex, int cost) a, (int length, int optionIndex, int cost) b) => b.length.CompareTo(a.length));
	}

	private int BuildRunSegments(MultiConstructor coil, int count)
	{
		_runSegments.Clear();
		CollectSegmentKinds(coil);
		int num = 0;
		int num2 = 0;
		while (num2 < count)
		{
			int num3 = count - num2;
			bool flag = false;
			foreach (var (num4, optionIndex, num5) in _segmentKinds)
			{
				if (num4 <= num3)
				{
					_runSegments.Add(new RunSegment(num2, optionIndex));
					num += num5;
					num2 += num4;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				_runSegments.Clear();
				return -1;
			}
		}
		return num;
	}

	private void TryGetCellLine(Vector3 originWorld, Vector3 endWorld)
	{
		_runCost = 0;
		MultiConstructor coil = GetCoil();
		if ((object)coil == null)
		{
			_previewCellCount = 0;
			_placementInfo = CanConstructInfo.ValidPlacement;
			return;
		}
		int straightIndex = GetStraightIndex(coil);
		if (straightIndex < 0)
		{
			_previewCellCount = 0;
			_placementInfo = CanConstructInfo.ValidPlacement;
			return;
		}
		GridController world = GridController.World;
		if (world == null)
		{
			_previewCellCount = 0;
			_placementInfo = CanConstructInfo.ValidPlacement;
			return;
		}
		Grid3 grid = world.WorldToLocalGrid(originWorld, SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset);
		Grid3 grid2 = world.WorldToLocalGrid(endWorld, SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset);
		ResolveRunAxis(grid, grid2, out var axis, out var sign, out var count);
		if (count > MaxPreviewSegments)
		{
			_previewCellCount = 0;
			_placementInfo = CanConstructInfo.InvalidPlacement(GameStrings.CableRunTooLong.DisplayString);
			return;
		}
		Quaternion runRotation = RotationForRun(axis, sign);
		int num = BuildRunSegments(coil, count);
		if (num < 0)
		{
			_previewCellCount = 0;
			_placementInfo = CanConstructInfo.ValidPlacement;
			return;
		}
		if (coil.Quantity < num)
		{
			_previewCellCount = 0;
			_placementInfo = CanConstructInfo.InvalidPlacement(GameStrings.CableRunNotEnoughCable.AsString(coil.DisplayName, num.ToString()));
			return;
		}
		_runCost = num;
		if (count == _lastLineCount && axis == _lastLineAxis && sign == _lastLineSign && grid.Equals(_lastLineOrigin) && coil.ReferenceId == _lastLineCoilId)
		{
			_previewCellCount = count;
			_runRotation = runRotation;
			return;
		}
		_lastLineOrigin = grid;
		_lastLineEnd = grid2;
		_lastLineAxis = axis;
		_lastLineSign = sign;
		_lastLineCount = count;
		_lastLineCoilId = coil.ReferenceId;
		GetLinearGrid3(grid, axis, sign, count, _previewCells);
		_previewCellCount = count;
		_runRotation = runRotation;
		_invalidCellIndex = FindFirstInvalidCell(coil, straightIndex, count, _previewCells);
	}

	private int FindFirstInvalidCell(MultiConstructor coil, int straightIndex, int count, Grid3[] cells)
	{
		_placementInfo = CanConstructInfo.ValidPlacement;
		Structure structureCursor = InventoryManager.GetStructureCursor(coil.Constructables[straightIndex].name);
		if ((object)structureCursor == null)
		{
			for (int i = 0; i < count; i++)
			{
				_previewCellValid[i] = false;
			}
			if (count > 0)
			{
				_placementInfo = CanConstructInfo.InvalidPlacement(string.Empty);
				return 0;
			}
			return -1;
		}
		Vector3 thingTransformPosition = structureCursor.ThingTransformPosition;
		Quaternion thingTransformRotation = structureCursor.ThingTransformRotation;
		int num = -1;
		for (int j = 0; j < count; j++)
		{
			structureCursor.ThingTransformPosition = cells[j].ToVector3();
			structureCursor.ThingTransformRotation = _runRotation;
			CanConstructInfo placementInfo = structureCursor.CanConstruct();
			if (placementInfo.CanConstruct)
			{
				Cable cable = GridController.World.GetSmallCell(cells[j])?.Cable;
				if ((object)cable != null)
				{
					placementInfo = CanConstructInfo.InvalidPlacement(GameStrings.CannotMergeWithSmallGrid.AsString(cable.DisplayName));
				}
			}
			_previewCellValid[j] = placementInfo.CanConstruct;
			if (!placementInfo.CanConstruct && num == -1)
			{
				num = j;
				_placementInfo = placementInfo;
			}
		}
		structureCursor.ThingTransformPosition = thingTransformPosition;
		structureCursor.ThingTransformRotation = thingTransformRotation;
		return num;
	}

	public static void GetLinearGrid3(Grid3 origin, int axis, int sign, int count, Grid3[] cells)
	{
		for (int i = 0; i < count; i++)
		{
			int num = i * _smallGridStep * sign;
			int num2 = i;
			cells[num2] = axis switch
			{
				0 => new Grid3(origin.x + num, origin.y, origin.z), 
				1 => new Grid3(origin.x, origin.y + num, origin.z), 
				_ => new Grid3(origin.x, origin.y, origin.z + num), 
			};
		}
	}

	public static Quaternion RotationForRun(int axis, int sign)
	{
		return axis switch
		{
			0 => Quaternion.Euler(0f, -90f * (float)sign, 0f), 
			1 => Quaternion.Euler(90f * (float)sign, 0f, 0f), 
			_ => (sign > 0) ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity, 
		};
	}

	private void UpdatePreview()
	{
		Wireframe previewWireframe = GetPreviewWireframe();
		if (!previewWireframe || _previewCellCount <= 0)
		{
			HidePreview();
			return;
		}
		if (previewWireframe != _previewSourceWireframe)
		{
			ClearPreview();
			_previewSourceWireframe = previewWireframe;
		}
		for (int i = 0; i < _previewCellCount; i++)
		{
			Wireframe previewPiece = GetPreviewPiece(i);
			previewPiece.transform.SetPositionAndRotation(_previewCells[i].ToVector3(), _runRotation);
			if (!previewPiece.gameObject.activeSelf)
			{
				previewPiece.gameObject.SetActive(value: true);
			}
			if ((bool)previewPiece.BlueprintRenderer)
			{
				previewPiece.BlueprintRenderer.material.color = (_previewCellValid[i] ? Color.green : Color.red);
			}
		}
		for (int j = _previewCellCount; j < _previewActiveCount; j++)
		{
			if ((bool)_previewPool[j] && _previewPool[j].gameObject.activeSelf)
			{
				_previewPool[j].gameObject.SetActive(value: false);
			}
		}
		_previewActiveCount = _previewCellCount;
	}

	private Wireframe GetPreviewWireframe()
	{
		MultiConstructor coil = GetCoil();
		if ((object)coil == null)
		{
			return null;
		}
		int straightIndex = GetStraightIndex(coil);
		if (straightIndex < 0)
		{
			return null;
		}
		Structure structureCursor = InventoryManager.GetStructureCursor(coil.Constructables[straightIndex].name);
		if (!structureCursor)
		{
			return null;
		}
		return structureCursor.Wireframe;
	}

	private Wireframe GetPreviewPiece(int index)
	{
		if (!_previewPool[index])
		{
			_previewPool[index] = CreatePreviewPiece();
		}
		return _previewPool[index];
	}

	private Wireframe CreatePreviewPiece()
	{
		Wireframe wireframe = Object.Instantiate(_previewSourceWireframe, PreviewPoolParent);
		wireframe.name = PreviewPieceName;
		wireframe.gameObject.SetActive(value: false);
		return wireframe;
	}

	private void HidePreview()
	{
		for (int i = 0; i < _previewActiveCount; i++)
		{
			if ((bool)_previewPool[i] && _previewPool[i].gameObject.activeSelf)
			{
				_previewPool[i].gameObject.SetActive(value: false);
			}
		}
		_previewActiveCount = 0;
	}

	private void ClearPreview()
	{
		for (int i = 0; i < _previewPool.Length; i++)
		{
			if ((bool)_previewPool[i])
			{
				Object.Destroy(_previewPool[i].gameObject);
			}
			_previewPool[i] = null;
		}
		_previewActiveCount = 0;
	}
}
