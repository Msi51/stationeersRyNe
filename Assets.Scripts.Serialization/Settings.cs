using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using Assets.Scripts.FirstPerson;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.OpenNat;
using Assets.Scripts.Sound;
using Assets.Scripts.UI;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.Util;
using ch.sycoforge.Flares;
using SFB;
using TerrainSystem;
using TerrainSystem.Lods;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Serialization;

public class Settings : UserInterfaceBase
{
	[XmlRoot]
	public class SettingData
	{
		[XmlElement]
		public string SettingsVersion = string.Empty;

		[XmlElement]
		public bool ShowFps;

		[XmlElement]
		public bool ShowLatency;

		[XmlElement]
		public bool AutoSave = true;

		[XmlElement]
		public int SaveInterval = 300;

		[XmlElement]
		public int MaxAutoSaves = 5;

		[XmlElement]
		public int MaxQuickSaves = 5;

		[XmlElement]
		public string SavePath = string.Empty;

		[XmlElement]
		public int HUDScale = 50;

		[XmlElement]
		public float TooltipOpacity = 0.95f;

		[XmlElement]
		public bool IngamePortrait = true;

		[XmlElement]
		public bool ExtendedTooltips = true;

		[XmlElement]
		public float ChatFadeTimer = 10f;

		[XmlElement]
		public int DayLength = 20;

		[XmlElement]
		public bool LegacyInventory;

		[XmlElement]
		public bool ShowSlotToolTips = true;

		public DeleteSkeletonOnDecay DeleteSkeletonOnDecay;

		[XmlElement]
		public int Monitor = 1;

		[XmlElement]
		public string ScreenWidth = "1920";

		[XmlElement]
		public string ScreenHeight = "1080";

		[XmlElement]
		public int RefreshRate = 60;

		[XmlElement]
		public string GraphicQuality = "Fantastic";

		[XmlElement]
		public string TextureQuality = "Very High";

		[XmlElement]
		public bool FullScreen = true;

		[XmlElement]
		public bool Vsync;

		[XmlElement]
		public string Shadows = "High";

		[XmlElement]
		public bool DistantShadows;

		[XmlElement]
		public string ShadowResolution = "Very High";

		[XmlElement]
		public int ShadowDistance = 100;

		[XmlElement]
		public int LightShadowDistance = 50;

		[XmlElement]
		public int RoomControlTickSpeed = 1;

		[XmlElement]
		public float ShadowNearPlaneOffset = 0.2f;

		[XmlElement]
		public int ShadowCascades = 4;

		[XmlElement]
		public float ShadowCascade2Split = 1f / 3f;

		[XmlElement]
		public Vector3 ShadowCascade4Split = new Vector3(1f / 15f, 0.2f, 7f / 15f);

		[XmlElement]
		public string ThingShadowMode = "High";

		[XmlElement]
		public float ThingShadowDistanceMultiplier = 2f;

		[XmlElement]
		public string RenderDistance = "High";

		[XmlElement]
		public bool WorldOrigin;

		[XmlElement]
		public int Brightness = 100;

		[XmlElement]
		public int FieldOfView = 70;

		[XmlElement]
		public string ColorBlind = "None";

		[XmlElement]
		public string ParticleQuality = "High";

		[XmlElement]
		public bool SoftParticles = true;

		[XmlElement]
		public bool EnvironmentElements = true;

		[XmlElement]
		public bool ExtendedTerrain = true;

		[XmlElement]
		public string VolumeLight = "Full";

		[XmlElement]
		public int PixelLightCount = 8;

		[XmlElement]
		public int MaxThingLights = 256;

		[XmlElement]
		public string Antialiasing = "FXAA";

		[XmlElement]
		public string FrameLock = "Off";

		[XmlElement]
		public bool AtmosphericScattering = true;

		[XmlElement]
		public string AmbientOcclusion = "Ultra";

		[XmlElement]
		public bool LensFlares = true;

		[XmlElement]
		public bool DisableWaterVisualizer = true;

		[XmlElement]
		public bool Clouds;

		[XmlElement]
		public bool HelmetOverlay = true;

		[XmlElement]
		public string WeatherEventQuality = "Medium";

		[XmlElement]
		public string TerrainDetail = "Medium";

		[XmlElement]
		public string MinableDistance = "Medium";

		[XmlElement]
		public string TerrainDistance = "Medium";

		[XmlElement]
		public int MasterVolume = 100;

		[XmlElement]
		public int SoundVolume = 100;

		[XmlElement]
		public int VoiceNotificationVolume = 90;

		[XmlElement]
		public int MusicVolume = 100;

		[XmlElement]
		public int InterfaceVolume = 100;

		[XmlElement]
		public int VirtualVoices = 512;

		[XmlElement]
		public int RealVoices = 32;

		[XmlElement]
		public AudioSpeakerMode UserSpeakerMode = AudioSpeakerMode.Stereo;

		[XmlElement]
		public string ServerName = "Stationeers";

		[XmlElement]
		public bool StartLocalHost;

		[XmlElement]
		public bool ServerVisible;

		[XmlElement]
		public string ServerPassword = string.Empty;

		[XmlElement]
		public string AdminPassword = string.Empty;

		public string ServerAuthSecret = string.Empty;

		[XmlElement]
		public int ServerMaxPlayers = 10;

		[XmlElement]
		public string UpdatePort = "27015";

		[XmlElement]
		public string GamePort = "27016";

		[XmlElement]
		public bool UPNPEnabled = true;

		[XmlElement]
		public bool UseSteamP2P = true;

		[XmlElement]
		public int DisconnectTimeout = 10000;

		[XmlElement]
		public int NetworkDebugFrequency = 500;

		[XmlElement]
		public string LocalIpAddress = string.Empty;

		public bool AutoPauseServer = true;

		[XmlElement]
		public LanguageCode LanguageCode = LanguageCode.EN;

		[XmlElement]
		public LanguageCode VoiceLanguageCode = LanguageCode.EN;

		[XmlElement]
		public bool Voice;

		[XmlElement]
		public bool PopupChat = true;

		[XmlElement]
		public int CameraSensitivity = 50;

		[XmlArray("Keyboard")]
		public List<KeyItem> KeyList = new List<KeyItem>();

		[XmlElement]
		public bool InvertMouse;

		[XmlElement]
		public bool InvertMouseWheelInventory;

		[XmlElement]
		public bool MenuLite;

		[XmlElement]
		public bool MouseWheelZoom = true;

		[XmlElement]
		public bool FirstRun = true;

		[XmlArray]
		public List<VoiceNotificationData> VoiceNotifications = new List<VoiceNotificationData>();

		[XmlArray]
		public List<long> CompletedTutorials = new List<long>();

		[XmlArray]
		public List<long> CompletedScenarios = new List<long>();

		[XmlElement]
		public bool DisplayHelperHints = true;

		[XmlElement]
		public bool AutoExpandHelperHints = true;

		[XmlElement]
		public ControllerData VerticalMovementAxis;

		public ControllerData HorizontalMovementAxis;

		public ControllerData ForwardMovementAxis;

		public ControllerData VerticalLookAxis;

		public ControllerData HorizontalLookAxis;

		[XmlElement]
		public bool UseCustomWorkThreadsCount;

		[XmlElement]
		public int MinWorkerThreads = Environment.ProcessorCount;

		[XmlElement]
		public int MinCompletionPortThreads = Environment.ProcessorCount;

		[XmlElement]
		public int MaxWorkerThreads = (Environment.ProcessorCount + 2) * 10;

		[XmlElement]
		public int MaxCompletionPortThreads = (Environment.ProcessorCount + 2) * 5;

		[XmlElement]
		public int MaxConcurrentWorkers = Environment.ProcessorCount - 1;

		[XmlElement]
		public float CoroutineTimeBudget = 1f;

		[XmlElement]
		public bool SmoothTerrain;

		[XmlElement]
		public float SmoothTerrainAngle = 60f;

		[XmlElement]
		public int ConsoleBufferSize = 1024;

		[XmlElement]
		public bool LegacyCpu;

		[XmlIgnore]
		private static string _path;

		[XmlIgnore]
		public static string Path
		{
			get
			{
				if (string.IsNullOrEmpty(_path))
				{
					_path = System.IO.Path.Combine(StationSaveUtils.GetSavePath(), "setting.xml");
				}
				return _path;
			}
			set
			{
				_path = value;
			}
		}

		public void Save()
		{
			this.SaveXml(Path);
		}

		public static SettingData Load()
		{
			FileInfo fileInfo = new FileInfo(Path);
			ConsoleWindow.PrintAction("Loading settings: " + fileInfo.FullName);
			if (!fileInfo.Exists)
			{
				return null;
			}
			return XmlSerialization.Deserialize<SettingData>(Path);
		}
	}

	public static Settings Instance;

	public static SettingData CurrentData = new SettingData();

	private static readonly SettingData DefaultData = new SettingData();

	public Transform[] Pages;

	public Transform ControlPage;

	public UserInterfaceAnimated WarningSavePathNotWritable;

	private Transform _currentPage;

	public static bool OverallInitialized;

	public static AudioSource _SliderAudioSource;

	public static AudioClip clip;

	private static readonly List<string> BlindTypes = new List<string> { "None", "Protanopia", "Deuteranopia", "Tritanopia" };

	private static readonly List<string> ParticleGrade = new List<string> { "Low", "Medium", "High" };

	private static readonly List<string> VolumetricLightModes = new List<string> { "None", "Quarter", "Half", "Full" };

	private static readonly List<string> ShadowTypes = new List<string> { "Disabled", "Low", "Medium", "High", "Extreme" };

