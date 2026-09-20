using System.Xml.Serialization;

[XmlRoot]
public class OrbitSimulationSaveData
{
	public DoubleReference AccumulatedTime;

	public DoubleReference SimulationTime;

	public void Deserialize(OrbitalSimulation system)
	{
		if (SimulationTime != null)
		{
			OrbitalSimulation.SetSimulationTime(SimulationTime.Value);
		}
		else if (AccumulatedTime != null)
		{
			OrbitalSimulation.SetRealTime(AccumulatedTime.Value);
		}
		if (AccumulatedTime != null)
		{
			OrbitalSimulation.System.TotalRealTimeSeconds = AccumulatedTime.Value;
		}
	}
}
