using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace Assets.Scripts;

public class TraderSlotWorldData
{
	[XmlAttribute("Operator")]
	public LogicOperator Operator = LogicOperator.Any;

	[XmlElement("Id")]
	public List<string> Ids = new List<string>();

	public bool IsValid()
	{
		if (string.IsNullOrEmpty(WorldSetting.Current.Id))
		{
			return true;
		}
		int num = Animator.StringToHash(WorldSetting.Current.Id);
		switch (Operator)
		{
		case LogicOperator.All:
			return true;
		case LogicOperator.Any:
			foreach (string id in Ids)
			{
				if (num == Animator.StringToHash(id))
				{
					return true;
				}
			}
			return false;
		case LogicOperator.None:
			foreach (string id2 in Ids)
			{
				if (num == Animator.StringToHash(id2))
				{
					return false;
				}
			}
			return true;
		default:
			return true;
		}
	}
}
