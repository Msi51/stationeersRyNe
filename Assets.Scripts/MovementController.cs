using System;
using System.Collections.Generic;
using Assets.Scripts.Emotes;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Clothing;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Structures;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using CharacterCustomisation;
using Cysharp.Threading.Tasks;
using Objects.Electrical;
using Objects.Rockets;
using UnityEngine;
using UnityEngine.Serialization;
using Weather;

namespace Assets.Scripts;

public class MovementController : GameBase, IPhysical, IProfile, IDensePoolable
{
	public enum Mode
	{
		Animation,
		Jetpack,
		Grab,
		JetpackGravity,
		Seated,
		LyingDown,
		Ladder,
		CryoTube
	}

	public static int ActiveHandHash = Animator.StringToHash("Active_Hand");

	public static int HasItemHash = Animator.StringToHash("Has_Item");

	public static int VHash = Animator.StringToHash("V");

	public static int HHash = Animator.StringToHash("H");

	public static int VelocityHash = Animator.StringToHash("Velocity");

	public static int IdleHash = Animator.StringToHash("Idle");

	public static int JetpackHash = Animator.StringToHash("JetPack");

	public static int FlyUpHash = Animator.StringToHash("FlyUp");

	public static int FlyDownHash = Animator.StringToHash("FlyDown");

	public static int GroundedHash = Animator.StringToHash("Grounded");

	public static int JumpHash = Animator.StringToHash("Jump");

	public static int CastingHash = Animator.StringToHash("Casting");

	public static int CastingAnimationHash = Animator.StringToHash("CastingAnimation");

	public static int ControlModeHash = Animator.StringToHash("ControlMode");

	public static int VerticalClimbHash = Animator.StringToHash("VerticalClimb");

	public static int HandGripHash = Animator.StringToHash("HandGrip");

	public static int ThrowingHash = Animator.StringToHash("Throwing");

	public static int LadderHash = Animator.StringToHash("Ladder");

	public static int GetUp = Animator.StringToHash("GetUp");

	[FormerlySerializedAs("DynamicThing")]
	[SerializeField]
	private DynamicThing dynamicThing;

	[FormerlySerializedAs("CameraPosition")]
	[SerializeField]
	private Transform cameraPosition;

	[FormerlySerializedAs("ControllingBody")]
	[ReadOnly]
	public Rigidbody controllingBody;

	[FormerlySerializedAs("ControllingAnimator")]
	[ReadOnly]
	public Animator controllingAnimator;

	[FormerlySerializedAs("ParentEntity")]
	[ReadOnly]
	public Human parentEntity;

	[Header("Movement Forces")]
	[FormerlySerializedAs("MovementForce")]
	public float movementForce = 0.2f;

	[FormerlySerializedAs("JumpForce")]
	public float jumpForce = 8f;

	[FormerlySerializedAs("CharacterMovementStrength")]
	public float characterMovementStrength = 100f;

	[FormerlySerializedAs("CharacterMaxSpeed")]
	public float characterMaxSpeed = 4f;

	[FormerlySerializedAs("GravityInRoom")]
	public float gravityInRoom = -0.0098f;

	private float _ladderCooldown;

	private Mode _controlMode;

	[Header("Misc Settings")]
	[Tooltip("Mask other players are on")]
	[FormerlySerializedAs("PlayerMask")]
	public LayerMask playerMask;

	[Tooltip("Pivot offset based off mouse input")]
	private Vector3 _targetVelocity;

	private Vector3 _grabPosition;

	private Vector3 _grabVector;

	private float _controlModeCd;

	[SerializeField]
	[Tooltip("Above this height the jetpack will apply no upward movement")]
	private float defaultJetpackMaxHeight = 10f;

	private bool _jetpackUsed;

	private static float _minimumJetpackMovement = 0.001f;

	private const float MaxJetpackVelocity = 50f;

	private bool _didApplyJumpForce;

	private bool _jumping;

	public static float TimeUntilClamp = 1f;

	[NonSerialized]
	public float UnclampTime;

	private Vector3 _lastLocalPosition;

	private long _lastMothershipId;

	[Header("Step Up")]
	[Tooltip("Maximum height of a surface the character will automatically step up onto (relative to their position). Set to 0 to disable the feature")]
	public float maxStepUpHeight;

	[Tooltip("Minimum height of a surface the character will attempt to automatically step up onto (relative to their position)")]
	private float _minStepUpHeight = 0.02f;

	[Tooltip("Velocity applied when stepping up onto something. Also acts as the maximum")]
	public float stepUpVelocity = 1.5f;

	private static readonly float StepUpStepsAhead = 5f;

	private readonly List<ContactPoint> _allContacts = new List<ContactPoint>(12);

	private bool _didStepUpLastFixedUpdate;

	private const float STUN_MOVEMENT_PENALTY = 0.9f;

	public const float MOOD_SPEED_MULTIPLIER = 0.95f;

	public const float HYGIENE_SPEED_MULTIPLIER = 1.05f;

	public const float SOILED_SPEED_MULTIPLIER = 0.95f;

	private UniTask _setJumpTask;

	private static readonly float MaxWalkableSlope = 45f;

	private bool canFinishJumping;

	private Vector3 debugVelocity;

	private static readonly string _profilerTag = "MovementController";

	private readonly DensePoolReference<IPhysical> _densePoolReference = new DensePoolReference<IPhysical>(Thing.PhysicalPoolActive);

	private bool Stabilizer
	{
		get
		{
			if (parentEntity.TryGetJetpack(out var jetpack))
			{
				return jetpack.OnOff;
			}
			return false;
		}
	}

