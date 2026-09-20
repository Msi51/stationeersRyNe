using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using Trading;
using UnityEngine;
using UnityEngine.UI;
using Util;

namespace Assets.Scripts.Objects.Electrical;

public class Computer : Device, ISmartRotatable, IComputer, IReferencable, IEvaluable, ITrading
{
	public const float GRAPHICS_RAYCAST_SQR_RANGE = 16f;

	[SerializeField]
	private GraphicRaycaster _GraphicRaycaster;

	[Header("Computer")]
	public GameObject ComputerScreen;

	[SerializeField]
	private SlidingPanelAnimationComponent _motherboardPanel;

	[SerializeField]
	private BoxCollider _motherboardSlotCollider;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	private bool _humanInZone;

	private Bounds _viewableBounds;

	private CancellationTokenWrapper _cancellation = new CancellationTokenWrapper();

	public GameObject Screen
	{
		get
		{
			return ComputerScreen;
		}
		set
		{
			ComputerScreen = value;
		}
	}

	public Motherboard CurrentMotherboard { get; set; }

	public virtual bool ShowComputerScreen
	{
		get
		{
			if (!IsOccluded && OnOff && Powered)
			{
				return _humanInZone;
			}
			return false;
		}
	}

	public GraphicRaycaster GraphicRaycaster => _GraphicRaycaster;

	protected override bool IsOperable
	{
		get
		{
			if (CurrentMotherboard != null)
			{
				return !CurrentMotherboard.IsError;
			}
			return false;
		}
	}

	public bool HasMotherboard => CurrentMotherboard != null;

	public Thing AsThing()
	{
		return this;
	}

	public Device AsDevice()
	{
		return this;
	}

	public float ZOffset()
	{
		return -2f;
	}

	public void CheckStatus()
	{
		if (GameManager.RunSimulation)
		{
			if (!IsOperable && Error == 0)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			else if (IsOperable && Error == 1)
			{
				OnServer.Interact(base.InteractError, 0);
			}
		}
	}

	public List<ILogicable> DeviceList()
	{
		if ((object)base.DataCable == null)
		{
			FindDataCable();
		}
		if (!(base.DataCable != null) || base.DataCable.CableNetwork == null)
		{
			return new List<ILogicable>();
		}
		return new List<ILogicable>(base.DataCable.CableNetwork.DataDeviceList);
	}

	public override void Awake()
	{
		base.Awake();
		_viewableBounds = new Bounds(Transform.position + Transform.forward * 4.5f, new Vector3(10f, 10f, 10f));
	}

