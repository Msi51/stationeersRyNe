using System;
using System.Runtime.CompilerServices;
using Assets.Scripts.Objects;
using ImGuiNET;
using UnityEngine;

namespace Assets.Scripts.UI.ImGuiUi;

public class ImGuiExtensions
{
	public static class Rendering
	{
		public static float LineThickness = 1f;

		public static uint RenderingColor = ImGuiColor.Integer.DefaultColor;

		public static void DrawTextInWorld(string text, Vector3 position, int id = -1, float cullDistance = 3.5f)
		{
			if (Camera.main == null)
			{
				return;
			}
			float z = Camera.main.WorldToViewportPoint(position).z;
			if (z < 0f || z > cullDistance)
			{
				return;
			}
			Vector3 screenSpacePoint = WorldToScreen(position);
			if (string.IsNullOrEmpty(text) || IsPointBehindCamera(ref screenSpacePoint))
			{
				return;
			}
			if (ImGui.Begin($"TextInWorld{id}", (ImGuiWindowFlags)799693))
			{
				if (id != -1)
				{
					ImGui.PushID(id);
				}
				ImGui.SetWindowPos(WorldToImGui(position) - ImGui.GetWindowSize() * 0.5f, ImGuiCond.Always);
				ImGui.PushStyleColor(ImGuiCol.Text, RenderingColor);
				ImGui.Text(text);
				ImGui.PopStyleColor();
				if (id != -1)
				{
					ImGui.PopID();
				}
			}
			ImGui.End();
		}

		public static void DrawTextInWorld(Action action, Vector3 position, int id = -1, float cullDistance = 3.5f)
		{
			if (Camera.main == null || action == null)
			{
				return;
			}
			float z = Camera.main.WorldToViewportPoint(position).z;
			if (z < 0f || z > cullDistance)
			{
				return;
			}
			Vector3 screenSpacePoint = WorldToScreen(position);
			if (IsPointBehindCamera(ref screenSpacePoint))
			{
				return;
			}
			if (ImGui.Begin($"TextInWorld{id}", (ImGuiWindowFlags)799693))
			{
				if (id != -1)
				{
					ImGui.PushID(id);
				}
				ImGui.SetWindowPos(WorldToImGui(position) - ImGui.GetWindowSize() * 0.5f, ImGuiCond.Always);
				action();
				if (id != -1)
				{
					ImGui.PopID();
				}
			}
			ImGui.End();
		}

		public static void DrawCross(Vector2 worldPos, float radius)
		{
			DrawClippedLine(WorldToScreen(worldPos - Vector2.left * radius), WorldToScreen(worldPos + Vector2.left * radius));
			DrawClippedLine(WorldToScreen(worldPos - Vector2.up * radius), WorldToScreen(worldPos + Vector2.up * radius));
		}

		public static void DrawArrow(Vector3 pos, Vector3 direction)
		{
			if (!(direction == Vector3.zero))
			{
				Vector3 vector = WorldToScreen(pos);
				Vector3 position = pos + direction;
				WorldToScreen(position);
				float num = position.x - pos.x;
				float num2 = position.y - pos.y;
				float num3 = Mathf.Sqrt(num * num + num2 * num2);
				num /= num3;
				DrawArrowhead(ny: 0f - num2 / num3, p: vector, nx: 0f - num);
			}
		}

		private static void DrawArrowhead(Vector2 p, float nx, float ny, float length = 0.25f)
		{
			float num = length * (0f - ny - nx);
			float num2 = length * (nx - ny);
			DrawClippedLine(WorldToScreen(new Vector2(p.x + num, p.y + num2)), p);
			DrawClippedLine(WorldToScreen(new Vector2(p.x - num2, p.y + num)), p);
		}