	private static readonly List<string> ShadowResolutions = new List<string> { "Low", "Medium", "High", "Very High" };

	private static readonly List<string> ShadowModes = new List<string> { "Low", "Medium", "High" };

	private static readonly List<string> TextureQualities = new List<string> { "Low", "Medium", "High", "Very High" };

	private static readonly List<string> AntialiasingTypes = new List<string> { "None", "SSAA", "DLAA", "FXAA", "TAA" };

	private static readonly List<string> FrameLock = new List<string> { "Off", "30", "50", "60", "75", "100", "120", "144", "165", "240" };

	private static readonly List<string> AmbientOcclusionTypes = new List<string> { "None", "Very Low", "Low", "Medium", "High", "Ultra" };

	private static readonly List<string> WeatherEventSettings = new List<string> { "Low", "Medium", "High" };

	private static readonly List<string> RenderDistanceTypes = new List<string> { "Lowest", "Low", "Medium", "High", "Extreme" };

	private static readonly List<string> TerrainTesselationTypes = new List<string> { "Off", "Low", "Medium", "High", "Extreme" };

	private static readonly List<string> TerrainRenderDistanceTypes = new List<string> { "Low", "Medium", "High" };

	private static readonly List<string> MinableRenderDistanceTypes = new List<string> { "Low", "Medium", "High" };

	public static float MinChannelVolume = -50f;

	private readonly List<Localization.LanguageFolder> _languageDropdown = new List<Localization.LanguageFolder>();

	private readonly List<LanguageCode> _voiceLanguageDropdown = new List<LanguageCode>();

	private static string[] resPresentation;

	private static Resolution[] availResolutions;

	private static int _languageDropdownValue;

	private static int _voiceLanguageDropdownValue;

	public ControlsAssignment ControlItemPrefab;

	public ControllerAxisItem ControlAxisItemPrefab;

	public ControlGroupItem ControlGroupPrefab;

	public LayoutGroup ControlGroup;

	public readonly Dictionary<int, string> audioDeviceHashToString = new Dictionary<int, string>();

	private static readonly int UiButtonPress02Hash = Animator.StringToHash("SFX_UI_ButtonPress02");

	public List<SettingItem> SettingItems = new List<SettingItem>();

	private static readonly Dictionary<SettingType, SettingItem> _settingItemLookup = new Dictionary<SettingType, SettingItem>();

	public GameObject UserSpeakerModeTF;

	private static readonly string _resetDefaultSettingsErrorText = "The game settings have changed since the last time you've played and encountered an issue. The game will now reset the settings to default to fix this issue.";

	public static bool InterfaceAudioOn
	{
		get
		{
			if ((float)CurrentData.MasterVolume > 0f)
			{
				return (float)CurrentData.InterfaceVolume > 0f;
			}
			return false;
		}
	}

	public static bool SoundOn
	{
		get
		{
			if ((float)CurrentData.MasterVolume > 0f)
			{
				return (float)CurrentData.SoundVolume > 0f;
			}
			return false;
		}
	}

	public static bool NotificationsOn
	{
		get
		{
			if ((float)CurrentData.MasterVolume > 0f)
			{
				return (float)CurrentData.VoiceNotificationVolume > 0f;
			}
			return false;
		}
	}

	public static System.Threading.ThreadPriority MainThreadPriority => System.Threading.ThreadPriority.AboveNormal;

	public static System.Threading.ThreadPriority FrameCriticalThreadPriority => System.Threading.ThreadPriority.Normal;

	public static System.Threading.ThreadPriority NonFrameCriticalThreadPriority
	{
		get
		{
			if (!CurrentData.LegacyCpu && !GameManager.IsBatchMode)
			{
				return System.Threading.ThreadPriority.BelowNormal;
			}
			return System.Threading.ThreadPriority.Normal;
		}
	}

	public void Initialize()
	{
		Instance = this;
		if (GameManager.IsBatchMode)
		{
			return;
		}
		SetVisible(isVisble: true);
		PopulateSettingItems();
		SetupValues();
		LayoutRebuilder.ForceRebuildLayoutImmediate(Instance.RectTransform);
		SetVisible(isVisble: false);
		CalculateDX9GraphicSettings();
		AudioConfiguration configuration = AudioSettings.GetConfiguration();
		if (configuration.speakerMode != AudioSettings.driverCapabilities)
		{
			configuration.speakerMode = AudioSettings.driverCapabilities;
			if (configuration.speakerMode == AudioSpeakerMode.Raw)
			{
				configuration.speakerMode = AudioSpeakerMode.Stereo;
			}
			UserSpeakerModeTF.GetComponent<TextMeshProUGUI>().SetText(Localization.GetInterface("SettingsUserSpeakerMode") + GetAudioDeviceName(configuration.speakerMode.GetHashCode()));
			AudioSettings.Reset(configuration);
		}
		if (GameManager.GameState == GameState.None && CurrentData.MusicVolume != 0 && CurrentData.MasterVolume != 0)
		{
			Singleton<GameManager>.Instance.MenuCutscene.gameObject.SetActive(value: true);
			UIAudioManager.PlayMainMenuMusic(2f);
			Singleton<GameManager>.Instance.MenuCutscene.GetComponent<MenuCutscene>().SetPosition();
		}
		InitVideoPageValues();
		InitAdvancedPageValues();
		ApplyVideoSettings();
	}

	public void Awake()
	{
		WorldManager.OnGameDataLoaded = (Action)Delegate.Combine(WorldManager.OnGameDataLoaded, new Action(OnInitComplete));
	}

	public override void OnEnable()
	{
		base.OnEnable();
		InitCurrentPage();
		RefreshPageLayouts();
	}

	public static void OnInitComplete()
	{
		if (ValidateSavePath())
		{
			Instance.ShowWarningSavePathNotWritable();
			GetInputField(SettingType.SavePath).text = CurrentData.SavePath;
		}
	}

	public void InitGameplayPageValues()
	{
		if (_settingItemLookup.Count != 0)
		{
			OverallInitialized = false;
			GetToggle(SettingType.ShowFps).isOn = CurrentData.ShowFps;
			GetToggle(SettingType.ShowLatency).isOn = CurrentData.ShowLatency;
			GetToggle(SettingType.AutoSave).isOn = CurrentData.AutoSave;
			GetSlider(SettingType.SaveDelay).value = CurrentData.SaveInterval;
			GetInputField(SettingType.MaxAutoSaves).text = StringManager.Get(CurrentData.MaxAutoSaves);
			GetInputField(SettingType.MaxQuickSaves).text = StringManager.Get(CurrentData.MaxQuickSaves);
			GetToggle(SettingType.HelmetOverlay).isOn = CurrentData.HelmetOverlay;
			WorldManager.Instance.UpdateFrameRate();
			GetSlider(SettingType.HUDScale).value = CurrentData.HUDScale;
			GetSlider(SettingType.TooltipOpacity).value = CurrentData.TooltipOpacity;
			PopulateLanguageDropdown();
			PopulateScreenModeDropdown();
			Slider slider = GetSlider(SettingType.DayLength);
			slider.value = CurrentData.DayLength;
			slider.interactable = GameManager.GameState == GameState.None;
			slider.GetComponentInChildren<TMP_InputField>().interactable = GameManager.GameState == GameState.None;
			GetToggle(SettingType.DisplayHelperHints).isOn = CurrentData.DisplayHelperHints;
			RefreshPageLayouts();
			OverallInitialized = true;
		}
	}

	private void RefreshResolutionAndRefreshRateDropdown()
	{
		List<string> list = availResolutions.Select((Resolution x) => $"{x.width} x {x.height}").Distinct().ToList();
		TMP_Dropdown dropdown = GetDropdown(SettingType.Resolution);
		dropdown.ClearOptions();
		dropdown.AddOptions(list);
		dropdown.value = list.IndexOf(CurrentData.ScreenWidth + " x " + CurrentData.ScreenHeight);
		List<int> list2 = availResolutions.Select((Resolution x) => x.refreshRate).Distinct().ToList();
		List<string> options = (from x in list2
			orderby x
			select $"{x}Hz").ToList();
		TMP_Dropdown dropdown2 = GetDropdown(SettingType.RefreshRate);
		dropdown2.ClearOptions();
		dropdown2.AddOptions(options);
		dropdown2.value = list2.IndexOf(CurrentData.RefreshRate);
	}

	public void InitVideoPageValues()
	{
		OverallInitialized = false;
		RefreshResolutionAndRefreshRateDropdown();
		GetDropdown(SettingType.OverallQuality).value = GetLevel(CurrentData.GraphicQuality);
		GetToggle(SettingType.VSync).isOn = CurrentData.Vsync;
		GetDropdown(SettingType.Shadow).value = ShadowTypes.IndexOf(CurrentData.Shadows);
		GetToggle(SettingType.DistantShadows).isOn = CurrentData.DistantShadows;
		GetDropdown(SettingType.RenderDistance).value = RenderDistanceTypes.IndexOf(CurrentData.RenderDistance);
		GetSlider(SettingType.Brightness).value = CurrentData.Brightness;
		GetSlider(SettingType.FieldOfView).value = CurrentData.FieldOfView;
		GetDropdown(SettingType.ParticleQuality).value = ParticleGrade.IndexOf(CurrentData.ParticleQuality);
		GetDropdown(SettingType.ScreenMode).value = ((!CurrentData.FullScreen) ? 1 : 0);
		GetDropdown(SettingType.TextureQuality).value = TextureQualities.IndexOf(CurrentData.TextureQuality);
		GetDropdown(SettingType.AntiAliasing).value = AntialiasingTypes.IndexOf(CurrentData.Antialiasing);
		GetDropdown(SettingType.AmbientOcclusion).value = AmbientOcclusionTypes.IndexOf(CurrentData.AmbientOcclusion);
		GetDropdown(SettingType.VolumeLight).value = VolumetricLightModes.IndexOf(CurrentData.VolumeLight);
		GetToggle(SettingType.LensFlares).isOn = CurrentData.LensFlares;
		GetDropdown(SettingType.TerrainDetail).value = TerrainTesselationTypes.IndexOf(CurrentData.TerrainDetail);
		GetDropdown(SettingType.MinableDistance).value = MinableRenderDistanceTypes.IndexOf(CurrentData.MinableDistance);
		GetDropdown(SettingType.TerrainDistance).value = TerrainRenderDistanceTypes.IndexOf(CurrentData.TerrainDistance);
		GetSlider(SettingType.PixelLightCount).value = CurrentData.PixelLightCount;
		GetDropdown(SettingType.RefreshRate).value = CurrentData.RefreshRate;
		CalculateDX9GraphicSettings();
		ApplyPixelLightCount();
		RefreshPageLayouts();
		OverallInitialized = true;
	}

