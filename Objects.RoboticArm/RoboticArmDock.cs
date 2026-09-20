using System.Collections.Generic;
using System.Text;
using System.Threading;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Effects;
using Trading;
using UnityEngine;
using Util;

namespace Objects.RoboticArm;

public class RoboticArmDock : RoboticArmRailDeviceBase, IRoboticArmBypass, IRoboticArmJunction, IRoboticArmRail, ISmallGrid, ITooltip, IReferencable, IEvaluable
{
	[Space(15f)]
	[Header("Robotic Arm Dock")]
	[SerializeField]
	protected RoboticArm _arm;

	[SerializeField]
	private float _moveSpeed = 1f;

	[SerializeField]
	private List<RailNode> _railNodes;

	[SerializeField]
	private Transform _pivot;

	[SerializeField]
	private Transform _bypassPoint;

	[SerializeField]
	private MaterialChanger _startingDockToggleMaterialChanger;

	[SerializeField]
	private Collider _infoScreenCollider;

	private IRoboticArmBypass _currentBypass;

	private Vector3 _bypassMoveTarget;

	private bool _playingMoveAudio;

	protected CancellationTokenWrapper _armCancellation = new CancellationTokenWrapper();

	private readonly Collider[] _collisionResults = new Collider[10];

	private readonly Vector3 _overlapBoxHalfExtents = new Vector3(0.16f, 0.4f, 0.61f) * 0.5f;

	private readonly Vector3 _overlapBoxOffset = new Vector3(0f, -0.273f, -0.24f);

	protected const int ARM_RESET_DELAY = 200;

	private ArmState _currentArmState;

	private const int BlockingCheck = 2;

	private bool _isFaceBlocked;

	private bool _doingBypassMove;

	private ArmState _armState;

	private bool _obstructed;

	private float _currentPosition;

	private int _targetJunctionIndex;

	private Vector3 _armWorldPosition;

	private Vector3 _armWorldRotation;

	private bool _isStartingDock;

	private Slot ArmSlot => Slots[0];

	public IRoboticArmBypass CurrentBypass
	{
		get
		{
			return _currentBypass;
		}
		set
		{
			if (NetworkManager.IsServer && NetworkServer.HasClients() && CurrentBypass != value)
			{
				base.NetworkUpdateFlags |= 32768;
			}
			_currentBypass = value;
		}
	}

	protected override bool IsOperable
	{
		get
		{
			if (base.RoboticArmNetwork != null && Powered && OnOff && CheckError())
			{
				return !IsBroken;
			}
			return false;
		}
	}

	public Connection LeftEnd => OpenEnds[0];

	private bool SkipCollisionCheck { get; set; }

	private bool BypassPointValid { get; set; }

	protected bool IsMoving
	{
		get
		{
			if (IsOperable && ArmState == ArmState.Up && CurrentBypass == null)
			{
				return !RocketMath.Approximately(CurrentPosition, TargetIndex);
			}
			return false;
		}
	}

	public bool Flipped { get; set; }

	private bool FlipArrowDirection => base.RoboticArmNetwork.StartingDockFlipped != Flipped;

	private int TargetIndex => base.RoboticArmNetwork.GetJunctionFromOffsetIndex(TargetJunctionIndex).RailNodes[0].Index;

	private int CurrentJunctionIndex
	{
		get
		{
			if (!IsMoving)
			{
				int num = (int)CurrentPosition;
				RailNode railNode = null;
				if (num >= 0 && base.RoboticArmNetwork != null && base.RoboticArmNetwork.RailNodeList.Count > num)
				{
					railNode = base.RoboticArmNetwork.RailNodeList[num];
				}
				if (railNode != null && railNode.Rail is IRoboticArmJunction roboticArmJunction)
				{
					return base.RoboticArmNetwork.GetOffsetJunctionIndex(roboticArmJunction.JunctionIndex);
				}
			}
			return -1;
		}
	}

	public bool IsFaceBlocked
	{
		get
		{
			return _isFaceBlocked;
		}
		private set
		{
			if (value != IsFaceBlocked && NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 16384;
			}
			_isFaceBlocked = value;
		}
	}

