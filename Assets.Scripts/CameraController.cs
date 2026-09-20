using System;
using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.FirstPerson;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using TerrainSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityStandardAssets.ImageEffects;
using Util;
using Weather;

namespace Assets.Scripts;

[DefaultExecutionOrder(-1000)]
public class CameraController : ManagerBase
{
	public delegate void ThirdPersonDelegate(bool isThirdPerson);

	public static Vector3 CameraPosition;

	public static Vector3 RigToCameraOffset;

	public static Quaternion CameraRotation;

	public static Vector3 CollisionOffset;

	public static MovementController EntityMovementController;

	public static CameraController Instance;

	private static float MaxPositionLook = 60f;

	private static float MinPositionLook = -60f;

	private static RawImage PortraitRawImage;

	private static int _basePortraitTextureWidth;

	private static int _basePortraitTextureHeight;

	public static Action OnInitialize;

	private static readonly float SensorFxFadeInSpeed = 1.5f;

	private static readonly float SensorFxFadeDownSpeed = 15f;

	private static readonly float SensorFxFadeOutSpeed = 0.4f;

	private static readonly float SensorFxGhostingOffset = 0.993f;

	private static readonly float SensorFxStartingIntensity = 0.25f;

	private static readonly float SensorFxEndingIntensity = 0.6f;

	private static readonly float SensorFxStartingVignetteSize = 1f;

	private static readonly float SensorFxEndingVignetteSize = 3f;

	private static readonly string _profilerTag = "CameraController";

	private static Entity PortraitEntity;

	private static RenderTexture PortraitTexture;

	private Thing _trackedThing;

	public CameraControlMode ControlMode;

	[Header("Portrait Camera")]
	public Camera PortraitCamera;

	public GameObject PortraitCameraDisplay;

	public Shader PortraitShader;

	public GameObject PortraitCameraPrefab;

	[Header("Night Vision")]
	public CameraFilterPack_NightVisionFX NightVisionFX;

	public Light NightVisionLight;

	[Header("Water Vision")]
	public CameraFilterPack_Light_Water WaterVisionFX;

	[Header("Lava Vision")]
	public CameraFilterPack_NightVisionFX LavaVisionFX;

	public GameObject LavaOnFireShader;

	[Header("Sensor Lenses")]
	public CameraFilterPack_TV_80 SensorLensesVisionFX;

	[Header("SolarStorm")]
	public CameraFilterPack_FX_Drunk SolarStormDistortionFX;

	public VignetteAndChromaticAberration SolarStormChromaticAberration;

	[ReadOnly]
	public float RotationY;

	[ReadOnly]
	public float RotationX;

	[ReadOnly]
	public Transform CamRig;

	[ReadOnly]
	public Vector3 JetPackAnimationOffset;

	public Animator CameraAnimator;

	public ThirdPersonOrbitCam ThirdController;

	public float DeltaRotationX;

	public float DeltaRotationY;

	[Header("Rotation & Movement Variables")]
	public float CameraRotationSpeed = 30f;

	public float PlayerRotationSpeed = 10f;

	public static float CameraSensitivity = 2f;

	[Tooltip("How far down the camera can tilt, in degrees")]
	public float CameraTiltMinimum = -90f;

	[Tooltip("How far up the camera can tilt, in degrees")]
	public float CameraTiltMaximum = 90f;

	[Tooltip("Yaw range (left/right) when free-looking from a seat, in degrees")]
	public float SeatedLookYawLimit = 90f;

	[Tooltip("Pitch range (up/down) when free-looking from a seat, in degrees")]
	public float SeatedLookPitchLimit = 90f;

	[Space(30f)]
	public Vector3 CameraPivotOffset = new Vector3(0f, 0.4f, 0f);

	public Transform MainCameraTransform;

	public Camera MainCamera;

	public Camera StormCardCamera;

	[Header("Third Person Cam")]
	public float TPCZoonDelta = 0.5f;

	public float TPCMinZoom = 2f;

	public float TPCMaxZoom = 1f;

	[Header("Camera Effects")]
	public VignetteAndChromaticAberration CameraVignette;

	public CameraFilterPack_Color_BrightContrastSaturation CameraColorControl;

	[Header("FOV")]
	[Tooltip("The maximum value for Field of View when adjusting with the keyboard ingame")]
	public float MaxFoV = 130f;

	[Tooltip("The minimum value for Field of View when adjusting with the keyboard ingame")]
	public float MinFoV = 50f;

	private readonly int _defaultFacePortraitWidth = 185;

	private readonly int _defaultFacePortraitHeight = 264;

	public float RotationalVelocitySmoothed;

	private Vector2 _rotationalVel;

	private float _rotationalVelocity;

	private const float DeltaVCap = 100f;

	private const float SmoothTimeMultiplier = 0.08f;

	private const float MaxSmoothSpeed = 50f;

	private const float MinSmoothRange = 0.11f;

	private const float DampValue = 1.4f;

	private const float DampSpeed = 15f;

	private float _smoothVelocity;

	private bool _resetRotation;

	private float _fadeFactor;

	private float _fadeDownFactor;

	public Antialiasing AntialiasingEffect;

	public TAASimple TaaSimpleEffect;

	public SSAOPro AmbientOcclusionEffect;

	[Header("Layer Masks")]
	[SerializeField]
	private LayerMask _firstPersonMask;

	[SerializeField]
	private LayerMask _thirdPersonMask;

