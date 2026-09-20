using System;
using System.Threading;
using System.Threading.Tasks;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Sound;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Networks;
using TraderUI;
using Trading;
using Trading.Waypoints;
using UI;
using UnityEngine;
using Util;
using Weather;

namespace Objects.Electrical;

public class LandingPadCenter : LandingPadModular, ITraderDestination, IReferencable, IEvaluable, IWaypoint, ILogicable
{
	[Header("Landing Pad Center")]
	[SerializeField]
	private Transform _traderPosition;

	[SerializeField]
	private Transform _shuttlePosition;

	private Vector3 _centerPosition;

	[ReadOnly]
	private TraderPilot _trader;

	[ReadOnly]
	private TraderShuttle _shuttle;

	private static readonly string[] LayerNames = new string[2] { "Default", "Terrain" };

	public static readonly string[] LandingPadModeStrings = Enum.GetNames(typeof(ContactStatus));

	public Vector3 lastPadOffsetValue;

	private int _phase;

	private Task _flashingTask;

	private CancellationTokenSource _flashingCancel;

	private bool _isTraderReady;

	private TraderContact _currentTradingContact;

	private CommsMotherboard _parentMotherboard;

	private float _virtualWaypointHeight;

	private Vector3 _syncedShuttleTargetPosition;

	private Vector3 _syncedShuttleTargetRotation;

	private ShuttleAnimationState _shuttleAnimationState;

	private bool _locked;

	private long _nextWaypointReferenceId;

	private long _parentMotherboardId;

	private long _currentContactId;

	private CancellationTokenWrapper _openDoorsToken = new CancellationTokenWrapper();

	public Vector3 ShuttlePosition => _centerPosition;

	public TraderShuttle Shuttle => _shuttle;

	public GameObject Trader
	{
		get
		{
			if (!_trader)
			{
				return null;
			}
			return _trader.gameObject;
		}
	}

	public override VolumeLitres Volume => new VolumeLitres(20.0);

	public override string[] ModeStrings => LandingPadModeStrings;

