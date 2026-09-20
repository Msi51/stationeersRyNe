using Assets.Scripts.Inventory;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.UI;

public class CursorBoundingLines : MonoBehaviour
{
	public int lineCount = 100;

	public float radius = 3f;

	public Renderer CursorRenderer;

	public Material lineMaterial;

	private Vector3 v3FrontTopLeft;

	private Vector3 v3FrontTopRight;

	private Vector3 v3FrontBottomLeft;

	private Vector3 v3FrontBottomRight;

	private Vector3 v3BackTopLeft;

	private Vector3 v3BackTopRight;

	private Vector3 v3BackBottomLeft;

	private Vector3 v3BackBottomRight;

	private Vector3 v3Center;

	private Vector3 v3Extents;

	private Bounds Bounds;

	private void Awake()
	{
		CursorRenderer = GetComponent<Renderer>();
	}

	private void CreateLineMaterial()
	{
		if (!lineMaterial)
		{
			Shader shader = Shader.Find("Hidden/Internal-Colored");
			lineMaterial = new Material(shader);
			lineMaterial.hideFlags = HideFlags.HideAndDontSave;
			lineMaterial.SetInt("_SrcBlend", 5);
			lineMaterial.SetInt("_DstBlend", 10);
			lineMaterial.SetInt("_Cull", 2);
			lineMaterial.SetInt("_ZWrite", 1);
		}
	}

	private void OnDrawGizmosSelected()
	{
		CursorRenderer = GetComponent<Renderer>();
		PopulatePositions(CursorRenderer);
		Gizmos.DrawLine(v3FrontTopLeft, v3FrontTopRight);
		Gizmos.DrawLine(v3FrontTopRight, v3FrontBottomRight);
		Gizmos.DrawLine(v3FrontBottomRight, v3FrontBottomLeft);
		Gizmos.DrawLine(v3FrontBottomLeft, v3FrontTopLeft);
		Gizmos.DrawLine(v3BackTopLeft, v3BackTopRight);
		Gizmos.DrawLine(v3BackTopRight, v3BackBottomRight);
		Gizmos.DrawLine(v3BackBottomRight, v3BackBottomLeft);
		Gizmos.DrawLine(v3BackBottomLeft, v3BackTopLeft);
		Gizmos.DrawLine(v3FrontTopLeft, v3BackTopLeft);
		Gizmos.DrawLine(v3FrontTopRight, v3BackTopRight);
		Gizmos.DrawLine(v3FrontBottomLeft, v3BackBottomLeft);
		Gizmos.DrawLine(v3FrontBottomRight, v3BackBottomRight);
	}

	private void PopulatePositions(Renderer cursor)
	{
		Bounds = new Bounds(Vector3.zero, base.transform.localScale);
		v3Center = Bounds.center;
		v3Extents = Bounds.extents;
		v3FrontTopLeft = base.transform.position + base.transform.rotation * Bounds.center + InputHelpers.RotatePointAroundPivot(new Vector3(v3Center.x - v3Extents.x, v3Center.y + v3Extents.y, v3Center.z - v3Extents.z), v3Center, base.transform.rotation.eulerAngles);
		v3FrontTopRight = base.transform.position + base.transform.rotation * Bounds.center + InputHelpers.RotatePointAroundPivot(new Vector3(v3Center.x + v3Extents.x, v3Center.y + v3Extents.y, v3Center.z - v3Extents.z), v3Center, base.transform.rotation.eulerAngles);
		v3FrontBottomLeft = base.transform.position + base.transform.rotation * Bounds.center + InputHelpers.RotatePointAroundPivot(new Vector3(v3Center.x - v3Extents.x, v3Center.y - v3Extents.y, v3Center.z - v3Extents.z), v3Center, base.transform.rotation.eulerAngles);
		v3FrontBottomRight = base.transform.position + base.transform.rotation * Bounds.center + InputHelpers.RotatePointAroundPivot(new Vector3(v3Center.x + v3Extents.x, v3Center.y - v3Extents.y, v3Center.z - v3Extents.z), v3Center, base.transform.rotation.eulerAngles);
		v3BackTopLeft = base.transform.position + base.transform.rotation * Bounds.center + InputHelpers.RotatePointAroundPivot(new Vector3(v3Center.x - v3Extents.x, v3Center.y + v3Extents.y, v3Center.z + v3Extents.z), v3Center, base.transform.rotation.eulerAngles);
		v3BackTopRight = base.transform.position + base.transform.rotation * Bounds.center + InputHelpers.RotatePointAroundPivot(new Vector3(v3Center.x + v3Extents.x, v3Center.y + v3Extents.y, v3Center.z + v3Extents.z), v3Center, base.transform.rotation.eulerAngles);
		v3BackBottomLeft = base.transform.position + base.transform.rotation * Bounds.center + InputHelpers.RotatePointAroundPivot(new Vector3(v3Center.x - v3Extents.x, v3Center.y - v3Extents.y, v3Center.z + v3Extents.z), v3Center, base.transform.rotation.eulerAngles);
		v3BackBottomRight = base.transform.position + base.transform.rotation * Bounds.center + InputHelpers.RotatePointAroundPivot(new Vector3(v3Center.x + v3Extents.x, v3Center.y - v3Extents.y, v3Center.z + v3Extents.z), v3Center, base.transform.rotation.eulerAngles);
	}

	public void OnRenderObject()
	{
		if (!(Camera.current == CameraController.Instance.StormCardCamera))
		{
			CreateLineMaterial();
			lineMaterial.SetPass(0);
			GL.PushMatrix();
			GL.Begin(1);
			GL.Color(CursorRenderer.material.color.SetAlpha(InventoryManager.Instance.CursorAlphaLine));
			PopulatePositions(CursorRenderer);
			DrawLine(v3FrontTopLeft, v3FrontTopRight);
			DrawLine(v3FrontTopRight, v3FrontBottomRight);
			DrawLine(v3FrontBottomRight, v3FrontBottomLeft);
			DrawLine(v3FrontBottomLeft, v3FrontTopLeft);
			DrawLine(v3BackTopLeft, v3BackTopRight);
			DrawLine(v3BackTopRight, v3BackBottomRight);
			DrawLine(v3BackBottomRight, v3BackBottomLeft);
			DrawLine(v3BackBottomLeft, v3BackTopLeft);
			DrawLine(v3FrontTopLeft, v3BackTopLeft);
			DrawLine(v3FrontTopRight, v3BackTopRight);
			DrawLine(v3FrontBottomLeft, v3BackBottomLeft);
			DrawLine(v3FrontBottomRight, v3BackBottomRight);
			GL.End();
			GL.PopMatrix();
		}
	}

	private void DrawLine(Vector3 v1, Vector3 v2)
	{
		GL.Vertex3(v1.x, v1.y, v1.z);
		GL.Vertex3(v2.x, v2.y, v2.z);
	}
}
