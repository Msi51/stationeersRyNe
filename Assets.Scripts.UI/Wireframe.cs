using System;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.UI;

public class Wireframe : MonoBehaviour
{
	public List<Edge> WireframeEdges = new List<Edge>();

	public Bounds BlueprintBounds;

	public Transform BlueprintTransform;

	public MeshFilter BlueprintMeshFilter;

	public Renderer BlueprintRenderer;

	public Material LineMaterial;

	public Vector3 OrientationArrowOffset;

	private ArrowStyle OrientationArrowStyle = ArrowStyle.NarrowCross;

	public static Color ColorBlueAlpha = Color.blue.SetAlpha(0.3f);

	private bool _redraw;

	private Grid3 _lastGrid;

	private Grid3 _currentGrid;

	private Vector3 _lastPosition;

	private Vector3 _currentPosition;

	private Quaternion _lastRotation;

	private Quaternion _currentRotation;

	private Vector3 TransformPoint(Vector3 point)
	{
		return BlueprintTransform.position + BlueprintTransform.rotation * BlueprintBounds.center + InputHelpers.RotatePointAroundPivot(point, Vector3.zero, BlueprintTransform.rotation.eulerAngles);
	}

	private void CreateLineMaterial()
	{
		if (!LineMaterial)
		{
			Shader shader = Shader.Find("Hidden/Internal-Colored");
			LineMaterial = new Material(shader);
			LineMaterial.hideFlags = HideFlags.HideAndDontSave;
			LineMaterial.SetInt("_SrcBlend", 5);
			LineMaterial.SetInt("_DstBlend", 10);
			LineMaterial.SetInt("_Cull", 2);
			LineMaterial.SetInt("_ZWrite", 1);
		}
	}

	private void DestroyChildren(Transform tran)
	{
		for (int num = tran.childCount - 1; num >= 0; num--)
		{
			DestroyChildren(tran.GetChild(num).gameObject.transform);
			UnityEngine.Object.Destroy(tran.GetChild(num).gameObject);
		}
	}

	public virtual void OnDestroy()
	{
		DestroyChildren(base.transform);
		UnityEngine.Object.Destroy(BlueprintMeshFilter);
		UnityEngine.Object.Destroy(LineMaterial);
		UnityEngine.Object.Destroy(BlueprintRenderer);
		WireframeEdges.Clear();
	}

	public void OnRenderObject()
	{
		if (Camera.current == CameraController.Instance.StormCardCamera)
		{
			return;
		}
		CreateLineMaterial();
		LineMaterial.SetPass(0);
		_currentPosition = BlueprintTransform.position;
		_currentRotation = BlueprintTransform.rotation;
		_redraw = _currentPosition != _lastPosition || _currentRotation != _lastRotation;
		GL.Begin(1);
		GL.Color(BlueprintRenderer.material.color.SetAlpha(InventoryManager.Instance.CursorAlphaLine));
		foreach (Edge wireframeEdge in WireframeEdges)
		{
			if (_redraw)
			{
				wireframeEdge.CachedPoint1 = TransformPoint(wireframeEdge.Point1);
				wireframeEdge.CachedPoint2 = TransformPoint(wireframeEdge.Point2);
				_lastPosition = _currentPosition;
				_lastRotation = _currentRotation;
			}
			DrawLine(wireframeEdge.CachedPoint1, wireframeEdge.CachedPoint2);
		}
		if (OrientationArrowOffset != Vector3.zero)
		{
			DrawArrow(_currentPosition + _currentRotation * OrientationArrowOffset, BlueprintTransform.up * BlueprintBounds.max.y, 0.25f, 20f, ArrowStyle.BroadCross, BlueprintTransform.forward);
		}
		GL.End();
	}

	private static void DrawLine(Vector3 v1, Vector3 v2)
	{
		GL.Vertex3(v1.x, v1.y, v1.z);
		GL.Vertex3(v2.x, v2.y, v2.z);
	}