	private bool _isSensorLensesFxActive;

	private CancellationTokenWrapper _solarStormFXCancellationToken = new CancellationTokenWrapper();

	private bool _isSolarStormEffectActive;

	public static bool AlreadyLavaCam;

	public List<Camera> ControlledCameras = new List<Camera>();

	private static List<Camera> _controlledCameras = new List<Camera>();

	private static Camera _mainCamera;

	public static Vector3 EffectiveCameraPosition;

	public Vector3 MainCameraForward;

	public List<CameraEffectCollection> CameraEffects = new List<CameraEffectCollection>();

	private const float LadderLookLimit = 80f;

	private const float SHAKE_DISTANCE = 0.2f;

	private const float SHAKE_DAMPING_SPEED = 1f;

	private Vector3 _shakeOffset;

	private float _solarStormFadeFactor;

	private float _solarStormFXFadeSpeed = 2f;

	private static float _shakeIntensity = 0f;

	public new string ProfilerTag => _profilerTag;

	public Thing TrackedThing
	{
		get
		{
			return _trackedThing;
		}
		private set
		{
			_trackedThing = value;
			TrackedBodyBag = _trackedThing as DynamicBodyBag;
			TrackedEntity = _trackedThing as Entity;
		}
	}

	public Entity TrackedEntity { get; private set; }

	public DynamicBodyBag TrackedBodyBag { get; private set; }

	public Vector3 MainCameraPosition { get; internal set; }

	public static bool IsThirdPerson
	{
		get
		{
			if (Instance != null && Instance.ThirdController.enabled)
			{
				return InventoryManager.Parent != null;
			}
			return false;
		}
	}

	public static bool IsHoldedCamera
	{
		get
		{
			if (IsThirdPerson)
			{
				return KeyManager.GetButton(KeyMap.ThirdPersonControl);
			}
			return false;
		}
	}

	public static Vector3 CameraOrigin
	{
		get
		{
			Vector3 result = CurrentCamera.transform.position - CurrentCamera.transform.rotation * CollisionOffset;
			if (IsThirdPerson)
			{
				result += CurrentCamera.transform.forward * Vector3.Distance(CurrentCamera.transform.position, CameraPosition);
			}
			return result;
		}
	}

	public static Camera CurrentCamera => _mainCamera;

	public static bool CinematicMode { get; set; }

	public static bool IsPlayerOnLadder
	{
		get
		{
			if (EntityMovementController != null)
			{
				return EntityMovementController.ControlMode == MovementController.Mode.Ladder;
			}
			return false;
		}
	}

	private static bool FixedInSlot
	{
		get
		{
			MovementController.Mode controlMode = EntityMovementController.ControlMode;
			return controlMode == MovementController.Mode.Seated || controlMode == MovementController.Mode.LyingDown || controlMode == MovementController.Mode.CryoTube;
		}
	}

	public bool IsSensorLensesFxActive
	{
		get
		{
			return _isSensorLensesFxActive;
		}
		set
		{
			if (value != _isSensorLensesFxActive)
			{
				_isSensorLensesFxActive = value;
				SensorLensesVisionFX.enabled = _isSensorLensesFxActive;
				if (IsSensorLensesFxActive)
				{
					DoSensorLensesFxFadeIn().Forget();
					VoxelTerrain.DirtyAllMinables(InventoryManager.ParentPosition).Forget();
				}
				else
				{
					DoSensorLensesFxFadeOut().Forget();
				}
			}
		}
	}

	public bool IsSolarStormEffectActive
	{
		get
		{
			return _isSolarStormEffectActive;
		}
		set
		{
			if (value != _isSolarStormEffectActive)
			{
				_isSolarStormEffectActive = value;
				_solarStormFXCancellationToken.CancelAndInitialize();
				if (_isSolarStormEffectActive)
				{
					DoSolarStormFxFadeIn(_solarStormFXCancellationToken.Token).Forget();
				}
				else
				{
					DoSolarStormFadeOut(_solarStormFXCancellationToken.Token).Forget();
				}
			}
		}
	}

	private static int PortraitWidth => Mathf.RoundToInt(Mathf.Max((float)_basePortraitTextureWidth * ((float)Settings.CurrentData.HUDScale / 100f), _basePortraitTextureWidth));

	private static int PortraitHeight => Mathf.RoundToInt(Mathf.Max((float)_basePortraitTextureHeight * ((float)Settings.CurrentData.HUDScale / 100f), _basePortraitTextureHeight));

	public bool RunPhysicsUpdate => true;

	public static float CameraShake => _shakeIntensity;

	public static bool IsUnderWater { get; private set; }

	public static event ThirdPersonDelegate ThirdPersonCameraChanged;

	public override void ManagerUpdate()
	{
		base.ManagerUpdate();
		if (GameManager.GameState == GameState.Running)
		{
			bool flag = (InventoryManager.Parent?.Cell != null && InventoryManager.Parent.Cell.HasLight) || (InventoryManager.ParentHuman?.Suit?.InternalAtmosphere != null && InventoryManager.ParentHuman.Suit.InternalAtmosphere.HasLight);
			bool flag2 = WeatherManager.CurrentEventAffects(InventoryManager.ParentPosition.y) && WeatherManager.CurrentWeatherEvent.DirectionalLight != null;
			IsSolarStormEffectActive = flag2 && flag;
		}
	}

