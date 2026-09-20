using System.Threading;
using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Networks;
using UnityEngine;
using UnityEngine.UI;
using Util;

namespace Objects.Rockets;

public class CrewModuleScreen : Device, IRocketInternals, IRocketComponent, ISmartRotatable, IRocketPanelHolder
{
	[SerializeField]
	private RocketMotherboardPanel _rocketMotherboardPanel;

	[SerializeField]
	private GameObject _canvasGameObject;

	[SerializeField]
	private Canvas _inWorldCanvas;

	[SerializeField]
	private GraphicRaycaster _inWorldCanvasRaycaster;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	private bool _humanInZone;

	private CancellationTokenWrapper _viewableRangeCancellation = new CancellationTokenWrapper();

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => true;

	public RocketNetwork RocketNetwork { get; set; }

	public override void Awake()
	{
		base.Awake();
		_inWorldCanvas.worldCamera = CameraController.CurrentCamera;
		_rocketMotherboardPanel.Initialize(this);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		_viewableRangeCancellation.Cancel();
	}

	public override void OnStartRender()
	{
		base.OnStartRender();
		_viewableRangeCancellation.CancelAndInitialize();
		CheckPlayerWithinViewableRange(_viewableRangeCancellation.Token).Forget();
	}

	public override void OnStopRender()
	{
		base.OnStopRender();
		_viewableRangeCancellation.Cancel();
	}

	private async UniTaskVoid CheckPlayerWithinViewableRange(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			await UniTask.Delay(500, ignoreTimeScale: false, PlayerLoopTiming.Update, cancellationToken);
			await UniTask.SwitchToMainThread();
			_inWorldCanvasRaycaster.enabled = CurrentCameraDistanceSquared < 16f;
			if (!(InventoryManager.ParentHuman == null))
			{
				bool flag = PlayerInBounds(InventoryManager.ParentHuman);
				if (flag != _humanInZone)
				{
					_humanInZone = flag;
					_canvasGameObject.SetActive(_humanInZone);
				}
			}
		}
	}

	private bool PlayerInBounds(Human player)
	{
		Vector3 point = player.Transform.position;
		return new Bounds(Transform.position + Transform.forward * 2f, new Vector3(4f, 4f, 4f)).Contains(point);
	}

	public override void Update1000MS(float deltaTime)
	{
		base.Update1000MS(deltaTime);
		RefreshScreen();
	}

	private void RefreshScreen()
	{
		foreach (IRocketInternals @internal in RocketNetwork.Internals)
		{
			if (@internal is RocketAvionicsDevice connectedRocketInfo)
			{
				_rocketMotherboardPanel.SetConnectedRocketInfo(connectedRocketInfo);
				return;
			}
		}
		_rocketMotherboardPanel.SetConnectedRocketInfo(null);
	}

	public void ToggleUI()
	{
		foreach (IRocketInternals @internal in RocketNetwork.Internals)
		{
			if (!(@internal is RocketDataDownLink rocketDataDownLink))
			{
				continue;
			}
			foreach (IReceiveDataNetworkDevices connectedDataNetReceiver in rocketDataDownLink.ConnectedDataNetReceivers)
			{
				foreach (Device device in connectedDataNetReceiver.DataCableNetwork.DeviceList)
				{
					if (device is Computer { CurrentMotherboard: RocketMotherboard currentMotherboard })
					{
						currentMotherboard.ToggleUI();
						return;
					}
				}
			}
		}
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
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

	public int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}

	public void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}
}
