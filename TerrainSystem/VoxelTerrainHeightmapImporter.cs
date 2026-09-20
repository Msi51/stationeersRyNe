using System;
using System.Collections.Generic;
using System.Diagnostics;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using ImGuiNET;
using UI.ImGuiUi;
using UnityEngine;

namespace TerrainSystem;

public static class VoxelTerrainHeightmapImporter
{
	public enum HeightmapImportTask
	{
		None,
		CopyTextureToArray,
		CopyCrustTypeTextureToArray,
		CopyCrackTextureToArray,
		CopyLavaHeightMapToArray,
		GenerateRegionFromHeightMap,
		FillJobs
	}

	public class CrackParameter
	{
		public float Frequency;

		public float Amplitude;

		public float Size;

		public CrackParameter(float frequency, float amplitude, float size)
		{
			Frequency = frequency;
			Amplitude = amplitude;
			Size = size;
		}

		public CrackParameter()
		{
		}

		public void Draw(int index)
		{
			ImGui.SliderFloat($"Frequency###freq{index}", ref Frequency, 0.01f, 1f);
			ImGui.SliderFloat($"Amplitude###amp{index}", ref Amplitude, 0f, 100f);
			ImGui.SliderFloat($"Size###size{index}", ref Size, -10f, 10f);
		}
	}

	public const int DEFAULT_TERRAIN_SIZE = 4096;

	public const int DEFAULT_BLUR_RADIUS = 1;

	public const float DEFAULT_MIN_TERRAIN_HEIGHT = 8f;

	public const float DEFAULT_MAX_TERRAIN_HEIGHT = 128f;

	private const int CRUST_SIZE = 6;

	public static float MINTerrainHeight = 8f;

	public static float MAXTerrainHeight = 128f;

	public static float MINLavaHeight = 8f;

	public static float MAXLavaHeight = 128f;

	public static int BlurRadius = 1;

	private static Vector2Int[] _blurOffsets;

	public static float CurrentProgress;

	public static HeightmapImportTask CurrentTask = HeightmapImportTask.None;

	private const int MILLI_SECONDS_TO_YIELD = 30;

	private const int EMISSIVE_LAVA_HEIGHT = 2;

	public static int CrackSeed = 5431759;

	public static CrackParameter CrackParam0 = new CrackParameter(0.055f, 30f, -10f);

	public static CrackParameter CrackParam1 = new CrackParameter(0.055f, 0.8f, 4f);

	public static float[,] HeightMapArray { get; private set; }

	public static float[,] ActualHeightArray { get; private set; }

	public static float[,] CrustTypeArray { get; private set; }

	public static float[,] CrackArray { get; private set; }

	public static float[,] LavaHeightMapArray { get; private set; }

	public static OpenSimplexNoise SimplexNoise { get; set; }

	public static void Init()
	{
		CreateBlurRadiusOffsets();
	}

	private static void CreateBlurRadiusOffsets()
	{
		int num = BlurRadius * 2 + 1;
		_blurOffsets = new Vector2Int[num * num];
		int num2 = 0;
		for (int i = -BlurRadius; i <= BlurRadius; i++)
		{
			for (int j = -BlurRadius; j <= BlurRadius; j++)
			{
				_blurOffsets[num2] = new Vector2Int(i, j);
				num2++;
			}
		}
	}

	public static float GetProgress()
	{
		return CurrentProgress * 100f;
	}