	public static void SetFieldOfView(float fov)
	{
		if ((object)Instance == null)
		{
			return;
		}
		foreach (Camera controlledCamera in _controlledCameras)
		{
			controlledCamera.fieldOfView = fov;
		}
	}

	public void ClearSolarStormEffect()
	{
		_isSolarStormEffectActive = false;
		_solarStormFXCancellationToken.Cancel();
		_solarStormFadeFactor = 0f;
		UpdateSolarStormEffectValues(_solarStormFadeFactor);
	}

	public override void ManagerAwake()
	{
		base.ManagerAwake();
		if (!GameManager.IsBatchMode)
		{
			Instance = this;
			_mainCamera = Instance.MainCamera;
			_controlledCameras = Instance.ControlledCameras;
			_basePortraitTextureWidth = _defaultFacePortraitWidth;
			_basePortraitTextureHeight = _defaultFacePortraitHeight;
			PortraitRawImage = PortraitCameraDisplay.GetComponentInChildren<RawImage>(includeInactive: true);
			if (OnInitialize != null)
			{
				OnInitialize();
				OnInitialize = null;
			}
		}
	}

	public void FovIncrease()
	{
		if (GameManager.IsRunning)
		{
			float num = CurrentCamera.fieldOfView;
			if (num < MaxFoV)
			{
				num += 1f;
			}
			SetFieldOfView(num);
		}
	}

	public void FovDecrease()
	{
		if (GameManager.IsRunning)
		{
			float num = CurrentCamera.fieldOfView;
			if (num > MinFoV)
			{
				num -= 1f;
			}
			SetFieldOfView(num);
		}
	}

	public void FovReset()
	{
		if (GameManager.IsRunning)
		{
			SetFieldOfView(Settings.CurrentData.FieldOfView);
		}
	}

	public void SetCullingMask(bool isThirdPerson)
	{
		CurrentCamera.cullingMask = (isThirdPerson ? _thirdPersonMask : _firstPersonMask);
	}

	public void SetCullingMask()
	{
		SetCullingMask(IsThirdPerson);
	}

	public void SetThirdPersonCamera(bool show, bool setCullingMask = true)
	{
		CollisionOffset = Vector3.zero;
		if (show)
		{
			ThirdController.enabled = true;
			ThirdController.Vertical = Mathf.Clamp(0f - RotationX, -45f, 70f);
		}
		else
		{
			RotationX = 0f - ThirdController.Vertical;
			ThirdController.enabled = false;
		}
		if (setCullingMask)
		{
			SetCullingMask();
		}
		CameraController.ThirdPersonCameraChanged?.Invoke(show);
		if (Settings.CurrentData.HelmetOverlay)
		{
			if ((bool)FirstPersonHelmetOverlay.CurrentEquippedHelmet?.Helmet)
			{
				FirstPersonHelmetOverlay.CurrentEquippedHelmet.Helmet.SetActive(!show);
			}
			if (!show && (bool)FirstPersonHelmetOverlay.Instance && (bool)InventoryManager.ParentHuman)
			{
				FirstPersonHelmetOverlay.Instance.EquippedHelmet(InventoryManager.ParentHuman.HeadAsSpaceHelmet);
			}
		}
		InventoryManager.RefreshSlotWearVisibility();
	}

	public void FixedUpdate()
	{
		if (!WorldManager.IsGamePaused && GameManager.GameState == GameState.Running)
		{
			SetUnderwater(AtmosphereHelper.IsSubmerged(CameraPosition));
		}
	}

	public void LateUpdate()
	{
		if (!EntityMovementController || CinematicMode)
		{
			return;
		}
		if ((!EntityMovementController.SideGrabLock || !FixedInSlot) && !IsPlayerOnLadder)
		{
			EntityState state = EntityMovementController.parentEntity.State;
			if ((state == EntityState.Alive || state == EntityState.Unconscious) && (bool)Instance.TrackedEntity)
			{
				MovementController.Mode controlMode = Instance.TrackedEntity.MovementController.ControlMode;
				if (controlMode == MovementController.Mode.Seated || controlMode == MovementController.Mode.CryoTube || controlMode == MovementController.Mode.LyingDown)
				{
					Instance.TrackedEntity.CharacterRotationY.localRotation = Quaternion.Euler(0f, 0f, 0f);
				}
				else
				{
					Instance.TrackedEntity.CharacterRotationY.rotation = Quaternion.Euler(0f, Instance.RotationY, 0f);
				}
			}
		}
		CacheCameraPosition();
		MainCameraForward = MainCameraTransform.forward;
	}

	private void SetMouseLook()
	{
		float num = Singleton<InputManager>.Instance.GetAxis("LookY");
		if (KeyManager.HasAxis(ControllerMap.VerticalLook))
		{
			num = Mathf.Clamp(num + ControllerMap.VerticalLook.Output, -1f, 1f);
		}
		float num2 = Singleton<InputManager>.Instance.GetAxis("LookX");
		if (KeyManager.HasAxis(ControllerMap.HorizontalLook))
		{
			num2 = Mathf.Clamp(num2 + ControllerMap.HorizontalLook.Output, -1f, 1f);
		}
		RotationX += num * CameraSensitivity * (float)((!Settings.CurrentData.InvertMouse) ? 1 : (-1));
		RotationY += num2 * CameraSensitivity;
		RotationX = InputHelpers.ClampAngle(RotationX, CameraTiltMinimum, CameraTiltMaximum);
	}

