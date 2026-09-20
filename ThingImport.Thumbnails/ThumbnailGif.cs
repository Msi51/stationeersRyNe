using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ThingImport.Thumbnails;

public static class ThumbnailGif
{
	private sealed class LzwEncoder
	{
		private const int Eof = -1;

		private const int Bits = 12;

		private const int HSize = 5003;

		private readonly int _imgW;

		private readonly int _imgH;

		private readonly byte[] _pixels;

		private readonly int _initCodeSize;

		private int _remaining;

		private int _curPixel;

		private int _nBits;

		private const int MaxBits = 12;

		private int _maxcode;

		private const int MaxMaxCode = 4096;

		private readonly int[] _htab = new int[5003];

		private readonly int[] _codetab = new int[5003];

		private int _freeEnt;

		private bool _clearFlg;

		private int _gInitBits;

		private int _clearCode;

		private int _eofCode;

		private int _curAccum;

		private int _curBits;

		private static readonly int[] Masks = new int[17]
		{
			0, 1, 3, 7, 15, 31, 63, 127, 255, 511,
			1023, 2047, 4095, 8191, 16383, 32767, 65535
		};

		private int _aCount;

		private readonly byte[] _accum = new byte[256];

		public LzwEncoder(int width, int height, byte[] pixels, int colorDepth)
		{
			_imgW = width;
			_imgH = height;
			_pixels = pixels;
			_initCodeSize = Math.Max(2, colorDepth);
		}

		public void Encode(Stream os)
		{
			os.WriteByte((byte)_initCodeSize);
			_remaining = _imgW * _imgH;
			_curPixel = 0;
			Compress(_initCodeSize + 1, os);
			os.WriteByte(0);
		}

		private void Add(byte c, Stream os)
		{
			_accum[_aCount++] = c;
			if (_aCount >= 254)
			{
				Flush(os);
			}
		}

		private void ClearTable(Stream os)
		{
			ResetCodeTable();
			_freeEnt = _clearCode + 2;
			_clearFlg = true;
			Output(_clearCode, os);
		}

		private void ResetCodeTable()
		{
			for (int i = 0; i < 5003; i++)
			{
				_htab[i] = -1;
			}
		}

		private void Compress(int initBits, Stream os)
		{
			_gInitBits = initBits;
			_clearFlg = false;
			_nBits = _gInitBits;
			_maxcode = MaxCode(_nBits);
			_clearCode = 1 << initBits - 1;
			_eofCode = _clearCode + 1;
			_freeEnt = _clearCode + 2;
			_aCount = 0;
			int num = NextPixel();
			int num2 = 0;
			for (int num3 = 5003; num3 < 65536; num3 *= 2)
			{
				num2++;
			}
			num2 = 8 - num2;
			ResetCodeTable();
			Output(_clearCode, os);
			int num4;
			while ((num4 = NextPixel()) != -1)
			{
				int num5 = (num4 << 12) + num;
				int num6 = (num4 << num2) ^ num;
				if (_htab[num6] == num5)
				{
					num = _codetab[num6];
					continue;
				}
				if (_htab[num6] >= 0)
				{
					int num7 = 5003 - num6;
					if (num6 == 0)
					{
						num7 = 1;
					}
					bool flag = false;
					do
					{
						if ((num6 -= num7) < 0)
						{
							num6 += 5003;
						}
						if (_htab[num6] == num5)
						{
							num = _codetab[num6];
							flag = true;
							break;
						}
					}
					while (_htab[num6] >= 0);
					if (flag)
					{
						continue;
					}
				}
				Output(num, os);
				num = num4;
				if (_freeEnt < 4096)
				{
					_codetab[num6] = _freeEnt++;
					_htab[num6] = num5;
				}
				else
				{
					ClearTable(os);
				}
			}
			Output(num, os);
			Output(_eofCode, os);
		}

		private void Flush(Stream os)
		{
			if (_aCount > 0)
			{
				os.WriteByte((byte)_aCount);
				os.Write(_accum, 0, _aCount);
				_aCount = 0;
			}
		}

