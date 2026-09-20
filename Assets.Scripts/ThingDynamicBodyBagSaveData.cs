using System;
using Assets.Scripts.Objects;
using CharacterCustomisation;

namespace Assets.Scripts;

[Serializable]
public class ThingDynamicBodyBagSaveData : DynamicThingSaveData
{
	public DamageUpdate BodyDamageState;

	public PlayerCosmetics BodyCosmeticData;

	public string PlayerDisplayName;
}