	public void RotateCameraToSlot(Human human)
	{
		MainCameraTransform.rotation = human.CameraRig.rotation;
		RotationY = 0f;
		RotationX = 0f;
	}

	public static void SetCameraPosition()
	{
		if ((object)Instance != null && !CinematicMode)
		{
			Instance.CacheCameraPosition();
		}
	}

	private void CacheCameraPosition()
	{
		if ((bool)TrackedBodyBag)
		{
			TrackedBodyBag.OnCameraUpdate(this);
		}
		else
		{
			if (!TrackedEntity)
			{
				return;
			}
			float rotationX = RotationX;
			float rotationY = RotationY;
			if (!ThirdController.enabled && !Cursor.visible && !TrackedEntity.Unconscious && !TrackedEntity.Dead && !WorldManager.IsGamePaused)
			{
				SetMouseLook();
				if (IsPlayerOnLadder)
				{
					float y = TrackedEntity.CharacterRotationY.eulerAngles.y;
					float num = RocketMath.SignedAngle(y, RotationY);
					if (num < -80f)
					{
						RotationY = y + 80f;
					}
					if (num > 80f)
					{
						RotationY = y - 80f;
					}
				}
				else
				{
					RotationY = InputHelpers.ClampAngle(RotationY, -360f, 360f);
				}
			}
			if (!Cursor.visible && !(InventoryManager.Instance.ActiveHand.Slot.Occupant as Tablet) && KeyManager.GetButton(KeyMap.ThirdPersonControl) && Settings.CurrentData.MouseWheelZoom && !EventSystem.current.IsPointerOverGameObject())
			{
				if (Input.mouseScrollDelta.y > 0f)
				{
					ThirdPersonOrbitCam.zoomOffset.z = Mathf.Clamp(ThirdPersonOrbitCam.zoomOffset.z + TPCZoonDelta, 0f - TPCMaxZoom, 4f);
				}
				else if (Input.mouseScrollDelta.y < 0f)
				{
					ThirdPersonOrbitCam.zoomOffset.z = Mathf.Clamp(ThirdPersonOrbitCam.zoomOffset.z - TPCZoonDelta, 0f - TPCMaxZoom, 4f);
				}
				if (ThirdController.enabled != ThirdPersonOrbitCam.zoomOffset.z < TPCMinZoom)
				{
					SetThirdPersonCamera(!ThirdController.enabled);
				}
			}
			if (!FixedInSlot && !TrackedEntity.Unconscious)
			{
				float num2 = RocketMath.MapToScale(CameraTiltMinimum, CameraTiltMaximum, MinPositionLook, MaxPositionLook, RotationX);
				Quaternion cameraRotation = Quaternion.Euler(0f - RotationX, RotationY, 0f);
				Quaternion quaternion = Quaternion.Euler(0f - num2, RotationY, 0f);
				RigToCameraOffset = quaternion * CameraPivotOffset + quaternion * CollisionOffset + CamRig.TransformDirection(JetPackAnimationOffset);
				CameraPosition = CamRig.position + RigToCameraOffset;
				CameraRotation = cameraRotation;
				if (IsThirdPerson)
				{
					ThirdController.UpdateCamera();
				}
				else
				{
					MainCameraTransform.SetPositionAndRotation(CameraPosition + _shakeOffset, CameraRotation);
					EffectiveCameraPosition = MainCameraTransform.position;
				}
				_resetRotation = false;
				CameraPosition = MainCameraTransform.position;
				if (!IsThirdPerson)
				{
					Debug.DrawLine(CamRig.position, CameraPosition);
				}
			}
			else
			{
				if (InventoryManager.Parent?.ParentSlot?.Parent is IVehicleCamera vehicleCamera)
				{
					Transform transform = (IsThirdPerson ? vehicleCamera.GetVehicleCameraTransform() : TrackedEntity.CameraRig);
					MainCameraTransform.forward = transform.forward;
					MainCameraTransform.position = transform.position;
					RotationY = 0f;
					RotationX = 0f;
					_resetRotation = false;
				}
				else if (!Cursor.visible && !TrackedEntity.Unconscious)
				{
					switch (ControlMode)
					{
					case CameraControlMode.Default:
						MainCameraTransform.forward = TrackedEntity.CameraRig.forward;
						MainCameraTransform.position = TrackedEntity.CameraRig.position;
						RotationY = 0f;
						RotationX = 0f;
						_resetRotation = false;
						break;
					case CameraControlMode.MouseLook:
					{
						if (!_resetRotation)
						{
							RotationY = 0f;
							RotationX = 0f;
							_resetRotation = true;
						}
						SetMouseLook();
						RotationY = InputHelpers.ClampAngle(RotationY, 0f - SeatedLookYawLimit, SeatedLookYawLimit);
						RotationX = InputHelpers.ClampAngle(RotationX, 0f - SeatedLookPitchLimit, SeatedLookPitchLimit);
						Vector3 eulerAngles = TrackedEntity.CameraRig.rotation.eulerAngles;
						Quaternion b = Quaternion.Euler(eulerAngles.x - RotationX, eulerAngles.y + RotationY, eulerAngles.z);
						CameraRotation = Quaternion.Lerp(MainCameraTransform.rotation, b, Time.smoothDeltaTime * CameraRotationSpeed);
						MainCameraTransform.SetPositionAndRotation(TrackedEntity.CameraRig.position + _shakeOffset, CameraRotation);
						break;
					}
					}
				}
				else if ((bool)MainCameraTransform && (bool)TrackedEntity.CameraRig)
				{
					MainCameraTransform.position = TrackedEntity.CameraRig.position + _shakeOffset;
				}
				CameraPosition = MainCameraTransform.position;
				EffectiveCameraPosition = CameraPosition;
			}
			if ((bool)TrackedEntity)
			{
				TrackedEntity.OnCameraUpdate(this);
			}
			DeltaRotationX = RotationX - rotationX;
			DeltaRotationY = RotationY - rotationY;
			_rotationalVel.x = DeltaRotationX;
			_rotationalVel.y = DeltaRotationY;
			float magnitude = _rotationalVel.magnitude;
			if (!(Time.deltaTime <= 0f))
			{
				float num3 = Mathf.Abs(magnitude - _rotationalVelocity) / Time.deltaTime;
				_rotationalVelocity = magnitude;
				float smoothTime = Mathf.Clamp(num3 / 100f, 0.11f, 1f) * 0.08f;
				float num4 = Mathf.SmoothDamp(RotationalVelocitySmoothed, _rotationalVelocity, ref _smoothVelocity, smoothTime, 50f);
				if (magnitude < 1.4f && magnitude < num4)
				{
					num4 = Mathf.Lerp(num4, magnitude, Time.deltaTime * 15f);
				}
				if (!float.IsNaN(num4))
				{
					RotationalVelocitySmoothed = num4;
				}
				if (_shakeIntensity > float.Epsilon)
				{
					_shakeOffset = UnityEngine.Random.insideUnitSphere * (0.2f * _shakeIntensity);
					_shakeIntensity -= 1f * Time.deltaTime;
				}
				MainCameraPosition = MainCameraTransform.position;
			}
		}
	}