	public Mode ControlMode
	{
		get
		{
			return _controlMode;
		}
		set
		{
			if (value == Mode.Ladder)
			{
				_ladderCooldown = 1f;
			}
			_controlMode = value;
			controllingAnimator.SetInteger(ControlModeHash, (value == Mode.JetpackGravity) ? 1 : ControlMode.GetHashCode());
			Mode controlMode = _controlMode;
			if (controlMode == Mode.Seated || controlMode == Mode.LyingDown || controlMode == Mode.CryoTube)
			{
				controllingAnimator.SetBool(IdleHash, value: true);
			}
		}
	}

	public bool SideGrabLock { get; private set; }

	public float HeightEfficiency { get; private set; }

	private bool NearGround { get; set; }

	private bool IsGround { get; set; }

	private static bool IsInputAscend => KeyManager.GetAscend() > 0.01f;

	public bool IsSteppingUp { get; private set; }

	public static bool IsAffectedByStorm
	{
		get
		{
			if (WeatherManager.CurrentWeatherEvent != null)
			{
				return WeatherManager.IsWeatherEventRunning;
			}
			return false;
		}
	}

	private bool IsInZeroG
	{
		get
		{
			if (dynamicThing.Room == null)
			{
				return !WorldManager.HasGravityAtHeight(parentEntity.Transform.position.y);
			}
			return false;
		}
	}

	private bool CanStepUp
	{
		get
		{
			if (ControlMode == Mode.Animation && maxStepUpHeight > _minStepUpHeight && InventoryManager.AllowMouseControl)
			{
				if (!IsGround)
				{
					return IsSteppingUp;
				}
				return true;
			}
			return false;
		}
	}

	private bool ShouldCheckGroundSlope
	{
		get
		{
			if (ControlMode == Mode.Animation)
			{
				return IsGround;
			}
			return false;
		}
	}

	public bool RunPhysicsUpdate => true;

	public string ProfilerTag => _profilerTag;

	private void Start()
	{
		ControlMode = _controlMode;
		GameObject.layer = Layers.PlayerImmune;
	}

	public void OnEnable()
	{
		Thing.Register(this);
	}

	private void OnDisable()
	{
		Thing.Deregister(this);
	}

	private void IdleChecker()
	{
		Mode controlMode = ControlMode;
		if (controlMode == Mode.Seated || controlMode == Mode.LyingDown || controlMode == Mode.CryoTube)
		{
			return;
		}
		if ((double)KeyManager.GetForwardAxis() > 0.001 || (double)KeyManager.GetRightAxis() < -0.001 || (double)KeyManager.GetForwardAxis() < -0.001 || (double)KeyManager.GetRightAxis() > 0.001 || (double)Math.Abs(KeyManager.GetDescend()) > 0.001 || (double)Math.Abs(KeyManager.GetAscend()) > 0.001)
		{
			if (ControlMode == Mode.Jetpack)
			{
				controllingAnimator.SetBool(IdleHash, value: false);
			}
			else if ((double)KeyManager.GetForwardAxis() > 0.001 || (double)KeyManager.GetRightAxis() > 0.001 || (double)KeyManager.GetForwardAxis() < -0.001 || (double)KeyManager.GetRightAxis() < -0.001)
			{
				controllingAnimator.SetBool(IdleHash, value: false);
			}
		}
		else
		{
			controllingAnimator.SetBool(IdleHash, value: true);
		}
	}

	private void IsFalling()
	{
		if (IsGround || NearGround)
		{
			controllingAnimator.SetBool(GroundedHash, value: true);
		}
		else if (Mathf.Abs(controllingBody.velocity.y) > 1f || (ControlMode == Mode.Animation && IsInZeroG))
		{
			controllingAnimator.SetBool(GroundedHash, value: false);
		}
	}

	public void SetCameraPosition(Transform cameraTransform)
	{
		cameraPosition = cameraTransform;
	}

	private void SetMovementMode()
	{
		if ((controllingBody.isKinematic && !parentEntity.IsForceKinematic) || !InventoryManager.AllowMouseControl)
		{
			return;
		}
		if (_controlModeCd > 0f)
		{
			_controlModeCd -= Time.deltaTime;
		}
		Mode controlMode = ControlMode;
		if (controlMode == Mode.Grab || controlMode == Mode.Seated || controlMode == Mode.LyingDown || controlMode == Mode.Ladder || controlMode == Mode.CryoTube)
		{
			return;
		}
		controllingAnimator.SetBool(VerticalClimbHash, value: false);
		SideGrabLock = false;
		if (KeyManager.GetButton(KeyMap.Grab) && _controlModeCd <= 0f)
		{
			if ((bool)parentEntity && (bool)cameraPosition && Vector3.Distance(InputHelpers.GetIKAimPosition(parentEntity.AllButPlayerImmune), cameraPosition.position) < 0.8f)
			{
				_controlModeCd = 0.5f;
				_grabPosition = InputHelpers.GetIKAimPosition(parentEntity.AllButPlayerImmune);
				_grabVector = _grabPosition - parentEntity.CenterPosition;
				ControlMode = Mode.Grab;
			}
			return;
		}
		if (dynamicThing.Room != null || WorldManager.HasGravityAtHeight(parentEntity.Transform.position.y))
		{
			Jetpack jetpack;
			bool flag = parentEntity.TryGetJetpack(out jetpack);
			if (ControlMode == Mode.JetpackGravity)
			{
				return;
			}
			if (KeyManager.GetButton(KeyMap.Jetpack) && _controlModeCd <= 0f && flag)
			{
				ControlMode = Mode.JetpackGravity;
				_controlModeCd = 0.5f;
				return;
			}
			if (ControlMode == Mode.Jetpack)
			{
				jetpack?.DisableJetAll();
			}
			ControlMode = Mode.Animation;
			return;
		}
		Jetpack jetpack2;
		bool flag2 = parentEntity.TryGetJetpack(out jetpack2);
		if (ControlMode != Mode.Jetpack)
		{
			if (KeyManager.GetButton(KeyMap.Jetpack) && _controlModeCd <= 0f && flag2)
			{
				ControlMode = Mode.Jetpack;
				_controlModeCd = 0.5f;
			}
			else
			{
				ControlMode = Mode.Animation;
			}
		}
	}

