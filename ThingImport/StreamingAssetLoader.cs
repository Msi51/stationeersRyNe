using System.Collections.Generic;
using System.IO;
using Assets.Scripts;
using Assets.Scripts.Serialization;
using Mono.WebBrowser;
using UnityEngine;
using UnityEngine.Rendering;

namespace ThingImport;

public class StreamingAssetLoader
{
	private static Dictionary<int, Texture2D> _loadedPathTextures = new Dictionary<int, Texture2D>();

	private static Dictionary<int, Sprite> _loadedPathSprites = new Dictionary<int, Sprite>();

	private static Dictionary<int, Mesh> _loadedPathMeshes = new Dictionary<int, Mesh>();

	private static readonly HashSet<int> _temporaryLoadTextures = new HashSet<int>();

	public static bool IsLoaded(TextureReference texRef)
	{
		if (texRef.TextureLoadType == TextureLoadType.Preload)
		{
			return true;
		}
		return _temporaryLoadTextures.Contains(texRef.PathHash);
	}

	public static Mesh LoadMesh(MeshReference meshReference, float scale)
	{
		if (!GetExistingPath(meshReference.Path, out var fullPath))
		{
			return null;
		}
		return LoadMeshFullPath(fullPath, scale);
	}

	public static Texture2D LoadTexture(TextureReference textureReference, TextureFormat format, bool linear = false)
	{
		if (!GetExistingPath(textureReference.Path, out var fullPath))
		{
			return null;
		}
		return LoadTextureFullPath(fullPath, format, textureReference.TextureLoadType, linear, textureReference.MipMapped);
	}

	public static Sprite LoadSprite(SpriteReference spriteReference)
	{
		if (!GetExistingPath(spriteReference.Path, out var fullPath))
		{
			return null;
		}
		return LoadSpriteFullPath(fullPath);
	}

	public static Texture2D LoadNormalMap(TextureReference textureReference, bool createFromGrayscale = false, bool flipGreen = false)
	{
		if (!GetExistingPath(textureReference.Path, out var fullPath))
		{
			return null;
		}
		return LoadNormalMapFullPath(fullPath, textureReference.TextureLoadType, createFromGrayscale, flipGreen, textureReference.MipMapped);
	}

	public static Texture2D LoadTextureFromStreamingAssets(string texturePath, TextureFormat format, bool mipMapped = true)
	{
		if (GameManager.IsBatchMode)
		{
			return Texture2D.whiteTexture;
		}
		return LoadTextureFullPath(Path.Join(Application.streamingAssetsPath, texturePath), format, TextureLoadType.Preload, linear: false, mipMapped);
	}

	private static Texture2D LoadTextureFullPath(string fullPath, TextureFormat format, TextureLoadType loadType, bool linear = false, bool mipMapped = true)
	{
		if (_loadedPathTextures.TryGetValue(Animator.StringToHash(fullPath), out var value))
		{
			return value;
		}
		value = new Texture2D(4, 4, format, mipMapped, linear);
		byte[] data = File.ReadAllBytes(fullPath);
		if (!value.LoadImage(data))
		{
			ConsoleWindow.PrintError("Failed to load " + fullPath + ". Incorrect format.");
			return null;
		}
		value.name = Path.GetFileName(fullPath);
		int num = Animator.StringToHash(fullPath);
		_loadedPathTextures.TryAdd(num, value);
		if (loadType == TextureLoadType.OnRequest)
		{
			_temporaryLoadTextures.Add(num);
		}
		return value;
	}

