using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Trading;

public class CursorThingCondition : ConditionData
{
	[XmlAttribute("Id")]
	public string PrefabId;

	[XmlIgnore]
	public int PrefabIdHash;

	public override void Initialise()
	{
		base.Initialise();
		PrefabIdHash = Animator.StringToHash(PrefabId);
	}

	public override int GetChecksum()
	{
		base.GetChecksum();
		return Animator.StringToHash(PrefabId) * 41;
	}

	public override bool Evaluate<T>(T t)
	{
		bool flag = false;
		if (t is Thing && GameManager.RunSimulation && !GameManager.IsBatchMode && CursorManager.CursorThing != null && CursorManager.CursorThing.PrefabHash == PrefabIdHash)
		{
			flag = true;
		}
		if (flag)
		{
			return base.Evaluate(t);
		}
		return false;
	}
}
