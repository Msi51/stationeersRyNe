using System;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using UnityEngine;

namespace Sound;

[Serializable]
public class ReverbSetting
{
	private static readonly int ReverbVolumeMin = -10000;

	private static readonly int HfREferenceMin = 1000;

	[Range(1000f, 20000f)]
	public static int HfReferenceAtmosMax;

	[Range(-10000f, 0f)]
	public static int RoomHfAtmosMax;

	[Range(-10000f, 0f)]
	public static int RoomAtmosMax;

	public string ReverbName;

	public int NameHash;

	[Range(-10000f, 0f)]
	public int Room;

	[Range(-10000f, 0f)]
	public int RoomHf;

	[Range(-10000f, 0f)]
	public int RoomLf;

	[Range(0.1f, 20f)]
	public float DecayTime;

	[Range(0.1f, 2f)]
	public float DecayHfRatio;

	[Range(-10000f, 1000f)]
	public int Reflections;

	[Range(0f, 0.3f)]
	public float ReflectionsDelay;

	[Range(-10000f, 2000f)]
	public int Reverb;

	[Range(0f, 0.1f)]
	public float ReverbDelay;

	[Range(1000f, 20000f)]
	public int HfReference;

	[Range(20f, 1000f)]
	public int LfReference;

	[Range(0f, 100f)]
	public float Diffusion;

	[Range(0f, 100f)]
	public float Density;

	public void SetSettingBlend(ReverbSetting settingA, ReverbSetting settingB, float ratio)
	{
		SetTargetSettingBlend(this, settingA, settingB, ratio);
	}

	private static int GetReverbVolumeOffset(int baseValue)
	{
		int num = Mathf.Min(Settings.CurrentData.SoundVolume, Settings.CurrentData.MasterVolume);
		return (int)RocketMath.MapToScale(0f, 100f, -10000f, baseValue, num);
	}

	public static void SetTargetSettingBlend(ReverbSetting target, ReverbSetting settingA, ReverbSetting settingB, float ratio)
	{
		target.Room = Mathf.RoundToInt(Mathf.Clamp(Mathf.Lerp(settingA.Room, settingB.Room, ratio), ReverbVolumeMin, RoomAtmosMax));
		target.RoomHf = Mathf.RoundToInt(Mathf.Clamp(Mathf.Lerp(settingA.RoomHf, settingB.RoomHf, ratio), ReverbVolumeMin, RoomHfAtmosMax));
		target.RoomLf = Mathf.RoundToInt(Mathf.Lerp(settingA.RoomLf, settingB.RoomLf, ratio));
		target.DecayTime = Mathf.Lerp(settingA.DecayTime, settingB.DecayTime, ratio);
		target.DecayHfRatio = Mathf.Lerp(settingA.DecayHfRatio, settingB.DecayHfRatio, ratio);
		target.Reflections = Mathf.RoundToInt(Mathf.Lerp(settingA.Reflections, settingB.Reflections, ratio));
		target.ReflectionsDelay = Mathf.Lerp(settingA.ReflectionsDelay, settingB.ReflectionsDelay, ratio);
		target.Reverb = Mathf.RoundToInt(Mathf.Lerp(settingA.Reverb, settingB.Reverb, ratio));
		target.ReverbDelay = Mathf.Lerp(settingA.ReverbDelay, settingB.ReverbDelay, ratio);
		target.HfReference = Mathf.RoundToInt(Mathf.Clamp(Mathf.Lerp(settingA.HfReference, settingB.HfReference, ratio), HfREferenceMin, HfReferenceAtmosMax));
		target.LfReference = Mathf.RoundToInt(Mathf.Lerp(settingA.LfReference, settingB.LfReference, ratio));
		target.Diffusion = Mathf.Lerp(settingA.Diffusion, settingB.Diffusion, ratio);
		target.Density = Mathf.Lerp(settingA.Density, settingB.Density, ratio);
	}

	public void Apply(AudioReverbZone reverbZone)
	{
		if (reverbZone.reverbPreset != AudioReverbPreset.User)
		{
			reverbZone.reverbPreset = AudioReverbPreset.User;
		}
		reverbZone.room = Room + GetReverbVolumeOffset(Room);
		reverbZone.roomHF = RoomHf + GetReverbVolumeOffset(RoomHf);
		reverbZone.roomLF = RoomLf + GetReverbVolumeOffset(RoomLf);
		reverbZone.decayTime = DecayTime;
		reverbZone.decayHFRatio = DecayHfRatio;
		reverbZone.reflections = Reflections;
		reverbZone.reflectionsDelay = ReflectionsDelay;
		reverbZone.reverb = Reverb;
		reverbZone.reverbDelay = ReverbDelay;
		reverbZone.HFReference = HfReference;
		reverbZone.LFReference = LfReference;
		reverbZone.diffusion = Diffusion;
		reverbZone.density = Density;
	}

	public void Set(ReverbSetting setting)
	{
		SetTarget(this, setting);
	}

	public static void SetTarget(ReverbSetting target, ReverbSetting setting)
	{
		target.Room = setting.Room;
		target.RoomHf = setting.RoomHf;
		target.RoomLf = setting.RoomLf;
		target.DecayTime = setting.DecayTime;
		target.DecayHfRatio = setting.DecayHfRatio;
		target.Reflections = setting.Reflections;
		target.ReflectionsDelay = setting.ReflectionsDelay;
		target.Reverb = setting.Reverb;
		target.ReverbDelay = setting.ReverbDelay;
		target.HfReference = setting.HfReference;
		target.LfReference = setting.LfReference;
		target.Diffusion = setting.Diffusion;
		target.Density = setting.Density;
	}
}
