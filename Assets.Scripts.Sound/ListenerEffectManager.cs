using System.Collections;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using CharacterCustomisation;
using Cysharp.Threading.Tasks;
using Sound;
using UnityEngine;
using UnityEngine.Audio;

namespace Assets.Scripts.Sound;

public class ListenerEffectManager : Singleton<ListenerEffectManager>
{
	public bool ReverbControl = true;

	public bool LowpassControl = true;

	public AudioMixerGroup MasterGroup;

	public AudioMixerGroup InterfaceGroup;

	public GasMask CurrentHelmet;

	public AnimationCurve AtmosFilterCurve;

	private bool _loadedSettings;

	private static float _worldVolumeMultiplier = 1f;

	private static float _localPlayerInternalLowpassSetting;

	private static float _localPlayerLowpassSetting;

	private static float _externalLowpassSetting;

	private static bool _helmetClosed;

	private static bool _closingHelmet;

	private static bool _openingHelmet;

	public const float LerpSpeed = 3f;

	public static float WorldVolumeMultiplier
	{
		get
		{
			return _worldVolumeMultiplier;
		}
		set
		{
			_worldVolumeMultiplier = value;
			if (!GameManager.IsBatchMode)
			{
				AudioManager.UpdateVolume(SettingType.SoundVolume);
			}
		}
	}

	private static float LocalPlayerInternalLowpass
	{
		get
		{
			return _localPlayerInternalLowpassSetting;
		}
		set
		{
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("InternalLowpass", value);
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("InternalNoFxLowpass", value);
			_localPlayerInternalLowpassSetting = value;
		}
	}

	private static float LocalPlayerLowpass
	{
		get
		{
			return _localPlayerLowpassSetting;
		}
		set
		{
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("LocalPlayerLowpass", value);
			_localPlayerLowpassSetting = value;
		}
	}

	private static float ExternalLowpass
	{
		get
		{
			return _externalLowpassSetting;
		}
		set
		{
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("ExternalLowpass", value);
			ReverbSetting.HfReferenceAtmosMax = Mathf.RoundToInt(value);
			ReverbSetting.RoomHfAtmosMax = Mathf.RoundToInt(Mathf.Lerp(-6000f, 0f, Mathf.Clamp01(value / 10000f)));
			ReverbSetting.RoomAtmosMax = Mathf.RoundToInt(Mathf.Lerp(-6000f, 0f, Mathf.Clamp01(value / 500f)));
			_externalLowpassSetting = value;
		}
	}

	public static bool HelmetClosed
	{
		get
		{
			return _helmetClosed;
		}
		private set
		{
			if (_helmetClosed != value && GameManager.GameState == GameState.Running)
			{
				if (value)
				{
					Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("LocalPlayerHelmetSend", 0f);
					Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("LocalPlayerGainAttenuation", 0.05f);
					Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("InternalHelmetFxSend", -4.5f);
					Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("InternalVolume", -4f);
					Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("InternalNoFxVolume", 0.5f);
					Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("HelmetFxVolume", -2f);
					Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("ExternalHelmetSend", -3f);
					Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("ExternalGainAttenuation", 0.05f);
					Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("ExternalHighpass", 150f);
				}
				else
				{
					Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("LocalPlayerHelmetSend", -40f);
					Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("LocalPlayerGainAttenuation", 1f);
					Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("InternalHelmetFxSend", -40f);
					Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("InternalVolume", 0.5f);
					Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("InternalNoFxVolume", 0.5f);
					Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("HelmetFxVolume", -40f);
					Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("ExternalHelmetSend", -40f);
					Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("ExternalGainAttenuation", 1f);
					Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("ExternalHighpass", 10f);
				}
				_helmetClosed = value;
			}
		}
	}

