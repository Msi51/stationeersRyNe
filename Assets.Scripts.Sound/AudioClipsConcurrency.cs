using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Sound;

[Serializable]
public class AudioClipsConcurrency
{
	public string Name;

	public int NameHash;

	public int MaxCount;

	public bool CanPause;

	public ConcurrencyResolutionRule ResolutionRule;

	private readonly List<GameAudioSource> _subscribed = new List<GameAudioSource>(128);

	public static readonly AudioClipsConcurrency[] ConcurrencySettings = new AudioClipsConcurrency[49]
	{
		new AudioClipsConcurrency("UI_SingleVoice", 1, canPause: false, ConcurrencyResolutionRule.StopOldest),
		new AudioClipsConcurrency("PoweredDevices", 8, canPause: true, ConcurrencyResolutionRule.StopFarthest),
		new AudioClipsConcurrency("UI_MouseOver", 1, canPause: false, ConcurrencyResolutionRule.StopOldest),
		new AudioClipsConcurrency("UI_Notify", 1, canPause: false, ConcurrencyResolutionRule.StopLowPriorityThenNew),
		new AudioClipsConcurrency("Collision", 12, canPause: false, ConcurrencyResolutionRule.StopInaudibleThenNew),
		new AudioClipsConcurrency("UI_InventoryPanel", 1, canPause: false, ConcurrencyResolutionRule.StopLowPriorityThenNew),
		new AudioClipsConcurrency("UI_NotifyLowPriority", 1, canPause: false, ConcurrencyResolutionRule.StopNew),
		new AudioClipsConcurrency("UI_ObjectiveSingleVoice", 1, canPause: false, ConcurrencyResolutionRule.StopLowPriorityThenNew),
		new AudioClipsConcurrency("UI_ObjectiveCompleteSingleVoice", 1, canPause: false, ConcurrencyResolutionRule.StopLowPriorityThenNew),
		new AudioClipsConcurrency("UI_Repeater", 1, canPause: false, ConcurrencyResolutionRule.StopNew),
		new AudioClipsConcurrency("UI_MoveToSlot", 1, canPause: false, ConcurrencyResolutionRule.StopLowPriorityThenOld),
		new AudioClipsConcurrency("ActiveVents", 4, canPause: true, ConcurrencyResolutionRule.StopFarthest),
		new AudioClipsConcurrency("Conveyors", 6, canPause: true, ConcurrencyResolutionRule.StopFarthest),
		new AudioClipsConcurrency("UI_RotateBlueprint", 1, canPause: false, ConcurrencyResolutionRule.StopNew),
		new AudioClipsConcurrency("FurnaceSmelt", 6, canPause: false, ConcurrencyResolutionRule.StopInaudibleThenOldest),
		new AudioClipsConcurrency("Equip", 1, canPause: false, ConcurrencyResolutionRule.StopLowPriorityThenOld),
		new AudioClipsConcurrency("InWorldUI", 1, canPause: false, ConcurrencyResolutionRule.StopNew),
		new AudioClipsConcurrency("LogicUnitSounds", 6, canPause: false, ConcurrencyResolutionRule.StopInaudibleThenNew),
		new AudioClipsConcurrency("LogicMathUnitSounds", 6, canPause: false, ConcurrencyResolutionRule.StopInaudibleThenNew),
		new AudioClipsConcurrency("UI_HighlightInWorld", 1, canPause: false, ConcurrencyResolutionRule.StopOldest),
		new AudioClipsConcurrency("WindClose", 12, canPause: false, ConcurrencyResolutionRule.StopOldest),
		new AudioClipsConcurrency("WindFar", 14, canPause: false, ConcurrencyResolutionRule.StopOldest),
		new AudioClipsConcurrency("HarvieArm", 6, canPause: false, ConcurrencyResolutionRule.StopFarthest),
		new AudioClipsConcurrency("WindTurbine", 6, canPause: true, ConcurrencyResolutionRule.StopFarthest),
		new AudioClipsConcurrency("OreDetector", 4, canPause: true, ConcurrencyResolutionRule.StopFarthest),
		new AudioClipsConcurrency("WindTurbineLarge", 6, canPause: true, ConcurrencyResolutionRule.StopFarthest),
		new AudioClipsConcurrency("UI_Click", 1, canPause: false, ConcurrencyResolutionRule.StopOldest),
		new AudioClipsConcurrency("UI_Slider", 1, canPause: false, ConcurrencyResolutionRule.StopNew),
		new AudioClipsConcurrency("SolarPanel", 6, canPause: true, ConcurrencyResolutionRule.StopFarthest),
		new AudioClipsConcurrency("ImportExport", 4, canPause: false, ConcurrencyResolutionRule.StopFarthest),
		new AudioClipsConcurrency("HarvieInteract", 3, canPause: false, ConcurrencyResolutionRule.StopFarthest),
		new AudioClipsConcurrency("AtmosphereFireStart", 12, canPause: false, ConcurrencyResolutionRule.StopInaudibleThenOldest),
		new AudioClipsConcurrency("AtmosphereFire", 16, canPause: false, ConcurrencyResolutionRule.StopInaudibleThenOldest),
		new AudioClipsConcurrency("PipeBurst", 6, canPause: true, ConcurrencyResolutionRule.StopFarthest),
		new AudioClipsConcurrency("PipeDamage", 3, canPause: false, ConcurrencyResolutionRule.StopInaudibleThenNew),
		new AudioClipsConcurrency("PipeAnalyser", 4, canPause: true, ConcurrencyResolutionRule.StopFarthest),
		new AudioClipsConcurrency("Shutter", 4, canPause: true, ConcurrencyResolutionRule.StopFarthest),
		new AudioClipsConcurrency("ShutterStop", 4, canPause: false, ConcurrencyResolutionRule.StopFarthest),
		new AudioClipsConcurrency("Lights", 4, canPause: true, ConcurrencyResolutionRule.StopFarthest),
		new AudioClipsConcurrency("Pumps", 4, canPause: true, ConcurrencyResolutionRule.StopFarthest),
		new AudioClipsConcurrency("AtmosphericsDevice", 5, canPause: true, ConcurrencyResolutionRule.StopFarthest),
		new AudioClipsConcurrency("Furnace", 6, canPause: true, ConcurrencyResolutionRule.StopFarthest),
		new AudioClipsConcurrency("DeepMiner", 4, canPause: true, ConcurrencyResolutionRule.StopFarthest),
		new AudioClipsConcurrency("DeepMinerGears", 3, canPause: true, ConcurrencyResolutionRule.StopFarthest),
		new AudioClipsConcurrency("Centrifuge", 8, canPause: true, ConcurrencyResolutionRule.StopFarthest),
		new AudioClipsConcurrency("CentrifugeStress", 6, canPause: true, ConcurrencyResolutionRule.StopFarthest),
		new AudioClipsConcurrency("Transformer", 4, canPause: true, ConcurrencyResolutionRule.StopFarthest),
		new AudioClipsConcurrency("Battery", 4, canPause: true, ConcurrencyResolutionRule.StopFarthest),
		new AudioClipsConcurrency("Apc", 4, canPause: true, ConcurrencyResolutionRule.StopFarthest)
	};

