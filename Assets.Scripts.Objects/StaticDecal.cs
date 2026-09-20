using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class StaticDecal : Thing
{
	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		DynamicThing sourceItem = attack.SourceItem;
		if (!sourceItem)
		{
			return null;
		}
		if (!(sourceItem as Cleaner))
		{
			return base.AttackWith(attack, doAction);
		}
		DelayedActionInstance result = new DelayedActionInstance
		{
			Duration = 1f,
			ActionMessage = "Clean"
		};
		if (doAction)
		{
			OnServer.Destroy(this);
		}
		return result;
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteVector3(base.transform.forward);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Vector3 forward = reader.ReadVector3();
		if (GameManager.GameState == GameState.Running)
		{
			ThingTransform.forward = forward;
		}
	}
}
