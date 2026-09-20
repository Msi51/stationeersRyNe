using System;
using System.Xml.Serialization;
using Assets.Scripts.Objects.Items;

namespace Assets.Scripts.Genetics;

[Serializable]
[XmlRoot("CurrentState")]
public class BooleanStateWrapper
{
	public PlantStatusType State;

	public bool Value;

	public BooleanStateWrapper()
	{
	}

	public BooleanStateWrapper(PlantStatusType statusType, bool value)
	{
		State = statusType;
		Value = value;
	}
}