	public bool DoingBypassMove
	{
		get
		{
			return _doingBypassMove;
		}
		private set
		{
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 4096;
			}
			_doingBypassMove = value;
		}
	}

	public ArmState ArmState
	{
		get
		{
			return _armState;
		}
		private set
		{
			_armState = value;
			MatchArmStateClient();
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 512;
			}
		}
	}

	public bool Obstructed
	{
		get
		{
			return _obstructed;
		}
		private set
		{
			_obstructed = value;
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 16384;
			}
		}
	}

	public float CurrentPosition
	{
		get
		{
			return _currentPosition;
		}
		private set
		{
			_currentPosition = value;
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 1024;
			}
		}
	}

	public int TargetJunctionIndex
	{
		get
		{
			return _targetJunctionIndex;
		}
		set
		{
			if (GameManager.RunSimulation)
			{
				_targetJunctionIndex = ((base.RoboticArmNetwork != null && base.RoboticArmNetwork.JunctionList.Count != 0) ? (base.RoboticArmNetwork.Looping ? RocketMath.Wrap(value, base.RoboticArmNetwork.MinJunctionIndex, base.RoboticArmNetwork.MaxJunctionIndex) : Mathf.Clamp(value, base.RoboticArmNetwork.MinJunctionIndex, base.RoboticArmNetwork.MaxJunctionIndex)) : 0);
			}
			else
			{
				_targetJunctionIndex = value;
			}
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 2048;
			}
		}
	}

	protected Vector3 ArmWorldPosition
	{
		get
		{
			return _armWorldPosition;
		}
		set
		{
			_armWorldPosition = value;
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 4096;
			}
		}
	}

	protected Vector3 ArmWorldRotation
	{
		get
		{
			return _armWorldRotation;
		}
		set
		{
			_armWorldRotation = value;
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 8192;
			}
		}
	}

	public bool IsStartingDock
	{
		get
		{
			return _isStartingDock;
		}
		private set
		{
			_isStartingDock = value;
			SetStartingDockToggleMaterial();
			if (_isStartingDock)
			{
				base.RoboticArmNetwork.ChangeStartingDockIndex(this);
			}
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 32768;
			}
		}
	}

	public List<RailNode> RailNodes => _railNodes;

	public int JunctionIndex { get; set; }

	public Vector3 Pivot { get; private set; }

	public Vector3 BypassPosition { get; private set; }

	public SmallGrid AsSmallGrid => this;

	public bool CanOpen
	{
		get
		{
			if (base.RoboticArmNetwork.ArmIsStationaryAtIndex(RailNodes[0].Index, out var dock) && dock != this)
			{
				return false;
			}
			if (Powered && OnOff)
			{
				return !IsOpen;
			}
			return false;
		}
	}

	public bool CanClose
	{
		get
		{
			if (Powered && OnOff)
			{
				return IsOpen;
			}
			return false;
		}
	}

	protected WorldGrid ArmWorldGrid => new WorldGrid(ArmWorldPosition);

	private void UpdateMoveAudio()
	{
		bool flag = DoingBypassMove && IsOperable;
		if (!_playingMoveAudio && (IsMoving || flag))
		{
			_playingMoveAudio = true;
			PlaySound(Defines.Sounds.RobotArmMoving);
		}
		else if (_playingMoveAudio && !IsMoving && !flag)
		{
			_playingMoveAudio = false;
			PlaySound(Defines.Sounds.RobotArmStop);
			StopSound(Defines.Sounds.RobotArmMoving);
		}
	}

	private bool CheckError()
	{
		if (!GameManager.RunSimulation)
		{
			return Error == 0;
		}
		bool flag = !Obstructed;
		if (Error == 0 && !flag)
		{
			OnServer.Interact(base.InteractError, 1);
		}
		else if (Error == 1 && flag)
		{
			OnServer.Interact(base.InteractError, 0);
		}
		return flag;
	}

	public bool ArmIsStationary(out int railNodeIndex)
	{
		if (CurrentBypass != null && !IsOpen)
		{
			railNodeIndex = CurrentBypass.RailNodes[0].Index;
			return true;
		}
		if (RocketMath.Approximately(CurrentPosition, TargetIndex))
		{
			railNodeIndex = TargetIndex;
			return true;
		}
		railNodeIndex = 0;
		return false;
	}

	public bool IsIdle()
	{
		if (!IsMoving && !DoingBypassMove)
		{
			return ArmState == ArmState.Up;
		}
		return false;
	}

	public override void UpdateAudio(float deltaTime)
	{
		base.UpdateAudio(deltaTime);
		if (IsBroken)
		{
			StopSound(Defines.Sounds.RobotArmMoving);
		}
		else if (base.RoboticArmNetwork != null)
		{
			UpdateMoveAudio();
		}
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		if (base.RoboticArmNetwork != null)
		{
			if (GameManager.RunSimulation)
			{
				DoUpdateServer();
			}
			else
			{
				DoUpdateClient();
			}
		}
	}

	private void DoUpdateServer()
	{
		CheckCollisions();
		if (IsOperable && ArmState == ArmState.Up)
		{
			if (CurrentBypass != null || DoingBypassMove)
			{
				MoveToBypass();
			}
			else
			{
				MoveOnRail();
			}
		}
	}

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		if (!IsBroken)
		{
			SetTargetSmallGrid();
			CheckFaceBlocked();
		}
	}

	protected virtual void SetTargetSmallGrid()
	{
	}

	protected virtual void MoveOnRail()
	{
		int num = MoveDirection();
		int nextIndex = GetNextIndex(num);
		int previousIndex = GetPreviousIndex(num);
		if (nextIndex != previousIndex)
		{
			RailNode railNode = base.RoboticArmNetwork.RailNodeList[previousIndex];
			RailNode railNode2 = base.RoboticArmNetwork.RailNodeList[nextIndex];
			if (railNode.Equals(railNode2))
			{
				CurrentPosition = nextIndex;
				return;
			}
			CurrentPosition = Mathf.MoveTowards(CurrentPosition, nextIndex, Time.deltaTime * _moveSpeed);
			float num2 = 1f - num switch
			{
				-1 => CurrentPosition - (float)nextIndex, 
				1 => (float)nextIndex - CurrentPosition, 
				_ => 0f, 
			};
			Quaternion rotation = Quaternion.Slerp(railNode.ArmRotation, railNode2.ArmRotation, num2);
			Vector3 vector = (ArmWorldPosition = LerpArmPosition(railNode, railNode2, num2));
			ArmWorldRotation = rotation.eulerAngles;
			_arm.Transform.position = vector;
			_arm.Transform.rotation = rotation;
		}
	}

	private void MoveToBypass()
	{
		_arm.Transform.position = Vector3.MoveTowards(_arm.Transform.position, _bypassMoveTarget, Time.deltaTime * 0.5f);
		ArmWorldPosition = _arm.Transform.position;
		if (RocketMath.Approximately(_arm.Transform.position, _bypassMoveTarget))
		{
			OnServer.Interact(base.InteractActivate, 0);
			DoingBypassMove = false;
		}
	}

	private Vector3 LerpArmPosition(RailNode previous, RailNode next, float lerpFactor)
	{
		if (previous.Rail == next.Rail)
		{
			return ArcLerp(previous.ArmPosition, next.ArmPosition, previous.Rail.Pivot, lerpFactor);
		}
		return Vector3.Lerp(previous.ArmPosition, next.ArmPosition, lerpFactor);
	}

	private Vector3 ArcLerp(Vector3 start, Vector3 end, Vector3 midPoint, float t)
	{
		Vector3 a = Vector3.Lerp(start, midPoint, t);
		Vector3 b = Vector3.Lerp(midPoint, end, t);
		return Vector3.Lerp(a, b, t);
	}

	private void DoUpdateClient()
	{
		float t = Time.deltaTime * 6f;
		_arm.Transform.position = Vector3.Lerp(_arm.Transform.position, ArmWorldPosition, t);
		_arm.Transform.rotation = Quaternion.Slerp(_arm.Transform.rotation, Quaternion.Euler(ArmWorldRotation), t);
	}

	private void CheckCollisions()
	{
		if (SkipCollisionCheck)
		{
			Obstructed = false;
			return;
		}
		int num = (((float)TargetIndex > CurrentPosition) ? 1 : (((float)TargetIndex < CurrentPosition) ? (-1) : 0));
		Vector3 vector = _arm.Transform.rotation * _overlapBoxOffset + _arm.Transform.forward * ((float)num * 0.1f);
		int num2 = Physics.OverlapBoxNonAlloc(_arm.Transform.position + vector, _overlapBoxHalfExtents, _collisionResults, _arm.Transform.rotation);
		Obstructed = false;
		for (int i = 0; i < num2; i++)
		{
			Collider collider = _collisionResults[i];
			if (Thing.TryFind(collider, out var thing) && !(thing == this) && (!collider.isTrigger || thing is RoboticArmDock))
			{
				Obstructed = true;
				break;
			}
		}
	}

	private int GetNextIndex(int moveDirection)
	{
		bool looping = base.RoboticArmNetwork.Looping;
		switch (moveDirection)
		{
		case 1:
		{
			int num2 = Mathf.FloorToInt(CurrentPosition + 1f);
			if (!looping)
			{
				return Mathf.Min(num2, base.RoboticArmNetwork.RailNodeList.Count - 1);
			}
			return RocketMath.Wrap(num2, 0, base.RoboticArmNetwork.RailNodeList.Count - 1);
		}
		case -1:
		{
			int num = Mathf.CeilToInt(CurrentPosition - 1f);
			if (!looping)
			{
				return Mathf.Max(num, 0);
			}
			return RocketMath.Wrap(num, 0, base.RoboticArmNetwork.RailNodeList.Count - 1);
		}
		default:
			return TargetIndex;
		}
	}

	private int GetPreviousIndex(int moveDirection)
	{
		bool looping = base.RoboticArmNetwork.Looping;
		switch (moveDirection)
		{
		case 1:
			return Mathf.FloorToInt(CurrentPosition);
		case -1:
		{
			int num = Mathf.CeilToInt(CurrentPosition);
			if (!looping)
			{
				return Mathf.Min(num, base.RoboticArmNetwork.RailNodeList.Count - 1);
			}
			return RocketMath.Wrap(num, 0, base.RoboticArmNetwork.RailNodeList.Count - 1);
		}
		default:
			return TargetIndex;
		}
	}

	private void GetNeighbourJunctions(out IRoboticArmJunction left, out IRoboticArmJunction right)
	{
		left = null;
		right = null;
		if (!base.RoboticArmNetwork.Looping || base.RoboticArmNetwork.JunctionList.Count < 2)
		{
			return;
		}
		int num = base.RoboticArmNetwork.RailNodeList.Count - 1;
		int num2 = Mathf.FloorToInt(CurrentPosition);
		for (int i = 0; i < num; i++)
		{
			int index = RocketMath.Wrap(num2 - i, 0, num - 1);
			if (base.RoboticArmNetwork.RailNodeList[index].Rail is IRoboticArmJunction roboticArmJunction)
			{
				left = roboticArmJunction;
				int index2 = RocketMath.Wrap(roboticArmJunction.JunctionIndex + 1, 0, base.RoboticArmNetwork.JunctionList.Count - 1);
				right = base.RoboticArmNetwork.JunctionList[index2];
				break;
			}
		}
	}

	private int GetDistanceBetweenJunctions(IRoboticArmJunction a, IRoboticArmJunction b)
	{
		int junctionIndex = a.JunctionIndex;
		int junctionIndex2 = b.JunctionIndex;
		int num = 0;
		for (int i = 0; i < base.RoboticArmNetwork.JunctionList.Count && RocketMath.Wrap(junctionIndex + i, 0, base.RoboticArmNetwork.JunctionList.Count - 1) != junctionIndex2; i++)
		{
			num++;
		}
		return num;
	}

	private int GetDirectionLooping()
	{
		GetNeighbourJunctions(out var left, out var right);
		IRoboticArmJunction junctionFromOffsetIndex = base.RoboticArmNetwork.GetJunctionFromOffsetIndex(TargetJunctionIndex);
		if (left == junctionFromOffsetIndex)
		{
			return -1;
		}
		if (right == junctionFromOffsetIndex)
		{
			return 1;
		}
		int distanceBetweenJunctions = GetDistanceBetweenJunctions(junctionFromOffsetIndex, left);
		int distanceBetweenJunctions2 = GetDistanceBetweenJunctions(right, junctionFromOffsetIndex);
		if (distanceBetweenJunctions > distanceBetweenJunctions2)
		{
			return 1;
		}
		if (distanceBetweenJunctions < distanceBetweenJunctions2)
		{
			return -1;
		}
		return 0;
	}

	private int MoveDirection()
	{
		float num = (float)TargetIndex - CurrentPosition;
		if (base.RoboticArmNetwork.Looping)
		{
			if (RocketMath.Approximately(num, 0f))
			{
				return 0;
			}
			int directionLooping = GetDirectionLooping();
			if (directionLooping != 0)
			{
				return directionLooping;
			}
			float num2;
			float num3;
			if (num > 0f)
			{
				num2 = num;
				num3 = (float)base.RoboticArmNetwork.RailNodeList.Count - num2;
			}
			else
			{
				if (!(num < 0f))
				{
					return 0;
				}
				num3 = Mathf.Abs(num);
				num2 = (float)base.RoboticArmNetwork.RailNodeList.Count - num3;
			}
			if (!(num3 > num2))
			{
				if (!(num3 < num2))
				{
					return 0;
				}
				return -1;
			}
			return 1;
		}
		if (!(num > 0f))
		{
			if (!(num < 0f))
			{
				return 0;
			}
			return -1;
		}
		return 1;
	}

	protected virtual void AnimateDownStarted()
	{
		PlaySound(Defines.Sounds.RobotArmAnimateDown);
	}

	protected virtual void AnimateDownFinished()
	{
		if (GameManager.RunSimulation)
		{
			RoboticArmActionHelper.DoContextualAction(ArmSlot, _arm.Transform, this);
		}
		OnServer.Interact(base.InteractActivate, 1);
	}

	protected virtual void AnimateUpStarted()
	{
		PlaySound(Defines.Sounds.RobotArmAnimateUp);
	}

	protected virtual void AnimateUpFinished()
	{
	}

	private void StartArmDownAnimation()
	{
		_armCancellation.CancelAndInitialize();
		AnimateArmDown(_armCancellation.Token).Forget();
	}

	private void StartArmUpAnimation()
	{
		_armCancellation.CancelAndInitialize();
		AnimateArmUp(_armCancellation.Token).Forget();
	}

	private async UniTaskVoid AnimateArmDown(CancellationToken cancellationToken)
	{
		if (GameManager.RunSimulation)
		{
			ArmState = ArmState.AnimatingDown;
		}
		AnimateDownStarted();
		await _arm.AnimateDown(cancellationToken);
		if (GameManager.RunSimulation)
		{
			ArmState = ArmState.Down;
			OnServer.Interact(base.InteractActivate, 0);
		}
		AnimateDownFinished();
	}

	private async UniTaskVoid AnimateArmUp(CancellationToken cancellationToken)
	{
		if (GameManager.RunSimulation)
		{
			ArmState = ArmState.AnimatingUp;
		}
		AnimateUpStarted();
		await _arm.AnimateUp(cancellationToken);
		if (GameManager.RunSimulation)
		{
			ArmState = ArmState.Up;
			OnServer.Interact(base.InteractActivate, 0);
		}
		AnimateUpFinished();
	}

	public override void Awake()
	{
		base.Awake();
		ArmWorldPosition = _arm.Transform.position;
		ArmWorldRotation = _arm.Transform.eulerAngles;
		BypassPointValid = _bypassPoint != null;
		if (ArmState == ArmState.Undefined)
		{
			ArmState = ArmState.Up;
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		CheckMidAnimationLoad();
		int previousIndex = GetPreviousIndex(0);
		SetArmPositionFromIndex(previousIndex);
		SetStartingDockToggleMaterial();
		SetBypassStateOnLoad();
	}

	private void CheckMidAnimationLoad()
	{
		ArmState armState = ArmState;
		if (armState == ArmState.Undefined || armState == ArmState.AnimatingUp)
		{
			ArmState = ArmState.Up;
		}
		else if (ArmState == ArmState.AnimatingDown)
		{
			ArmState = ArmState.Down;
		}
		_arm.Animate((ArmState != ArmState.Up) ? 1 : 0);
		if (base.InteractActivate.State == 1)
		{
			OnServer.Interact(base.InteractActivate, 0);
		}
	}

	private void SetArmPositionFromIndex(int index)
	{
		if (base.RoboticArmNetwork != null && index < base.RoboticArmNetwork.RailNodeList.Count)
		{
			RailNode railNode = base.RoboticArmNetwork.RailNodeList[index];
			ArmWorldPosition = railNode.ArmPosition;
			ArmWorldRotation = railNode.ArmRotation.eulerAngles;
			_arm.Transform.position = railNode.ArmPosition;
			_arm.Transform.rotation = railNode.ArmRotation;
		}
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		foreach (RailNode railNode in _railNodes)
		{
			railNode.Init();
		}
		_arm.SetActive(active: true);
		BypassPosition = _bypassPoint.position;
		Pivot = _pivot.position;
	}

	public override void OnDestroy()
	{
		_armCancellation.Cancel();
		base.OnDestroy();
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		string value = GameStrings.RoboticArmInvalid.AsColor("red");
		ArmState armState = ArmState;
		bool flag = armState == ArmState.Down || armState == ArmState.AnimatingDown;
		bool flag2 = IsIdle();
		if (IsMoving)
		{
			value = GameStrings.RoboticArmMoving.AsColor("yellow");
		}
		else if (!IsOpen)
		{
			value = GameStrings.RoboticArmDocked.AsColor("yellow");
		}
		else if (IsFaceBlocked)
		{
			value = GameStrings.RoboticArmFaceBlocked.AsColor("yellow");
		}
		else if (flag)
		{
			value = GameStrings.RoboticArmExtended.AsColor("yellow");
		}
		else if (flag2)
		{
			value = GameStrings.RoboticArmIdle.AsColor("yellow");
		}
		extendedText.Append(value).AppendLine();
		extendedText.Append(GameStrings.RoboticArmIndex.AsString(StringManager.Get(TargetJunctionIndex).AsColor("yellow"))).AppendLine();
		extendedText.Append(GameStrings.RoboticArmPosition.AsString(StringManager.Get(CurrentJunctionIndex).AsColor("yellow"))).AppendLine();
		return extendedText;
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip passiveTooltip = base.GetPassiveTooltip(hitCollider);
		if (!string.IsNullOrEmpty(passiveTooltip.Title))
		{
			return passiveTooltip;
		}
		if (hitCollider != null && hitCollider == _infoScreenCollider)
		{
			passiveTooltip.Title = DisplayName;
			passiveTooltip.Extended = GetExtendedText().ToString();
		}
		else
		{
			int value = base.RoboticArmNetwork?.GetOffsetJunctionIndex(JunctionIndex) ?? 0;
			passiveTooltip.Title = GameStrings.RoboticArmJunction.AsString(StringManager.Get(value));
		}
		return passiveTooltip;
	}

	private void SetStartingDockToggleMaterial()
	{
		_startingDockToggleMaterialChanger.ChangeState(IsStartingDock ? Defines.Animator.On : Defines.Animator.Off);
	}

	private bool CanBypass(IRoboticArmBypass bypass)
	{
		if (!(bypass is RoboticArmDock roboticArmDock))
		{
			return true;
		}
		return roboticArmDock == this;
	}

	protected SmallCell GetArmInteractionCell()
	{
		Transform transform = _arm.Transform;
		return RoboticArmActionHelper.GetSmallCellBelow((transform.position - transform.up * 0.25f).ToGrid(SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset).ToVector3(), -transform.up, 3);
	}

	private void MatchArmStateClient()
	{
		if (!GameManager.RunSimulation && _currentArmState != ArmState)
		{
			if (ArmState == ArmState.AnimatingDown)
			{
				StartArmDownAnimation();
			}
			else if (ArmState == ArmState.AnimatingUp)
			{
				StartArmUpAnimation();
			}
			else if (ArmState == ArmState.Up)
			{
				_arm.Animate(0f);
				AnimateUpFinished();
			}
			else if (ArmState == ArmState.Down)
			{
				_arm.Animate(1f);
				AnimateDownFinished();
			}
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (GameManager.GameState == GameState.Running && interactable.Action == InteractableType.Activate)
		{
			InteractActivateUpdated(interactable);
		}
	}

	private void InteractActivateUpdated(Interactable interactable)
	{
		if (!GameManager.RunSimulation || interactable.State != 1 || HandleByPass())
		{
			return;
		}
		if (IsFaceBlocked)
		{
			OnServer.Interact(base.InteractActivate, 0);
			return;
		}
		switch (ArmState)
		{
		case ArmState.Up:
			StartArmDownAnimation();
			break;
		case ArmState.Down:
			StartArmUpAnimation();
			break;
		}
	}

	protected void CheckFaceBlocked()
	{
		Vector3 vector = (_arm.Transform.position - _arm.Transform.up * 0.25f).ToGrid(SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset).ToVector3() + -_arm.Transform.up.normalized * 1f;
		WorldGrid worldGrid = new WorldGrid(vector);
		if (worldGrid != ArmWorldGrid)
		{
			Structure structure = GridController.World.Get<Structure>(worldGrid);
			if (structure != null && !structure.CanAirPass)
			{
				IsFaceBlocked = true;
				_armCancellation.Cancel();
				return;
			}
			foreach (Structure faceStructure in GridController.World.GetFaceStructures((worldGrid.Value + ArmWorldGrid.Value) / 2))
			{
				if (!faceStructure.CanAirPass)
				{
					IsFaceBlocked = true;
					_armCancellation.Cancel();
					return;
				}
			}
		}
		IsFaceBlocked = false;
	}

	private void SetBypassStateOnLoad()
	{
		if (!IsOpen)
		{
			IRoboticArmRail rail = base.RoboticArmNetwork.RailNodeList[(int)CurrentPosition].Rail;
			bool flag = rail is RoboticArmDock roboticArmDock && !roboticArmDock.BypassPointValid;
			if (rail is IRoboticArmBypass roboticArmBypass && !flag)
			{
				CurrentBypass = roboticArmBypass;
				ArmWorldPosition = roboticArmBypass.BypassPosition;
				_arm.Transform.position = roboticArmBypass.BypassPosition;
				_bypassMoveTarget = roboticArmBypass.BypassPosition;
			}
		}
	}

	private bool HandleByPass()
	{
		if (!ArmIsStationary(out var railNodeIndex) && IsOpen)
		{
			return false;
		}
		if (!HasOpenState)
		{
			return false;
		}
		IRoboticArmRail rail = base.RoboticArmNetwork.RailNodeList[railNodeIndex].Rail;
		bool flag = rail is RoboticArmDock roboticArmDock && !roboticArmDock.BypassPointValid;
		if (rail is IRoboticArmBypass roboticArmBypass && !flag)
		{
			if (!GameManager.RunSimulation)
			{
				return true;
			}
			if (IsOpen && roboticArmBypass.CanClose && CanBypass(roboticArmBypass))
			{
				HandleBypassAction(roboticArmBypass, 0);
			}
			else if (!IsOpen && roboticArmBypass.CanOpen)
			{
				HandleBypassAction(null, 1);
			}
			else
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
			return true;
		}
		return false;
	}

	public void HandleBypassAction(IRoboticArmBypass bypass, int openState)
	{
		switch (openState)
		{
		case 0:
			DoingBypassMove = true;
			CurrentBypass = bypass;
			_bypassMoveTarget = CurrentBypass.BypassPosition;
			CurrentBypass.SetOpen(openState);
			SetOpen(openState);
			break;
		case 1:
			DoingBypassMove = true;
			_bypassMoveTarget = CurrentBypass.RailNodes[0].ArmPosition;
			CurrentBypass.SetOpen(openState);
			CurrentBypass = null;
			SetOpen(openState);
			break;
		}
	}

	public bool TrySetOpenState(int openState)
	{
		if (!GameManager.RunSimulation)
		{
			return true;
		}
		if (openState == 1 == IsOpen)
		{
			return false;
		}
		if (!ArmIsStationary(out var railNodeIndex))
		{
			return false;
		}
		if (base.RoboticArmNetwork == null || base.RoboticArmNetwork.RailNodeList.Count == 0 || railNodeIndex >= base.RoboticArmNetwork.RailNodeList.Count)
		{
			return false;
		}
		IRoboticArmRail obj = base.RoboticArmNetwork?.RailNodeList[railNodeIndex].Rail;
		bool flag = obj is RoboticArmDock roboticArmDock && !roboticArmDock.BypassPointValid;
		IRoboticArmBypass roboticArmBypass = obj as IRoboticArmBypass;
		if (roboticArmBypass == null || flag)
		{
			return false;
		}
		if (IsOpen && roboticArmBypass.CanClose && CanBypass(roboticArmBypass))
		{
			HandleBypassAction(roboticArmBypass, 0);
			return true;
		}
		if (!IsOpen && roboticArmBypass.CanOpen)
		{
			HandleBypassAction(null, 1);
			return true;
		}
		return false;
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		return interactable.Action switch
		{
			InteractableType.OnOff => HandleOnOff(interactable, interaction, doAction), 
			InteractableType.Activate => HandleActivate(interactable, doAction), 
			InteractableType.Button1 => HandleRightArrow(interactable, doAction), 
			InteractableType.Button2 => HandleLeftArrow(interactable, doAction), 
			InteractableType.Button3 => HandleSetStart(interactable, doAction), 
			_ => base.InteractWith(interactable, interaction, doAction), 
		};
	}

	private DelayedActionInstance HandleOnOff(Interactable interactable, Interaction interaction, bool doAction)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (OnOff && Powered && Error == 1)
		{
			delayedActionInstance.AppendStateMessage(GameStrings.ThingCurrentlyFlashingError);
		}
		if (OnOff && Powered && Obstructed)
		{
			delayedActionInstance.AppendStateMessage(GameStrings.RoboticArmObstructed);
		}
		if (doAction)
		{
			OnServer.Interact(interactable, (!OnOff) ? 1 : 0);
		}
		return delayedActionInstance.Succeed();
	}

	protected virtual DelayedActionInstance HandleActivate(Interactable interactable, bool doAction)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (!OnOff)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
		}
		if (!Powered)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
		}
		if (Error != 0)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceError);
		}
		if (IsMoving || Activate == 1)
		{
			return delayedActionInstance.Fail(GameStrings.RoboticArmBusy);
		}
		if (IsFaceBlocked)
		{
			return delayedActionInstance.Fail(GameStrings.RoboticArmFaceBlockedInfo);
		}
		if (doAction)
		{
			OnServer.Interact(base.InteractActivate, 1);
		}
		return delayedActionInstance.Succeed();
	}

	private DelayedActionInstance HandleRightArrow(Interactable interactable, bool doAction)
	{
		return ChangeTarget(interactable, doAction, (!FlipArrowDirection) ? 1 : (-1));
	}

	private DelayedActionInstance HandleLeftArrow(Interactable interactable, bool doAction)
	{
		return ChangeTarget(interactable, doAction, FlipArrowDirection ? 1 : (-1));
	}

	private DelayedActionInstance HandleSetStart(Interactable interactable, bool doAction)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (!OnOff)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
		}
		if (!Powered)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
		}
		if (doAction && GameManager.RunSimulation)
		{
			IsStartingDock = true;
			foreach (RoboticArmDock dock in base.RoboticArmNetwork.DockList)
			{
				if (!(dock == this))
				{
					dock.IsStartingDock = false;
				}
			}
		}
		delayedActionInstance.ActionMessage = GameStrings.RoboticArmSetStartingDock.DisplayString;
		return delayedActionInstance.Succeed();
	}

	private DelayedActionInstance ChangeTarget(Interactable interactable, bool doAction, int increment)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (!OnOff)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
		}
		if (!Powered)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
		}
		if (doAction && GameManager.RunSimulation)
		{
			TargetJunctionIndex += increment;
		}
		delayedActionInstance.ActionMessage = GameStrings.RoboticArmTargetIndex.AsString(StringManager.Get(TargetJunctionIndex));
		return delayedActionInstance.Succeed();
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Setting => true, 
			LogicType.Idle => true, 
			LogicType.Extended => true, 
			LogicType.PositionX => true, 
			_ => base.CanLogicRead(logicType), 
		};
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		switch (logicType)
		{
		case LogicType.Setting:
			TargetJunctionIndex = (int)value;
			break;
		case LogicType.Activate:
			if (IsOperable && !IsMoving)
			{
				OnServer.Interact(base.InteractActivate, (int)value);
			}
			break;
		case LogicType.Open:
		{
			int openState = (int)Mathf.Clamp((float)value, 0f, 1f);
			TrySetOpenState(openState);
			break;
		}
		default:
			base.SetLogicValue(logicType, value);
			break;
		}
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Setting => TargetJunctionIndex, 
			LogicType.PositionX => CurrentJunctionIndex, 
			LogicType.Idle => IsIdle() ? 1 : 0, 
			LogicType.Extended => (ArmState == ArmState.Down) ? 1 : 0, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new RoboticArmDockSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData thingSaveData)
	{
		base.InitialiseSaveData(ref thingSaveData);
		if (thingSaveData is RoboticArmDockSaveData roboticArmDockSaveData)
		{
			roboticArmDockSaveData.TargetJunctionIndex = TargetJunctionIndex;
			roboticArmDockSaveData.CurrentIndex = CurrentPosition;
			roboticArmDockSaveData.IsStartingDock = IsStartingDock;
			roboticArmDockSaveData.ArmState = ArmState;
		}
	}

	public override void DeserializeSave(ThingSaveData thingSaveData)
	{
		base.DeserializeSave(thingSaveData);
		if (thingSaveData is RoboticArmDockSaveData roboticArmDockSaveData)
		{
			_targetJunctionIndex = roboticArmDockSaveData.TargetJunctionIndex;
			_currentPosition = roboticArmDockSaveData.CurrentIndex;
			_isStartingDock = roboticArmDockSaveData.IsStartingDock;
			_armState = roboticArmDockSaveData.ArmState;
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt32(TargetJunctionIndex);
		writer.WriteBoolean(IsStartingDock);
		writer.WriteByte((byte)ArmState);
		writer.WriteBoolean(Obstructed);
		writer.WriteBoolean(IsFaceBlocked);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		TargetJunctionIndex = reader.ReadInt32();
		IsStartingDock = reader.ReadBoolean();
		ArmState = (ArmState)reader.ReadByte();
		Obstructed = reader.ReadBoolean();
		IsFaceBlocked = reader.ReadBoolean();
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteByte((byte)ArmState);
		}
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			writer.WriteInt16((short)TargetJunctionIndex);
		}
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			writer.WriteSingle(CurrentPosition);
		}
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			writer.WriteVector3(ArmWorldPosition);
			writer.WriteBoolean(DoingBypassMove);
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			writer.WriteVector3(ArmWorldRotation);
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			writer.WriteBoolean(Obstructed);
			writer.WriteBoolean(IsFaceBlocked);
		}
		if (Thing.IsNetworkUpdateRequired(32768u, networkUpdateType))
		{
			writer.WriteBoolean(IsStartingDock);
			Network.WritePackedId(writer, CurrentBypass);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			ArmState = (ArmState)reader.ReadByte();
		}
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			TargetJunctionIndex = reader.ReadInt16();
		}
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			CurrentPosition = reader.ReadSingle();
		}
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			ArmWorldPosition = reader.ReadVector3();
			DoingBypassMove = reader.ReadBoolean();
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			ArmWorldRotation = reader.ReadVector3();
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			Obstructed = reader.ReadBoolean();
			IsFaceBlocked = reader.ReadBoolean();
		}
		if (Thing.IsNetworkUpdateRequired(32768u, networkUpdateType))
		{
			IsStartingDock = reader.ReadBoolean();
			Network.ReadPackedId(reader, out var referenceId);
			CurrentBypass = Thing.Find<IRoboticArmBypass>(referenceId);
		}
	}

	public Connection OtherEnd(Connection end)
	{
		if (end != OpenEnds[0])
		{
			return OpenEnds[0];
		}
		return OpenEnds[1];
	}

	public void SetOpen(int state)
	{
		OnServer.Interact(base.InteractOpen, state);
	}

	public virtual void RailNetworkUpdated()
	{
		if (GameManager.GameState == GameState.Running && base.RoboticArmNetwork != null)
		{
			int index = RailNodes[0].Index;
			CurrentPosition = index;
			SetArmPositionFromIndex(index);
			TargetJunctionIndex = base.RoboticArmNetwork.GetOffsetJunctionIndex(JunctionIndex);
			CurrentBypass = null;
			OnServer.Interact(base.InteractOpen, 1);
			OnServer.Interact(base.InteractAccess, 0);
			_arm.Animate(0f);
			ArmState = ArmState.Up;
			SetStartingDockToggleMaterial();
		}
	}
}