	public override void ManagerUpdate()
	{
		base.ManagerUpdate();
		if (WorldManager.IsGamePaused)
		{
			return;
		}
		if (!_loadedSettings && (bool)Settings.Instance)
		{
			_loadedSettings = true;
			Settings.ApplyVolumeSettings();
		}
		if (GameManager.GameState != GameState.Running || !InventoryManager.Parent)
		{
			return;
		}
		if (!Singleton<ListenerEffectManager>.Instance.LowpassControl)
		{
			ExternalLowpass = 22000f;
			LocalPlayerLowpass = 22000f;
			LocalPlayerInternalLowpass = 22000f;
			return;
		}
		if (!InventoryManager.Parent.AsHuman.HelmetSlot.Occupant)
		{
			SetHelmetFilter(isOn: false, null);
		}
		float num = ((InventoryManager.Parent.WorldAtmosphere != null) ? InventoryManager.Parent.WorldAtmosphere.RatioOneAtmosphereClamped() : 0f);
		if (float.IsNaN(num))
		{
			num = 1f;
		}
		num *= 1f - Mathf.Clamp01(InventoryManager.Parent.DamageState.Stun / 100f);
		num = AtmosFilterCurve.Evaluate(Mathf.Clamp01(num));
		if (CameraController.IsUnderWater)
		{
			num *= 0.1f;
		}
		float num2 = num;
		float num3 = 600f;
		if (_closingHelmet || HelmetClosed)
		{
			if (!InventoryManager.Parent.AsHuman.HelmetSlot.Occupant)
			{
				num2 = num;
				if (InventoryManager.Parent.AsHuman.SpeciesClass == SpeciesClass.Robot)
				{
					num2 = 5000f;
					num3 = 900f;
				}
			}
			else
			{
				num2 = ((InventoryManager.Parent.AsHuman.HelmetSlot.Occupant.InternalAtmosphere != null) ? (InventoryManager.Parent.AsHuman.HelmetSlot.Occupant.InternalAtmosphere.RatioOneAtmosphereClamped() * 22000f) : num);
				if (InventoryManager.Parent.AsHuman.SpeciesClass == SpeciesClass.Robot && num2 < 5000f)
				{
					num2 = 5000f;
				}
			}
		}
		else if (_openingHelmet || !HelmetClosed)
		{
			num2 = num;
			if (InventoryManager.Parent.AsHuman.SpeciesClass == SpeciesClass.Robot)
			{
				num2 = 5000f;
			}
		}
		num2 = Mathf.Clamp(num2, 1500f, 22000f);
		ExternalLowpass = Mathf.Lerp(ExternalLowpass, num, Time.deltaTime * 3f);
		LocalPlayerLowpass = Mathf.Lerp(LocalPlayerLowpass, num + num3, Time.deltaTime * 3f);
		LocalPlayerInternalLowpass = Mathf.Lerp(LocalPlayerInternalLowpass, num2, Time.deltaTime * 3f);
	}

	public static Coroutine CloseHelmet()
	{
		_closingHelmet = true;
		_openingHelmet = false;
		return Singleton<ListenerEffectManager>.Instance.StartCoroutine(ClosingHelmet());
	}

	private static IEnumerator ClosingHelmet()
	{
		while (_closingHelmet && !_openingHelmet)
		{
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("LocalPlayerHelmetSend", Mathf.Lerp(GetMixerData("LocalPlayerHelmetSend"), 0f, 3f * Time.deltaTime));
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("LocalPlayerGainAttenuation", Mathf.Lerp(GetMixerData("LocalPlayerGainAttenuation"), 0.05f, 3f * Time.deltaTime));
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("InternalHelmetFxSend", Mathf.Lerp(GetMixerData("InternalHelmetFxSend"), -4.5f, 3f * Time.deltaTime));
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("InternalVolume", Mathf.Lerp(GetMixerData("InternalVolume"), -4f, 3f * Time.deltaTime));
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("InternalNoFxVolume", Mathf.Lerp(GetMixerData("InternalNoFxVolume"), 0.5f, 3f * Time.deltaTime));
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("HelmetFxVolume", Mathf.Lerp(GetMixerData("HelmetFxVolume"), -2f, 3f * Time.deltaTime));
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("ExternalHelmetSend", Mathf.Lerp(GetMixerData("ExternalHelmetSend"), -3f, 3f * Time.deltaTime));
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("ExternalGainAttenuation", Mathf.Lerp(GetMixerData("ExternalGainAttenuation"), 0.05f, 3f * Time.deltaTime));
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("ExternalHighpass", Mathf.Lerp(GetMixerData("ExternalHighpass"), 150f, 3f * Time.deltaTime));
			if (GetMixerData("LocalPlayerHelmetSend") >= -1f)
			{
				HelmetClosed = true;
				_closingHelmet = false;
			}
			yield return Yielders.EndOfFrame;
		}
	}