	public void InitAdvancedPageValues()
	{
		OverallInitialized = false;
		GetDropdown(SettingType.OverallQuality).value = GetLevel(CurrentData.GraphicQuality);
		GetToggle(SettingType.VSync).isOn = CurrentData.Vsync;
		GetDropdown(SettingType.RenderDistance).value = RenderDistanceTypes.IndexOf(CurrentData.RenderDistance);
		GetDropdown(SettingType.TerrainDistance).value = TerrainRenderDistanceTypes.IndexOf(CurrentData.TerrainDistance);
		GetDropdown(SettingType.MinableDistance).value = MinableRenderDistanceTypes.IndexOf(CurrentData.MinableDistance);
		GetDropdown(SettingType.Shadow).value = ShadowTypes.IndexOf(CurrentData.Shadows);
		GetToggle(SettingType.DistantShadows).isOn = CurrentData.DistantShadows;
		GetSlider(SettingType.Brightness).value = CurrentData.Brightness;
		GetSlider(SettingType.FieldOfView).value = CurrentData.FieldOfView;
		GetDropdown(SettingType.ParticleQuality).value = ParticleGrade.IndexOf(CurrentData.ParticleQuality);
		GetDropdown(SettingType.Framelock).value = FrameLock.IndexOf(CurrentData.FrameLock);
		GetDropdown(SettingType.ScreenMode).value = ((!CurrentData.FullScreen) ? 1 : 0);
		GetDropdown(SettingType.TextureQuality).value = TextureQualities.IndexOf(CurrentData.TextureQuality);
		GetDropdown(SettingType.AntiAliasing).value = AntialiasingTypes.IndexOf(CurrentData.Antialiasing);
		GetDropdown(SettingType.AmbientOcclusion).value = AmbientOcclusionTypes.IndexOf(CurrentData.AmbientOcclusion);
		GetDropdown(SettingType.WeatherEventQuality).value = WeatherEventSettings.IndexOf(CurrentData.WeatherEventQuality);
		GetSlider(SettingType.PixelLightCount).value = CurrentData.PixelLightCount;
		GetDropdown(SettingType.RefreshRate).value = CurrentData.RefreshRate;
		GetToggle(SettingType.LensFlares).isOn = CurrentData.LensFlares;
		CalculateDX9GraphicSettings();
		ApplyPixelLightCount();
		OverallInitialized = true;
		RefreshPageLayouts();
	}

	private void RefreshPageLayouts()
	{
		foreach (Transform item in Pages.Where((Transform x) => (bool)x && x.gameObject.activeInHierarchy))
		{
			if (item.TryGetComponent<LayoutGroup>(out var component))
			{
				StartCoroutine(RefreshPanelAsync(component));
			}
		}
	}

	public static IEnumerator RefreshPanelAsync(LayoutGroup pageLayoutGroup)
	{
		yield return null;
		pageLayoutGroup.CalculateLayoutInputVertical();
		LayoutRebuilder.ForceRebuildLayoutImmediate(pageLayoutGroup.transform as RectTransform);
	}

	public static void ApplyPixelLightCount()
	{
		QualitySettings.pixelLightCount = CurrentData.PixelLightCount;
	}

	private static void CalculateDX9GraphicSettings()
	{
		if (GameManager.IsBatchMode)
		{
			return;
		}
		CameraController.SetAmbientOcclusion();
		if ((bool)RenderSettings.sun)
		{
			EasyFlares component = RenderSettings.sun.GetComponent<EasyFlares>();
			if ((bool)component && CursorManager.DefaultEasyFlareEnabled)
			{
				component.enabled = CurrentData.LensFlares;
			}
		}
	}

	public void InitAudioPageValues()
	{
		OverallInitialized = false;
		GetSlider(SettingType.MasterVolume).value = CurrentData.MasterVolume;
		GetSlider(SettingType.SoundVolume).value = CurrentData.SoundVolume;
		GetSlider(SettingType.VoiceNotificationVolume).value = CurrentData.VoiceNotificationVolume;
		GetSlider(SettingType.MusicVolume).value = CurrentData.MusicVolume;
		GetSlider(SettingType.InterfaceVolume).value = CurrentData.InterfaceVolume;
		PopulateVoiceLanguageDropdown();
		if (CurrentData.UserSpeakerMode == AudioSpeakerMode.Raw)
		{
			CurrentData.UserSpeakerMode = AudioSpeakerMode.Stereo;
		}
		UserSpeakerModeTF.GetComponent<TextMeshProUGUI>().SetText(Localization.GetInterface("SettingsUserSpeakerMode") + GetAudioDeviceName(CurrentData.UserSpeakerMode.GetHashCode()));
		if (!OverallInitialized)
		{
			OverallInitialized = true;
		}
	}

	public static void ApplyVolumeSettings()
	{
		Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("MasterVolume", GetScaledVolume(CurrentData.MasterVolume));
		Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("WorldVolume", GetScaledVolume((float)CurrentData.SoundVolume * ListenerEffectManager.WorldVolumeMultiplier * (float)((!WorldManager.IsGamePaused) ? 1 : 0)));
		Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("VoiceNotificationVolume", GetScaledVolume(CurrentData.VoiceNotificationVolume, 6f));
		Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("MusicVolume", GetScaledVolume(CurrentData.MusicVolume));
		Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("InterfaceVolume", GetScaledVolume(CurrentData.InterfaceVolume));
	}

	public static void ApplyVolumeSetting(SettingType settingType)
	{
		switch (settingType)
		{
		case SettingType.MasterVolume:
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("MasterVolume", GetScaledVolume(CurrentData.MasterVolume));
			break;
		case SettingType.SoundVolume:
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("WorldVolume", GetScaledVolume((float)CurrentData.SoundVolume * ListenerEffectManager.WorldVolumeMultiplier * (float)((!WorldManager.IsGamePaused) ? 1 : 0)));
			break;
		case SettingType.VoiceNotificationVolume:
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("VoiceNotificationVolume", GetScaledVolume(CurrentData.VoiceNotificationVolume, 6f));
			break;
		case SettingType.MusicVolume:
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("MusicVolume", GetScaledVolume(CurrentData.MusicVolume));
			break;
		case SettingType.InterfaceVolume:
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("InterfaceVolume", GetScaledVolume(CurrentData.InterfaceVolume));
			break;
		}
	}

	public static float GetScaledVolume(float volumeSetting, float maxValue = 0f)
	{
		if (!(volumeSetting < 1f))
		{
			return RocketMath.MapToScale(0f, 100f, MinChannelVolume, maxValue, volumeSetting);
		}
		return -100f;
	}

	public void InitControlsPageValues()
	{
		GetToggle(SettingType.InvertMouse).isOn = CurrentData.InvertMouse;
		GetToggle(SettingType.InvertMouseWheelInventory).isOn = CurrentData.InvertMouseWheelInventory;
		GetSlider(SettingType.CameraSensitivity).value = CurrentData.CameraSensitivity;
		RefreshPageLayouts();
	}

	public void InitMultiplayerPageValues()
	{
		OverallInitialized = false;
		GetInputField(SettingType.ServerName).text = CurrentData.ServerName;
		GetToggle(SettingType.StartLocalHost).isOn = CurrentData.StartLocalHost;
		GetToggle(SettingType.ServerVisible).isOn = CurrentData.ServerVisible;
		GetInputField(SettingType.ServerPassword).text = CurrentData.ServerPassword;
		GetInputField(SettingType.ServerAdminPassword).text = CurrentData.AdminPassword;
		GetSlider(SettingType.ServerMaxPlayers).value = CurrentData.ServerMaxPlayers;
		GetInputField(SettingType.ServerUpdatePort).text = CurrentData.UpdatePort;
		GetInputField(SettingType.ServerGamePort).text = CurrentData.GamePort;
		GetToggle(SettingType.UPNPEnabled).isOn = CurrentData.UPNPEnabled;
		GetSlider(SettingType.ServerMaxPlayers).interactable = GameManager.GameState == GameState.None;
		GetSlider(SettingType.ServerMaxPlayers).GetComponentInChildren<TMP_InputField>().interactable = GameManager.GameState == GameState.None;
		GetInputField(SettingType.ServerUpdatePort).interactable = GameManager.GameState == GameState.None;
		GetInputField(SettingType.ServerGamePort).interactable = GameManager.GameState == GameState.None;
		RefreshPageLayouts();
		OverallInitialized = true;
	}

