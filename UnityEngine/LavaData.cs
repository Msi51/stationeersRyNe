using System;
using System.Xml.Serialization;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using TerrainSystem;
using ThingImport;

namespace UnityEngine;

public class LavaData
{
	public const int LavaMeshSize = 256;

	[XmlAttribute("MinHeight")]
	public int MinHeight = 1;

	[XmlAttribute("MaxHeight")]
	public int MaxHeight = 1;

	private ushort[] _heightData;

	public static bool IsDebugLavaHeight;

	[XmlElement("HeightTexture")]
	public TextureReference LavaHeight { get; set; }

	public LavaData()
	{
	}

	public LavaData(Texture2D lavaHeightMap, int minHeight, int maxHeight)
	{
		MinHeight = minHeight;
		MaxHeight = maxHeight;
		InitArray(lavaHeightMap);
	}

	public void Initialize(ModAbout mod)
	{
		LavaHeight?.Load();
		Texture2D texture2D = LavaHeight?.Texture;
		if ((bool)texture2D)
		{
			if (!RocketMath.IsPowerOfTwo(texture2D.width))
			{
				throw new Exception(mod?.Name + " LavaTexture size must be a power of two");
			}
			InitArray(texture2D);
		}
	}

	private void InitArray(Texture2D tex)
	{
		int width = tex.width;
		int height = tex.height;
		int num = width / 256;
		Color[] pixels = tex.GetPixels();
		_heightData = new ushort[65536];
		for (int i = 0; i < height; i += num)
		{
			for (int j = 0; j < width; j += num)
			{
				float u = (float)j / (float)width;
				float v = (float)i / (float)height;
				float pixelBilinear = GetPixelBilinear(pixels, width, height, u, v);
				_heightData[i / num * 256 + j / num] = (ushort)(pixelBilinear * (float)(MaxHeight - MinHeight) + (float)MinHeight);
			}
		}
		Array.Clear(pixels, 0, pixels.Length);
	}

	public bool IsUnderLava(Vector3 worldPosition)
	{
		return GetLavaHeight(worldPosition) > worldPosition.y;
	}

	public static void DebugLavaHeight(Vector3 worldPosition)
	{
		if (!IsDebugLavaHeight)
		{
			return;
		}
		ushort[] array = WorldSetting.Current?.Data?.TerrainSettings?.LavaData?._heightData;
		if (array != null)
		{
			Vector3 vector = worldPosition;
			worldPosition += (Vector3)VoxelConstants.OriginOffsetInt;
			if (!(worldPosition.x < 0f) && !(worldPosition.z < 0f) && !(worldPosition.x >= (float)VoxelConstants.Size) && !(worldPosition.z >= (float)VoxelConstants.Size))
			{
				float num = worldPosition.x / (float)VoxelConstants.Size;
				float num2 = worldPosition.z / (float)VoxelConstants.Size;
				float num3 = num * 256f;
				float num4 = num2 * 256f;
				int num5 = Mathf.Clamp((int)num3, 0, 255);
				int num6 = Mathf.Clamp((int)num4, 0, 255);
				int num7 = Mathf.Min(num5 + 1, 255);
				int num8 = Mathf.Min(num6 + 1, 255);
				float t = num3 - (float)num5;
				float t2 = num4 - (float)num6;
				float num9 = (int)array[num6 * 256 + num5];
				float num10 = (int)array[num6 * 256 + num7];
				float num11 = (int)array[num8 * 256 + num5];
				float num12 = (int)array[num8 * 256 + num7];
				int num13 = VoxelConstants.Size / 256;
				Vector3 screenSpace = new Vector3(num5 * num13 - VoxelConstants.Offset, num9, num6 * num13 - VoxelConstants.Offset);
				Vector3 vector2 = new Vector3(num7 * num13 - VoxelConstants.Offset, num10, num6 * num13 - VoxelConstants.Offset);
				Vector3 vector3 = new Vector3(num5 * num13 - VoxelConstants.Offset, num11, num8 * num13 - VoxelConstants.Offset);
				Vector3 screenSpace2 = new Vector3(num7 * num13 - VoxelConstants.Offset, num12, num8 * num13 - VoxelConstants.Offset);
				ImGuiExtensions.Rendering.DrawClippedLine(screenSpace, vector2);
				ImGuiExtensions.Rendering.DrawClippedLine(vector3, screenSpace2);
				ImGuiExtensions.Rendering.DrawClippedLine(screenSpace, vector3);
				ImGuiExtensions.Rendering.DrawClippedLine(vector2, screenSpace2);
				float a = Mathf.Lerp(num9, num10, t);
				float b = Mathf.Lerp(num11, num12, t);
				float num14 = Mathf.Lerp(a, b, t2);
				Vector3 vector4 = new Vector3(vector.x, num14, vector.z);
				ImGuiExtensions.Rendering.DrawCube(vector4, Vector3.one);
				ImGuiExtensions.Rendering.DrawTextInWorld($"height: {num14}", vector4, 10);
			}
		}
	}

	public float GetLavaHeight(Vector3 worldPosition)
	{
		if (_heightData == null)
		{
			return 0f;
		}
		worldPosition += (Vector3)VoxelConstants.OriginOffsetInt;
		if (worldPosition.x < 0f || worldPosition.z < 0f || worldPosition.x >= (float)VoxelConstants.Size || worldPosition.z >= (float)VoxelConstants.Size)
		{
			return 0f;
		}
		float num = worldPosition.x / (float)VoxelConstants.Size;
		float num2 = worldPosition.z / (float)VoxelConstants.Size;
		float num3 = num * 256f;
		float num4 = num2 * 256f;
		int num5 = Mathf.Clamp((int)num3, 0, 255);
		int num6 = Mathf.Clamp((int)num4, 0, 255);
		int num7 = Mathf.Min(num5 + 1, 255);
		int num8 = Mathf.Min(num6 + 1, 255);
		float t = num3 - (float)num5;
		float t2 = num4 - (float)num6;
		float a = (int)_heightData[num6 * 256 + num5];
		float b = (int)_heightData[num6 * 256 + num7];
		float a2 = (int)_heightData[num8 * 256 + num5];
		float b2 = (int)_heightData[num8 * 256 + num7];
		float a3 = Mathf.Lerp(a, b, t);
		float b3 = Mathf.Lerp(a2, b2, t);
		return Mathf.Lerp(a3, b3, t2);
	}

	public int GetLavaHeightForMeshGeneration(int x, int z)
	{
		int num = z * 256 + x;
		return _heightData[num];
	}

	public static float GetPixelBilinear(Color[] pixels, int texWidth, int texHeight, float u, float v)
	{
		u = Mathf.Clamp01(u);
		v = Mathf.Clamp01(v);
		float num = u * (float)(texWidth - 1);
		float num2 = v * (float)(texHeight - 1);
		int num3 = (int)num;
		int num4 = (int)num2;
		int num5 = Mathf.Min(num3 + 1, texWidth - 1);
		int num6 = Mathf.Min(num4 + 1, texHeight - 1);
		float t = num - (float)num3;
		float t2 = num2 - (float)num4;
		float r = pixels[num4 * texWidth + num3].r;
		float r2 = pixels[num4 * texWidth + num5].r;
		float r3 = pixels[num6 * texWidth + num3].r;
		float r4 = pixels[num6 * texWidth + num5].r;
		float a = Mathf.Lerp(r, r2, t);
		float b = Mathf.Lerp(r3, r4, t);
		return Mathf.Lerp(a, b, t2);
	}
}