	public void UnitTest_Rotation(float InputX, float InputY)
	{
		if ((bool)TrackedEntity)
		{
			if (!Cursor.visible)
			{
				RotationY += InputX * CameraSensitivity;
				RotationX += InputY * CameraSensitivity;
				RotationX = InputHelpers.ClampAngle(RotationX, -90f, 90f);
			}
			UnitTest_SetRotation(RotationX, RotationY);
		}
	}

	public void UnitTest_SetRotation(float InRotationX, float InRotationY)
	{
		if ((bool)TrackedEntity)
		{
			RotationX = InRotationX;
			RotationY = InRotationY;
			MainCameraTransform.rotation = Quaternion.Lerp(MainCameraTransform.rotation, Quaternion.Euler(0f - RotationX, RotationY, 0f), Time.smoothDeltaTime * CameraRotationSpeed);
			Quaternion quaternion = Quaternion.Euler(0f - RotationX, RotationY, 0f);
			MainCameraTransform.position = CamRig.position + quaternion * CameraPivotOffset + MainCameraTransform.rotation * CollisionOffset;
			if ((bool)TrackedEntity)
			{
				TrackedEntity.OnCameraUpdate(this);
			}
		}
	}

	public void InitializeCamera(Thing trackedThing)
	{
		TrackedThing = trackedThing;
		if ((bool)TrackedEntity)
		{
			EntityMovementController = TrackedEntity.GetComponent<MovementController>();
			MainCameraTransform = CurrentCamera.transform;
			CamRig = TrackedEntity.CameraRig;
			MainCameraTransform.rotation = TrackedEntity.ThingTransform.rotation;
			RotationY = MainCameraTransform.rotation.eulerAngles.y;
			if (TrackedEntity.ParentSlot == null)
			{
				TrackedEntity.MovementController.ControlMode = MovementController.Mode.Animation;
				TrackedEntity.CameraRig = Instance.CamRig;
				Instance.ControlMode = CameraControlMode.Default;
			}
		}
	}

	public static void SetHudScale(CanvasScaler canvasScaler)
	{
		Vector2 referenceResolution = canvasScaler.referenceResolution;
		referenceResolution.y = RocketMath.MapToScale(20f, 140f, 2304f, 768f, Settings.CurrentData.HUDScale);
		canvasScaler.referenceResolution = referenceResolution;
	}

	public static void SetPortrait(Entity parent)
	{
		if (!Instance || !Instance.PortraitCamera)
		{
			return;
		}
		if (parent == null)
		{
			Instance.PortraitCamera.transform.parent = null;
			return;
		}
		Slot slot = parent.Slots.Find((Slot s) => s.Type == Slot.Class.Helmet);
		if (slot != null)
		{
			Instance.PortraitCamera.transform.parent = slot.Location.parent.parent;
			Instance.PortraitCamera.transform.localPosition = new Vector3(-0.676f, 1.532f, 0f);
			Instance.PortraitCamera.transform.localRotation = Quaternion.Euler(108.985f, 270f, -1.5258f);
			PortraitEntity = parent;
		}
		Instance.PortraitCamera.SetReplacementShader(Instance.PortraitShader, "RenderType");
		RefreshPortrait();
		WorldManager.OnHudScaleUpdate += RefreshPortrait;
	}