	public void InitMiscPageValues()
	{
		OverallInitialized = false;
		PopulateLanguageDropdown();
		PopulateVoiceLanguageDropdown();
		GetToggle(SettingType.VoiceControl).isOn = CurrentData.Voice;
		GetToggle(SettingType.PopupChat).isOn = CurrentData.PopupChat;
		GetSlider(SettingType.CameraSensitivity).value = CurrentData.CameraSensitivity;
		GetToggle(SettingType.InvertMouse).isOn = CurrentData.InvertMouse;
		GetToggle(SettingType.InvertMouseWheelInventory).isOn = CurrentData.InvertMouseWheelInventory;
		GetToggle(SettingType.MenuLite).isOn = CurrentData.MenuLite;
		GetToggle(SettingType.MouseWheelZoom).isOn = CurrentData.MouseWheelZoom;
		GetToggle(SettingType.HelmetOverlay).isOn = CurrentData.HelmetOverlay;
		if (GetWindowVersion() < 10)
		{
			CurrentData.Voice = false;
		}
		RefreshPageLayouts();
		OverallInitialized = true;
	}

	public static void StoreAvailableResolutions()
	{
		Resolution[] resolutions = Screen.resolutions;
		Dictionary<string, Resolution> unique = new Dictionary<string, Resolution>();
		Resolution[] array = resolutions;
		for (int i = 0; i < array.Length; i++)
		{
			Resolution value = array[i];
			unique[value.ToString()] = value;
		}
		List<string> list = unique.Keys.ToList();
		list.Sort(delegate(string a, string b)
		{
			int num2 = unique[a].width.CompareTo(unique[b].width);
			return (num2 != 0) ? num2 : unique[a].height.CompareTo(unique[b].height);
		});
		availResolutions = new Resolution[list.Count];
		resPresentation = new string[list.Count];
		for (int num = 0; num < list.Count; num++)
		{
			resPresentation[num] = list[num];
			availResolutions[num] = unique[list[num]];
		}
	}

	public static int GetWindowVersion()
	{
		int result = 0;
		string[] array = SystemInfo.operatingSystem.Split(' ');
		if (array[0].ToLower().Equals("windows"))
		{
			int.TryParse(array[1], out result);
		}
		return result;
	}

	public static bool GetIsAMDGPU()
	{
		return SystemInfo.graphicsDeviceVendorID == 1002;
	}

	public static int GetParticleGrade()
	{
		return ParticleGrade.IndexOf(CurrentData.ParticleQuality);
	}

	private static void PopulateScreenModeDropdown()
	{
		if ((object)Instance != null)
		{
			GetDropdown(SettingType.ScreenMode).ClearOptions();
			List<TMP_Dropdown.OptionData> list = new List<TMP_Dropdown.OptionData>();
			list.Add(new TMP_Dropdown.OptionData(GameStrings.SettingsFullScreenDisplayMode.DisplayString));
			list.Add(new TMP_Dropdown.OptionData(GameStrings.SettingsWindowDisplayMode.DisplayString));
			GetDropdown(SettingType.ScreenMode).AddOptions(list);
		}
	}

	private static void PopulateLanguageDropdown()
	{
		if ((object)Instance == null)
		{
			return;
		}
		GetDropdown(SettingType.Language).ClearOptions();
		List<TMP_Dropdown.OptionData> list = new List<TMP_Dropdown.OptionData>();
		Instance._languageDropdown.Clear();
		foreach (KeyValuePair<LanguageCode, Localization.LanguageFolder> languageDatum in Localization.LanguageData)
		{
			list.Add(new TMP_Dropdown.OptionData(languageDatum.Value.Name));
			Instance._languageDropdown.Add(languageDatum.Value);
		}
		GetDropdown(SettingType.Language).AddOptions(list);
		SetLanguageDropdown();
	}

	public static void SetLanguageDropdown()
	{
		if (!(Instance == null))
		{
			int num = Instance._languageDropdown.FindIndex((Localization.LanguageFolder l) => l.Code == CurrentData.LanguageCode);
			if (_languageDropdownValue != num)
			{
				GetDropdown(SettingType.Language).value = num;
			}
		}
	}

	private static void PopulateVoiceLanguageDropdown()
	{
		if ((object)Instance != null)
		{
			GetDropdown(SettingType.VoiceLanguage).ClearOptions();
			List<TMP_Dropdown.OptionData> list = new List<TMP_Dropdown.OptionData>();
			Instance._voiceLanguageDropdown.Clear();
			list.Add(new TMP_Dropdown.OptionData("English"));
			Instance._voiceLanguageDropdown.Add(LanguageCode.EN);
			list.Add(new TMP_Dropdown.OptionData("German"));
			Instance._voiceLanguageDropdown.Add(LanguageCode.DE);
			list.Add(new TMP_Dropdown.OptionData("Russian"));
			Instance._voiceLanguageDropdown.Add(LanguageCode.RU);
			list.Add(new TMP_Dropdown.OptionData("Chinese"));
			Instance._voiceLanguageDropdown.Add(LanguageCode.ZH);
			GetDropdown(SettingType.VoiceLanguage).AddOptions(list);
			SetVoiceLanguageDropdown();
		}
	}

	public static void SetVoiceLanguageDropdown()
	{
		if (!(Instance == null))
		{
			int num = Instance._voiceLanguageDropdown.FindIndex((LanguageCode l) => l == CurrentData.VoiceLanguageCode);
			if (_voiceLanguageDropdownValue != num)
			{
				GetDropdown(SettingType.VoiceLanguage).value = num;
			}
		}
	}

	private static int GetLevel(string type)
	{
		for (int i = 0; i < QualitySettings.names.Length; i++)
		{
			if (QualitySettings.names[i].ToLower().Equals(type.ToLower()))
			{
				return i;
			}
		}
		return 0;
	}

	public void CloseSetting()
	{
		CancelSetting();
		if (InventoryManager.Instance.GameMenuPanel.activeInHierarchy)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		base.gameObject.SetActive(value: false);
		InventoryManager.Instance.MainMenuPanel.SetActive(value: true);
	}

	public void CancelSetting()
	{
		SaveSettings();
		WorldManager.Instance.UpdateFrameRate();
	}

	public void PromptForRestore()
	{
		PromptPanel.Instance.ShowPrompt(PromptResetDefaultsStrings.Title, PromptResetDefaultsStrings.Body, PromptResetDefaultsStrings.Button, delegate
		{
			RestoreSetting();
		});
	}

	public void PromptForResetWindows()
	{
		PromptPanel.Instance.ShowPrompt(Localization.GetInterface("ResetInventoryTitle"), Localization.GetInterface("ResetInventoryDescription"), Localization.GetInterface("ConfirmReset"), delegate
		{
			ResetWindows();
		});
	}

	private void ResetWindows()
	{
		if (!InventoryWindowManager.Instance)
		{
			return;
		}
		foreach (InventoryWindow window in InventoryWindowManager.Instance.Windows)
		{
			if (window.IsUndocked)
			{
				window.ButtonDockClicked();
			}
		}
		Stationpedia.ResetWindow();
	}

	public void PromptFolderBrowser()
	{
		if (CurrentData.SavePath == string.Empty)
		{
			string text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/My Games/Stationeers";
			text = text.Replace("\\", "/");
			text = text.Replace("//", "/");
			text = text.TrimEnd('/');
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			CurrentData.SavePath = text;
		}
		string[] array = StandaloneFileBrowser.OpenFolderPanel("Select save path", CurrentData.SavePath, multiselect: false);
		if (array.Length == 1)
		{
			SettingData currentData = CurrentData;
			string savePath = (GetInputField(SettingType.SavePath).text = array[0]);
			currentData.SavePath = savePath;
			if (ValidateSavePath())
			{
				GetInputField(SettingType.SavePath).text = CurrentData.SavePath;
				ShowWarningSavePathNotWritable();
			}
		}
	}

	public void InitCurrentPage()
	{
		foreach (Transform item in Pages.Where((Transform x) => x))
		{
			if (item.gameObject.activeSelf)
			{
				switch (item.name)
				{
				case "Gameplay":
					InitGameplayPageValues();
					break;
				case "Video":
					InitVideoPageValues();
					break;
				case "Advanced":
					InitAdvancedPageValues();
					break;
				case "Audio":
					InitAudioPageValues();
					break;
				case "Controls":
					InitControlsPageValues();
					break;
				case "Multiplayer":
					InitMultiplayerPageValues();
					break;
				case "Misc":
					InitMiscPageValues();
					break;
				}
			}
		}
	}

