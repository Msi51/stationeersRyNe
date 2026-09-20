using System;
using System.Collections.Generic;

[Serializable]
public class MaterialKeyFrameCollection : IKeyFrameCollection
{
	public List<MaterialAnimData> AnimData = new List<MaterialAnimData>();

	public void Apply()
	{
		foreach (MaterialAnimData animDatum in AnimData)
		{
			animDatum.Apply();
		}
	}

	public bool MoveTowards(float speed)
	{
		bool result = true;
		foreach (MaterialAnimData animDatum in AnimData)
		{
			if (!animDatum.MoveTowards(speed))
			{
				result = false;
			}
		}
		return result;
	}

	public void Lerp(IKeyFrameCollection other, float t)
	{
		for (int i = 0; i < AnimData.Count; i++)
		{
			AnimData[i].Lerp(((MaterialKeyFrameCollection)other).AnimData[i], t);
		}
	}
}