	public static void OpenHelmet()
	{
		_closingHelmet = false;
		_openingHelmet = true;
		OpeningHelmet().Forget();
	}

	private static async UniTaskVoid OpeningHelmet()
	{
		while (_openingHelmet && !_closingHelmet)
		{
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("LocalPlayerHelmetSend", Mathf.Lerp(GetMixerData("LocalPlayerHelmetSend"), -40f, 3f * Time.deltaTime));
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("LocalPlayerGainAttenuation", Mathf.Lerp(GetMixerData("LocalPlayerGainAttenuation"), 1f, 3f * Time.deltaTime));
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("InternalHelmetFxSend", Mathf.Lerp(GetMixerData("InternalHelmetFxSend"), -40f, 3f * Time.deltaTime));
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("InternalVolume", Mathf.Lerp(GetMixerData("InternalVolume"), 0.5f, 3f * Time.deltaTime));
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("InternalNoFxVolume", Mathf.Lerp(GetMixerData("InternalNoFxVolume"), 0.5f, 3f * Time.deltaTime));
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("HelmetFxVolume", Mathf.Lerp(GetMixerData("HelmetFxVolume"), -40f, 3f * Time.deltaTime));
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("ExternalHelmetSend", Mathf.Lerp(GetMixerData("ExternalHelmetSend"), -40f, 3f * Time.deltaTime));
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("ExternalGainAttenuation", Mathf.Lerp(GetMixerData("ExternalGainAttenuation"), 1f, 3f * Time.deltaTime));
			Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.SetFloat("ExternalHighpass", Mathf.Lerp(GetMixerData("ExternalHighpass"), 10f, 3f * Time.deltaTime));
			if (GetMixerData("LocalPlayerHelmetSend") <= -39f)
			{
				HelmetClosed = false;
				_openingHelmet = false;
			}
			await UniTask.NextFrame();
		}
	}

	public static float GetMixerData(string parameterName)
	{
		if (!Singleton<ListenerEffectManager>.Instance.MasterGroup.audioMixer.GetFloat(parameterName, out var value))
		{
			return 0f;
		}
		return value;
	}

	public static void SetHelmetFilter(bool isOn, GasMask helmet)
	{
		if (!Singleton<ListenerEffectManager>.Instance)
		{
			return;
		}
		if (!Singleton<ListenerEffectManager>.Instance.ReverbControl)
		{
			HelmetClosed = false;
			return;
		}
		if (!Singleton<ListenerEffectManager>.Instance.CurrentHelmet && HelmetClosed)
		{
			HelmetClosed = false;
		}
		isOn = isOn && (bool)helmet.ParentHuman && helmet.ParentHuman.HasAuthority && helmet.ParentSlot.Type == Slot.Class.Helmet;
		if (Singleton<ListenerEffectManager>.Instance.CurrentHelmet == helmet)
		{
			if (isOn)
			{
				CloseHelmet();
				Singleton<ListenerEffectManager>.Instance.CurrentHelmet = helmet;
			}
			else
			{
				OpenHelmet();
				Singleton<ListenerEffectManager>.Instance.CurrentHelmet = null;
			}
		}
		else if (isOn)
		{
			CloseHelmet();
			Singleton<ListenerEffectManager>.Instance.CurrentHelmet = helmet;
		}
	}
}