	public void RestoreSetting()
	{
		Transform[] pages = Pages;
		foreach (Transform transform in pages)
		{
			if (transform.gameObject.activeSelf)
			{
				if (transform.name.Equals("Gameplay"))
				{
					CurrentData.ShowFps = DefaultData.ShowFps;
					CurrentData.ShowLatency = DefaultData.ShowLatency;
					CurrentData.AutoSave = DefaultData.AutoSave;
					CurrentData.MaxAutoSaves = DefaultData.MaxAutoSaves;
					CurrentData.MaxQuickSaves = DefaultData.MaxQuickSaves;
					CurrentData.SaveInterval = DefaultData.SaveInterval;
					CurrentData.SavePath = DefaultData.SavePath;
					CurrentData.HUDScale = DefaultData.HUDScale;
					CurrentData.TooltipOpacity = DefaultData.TooltipOpacity;
					CurrentData.ChatFadeTimer = DefaultData.ChatFadeTimer;
					CurrentData.IngamePortrait = DefaultData.IngamePortrait;
					CurrentData.DayLength = DefaultData.DayLength;
					CurrentData.LegacyInventory = DefaultData.LegacyInventory;
					CurrentData.ShowSlotToolTips = DefaultData.ShowSlotToolTips;
					InitGameplayPageValues();
				}
				else if (transform.name.Equals("Video"))
				{
					CurrentData.ScreenHeight = DefaultData.ScreenHeight;
					CurrentData.ScreenWidth = DefaultData.ScreenWidth;
					CurrentData.RefreshRate = DefaultData.RefreshRate;
					CurrentData.FullScreen = DefaultData.FullScreen;
					CurrentData.Vsync = DefaultData.Vsync;
					CurrentData.Brightness = DefaultData.Brightness;
					CurrentData.FieldOfView = DefaultData.FieldOfView;
					CurrentData.GraphicQuality = DefaultData.GraphicQuality;
					InitVideoPageValues();
				}
				else if (transform.name.Equals("Advanced"))
				{
					CurrentData.Antialiasing = DefaultData.Antialiasing;
					CurrentData.TextureQuality = DefaultData.TextureQuality;
					CurrentData.AmbientOcclusion = DefaultData.AmbientOcclusion;
					CurrentData.ParticleQuality = DefaultData.ParticleQuality;
					CurrentData.SoftParticles = DefaultData.SoftParticles;
					CurrentData.FrameLock = DefaultData.FrameLock;
					CurrentData.VolumeLight = DefaultData.VolumeLight;
					CurrentData.PixelLightCount = DefaultData.PixelLightCount;
					CurrentData.Shadows = DefaultData.Shadows;
					CurrentData.DistantShadows = DefaultData.DistantShadows;
					CurrentData.ShadowResolution = DefaultData.ShadowResolution;
					CurrentData.ShadowDistance = DefaultData.ShadowDistance;
					CurrentData.TerrainDetail = DefaultData.TerrainDetail;
					CurrentData.RenderDistance = DefaultData.RenderDistance;
					CurrentData.EnvironmentElements = DefaultData.EnvironmentElements;
					CurrentData.ExtendedTerrain = DefaultData.ExtendedTerrain;
					CurrentData.AtmosphericScattering = DefaultData.AtmosphericScattering;
					CurrentData.LensFlares = DefaultData.LensFlares;
					CurrentData.RoomControlTickSpeed = DefaultData.RoomControlTickSpeed;
					CurrentData.WorldOrigin = DefaultData.WorldOrigin;
					CurrentData.SmoothTerrain = DefaultData.SmoothTerrain;
					CurrentData.HelmetOverlay = DefaultData.HelmetOverlay;
					CurrentData.WeatherEventQuality = DefaultData.WeatherEventQuality;
					InitAdvancedPageValues();
				}
				else if (transform.name.Equals("Audio"))
				{
					CurrentData.MasterVolume = DefaultData.MasterVolume;
					CurrentData.SoundVolume = DefaultData.SoundVolume;
					CurrentData.VoiceNotificationVolume = DefaultData.VoiceNotificationVolume;
					CurrentData.MusicVolume = DefaultData.MusicVolume;
					CurrentData.InterfaceVolume = DefaultData.InterfaceVolume;
					InitAudioPageValues();
				}
				else if (transform.name.Equals("Controls"))
				{
					CurrentData.InvertMouse = DefaultData.InvertMouse;
					CurrentData.InvertMouseWheelInventory = DefaultData.InvertMouseWheelInventory;
					CurrentData.MouseWheelZoom = DefaultData.MouseWheelZoom;
					CurrentData.MenuLite = DefaultData.MenuLite;
					CurrentData.CameraSensitivity = DefaultData.CameraSensitivity;
					ResetKeyMappings();
					InitControlsPageValues();
				}
				else if (transform.name.Equals("Multiplayer"))
				{
					CurrentData.ServerName = DefaultData.ServerName;
					CurrentData.ServerVisible = DefaultData.ServerVisible;
					CurrentData.StartLocalHost = DefaultData.StartLocalHost;
					CurrentData.ServerPassword = DefaultData.ServerPassword;
					CurrentData.AdminPassword = DefaultData.AdminPassword;
					CurrentData.ServerMaxPlayers = DefaultData.ServerMaxPlayers;
					CurrentData.GamePort = DefaultData.GamePort;
					CurrentData.UpdatePort = DefaultData.UpdatePort;
					CurrentData.UPNPEnabled = DefaultData.UPNPEnabled;
					CurrentData.UseSteamP2P = DefaultData.UseSteamP2P;
					InitMultiplayerPageValues();
				}
				else if (transform.name.Equals("Misc"))
				{
					CurrentData.LanguageCode = DefaultData.LanguageCode;
					CurrentData.Voice = DefaultData.Voice;
					CurrentData.PopupChat = DefaultData.PopupChat;
					CurrentData.CameraSensitivity = DefaultData.CameraSensitivity;
					InitMiscPageValues();
				}
			}
		}
	}

	private void SetupValues()
	{
		string[] array = new string[Display.displays.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (i + 1).ToString();
		}
		if (!GameManager.IsBatchMode)
		{
			StoreAvailableResolutions();
			RefreshResolutionAndRefreshRateDropdown();
			GetDropdown(SettingType.OverallQuality).ClearOptions();
			GetDropdown(SettingType.OverallQuality).AddOptions(QualitySettings.names.ToList());
			GetDropdown(SettingType.AntiAliasing).ClearOptions();
			GetDropdown(SettingType.AntiAliasing).AddOptions(AntialiasingTypes);
			GetDropdown(SettingType.AmbientOcclusion).ClearOptions();
			GetDropdown(SettingType.AmbientOcclusion).AddOptions(AmbientOcclusionTypes);
			GetDropdown(SettingType.WeatherEventQuality).ClearOptions();
			GetDropdown(SettingType.WeatherEventQuality).AddOptions(WeatherEventSettings);
			GetSlider(SettingType.MasterVolume).value = CurrentData.MasterVolume;
			GetSlider(SettingType.SoundVolume).value = CurrentData.SoundVolume;
			GetSlider(SettingType.VoiceNotificationVolume).value = CurrentData.VoiceNotificationVolume;
			GetSlider(SettingType.MusicVolume).value = CurrentData.MusicVolume;
			GetSlider(SettingType.InterfaceVolume).value = CurrentData.InterfaceVolume;
			base.gameObject.SetActive(value: true);
			foreach (ControlsGroup allControlGroup in ControlsGroup.AllControlGroups)
			{
				ControlGroupItem controlGroupItem = UnityEngine.Object.Instantiate(ControlGroupPrefab, ControlGroup.transform);
				allControlGroup.Transform = controlGroupItem.Transform;
				controlGroupItem.Title.text = allControlGroup.Name;
				controlGroupItem.name = "ControlGroup" + allControlGroup.Name;
			}
			ControlsGroup controlsGroup = KeyManager.GetControlsGroup("Movement");
			ControlsGroup controlsGroup2 = KeyManager.GetControlsGroup("Camera");
			UnityEngine.Object.Instantiate(ControlAxisItemPrefab, controlsGroup.Transform).Created("Strafe", ControllerMap.HorizontalMovement);
			UnityEngine.Object.Instantiate(ControlAxisItemPrefab, controlsGroup.Transform).Created("Turn", ControllerMap.HorizontalLook);
			UnityEngine.Object.Instantiate(ControlAxisItemPrefab, controlsGroup.Transform).Created("Forward", ControllerMap.ForwardMovement);
			UnityEngine.Object.Instantiate(ControlAxisItemPrefab, controlsGroup.Transform).Created("Vertical", ControllerMap.VerticalMovement);
			UnityEngine.Object.Instantiate(ControlAxisItemPrefab, controlsGroup2.Transform).Created("Vertical", ControllerMap.VerticalLook);
			for (int j = 0; j < KeyManager.AllKeys.Count; j++)
			{
				KeyItem keyItem = KeyManager.AllKeys[j];
				ControlsGroup controlsGroup3 = KeyManager.GetControlsGroup(keyItem.Name);
				ControlsAssignment controlsAssignment = UnityEngine.Object.Instantiate(ControlItemPrefab, controlsGroup3.Transform);
				controlsAssignment.Created(keyItem, j);
				controlsAssignment.name = "Control" + keyItem.Name;
				controlsAssignment.SetVisible(!keyItem.Hidden);
			}
			ControlsAssignment.RefreshState();
			LayoutRebuilder.ForceRebuildLayoutImmediate(ControlGroup.GetComponent<RectTransform>());
			base.gameObject.SetActive(value: false);
			Singleton<GameManager>.Instance.MenuCutscene.MenuLite(CurrentData.MenuLite);
		}
		audioDeviceHashToString.Add(AudioSpeakerMode.Mode5point1.GetHashCode(), "5.1 Surround");
		audioDeviceHashToString.Add(AudioSpeakerMode.Stereo.GetHashCode(), "Stereo");
		audioDeviceHashToString.Add(AudioSpeakerMode.Mono.GetHashCode(), "Mono");
		audioDeviceHashToString.Add(AudioSpeakerMode.Mode7point1.GetHashCode(), "7.1 Surround");
		audioDeviceHashToString.Add(AudioSpeakerMode.Quad.GetHashCode(), "Quad");
		audioDeviceHashToString.Add(AudioSpeakerMode.Surround.GetHashCode(), "Surround");
		audioDeviceHashToString.Add(AudioSpeakerMode.Prologic.GetHashCode(), "ProLogic");
	}

