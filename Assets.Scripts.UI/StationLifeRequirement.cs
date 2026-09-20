using Assets.Scripts.Genetics;

namespace Assets.Scripts.UI;

public class StationLifeRequirement
{
	public string Name;

	public string Value;

	public string Gene;

	public int ValueSize = 18;

	public StationLifeRequirement()
	{
	}

	public StationLifeRequirement(string displayName, string value, Gene gene, int valueSize = 18)
	{
		string text = "Gene" + gene;
		Name = displayName;
		Value = "<color=orange>" + value + "</color>";
		Gene = "<link=" + text + "><color=green>" + GeneHelper.DisplayName(gene) + "</color></link>";
		ValueSize = valueSize;
	}
}
