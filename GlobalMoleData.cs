using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using ImGuiNET;
using UI.ImGuiUi;

public class GlobalMoleData
{
	[XmlAttribute("Type")]
	public Chemistry.GasType Type;

	[XmlAttribute("Quantity")]
	public float Quantity;

	[XmlIgnore]
	private string _quantityString;

	private const string GAS_TYPE = "Type";

	private const string QUANTITY = "Quantity";

	private const string REMOVE = "-";

	public GlobalMoleData()
	{
	}

	public GlobalMoleData(Chemistry.GasType type, float quantity)
	{
		Type = type;
		Quantity = quantity;
		InitStringValues();
	}

	public GlobalMoleData(GlobalMoleData moleData)
	{
		Type = moleData.Type;
		Quantity = moleData.Quantity;
		InitStringValues();
	}

	public MoleQuantity ToMoleQuantity(VolumeLitres globalVolume)
	{
		return new MoleQuantity((double)Quantity * (globalVolume / Chemistry.GridVolume).ToDouble());
	}

	public static GlobalMoleData Create(GlobalMoleData moleData)
	{
		return new GlobalMoleData(moleData);
	}

	public static GlobalMoleData Create(SpawnGas spawnGas)
	{
		return new GlobalMoleData(spawnGas.Type, spawnGas.Quantity);
	}

	public void Draw(GlobalGasMixData parentGlobalGasMix)
	{
		if (ImGui.CollapsingHeader(EnumCollections.GasTypes.GetName(Type)))
		{
			ImguiHelper.DrawCombo("Type", ref Type, EnumCollections.GasTypes);
			ImguiHelper.DrawInput("Quantity", ref _quantityString);
			if (ImGui.Button("-"))
			{
				parentGlobalGasMix.GlobalMoleDatas.Remove(this);
			}
		}
	}

	public void InitStringValues()
	{
		_quantityString = StringManager.Get(Quantity);
	}

	public bool ApplyStringValues()
	{
		bool result = false;
		if (float.TryParse(_quantityString, out var result2))
		{
			if (Quantity != result2)
			{
				result = true;
			}
			Quantity = result2;
		}
		return result;
	}
}