	public static void SaveSettings()
	{
		CurrentData.SettingsVersion = GameManager.GetGameVersion();
		CurrentData.VerticalLookAxis = ControllerMap.VerticalLook.Serialize();
		CurrentData.VerticalMovementAxis = ControllerMap.VerticalMovement.Serialize();
		CurrentData.HorizontalLookAxis = ControllerMap.HorizontalLook.Serialize();
		CurrentData.HorizontalMovementAxis = ControllerMap.HorizontalMovement.Serialize();
		CurrentData.ForwardMovementAxis = ControllerMap.ForwardMovement.Serialize();
		CurrentData.KeyList = KeyManager.AllKeys;
		ApplyPixelLightCount();
		if (StatusUpdates.Save(out var data))
		{
			CurrentData.VoiceNotifications = data;
		}
		if (CurrentData.UserSpeakerMode == AudioSpeakerMode.Raw)
		{
			CurrentData.UserSpeakerMode = AudioSpeakerMode.Stereo;
		}
		CurrentData.Save();
	}

	public void ApplyDisplaySettings()
	{
		string[] array = GetDropdown(SettingType.Resolution).captionText.text.Split('x');
		CurrentData.ScreenWidth = array[0].Trim();
		CurrentData.ScreenHeight = array[1].Trim();
		string s = GetDropdown(SettingType.RefreshRate).captionText.text.ToLower().Replace("hz", string.Empty).Trim();
		CurrentData.RefreshRate = int.Parse(s);
		CurrentData.FullScreen = GetDropdown(SettingType.ScreenMode).value == 0;
		CurrentData.Vsync = GetToggle(SettingType.VSync).isOn;
		UIAudioManager.Play(UiButtonPress02Hash);
		QualitySettings.vSyncCount = (CurrentData.Vsync ? 1 : 0);
		Screen.SetResolution(int.Parse(CurrentData.ScreenWidth), int.Parse(CurrentData.ScreenHeight), CurrentData.FullScreen, CurrentData.RefreshRate);
	}

	public static void ResetKeyMappings()
	{
		KeyManager.AssignDefaultKeys();
	}

	public string GetAudioDeviceName(int hash)
	{
		audioDeviceHashToString.TryGetValue(hash, out var value);
		return value;
	}

	public static void OnVersionChange(int revision)
	{
	}

	public static void LoadSettings()
	{
		KeyManager.SetupKeyBindings();
		SettingData settingData = SettingData.Load();
		if (settingData != null)
		{
			CurrentData = settingData;
			if (settingData.SettingsVersion != GameManager.GetGameVersion())
			{
				OnVersionChange(XmlSaveLoad.GetRevisionNumber(settingData.SettingsVersion));
			}
			if (int.TryParse(CurrentData.ScreenWidth, out var result) && int.TryParse(CurrentData.ScreenHeight, out var result2))
			{
				Screen.SetResolution(result, result2, CurrentData.FullScreen, CurrentData.RefreshRate);
			}
			else
			{
				ConsoleWindow.Print("Cannot parse resolution leaving at default");
			}
		}
		for (int i = 0; i < CurrentData.KeyList.Count; i++)
		{
			KeyItem keyItem = CurrentData.KeyList[i];
			KeyItem keyitem = KeyManager.GetKeyitem(keyItem.Name);
			if (keyitem != null && !(keyitem.Name == "ShowControls"))
			{
				keyitem.Key = keyItem.Key;
				CurrentData.KeyList[i] = keyitem;
			}
		}
		KeyManager.LoadControllers();
		KeyManager.LoadKeyboardSetting();
		StatusUpdates.Load(CurrentData.VoiceNotifications);
		Localization.SetLanguage(CurrentData.LanguageCode);
		CalculateDX9GraphicSettings();
		CursorManager.UpdateAtmosphericScattering();
	}

	public void ApplyVideoSettings()
	{
		QualitySettings.SetQualityLevel(GetLevel(CurrentData.GraphicQuality));
		QualitySettings.vSyncCount = (CurrentData.Vsync ? 1 : 0);
		QualitySettings.softParticles = CurrentData.SoftParticles;
		QualitySettings.pixelLightCount = CurrentData.PixelLightCount;
		CameraController.SetAntialiasing();
		CameraController.SetAmbientOcclusion();
		ShadowQualitySetting.GetSetting(CurrentData.Shadows).Apply();
		ShadowQualitySetting.Current.Update(OrbitalSimulation.WorldSunVector);
		WorldManager.Instance.UpdateBrightness();
		WorldManager.Instance.UpdateFoV();
		WorldManager.Instance.UpdateColorBlind();
		WorldManager.Instance.UpdateParticleQuality();
		WorldManager.Instance.UpdateFrameLimiter();
		WorldManager.Instance.UpdateVolumeLight();
		OcclusionManager.Instance.UpdateRenderDistanceMultiplier();
		MinableDrawCallCollection.RefreshMinableRenderDistance();
		LodManager.UpdateRenderDistance();
		VoxelTerrain.UpdateRenderDistance().Forget();
		Screen.SetResolution(int.Parse(CurrentData.ScreenWidth), int.Parse(CurrentData.ScreenHeight), CurrentData.FullScreen, CurrentData.RefreshRate);
	}

	public void OnUpdateOverall()
	{
		if (!OverallInitialized)
		{
			OverallInitialized = true;
			return;
		}
		CurrentData.PixelLightCount = QualitySettings.pixelLightCount;
		CurrentData.TextureQuality = TextureQualities[3 - QualitySettings.masterTextureLimit];
		CurrentData.SoftParticles = QualitySettings.softParticles;
		CurrentData.Shadows = ShadowQualitySetting.GetSettingFromOverall(CurrentData.GraphicQuality).GetName;
		CurrentData.TerrainDetail = GetTerrainDetailFromOverall(CurrentData.GraphicQuality);
		CurrentData.RenderDistance = GetRenderDistanceFromOverAll(CurrentData.GraphicQuality);
		CurrentData.MinableDistance = GetMinableDistanceFromOverAll(CurrentData.GraphicQuality);
		CurrentData.TerrainDistance = GetTerrainDetailFromOverall(CurrentData.GraphicQuality);
		InitVideoPageValues();
		InitAdvancedPageValues();
	}

	private string GetTerrainDetailFromOverall(string overAll)
	{
		switch (overAll)
		{
		case "Fastest":
		case "Fast":
		case "Simple":
			return "Low";
		case "Good":
			return "Medium";
		case "Beautiful":
			return "Medium";
		case "Fantastic":
			return "High";
		default:
			return "High";
		}
	}

	private string GetRenderDistanceFromOverAll(string overAll)
	{
		return overAll switch
		{
			"Fastest" => "Lowest", 
			"Fast" => "Low", 
			"Simple" => "Medium", 
			"Good" => "Medium", 
			"Beautiful" => "High", 
			"Fantastic" => "High", 
			_ => "High", 
		};
	}

	private string GetMinableDistanceFromOverAll(string overAll)
	{
		return overAll switch
		{
			"Fastest" => "Low", 
			"Fast" => "Low", 
			"Simple" => "Low", 
			"Good" => "Low", 
			"Beautiful" => "Medium", 
			"Fantastic" => "Medium", 
			_ => "High", 
		};
	}

	private string GetTerrainDistanceFromOverAll(string overAll)
	{
		return overAll switch
		{
			"Fastest" => "Low", 
			"Fast" => "Low", 
			"Simple" => "Low", 
			"Good" => "Low", 
			"Beautiful" => "Medium", 
			"Fantastic" => "Medium", 
			_ => "High", 
		};
	}

	private void PopulateSettingItems()
	{
		_settingItemLookup.Clear();
		Transform[] pages = Pages;
		for (int i = 0; i < pages.Length; i++)
		{
			SettingItem[] componentsInChildren = pages[i].GetComponentsInChildren<SettingItem>(includeInactive: true);
			foreach (SettingItem item in componentsInChildren)
			{
				if (!SettingItems.Contains(item))
				{
					SettingItems.Add(item);
				}
			}
		}
		foreach (SettingItem settingItem in SettingItems)
		{
			if ((bool)settingItem && !_settingItemLookup.ContainsKey(settingItem.SettingType))
			{
				settingItem.Setup();
				_settingItemLookup.Add(settingItem.SettingType, settingItem);
			}
		}
		InitGameplayPageValues();
		InitMultiplayerPageValues();
	}

	public static Slider GetSlider(SettingType settingType)
	{
		return GetSettingItem<Slider>(settingType);
	}

	private static TMP_Dropdown GetDropdown(SettingType settingType)
	{
		return GetSettingItem<TMP_Dropdown>(settingType);
	}

	private static Toggle GetToggle(SettingType settingType)
	{
		return GetSettingItem<Toggle>(settingType);
	}

	private static TMP_InputField GetInputField(SettingType settingType)
	{
		return GetSettingItem<TMP_InputField>(settingType);
	}

	private static T GetSettingItem<T>(SettingType settingType) where T : Selectable
	{
		if (!_settingItemLookup.TryGetValue(settingType, out var value))
		{
			Debug.LogWarning($"Setting Item not found '{settingType}', keys: {_settingItemLookup.Keys.ToArrayString()}");
			return null;
		}
		if (!(value.Selectable is T result))
		{
			Debug.LogError($"Can not cast type {value.Selectable.GetType()} to {typeof(T)}");
			return null;
		}
		return result;
	}

	private static void EnableSettingUI(SettingType settingType, bool enable)
	{
		GetToggle(settingType).enabled = enable;
	}

	public static string RemoveInvalidFileChars(string path)
	{
		return path;
	}