	public static void RefreshPortrait()
	{
		if (!GameManager.IsBatchMode && (bool)Instance && (bool)Instance.PortraitCamera)
		{
			Instance.PortraitCamera.gameObject.SetActive(Settings.CurrentData.IngamePortrait && (bool)PortraitEntity);
			Instance.PortraitCameraDisplay.SetActive(Settings.CurrentData.IngamePortrait && (bool)PortraitEntity);
			PortraitTexture = new RenderTexture(PortraitWidth, PortraitHeight, 0, RenderTextureFormat.ARGB32);
			PortraitTexture.filterMode = FilterMode.Trilinear;
			PortraitTexture.antiAliasing = ((QualitySettings.antiAliasing <= 0) ? 1 : QualitySettings.antiAliasing);
			PortraitTexture.anisoLevel = 0;
			Instance.PortraitCamera.targetTexture = PortraitTexture;
			PortraitRawImage.texture = PortraitTexture;
		}
	}

	private static void SetAntialising(Antialiasing component)
	{
		switch (Settings.CurrentData.Antialiasing)
		{
		case "None":
			component.enabled = false;
			QualitySettings.antiAliasing = 1;
			break;
		case "SSAA":
			component.enabled = true;
			component.mode = AAMode.SSAA;
			QualitySettings.antiAliasing = 2;
			break;
		case "DLAA":
			component.enabled = true;
			component.mode = AAMode.DLAA;
			QualitySettings.antiAliasing = 2;
			break;
		case "FXAA":
			component.enabled = true;
			component.mode = AAMode.FXAA3Console;
			QualitySettings.antiAliasing = 4;
			break;
		case "TAA":
			component.mode = AAMode.TAA;
			QualitySettings.antiAliasing = 5;
			break;
		default:
			ConsoleWindow.PrintError("AntiAliasing value invalid: " + Settings.CurrentData.Antialiasing);
			break;
		}
	}

	public static void SetAntialiasing()
	{
		if (Instance != null)
		{
			SetAntialising(Instance.AntialiasingEffect);
			if (string.Equals(Settings.CurrentData.Antialiasing, "TAA") && GameManager.GameState != GameState.None)
			{
				Instance.TaaSimpleEffect.enabled = true;
			}
			else
			{
				Instance.TaaSimpleEffect.enabled = false;
			}
			if (Instance.PortraitCamera != null && !string.Equals(Settings.CurrentData.Antialiasing, "DLAA"))
			{
				SetAntialising(Instance.PortraitCamera.GetComponent<Antialiasing>());
			}
		}
	}

	private static void SetAmbientOcclusion(SSAOPro component)
	{
		component.enabled = false;
	}

	public static void SetAmbientOcclusion()
	{
		if (Instance != null)
		{
			SetAmbientOcclusion(Instance.AmbientOcclusionEffect);
		}
	}

	public static void SetUnderLava(bool show)
	{
		if ((object)Instance != null)
		{
			AlreadyLavaCam = show;
			Instance.LavaOnFireShader.SetActive(show);
			if (show)
			{
				Instance.LavaVisionFX.enabled = true;
			}
			else
			{
				Instance.LavaVisionFX.enabled = false;
			}
		}
	}

	private void UpdateSolarStormEffectValues(float t)
	{
		SolarStormChromaticAberration.chromaticAberration = Mathf.Lerp(0f, 3f, t);
		SolarStormChromaticAberration.intensity = Mathf.Lerp(0f, 0.3f, t);
		SolarStormChromaticAberration.blurDistance = Mathf.Lerp(0f, 0.2f, t);
		SolarStormChromaticAberration.blur = Mathf.Lerp(0f, 0.35f, t);
		SolarStormDistortionFX.Fade = Mathf.Lerp(0f, 0.2f, t);
	}

	private async UniTaskVoid DoSolarStormFxFadeIn(CancellationToken token)
	{
		while (IsSolarStormEffectActive && _solarStormFadeFactor < 1f && !token.IsCancellationRequested)
		{
			SolarStormChromaticAberration.enabled = true;
			SolarStormDistortionFX.enabled = true;
			_solarStormFadeFactor = Mathf.Clamp01(_solarStormFadeFactor + Time.deltaTime / _solarStormFXFadeSpeed);
			UpdateSolarStormEffectValues(_solarStormFadeFactor);
			await UniTask.NextFrame(token);
		}
		if (!token.IsCancellationRequested)
		{
			_solarStormFadeFactor = 1f;
			UpdateSolarStormEffectValues(_solarStormFadeFactor);
		}
	}

	private async UniTaskVoid DoSolarStormFadeOut(CancellationToken token)
	{
		while (!IsSolarStormEffectActive && _solarStormFadeFactor > 0f && !token.IsCancellationRequested)
		{
			_solarStormFadeFactor = Mathf.Clamp01(_solarStormFadeFactor - Time.deltaTime / _solarStormFXFadeSpeed);
			UpdateSolarStormEffectValues(_solarStormFadeFactor);
			await UniTask.NextFrame(token);
		}
		if (!token.IsCancellationRequested)
		{
			_solarStormFadeFactor = 0f;
			UpdateSolarStormEffectValues(_solarStormFadeFactor);
			SolarStormChromaticAberration.enabled = false;
			SolarStormDistortionFX.enabled = false;
		}
	}

