using System;
using System.IO;
using UnityEngine;

namespace Util;

public static class LoadTextureFromPath
{
	public static bool Load(string path, out Texture2D texture)
	{
		return Load(path, TextureFormat.R16, out texture);
	}

	private static bool Load(string path, TextureFormat format, out Texture2D texture)
	{
		if (string.IsNullOrEmpty(path))
		{
			texture = null;
			return false;
		}
		Texture2D texture2D = new Texture2D(4, 4, format, mipChain: false, linear: false);
		byte[] data = File.ReadAllBytes(path);
		if (!texture2D.LoadImage(data))
		{
			texture = null;
			return false;
		}
		texture2D.name = Path.GetFileName(path);
		texture = texture2D;
		return true;
	}

	public static bool LoadRaw(string path, TextureFormat format, int width, int height, out Texture2D texture)
	{
		if (string.IsNullOrEmpty(path))
		{
			texture = null;
			return false;
		}
		byte[] array = File.ReadAllBytes(path);
		int num = 4;
		int num2 = width * num;
		byte[] array2 = new byte[array.Length];
		for (int i = 0; i < height; i++)
		{
			int srcOffset = i * num2;
			int dstOffset = (height - 1 - i) * num2;
			Buffer.BlockCopy(array, srcOffset, array2, dstOffset, num2);
		}
		Texture2D texture2D = new Texture2D(width, height, format, mipChain: false, linear: true);
		texture2D.LoadRawTextureData(array2);
		texture2D.Apply();
		texture2D.name = Path.GetFileName(path);
		texture = texture2D;
		return true;
	}
}
