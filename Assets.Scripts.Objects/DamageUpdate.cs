using System.Xml.Serialization;

namespace Assets.Scripts.Objects;

[XmlRoot]
public struct DamageUpdate(IndestructableDamageState damageState)
{
	public float Brute = damageState?.Brute ?? 0f;

	public float Burn = damageState?.Burn ?? 0f;

	public float Oxygen = damageState?.Oxygen ?? 0f;

	public float Hydration = damageState?.Hydration ?? 0f;

	public float Starvation = damageState?.Starvation ?? 0f;

	public float Toxic = damageState?.Toxic ?? 0f;

	public float Radiation = damageState?.Radiation ?? 0f;

	public float Stun = damageState?.Stun ?? 0f;

	public float Decay = damageState?.Decay ?? 0f;
}
