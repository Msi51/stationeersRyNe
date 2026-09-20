using System;
using System.Collections.Generic;
using System.IO;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using UnityEngine;

namespace ThingImport.Thumbnails;

public static class ThumbnailOverrides
{
	private static readonly HashSet<string> Applied = new HashSet<string>();

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Hook()
	{
		Prefab.OnPrefabsLoaded += delegate
		{
			Apply(Prefab.AllPrefabs);
		};
	}

	public static void Apply(IReadOnlyList<Thing> prefabs)
	{
		if (ThumbnailPaths.IsProjectFolder)
		{
			return;
		}
		HashSet<string> hashSet;
		try
		{
			hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			string[] files = Directory.GetFiles(ThumbnailPaths.Folder, "*.png");
			foreach (string path in files)
			{
				hashSet.Add(Path.GetFileName(path));
			}
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			return;
		}
		if (hashSet.Count == 0)
		{
			return;
		}
		List<ColorSwatch> list = ((Singleton<GameManager>.Instance != null) ? Singleton<GameManager>.Instance.CustomColors : null);
		foreach (Thing prefab in prefabs)
		{
			if (prefab == null || string.IsNullOrEmpty(prefab.PrefabName) || Applied.Contains(prefab.PrefabName))
			{
				continue;
			}
			string path2 = ThumbnailPaths.Base(prefab.PrefabName);
			bool flag = hashSet.Contains(Path.GetFileName(path2));
			Sprite[] array = null;
			if (list != null)
			{
				for (int j = 0; j < list.Count; j++)
				{
					string path3 = ThumbnailPaths.Variant(prefab.PrefabName, ThumbnailPaths.ColorVariantName(list[j], j));
					if (!hashSet.Contains(Path.GetFileName(path3)))
					{
						continue;
					}
					if (array == null)
					{
						array = new Sprite[list.Count];
						if (prefab.Thumbnails != null)
						{
							for (int k = 0; k < array.Length && k < prefab.Thumbnails.Length; k++)
							{
								array[k] = prefab.Thumbnails[k];
							}
						}
					}
					Sprite sprite = LoadSprite(path3, $"{prefab.PrefabName}_{j}");
					if (sprite != null)
					{
						array[j] = sprite;
					}
				}
			}
			bool flag2 = false;
			if (prefab is Structure { BuildStates: not null } structure)
			{
				for (int l = 0; l < structure.BuildStates.Count; l++)
				{
					if (structure.BuildStates[l] == null)
					{
						continue;
					}
					string path4 = ThumbnailPaths.BuildState(prefab.PrefabName, l);
					if (hashSet.Contains(Path.GetFileName(path4)))
					{
						Sprite sprite2 = LoadSprite(path4, $"{prefab.PrefabName}_BuildState{l}");
						if (!(sprite2 == null))
						{
							structure.BuildStates[l].Thumbnail = sprite2;
							flag2 = true;
						}
					}
				}
			}
			if (!flag && array == null && !flag2)
			{
				continue;
			}
			Applied.Add(prefab.PrefabName);
			if (flag)
			{
				Sprite sprite3 = LoadSprite(path2, prefab.PrefabName);
				if (sprite3 != null)
				{
					prefab.Thumbnail = sprite3;
				}
			}
			if (array != null)
			{
				prefab.Thumbnails = array;
			}
		}
	}

	private static Sprite LoadSprite(string path, string name)
	{
		try
		{
			byte[] data = File.ReadAllBytes(path);
			Texture2D texture2D = new Texture2D(2, 2, TextureFormat.ARGB32, mipChain: true);
			if (!texture2D.LoadImage(data))
			{
				UnityEngine.Object.Destroy(texture2D);
				return null;
			}
			texture2D.name = name;
			Sprite sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
			sprite.name = name;
			return sprite;
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			return null;
		}
	}
}
