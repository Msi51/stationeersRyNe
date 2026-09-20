using UnityEngine;

namespace Assets.Scripts;

public readonly struct HSVColor
{
	public readonly float H;

	public readonly float S;

	public readonly float V;

	public HSVColor(float h, float s, float v)
	{
		H = h;
		S = s;
		V = v;
	}

	public HSVColor(Color color)
	{
		Color.RGBToHSV(color, out H, out S, out V);
	}

	public Color ToColor(bool hdr = false)
	{
		return Color.HSVToRGB(H, S, V, hdr);
	}
}