	private async UniTaskVoid DoSensorLensesFxFadeIn()
	{
		_fadeFactor = 0f;
		while (GameManager.GameState == GameState.Running && IsSensorLensesFxActive && _fadeFactor < 1f)
		{
			_fadeFactor = Mathf.Clamp01(_fadeFactor + Time.deltaTime / SensorFxFadeInSpeed);
			SensorLensesVisionFX.Fade = Mathf.Lerp(SensorFxStartingIntensity, SensorFxEndingIntensity, _fadeFactor);
			SensorLensesVisionFX.Ghosting = Mathf.Lerp(SensorFxGhostingOffset, 1f, _fadeFactor);
			SensorLensesVisionFX.Vignette = Mathf.Lerp(SensorFxStartingVignetteSize, SensorFxEndingVignetteSize, _fadeFactor);
			await UniTask.NextFrame();
		}
		_fadeDownFactor = 0f;
		while (GameManager.GameState == GameState.Running && IsSensorLensesFxActive && _fadeDownFactor < 1f)
		{
			_fadeDownFactor = Mathf.Clamp01(_fadeDownFactor + Time.deltaTime / SensorFxFadeDownSpeed);
			SensorLensesVisionFX.Fade = Mathf.Lerp(SensorFxEndingIntensity, SensorFxStartingIntensity, _fadeDownFactor);
			SensorLensesVisionFX.Intensity = Mathf.Lerp(1f, 0f, _fadeDownFactor);
			await UniTask.NextFrame();
		}
	}

	private async UniTaskVoid DoSensorLensesFxFadeOut()
	{
		_fadeFactor = 1f;
		float startFade = SensorLensesVisionFX.Fade;
		float startGhost = SensorLensesVisionFX.Ghosting;
		float startVignette = SensorLensesVisionFX.Vignette;
		while (GameManager.GameState == GameState.Running && !IsSensorLensesFxActive && _fadeFactor > 0f)
		{
			_fadeFactor = Mathf.Clamp01(_fadeFactor - Time.deltaTime / SensorFxFadeOutSpeed);
			SensorLensesVisionFX.Fade = Mathf.Lerp(startFade, SensorFxEndingIntensity, _fadeFactor);
			SensorLensesVisionFX.Ghosting = Mathf.Lerp(startGhost, 1f, _fadeFactor);
			SensorLensesVisionFX.Vignette = Mathf.Lerp(startVignette, SensorFxEndingVignetteSize, _fadeFactor);
			await UniTask.NextFrame();
		}
	}

	public static void SetUnderwater(bool show)
	{
		if (!GameManager.IsBatchMode)
		{
			IsUnderWater = show;
			Instance.WaterVisionFX.enabled = show;
		}
	}

