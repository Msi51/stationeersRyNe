using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Assets.Scripts.Genetics;

[Serializable]
[XmlRoot("GeneCollection")]
public class GeneCollectionWrapper
{
	public string PlantCustomName;

	public string PlanterCustomName;

	public List<GeneWrapper> GeneWrappers;

	public GeneCollectionWrapper()
	{
		GeneWrappers = new List<GeneWrapper>();
	}

	public GeneCollectionWrapper(List<GeneWrapper> geneWrappers)
	{
		GeneWrappers = new List<GeneWrapper>(geneWrappers.Count);
		for (int i = 0; i < geneWrappers.Count; i++)
		{
			GeneWrapper geneWrapper = geneWrappers[i];
			GeneWrappers[i] = new GeneWrapper(geneWrapper.Gene, geneWrapper.Value, geneWrapper.Stability);
		}
	}
}