		private static int MaxCode(int nBits)
		{
			return (1 << nBits) - 1;
		}

		private int NextPixel()
		{
			if (_remaining == 0)
			{
				return -1;
			}
			_remaining--;
			return _pixels[_curPixel++] & 0xFF;
		}

		private void Output(int code, Stream os)
		{
			_curAccum &= Masks[_curBits];
			_curAccum = ((_curBits > 0) ? (_curAccum | (code << _curBits)) : code);
			_curBits += _nBits;
			while (_curBits >= 8)
			{
				Add((byte)(_curAccum & 0xFF), os);
				_curAccum >>= 8;
				_curBits -= 8;
			}
			if (_freeEnt > _maxcode || _clearFlg)
			{
				if (_clearFlg)
				{
					_maxcode = MaxCode(_nBits = _gInitBits);
					_clearFlg = false;
				}
				else
				{
					_nBits++;
					_maxcode = ((_nBits == 12) ? 4096 : MaxCode(_nBits));
				}
			}
			if (code == _eofCode)
			{
				while (_curBits > 0)
				{
					Add((byte)(_curAccum & 0xFF), os);
					_curAccum >>= 8;
					_curBits -= 8;
				}
				Flush(os);
			}
		}
	}

	private const int TransparentIndex = 255;

	public static byte[] Encode(IReadOnlyList<Color32[]> frames, int width, int height, int delayCentiseconds, byte alphaThreshold, bool dither)
	{
		if (frames == null || frames.Count == 0)
		{
			return null;
		}
		Color32[] palette = BuildPalette(frames, alphaThreshold);
		byte[] grid = BuildLookupGrid(palette);
		using MemoryStream memoryStream = new MemoryStream();
		WriteHeader(memoryStream, width, height, palette, frames.Count);
		byte[] indices = new byte[width * height];
		foreach (Color32[] frame in frames)
		{
			if (dither)
			{
				MapFrameDithered(frame, width, height, alphaThreshold, palette, grid, indices);
			}
			else
			{
				MapFrame(frame, width, height, alphaThreshold, grid, indices);
			}
			WriteFrame(memoryStream, width, height, indices, delayCentiseconds);
		}
		memoryStream.WriteByte(59);
		return memoryStream.ToArray();
	}

	private static int GridIndex(int r, int g, int b)
	{
		return (r >> 2 << 12) | (g >> 2 << 6) | (b >> 2);
	}

	private static void MapFrame(Color32[] frame, int width, int height, byte alphaThreshold, byte[] grid, byte[] indices)
	{
		for (int i = 0; i < height; i++)
		{
			int num = (height - 1 - i) * width;
			int num2 = i * width;
			for (int j = 0; j < width; j++)
			{
				Color32 color = frame[num + j];
				indices[num2 + j] = ((color.a < alphaThreshold) ? byte.MaxValue : grid[GridIndex(color.r, color.g, color.b)]);
			}
		}
	}

	private static void MapFrameDithered(Color32[] frame, int width, int height, byte alphaThreshold, Color32[] palette, byte[] grid, byte[] indices)
	{
		int num = width * height;
		float[] array = new float[num];
		float[] array2 = new float[num];
		float[] array3 = new float[num];
		bool[] array4 = new bool[num];
		for (int i = 0; i < height; i++)
		{
			int num2 = (height - 1 - i) * width;
			int num3 = i * width;
			for (int j = 0; j < width; j++)
			{
				Color32 color = frame[num2 + j];
				int num4 = num3 + j;
				array[num4] = (int)color.r;
				array2[num4] = (int)color.g;
				array3[num4] = (int)color.b;
				array4[num4] = color.a >= alphaThreshold;
			}
		}
		for (int k = 0; k < height; k++)
		{
			for (int l = 0; l < width; l++)
			{
				int num5 = k * width + l;
				if (!array4[num5])
				{
					indices[num5] = byte.MaxValue;
					continue;
				}
				int num6 = Mathf.Clamp((int)(array[num5] + 0.5f), 0, 255);
				int num7 = Mathf.Clamp((int)(array2[num5] + 0.5f), 0, 255);
				int num8 = Mathf.Clamp((int)(array3[num5] + 0.5f), 0, 255);
				Color32 color2 = palette[indices[num5] = grid[GridIndex(num6, num7, num8)]];
				float er = num6 - color2.r;
				float eg = num7 - color2.g;
				float eb = num8 - color2.b;
				Diffuse(array, array2, array3, array4, width, height, l + 1, k, er, eg, eb, 0.4375f);
				Diffuse(array, array2, array3, array4, width, height, l - 1, k + 1, er, eg, eb, 0.1875f);
				Diffuse(array, array2, array3, array4, width, height, l, k + 1, er, eg, eb, 0.3125f);
				Diffuse(array, array2, array3, array4, width, height, l + 1, k + 1, er, eg, eb, 0.0625f);
			}
		}
	}

