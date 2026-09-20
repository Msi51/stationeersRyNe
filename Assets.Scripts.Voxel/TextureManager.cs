using UnityEngine;

namespace Assets.Scripts.Voxel;

public static class TextureManager
{
	public static TextureAtlas MakeAtlas(Texture2D[] source, int padding = 0)
	{
		int num = 0;
		int num2 = 0;
		foreach (Texture2D texture2D in source)
		{
			num += texture2D.width + padding;
			num2 += texture2D.height + padding;
		}
		Texture2D texture2D2 = new Texture2D(num, num2, TextureFormat.ARGB32, mipChain: true);
		TextureAtlas textureAtlas = new TextureAtlas();
		Rect[] uvs = texture2D2.PackTextures(source, padding, 2048, makeNoLongerReadable: true);
		textureAtlas.atlas = texture2D2;
		textureAtlas.uvs = uvs;
		return textureAtlas;
	}

	public static TextureAtlas MakeAtlas(Texture2D[] source, Texture2D def, int padding = 0)
	{
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < source.Length; i++)
		{
			if (source[i] == null)
			{
				source[i] = def;
			}
		}
		foreach (Texture2D texture2D in source)
		{
			num += texture2D.width + padding;
			num2 += texture2D.height + padding;
		}
		Texture2D texture2D2 = new Texture2D(num, num2);
		TextureAtlas textureAtlas = new TextureAtlas();
		Rect[] uvs = texture2D2.PackTextures(source, padding, 2048, makeNoLongerReadable: true);
		textureAtlas.atlas = texture2D2;
		textureAtlas.uvs = uvs;
		return textureAtlas;
	}
}
