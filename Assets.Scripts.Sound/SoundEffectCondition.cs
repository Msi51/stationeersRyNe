using System;
using Assets.Scripts.Objects;

namespace Assets.Scripts.Sound;

[Serializable]
public class SoundEffectCondition
{
	[ReadOnly]
	public InteractableType Type;

	[ReadOnly]
	public int Value;
}
