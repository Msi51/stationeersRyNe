using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Util;
using Trading;
using UnityEngine;

namespace Assets.Scripts;

public class TraderSlotTraderSelectData
{
	[XmlAttribute("Operator")]
	public LogicOperator Operator = LogicOperator.Any;

	[XmlElement("Id")]
	public List<string> Ids = new List<string>();

	public TraderData Select()
	{
		if (Operator == LogicOperator.All)
		{
			return TraderData.AllTraderData.Pick();
		}
		List<TraderData> list = new List<TraderData>(TraderData.AllTraderData);
		for (int num = list.Count - 1; num >= 0; num--)
		{
			bool flag = Operator != LogicOperator.Any;
			foreach (string id in Ids)
			{
				if (Animator.StringToHash(id) == list[num].IdHash)
				{
					flag = Operator == LogicOperator.Any;
				}
			}
			if (!flag)
			{
				list.RemoveAt(num);
			}
		}
		return list.Pick();
	}
}
