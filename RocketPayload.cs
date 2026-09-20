using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Objects.Rockets.Scanning;
using Trading;
using UnityEngine;

public abstract class RocketPayload : DraggableThing, IRocketPayload, IReferencable, IEvaluable
{
	private const float DEFAULT_MASS = 200f;

	private const float TIME_TO_DEPLOY = 20f;

	public Collider MainCollider;

	private const int SEARCH_RANGE = 2;

	public virtual float MassContribution => 200f;

	public virtual float TimeToDeploy => 20f;

	public abstract void OnDeploy();

	public abstract RocketActionResult CanDeploy();

	private IPayloadMount FindPayloadMount()
	{
		IPayloadMount payloadMount = SmallCell.Get<IPayloadMount>(CenterPosition);
		if (payloadMount == null)
		{
			for (int i = -2; i < 2; i++)
			{
				for (int j = -2; j < 2; j++)
				{
					for (int k = -2; k < 2; k++)
					{
						IPayloadMount payloadMount2 = SmallCell.Get<IPayloadMount>(CenterPosition + new Vector3(i, j, k) * 0.5f);
						if (payloadMount2 != null)
						{
							float num = Vector3.SqrMagnitude(payloadMount2.Position - CenterPosition);
							if (payloadMount == null || num < Vector3.SqrMagnitude(payloadMount.Position - CenterPosition))
							{
								payloadMount = payloadMount2;
							}
						}
					}
				}
			}
		}
		return payloadMount;
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if ((bool)base.Joint)
		{
			return base.AttackWith(attack, doAction);
		}
		if (attack.SourceItem is Wrench)
		{
			IPayloadMount payloadMount = FindPayloadMount();
			if (payloadMount == null && base.ParentSlot == null)
			{
				return base.AttackWith(attack, doAction);
			}
			DelayedActionInstance result = new DelayedActionInstance
			{
				Duration = 1f,
				ActionMessage = ((base.ParentSlot != null) ? ActionStrings.Disconnect : ActionStrings.Connect)
			};
			if (!doAction || !GameManager.RunSimulation)
			{
				return result;
			}
			if (base.ParentSlot != null)
			{
				OnServer.MoveToWorld(this);
			}
			else if (payloadMount != null)
			{
				OnServer.MoveToSlot(this, payloadMount.PayloadSlot);
			}
			return result;
		}
		return base.AttackWith(attack, doAction);
	}
}