	public static async UniTaskVoid GenerateTask(Texture2D heightmap, Texture2D crustTypeMap, Texture2D crackMap, LavaData lavaData, VoxelOctree octree, VoxelNodeType fillType)
	{
		if (heightmap.width != heightmap.height)
		{
			throw new Exception("Texture must be square");
		}
		Stopwatch sw = new Stopwatch();
		sw.Start();
		if (HeightMapArray != null)
		{
			Array.Clear(HeightMapArray, 0, HeightMapArray.Length);
			HeightMapArray = null;
		}
		if (ActualHeightArray != null)
		{
			Array.Clear(ActualHeightArray, 0, ActualHeightArray.Length);
			ActualHeightArray = null;
		}
		if (CrustTypeArray != null)
		{
			Array.Clear(CrustTypeArray, 0, CrustTypeArray.Length);
			CrustTypeArray = null;
		}
		if (CrackArray != null)
		{
			Array.Clear(CrackArray, 0, CrackArray.Length);
			CrackArray = null;
		}
		if (LavaHeightMapArray != null)
		{
			Array.Clear(LavaHeightMapArray, 0, LavaHeightMapArray.Length);
			LavaHeightMapArray = null;
		}
		CurrentTask = HeightmapImportTask.CopyTextureToArray;
		await CopyTextureToArray(heightmap);
		PrintElapsedThenRestart(sw, "Texture copied to array");
		await UniTask.WaitForEndOfFrame();
		VoxelConstants.SetWorldSizeOffset(heightmap.width);
		if ((bool)crustTypeMap)
		{
			CurrentTask = HeightmapImportTask.CopyCrustTypeTextureToArray;
			await CopyCrustTypeTextureToArray(crustTypeMap);
			await UniTask.WaitForEndOfFrame();
		}
		if ((bool)crackMap)
		{
			CurrentTask = HeightmapImportTask.CopyCrackTextureToArray;
			await CopyCrackTextureToArray(crackMap);
			await UniTask.WaitForEndOfFrame();
			SimplexNoise = new OpenSimplexNoise(CrackSeed);
		}
		if (lavaData != null)
		{
			CurrentTask = HeightmapImportTask.CopyLavaHeightMapToArray;
			await CopyLavaHeightMapToArray(lavaData);
			await UniTask.WaitForEndOfFrame();
		}
		CurrentTask = HeightmapImportTask.GenerateRegionFromHeightMap;
		await GenerateRegionFromHeightMap(heightmap, octree);
		PrintElapsedThenRestart(sw, "Octree generated from heightmap");
		await UniTask.WaitForEndOfFrame();
		CurrentTask = HeightmapImportTask.FillJobs;
		AssignAndExecuteFillJobs(octree, fillType);
		PrintElapsedThenRestart(sw, "Fill jobs assigned");
		CurrentProgress = 0f;
		sw.Restart();
		while (FillOctreeWorker.IsAnyWorking())
		{
			float num = (float)sw.ElapsedMilliseconds / 1000f;
			CurrentProgress = num / (GetEstFillTimeSeconds(heightmap.width) + num);
			await UniTask.WaitForEndOfFrame();
		}
		CurrentProgress = 1f;
		PrintElapsedThenRestart(sw, "Fill jobs finished");
		await UniTask.WaitForEndOfFrame();
		CurrentTask = HeightmapImportTask.None;
		HeightMapArray = null;
		ActualHeightArray = null;
	}

	public static float GetEstFillTimeSeconds(float size)
	{
		if (size <= 1024f)
		{
			return 4f;
		}
		if (size <= 2048f)
		{
			return 16f;
		}
		if (size <= 4096f)
		{
			return 64f;
		}
		if (size <= 8192f)
		{
			return 256f;
		}
		return 1024f;
	}

	private static void PrintElapsedThenRestart(Stopwatch stopwatch, string message)
	{
		UnityEngine.Debug.Log($"{message} : {stopwatch.ElapsedMilliseconds} ms");
		stopwatch.Restart();
	}

	private static async UniTask CopyTextureToArray(Texture2D heightmap)
	{
		Stopwatch sw = new Stopwatch();
		CurrentProgress = 0f;
		int texSize = heightmap.width;
		HeightMapArray = new float[texSize, texSize];
		ActualHeightArray = new float[texSize, texSize];
		sw.Restart();
		for (int x = 0; x < texSize; x++)
		{
			for (int i = 0; i < texSize; i++)
			{
				HeightMapArray[x, i] = heightmap.GetPixel(x, i).r;
			}
			CurrentProgress = (float)x / (float)texSize;
			if (sw.ElapsedMilliseconds > 30)
			{
				await UniTask.Yield();
				sw.Restart();
			}
		}
	}