	public bool IsTraderReady
	{
		get
		{
			return _isTraderReady;
		}
		set
		{
			_isTraderReady = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 32768;
			}
			if (_isTraderReady && !_trader)
			{
				_trader = Singleton<TraderPilotPool>.Instance.ShowPilot(CurrentTradingContact, _traderPosition, lastPadOffsetValue);
			}
			if (!_isTraderReady && (bool)_trader)
			{
				UnityEngine.Object.Destroy(_trader.gameObject);
				_trader.IsBeingDestroyed = true;
				_trader = null;
			}
		}
	}

	public TraderContact CurrentTradingContact
	{
		get
		{
			return _currentTradingContact;
		}
		set
		{
			if (_currentTradingContact?.ReferenceId != value?.ReferenceId)
			{
				Achievements.AssessClearedToLand(value);
				Achievements.AssessYouStillOpen(value);
				_currentTradingContact = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 2048;
				}
			}
		}
	}

	public CommsMotherboard ParentMotherboard
	{
		get
		{
			return _parentMotherboard;
		}
		set
		{
			if (_parentMotherboard?.ReferenceId != value?.ReferenceId)
			{
				_parentMotherboard = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 1024;
				}
			}
		}
	}

	public float VirtualWaypointHeight
	{
		get
		{
			return _virtualWaypointHeight;
		}
		set
		{
			_virtualWaypointHeight = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 8192;
			}
		}
	}

	public Vector3 SyncedShuttleTargetPosition
	{
		get
		{
			return _syncedShuttleTargetPosition;
		}
		set
		{
			_syncedShuttleTargetPosition = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 16384;
			}
		}
	}

	public Vector3 SyncedShuttleTargetRotation
	{
		get
		{
			return _syncedShuttleTargetRotation;
		}
		set
		{
			_syncedShuttleTargetRotation = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 16384;
			}
		}
	}

	public ShuttleAnimationState ShuttleAnimationState
	{
		get
		{
			return _shuttleAnimationState;
		}
		set
		{
			_shuttleAnimationState = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 16384;
			}
		}
	}

	public bool Locked
	{
		get
		{
			return _locked;
		}
		set
		{
			if (_locked != value)
			{
				_locked = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 512;
				}
			}
		}
	}

	public override bool Stressed
	{
		get
		{
			return _stressed;
		}
		set
		{
			if (value != _stressed)
			{
				if (value)
				{
					AtmosphericAudioHandler.Instance.AddStressedPipe(this);
				}
				else
				{
					AtmosphericAudioHandler.Instance.RemoveStressedPipe(this);
				}
				_stressed = value;
			}
		}
	}

	private ContactStatus ContactStatus => (ContactStatus)Mode;

	public Vector3 WaypointPosition => Transform.position + lastPadOffsetValue - Vector3.up * 0.754f;

	public Vector3 WaypointForward => -Transform.forward;

	public string WaypointName => DisplayName;

	public WaypointType WaypointType => WaypointType.PadCenter;

	public IWaypoint NextWaypoint { get; set; }

	public IWaypoint PreviousWaypoint { get; set; }

	public new int TotalSlots { get; }

	public override void OnAssignedReference()
	{
		base.OnAssignedReference();
		_centerPosition = _shuttlePosition.position;
	}

	public bool CanTraderLand(TraderContact trader, out string errorMessage)
	{
		errorMessage = string.Empty;
		if (!OnOff)
		{
			errorMessage = GameStrings.ContactTradePadUnpowered.DisplayString;
			return false;
		}
		if (Error != 0 || base.LandingPadNetwork?.LandingPadCenter != this)
		{
			errorMessage = GameStrings.ContactTradeLandingPadError.DisplayString;
			return false;
		}
		if (WeatherManager.IsWeatherEventRunning && !trader.RequiresThreshold)
		{
			errorMessage = Localization.GetInterface("ContactLandingRequestFailStorm");
			return false;
		}
		if (!CheckPadSize(trader))
		{
			errorMessage = GameStrings.ContactTradeLandingPadToSmall.DisplayString;
			return false;
		}
		if (!ThresholdChecks(trader))
		{
			errorMessage = GameStrings.ContactTradeLandingNoThreshold.DisplayString;
			return false;
		}
		return true;
	}

	private bool ThresholdChecks(TraderContact contact)
	{
		if (!contact.RequiresThreshold)
		{
			return true;
		}
		bool result = false;
		if (!base.LandingPadNetwork.ValidApproachState(out var validThreshold))
		{
			return false;
		}
		if (validThreshold != null)
		{
			result = true;
		}
		return result;
	}

	public bool CheckPadSize(TraderContact contact)
	{
		return CheckPadSize(contact.RequiredPadSize());
	}

	public bool CheckPadSize(Vector2 padSize)
	{
		if (RocketMath.Approximately(Forward, Grid3.East.ToVector3().normalized, 0.001f) || RocketMath.Approximately(Forward, Grid3.West.ToVector3().normalized, 0.001f))
		{
			padSize = new Vector2(padSize.y, padSize.x);
		}
		if (CheckPadRectangle(padSize, new Vector2(0f, 0f), CollisionCheck: false))
		{
			lastPadOffsetValue = Vector3.zero;
			return true;
		}
		Vector2 vector = IsPadBigEnoughWithVirtualCenter(padSize);
		if (vector != Vector2.zero)
		{
			vector.ToString();
			lastPadOffsetValue = new Vector3(vector.y, 0f, vector.x);
			Vector3 axis = new Vector3(0f, 1f, 0f);
			Vector3 vector2 = Quaternion.AngleAxis(90f, axis) * lastPadOffsetValue;
			lastPadOffsetValue = vector2;
			return true;
		}
		return false;
	}

	private Vector2 IsPadBigEnoughWithVirtualCenter(Vector2 PadSize)
	{
		for (int i = -1; i <= 1; i++)
		{
			if (PadSize.x % 2f != 0f && i != 0)
			{
				continue;
			}
			for (int j = -1; j <= 1; j++)
			{
				if ((PadSize.y % 2f == 0f || j == 0) && CheckPadRectangle(PadSize, new Vector2(i, j), CollisionCheck: false))
				{
					return new Vector2(i, j);
				}
			}
		}
		return Vector2.zero;
	}

	public bool IsObstructed(TraderContact trader)
	{
		Grid3 grid = new WorldGrid(_shuttlePosition.position).Value + Grid3.Up;
		int num = (int)(trader.RequiredPadSize().x / 2f);
		int num2 = (int)(trader.RequiredPadSize().y / 2f);
		for (int i = -num; i < num; i++)
		{
			for (int j = -num2; j < num2; j++)
			{
				if (IsPathBlocking(grid + Grid3.East / 2 * i * 2 + Grid3.North / 2 * j * 2, Grid3.Up / 2))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool CheckPadRectangle(Vector2 padSize, Vector2 offset, bool CollisionCheck)
	{
		int num = -(int)padSize.x / 2;
		int num2 = (int)padSize.x / 2;
		int num3 = -(int)padSize.y / 2;
		int num4 = (int)padSize.y / 2;
		if (offset.x == 1f)
		{
			num++;
		}
		if (offset.x == -1f)
		{
			num2--;
		}
		if (offset.y == 1f)
		{
			num4--;
		}
		if (offset.y == -1f)
		{
			num3++;
		}
		Vector3 vector = Transform.position;
		bool flag = false;
		for (int i = num; i <= num2; i++)
		{
			if (flag)
			{
				break;
			}
			for (int j = num3; j <= num4; j++)
			{
				Vector3 worldPosition = new Vector3(vector.x + (float)(i * 2), vector.y, vector.z + (float)(j * 2));
				Grid3 value = new WorldGrid(worldPosition).Value;
				Grid3 localGrid = base.GridController.WorldToLocalGrid(worldPosition);
				SmallCell smallCell = base.GridController.GetSmallCell(localGrid);
				if (smallCell?.Other == null || (!(smallCell.Other is LandingPadTile) && smallCell.Other != this) || (CollisionCheck && IsPathBlocking(value + Grid3.East / 2 + Grid3.North / 2, Grid3.Up / 2)))
				{
					flag = true;
					break;
				}
			}
		}
		if (flag)
		{
			return false;
		}
		return true;
	}

	public void RefreshPadPower()
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		int state = 0;
		if (base.LandingPadNetwork != null && base.IsStructureCompleted)
		{
			foreach (INetworkedStructure structure in base.LandingPadNetwork.StructureList)
			{
				if (structure is LandingPadDataPowerConnection { Powered: not false, IsStructureCompleted: not false })
				{
					state = 1;
				}
			}
		}
		OnServer.Interact(base.InteractOnOff, state);
		if (base.LandingPadNetwork == null)
		{
			return;
		}
		foreach (INetworkedStructure structure2 in base.LandingPadNetwork.StructureList)
		{
			if (!(structure2 is LandingPadCenter))
			{
				structure2.OnStructureNetworkUpdated();
			}
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.OnOff)
		{
			CheckError();
			if (OnOff && (_flashingTask == null || _flashingTask.IsCompleted))
			{
				_flashingCancel = new CancellationTokenSource();
				_flashingTask = Flash(_flashingCancel.Token).AsTask();
			}
			else if (!OnOff)
			{
				Task flashingTask = _flashingTask;
				if (flashingTask != null && !flashingTask.IsCompleted)
				{
					_flashingCancel.Cancel();
				}
				_flashingTask = null;
			}
		}
		if (interactable.Action == InteractableType.Error && Error == 0 && OnOff && (_flashingTask == null || _flashingTask.IsCompleted))
		{
			_flashingCancel = new CancellationTokenSource();
			_flashingTask = Flash(_flashingCancel.Token).AsTask();
		}
		if (GameManager.RunSimulation && interactable.Action == InteractableType.Activate && Activate == 1 && Shuttle != null && !Shuttle.AnimationHandler.WaitingForAnimation && Mode == 4)
		{
			ServerCallTrader(isLanding: false, CurrentTradingContact);
		}
	}

	private async UniTask Flash(CancellationToken cancelToken)
	{
		_phase = 4;
		while (OnOff && Error == 0)
		{
			await UniTask.Delay(400, ignoreTimeScale: false, PlayerLoopTiming.Update, cancelToken);
			if (cancelToken.IsCancellationRequested)
			{
				break;
			}
			if (base.LandingPadNetwork == null)
			{
				continue;
			}
			_phase--;
			if (_phase < 0)
			{
				_phase = 3;
			}
			foreach (INetworkedStructure structure in base.LandingPadNetwork.StructureList)
			{
				if (cancelToken.IsCancellationRequested)
				{
					_flashingTask = null;
					return;
				}
				if (structure is INetworkedLandingPad networkedLandingPad)
				{
					if (!networkedLandingPad.AnimateLights)
					{
						networkedLandingPad.FlashLights(flash: true);
					}
					else
					{
						networkedLandingPad.FlashLights(networkedLandingPad.PhaseBucket != -1 && _phase == networkedLandingPad.PhaseBucket % 4);
					}
				}
			}
		}
		_flashingTask = null;
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		if (IsCursor)
		{
			return;
		}
		if (MaterialChanger != null)
		{
			if (Error == 1 && OnOff)
			{
				if (ErrorAnimTask == null || ErrorAnimTask.IsCompleted)
				{
					ErrorAnimTask = ErrorAnim().AsTask();
				}
			}
			else if (OnOff)
			{
				MaterialChanger.ChangeState(Powered ? Defines.Animator.OnPowered : Defines.Animator.On);
			}
			else
			{
				MaterialChanger.ChangeState(Defines.Animator.Off);
			}
		}
		if (base.SwitchOnOff != null)
		{
			base.SwitchOnOff.RefreshState(skipAnimation);
		}
	}

	protected override async UniTask ErrorAnim()
	{
		while (Error == 1 && OnOff)
		{
			MaterialChanger.ChangeState(Defines.Animator.Error0);
			await UniTask.Delay(250);
			if (Error != 1 || !OnOff)
			{
				break;
			}
			MaterialChanger.ChangeState(Defines.Animator.Error1);
			await UniTask.Delay(250);
		}
		ErrorAnimTask = null;
	}

	public override void OnStructureNetworkUpdated()
	{
		CheckError();
		RefreshPadPower();
	}

	public void CheckError()
	{
		if (base.LandingPadNetwork?.LandingPadCenter == null || base.LandingPadNetwork.LandingPadCenter != this || !base.LandingPadNetwork.IsCompleted() || !base.LandingPadNetwork.ValidApproachState(out var _))
		{
			OnServer.Interact(base.InteractError, 1);
		}
		else
		{
			OnServer.Interact(base.InteractError, 0);
		}
	}

	private void CreateOrDestroyShuttleForClients()
	{
		if (!base.IsBeingDestroyed)
		{
			if (ShuttleAnimationState == ShuttleAnimationState.None)
			{
				UnAssignShuttle();
			}
			else if (!IsBroken && _shuttle == null && CurrentTradingContact != null)
			{
				AssignShuttle();
				MoveShuttleIntoPosition(SyncedShuttleTargetPosition, SyncedShuttleTargetRotation);
			}
		}
	}

	private void AssignShuttle()
	{
		_shuttle = Singleton<TraderShuttlePool>.Instance.RequestShuttle(CurrentTradingContact.ShuttleType);
		_traderPosition.localPosition = _shuttle.pilotPosition.position + Vector3.up * 0.25f;
		_shuttle.LandingPadCenter = this;
	}

	private void UnAssignShuttle()
	{
		if (!(_shuttle == null))
		{
			_shuttle.DestroyShuttle();
			_shuttle.LandingPadCenter = null;
			_shuttle = null;
		}
	}

	public void ServerCallTrader(bool isLanding, TraderContact contact)
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		if (isLanding & (CurrentTradingContact == null))
		{
			if (!contact.CurrentlyTrading)
			{
				CurrentTradingContact = contact;
				contact.ConnectedPad = this;
				contact.CurrentlyTrading = true;
				ShuttleApproachAsync().Forget();
			}
		}
		else
		{
			contact.ConnectedPad = null;
			contact.CurrentlyTrading = false;
			ShuttleDepartAsync().Forget();
		}
	}

	private bool IsPathBlocking(Grid3 start, Grid3 step, int iterations = 100)
	{
		for (int i = 0; i < iterations; i++)
		{
			WorldGrid worldGrid = new WorldGrid(start + step * i);
			if (base.GridController.Get<Structure>(worldGrid) != null)
			{
				return true;
			}
			if (!base.GridController.FaceLookup.TryGetValue(worldGrid, out var value))
			{
				continue;
			}
			foreach (Structure item in value)
			{
				if (!item.IsDoor || !item.CanAirPass)
				{
					return true;
				}
			}
		}
		return false;
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if (attack.SourceItem is Labeller labeller)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = ActionStrings.Rename
			};
			if (!labeller.OnOff)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
			}
			if (!labeller.IsOperable)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
			}
			if (!doAction)
			{
				return delayedActionInstance;
			}
			labeller.Rename(this);
			return delayedActionInstance;
		}
		if (attack.SourceItem is AuthoringTool || (attack.SourceItem != null && (attack.SourceItem.PrefabHash == base.CurrentBuildState.Tool.ToolExit.PrefabHash || (attack.SourceItem as Item)?.ReplacementOf?.PrefabHash == base.CurrentBuildState.Tool.ToolExit.PrefabHash)))
		{
			return base.AttackWith(attack, doAction);
		}
		if (!IsTraderReady || attack.TargetCollider == null || attack.TargetCollider.transform != Trader.transform || (bool)(attack.SourceItem as SprayCan))
		{
			return base.AttackWith(attack, doAction);
		}
		DelayedActionInstance delayedActionInstance2 = new DelayedActionInstance(0.5f)
		{
			ActionMessage = GameStrings.TraderTrade.DisplayString,
			Selection = GetSelection(attack.TargetCollider)
		};
		if (KeyManager.GetButtonDown(KeyMap.PrimaryAction))
		{
			CurrentTradingContact.HumanTradingSteamID = InventoryManager.ParentHuman.OrganBrain.ClientId;
			CommsTerminal.CurrentContact = CurrentTradingContact;
			if (Error == 1)
			{
				if (!Singleton<ConfirmationPanel>.Instance.IsVisible)
				{
					Singleton<ConfirmationPanel>.Instance.Show("TradeErrorNoMotherboardInsertedTitle", "TradeErrorPadError", "ButtonOk");
				}
				return delayedActionInstance2.Fail();
			}
			if (!OnOff)
			{
				if (!Singleton<ConfirmationPanel>.Instance.IsVisible)
				{
					Singleton<ConfirmationPanel>.Instance.Show("TradeErrorNoMotherboardInsertedTitle", "TradeErrorPadOff", "ButtonOk");
				}
				return delayedActionInstance2.Fail();
			}
			TradeDataHelper tradeDataHelper = new TradeDataHelper(CurrentTradingContact);
			TradeData tradeData = tradeDataHelper.Convert();
			if (tradeData != null)
			{
				TraderCanvas.Instance.Show(tradeData, tradeDataHelper);
			}
			else
			{
				ConsoleWindow.PrintError("Unable to fetch trader data from current contact - forcing trader to depart");
				tradeDataHelper.Depart();
			}
			return delayedActionInstance2.Succeed();
		}
		return delayedActionInstance2;
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		AtmosphericsManager.Instance.Register(this);
		WaypointManager.Register(this);
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		AtmosphericsManager.Instance.Deregister(this);
		WaypointManager.Deregister(this);
	}

	public override void OnDestroy()
	{
		if ((bool)Shuttle)
		{
			Shuttle.DepartInvalid(ShuttleAnimationState).Forget();
		}
		IsTraderReady = false;
		base.OnDestroy();
	}

	public override void OnStructureBroken()
	{
		base.OnStructureBroken();
		if (GameManager.GameState == GameState.Running && !GameManager.IsBatchMode)
		{
			Singleton<AudioManager>.Instance.PlayAudioClipsData(Defines.Sounds.PipeFailHash, base.Position + Vector3.up);
		}
		ShuttleDepartAsync().Forget();
	}

	public override void UpdateStateVisualizer(bool visualOnly = false)
	{
		base.UpdateStateVisualizer(visualOnly);
		if (!GameManager.RunSimulation && IsBroken && GameManager.GameState == GameState.Running)
		{
			Singleton<AudioManager>.Instance.PlayAudioClipsData(Defines.Sounds.PipeFailHash, base.Position + Vector3.up);
		}
	}

	private void CheckContactEnvironmentRequirements()
	{
		if (ContactStatus != ContactStatus.Landed || CurrentTradingContact == null)
		{
			return;
		}
		bool flag = CurrentTradingContact.EnvironmentRequirementMet(this);
		if (IsTraderReady && !flag)
		{
			if (_openDoorsToken.Initialized)
			{
				_openDoorsToken.Cancel();
			}
			_openDoorsToken.Initialize();
			CloseDoors(_openDoorsToken.Token).Forget();
		}
		else if (!IsTraderReady && flag)
		{
			if (_openDoorsToken.Initialized)
			{
				_openDoorsToken.Cancel();
			}
			_openDoorsToken.Initialize();
			OpenDoors(_openDoorsToken.Token).Forget();
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		CheckContactEnvironmentRequirements();
		if (!base.HasOpenGrid)
		{
			return;
		}
		Atmosphere atmosphere = base.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid);
		PressurekPa pressurekPa = PressurekPa.Zero;
		if (base.LandingPadNetwork?.Atmosphere == null)
		{
			return;
		}
		PressurekPa pressureGassesAndLiquids = base.LandingPadNetwork.Atmosphere.PressureGassesAndLiquids;
		if (atmosphere != null)
		{
			pressurekPa = atmosphere.PressureGassesAndLiquids;
		}
		RocketMath.Abs(pressurekPa - pressureGassesAndLiquids);
		if (IsBroken)
		{
			if (pressureGassesAndLiquids < new PressurekPa(0.0010000000474974513) && atmosphere == null)
			{
				base.LandingPadNetwork.Atmosphere.GasMixture.Reset();
				return;
			}
			Atmosphere outputAtmos = base.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
			AtmosphereHelper.Mix(base.LandingPadNetwork.Atmosphere, outputAtmos, AtmosphereHelper.MatterState.All);
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteBoolean(Locked);
		}
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			writer.WriteInt64(ParentMotherboard?.ReferenceId ?? 0);
		}
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			writer.WriteInt64(CurrentTradingContact?.ReferenceId ?? 0);
		}
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			writer.WriteInt64((NextWaypoint as Thing)?.ReferenceId ?? 0);
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			writer.WriteSingle(VirtualWaypointHeight);
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			writer.WriteInt32((int)_shuttleAnimationState);
			writer.WriteVector3(_syncedShuttleTargetPosition);
			writer.WriteVector3(_syncedShuttleTargetRotation);
		}
		if (Thing.IsNetworkUpdateRequired(32768u, networkUpdateType))
		{
			writer.WriteBoolean(IsTraderReady);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			Locked = reader.ReadBoolean();
		}
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			ParentMotherboard = Thing.Find<CommsMotherboard>(reader.ReadInt64());
		}
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			CurrentTradingContact = Referencable.Find<TraderContact>(reader.ReadInt64());
		}
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			NextWaypoint = Thing.Find<Thing>(reader.ReadInt64()) as IWaypoint;
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			VirtualWaypointHeight = reader.ReadSingle();
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			ShuttleAnimationState = (ShuttleAnimationState)reader.ReadInt32();
			SyncedShuttleTargetPosition = reader.ReadVector3();
			SyncedShuttleTargetRotation = reader.ReadVector3();
			CreateOrDestroyShuttleForClients();
			SetShuttleStateForClients();
		}
		if (Thing.IsNetworkUpdateRequired(32768u, networkUpdateType))
		{
			IsTraderReady = reader.ReadBoolean();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteBoolean(Locked);
		writer.WriteInt64(ParentMotherboard?.ReferenceId ?? 0);
		writer.WriteInt64(CurrentTradingContact?.ReferenceId ?? 0);
		writer.WriteInt64((NextWaypoint as Thing)?.ReferenceId ?? 0);
		writer.WriteSingle(VirtualWaypointHeight);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Locked = reader.ReadBoolean();
		_parentMotherboardId = reader.ReadInt64();
		_currentContactId = reader.ReadInt64();
		_nextWaypointReferenceId = reader.ReadInt64();
		_virtualWaypointHeight = reader.ReadSingle();
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new LandingPadCenterSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is LandingPadCenterSaveData landingPadCenterSaveData)
		{
			landingPadCenterSaveData.TraderReferenceID = CurrentTradingContact?.ReferenceId ?? 0;
			landingPadCenterSaveData.ParentMotherboardID = ParentMotherboard?.ReferenceId ?? 0;
			landingPadCenterSaveData.NextWaypointReferenceId = (NextWaypoint as Thing)?.ReferenceId ?? 0;
			landingPadCenterSaveData.VirtualWaypointHeight = VirtualWaypointHeight;
		}
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is LandingPadCenterSaveData landingPadCenterSaveData)
		{
			_currentContactId = landingPadCenterSaveData.TraderReferenceID;
			_parentMotherboardId = landingPadCenterSaveData.ParentMotherboardID;
			_nextWaypointReferenceId = landingPadCenterSaveData.NextWaypointReferenceId;
			_virtualWaypointHeight = landingPadCenterSaveData.VirtualWaypointHeight;
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		ParentMotherboard = Thing.Find<CommsMotherboard>(_parentMotherboardId);
		CurrentTradingContact = Referencable.Find<TraderContact>(_currentContactId);
		if (CurrentTradingContact != null)
		{
			CheckPadSize(CurrentTradingContact);
			CurrentTradingContact.ConnectedPad = this;
			CurrentTradingContact.CurrentlyTrading = true;
			PositionShuttleOnLoad();
		}
		RefreshPadPower();
		NextWaypoint = Thing.Find<Thing>(_nextWaypointReferenceId) as IWaypoint;
	}

	public bool GetTaxiThresholdPiece(out IWaypoint threshold)
	{
		IWaypoint waypoint = this;
		while (waypoint.PreviousWaypoint != null)
		{
			waypoint = waypoint.PreviousWaypoint;
			if (waypoint is LandingPadTaxiThreshold)
			{
				threshold = waypoint;
				return true;
			}
		}
		threshold = null;
		return false;
	}

	private IWaypoint GetFirstApproachWaypoint()
	{
		IWaypoint waypoint = this;
		while (waypoint.PreviousWaypoint != null)
		{
			waypoint = waypoint.PreviousWaypoint;
		}
		if (waypoint != this)
		{
			return waypoint;
		}
		return new VirtualWaypoint(WaypointPosition + Vector3.up * VirtualWaypointHeight, WaypointForward, WaypointType.PadCenterHeight)
		{
			NextWaypoint = this
		};
	}

	public override void OnServerTick(float deltaTime)
	{
		if (!(_shuttle == null))
		{
			ShuttleAnimationState = _shuttle.AnimationHandler.GetAnimationState();
			SyncedShuttleTargetPosition = _shuttle.TargetPosition;
			SyncedShuttleTargetRotation = _shuttle.TargetRotation.eulerAngles;
		}
	}

	private void SetShuttleStateForClients()
	{
		if (!GameManager.RunSimulation && !(_shuttle == null))
		{
			_shuttle.SetState(SyncedShuttleTargetPosition, SyncedShuttleTargetRotation, ShuttleAnimationState);
		}
	}

	private void MoveShuttleIntoPosition(Vector3 position, Vector3 rotation)
	{
		_shuttle.AnimationHandler.SetShuttlePositionAndRotation(position, rotation);
	}

	private async UniTaskVoid ShuttleApproachAsync()
	{
		SetMode(ContactStatus.NoContact);
		if ((bool)_shuttle)
		{
			return;
		}
		AssignShuttle();
		IWaypoint firstApproachWaypoint = GetFirstApproachWaypoint();
		VirtualWaypoint virtualWaypoint = new VirtualWaypoint(firstApproachWaypoint.WaypointPosition + firstApproachWaypoint.WaypointForward.normalized * (-1f * _currentTradingContact.DeorbitDistance.x) + new Vector3(0f, _currentTradingContact.DeorbitDistance.y, 0f), firstApproachWaypoint.WaypointForward, WaypointType.BeaconDeorbit);
		virtualWaypoint.NextWaypoint = firstApproachWaypoint;
		IWaypoint toWaypoint = virtualWaypoint;
		Vector3 rotation = (firstApproachWaypoint as Thing)?.ThingTransform.eulerAngles ?? Quaternion.LookRotation(firstApproachWaypoint.WaypointForward).eulerAngles;
		SetMode(ContactStatus.Moving);
		MoveShuttleIntoPosition(toWaypoint.WaypointPosition, rotation);
		await _shuttle.AnimationHandler.EnterAtmosphere(toWaypoint);
		_ = toWaypoint;
		IWaypoint waypoint = toWaypoint;
		toWaypoint = toWaypoint.NextWaypoint;
		while (toWaypoint != null)
		{
			if (!_shuttle)
			{
				ClearContact();
				return;
			}
			if (!_currentTradingContact.IsPlane)
			{
				WaypointType waypointType = toWaypoint.WaypointType;
				if (waypointType == WaypointType.RunwayStart || waypointType == WaypointType.RunwayStop)
				{
					waypoint = toWaypoint;
					toWaypoint = toWaypoint.NextWaypoint;
					continue;
				}
			}
			VirtualWaypoint virtualWaypoint2 = toWaypoint as VirtualWaypoint;
			if (toWaypoint.WaypointType == WaypointType.RunwayStop)
			{
				virtualWaypoint2?.SetPosition(virtualWaypoint2.OriginalPosition + toWaypoint.WaypointForward * (_currentTradingContact.RequiredRunwayLength * 2));
			}
			if (_currentTradingContact.IsPlane && toWaypoint.WaypointType == WaypointType.RunwayStop)
			{
				IWaypoint waypoint2 = toWaypoint;
				while (waypoint2.NextWaypoint != null)
				{
					if (virtualWaypoint2 != null && RocketMath.Approximately(waypoint2.WaypointPosition, virtualWaypoint2.WaypointPosition, 1f) && virtualWaypoint2 != waypoint2)
					{
						toWaypoint = waypoint2.NextWaypoint;
						waypoint = waypoint2.PreviousWaypoint;
						break;
					}
					waypoint2 = waypoint2.NextWaypoint;
				}
			}
			if (toWaypoint.WaypointType == WaypointType.PadCenter && waypoint.WaypointType == WaypointType.Beacon && !_currentTradingContact.IsPlane)
			{
				LandingPadCenter landingPadCenter = toWaypoint as LandingPadCenter;
				VirtualWaypoint virtualWaypoint3 = new VirtualWaypoint(toWaypoint.WaypointPosition + Vector3.up * landingPadCenter.VirtualWaypointHeight, toWaypoint.WaypointForward, WaypointType.PadCenterHeight);
				virtualWaypoint3.NextWaypoint = toWaypoint;
				toWaypoint = virtualWaypoint3;
			}
			if (!toWaypoint.IsLinkedToCenter())
			{
				await _shuttle.DepartInvalid(ShuttleAnimationState);
				ClearContact();
				return;
			}
			await _shuttle.AnimationHandler.MoveTo(toWaypoint, waypoint);
			if (!_shuttle)
			{
				ClearContact();
				return;
			}
			if (toWaypoint.WaypointType == WaypointType.TaxiHold)
			{
				await WaitAtHoldPiece(toWaypoint);
				if (!_shuttle)
				{
					ClearContact();
					return;
				}
			}
			waypoint = toWaypoint;
			toWaypoint = toWaypoint.NextWaypoint;
		}
		if (!_shuttle)
		{
			ClearContact();
			return;
		}
		if (waypoint != this)
		{
			await _shuttle.DepartInvalid(ShuttleAnimationState);
			_shuttle = null;
			CurrentTradingContact = null;
		}
		if (!Transform)
		{
			return;
		}
		await _shuttle.AnimationHandler.RotateTowards(WaypointForward);
		if (!_shuttle)
		{
			ClearContact();
			return;
		}
		await _shuttle.AnimationHandler.TouchDown();
		if (!_shuttle)
		{
			ClearContact();
		}
		else
		{
			SetMode(ContactStatus.Landed);
		}
	}

	private void ClearContact()
	{
		ShuttleAnimationState = ShuttleAnimationState.None;
		CurrentTradingContact = null;
		IsTraderReady = false;
		SetMode(ContactStatus.NoContact);
	}

	private async UniTaskVoid ShuttleDepartAsync()
	{
		SetMode(ContactStatus.Moving);
		await UniTask.SwitchToMainThread();
		IsTraderReady = false;
		if (!_shuttle)
		{
			ClearContact();
			return;
		}
		IWaypoint fromWaypoint = null;
		IWaypoint toWaypoint = this;
		ShuttleType shuttleType = _shuttle.ShuttleType;
		bool isPlane = shuttleType == ShuttleType.LargePlane || shuttleType == ShuttleType.MediumPlane;
		await _shuttle.AnimationHandler.LiftUp();
		if (toWaypoint.PreviousWaypoint != null)
		{
			while (toWaypoint != null)
			{
				if (toWaypoint == this && toWaypoint.PreviousWaypoint.WaypointType == WaypointType.Beacon)
				{
					VirtualWaypoint virtualWaypoint = new VirtualWaypoint(WaypointPosition + Vector3.up * VirtualWaypointHeight, WaypointForward, WaypointType.PadCenterHeight);
					virtualWaypoint.PreviousWaypoint = toWaypoint.PreviousWaypoint;
					toWaypoint = virtualWaypoint;
				}
				WaypointType waypointType = toWaypoint.WaypointType;
				if (waypointType == WaypointType.RunwayStart || waypointType == WaypointType.RunwayStop)
				{
					toWaypoint = toWaypoint.PreviousWaypoint;
					continue;
				}
				if (!_shuttle)
				{
					break;
				}
				await _shuttle.AnimationHandler.MoveTo(toWaypoint, toWaypoint.NextWaypoint, isPlane ? 0.5f : 1f);
				if (!_shuttle)
				{
					break;
				}
				if (toWaypoint.WaypointType == WaypointType.TaxiHold)
				{
					await WaitAtHoldPiece(toWaypoint);
				}
				fromWaypoint = toWaypoint;
				toWaypoint = toWaypoint.PreviousWaypoint;
			}
		}
		else
		{
			if (!_shuttle)
			{
				ClearContact();
				return;
			}
			VirtualWaypoint toWaypoint2 = new VirtualWaypoint(WaypointPosition + Vector3.up * VirtualWaypointHeight, WaypointForward, WaypointType.Taxi);
			await _shuttle.AnimationHandler.MoveTo(toWaypoint2, this, isPlane ? 0.5f : 1f);
			if (!_shuttle)
			{
				ClearContact();
				return;
			}
			await _shuttle.AnimationHandler.RotateTowards(-WaypointForward);
		}
		if ((bool)_shuttle)
		{
			if (fromWaypoint != null)
			{
				await _shuttle.AnimationHandler.RotateTowards(-fromWaypoint.WaypointForward);
			}
			await _shuttle.AnimationHandler.LeaveAtmosphere();
		}
		UnAssignShuttle();
		ClearContact();
	}

	public void ShuttleDepartImmediate()
	{
		IsTraderReady = false;
		UnAssignShuttle();
		ClearContact();
	}

	private async UniTask WaitAtHoldPiece(IWaypoint holdPiece)
	{
		SetMode(ContactStatus.Holding);
		while (Activate == 0 && Shuttle != null && !holdPiece.BeingDestroyed)
		{
			await UniTask.WaitForEndOfFrame();
		}
		if ((bool)Shuttle)
		{
			if (holdPiece.BeingDestroyed)
			{
				await Shuttle.DepartInvalid(ShuttleAnimationState);
				_shuttle = null;
				CurrentTradingContact = null;
			}
			else
			{
				SetMode(ContactStatus.Moving);
			}
		}
	}

	private async UniTask CloseDoors(CancellationToken cancellationToken)
	{
		await UniTask.SwitchToMainThread(cancellationToken);
		if (!_shuttle.AnimationHandler.WaitingForAnimation && GameManager.GameState == GameState.Running && Mode == 4)
		{
			IsTraderReady = false;
			await _shuttle.AnimationHandler.CloseDoors();
		}
	}

	private async UniTask OpenDoors(CancellationToken cancellationToken)
	{
		await UniTask.SwitchToMainThread(cancellationToken);
		if (!_shuttle.AnimationHandler.WaitingForAnimation && GameManager.GameState == GameState.Running && Mode == 4)
		{
			await _shuttle.AnimationHandler.OpenDoors();
			IsTraderReady = true;
		}
	}

	private void PositionShuttleOnLoad()
	{
		if (!_shuttle)
		{
			AssignShuttle();
			Vector3 eulerAngles = Quaternion.LookRotation(WaypointForward).eulerAngles;
			MoveShuttleIntoPosition(WaypointPosition, eulerAngles);
			_shuttle.AnimationHandler.LandImmediate(this);
			base.InteractMode.Interact(4);
		}
	}

	private void SetMode(ContactStatus status)
	{
		OnServer.Interact(base.InteractMode, (int)status);
	}

	public bool IsLinkedToCenter()
	{
		return true;
	}

	public void OnDeregister()
	{
		if (NextWaypoint != null)
		{
			NextWaypoint.PreviousWaypoint = null;
		}
		if (PreviousWaypoint != null)
		{
			PreviousWaypoint.NextWaypoint = null;
			if (PreviousWaypoint is Waypoint waypoint)
			{
				waypoint.TargetWaypoint = null;
			}
		}
	}

	public new Slot GetSlot(int slotIndex)
	{
		return null;
	}

	public int GetNextSlotId(int slotIndex, bool isForward)
	{
		return 0;
	}

	public bool IsLogicSlotReadable()
	{
		return false;
	}

	public bool CanLogicRead(LogicSlotType logicSlotType, int slotId)
	{
		return false;
	}

	public double GetLogicValue(LogicSlotType logicSlotType, int slotId)
	{
		return 0.0;
	}

	public bool IsLogicReadable()
	{
		return true;
	}

	public bool IsLogicWritable()
	{
		return true;
	}

	public bool CanLogicRead(LogicType logicType)
	{
		return false;
	}

	public bool CanLogicWrite(LogicType logicType)
	{
		return false;
	}

	public void SetLogicValue(LogicType logicType, double value)
	{
		switch (logicType)
		{
		case LogicType.Activate:
			if (OnOff)
			{
				int state2 = (int)Mathf.Clamp((float)value, 0f, 1f);
				OnServer.Interact(base.InteractActivate, state2);
			}
			break;
		case LogicType.Mode:
		{
			int state = (int)value.Clamp(0.0, ModeStrings.Length);
			OnServer.Interact(base.InteractMode, state);
			break;
		}
		case LogicType.Vertical:
			VirtualWaypointHeight = Mathf.Clamp((float)value, 0f, 50f);
			break;
		}
	}

	public double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Activate => Activate, 
			LogicType.Mode => Mode, 
			LogicType.Vertical => VirtualWaypointHeight, 
			_ => 0f, 
		};
	}
}