	public AudioClipsConcurrency(string name, int maxCount, bool canPause, ConcurrencyResolutionRule resolutionRule)
	{
		Name = name;
		NameHash = Animator.StringToHash(name);
		MaxCount = ((maxCount <= 0) ? 1 : maxCount);
		CanPause = canPause;
		ResolutionRule = resolutionRule;
	}

	public static AudioClipsConcurrency Get(int concurrencyNameHash)
	{
		AudioClipsConcurrency[] concurrencySettings = ConcurrencySettings;
		foreach (AudioClipsConcurrency audioClipsConcurrency in concurrencySettings)
		{
			if (audioClipsConcurrency.NameHash == concurrencyNameHash)
			{
				return audioClipsConcurrency;
			}
		}
		return null;
	}

	public static void Sort(int id)
	{
		Get(id)?.Sort();
	}

	public void Sort()
	{
		if (ResolutionRule == ConcurrencyResolutionRule.StopFarthest)
		{
			_subscribed.Sort((GameAudioSource a, GameAudioSource b) => b.SortByDistanceClosestToFarthest(a));
		}
	}

	public static void Register(int id, GameAudioSource audioSource)
	{
		Get(id)?.Register(audioSource);
	}

	public void Register(GameAudioSource audioSource)
	{
		if (_subscribed.Contains(audioSource))
		{
			return;
		}
		bool flag = false;
		switch (ResolutionRule)
		{
		case ConcurrencyResolutionRule.StopOldest:
		case ConcurrencyResolutionRule.StopInaudibleThenOldest:
		case ConcurrencyResolutionRule.StopNew:
		case ConcurrencyResolutionRule.StopInaudibleThenNew:
			_subscribed.Add(audioSource);
			flag = true;
			break;
		case ConcurrencyResolutionRule.StopFarthest:
		{
			for (int i = 0; i < _subscribed.Count; i++)
			{
				if (_subscribed[i].SortByDistanceClosestToFarthest(audioSource) != 1)
				{
					_subscribed.Insert(i, audioSource);
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				_subscribed.Add(audioSource);
			}
			break;
		}
		case ConcurrencyResolutionRule.StopLowPriorityThenNew:
		case ConcurrencyResolutionRule.StopLowPriorityThenOld:
		{
			for (int num = _subscribed.Count - 1; num >= 0; num--)
			{
				if (num > 0 && _subscribed[num].SortByPriorityHighestToLowest(audioSource) == -1 && _subscribed[num - 1].SortByPriorityHighestToLowest(audioSource) != -1)
				{
					_subscribed.Insert(num, audioSource);
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				_subscribed.Add(audioSource);
			}
			break;
		}
		}
	}

	public static void RemoveNullSubscribers(int id)
	{
		Get(id)?.RemoveNullSubscribers();
	}

	public void RemoveNullSubscribers()
	{
		for (int num = _subscribed.Count - 1; num >= 0; num--)
		{
			if (_subscribed[num] == null)
			{
				_subscribed.RemoveAt(num);
			}
		}
	}

	public static void Remove(int id, GameAudioSource audioSource)
	{
		Get(id)?.Remove(audioSource);
	}

	public void Remove(GameAudioSource audioSource)
	{
		_subscribed.Remove(audioSource);
	}

	public static void ManageConcurrency(int id, bool fadeVolume = true)
	{
		Get(id)?.ManageConcurrency(fadeVolume);
	}

	public void ManageConcurrency(bool fadeVolume)
	{
		if (_subscribed.Count > MaxCount || CanPause)
		{
			switch (ResolutionRule)
			{
			case ConcurrencyResolutionRule.StopOldest:
				StopOldest();
				break;
			case ConcurrencyResolutionRule.StopFarthest:
				StopFarthest(fadeVolume);
				break;
			case ConcurrencyResolutionRule.StopInaudibleThenOldest:
				StopInAudible();
				StopOldest();
				break;
			case ConcurrencyResolutionRule.StopNew:
			case ConcurrencyResolutionRule.StopLowPriorityThenNew:
				StopNew();
				break;
			case ConcurrencyResolutionRule.StopLowPriorityThenOld:
				StopLowPriorityThenOld();
				break;
			case ConcurrencyResolutionRule.StopInaudibleThenNew:
				StopInAudible();
				StopNew();
				break;
			}
		}
	}

	private void StopOldest()
	{
		if (CanPause)
		{
			return;
		}
		for (int num = _subscribed.Count - MaxCount - 1; num >= 0; num--)
		{
			if (!_subscribed[num].StopForConcurrency(this))
			{
				_subscribed.RemoveAt(num);
			}
		}
	}

	private void StopFarthest(bool fadeVolume)
	{
		if (CanPause)
		{
			for (int num = _subscribed.Count - 1; num >= 0; num--)
			{
				if (num < MaxCount)
				{
					if (!_subscribed[num].ResumeForConcurrency(this))
					{
						_subscribed.RemoveAt(num);
					}
				}
				else if (!_subscribed[num].PauseForConcurrency(this, fadeVolume))
				{
					_subscribed.RemoveAt(num);
				}
			}
			return;
		}
		for (int num2 = _subscribed.Count - 1; num2 > MaxCount - 1; num2--)
		{
			if (!_subscribed[num2].StopForConcurrency(this))
			{
				_subscribed.RemoveAt(num2);
			}
		}
	}

	private void StopInAudible()
	{
		if (CanPause)
		{
			return;
		}
		int num = _subscribed.Count - 1;
		while (num >= 0)
		{
			if (!_subscribed[num].InAudibleRange && !_subscribed[num].StopForConcurrency(this))
			{
				_subscribed.RemoveAt(num);
			}
			if (_subscribed.Count > MaxCount)
			{
				num--;
				continue;
			}
			break;
		}
	}

	private void StopNew()
	{
		if (CanPause)
		{
			return;
		}
		for (int num = _subscribed.Count - 1; num > MaxCount - 1; num--)
		{
			if (!_subscribed[num].StopForConcurrency(this))
			{
				_subscribed.RemoveAt(num);
			}
		}
	}

	private void StopLowPriorityThenOld()
	{
		int num = 0;
		for (int num2 = _subscribed.Count - 1; num2 > 0; num2--)
		{
			if (_subscribed[num2].priority > _subscribed[num2 - 1].priority)
			{
				num = num2;
				break;
			}
			if (num2 == 1)
			{
				break;
			}
		}
		for (int i = num; i < _subscribed.Count; i++)
		{
			if (!_subscribed[i].StopForConcurrency(this))
			{
				_subscribed.RemoveAt(i);
			}
			if (_subscribed.Count <= MaxCount)
			{
				break;
			}
		}
	}
}