	private void ResetJetpackEmissions()
	{
		if (parentEntity.TryGetJetpack(out var jetpack))
		{
			jetpack.ClearEmissions();
		}
	}

	public bool IsLadderFloating()
	{
		RaycastHit hitInfo;
		bool num = Physics.Raycast(new Ray(controllingBody.transform.position, Vector3.down), out hitInfo, 0.1f, parentEntity.AllButPlayerImmune);
		LadderPlatform ladderPlatform = Thing.Find<LadderPlatform>(hitInfo.collider);
		if (num)
		{
			return ladderPlatform != null;
		}
		return true;
	}

	private void HandleLowOrbitPlayableArea()
	{
		Vector3 position = Transform.position;
		float power;
		float maxForce;
		if (!(position.y < 1000f))
		{
			power = 5f;
			maxForce = 1000f;
			Bounds lowOrbitPlayableBounds = Rocket.LowOrbitPlayableBounds;
			if (position.x > lowOrbitPlayableBounds.max.x)
			{
				ApplyOutOfBoundsForce(position.x, lowOrbitPlayableBounds.max.x, Vector3.left);
			}
			if (position.x < lowOrbitPlayableBounds.min.x)
			{
				ApplyOutOfBoundsForce(position.x, lowOrbitPlayableBounds.min.x, Vector3.right);
			}
			if (position.y > lowOrbitPlayableBounds.max.y)
			{
				ApplyOutOfBoundsForce(position.y, lowOrbitPlayableBounds.max.y, Vector3.down);
			}
			if (position.y < lowOrbitPlayableBounds.min.y)
			{
				ApplyOutOfBoundsForce(position.y, lowOrbitPlayableBounds.min.y, Vector3.up);
			}
			if (position.z > lowOrbitPlayableBounds.max.z)
			{
				ApplyOutOfBoundsForce(position.z, lowOrbitPlayableBounds.max.z, Vector3.back);
			}
			if (position.z < lowOrbitPlayableBounds.min.z)
			{
				ApplyOutOfBoundsForce(position.z, lowOrbitPlayableBounds.min.z, Vector3.forward);
			}
		}
		void ApplyOutOfBoundsForce(float x1, float x2, Vector3 dir)
		{
			float num = Mathf.Min(Mathf.Abs(x1 - x2) * power, maxForce);
			controllingBody.AddForce(num * dir);
		}
	}

