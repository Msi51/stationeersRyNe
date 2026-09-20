using System.Xml.Serialization;
using UnityEngine;

namespace Trading;

public class SlotIdReference : IChecksum
{
	[XmlAttribute("Value")]
	public string SlotId = string.Empty;

	[XmlIgnore]
	public int SlotIdHash;

	public int GetChecksum()
	{
		return Animator.StringToHash(SlotId);
	}

	public void Initialise()
	{
		SlotIdHash = Animator.StringToHash(SlotId);
	}
}
