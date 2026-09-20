using System;
using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Entities;

[Serializable]
public enum EntityState : byte
{
	[XmlEnum("Alive")]
	Alive,
	[XmlEnum("Dead")]
	Dead,
	[XmlEnum("Unconscious")]
	Unconscious,
	[XmlEnum("Decay")]
	Decay
}
