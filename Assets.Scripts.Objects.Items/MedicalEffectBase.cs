using System.Xml.Serialization;
using Assets.Scripts.Objects.Entities;

namespace Assets.Scripts.Objects.Items;

[XmlInclude(typeof(StunEffect))]
[XmlInclude(typeof(StimEffect))]
[XmlInclude(typeof(HealingEffect))]
public abstract class MedicalEffectBase : IMedicalEffect
{
	public float Remaining;

	[XmlIgnore]
	public Human Human { get; set; }

	public abstract MedicalEffectType TypeId { get; }

	public float GetRemaining()
	{
		return Remaining;
	}

	protected MedicalEffectBase()
	{
	}

	protected MedicalEffectBase(float duration)
	{
		Remaining = duration;
	}

	public virtual void OnApplied()
	{
	}

	public virtual void Update(float deltaTime)
	{
		Remaining -= deltaTime;
		if (Remaining <= 0f)
		{
			Human.RemoveEffect(this);
		}
	}

	public static MedicalEffectBase Create(MedicalEffectType type, float remaining)
	{
		MedicalEffectBase medicalEffectBase = type switch
		{
			MedicalEffectType.Stun => new StunEffect(), 
			MedicalEffectType.Stim => new StimEffect(), 
			MedicalEffectType.Healing => new HealingEffect(), 
			_ => null, 
		};
		if (medicalEffectBase != null)
		{
			medicalEffectBase.Remaining = remaining;
		}
		return medicalEffectBase;
	}
}