	public static string CheckFolderStructureCorrect(string path)
	{
		string empty = string.Empty;
		string empty2 = string.Empty;
		string text = GetInputField(SettingType.SavePath).text;
		while (text.Contains("\\\\"))
		{
			text = text.Replace("\\\\", "\\");
		}
		if (!text.Contains("\\"))
		{
			return string.Empty;
		}
		string[] array = text.Split('\\');
		if (array.Length == 0)
		{
			return string.Empty;
		}
		empty2 = array[0];
		if (array.Length > 1)
		{
			for (int i = 1; i < array.Length; i++)
			{
				empty = RemoveInvalidFileChars(array[i]);
				empty2 = empty2 + "\\" + empty;
			}
		}
		return empty2;
	}

	public static bool ValidateSavePath()
	{
		string savePath = CurrentData.SavePath;
		if (string.IsNullOrEmpty(savePath))
		{
			return false;
		}
		savePath = savePath.SanitizePath();
		try
		{
			string path = $"{savePath}/{Guid.NewGuid()}.tmp";
			File.Create(path).Dispose();
			File.Delete(path);
		}
		catch
		{
			CurrentData.SavePath = string.Empty;
			StationSaveUtils.GetSavePath();
			SaveSettings();
			return true;
		}
		return false;
	}

	public void ShowWarningSavePathNotWritable()
	{
		Localization.Variable1 = CurrentData.SavePath;
		WarningSavePathNotWritable.Show();
	}

