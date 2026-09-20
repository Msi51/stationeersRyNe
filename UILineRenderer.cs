using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UILineRenderer : MonoBehaviour
{
	[SerializeField]
	private GameObject linePrefab;

	public void DrawLines(List<Vector2> points, Color color, float thickness = 2f)
	{
		for (int i = 0; i < points.Count - 1; i++)
		{
			DrawLine(points[i], points[i + 1], color, thickness);
		}
	}

	public RectTransform DrawLine(Vector2 pointA, Vector2 pointB, Color color, float thickness)
	{
		Vector2 normalized = (pointB - pointA).normalized;
		float x = Vector2.Distance(pointA, pointB);
		GameObject obj = Object.Instantiate(linePrefab, base.transform);
		obj.GetComponent<Image>().color = color;
		RectTransform component = obj.GetComponent<RectTransform>();
		component.sizeDelta = new Vector2(x, thickness);
		component.pivot = new Vector2(0f, 0.5f);
		component.localPosition = pointA;
		component.right = normalized;
		return component;
	}

	public void ClearLines()
	{
		foreach (Transform item in base.transform)
		{
			Object.Destroy(item.gameObject);
		}
	}
}