	private static void Diffuse(float[] r, float[] g, float[] b, bool[] opaque, int width, int height, int x, int y, float er, float eg, float eb, float w)
	{
		if (x >= 0 && x < width && y >= 0 && y < height)
		{
			int num = y * width + x;
			if (opaque[num])
			{
				r[num] += er * w;
				g[num] += eg * w;
				b[num] += eb * w;
			}
		}
	}

	private static void WriteHeader(Stream s, int width, int height, Color32[] palette, int frameCount)
	{
		string text = "GIF89a";
		foreach (char c in text)
		{
			s.WriteByte((byte)c);
		}
		WriteShort(s, width);
		WriteShort(s, height);
		s.WriteByte(247);
		s.WriteByte(0);
		s.WriteByte(0);
		for (int j = 0; j < 256; j++)
		{
			Color32 color = palette[j];
			s.WriteByte(color.r);
			s.WriteByte(color.g);
			s.WriteByte(color.b);
		}
		if (frameCount > 1)
		{
			s.WriteByte(33);
			s.WriteByte(byte.MaxValue);
			s.WriteByte(11);
			text = "NETSCAPE2.0";
			foreach (char c2 in text)
			{
				s.WriteByte((byte)c2);
			}
			s.WriteByte(3);
			s.WriteByte(1);
			WriteShort(s, 0);
			s.WriteByte(0);
		}
	}

	private static void WriteFrame(Stream s, int width, int height, byte[] indices, int delayCentiseconds)
	{
		s.WriteByte(33);
		s.WriteByte(249);
		s.WriteByte(4);
		s.WriteByte(9);
		WriteShort(s, Mathf.Max(2, delayCentiseconds));
		s.WriteByte(byte.MaxValue);
		s.WriteByte(0);
		s.WriteByte(44);
		WriteShort(s, 0);
		WriteShort(s, 0);
		WriteShort(s, width);
		WriteShort(s, height);
		s.WriteByte(0);
		new LzwEncoder(width, height, indices, 8).Encode(s);
	}

	private static void WriteShort(Stream s, int value)
	{
		s.WriteByte((byte)(value & 0xFF));
		s.WriteByte((byte)((value >> 8) & 0xFF));
	}

