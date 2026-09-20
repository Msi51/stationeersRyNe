using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;

public class HealingEffect : MedicalEffectBase, IHealEffectMoodle, IMedicalEffect
{
	public float HealAmount;

	public float Duration;

	public override MedicalEffectType TypeId => MedicalEffectType.Healing;

	public HealingEffect()
	{
	}

	public HealingEffect(float healAmount, float duration)
		: base(duration)
	{
		HealAmount = healAmount;
		Duration = duration;
	}

	public override void Update(float deltaTime)
	{
		base.Update(deltaTime);
		float minDamageRemaining = HealAmount / Duration * deltaTime;
		if (base.Human.DamageState.TotalRatio > 0.51f)
		{
			base.Human.DamageState.HealAll(minDamageRemaining);
		}
		foreach (Organ organ in base.Human.Organs)
		{
			if (organ.DamageState.TotalRatio > 0.51f)
			{
				organ.DamageState.HealAll(minDamageRemaining);
			}
		}
	}
}