	private static async UniTask CopyCrustTypeTextureToArray(Texture2D texture)
	{
		Stopwatch sw = new Stopwatch();
		CurrentProgress = 0f;
		int texSize = texture.width;
		CrustTypeArray = new float[texSize, texSize];
		sw.Restart();
		for (int x = 0; x < texSize; x++)
		{
			for (int i = 0; i < texSize; i++)
			{
				float u = (float)x / (float)texSize;
				float v = (float)i / (float)texSize;
				CrustTypeArray[x, i] = texture.GetPixelBilinear(u, v).grayscale;
			}
			CurrentProgress = (float)x / (float)texSize;
			if (sw.ElapsedMilliseconds > 30)
			{
				await UniTask.Yield();
				sw.Restart();
			}
		}
	}

	private static async UniTask CopyLavaHeightMapToArray(LavaData data)
	{
		Stopwatch sw = new Stopwatch();
		CurrentProgress = 0f;
		int texSize = VoxelConstants.Size;
		LavaHeightMapArray = new float[texSize, texSize];
		sw.Restart();
		for (int x = 0; x < texSize; x++)
		{
			for (int i = 0; i < texSize; i++)
			{
				LavaHeightMapArray[x, i] = data.GetLavaHeight(VoxelTerrain.OctreeToWorldSpace(new Vector3(x, 0f, i), VoxelConstants.OriginOffsetInt));
			}
			CurrentProgress = (float)x / (float)texSize;
			if (sw.ElapsedMilliseconds > 30)
			{
				await UniTask.Yield();
				sw.Restart();
			}
		}
	}

	private static async UniTask CopyCrackTextureToArray(Texture2D texture)
	{
		Stopwatch sw = new Stopwatch();
		CurrentProgress = 0f;
		int texSize = texture.width;
		CrackArray = new float[texSize, texSize];
		sw.Restart();
		for (int x = 0; x < texSize; x++)
		{
			for (int i = 0; i < texSize; i++)
			{
				float u = (float)x / (float)texSize;
				float v = (float)i / (float)texSize;
				CrackArray[x, i] = texture.GetPixelBilinear(u, v).grayscale;
			}
			CurrentProgress = (float)x / (float)texSize;
			if (sw.ElapsedMilliseconds > 30)
			{
				await UniTask.Yield();
				sw.Restart();
			}
		}
	}