		public static void DrawCubeBounds(Vector3 min, Vector3 max)
		{
			Vector3 t = WorldToScreen(new Vector3(min.x, max.y, min.z));
			Vector3 t2 = WorldToScreen(new Vector3(max.x, max.y, min.z));
			Vector3 t3 = WorldToScreen(new Vector3(max.x, max.y, max.z));
			Vector3 t4 = WorldToScreen(new Vector3(min.x, max.y, max.z));
			Vector3 b = WorldToScreen(new Vector3(min.x, min.y, min.z));
			Vector3 b2 = WorldToScreen(new Vector3(max.x, min.y, min.z));
			Vector3 b3 = WorldToScreen(new Vector3(max.x, min.y, max.z));
			Vector3 b4 = WorldToScreen(new Vector3(min.x, min.y, max.z));
			DrawCubeLines(t, t2, t3, t4, b, b2, b3, b4);
		}

		public static void DrawCube(Vector3 center, Vector3 size, string text)
		{
			RenderingColor = ImGuiColor.Integer.White;
			DrawTextInWorld(text, Vector3.zero, 0, float.MaxValue);
			DrawCube(center, size);
		}

		public static void DrawCube(Vector3 center, Vector3 size)
		{
			Vector3 vector = size / 2f;
			Vector3 vector2 = center - vector;
			Vector3 vector3 = center + vector;
			Vector3 t = WorldToScreen(new Vector3(vector2.x, vector3.y, vector2.z));
			Vector3 t2 = WorldToScreen(new Vector3(vector3.x, vector3.y, vector2.z));
			Vector3 t3 = WorldToScreen(new Vector3(vector3.x, vector3.y, vector3.z));
			Vector3 t4 = WorldToScreen(new Vector3(vector2.x, vector3.y, vector3.z));
			Vector3 b = WorldToScreen(new Vector3(vector2.x, vector2.y, vector2.z));
			Vector3 b2 = WorldToScreen(new Vector3(vector3.x, vector2.y, vector2.z));
			Vector3 b3 = WorldToScreen(new Vector3(vector3.x, vector2.y, vector3.z));
			Vector3 b4 = WorldToScreen(new Vector3(vector2.x, vector2.y, vector3.z));
			DrawCubeLines(t, t2, t3, t4, b, b2, b3, b4);
		}

		public static void DrawLineSimple(Vector3 start, Vector3 end)
		{
			Vector3 screenSpace = WorldToScreen(start);
			Vector3 screenSpace2 = WorldToScreen(end);
			DrawClippedLine(screenSpace, screenSpace2);
		}

