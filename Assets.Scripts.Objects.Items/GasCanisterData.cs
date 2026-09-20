using System.Collections.Generic;
using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Items;

[XmlInclude(typeof(ThingModData))]
public class GasCanisterData : ItemModData
{
	public float MaxPressure = float.NaN;

	public float Litres = float.NaN;

	public float PressurePerTick = float.NaN;

	public List<SpawnGas> SpawnContents;
}