	private static Color32[] BuildPalette(IReadOnlyList<Color32[]> frames, byte alphaThreshold)
	{
		long num = (long)frames[0].Length * (long)frames.Count;
		int num2 = (int)Math.Max(1L, num / 60000);
		List<int> list = new List<int>();
		int num3 = 0;
		foreach (Color32[] frame in frames)
		{
			for (int i = 0; i < frame.Length; i++)
			{
				Color32 color = frame[i];
				if (color.a >= alphaThreshold && num3++ % num2 == 0)
				{
					list.Add((color.r << 16) | (color.g << 8) | color.b);
				}
			}
		}
		if (list.Count == 0)
		{
			list.Add(0);
		}
		int[] array = list.ToArray();
		List<(int, int)> list2 = new List<(int, int)> { (0, array.Length - 1) };
		while (list2.Count < 255)
		{
			int num4 = -1;
			int num5 = 0;
			int num6 = 0;
			for (int j = 0; j < list2.Count; j++)
			{
				var (num7, num8) = list2[j];
				if (num8 > num7)
				{
					GetRanges(array, num7, num8, out var rr, out var gr, out var br);
					int num9 = Math.Max(rr, Math.Max(gr, br));
					if (num9 > num5)
					{
						num5 = num9;
						num4 = j;
						num6 = ((rr >= gr && rr >= br) ? 16 : ((gr >= br) ? 8 : 0));
					}
				}
			}
			if (num4 < 0)
			{
				break;
			}
			(int, int) tuple2 = list2[num4];
			int item = tuple2.Item1;
			int item2 = tuple2.Item2;
			int channel = num6;
			Array.Sort(array, item, item2 - item + 1, Comparer<int>.Create((int x, int y) => ((x >> channel) & 0xFF).CompareTo((y >> channel) & 0xFF)));
			int num10 = (item + item2) / 2;
			list2[num4] = (item, num10);
			list2.Add((num10 + 1, item2));
		}
		Color32[] array2 = new Color32[256];
		for (int num11 = 0; num11 < 256; num11++)
		{
			if (num11 < list2.Count)
			{
				(int, int) tuple3 = list2[num11];
				int item3 = tuple3.Item1;
				int item4 = tuple3.Item2;
				long num12 = 0L;
				long num13 = 0L;
				long num14 = 0L;
				for (int num15 = item3; num15 <= item4; num15++)
				{
					int num16 = array[num15];
					num12 += (num16 >> 16) & 0xFF;
					num13 += (num16 >> 8) & 0xFF;
					num14 += num16 & 0xFF;
				}
				int num17 = item4 - item3 + 1;
				array2[num11] = new Color32((byte)(num12 / num17), (byte)(num13 / num17), (byte)(num14 / num17), byte.MaxValue);
			}
			else
			{
				array2[num11] = new Color32(0, 0, 0, byte.MaxValue);
			}
		}
		return array2;
	}

	private static void GetRanges(int[] a, int lo, int hi, out int rr, out int gr, out int br)
	{
		int num = 255;
		int num2 = 0;
		int num3 = 255;
		int num4 = 0;
		int num5 = 255;
		int num6 = 0;
		for (int i = lo; i <= hi; i++)
		{
			int num7 = a[i];
			int num8 = (num7 >> 16) & 0xFF;
			int num9 = (num7 >> 8) & 0xFF;
			int num10 = num7 & 0xFF;
			if (num8 < num)
			{
				num = num8;
			}
			if (num8 > num2)
			{
				num2 = num8;
			}
			if (num9 < num3)
			{
				num3 = num9;
			}
			if (num9 > num4)
			{
				num4 = num9;
			}
			if (num10 < num5)
			{
				num5 = num10;
			}
			if (num10 > num6)
			{
				num6 = num10;
			}
		}
		rr = num2 - num;
		gr = num4 - num3;
		br = num6 - num5;
	}

	private static byte[] BuildLookupGrid(Color32[] palette)
	{
		byte[] array = new byte[262144];
		for (int i = 0; i < 64; i++)
		{
			for (int j = 0; j < 64; j++)
			{
				for (int k = 0; k < 64; k++)
				{
					int num = i * 4 + 2;
					int num2 = j * 4 + 2;
					int num3 = k * 4 + 2;
					int num4 = 0;
					int num5 = int.MaxValue;
					for (int l = 0; l < 255; l++)
					{
						Color32 color = palette[l];
						int num6 = num - color.r;
						int num7 = num2 - color.g;
						int num8 = num3 - color.b;
						int num9 = num6 * num6 + num7 * num7 + num8 * num8;
						if (num9 < num5)
						{
							num5 = num9;
							num4 = l;
						}
					}
					array[(i << 12) | (j << 6) | k] = (byte)num4;
				}
			}
		}
		return array;
	}
}