	public static void SetNightVision(bool show, float binocularSize = 0.5f, float duringTime = 0.3f, bool robotMode = false)
	{
		if (GameManager.IsBatchMode)
		{
			return;
		}
		LeanTween.cancel(Instance.gameObject);
		if (show)
		{
			Instance.NightVisionFX.Preset = CameraFilterPack_NightVisionFX.preset.Night_Vision_Full;
			Instance.NightVisionFX.enabled = true;
			Instance.NightVisionLight.gameObject.SetActive(value: true);
			Instance.NightVisionFX.OnOff = 0f;
			LeanTween.value(Instance.gameObject, Instance.NightVisionFX.Greenness, 1.45f, duringTime).setOnUpdate(delegate(float value)
			{
				Instance.NightVisionFX.Greenness = value;
			}).setEase(LeanTweenType.easeOutExpo);
			LeanTween.value(Instance.gameObject, Instance.NightVisionFX.Vignette, 0.292f, duringTime).setOnUpdate(delegate(float value)
			{
				Instance.NightVisionFX.Vignette = value;
			}).setEase(LeanTweenType.easeOutExpo);
			LeanTween.value(Instance.gameObject, Instance.NightVisionFX.Vignette_Alpha, 0.112f, duringTime).setOnUpdate(delegate(float value)
			{
				Instance.NightVisionFX.Vignette_Alpha = value;
			}).setEase(LeanTweenType.easeOutExpo);
			LeanTween.value(Instance.gameObject, Instance.NightVisionFX.Noise, 0.045f, duringTime).setOnUpdate(delegate(float value)
			{
				Instance.NightVisionFX.Noise = value;
			}).setEase(LeanTweenType.easeOutExpo);
			LeanTween.value(Instance.gameObject, Instance.NightVisionFX.Intensity, -0.077f, duringTime).setOnUpdate(delegate(float value)
			{
				Instance.NightVisionFX.Intensity = value;
			}).setEase(LeanTweenType.easeOutExpo);
			LeanTween.value(Instance.gameObject, Instance.NightVisionFX.Light, 0.071f, duringTime).setOnUpdate(delegate(float value)
			{
				Instance.NightVisionFX.Light = value;
			}).setEase(LeanTweenType.easeOutExpo);
			LeanTween.value(Instance.gameObject, Instance.NightVisionFX.Line, 0f, duringTime).setOnUpdate(delegate(float value)
			{
				Instance.NightVisionFX.Line = value;
			}).setEase(LeanTweenType.easeOutExpo);
			LeanTween.value(Instance.gameObject, Instance.NightVisionFX.Color_R, -2f, duringTime).setOnUpdate(delegate(float value)
			{
				Instance.NightVisionFX.Color_R = value;
			}).setEase(LeanTweenType.easeOutExpo);
			LeanTween.value(Instance.gameObject, Instance.NightVisionFX.Color_G, 0.27f, duringTime).setOnUpdate(delegate(float value)
			{
				Instance.NightVisionFX.Color_G = value;
			}).setEase(LeanTweenType.easeOutExpo);
			LeanTween.value(Instance.gameObject, Instance.NightVisionFX.Color_B, -2f, duringTime).setOnUpdate(delegate(float value)
			{
				Instance.NightVisionFX.Color_B = value;
			}).setEase(LeanTweenType.easeOutExpo);
			LeanTween.value(Instance.gameObject, 1f, binocularSize, duringTime).setOnUpdate(delegate(float value)
			{
				Instance.NightVisionFX._Binocular_Size = value;
			}).setEase(LeanTweenType.easeOutExpo);
		}
		else
		{
			Instance.NightVisionLight.gameObject.SetActive(value: false);
			LeanTween.value(Instance.gameObject, Instance.NightVisionFX.Greenness, 0f, duringTime).setOnUpdate(delegate(float value)
			{
				Instance.NightVisionFX.Greenness = value;
			}).setEase(LeanTweenType.linear);
			LeanTween.value(Instance.gameObject, Instance.NightVisionFX.Vignette, 0f, duringTime).setOnUpdate(delegate(float value)
			{
				Instance.NightVisionFX.Vignette = value;
			}).setEase(LeanTweenType.linear);
			LeanTween.value(Instance.gameObject, Instance.NightVisionFX.Vignette_Alpha, 0f, duringTime).setOnUpdate(delegate(float value)
			{
				Instance.NightVisionFX.Vignette_Alpha = value;
			}).setEase(LeanTweenType.linear);
			LeanTween.value(Instance.gameObject, Instance.NightVisionFX.Noise, 0f, duringTime).setOnUpdate(delegate(float value)
			{
				Instance.NightVisionFX.Noise = value;
			}).setEase(LeanTweenType.linear);
			LeanTween.value(Instance.gameObject, Instance.NightVisionFX.Intensity, 0f, duringTime).setOnUpdate(delegate(float value)
			{
				Instance.NightVisionFX.Intensity = value;
			}).setEase(LeanTweenType.linear);
			LeanTween.value(Instance.gameObject, Instance.NightVisionFX.Light, 0f, duringTime).setOnUpdate(delegate(float value)
			{
				Instance.NightVisionFX.Light = value;
			}).setEase(LeanTweenType.linear);
			LeanTween.value(Instance.gameObject, Instance.NightVisionFX.Line, 0f, duringTime).setOnUpdate(delegate(float value)
			{
				Instance.NightVisionFX.Line = value;
			}).setEase(LeanTweenType.linear);
			LeanTween.value(Instance.gameObject, Instance.NightVisionFX.Color_R, 0f, duringTime).setOnUpdate(delegate(float value)
			{
				Instance.NightVisionFX.Color_R = value;
			}).setEase(LeanTweenType.linear);
			LeanTween.value(Instance.gameObject, Instance.NightVisionFX.Color_G, 0f, duringTime).setOnUpdate(delegate(float value)
			{
				Instance.NightVisionFX.Color_G = value;
			}).setEase(LeanTweenType.linear);
			LeanTween.value(Instance.gameObject, Instance.NightVisionFX.Color_B, 0f, duringTime).setOnUpdate(delegate(float value)
			{
				Instance.NightVisionFX.Color_B = value;
			}).setEase(LeanTweenType.linear);
			LeanTween.value(Instance.gameObject, binocularSize, 1f, duringTime).setOnUpdate(delegate(float value)
			{
				Instance.NightVisionFX._Binocular_Size = value;
			}).setEase(LeanTweenType.easeOutExpo)
				.setOnComplete((Action)delegate
				{
					Instance.NightVisionFX.enabled = false;
					Instance.NightVisionFX.OnOff = 1f;
				});
		}
		Human.CurrentlyUsingNightVision = show;
	}

	private static void NightVisionOn()
	{
		SetNightVision(show: true);
	}

	private static void NightVisionOff()
	{
		SetNightVision(show: false);
	}

	public static void SetActive(bool b)
	{
		if ((object)Instance == null)
		{
			return;
		}
		foreach (Camera controlledCamera in _controlledCameras)
		{
			controlledCamera.enabled = b;
		}
	}

	public static void SetVolumetricLights(bool enabled)
	{
		if ((object)Instance == null)
		{
			return;
		}
		foreach (CameraEffectCollection cameraEffect in Instance.CameraEffects)
		{
			if ((object)cameraEffect.VolumetricLight != null)
			{
				cameraEffect.VolumetricLight.enabled = enabled;
			}
		}
	}

	public static void SetVolumetricLightResolution(VolumetricLightRenderer.VolumtericResolution setting)
	{
		if ((object)Instance == null)
		{
			return;
		}
		foreach (CameraEffectCollection cameraEffect in Instance.CameraEffects)
		{
			if ((object)cameraEffect.VolumetricLight != null)
			{
				cameraEffect.VolumetricLight.Resolution = setting;
			}
		}
	}

	public static void SetBloom(bool enabled)
	{
		if ((object)Instance == null)
		{
			return;
		}
		foreach (CameraEffectCollection cameraEffect in Instance.CameraEffects)
		{
			cameraEffect.Bloom.enabled = enabled;
		}
	}

	public static void SetCameraShake(float intensity)
	{
		if ((object)Instance != null)
		{
			_shakeIntensity = Mathf.Max(Mathf.Clamp(intensity, 0f, 2.5f), _shakeIntensity);
		}
	}

	public static void ClearCameraShake()
	{
		_shakeIntensity = 0f;
	}
}
