using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects;
using ImGuiNET;

public class GlobalGasMixData
{
	[XmlElement("Gas")]
	public List<GlobalMoleData> GlobalMoleDatas = new List<GlobalMoleData>();

	private const string ADD = "+";

	public GlobalGasMixData()
	{
	}

	public GlobalGasMixData(GlobalGasMixData globalGasMixData)
	{
		if (globalGasMixData == null)
		{
			return;
		}
		foreach (GlobalMoleData globalMoleData in globalGasMixData.GlobalMoleDatas)
		{
			GlobalMoleDatas.Add(GlobalMoleData.Create(globalMoleData));
		}
	}

	public GlobalGasMixData(List<SpawnGas> spawnGasses)
	{
		foreach (SpawnGas spawnGass in spawnGasses)
		{
			GlobalMoleDatas.Add(GlobalMoleData.Create(spawnGass));
		}
	}

	public void Clear()
	{
		GlobalMoleDatas.Clear();
	}

	public void Draw()
	{
		if (ImGui.Button("+"))
		{
			AddMoleData();
		}
		for (int num = GlobalMoleDatas.Count - 1; num >= 0; num--)
		{
			GlobalMoleDatas[num].Draw(this);
		}
	}

	public bool Apply()
	{
		bool result = false;
		foreach (GlobalMoleData globalMoleData in GlobalMoleDatas)
		{
			if (globalMoleData.ApplyStringValues())
			{
				result = true;
			}
		}
		return result;
	}

	private void AddMoleData()
	{
		foreach (GlobalMoleData globalMoleData in GlobalMoleDatas)
		{
			if (globalMoleData.Type == Chemistry.GasType.Undefined)
			{
				return;
			}
		}
		GlobalMoleDatas.Add(new GlobalMoleData());
	}

	public void Init()
	{
		foreach (GlobalMoleData globalMoleData in GlobalMoleDatas)
		{
			globalMoleData.InitStringValues();
		}
	}
}
