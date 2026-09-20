using System;
using Assets.Scripts.Atmospherics;

namespace Objects.Structures;

[Serializable]
public class GasQuantity
{
	public Chemistry.GasType GasType;

	public float Moles;

	public float TemperatureC;
}
