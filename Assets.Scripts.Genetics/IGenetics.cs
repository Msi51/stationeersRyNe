using System.Collections.Generic;

namespace Assets.Scripts.Genetics;

public interface IGenetics
{
	static List<IGenetics> AllGeneticsList;

	static IGenetics()
	{
		AllGeneticsList = new List<IGenetics>();
	}
}