	private static async UniTask GenerateRegionFromHeightMap(Texture2D heightmap, VoxelOctree octree)
	{
		Stopwatch sw = new Stopwatch();
		CurrentProgress = 0f;
		sw.Restart();
		int texSize = heightmap.width;
		int crustTypeTexSize = CrustTypeArray?.GetLength(0) ?? (-1);
		int crackTexSize = CrackArray?.GetLength(0) ?? (-1);
		for (int texX = 0; texX < texSize; texX++)
		{
			for (int i = 0; i < texSize; i++)
			{
				float num = 0f;
				int num2 = 0;
				Vector2Int[] blurOffsets = _blurOffsets;
				for (int j = 0; j < blurOffsets.Length; j++)
				{
					Vector2Int vector2Int = blurOffsets[j];
					int num3 = texX + vector2Int.x;
					int num4 = i + vector2Int.y;
					if (num3 >= 0 && num3 < texSize && num4 >= 0 && num4 < texSize)
					{
						num += HeightMapArray[num3, num4];
						num2++;
					}
				}
				float num5 = num / (float)num2 * (MAXTerrainHeight - 128f) + MINTerrainHeight;
				ActualHeightArray[texX, i] = num5 - (float)VoxelConstants.OriginOffsetInt.y;
				int num6 = Mathf.CeilToInt(num5);
				VoxelNodeType nodeType;
				if (CrustTypeArray != null)
				{
					float num7 = (float)crustTypeTexSize / (float)texSize;
					int num8 = (int)((float)texX * num7);
					int num9 = (int)((float)i * num7);
					float num10 = CrustTypeArray[num8, num9];
					nodeType = ((num10 > 2f / 3f) ? (VoxelNodeType.Crust | VoxelNodeType.Macro) : ((!(num10 > 1f / 3f)) ? (VoxelNodeType.Dirt | VoxelNodeType.Macro) : (VoxelNodeType.Crust | VoxelNodeType.Dirt | VoxelNodeType.Macro)));
				}
				else
				{
					nodeType = VoxelNodeType.Crust | VoxelNodeType.Macro;
				}
				bool flag = false;
				float b = 0f;
				if (CrackArray != null)
				{
					float num11 = (float)crackTexSize / (float)texSize;
					int num12 = (int)((float)texX * num11);
					int num13 = (int)((float)i * num11);
					float num14 = CrackArray[num12, num13];
					flag = num14 > 0.05f;
					b = RocketMath.MapToScale(0.05f, 1f, 0f, ImGuiTerrainUtilityWindow.CrackDepth, num14);
				}
				float b2 = Mathf.Max(6f, b);
				b2 = Mathf.Min(num6 - 1, b2);
				float num15 = -1f;
				if (LavaHeightMapArray != null)
				{
					num15 = LavaHeightMapArray[texX, i];
				}
				for (int k = 0; (float)k < b2; k++)
				{
					int num16 = num6 - k;
					if (num16 < 0)
					{
						break;
					}
					float num17 = (float)num16 + 0.5f;
					float num18 = Mathf.Clamp(0.5f + (num5 - num17) * 0.25f, 0f, 1f);
					if (num18 <= 0f)
					{
						continue;
					}
					Vector3 vector = new Vector3(texX, num16, i) - VoxelConstants.OriginOffsetInt;
					if (LavaHeightMapArray != null)
					{
						float y = vector.y;
						if (y <= num15)
						{
							nodeType = VoxelNodeType.Dirt | VoxelNodeType.Macro;
						}
						else if (y < num15 + 2f)
						{
							nodeType = VoxelNodeType.Crust | VoxelNodeType.Dirt | VoxelNodeType.Macro;
						}
					}
					if (flag && num16 > 0 && (float)num16 > num15 - 2f)
					{
						float num19 = 0f;
						num19 += GetCracks(vector, CrackParam0);
						num19 += GetCracks(vector, CrackParam1);
						num19 = Mathf.Clamp01(num19);
						num18 = Mathf.Min(Mathf.Clamp(1f - num19, 0.01f, 1f), num18);
					}
					Vector3Int vector3Int = VoxelTerrain.WorldToOctreeSpace(vector, VoxelConstants.OriginOffsetInt);
					octree.SetDensity(vector3Int, (byte)(num18 * 255f), setNodeType: true, nodeType);
				}
				if (LavaHeightMapArray == null)
				{
					continue;
				}
				for (int l = 0; l < 3; l++)
				{
					int num20 = (int)num15 + l;
					if ((float)num20 < (float)num6 - b2)
					{
						Vector3Int vector3Int2 = VoxelTerrain.WorldToOctreeSpace(new Vector3(texX, num20, i) - VoxelConstants.OriginOffsetInt, VoxelConstants.OriginOffsetInt);
						octree.SetDensity(vector3Int2, byte.MaxValue, setNodeType: true, VoxelNodeType.Dirt);
					}
				}
			}
			CurrentProgress = (float)texX / (float)texSize;
			if (sw.ElapsedMilliseconds > 30)
			{
				await UniTask.Yield();
				sw.Restart();
			}
		}
	}

	private static float GetCracks(Vector3 pos, CrackParameter parameter)
	{
		float frequency = parameter.Frequency;
		float amplitude = parameter.Amplitude;
		float size = parameter.Size;
		return Mathf.Pow(RocketMath.MapToScale(-1f, 1f, 0f, 1f, SimplexNoise.Evaluate(pos.x * frequency, pos.y * frequency, pos.z * frequency) * amplitude), (size == 0f) ? 1f : size);
	}

	private static void AssignAndExecuteFillJobs(VoxelOctree octree, VoxelNodeType fillType)
	{
		int depth = 2;
		List<Node> list = new List<Node>();
		VoxelTerrain.GetNodesAtDepth(octree.Root, list, depth);
		foreach (Node item in list)
		{
			FillOctreeWorker.Assign(new FillOctreeJob(item, fillType));
		}
		FillOctreeWorker.ExecuteAll();
	}

	public static void SaveTerrain(string path, VoxelOctree octree, out string result)
	{
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		octree?.Serialize(path);
		stopwatch.Stop();
		result = $"Terrain Data Saved in {stopwatch.ElapsedMilliseconds / 1000}s. \n{path}";
	}
}