	public static void DrawArrow(Vector3 pos, Vector3 direction, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20f, ArrowStyle style = ArrowStyle.Narrow, Vector3 upHint = default(Vector3))
	{
		Vector3 vector = pos + direction;
		Quaternion quaternion = Quaternion.LookRotation(direction, (upHint == Vector3.zero) ? Vector3.up : upHint);
		switch (style)
		{
		case ArrowStyle.Narrow:
		{
			DrawLine(pos, pos + direction);
			Vector3 vector7 = quaternion * Quaternion.Euler(0f, 180f + arrowHeadAngle, 0f) * Vector3.forward;
			Vector3 vector8 = quaternion * Quaternion.Euler(0f, 180f - arrowHeadAngle, 0f) * Vector3.forward;
			DrawLine(vector, vector + vector7 * arrowHeadLength);
			DrawLine(vector, vector + vector8 * arrowHeadLength);
			break;
		}
		case ArrowStyle.NarrowCross:
		{
			DrawLine(pos, pos + direction);
			Vector3 vector3 = quaternion * Quaternion.Euler(0f, 180f + arrowHeadAngle, 0f) * Vector3.forward;
			Vector3 vector4 = quaternion * Quaternion.Euler(0f, 180f - arrowHeadAngle, 0f) * Vector3.forward;
			Vector3 vector5 = quaternion * Quaternion.Euler(180f + arrowHeadAngle, 0f, 0f) * Vector3.forward;
			Vector3 vector6 = quaternion * Quaternion.Euler(180f - arrowHeadAngle, 0f, 0f) * Vector3.forward;
			DrawLine(vector, vector + vector3 * arrowHeadLength);
			DrawLine(vector, vector + vector4 * arrowHeadLength);
			DrawLine(vector, vector + vector5 * arrowHeadLength);
			DrawLine(vector, vector + vector6 * arrowHeadLength);
			break;
		}
		case ArrowStyle.Broad:
			DrawBroadArrow(pos, direction, arrowHeadLength, arrowHeadAngle, (upHint == Vector3.zero) ? Vector3.up : upHint);
			break;
		case ArrowStyle.BroadCross:
		{
			Vector3 vector2 = ((upHint == Vector3.zero) ? Vector3.up : upHint);
			DrawBroadArrow(pos, direction, arrowHeadLength, arrowHeadAngle, vector2);
			DrawBroadArrow(pos, direction, arrowHeadLength, arrowHeadAngle, Quaternion.AngleAxis(90f, direction.normalized) * vector2);
			break;
		}
		}
	}

	private static void DrawBroadArrow(Vector3 pos, Vector3 direction, float arrowHeadLength, float arrowHeadAngle, Vector3 upHint)
	{
		Quaternion quaternion = Quaternion.LookRotation(direction, upHint);
		float num = arrowHeadLength * 0.2f;
		Vector3 vector = quaternion * Vector3.right;
		Vector3 normalized = direction.normalized;
		Vector3 vector2 = pos + direction;
		Vector3 vector3 = quaternion * Quaternion.Euler(0f, 180f - arrowHeadAngle, 0f) * Vector3.forward;
		Vector3 vector4 = quaternion * Quaternion.Euler(0f, 180f + arrowHeadAngle, 0f) * Vector3.forward;
		Vector3 v = vector2 + vector3 * arrowHeadLength;
		Vector3 v2 = vector2 + vector4 * arrowHeadLength;
		float num2 = arrowHeadLength * Mathf.Cos(arrowHeadAngle * (MathF.PI / 180f));
		Vector3 vector5 = vector2 - normalized * num2 + vector * num;
		Vector3 vector6 = vector2 - normalized * num2 - vector * num;
		DrawLine(pos + vector * num, vector5);
		DrawLine(pos - vector * num, vector6);
		DrawLine(pos + vector * num, pos - vector * num);
		DrawLine(vector6, v2);
		DrawLine(vector5, v);
		DrawLine(vector2, v);
		DrawLine(vector2, v2);
	}
}