	private void MovementHandler()
	{
		controllingAnimator.SetFloat(VelocityHash, controllingBody.velocity.magnitude);
		if (InputWindowBase.IsInputWindow || CharacterCustomisationManager.IsSceneLoaded || Stationpedia.IsOpenAndLocked)
		{
			if (ControlMode == Mode.Jetpack || ControlMode == Mode.JetpackGravity)
			{
				StabilizeJetpack();
				return;
			}
			IsFalling();
			LockToGround();
			return;
		}
		SetMovementMode();
		if (ControlMode != Mode.Jetpack || ControlMode != Mode.JetpackGravity)
		{
			CameraController.Instance.JetPackAnimationOffset = Vector3.zero;
		}
		parentEntity.AddExtraGravity = ControlMode == Mode.Animation;
		CameraController.Instance.JetPackAnimationOffset = Vector3.Lerp(CameraController.Instance.JetPackAnimationOffset, Vector3.left * (0.1f * controllingAnimator.GetFloat(HHash)), Time.deltaTime * 10f);
		parentEntity.TryGetJetpack(out var jetpack);
		HandleLowOrbitPlayableArea();
		switch (ControlMode)
		{
		case Mode.Animation:
		{
			if (!RocketMath.Approximately(parentEntity.ThingTransform.rotation, Quaternion.identity, float.Epsilon))
			{
				parentEntity.ThingTransform.rotation = Quaternion.identity;
			}
			IsFalling();
			HandleJump();
			Vector3 movementDir = GetDesiredGroundVelocity();
			float stepUpHeight = GetStepUpHeight(in movementDir);
			if (stepUpHeight > float.Epsilon)
			{
				IsSteppingUp = true;
				movementDir *= Mathf.Max(1f - stepUpHeight / parentEntity.MasterCollider.radius, 0f);
			}
			else
			{
				IsSteppingUp = false;
				movementDir = GetSlopeWalkingGroundVelocity(in movementDir);
			}
			float magnitude = movementDir.magnitude;
			if (_ladderCooldown > 0f)
			{
				if (!IsLadderFloating())
				{
					_ladderCooldown = 0f;
				}
				else if (!NearGround)
				{
					controllingBody.MovePosition(Transform.position + parentEntity.CharacterRotationY.forward * 0.3f);
					_ladderCooldown = 0f;
				}
			}
			DynamicThing dynamicThing = InventoryManager.ActiveHandSlot?.Occupant;
			float num = 1f;
			if (dynamicThing != null && (bool)dynamicThing.Joint && dynamicThing.IsEntity)
			{
				num = 1.5f;
			}
			if (parentEntity.State == EntityState.Alive)
			{
				if (IsSteppingUp)
				{
					if (controllingBody.velocity.y < stepUpVelocity)
					{
						float f = Mathf.Min(stepUpVelocity - controllingBody.velocity.y, stepUpVelocity);
						controllingBody.AddForce(f.ToVector3Y(), ForceMode.VelocityChange);
					}
				}
				else if (_didStepUpLastFixedUpdate && controllingBody.velocity.y > float.Epsilon)
				{
					controllingBody.AddForce(-controllingBody.velocity.y.ToVector3Y(), ForceMode.VelocityChange);
				}
				if (magnitude > float.Epsilon)
				{
					Vector3 vector = new Vector3(controllingBody.velocity.x, 0f, controllingBody.velocity.z);
					Vector3 vector2 = -((vector.normalized - new Vector3(movementDir.x, 0f, movementDir.z).normalized) * vector.magnitude) / 2f;
					controllingBody.AddForce(vector2, ForceMode.VelocityChange);
					Vector3 vector3 = vector + vector2;
					Vector3 vector4 = movementDir * (characterMovementStrength * num);
					float num2 = parentEntity?.Suit?.MovementSpeedMultiplier ?? 1f;
					float num3 = 1f;
					if ((bool)parentEntity)
					{
						num3 = ((parentEntity.Hygiene > 1f) ? 1.05f : 1f);
					}
					float num4 = (((bool)parentEntity && parentEntity.IsSoiled) ? 0.95f : 1f);
					float num5 = characterMaxSpeed * ((parentEntity.Mood > 0f) ? 1f : 0.95f) * num2 * num3 * num4;
					float num6 = num5 - vector3.magnitude;
					if (IsAffectedByStorm && parentEntity.CanBeExposedToStorm() && WeatherManager.CurrentEventAffects(parentEntity.Position.y))
					{
						float num7 = WeatherManager.CurrentWeatherEvent.MovementSpeedMultiplier.Value;
						if (parentEntity.SuitSlot.Contains<SuitBase>(out var occupant))
						{
							num7 = occupant.StormMovementSpeedMultiplier;
						}
						num6 *= num7;
					}
					float num8 = parentEntity.DamageState.Stun / 100f;
					if (num8 > float.Epsilon)
					{
						num6 *= Mathf.Clamp01(1f - 0.9f * num8);
					}
					num6 = Mathf.Clamp(num6, 0f, num5);
					vector4 = Vector3.ClampMagnitude(vector4, num6);
					vector4 *= GetOutOfBoundsForceMultiplier(vector4);
					controllingBody.AddForce(vector4, ForceMode.VelocityChange);
					LockToGround(dampen: false);
				}
				else
				{
					LockToGround();
				}
			}
			_didStepUpLastFixedUpdate = IsSteppingUp;
			break;
		}
		case Mode.Ladder:
		{
			controllingBody.useGravity = false;
			IsSteppingUp = false;
			float y = 0f;
			if (KeyManager.GetButton(KeyMap.Forward))
			{
				y = 2f;
			}
			else if (KeyManager.GetButton(KeyMap.Backward))
			{
				if (!IsLadderFloating())
				{
					InventoryManager.Parent.MovementController.ControlMode = Mode.Animation;
					controllingBody.MovePosition(controllingBody.position - parentEntity.CharacterRotationY.forward * 0.7f);
					break;
				}
				y = -2f;
			}
			if (IsInputAscend)
			{
				InventoryManager.Parent.MovementController.ControlMode = Mode.Animation;
				controllingBody.MovePosition(controllingBody.position - parentEntity.CharacterRotationY.forward * 0.7f);
			}
			else
			{
				parentEntity.ThingTransform.rotation = Quaternion.identity;
				parentEntity.CharacterRotationY.localRotation = Quaternion.Euler(0f, parentEntity.TargetLadder.ThingTransform.eulerAngles.y - 180f, 0f);
				controllingBody.velocity = new Vector3(0f, y, 0f);
			}
			break;
		}
		case Mode.Grab:
			IsSteppingUp = false;
			if (KeyManager.GetButton(KeyMap.Grab) && _controlModeCd <= 0f)
			{
				_controlModeCd = 0.5f;
				ControlMode = Mode.Animation;
			}
			IsFalling();
			HandleClimbing(KeyManager.GetRightAxis(), KeyManager.GetForwardAxis());
			if (controllingBody.velocity.magnitude > 3f && !controllingBody.isKinematic)
			{
				controllingBody.velocity = Vector3.ClampMagnitude(controllingBody.velocity, 3f);
			}
			if (controllingAnimator.GetBool(IdleHash))
			{
				controllingBody.AddForce(controllingBody.velocity.normalized * -10f);
			}
			break;
		case Mode.Jetpack:
			IsSteppingUp = false;
			if ((object)jetpack == null)
			{
				ControlMode = Mode.Animation;
				break;
			}
			jetpack.JetPackActivate = true;
			if (KeyManager.GetButton(KeyMap.Jetpack) && _controlModeCd <= 0f && !Cursor.visible)
			{
				_controlModeCd = 0.5f;
				ControlMode = Mode.Animation;
			}
			HandleJetpack(jetpack.JetPackPower, haveGravity: false);
			break;
		case Mode.JetpackGravity:
		{
			IsSteppingUp = false;
			if ((object)jetpack == null)
			{
				ControlMode = Mode.Animation;
			}
			if ((object)jetpack != null && !jetpack.HasPropellent)
			{
				ControlMode = Mode.Animation;
			}
			float force = 0f;
			if ((object)jetpack != null)
			{
				force = jetpack.JetPackPower;
				force *= jetpack.OutputSetting;
				jetpack.JetPackActivate = true;
			}
			if (KeyManager.GetButton(KeyMap.Jetpack) && _controlModeCd <= 0f && !Cursor.visible)
			{
				_controlModeCd = 0.5f;
				ControlMode = Mode.Animation;
			}
			HandleJetpack(force, haveGravity: true);
			break;
		}
		case Mode.Seated:
		case Mode.LyingDown:
		case Mode.CryoTube:
		{
			bool flag = parentEntity.ParentSlot?.Parent != null && parentEntity.ParentSlot.Parent is IPlayerVehicle;
			if (ControlMode == Mode.Seated && !flag && (KeyManager.GetButton(KeyMap.Ascend) || KeyManager.GetButton(KeyMap.Forward)))
			{
				parentEntity.ExitAnything();
			}
			if ((object)jetpack != null)
			{
				if (jetpack.JetPackActivate)
				{
					jetpack.ClearEmissions();
				}
				jetpack.JetPackActivate = false;
			}
			IsSteppingUp = false;
			Transform.localRotation = Quaternion.identity;
			parentEntity.CharacterRotationY.localRotation = Quaternion.identity;
			break;
		}
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	private void LockToGround(bool dampen = true)
	{
		if (dampen && IsGround)
		{
			controllingBody.AddForce(-controllingBody.velocity / 2f, ForceMode.VelocityChange);
		}
		if (NearGround && !_didApplyJumpForce && parentEntity.Room == null)
		{
			controllingBody.AddForce(WorldManager.EarthGravityOffset, ForceMode.Acceleration);
		}
	}

	private void HandleJump()
	{
		if (_didApplyJumpForce)
		{
			_didApplyJumpForce = false;
		}
		else
		{
			if (KeyManager.InputState != KeyInputState.Game || Cursor.visible || !IsGround || _jumping || !IsInputAscend)
			{
				return;
			}
			float num;
			if (parentEntity.Room != null)
			{
				num = 1f;
			}
			else
			{
				if (!WorldManager.HasGravity)
				{
					return;
				}
				num = RocketMath.MapToScale(0f, 1f, 0.6f, 1f, WorldManager.EarthGravityRatio);
			}
			_didApplyJumpForce = true;
			controllingBody.AddForce(dynamicThing.ThingTransform.up * (jumpForce * num), ForceMode.Impulse);
			_setJumpTask = SetJump(JumpHash, 400);
		}
	}

	private Vector3 GetDesiredGroundVelocity()
	{
		if (GameManager.IsBatchMode)
		{
			return Vector3.zero;
		}
		bool flag = IsGround || NearGround;
		Vector3 result = Vector3.zero;
		if (InventoryManager.AllowMouseControl && (flag || IsSteppingUp))
		{
			result = parentEntity.CharacterRotationY.TransformDirection(new Vector3(KeyManager.GetRightAxis(), 0f, KeyManager.GetForwardAxis()));
			result *= movementForce;
			result = Vector3.ClampMagnitude(result, 1f);
		}
		return result;
	}

	private Vector3 GetSlopeWalkingGroundVelocity(in Vector3 groundVelocity)
	{
		Vector3 planeNormal = CalculateGroundNormalForSlopeWalking();
		float magnitude = groundVelocity.magnitude;
		return Vector3.ProjectOnPlane(groundVelocity, planeNormal).normalized * magnitude;
	}

	private static void SortDescending(Span<int> span)
	{
		for (int i = 1; i < span.Length; i++)
		{
			int num = span[i];
			int num2 = i - 1;
			while (num2 >= 0 && span[num2] < num)
			{
				span[num2 + 1] = span[num2];
				num2--;
			}
			span[num2 + 1] = num;
		}
	}

	private float GetStepUpHeight(in Vector3 movementDir)
	{
		if (!CanStepUp)
		{
			return 0f;
		}
		if (movementDir.sqrMagnitude <= 0f)
		{
			return 0f;
		}
		if (_allContacts.Count <= 0)
		{
			return 0f;
		}
		Span<ContactPoint> span = stackalloc ContactPoint[_allContacts.Count];
		int num = 0;
		foreach (ContactPoint allContact in _allContacts)
		{
			if (!(allContact.otherCollider == null) && !(allContact.otherCollider.attachedRigidbody != null) && !allContact.otherCollider.isTrigger)
			{
				span[num++] = allContact;
			}
		}
		Span<ContactPoint> span2 = span;
		Span<ContactPoint> span3 = span2.Slice(0, num);
		Span<int> span4 = stackalloc int[256];
		for (int i = 0; i < span3.Length; i++)
		{
			int num2 = 0;
			for (int j = i + 1; j < span3.Length; j++)
			{
				if (span3[i].otherCollider == span3[j].otherCollider)
				{
					span4[num2++] = j;
				}
			}
			if (num2 == 0)
			{
				continue;
			}
			int num3 = -1;
			float num4 = float.NegativeInfinity;
			bool flag = false;
			Span<int> span5 = span4;
			Span<int> span6 = span5.Slice(0, num2);
			span5 = span6;
			for (int k = 0; k < span5.Length; k++)
			{
				int num5 = span5[k];
				float y = span3[num5].point.y;
				if (y > num4)
				{
					num4 = y;
					num3 = num5;
				}
			}
			if (num3 == -1)
			{
				continue;
			}
			SortDescending(span6);
			span5 = span6;
			for (int k = 0; k < span5.Length; k++)
			{
				int num6 = span5[k];
				if (num6 != num3)
				{
					num--;
					if (num6 == i)
					{
						flag = true;
					}
				}
			}
			if (flag)
			{
				i--;
			}
		}
		if (num <= 0)
		{
			return 0f;
		}
		Vector3 position = parentEntity.ThingTransform.position;
		span2 = span;
		span3 = span2.Slice(0, num);
		for (int num7 = span3.Length - 1; num7 >= 0; num7--)
		{
			float num8 = span3[num7].point.y - position.y;
			if (num8 < _minStepUpHeight || num8 > maxStepUpHeight)
			{
				span3[num7] = span3[span3.Length - 1];
				num--;
			}
		}
		if (num <= 0)
		{
			return 0f;
		}
		span2 = span;
		span3 = span2.Slice(0, num);
		Vector3 normalized = movementDir.normalized;
		for (int num9 = span3.Length - 1; num9 >= 0; num9--)
		{
			Vector3 rhs = Vector3.Normalize(span3[num9].point.ZeroHeight() - position.ZeroHeight());
			if (Vector3.Dot(normalized, rhs) <= 0.5f)
			{
				span3[num9] = span3[span3.Length - 1];
				num--;
			}
		}
		if (num <= 0)
		{
			return 0f;
		}
		span2 = span;
		span3 = span2.Slice(0, num);
		CapsuleCollider masterCollider = parentEntity.MasterCollider;
		float num10 = float.NegativeInfinity;
		Vector3 vector = (masterCollider.height - masterCollider.radius).ToVector3Y();
		Vector3 vector2 = masterCollider.radius.ToVector3Y();
		Vector3 vector3 = movementDir * (Time.fixedDeltaTime * StepUpStepsAhead);
		for (int num11 = span3.Length - 1; num11 >= 0; num11--)
		{
			float num12 = span3[num11].point.y - position.y;
			Vector3 vector4 = position + num12.ToVector3Y();
			Vector3 start = vector4 + vector;
			Vector3 end = vector4 + vector2;
			if (!Physics.CheckCapsule(start, end, masterCollider.radius, parentEntity.AllButPlayerImmune, QueryTriggerInteraction.Ignore))
			{
				start += vector3;
				end += vector3;
				if (!Physics.CheckCapsule(start, end, masterCollider.radius, parentEntity.AllButPlayerImmune, QueryTriggerInteraction.Ignore) && num12 > num10)
				{
					num10 = num12;
				}
			}
		}
		if (float.IsNegativeInfinity(num10))
		{
			return 0f;
		}
		return num10;
	}

	private Vector3 CalculateGroundNormalForSlopeWalking()
	{
		if (!ShouldCheckGroundSlope)
		{
			return Vector3.up;
		}
		Vector3 zero = Vector3.zero;
		int num = 0;
		foreach (ContactPoint allContact in _allContacts)
		{
			if (!(allContact.otherCollider == null) && !(allContact.otherCollider.attachedRigidbody != null) && !allContact.otherCollider.isTrigger)
			{
				Vector3 normal = allContact.normal;
				if (!(Mathf.Abs(Vector3.Dot(Vector3.up, normal)) < 1f - MaxWalkableSlope / 180f))
				{
					normal = Vector3.ProjectOnPlane(normal, parentEntity.CharacterRotationY.right).normalized;
					zero += normal;
					num++;
				}
			}
		}
		if (num <= 0)
		{
			return Vector3.up;
		}
		return zero / num;
	}

	private void AddContacts(Collision other)
	{
		if (other.contactCount > 0 && (CanStepUp || ShouldCheckGroundSlope))
		{
			for (int i = 0; i < other.contactCount; i++)
			{
				_allContacts.Add(other.GetContact(i));
			}
		}
	}

	private void ResetContacts()
	{
		_allContacts.Clear();
	}

	private void OnCollisionEnter(Collision other)
	{
		AddContacts(other);
	}

	private void OnCollisionStay(Collision other)
	{
		AddContacts(other);
	}

	private float GetOutOfBoundsForceMultiplier(Vector3 force)
	{
		if (parentEntity.PlayableAreaState == PlayableAreaRule.Invalid)
		{
			return Mathf.Clamp01(Vector2.Dot(new Vector2(force.x, force.z), parentEntity.LastValidPlayablePosition - new Vector2(parentEntity.Position.x, parentEntity.Position.z)));
		}
		return 1f;
	}

	private void HandleJetpack(float force, bool haveGravity)
	{
		parentEntity.TryGetJetpack(out var jetpack);
		_jetpackUsed = false;
		if ((object)jetpack == null || !jetpack.HasPropellent)
		{
			if ((object)jetpack != null)
			{
				ResetJetpackEmissions();
			}
			return;
		}
		if (ControlMode != Mode.Jetpack && ControlMode != Mode.JetpackGravity)
		{
			jetpack.ClearEmissions();
			return;
		}
		HeightEfficiency = jetpack.GetHeightEfficiency();
		int num = 0;
		if (!Cursor.visible && controllingBody.velocity.magnitude < 50f)
		{
			float forwardAxis = KeyManager.GetForwardAxis();
			if (Mathf.Abs(forwardAxis) > _minimumJetpackMovement)
			{
				float num2 = forwardAxis.CompareTo(0f);
				Vector3 force2 = parentEntity.CharacterRotationY.forward * (force * num2);
				force2 *= GetOutOfBoundsForceMultiplier(force2);
				controllingBody.AddForce(force2);
				num += ((num2 > 0f) ? 4 : 2);
				_jetpackUsed = true;
			}
			float rightAxis = KeyManager.GetRightAxis();
			if (Mathf.Abs(rightAxis) > _minimumJetpackMovement)
			{
				float num3 = rightAxis.CompareTo(0f);
				Vector3 force3 = parentEntity.CharacterRotationY.right * (force * num3);
				force3 *= GetOutOfBoundsForceMultiplier(force3);
				controllingBody.AddForce(force3);
				num += ((num3 > 0f) ? 8 : 16);
				_jetpackUsed = true;
			}
			if (Math.Abs(KeyManager.GetAscend()) > _minimumJetpackMovement)
			{
				Vector3 force4 = parentEntity.CharacterRotationY.up * (force * HeightEfficiency);
				if (haveGravity)
				{
					force4 += -Physics.gravity * (HeightEfficiency * 1.3f);
				}
				controllingBody.AddForce(force4);
				num += 64;
				_jetpackUsed = true;
			}
			if (Math.Abs(KeyManager.GetDescend()) > _minimumJetpackMovement)
			{
				Vector3 force5 = -parentEntity.CharacterRotationY.up * (force * HeightEfficiency);
				if (haveGravity)
				{
					force5 += Physics.gravity * 1.3f;
				}
				controllingBody.AddForce(force5);
				num += 32;
				_jetpackUsed = true;
			}
		}
		if (Stabilizer)
		{
			StabilizeJetpack();
			if (haveGravity)
			{
				num++;
			}
		}
		jetpack.CurrentEmission = num;
	}

	private float SetVelocityAnimRange(float value)
	{
		value = Mathf.Clamp(value, -3.8f, 3.8f);
		if (value > -0.05f && value < 0.05f)
		{
			value = 0f;
		}
		return value;
	}

	private void UpdateControlModeAnimData()
	{
		Jetpack jetpack;
		bool flag = parentEntity.TryGetJetpack(out jetpack);
		Vector3 zero = Vector3.zero;
		float velocityAnimRange = Vector3.Dot(parentEntity.CharacterRotationY.forward, controllingBody.velocity - zero);
		float velocityAnimRange2 = Vector3.Dot(parentEntity.CharacterRotationY.TransformDirection(Vector3.left), controllingBody.velocity - zero);
		velocityAnimRange = SetVelocityAnimRange(velocityAnimRange);
		velocityAnimRange2 = SetVelocityAnimRange(velocityAnimRange2);
		switch (ControlMode)
		{
		case Mode.Animation:
			controllingAnimator.SetFloat(JetpackHash, Mathf.Lerp(controllingAnimator.GetFloat(JetpackHash), 0f, Time.deltaTime * 10f));
			controllingAnimator.SetFloat(VHash, Mathf.Lerp(controllingAnimator.GetFloat(VHash), velocityAnimRange, Time.deltaTime * 10f));
			controllingAnimator.SetFloat(HHash, Mathf.Lerp(controllingAnimator.GetFloat(HHash), velocityAnimRange2, Time.deltaTime * 10f));
			controllingAnimator.SetBool(FlyUpHash, value: false);
			controllingAnimator.SetBool(FlyDownHash, value: false);
			controllingAnimator.SetInteger(LadderHash, 0);
			if ((bool)parentEntity && flag && jetpack.CurrentEmission != jetpack.LastFrameEmissions)
			{
				ResetJetpackEmissions();
			}
			break;
		case Mode.Jetpack:
		case Mode.JetpackGravity:
			controllingAnimator.SetFloat(JetpackHash, Mathf.Lerp(controllingAnimator.GetFloat(JetpackHash), 1f, Time.deltaTime * 10f));
			controllingAnimator.SetBool(GroundedHash, value: true);
			controllingAnimator.SetFloat(HHash, Mathf.Lerp(controllingAnimator.GetFloat(HHash), velocityAnimRange2, 0.5f));
			controllingAnimator.SetFloat(VHash, Mathf.Lerp(controllingAnimator.GetFloat(VHash), velocityAnimRange, 0.5f));
			controllingAnimator.SetBool(FlyUpHash, (double)Math.Abs(KeyManager.GetAscend()) > 0.001);
			controllingAnimator.SetBool(FlyDownHash, (double)Math.Abs(KeyManager.GetDescend()) > 0.001);
			controllingAnimator.SetInteger(LadderHash, 0);
			break;
		case Mode.Grab:
			controllingAnimator.SetFloat(VHash, Mathf.Lerp(controllingAnimator.GetFloat(VHash), KeyManager.GetForwardAxis(), 0.5f));
			controllingAnimator.SetFloat(HHash, Mathf.Lerp(controllingAnimator.GetFloat(HHash), KeyManager.GetRightAxis() * -1f, 0.5f));
			ResetJetpackEmissions();
			break;
		case Mode.Ladder:
		{
			float num = 0f;
			if (KeyManager.GetButton(KeyMap.Forward))
			{
				num = 2f;
			}
			else if (KeyManager.GetButton(KeyMap.Backward) && IsLadderFloating())
			{
				num = -2f;
			}
			controllingAnimator.SetInteger(LadderHash, (int)num);
			controllingAnimator.SetFloat(VHash, num * 3f);
			controllingAnimator.SetFloat(HHash, 0f);
			controllingAnimator.SetBool(GroundedHash, value: true);
			controllingAnimator.SetFloat(JetpackHash, 0f);
			controllingAnimator.SetBool(FlyUpHash, value: false);
			controllingAnimator.SetBool(FlyDownHash, value: false);
			ResetJetpackEmissions();
			break;
		}
		case Mode.Seated:
		case Mode.LyingDown:
		case Mode.CryoTube:
			break;
		}
	}

	private void StabilizeJetpack()
	{
		if (parentEntity.TryGetJetpack(out var jetpack))
		{
			if (WorldManager.HasGravityAtHeight(parentEntity.Transform.position.y))
			{
				float num = Mathf.Max((-Physics.gravity).y, 0f);
				controllingBody.AddForce(Vector3.up * (num * HeightEfficiency), ForceMode.Acceleration);
				jetpack.CurrentEmission = 1;
			}
			bool flag = parentEntity.PlayableAreaState == PlayableAreaRule.Invalid && GetOutOfBoundsForceMultiplier(parentEntity.RigidBody.velocity) < 0.01f;
			if ((!_jetpackUsed && controllingBody.velocity.magnitude > 0f) || flag)
			{
				float num2 = jetpack.JetPackPower * jetpack.OutputSetting * HeightEfficiency;
				Vector3 vector = Vector3.ClampMagnitude(controllingBody.velocity, 1f);
				controllingBody.AddForce(vector * (0f - num2));
			}
			jetpack.JetPackActivate = true;
		}
	}

	public void UnitTest_SetPos(Vector3 position)
	{
		parentEntity.CharacterRotationY.position = Vector3.Lerp(parentEntity.CharacterRotationY.position, position, Time.deltaTime * 1f);
	}

	public void PhysicsUpdate()
	{
		if (GameManager.GameState != GameState.Running || !InventoryManager.EnablePlayerKeys)
		{
			return;
		}
		if (parentEntity != null && parentEntity.ActiveRigidbody != null && parentEntity.ActiveRigidbody.interpolation != RigidbodyInterpolation.Interpolate)
		{
			parentEntity.ActiveRigidbody.interpolation = ((parentEntity.ParentSlot == null) ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None);
		}
		IsGround = parentEntity.IsGrounded(parentEntity.AllButPlayerImmune);
		NearGround = parentEntity.IsGrounded(parentEntity.AllButPlayerImmune, 0.7f);
		if (_jumping && IsGround && !_didApplyJumpForce && _setJumpTask.Status != UniTaskStatus.Pending)
		{
			if (!canFinishJumping)
			{
				canFinishJumping = true;
			}
			else
			{
				_jumping = false;
				canFinishJumping = false;
			}
		}
		parentEntity.BackpackSlot.Get<Jetpack>()?.CalculateHeightEfficiency();
		MovementHandler();
		ResetContacts();
	}

	private async UniTask SetJump(int emoteHash, int time)
	{
		_jumping = true;
		controllingAnimator.SetBool(emoteHash, value: true);
		await UniTask.Delay(time, ignoreTimeScale: false, PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());
		controllingAnimator.SetBool(emoteHash, value: false);
	}

	private void HandleClimbing(float hAxis, float vAxis)
	{
		if (Physics.Raycast(parentEntity.Position, _grabVector, out var hitInfo, 2f, parentEntity.AllButPlayerImmune))
		{
			if (Mathf.Approximately(hitInfo.normal.y, 0f))
			{
				SideGrabLock = true;
				Vector3 vector = Vector3.Cross(hitInfo.normal, -parentEntity.CharacterRotationY.up);
				if (Vector3.Distance(hitInfo.point, parentEntity.ThingTransformPosition) > 1.5f)
				{
					_targetVelocity = parentEntity.CharacterRotationY.forward;
					controllingBody.AddForce(_targetVelocity * 15f);
				}
				_targetVelocity = parentEntity.CharacterRotationY.up * vAxis + vector * (0f - hAxis);
				controllingBody.AddForce(_targetVelocity * 5f);
			}
			else
			{
				Vector3 vector2 = Vector3.Cross(hitInfo.normal, CameraController.CurrentCamera.transform.right);
				if (Vector3.Distance(hitInfo.point, parentEntity.ThingTransformPosition) > 1.5f)
				{
					_targetVelocity = parentEntity.CharacterRotationY.up + vector2;
					controllingBody.AddForce(_targetVelocity * 15f);
				}
				controllingAnimator.SetBool(VerticalClimbHash, value: true);
				_targetVelocity = vector2 * vAxis + CameraController.CurrentCamera.transform.left() * (0f - hAxis);
				controllingBody.AddForce(_targetVelocity * 5f);
			}
		}
		else if (ControlMode != Mode.Ladder)
		{
			ControlMode = Mode.Animation;
		}
	}

	private void EmoteHandler()
	{
		if (KeyManager.GetButtonDown(KeyMap.EmoteWave) && !Cursor.visible)
		{
			Emote.Trigger("wave");
		}
	}

	private void Update()
	{
		if (!WorldManager.IsGamePaused)
		{
			IdleChecker();
			EmoteHandler();
			UpdateControlModeAnimData();
		}
	}

	public bool OnAddToPool(object densePool, int slot)
	{
		if (_densePoolReference.CanAddToPool(densePool))
		{
			return _densePoolReference.AddToPool(densePool, slot);
		}
		return false;
	}

	public void OnRemoveFromPool(object densePool)
	{
		_densePoolReference.OnRemovedFrom(densePool);
	}
}
