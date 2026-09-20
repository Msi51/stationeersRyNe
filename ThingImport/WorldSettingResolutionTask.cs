using Assets.Scripts;
using Objects.Rockets;
using TerrainSystem;
using UnityEngine;

namespace ThingImport;

public class WorldSettingResolutionTask : DataResolutionTask
{
	private WorldSettingData _data;

	public WorldSettingResolutionTask(WorldSettingData data)
	{
		_data = data;
	}

	public override void Resolve()
	{
		if (_data == null)
		{
			return;
		}
		for (int num = _data.WeatherEvents.Count - 1; num >= 0; num--)
		{
			WeatherEvent weatherEvent = _data.WeatherEvents[num];
			if (!weatherEvent.IsValid())
			{
				_data.WeatherEvents[num] = DataCollection.Get<WeatherEvent>(weatherEvent.Id);
			}
		}
		if (_data.SpaceMapData != null && !_data.SpaceMapData.IsValid())
		{
			_data.SpaceMapData = DataCollection.Get<SpaceMapData>(_data.SpaceMapData.Id);
		}
		for (int num2 = _data.RegionSets.Count - 1; num2 >= 0; num2--)
		{
			RegionSet regionSet = _data.RegionSets[num2];
			if (!regionSet.IsValid())
			{
				_data.RegionSets[num2] = DataCollection.Get<RegionSet>(regionSet.IdHash);
			}
		}
		RegionSet regionSet2 = _data.GeographicRegionData?.RegionSet;
		if (regionSet2 != null && !regionSet2.IsValid())
		{
			_data.GeographicRegionData.RegionSet = DataCollection.Get<RegionSet>(regionSet2.IdHash);
		}
		RegionSet regionSet3 = _data.DeepMinablesRegionData?.RegionSet;
		if (regionSet3 != null && !regionSet3.IsValid())
		{
			_data.DeepMinablesRegionData.RegionSet = DataCollection.Get<RegionSet>(regionSet3.IdHash);
		}
		foreach (MinablesGenerationData minablesDatum in _data.MinablesData)
		{
			if (minablesDatum != null && !minablesDatum.IsValid())
			{
				MinablesGenerationData minablesGenerationData = DataCollection.Get<MinablesGenerationData>(minablesDatum.Id);
				if (minablesGenerationData == null)
				{
					ConsoleWindow.PrintError("Can't resolve minables vein data for " + minablesGenerationData.Id);
				}
				else
				{
					minablesDatum.VeinData = minablesGenerationData.VeinData;
				}
			}
		}
		foreach (DeepMinablesGenerationData deepMinablesDatum in _data.DeepMinablesData)
		{
			if (deepMinablesDatum == null || deepMinablesDatum.IsValid())
			{
				continue;
			}
			DeepMinablesGenerationData deepMinablesGenerationData = DataCollection.Get<DeepMinablesGenerationData>(deepMinablesDatum.Id);
			if (deepMinablesGenerationData == null)
			{
				ConsoleWindow.PrintError("Can't find target Deep Minables " + _data.Id);
				continue;
			}
			DeepMinablesGenerationData deepMinablesGenerationData2 = deepMinablesDatum;
			if (deepMinablesGenerationData2.ReagentAction == null)
			{
				deepMinablesGenerationData2.ReagentAction = deepMinablesGenerationData.ReagentAction;
			}
			deepMinablesGenerationData2 = deepMinablesDatum;
			if (deepMinablesGenerationData2.Quantity == null)
			{
				deepMinablesGenerationData2.Quantity = deepMinablesGenerationData.Quantity;
			}
			deepMinablesGenerationData2 = deepMinablesDatum;
			if (deepMinablesGenerationData2.Time == null)
			{
				deepMinablesGenerationData2.Time = deepMinablesGenerationData.Time;
			}
		}
	}
}