		public static void DrawWireSphere(Vector3 center, float radius)
		{
			int num = 32;
			Vector3 vector2 = default(Vector3);
			for (int i = 0; i < 3; i++)
			{
				Vector3 vector = Vector3.zero;
				for (int j = 0; j <= num; j++)
				{
					float f = MathF.PI * 2f * (float)j / (float)num;
					float num2 = radius * Mathf.Cos(f);
					float num3 = radius * Mathf.Sin(f);
					switch (i)
					{
					case 0:
						vector2 = new Vector3(num2, num3, 0f);
						break;
					case 1:
						vector2 = new Vector3(num2, 0f, num3);
						break;
					case 2:
						vector2 = new Vector3(0f, num2, num3);
						break;
					default:
						global::_003CPrivateImplementationDetails_003E.ThrowSwitchExpressionException(i);
						break;
					}
					Vector3 vector3 = vector2;
					vector3 += center;
					if (j > 0)
					{
						Vector3 screenSpace = WorldToScreen(new Vector3(vector.x, vector.y, vector.z));
						Vector3 screenSpace2 = WorldToScreen(new Vector3(vector3.x, vector3.y, vector3.z));
						DrawClippedLine(screenSpace, screenSpace2);
					}
					vector = vector3;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void DrawCubeLines(Vector3 t1, Vector3 t2, Vector3 t3, Vector3 t4, Vector3 b1, Vector3 b2, Vector3 b3, Vector3 b4)
		{
			DrawQuadLines(t1, t2, t3, t4);
			DrawClippedLine(t1, b1);
			DrawClippedLine(t2, b2);
			DrawClippedLine(t3, b3);
			DrawClippedLine(t4, b4);
			DrawQuadLines(b1, b2, b3, b4);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void DrawQuadLines(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
		{
			DrawClippedLine(p1, p2);
			DrawClippedLine(p2, p3);
			DrawClippedLine(p3, p4);
			DrawClippedLine(p4, p1);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void DrawClippedLine(Vector3 screenSpace1, Vector3 screenSpace2)
		{
			if (IsLineBehindCamera(ref screenSpace1, ref screenSpace2))
			{
				DrawLine(screenSpace1, screenSpace2);
			}
		}

		public static void DrawLine(Vector3 screenSpace1, Vector3 screenSpace2)
		{
			ImGui.GetBackgroundDrawList().AddLine(screenSpace1, screenSpace2, RenderingColor, LineThickness);
		}

		public static void DrawClippedDottedLine(Vector3 worldPos1, Vector3 worldPos2, int nDots = 3)
		{
			Vector3 screenSpace = WorldToScreen(worldPos1);
			Vector3 screenSpace2 = WorldToScreen(worldPos2);
			if (IsLineBehindCamera(ref screenSpace, ref screenSpace2))
			{
				Span<Vector3> span = stackalloc Vector3[1024];
				int num = 0;
				Vector3 vector = worldPos1;
				float num2 = 1f / (float)nDots;
				float num3 = num2;
				for (int i = 0; i <= nDots; i++)
				{
					span[num++] = vector;
					vector = Vector3.Lerp(vector, worldPos2, num3);
					num3 += num2;
				}
				for (int j = 0; j <= num / 2; j += 2)
				{
					DrawLine(WorldToScreen(span[j]), WorldToScreen(span[j + 1]));
				}
			}
		}

		public static void DrawClippedDottedLine(Vector3 worldPos1, Vector3 worldPos2, Vector3 direction, float length = 0.1f, int nDots = 3)
		{
			Span<Vector3> span = stackalloc Vector3[1024];
			int num = 0;
			Vector3 vector = worldPos1;
			float num2 = 1f / (float)nDots;
			float num3 = num2;
			for (int i = 0; i <= nDots; i++)
			{
				span[num++] = vector;
				vector = Vector3.Lerp(vector, worldPos2, num3);
				num3 += num2;
			}
			for (int j = 0; j <= num / 2; j += 2)
			{
				DrawClippedLine(WorldToScreen(span[j] + direction * length), WorldToScreen(span[j] - direction * length));
				DrawClippedLine(WorldToScreen(span[j + 1] + direction * length), WorldToScreen(span[j + 1] - direction * length));
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsLineBehindCamera(ref Vector3 screenSpace1, ref Vector3 screenSpace2)
		{
			if (IsPointBehindCamera(ref screenSpace1) || IsPointBehindCamera(ref screenSpace2))
			{
				return false;
			}
			return true;
		}

		public static bool IsPointBehindCamera(ref Vector3 screenSpacePoint)
		{
			if (screenSpacePoint.z <= 0f)
			{
				return true;
			}
			return false;
		}
	}

	public static Vector2 GetScreenCenter()
	{
		return new Vector2(Screen.width, Screen.height) * 0.5f;
	}

	public static Vector3 WorldToScreen(Vector3 position, bool flipY = true)
	{
		if ((object)Camera.main != null)
		{
			Vector3 result = Camera.main.WorldToScreenPoint(position, Camera.MonoOrStereoscopicEye.Mono);
			if (flipY)
			{
				result.y = 0f - (result.y - (float)Screen.height);
			}
			return result;
		}
		return default(Vector3);
	}

	public static Vector2 WorldToImGui(Vector3 position)
	{
		return ImGuiUn.ScreenToImGui((Vector2)WorldToScreen(position, flipY: false));
	}

	public static uint GetThingColour(Thing thing)
	{
		return ImGui.ColorConvertFloat4ToU32(thing.CustomColor.Color);
	}

	public static ImGuiStylePtr GetFontStyle()
	{
		return ImGui.GetStyle();
	}

	public static uint ColorConvertFloat4ToU32(Color lerp)
	{
		return ImGui.ColorConvertFloat4ToU32(lerp);
	}
}
