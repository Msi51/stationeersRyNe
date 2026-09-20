using UnityEngine;

namespace Assets.Scripts.Util;

public static class DebugHelpers
{
	public static void DrawGizmoText(Vector3 worldspacePosition, string text, GUIStyle guiStyle, float maxDist = 5f)
	{
	}

	public static void DrawCube(Vector3 position, Quaternion rotation, Vector3? scale = null)
	{
	}

	public static void DrawCube(Transform transform)
	{
	}

	public static void DrawWireCube(Vector3 position, Quaternion rotation, Vector3? scale = null)
	{
	}

	public static void DrawWireCube(Transform transform)
	{
	}

	public static void DrawArrow(Vector3 pos, Vector3 direction, Color color, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20f)
	{
		Debug.DrawRay(pos, direction, color);
		Vector3 vector = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, 180f + arrowHeadAngle, 0f) * new Vector3(0f, 0f, 1f);
		Vector3 vector2 = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, 180f - arrowHeadAngle, 0f) * new Vector3(0f, 0f, 1f);
		Debug.DrawRay(pos + direction, vector * arrowHeadLength, color);
		Debug.DrawRay(pos + direction, vector2 * arrowHeadLength, color);
	}

	public static void DrawArrow(Vector3 pos, Vector3 direction, float duration, Color color, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20f)
	{
		Debug.DrawRay(pos, direction, color);
		Vector3 vector = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, 180f + arrowHeadAngle, 0f) * new Vector3(0f, 0f, 1f);
		Vector3 vector2 = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, 180f - arrowHeadAngle, 0f) * new Vector3(0f, 0f, 1f);
		Debug.DrawRay(pos + direction, vector * arrowHeadLength, color, duration);
		Debug.DrawRay(pos + direction, vector2 * arrowHeadLength, color, duration);
	}

	public static void DrawArrowGizmo(Vector3 pos, Vector3 direction, float arrowPosition = 0.5f, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20f)
	{
		Gizmos.DrawRay(pos, direction);
		DrawArrowEndsGizmo(pos, direction, arrowPosition, arrowHeadLength, arrowHeadAngle);
	}

	private static void DrawArrowEndsGizmo(Vector3 pos, Vector3 direction, float arrowPosition = 0.5f, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20f)
	{
		Quaternion quaternion = Quaternion.LookRotation(direction);
		Vector3 direction2 = quaternion * Quaternion.Euler(arrowHeadAngle, 0f, 0f) * Vector3.back * arrowHeadLength / 2f;
		Vector3 direction3 = quaternion * Quaternion.Euler(0f - arrowHeadAngle, 0f, 0f) * Vector3.back * arrowHeadLength / 2f;
		Vector3 direction4 = quaternion * Quaternion.Euler(0f, arrowHeadAngle, 0f) * Vector3.back * arrowHeadLength / 2f;
		Vector3 direction5 = quaternion * Quaternion.Euler(0f, 0f - arrowHeadAngle, 0f) * Vector3.back * arrowHeadLength / 2f;
		Vector3 vector = pos + direction * arrowPosition;
		Gizmos.DrawRay(vector, direction2);
		Gizmos.DrawRay(vector, direction3);
		Gizmos.DrawRay(vector, direction4);
		Gizmos.DrawRay(vector, direction5);
	}

	public static void DrawWireCube(Vector3 bottomLeftCorner, float size, Color color, float duration = 0f)
	{
		Vector3 vector = size * Vector3.one;
		Vector3 center = bottomLeftCorner + vector / 2f;
		Vector3 halfExtents = vector / 2f;
		DrawWireCube(center, halfExtents, Quaternion.identity, color, duration);
	}

	public static void DrawWireCube(Vector3 center, Vector3 halfExtents, Quaternion rotation, Color color, float duration = 0f)
	{
		Vector3 vector = new Vector3(halfExtents.x, 0f, 0f);
		Vector3 vector2 = new Vector3(0f, halfExtents.y, 0f);
		Vector3 vector3 = new Vector3(0f, 0f, halfExtents.z);
		Vector3 start = center + rotation * (vector + vector2 + vector3);
		Vector3 vector4 = center + rotation * (vector + vector2 - vector3);
		Vector3 vector5 = center + rotation * (vector - vector2 + vector3);
		Vector3 vector6 = center + rotation * (vector - vector2 - vector3);
		Vector3 vector7 = center + rotation * (-vector + vector2 + vector3);
		Vector3 vector8 = center + rotation * (-vector + vector2 - vector3);
		Vector3 vector9 = center + rotation * (-vector - vector2 + vector3);
		Vector3 end = center + rotation * (-vector - vector2 - vector3);
		Debug.DrawLine(start, vector4, color, duration);
		Debug.DrawLine(start, vector5, color, duration);
		Debug.DrawLine(start, vector7, color, duration);
		Debug.DrawLine(vector4, vector6, color, duration);
		Debug.DrawLine(vector4, vector8, color, duration);
		Debug.DrawLine(vector5, vector6, color, duration);
		Debug.DrawLine(vector5, vector9, color, duration);
		Debug.DrawLine(vector6, end, color, duration);
		Debug.DrawLine(vector7, vector8, color, duration);
		Debug.DrawLine(vector7, vector9, color, duration);
		Debug.DrawLine(vector8, end, color, duration);
		Debug.DrawLine(vector9, end, color, duration);
	}
}
