using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rendering;

[Serializable]
public class SpatialBucket
{
	public List<DrawCall> DrawCalls = new List<DrawCall>();

	private DrawCall FindOrAddDrawCall(DrawData drawData)
	{
		DrawCall result = FindDrawCall(drawData);
		if (FindDrawCall(drawData) != null)
		{
			return result;
		}
		result = new DrawCall(drawData);
		DrawCalls.Add(result);
		return result;
	}

	private DrawCall FindDrawCall(DrawData drawData)
	{
		foreach (DrawCall drawCall in DrawCalls)
		{
			if (drawCall.IsSameDrawData(drawData))
			{
				return drawCall;
			}
		}
		return null;
	}

	public void SetFloat(DrawData drawData, Matrix4x4 localToWorldTransform, int propertyID, float value)
	{
		FindDrawCall(drawData)?.SetFloat(localToWorldTransform, propertyID, value);
	}

	public void SetVector(DrawData drawData, Matrix4x4 localToWorldTransform, int propertyID, Vector4 value)
	{
		FindDrawCall(drawData)?.SetVector(localToWorldTransform, propertyID, value);
	}

	public void Register(DrawData drawData, Matrix4x4 localToWorldTransform)
	{
		FindOrAddDrawCall(drawData).Register(localToWorldTransform);
	}

	public void Deregister(DrawData drawData, Matrix4x4 localToWorldTransform)
	{
		for (int i = 0; i < DrawCalls.Count; i++)
		{
			DrawCall drawCall = DrawCalls[i];
			if (drawCall.IsSameDrawData(drawData))
			{
				drawCall.Deregister(localToWorldTransform);
				if (drawCall.instanceCount <= 0)
				{
					DrawCalls.RemoveAt(i);
				}
				break;
			}
		}
	}

	public void Draw()
	{
		foreach (DrawCall drawCall in DrawCalls)
		{
			drawCall.Draw();
		}
	}
}