	public static void OnValueChanged(SettingType settingType)
	{
		try
		{
			switch (settingType)
			{
			case SettingType.AutoSave:
				CurrentData.AutoSave = GetToggle(settingType).isOn;
				StationAutoSave.ResetAutoSave();
				break;
			case SettingType.SaveDelay:
				CurrentData.SaveInterval = (int)GetSlider(settingType).value;
				StationAutoSave.ResetAutoSave();
				break;
			case SettingType.MaxAutoSaves:
			{
				int result = DefaultData.MaxAutoSaves;
				int.TryParse(GetInputField(settingType).text, out result);
				result = Mathf.Clamp(result, 1, 20);
				GetInputField(settingType).text = StringManager.Get(result);
				CurrentData.MaxAutoSaves = result;
				break;
			}
			case SettingType.MaxQuickSaves:
			{
				int result2 = DefaultData.MaxQuickSaves;
				int.TryParse(GetInputField(settingType).text, out result2);
				result2 = Mathf.Clamp(result2, 1, 20);
				GetInputField(settingType).text = StringManager.Get(result2);
				CurrentData.MaxQuickSaves = result2;
				break;
			}
			case SettingType.SavePath:
				if (CurrentData.SavePath != GetInputField(SettingType.SavePath).text && GetInputField(SettingType.SavePath).text != string.Empty)
				{
					string text = CheckFolderStructureCorrect(GetInputField(SettingType.SavePath).text);
					if (text != string.Empty)
					{
						CurrentData.SavePath = text;
						if (ValidateSavePath())
						{
							Instance.ShowWarningSavePathNotWritable();
						}
						GetInputField(SettingType.SavePath).text = CurrentData.SavePath;
						if (GameManager.GameState == GameState.Running && CurrentData.AutoSave)
						{
							StationAutoSave.ResetAutoSave();
						}
					}
					else
					{
						GetInputField(SettingType.SavePath).text = CurrentData.SavePath;
					}
				}
				else
				{
					GetInputField(SettingType.SavePath).text = CurrentData.SavePath;
				}
				break;
			case SettingType.ShowFps:
				CurrentData.ShowFps = GetToggle(settingType).isOn;
				WorldManager.Instance.UpdateFrameRate();
				break;
			case SettingType.ShowLatency:
				CurrentData.ShowLatency = GetToggle(settingType).isOn;
				break;
			case SettingType.ShowExtendedTooltips:
				CurrentData.ExtendedTooltips = GetToggle(settingType).isOn;
				break;
			case SettingType.HUDScale:
				CurrentData.HUDScale = (int)GetSlider(settingType).value;
				WorldManager.Instance.UpdateHUDScale();
				break;
			case SettingType.TooltipOpacity:
				GetSlider(settingType).value = Mathf.Round(GetSlider(settingType).value * 100f) / 100f;
				CurrentData.TooltipOpacity = GetSlider(settingType).value;
				WorldManager.Instance.Tooltip.UiComponentRenderer.RefreshVisible(forceRefresh: true);
				break;
			case SettingType.ChatFadeTimer:
			{
				Slider slider3 = GetSlider(settingType);
				CurrentData.ChatFadeTimer = slider3.value;
				break;
			}
			case SettingType.DayLength:
			{
				Slider slider = GetSlider(settingType);
				slider.value = Mathf.Round(slider.value);
				CurrentData.DayLength = (int)slider.value;
				break;
			}
			case SettingType.RoomControlTickSpeed:
			{
				Slider slider2 = GetSlider(settingType);
				slider2.value = Mathf.Round(slider2.value);
				CurrentData.RoomControlTickSpeed = (int)slider2.value;
				RoomManager.Instance.TickSpeed = CurrentData.RoomControlTickSpeed;
				break;
			}
			case SettingType.WorldOrigin:
				CurrentData.WorldOrigin = GetToggle(settingType).isOn;
				break;
			case SettingType.SmoothTerrain:
				CurrentData.SmoothTerrain = GetToggle(settingType).isOn;
				break;
			case SettingType.SmoothTerrainAngle:
				CurrentData.SmoothTerrainAngle = GetSlider(settingType).value;
				break;
			case SettingType.OverallQuality:
				CurrentData.GraphicQuality = GetDropdown(settingType).captionText.text;
				QualitySettings.SetQualityLevel(GetLevel(CurrentData.GraphicQuality));
				Instance.OnUpdateOverall();
				break;
			case SettingType.AntiAliasing:
				CurrentData.Antialiasing = GetDropdown(settingType).captionText.text;
				CameraController.SetAntialiasing();
				break;
			case SettingType.TextureQuality:
			{
				TMP_Dropdown dropdown = GetDropdown(settingType);
				CurrentData.TextureQuality = TextureQualities[dropdown.value];
				QualitySettings.globalTextureMipmapLimit = 3 - dropdown.value;
				break;
			}
			case SettingType.Shadow:
				CurrentData.Shadows = ShadowTypes[GetDropdown(settingType).value];
				ShadowQualitySetting.GetSetting(CurrentData.Shadows).Apply();
				ShadowQualitySetting.Current.Update(OrbitalSimulation.WorldSunVector);
				break;
			case SettingType.DistantShadows:
				CurrentData.DistantShadows = GetToggle(settingType).isOn;
				ShadowQualitySetting.GetSetting(CurrentData.Shadows).Apply();
				ShadowQualitySetting.Current.Update(OrbitalSimulation.WorldSunVector);
				break;
			case SettingType.ShadowResolution:
				QualitySettings.shadowResolution = (ShadowResolution)ShadowResolutions.IndexOf(CurrentData.ShadowResolution);
				break;
			case SettingType.ShadowDistance:
				QualitySettings.shadowDistance = CurrentData.ShadowDistance;
				break;
			case SettingType.VSync:
				CurrentData.Vsync = GetToggle(settingType).isOn;
				QualitySettings.vSyncCount = (CurrentData.Vsync ? 1 : 0);
				break;
			case SettingType.Brightness:
				CurrentData.Brightness = (int)GetSlider(settingType).value;
				WorldManager.Instance.UpdateBrightness();
				break;
			case SettingType.FieldOfView:
				CurrentData.FieldOfView = (int)GetSlider(settingType).value;
				WorldManager.Instance.UpdateFoV();
				break;
			case SettingType.ColorBlind:
				CurrentData.ColorBlind = BlindTypes[GetDropdown(settingType).value];
				WorldManager.Instance.UpdateColorBlind();
				break;
			case SettingType.ParticleQuality:
				CurrentData.ParticleQuality = ParticleGrade[GetDropdown(settingType).value];
				WorldManager.Instance.UpdateParticleQuality();
				break;
			case SettingType.SoftParticles:
				CurrentData.SoftParticles = GetToggle(settingType).isOn;
				QualitySettings.softParticles = CurrentData.SoftParticles;
				break;
			case SettingType.Framelock:
				CurrentData.FrameLock = FrameLock[GetDropdown(settingType).value];
				WorldManager.Instance.UpdateFrameLimiter();
				break;
			case SettingType.EnvironmentElements:
				CurrentData.EnvironmentElements = GetToggle(settingType).isOn;
				WorldManager.Instance.UpdateEnvironmentElements();
				break;
			case SettingType.VolumeLight:
				CurrentData.VolumeLight = VolumetricLightModes[GetDropdown(settingType).value];
				WorldManager.Instance.UpdateVolumeLight();
				break;
			case SettingType.PixelLightCount:
				CurrentData.PixelLightCount = (int)GetSlider(settingType).value;
				QualitySettings.pixelLightCount = CurrentData.PixelLightCount;
				break;
			case SettingType.AtmosphericScattering:
				CurrentData.AtmosphericScattering = GetToggle(settingType).isOn;
				CursorManager.UpdateAtmosphericScattering();
				break;
			case SettingType.RenderDistance:
				CurrentData.RenderDistance = RenderDistanceTypes[GetDropdown(settingType).value];
				OcclusionManager.Instance.UpdateRenderDistanceMultiplier();
				break;
			case SettingType.MinableDistance:
				CurrentData.MinableDistance = MinableRenderDistanceTypes[GetDropdown(settingType).value];
				MinableDrawCallCollection.RefreshMinableRenderDistance();
				VoxelTerrain.UpdateRenderDistance().Forget();
				LodManager.UpdateRenderDistance();
				break;
			case SettingType.TerrainDistance:
				CurrentData.TerrainDistance = TerrainRenderDistanceTypes[GetDropdown(settingType).value];
				LodManager.UpdateRenderDistance();
				break;
			case SettingType.MasterVolume:
				CurrentData.MasterVolume = (int)GetSlider(settingType).value;
				if (!GameManager.IsBatchMode)
				{
					AudioManager.UpdateVolume(settingType);
				}
				break;
			case SettingType.SoundVolume:
				CurrentData.SoundVolume = (int)GetSlider(settingType).value;
				if (!GameManager.IsBatchMode)
				{
					AudioManager.UpdateVolume(settingType);
				}
				break;
			case SettingType.VoiceNotificationVolume:
				CurrentData.VoiceNotificationVolume = (int)GetSlider(settingType).value;
				if (!GameManager.IsBatchMode)
				{
					AudioManager.UpdateVolume(settingType);
				}
				break;
			case SettingType.MusicVolume:
				CurrentData.MusicVolume = (int)GetSlider(settingType).value;
				if (!GameManager.IsBatchMode)
				{
					AudioManager.UpdateVolume(settingType);
				}
				break;
			case SettingType.InterfaceVolume:
				CurrentData.InterfaceVolume = (int)GetSlider(settingType).value;
				if (!GameManager.IsBatchMode)
				{
					AudioManager.UpdateVolume(settingType);
				}
				break;
			case SettingType.ServerName:
				CurrentData.ServerName = GetInputField(settingType).text;
				break;
			case SettingType.ServerVisible:
				CurrentData.ServerVisible = GetToggle(settingType).isOn;
				break;
			case SettingType.ServerPassword:
				CurrentData.ServerPassword = GetInputField(settingType).text.Trim();
				break;
			case SettingType.ServerAdminPassword:
				CurrentData.AdminPassword = GetInputField(settingType).text;
				break;
			case SettingType.ServerMaxPlayers:
				CurrentData.ServerMaxPlayers = (int)GetSlider(settingType).value;
				break;
			case SettingType.ServerGamePort:
			{
				int num2 = int.Parse(GetInputField(settingType).text);
				if (num2 < 0 || num2 > 65535)
				{
					AlertPanel.Instance.ShowAlert(AlertStrings.InvalidPort, AlertState.Alert);
				}
				CurrentData.GamePort = Mathf.Clamp(int.Parse(GetInputField(settingType).text), 0, 65535).ToString();
				GetInputField(settingType).text = CurrentData.GamePort;
				break;
			}
			case SettingType.ServerUpdatePort:
			{
				if (GameManager.GameState != GameState.None)
				{
					AlertPanel.Instance.ShowAlert(AlertStrings.CannotUpdatePort, AlertState.Alert);
					return;
				}
				int num = int.Parse(GetInputField(settingType).text);
				if (num < 0 || num > 65535)
				{
					AlertPanel.Instance.ShowAlert(AlertStrings.InvalidPort, AlertState.Alert);
				}
				CurrentData.UpdatePort = Mathf.Clamp(num, 0, 65535).ToString();
				GetInputField(settingType).text = CurrentData.UpdatePort;
				break;
			}
			case SettingType.UPNPEnabled:
				CurrentData.UPNPEnabled = GetToggle(settingType).isOn;
				if (CurrentData.UPNPEnabled)
				{
					Singleton<NatDiscoverer>.Instance.DiscoverDeviceAsync();
				}
				else
				{
					Singleton<NatDiscoverer>.Instance.DisableDeviceAsync();
				}
				break;
			case SettingType.Language:
			{
				LanguageCode code = Instance._languageDropdown[GetDropdown(settingType).value].Code;
				if (CurrentData.LanguageCode != code)
				{
					CurrentData.LanguageCode = code;
					Localization.SetLanguage(CurrentData.LanguageCode);
					StatusUpdates.Instance.Initialize();
				}
				break;
			}
			case SettingType.VoiceLanguage:
			{
				LanguageCode languageCode = Instance._voiceLanguageDropdown[GetDropdown(settingType).value];
				if (CurrentData.VoiceLanguageCode != languageCode)
				{
					CurrentData.VoiceLanguageCode = languageCode;
					StatusUpdates.OnSettingChanged();
				}
				break;
			}
			case SettingType.VoiceControl:
				CurrentData.Voice = GetToggle(settingType).isOn && GetWindowVersion() >= 10;
				break;
			case SettingType.PopupChat:
				CurrentData.PopupChat = GetToggle(settingType).isOn;
				break;
			case SettingType.MouseWheelZoom:
				CurrentData.MouseWheelZoom = GetToggle(settingType).isOn;
				break;
			case SettingType.CameraSensitivity:
				CurrentData.CameraSensitivity = (int)GetSlider(settingType).value;
				WorldManager.Instance.UpdateCameraSensitivity();
				break;
			case SettingType.ShowDisplayPortrait:
				CurrentData.IngamePortrait = GetToggle(settingType).isOn;
				CameraController.RefreshPortrait();
				break;
			case SettingType.InvertMouse:
				CurrentData.InvertMouse = GetToggle(settingType).isOn;
				break;
			case SettingType.InvertMouseWheelInventory:
				CurrentData.InvertMouseWheelInventory = GetToggle(settingType).isOn;
				break;
			case SettingType.MenuLite:
				CurrentData.MenuLite = GetToggle(settingType).isOn;
				Singleton<GameManager>.Instance.MenuCutscene.MenuLite(CurrentData.MenuLite);
				break;
			case SettingType.AmbientOcclusion:
				CameraController.SetAmbientOcclusion();
				break;
			case SettingType.LensFlares:
			{
				if (!RenderSettings.sun)
				{
					break;
				}
				EasyFlares component = RenderSettings.sun.GetComponent<EasyFlares>();
				if ((bool)RenderSettings.sun)
				{
					CurrentData.LensFlares = GetToggle(SettingType.LensFlares).isOn;
					if ((bool)component)
					{
						component.enabled = CurrentData.LensFlares && CursorManager.DefaultEasyFlareEnabled;
					}
				}
				break;
			}
			case SettingType.Clouds:
				if (!GameManager.IsBatchMode)
				{
					bool isOn = GetToggle(SettingType.Clouds).isOn;
					CurrentData.Clouds = isOn;
				}
				break;
			case SettingType.LegacyInventory:
				CurrentData.LegacyInventory = GetToggle(settingType).isOn;
				break;
			case SettingType.SlotToolTips:
				CurrentData.ShowSlotToolTips = GetToggle(settingType).isOn;
				break;
			case SettingType.HelmetOverlay:
				CurrentData.HelmetOverlay = GetToggle(settingType).isOn;
				if (GameManager.GameState == GameState.Running)
				{
					if (CurrentData.HelmetOverlay)
					{
						FirstPersonHelmetOverlay.Instance?.EquippedHelmet(InventoryManager.ParentHuman?.HeadAsSpaceHelmet);
					}
					else
					{
						FirstPersonHelmetOverlay.Instance?.RemoveHelmet();
					}
				}
				break;
			case SettingType.TerrainDetail:
				CurrentData.TerrainDetail = TerrainTesselationTypes[GetDropdown(settingType).value];
				TerrainShaderScript.SetTerrainDetail(CurrentData.TerrainDetail);
				break;
			case SettingType.WeatherEventQuality:
				CurrentData.WeatherEventQuality = WeatherEventSettings[GetDropdown(settingType).value];
				if (GameManager.GameState != GameState.Running)
				{
				}
				break;
			case SettingType.StartLocalHost:
				CurrentData.StartLocalHost = GameManager.IsBatchMode || GetToggle(settingType).isOn;
				NetworkServer.ApplyLocalHostSetting(CurrentData.StartLocalHost);
				break;
			case SettingType.RefreshRate:
			{
				string s = GetDropdown(SettingType.RefreshRate).captionText.text.ToLower().Replace("hz", string.Empty).Trim();
				CurrentData.RefreshRate = int.Parse(s);
				Screen.SetResolution(int.Parse(CurrentData.ScreenWidth), int.Parse(CurrentData.ScreenHeight), CurrentData.FullScreen, CurrentData.RefreshRate);
				break;
			}
			case SettingType.ShadowNearPlaneOffset:
				QualitySettings.shadowNearPlaneOffset = CurrentData.ShadowNearPlaneOffset;
				break;
			case SettingType.ShadowCascades:
				QualitySettings.shadowCascades = CurrentData.ShadowCascades;
				break;
			case SettingType.DisplayHelperHints:
				CurrentData.DisplayHelperHints = GetToggle(settingType).isOn;
				HelperHintsTextController.RefreshDisplayState();
				break;
			default:
				Debug.LogError($"Setting {settingType} Not implemented!");
				break;
			case SettingType.ExtendedTerrain:
			case SettingType.Resolution:
			case SettingType.ScreenMode:
			case SettingType.DisableWaterVisualizer:
			case SettingType.P2PHostEnabled:
			case SettingType.TerrainClutter:
			case SettingType.TerrainClutterMultiplier:
			case SettingType.DoubleClickDelay:
			case SettingType.LightShadowDistance:
			case SettingType.MaxThingLights:
				break;
			}
			NetworkManager.UpdateSessionData(CurrentData);
			SaveSettings();
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}

	public void ResetConsoleFadeTimeButton()
	{
	}

	public bool ApplySetting(string setting, string result)
	{
		ConsoleWindow.Print("Applying settings not implemented - Setting: " + setting + " - result: " + result);
		return false;
	}
}
