using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;

public class StimEffect : MedicalEffectBase, IStimEffectMoodle, IMedicalEffect
{
	public float Amount;

	public float Duration;

	public override MedicalEffectType TypeId => MedicalEffectType.Stim;

	public StimEffect()
	{
	}

	public StimEffect(float duration, float amount)
		: base(duration)
	{
		Amount = amount;
		Duration = duration;
	}

	public override void OnApplied()
	{
		base.OnApplied();
		base.Human.DamageState.Damage(ChangeDamageType.Decrement, Amount, DamageUpdateType.Stun);
	}

	public override void Update(float deltaTime)
	{
		base.Update(deltaTime);
		base.Human.DamageState.Damage(ChangeDamageType.Decrement, Amount / Duration * deltaTime, DamageUpdateType.Stun);
	}
}
