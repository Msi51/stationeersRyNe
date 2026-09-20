using Assets.Scripts;
using TerrainSystem;

namespace ThingImport;

public class MinablesGenerationResolutionTask : DataResolutionTask
{
	private MinablesGenerationData _data;

	public MinablesGenerationResolutionTask(MinablesGenerationData data)
	{
		_data = data;
	}

	public override void Resolve()
	{
		if (_data == null)
		{
			return;
		}
		for (int i = 0; i < _data.VeinData.Count; i++)
		{
			VeinGenerationData veinGenerationData = _data.VeinData[i];
			if (!veinGenerationData.IsValid())
			{
				_data.VeinData[i] = DataCollection.Get<VeinGenerationData>(veinGenerationData.Id);
			}
		}
	}
}