	private static Mesh LoadMeshFullPath(string fullPath, float scale)
	{
		if (_loadedPathMeshes.TryGetValue(Animator.StringToHash(fullPath), out var value))
		{
			return value;
		}
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullPath);
		OBJImporter.MeshData meshData = OBJImporter.ImportOBJ(File.ReadAllText(fullPath), scale);
		value = new Mesh
		{
			name = fileNameWithoutExtension,
			indexFormat = ((meshData.vertices.Length > 65535) ? IndexFormat.UInt32 : IndexFormat.UInt16)
		};
		value.SetVertices(meshData.vertices);
		value.SetNormals(meshData.normals);
		value.SetUVs(0, meshData.uvs);
		value.SetTriangles(meshData.triangles, 0);
		value.RecalculateTangents();
		value.RecalculateBounds();
		value.Optimize();
		_loadedPathMeshes.Add(Animator.StringToHash(fullPath), value);
		return value;
	}

	private static Texture2D LoadNormalMapFullPath(string fullPath, TextureLoadType loadType, bool createFromGrayscale, bool flipGreen, bool mipMapped)
	{
		int num = Animator.StringToHash(fullPath + "_normal");
		if (_loadedPathTextures.TryGetValue(num, out var value))
		{
			return value;
		}
		byte[] data = File.ReadAllBytes(fullPath);
		Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, mipMapped, linear: true);
		if (!texture2D.LoadImage(data, markNonReadable: false))
		{
			ConsoleWindow.PrintError("Failed to load normal map at " + fullPath + ". Incorrect format.");
			return null;
		}
		int width = texture2D.width;
		int height = texture2D.height;
		Color[] pixels = texture2D.GetPixels();
		Color[] array = new Color[pixels.Length];
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				int num2 = i * width + j;
				Vector3 normalized;
				if (createFromGrayscale)
				{
					float grayscale = texture2D.GetPixel((j - 1 + width) % width, i).grayscale;
					float grayscale2 = texture2D.GetPixel((j + 1) % width, i).grayscale;
					float grayscale3 = texture2D.GetPixel(j, (i - 1 + height) % height).grayscale;
					float grayscale4 = texture2D.GetPixel(j, (i + 1) % height).grayscale;
					normalized = new Vector3(grayscale - grayscale2, grayscale3 - grayscale4, 1f).normalized;
				}
				else
				{
					Color color = pixels[num2];
					normalized = new Vector3(color.r * 2f - 1f, (flipGreen ? (-1f) : 1f) * (color.g * 2f - 1f), color.b * 2f - 1f).normalized;
				}
				array[num2] = new Color(normalized.y * 0.5f + 0.5f, normalized.x * 0.5f + 0.5f, normalized.z * 0.5f + 0.5f, 1f);
			}
		}
		Texture2D texture2D2 = new Texture2D(width, height, TextureFormat.RGBA32, mipMapped, linear: true);
		texture2D2.SetPixels(array);
		texture2D2.Apply(updateMipmaps: true, makeNoLongerReadable: false);
		texture2D2.name = Path.GetFileName(fullPath);
		_loadedPathTextures.TryAdd(num, texture2D2);
		if (loadType == TextureLoadType.OnRequest)
		{
			_temporaryLoadTextures.Add(num);
		}
		Object.Destroy(texture2D);
		texture2D2.wrapMode = TextureWrapMode.Repeat;
		texture2D2.filterMode = FilterMode.Bilinear;
		return texture2D2;
	}

	private static Sprite LoadSpriteFullPath(string fullPath)
	{
		if (_loadedPathSprites.TryGetValue(Animator.StringToHash(fullPath), out var value))
		{
			return value;
		}
		Texture2D texture2D = new Texture2D(4, 4, TextureFormat.DXT5, mipChain: false, linear: false);
		byte[] data = File.ReadAllBytes(fullPath);
		if (!texture2D.LoadImage(data))
		{
			ConsoleWindow.PrintError("Failed to load image data at " + fullPath + ". Incorrect format.");
			return null;
		}
		value = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), Vector2.one * 0.5f);
		_loadedPathSprites.TryAdd(Animator.StringToHash(fullPath), value);
		return value;
	}

	public static void UnloadOnDemandTextures()
	{
		foreach (int temporaryLoadTexture in _temporaryLoadTextures)
		{
			Texture2D obj = _loadedPathTextures[temporaryLoadTexture];
			_loadedPathTextures.Remove(temporaryLoadTexture);
			Object.Destroy(obj);
		}
		_temporaryLoadTextures.Clear();
	}

	public static void Clear()
	{
		_loadedPathMeshes.Clear();
		_loadedPathSprites.Clear();
		_loadedPathTextures.Clear();
	}

	public static bool GetExistingDirectory(string pathStem, out string fullPath)
	{
		try
		{
			foreach (string pathRoot in GetPathRoots())
			{
				string text = Path.Combine(pathRoot, pathStem);
				if (Directory.Exists(text))
				{
					fullPath = text;
					return true;
				}
			}
			fullPath = null;
			return false;
		}
		catch (Exception exception)
		{
			ConsoleWindow.PrintError(exception).Forget();
		}
		fullPath = string.Empty;
		return false;
	}

	private static bool GetExistingPath(string pathStem, out string fullPath)
	{
		pathStem = pathStem.Replace('\\', '/');
		try
		{
			foreach (string pathRoot in GetPathRoots())
			{
				string text = Path.Combine(pathRoot, pathStem);
				if (File.Exists(text))
				{
					fullPath = text;
					return true;
				}
			}
			fullPath = null;
			return false;
		}
		catch (Exception exception)
		{
			ConsoleWindow.PrintError(exception).Forget();
		}
		fullPath = string.Empty;
		return false;
	}

	private static List<string> GetPathRoots()
	{
		List<string> list = new List<string>(3);
		list.Add(Application.streamingAssetsPath);
		list.Add(Path.Combine(Settings.CurrentData.SavePath, "mods"));
		if (WorkshopMenu.ModsConfig != null)
		{
			foreach (ModData enabledMod in WorkshopMenu.ModsConfig.GetEnabledMods())
			{
				list.Add(enabledMod.LocalPath);
			}
		}
		return list;
	}
}
