using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using Networks;
using UnityEngine;

namespace Trading;

public class GasAction : ActionData, IGasTrade
{
	private const string TYPE_ATTRIBUTE = "Type";

	[XmlAttribute("Type")]
	public Chemistry.GasType Type;

	private const string MOLES_ATTRIBUTE = "Moles";

	[XmlAttribute("Moles")]
	public float Moles = float.NaN;

	private const string LITRES_ATTRIBUTE = "Litres";

	[XmlAttribute("Litres")]
	public float Litres = float.NaN;

	private const string CELSIUS_ATTRIBUTE = "Celsius";

	[XmlAttribute("Celsius")]
	public float Celsius = float.NaN;

	private const string KELVIN_ATTRIBUTE = "Kelvin";

	[XmlAttribute("Kelvin")]
	public float Kelvin = float.NaN;

	private const string ENERGY_ATTRIBUTE = "Energy";

	[XmlAttribute("Energy")]
	public float Energy = float.NaN;

	private const string X_ELEMENT_NAME = "Gas";

	public override string XElementName => "Gas";

	public Chemistry.GasType GetGasType()
	{
		return Type;
	}

	public static List<GasAction> CreateGasActions(Atmosphere atmosphere)
	{
		if (atmosphere.TotalMoles.Equals(MoleQuantity.Zero))
		{
			return null;
		}
		return new List<GasAction>();
	}

	public override int GetChecksum()
	{
		return (int)(((((((((((uint)Type ^ (uint)(int)Moles) * 41) ^ (uint)((!float.IsNaN(Celsius)) ? ((int)Celsius) : 0)) * 41) ^ (uint)((!float.IsNaN(Kelvin)) ? ((int)Kelvin) : 0)) * 41) ^ (uint)((!float.IsNaN(Litres)) ? ((int)Litres) : 0)) * 41) ^ (uint)((!float.IsNaN(Energy)) ? ((int)Energy) : 0)) * 41);
	}

	public override void Add(ref XElement parentElement, string actionElementName)
	{
		if (Type != Chemistry.GasType.Undefined && (!float.IsNaN(Moles) || !float.IsNaN(Litres)) && (!float.IsNaN(Celsius) || !float.IsNaN(Kelvin) || !float.IsNaN(Energy)))
		{
			XElement element = XDocumentHelper.MakeElement(actionElementName, ref parentElement);
			XDocumentHelper.SetAttribute(element, "Type", Type.GetXmlEnumAttributeValueFromEnum());
			if (!float.IsNaN(Moles))
			{
				XDocumentHelper.SetAttribute(element, "Moles", Moles.ToString(CultureInfo.InvariantCulture));
			}
			if (!float.IsNaN(Litres))
			{
				XDocumentHelper.SetAttribute(element, "Litres", Litres.ToString(CultureInfo.InvariantCulture));
			}
			if (!float.IsNaN(Celsius))
			{
				XDocumentHelper.SetAttribute(element, "Celsius", Celsius.ToString(CultureInfo.InvariantCulture));
			}
			if (!float.IsNaN(Kelvin))
			{
				XDocumentHelper.SetAttribute(element, "Kelvin", Kelvin.ToString(CultureInfo.InvariantCulture));
			}
			if (!float.IsNaN(Energy))
			{
				XDocumentHelper.SetAttribute(element, "Energy", Energy.ToString(CultureInfo.InvariantCulture));
			}
		}
	}

