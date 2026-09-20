using System;
using System.Collections.Generic;

[Serializable]
public class AnimKeyFrameCollection : IKeyFrameCollection
{
	public List<ObjectAnimData> AnimData = new List<ObjectAnimData>();

	public void Apply()
	{
		foreach (ObjectAnimData animDatum in AnimData)
		{
			animDatum.Apply();
		}
	}

	public bool IsValid()
	{
		foreach (ObjectAnimData animDatum in AnimData)
		{
			if (!animDatum.IsValid())
			{
				return false;
			}
		}
		return true;
	}

	public bool MoveTowards(float speed)
	{
		bool result = true;
		foreach (ObjectAnimData animDatum in AnimData)
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
			AnimData[i].Lerp(((AnimKeyFrameCollection)other).AnimData[i], t);
		}
	}

	public void SetAnimDataValue(int index, KeyFrameData newValues)
	{
		if (index < AnimData.Count)
		{
			AnimData[index].SetKeyframeValues(newValues);
		}
	}

	public KeyFrameData GetAnimDataValue(int index)
	{
		if (index >= AnimData.Count)
		{
			return default(KeyFrameData);
		}
		ObjectAnimData objectAnimData = AnimData[index];
		return new KeyFrameData
		{
			Position = objectAnimData.position,
			Rotation = objectAnimData.rotation,
			Scale = objectAnimData.scale
		};
	}
}