	private async UniTaskVoid CheckPlayerWithinViewableRange(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			await UniTask.Delay(500, ignoreTimeScale: false, PlayerLoopTiming.Update, cancellationToken);
			await UniTask.SwitchToMainThread();
			_GraphicRaycaster.enabled = CurrentCameraDistanceSquared < 16f;
			if (!(InventoryManager.ParentHuman == null))
			{
				Vector3 point = InventoryManager.ParentHuman.Transform.position;
				bool flag = _viewableBounds.Contains(point);
				if (flag != _humanInZone)
				{
					_humanInZone = flag;
					EnableAppropriateScreen();
				}
			}
		}
	}

	public override void InitializeDevice()
	{
		base.InitializeDevice();
		InitializeNextFrame().Forget();
	}

	public override void OnRenamed()
	{
		base.OnRenamed();
		if ((bool)CurrentMotherboard)
		{
			CurrentMotherboard.OnDeviceListChanged();
		}
		if ((bool)CurrentMotherboard)
		{
			CurrentMotherboard.OnRenamed();
		}
	}

	protected override void UpdateSoundsOnBuildState(bool construct)
	{
		foreach (GameAudioEvent audioEvent in AudioEvents)
		{
			if (audioEvent.Conditions.Count > 0)
			{
				if (audioEvent.IsValid && audioEvent.ClipsData.IsLooping)
				{
					audioEvent.Trigger();
				}
				else
				{
					audioEvent.Stop();
				}
			}
		}
		base.UpdateSoundsOnBuildState(construct);
	}

	private async UniTaskVoid InitializeNextFrame()
	{
		while (GameManager.GameState != GameState.Running)
		{
			await UniTask.NextFrame();
		}
		await UniTask.NextFrame();
		await UniTask.NextFrame();
		if ((bool)CurrentMotherboard)
		{
			CurrentMotherboard.OnDeviceListChanged();
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (_motherboardPanel != null)
		{
			_motherboardPanel.RefreshState(skipAnimation);
		}
	}

	public override void OnNetworkedRefresh(Device device)
	{
		base.OnNetworkedRefresh(device);
		if ((bool)CurrentMotherboard)
		{
			CurrentMotherboard.RefreshDevice(device);
		}
	}

	public override void OnNetworkedDeviceNameChanged(Device device)
	{
		base.OnNetworkedDeviceNameChanged(device);
		if ((bool)CurrentMotherboard)
		{
			CurrentMotherboard.OnDeviceListChanged();
		}
	}

	public override void OnAddCableNetwork(CableNetwork newNetwork)
	{
		base.OnAddCableNetwork(newNetwork);
		FindDataCable();
		if ((bool)CurrentMotherboard)
		{
			CurrentMotherboard.OnDeviceListChanged();
		}
	}

	public override void OnRemoveCableNetwork(CableNetwork oldNetwork)
	{
		base.OnRemoveCableNetwork(oldNetwork);
		FindDataCable();
		if ((bool)CurrentMotherboard)
		{
			CurrentMotherboard.OnDeviceListChanged();
		}
	}

	public override void OnDeviceConnectToNetwork(Device device)
	{
		base.OnDeviceConnectToNetwork(device);
		if ((bool)CurrentMotherboard)
		{
			CurrentMotherboard.OnDeviceListChanged();
		}
	}

	public override void OnDeviceDisconnectFromNetwork(Device device)
	{
		base.OnDeviceDisconnectFromNetwork(device);
		if ((bool)CurrentMotherboard)
		{
			CurrentMotherboard.OnDeviceListChanged();
		}
	}

	public void CheckMotherboardMissingError()
	{
		OnServer.Interact(base.InteractError, (!HasMotherboard) ? 1 : 0);
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		Motherboard motherboard = newChild as Motherboard;
		if ((bool)motherboard)
		{
			CurrentMotherboard = motherboard;
			foreach (GameObject screen in CurrentMotherboard.Screens)
			{
				screen.transform.SetParent(Screen.transform, worldPositionStays: false);
				screen.transform.localRotation = Quaternion.identity;
				Vector3 zero = Vector3.zero;
				zero.z = ZOffset();
				screen.transform.localPosition = zero;
				screen.transform.localScale = Vector3.one;
				RectTransform component = screen.GetComponent<RectTransform>();
				component.offsetMin = Vector2.zero;
				component.offsetMax = Vector2.zero;
			}
			CurrentMotherboard.SetMode(isNormal: true);
			CurrentMotherboard.ParentComputer = this;
			CurrentMotherboard.OnInsertedToComputer(this);
			InitializeDataConnection();
			if (GameManager.RunSimulation && Error == 1 && !CurrentMotherboard.IsError)
			{
				OnServer.Interact(base.InteractError, 0);
			}
		}
		EnableAppropriateScreen();
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		if (CurrentMotherboard == previousChild && !previousChild.BeingDestroyed)
		{
			Motherboard currentMotherboard = CurrentMotherboard;
			foreach (GameObject screen in CurrentMotherboard.Screens)
			{
				screen.transform.SetParent(CurrentMotherboard.ThingTransform, worldPositionStays: false);
				screen.transform.localRotation = Quaternion.identity;
				screen.transform.localPosition = Vector3.zero;
				screen.transform.localScale = Vector3.one;
			}
			CurrentMotherboard.SetMode(isNormal: true);
			CurrentMotherboard.ParentComputer = null;
			CurrentMotherboard = null;
			currentMotherboard.OnRemovedFromComputer(this);
			CheckStatus();
		}
		EnableAppropriateScreen();
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		if (_motherboardPanel != null)
		{
			_motherboardPanel.RefreshState(skipAnimation: true);
		}
		CheckStatus();
		if ((bool)Screen)
		{
			EnableAppropriateScreen();
		}
	}

	public override void OnStartRender()
	{
		base.OnStartRender();
		if (_cancellation.Initialized)
		{
			_cancellation.Cancel();
		}
		_cancellation.Initialize();
		CheckPlayerWithinViewableRange(_cancellation.Token).Forget();
	}

	public void EnableAppropriateScreen()
	{
		if (!base.IsBeingDestroyed)
		{
			Screen.SetActive(ShowComputerScreen);
		}
	}

	public override void OnStopRender()
	{
		base.OnStopRender();
		if (_cancellation.Initialized)
		{
			_cancellation.Cancel();
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (_cancellation.Initialized)
		{
			_cancellation.Cancel();
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (GameManager.GameState == GameState.Running)
		{
			if (interactable.Action == InteractableType.Open && _motherboardSlotCollider != null)
			{
				_motherboardSlotCollider.enabled = IsOpen;
			}
			CheckStatus();
			if ((bool)Screen)
			{
				EnableAppropriateScreen();
			}
		}
	}

	protected override void OnBuildStateUpdated(int newState, int previousState)
	{
		base.OnBuildStateUpdated(newState, previousState);
		if (GameManager.RunSimulation && previousState == BuildStates.Count - 1)
		{
			MoveAllSlotItemsToWorld();
		}
	}

	public virtual void MoveAllSlotItemsToWorld()
	{
		if (Slots.Count <= 0)
		{
			return;
		}
		foreach (Slot slot in Slots)
		{
			if ((bool)slot.Occupant)
			{
				OnServer.MoveToWorld(slot.Occupant);
			}
		}
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
}
