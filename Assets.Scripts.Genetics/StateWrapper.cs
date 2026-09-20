using System;
using System.Xml.Serialization;
using Assets.Scripts.Objects.Items;

namespace Assets.Scripts.Genetics;

[Serializable]
[XmlRoot("State")]
public class StateWrapper
{
	public PlantStatusType State;

	public float Value;

	public StateWrapper()
	{
	}

	public StateWrapper(PlantStatusType statusType, float value)
	{
		State = statusType;
		Value = value;
	}
}
