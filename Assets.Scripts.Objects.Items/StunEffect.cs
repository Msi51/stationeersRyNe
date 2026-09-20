namespace Assets.Scripts.Objects.Items;

public class StunEffect : MedicalEffectBase, IStunEffectMoodle, IMedicalEffect
{
	public float Amount;

	public float Duration;

	public override MedicalEffectType TypeId => MedicalEffectType.Stun;

	public StunEffect()
	{
	}

	public StunEffect(float duration, float amount)
		: base(duration)
	{
		Amount = amount;
		Duration = duration;
	}

	public override void OnApplied()
	{
		base.OnApplied();
		base.Human.DamageState.Damage(ChangeDamageType.Increment, Amount, DamageUpdateType.Stun);
	}

	public override void Update(float deltaTime)
	{
		base.Update(deltaTime);
		base.Human.DamageState.Damage(ChangeDamageType.Increment, Amount / Duration * deltaTime, DamageUpdateType.Stun);
	}
}