	public override bool Execute<T>(T t, Entity player)
	{
		if (float.IsNaN(Celsius) && float.IsNaN(Kelvin) && float.IsNaN(Energy))
		{
			return false;
		}
		TemperatureKelvin temperature = (float.IsNaN(Celsius) ? new TemperatureKelvin(Kelvin) : RocketMath.CelsiusToKelvin(Celsius));
		MoleQuantity quantity = MoleQuantity.Zero;
		GasMixture gasMixture = GasMixtureHelper.Create();
		if (!float.IsNaN(Moles))
		{
			quantity = new MoleQuantity(Moles);
		}
		else if (!float.IsNaN(Litres))
		{
			quantity = IdealGas.Quantity(new VolumeLitres(Litres), Mole.MolarVolume(Type));
		}
		MoleEnergy zero = MoleEnergy.Zero;
		gasMixture.Add(new Mole(energy: float.IsNaN(Energy) ? IdealGas.Energy(temperature, Mole.SpecificHeat(Type), quantity) : new MoleEnergy(Energy), gasType: Type, quantity: quantity));
		if (t is Thing { InternalAtmosphere: not null } thing)
		{
			AtmosphericEventInstance.CreateAdd(thing.InternalAtmosphere, gasMixture);
		}
		else if (t is Thing thing2 && thing2 is INetworkedAtmospherics networkedAtmospherics)
		{
			Atmosphere atmosphere = ((AtmosphericsNetwork)networkedAtmospherics.StructureNetwork)?.Atmosphere;
			if (atmosphere != null)
			{
				AtmosphericEventInstance.CreateAdd(atmosphere, gasMixture);
			}
		}
		else if (t is Atmosphere atmosphere2)
		{
			AtmosphericEventInstance.CreateAdd(atmosphere2, gasMixture);
		}
		else if (typeof(T) == typeof(WorldGrid))
		{
			WorldGrid worldGrid = __refvalue(__makeref(t), WorldGrid);
			if (worldGrid != WorldGrid.INVALID)
			{
				AtmosphericEventInstance.CreateEmptyAndAdd(worldGrid, gasMixture);
			}
		}
		return true;
	}

	public override bool Execute(ref GasMixture tradable, int totalQuantitySold)
	{
		if (float.IsNaN(Celsius) && float.IsNaN(Kelvin))
		{
			return false;
		}
		TemperatureKelvin temperature = (float.IsNaN(Celsius) ? new TemperatureKelvin(Kelvin) : RocketMath.CelsiusToKelvin(Celsius));
		GasMixture newGasMix = GasMixtureHelper.Create();
		MoleQuantity moleQuantity = MoleQuantity.Zero;
		if (!float.IsNaN(Moles))
		{
			moleQuantity = new MoleQuantity(Moles);
		}
		else if (!float.IsNaN(Litres))
		{
			moleQuantity = new MoleQuantity(Mathf.Round(Litres / Mole.MolarVolume(Type).ToFloat()));
		}
		MoleQuantity quantity = moleQuantity * totalQuantitySold;
		newGasMix.Add(new Mole(Type, quantity, IdealGas.Energy(temperature, Mole.SpecificHeat(Type), quantity)));
		tradable.Add(newGasMix);
		return true;
	}

	public override void ToolTip(StringBuilder stringBuilder, int generations, Thing prefab = null)
	{
		for (int i = 0; i < generations; i++)
		{
			stringBuilder.Append("    ");
		}
		stringBuilder.Append(Localization.GetName(Type).AsColor("#44AD83"));
		stringBuilder.Append(" x ");
		if (!float.IsNaN(Moles))
		{
			stringBuilder.Append(Moles.ToStringPrefix("mol", "yellow"));
		}
		else if (!float.IsNaN(Litres))
		{
			MoleQuantity moleQuantity = IdealGas.Quantity(new VolumeLitres(Litres), Mole.MolarVolume(Type));
			stringBuilder.Append(Litres.ToStringPrefix("L", "yellow"));
			stringBuilder.Append(" ").Append(moleQuantity.ToFloat().ToStringPrefix("mol"));
		}
		if (!float.IsNaN(Celsius))
		{
			stringBuilder.Append(" at ");
			stringBuilder.Append(Celsius.ToStringPrefix("°C", "yellow"));
		}
		else if (!float.IsNaN(Kelvin))
		{
			stringBuilder.Append(" at ");
			stringBuilder.Append(Kelvin.ToStringPrefix("K", "yellow"));
		}
		stringBuilder.AppendLine();
	}
}
