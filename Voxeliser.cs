using System;
using UnityEngine;

[Serializable]
public class Voxeliser
{
	private float[][][] _voxelMap;

	private int _xDensity;

	private int _yDensity;

	private int _zDensity;

	private float _ratio;

	private LayerMask _layerMask;

	public int Xsize => _xDensity;

	public int Ysize => _yDensity;

	public int Zsize => _zDensity;

	public float GetVoxel(int x, int y, int z)
	{
		return _voxelMap[x][y][z];
	}

	public void SetVoxel(int x, int y, int z, byte type, float density)
	{
		_voxelMap[x][y][z] = density;
	}

	public float GetDensity(Bounds bound)
	{
		float num = 0f;
		float num2 = 8f;
		float num3 = _ratio / num2;
		Vector3 vector = new Vector3(num3, num3, num3);
		Vector3 vector2 = vector / 2f;
		int num4 = 0;
		Vector3 min = bound.min;
		Vector3 max = bound.max;
		for (float num5 = min.y; num5 < max.y; num5 += num3)
		{
			for (float num6 = min.x; num6 < max.x; num6 += num3)
			{
				for (float num7 = min.z; num7 < max.z; num7 += num3)
				{
					num4++;
					if (Physics.CheckBox(new Vector3(num6, num5, num7) + vector2, vector, Quaternion.identity, _layerMask))
					{
						num += 1f;
					}
				}
			}
		}
		return Mathf.Clamp01((num4 > 0) ? (num / (float)num4) : 0f);
	}

	private void SmoothVoxelMap(int iterations = 1)
	{
		for (int i = 0; i < iterations; i++)
		{
			float[][][] array = new float[_xDensity][][];
			for (int j = 0; j < _xDensity; j++)
			{
				array[j] = new float[_yDensity][];
				for (int k = 0; k < _yDensity; k++)
				{
					array[j][k] = new float[_zDensity];
					for (int l = 0; l < _zDensity; l++)
					{
						float num = 0f;
						int num2 = 0;
						for (int m = Mathf.Max(0, j - 1); m <= Mathf.Min(_xDensity - 1, j + 1); m++)
						{
							for (int n = Mathf.Max(0, k - 1); n <= Mathf.Min(_yDensity - 1, k + 1); n++)
							{
								for (int num3 = Mathf.Max(0, l - 1); num3 <= Mathf.Min(_zDensity - 1, l + 1); num3++)
								{
									num += _voxelMap[m][n][num3];
									num2++;
								}
							}
						}
						array[j][k][l] = num / (float)num2;
					}
				}
			}
			_voxelMap = array;
		}
	}

	public Voxeliser(float ratio, Bounds bounds, LayerMask layerMask, int smoothIterations)
	{
		_ratio = ratio;
		_layerMask = layerMask;
		Vector3 vector = new Vector3(Mathf.CeilToInt(bounds.size.x / ratio), Mathf.CeilToInt(bounds.size.y / ratio), Mathf.CeilToInt(bounds.size.z / ratio));
		_xDensity = (int)vector.x;
		_yDensity = (int)vector.y;
		_zDensity = (int)vector.z;
		_voxelMap = new float[_xDensity][][];
		for (int i = 0; i < _xDensity; i++)
		{
			_voxelMap[i] = new float[_yDensity][];
			for (int j = 0; j < _yDensity; j++)
			{
				_voxelMap[i][j] = new float[_zDensity];
			}
		}
		Vector3 min = bounds.min;
		float[] array = new float[_xDensity];
		float[] array2 = new float[_yDensity];
		float[] array3 = new float[_zDensity];
		for (int k = 0; k < _xDensity; k++)
		{
			array[k] = (float)k * ratio + min.x;
		}
		for (int l = 0; l < _yDensity; l++)
		{
			array2[l] = (float)l * ratio + min.y;
		}
		for (int m = 0; m < _zDensity; m++)
		{
			array3[m] = (float)m * ratio + min.z;
		}
		Vector3 size = new Vector3(ratio, ratio, ratio);
		RaycastHit hitInfo;
		for (int n = 0; n < _yDensity; n++)
		{
			for (int num = 0; num < _xDensity; num++)
			{
				for (int num2 = 0; num2 < _zDensity; num2++)
				{
					if (!Physics.Raycast(new Vector3(array[num], array2[n], array3[num2]), Vector3.forward, out hitInfo, ratio, _layerMask))
					{
						continue;
					}
					SetVoxel(num, n, num2, 2, 1f);
					for (int num3 = num2 + 1; num3 < _zDensity; num3++)
					{
						if (!Physics.Raycast(new Vector3(array[num], array2[n], array3[num3]), Vector3.back, out hitInfo, ratio, _layerMask))
						{
							SetVoxel(num, n, num3, 2, 1f);
							continue;
						}
						num2 = num3;
						SetVoxel(num, n, num3, 2, 1f);
						break;
					}
				}
			}
		}
		for (int num4 = 0; num4 < _yDensity; num4++)
		{
			for (int num5 = 0; num5 < _zDensity; num5++)
			{
				for (int num6 = 0; num6 < _xDensity; num6++)
				{
					if (!Physics.Raycast(new Vector3(array[num6], array2[num4], array3[num5]), Vector3.right, out hitInfo, ratio, _layerMask))
					{
						continue;
					}
					SetVoxel(num6, num4, num5, 2, 1f);
					for (int num7 = num6 + 1; num7 < _xDensity; num7++)
					{
						if (!Physics.Raycast(new Vector3(array[num7], array2[num4], array3[num5]), Vector3.left, out hitInfo, ratio, _layerMask))
						{
							SetVoxel(num7, num4, num5, 2, 1f);
							continue;
						}
						num6 = num7;
						SetVoxel(num7, num4, num5, 2, 1f);
						break;
					}
				}
			}
		}
		for (int num8 = 0; num8 < _xDensity; num8++)
		{
			for (int num9 = 0; num9 < _zDensity; num9++)
			{
				for (int num10 = 0; num10 < _yDensity; num10++)
				{
					if (!Physics.Raycast(new Vector3(array[num8], array2[num10], array3[num9]), Vector3.up, out hitInfo, ratio, _layerMask))
					{
						continue;
					}
					SetVoxel(num8, num10, num9, 2, 1f);
					for (int num11 = num10 + 1; num11 < _yDensity; num11++)
					{
						if (!Physics.Raycast(new Vector3(array[num8], array2[num11], array3[num9]), Vector3.down, out hitInfo, ratio, _layerMask))
						{
							SetVoxel(num8, num11, num9, 2, 1f);
							continue;
						}
						num10 = num11;
						SetVoxel(num8, num11, num9, 2, 1f);
						break;
					}
				}
			}
		}
		for (int num12 = 0; num12 < _yDensity; num12++)
		{
			for (int num13 = 0; num13 < _xDensity; num13++)
			{
				for (int num14 = 0; num14 < _zDensity; num14++)
				{
					Vector3 vector2 = new Vector3(array[num13], array2[num12], array3[num14]);
					if (!Physics.Raycast(vector2, Vector3.forward, out hitInfo, ratio, _layerMask))
					{
						continue;
					}
					Bounds bound = new Bounds(vector2, size);
					SetVoxel(num13, num12, num14, 1, GetDensity(bound));
					for (int num15 = num14 + 1; num15 < _zDensity; num15++)
					{
						Vector3 vector3 = new Vector3(array[num13], array2[num12], array3[num15]);
						if (!Physics.Raycast(vector3, Vector3.back, out hitInfo, ratio, _layerMask))
						{
							SetVoxel(num13, num12, num15, 1, 1f);
							continue;
						}
						num14 = num15;
						SetVoxel(num13, num12, num15, 1, GetDensity(new Bounds(vector3, size)));
						break;
					}
				}
			}
		}
		for (int num16 = 0; num16 < _yDensity; num16++)
		{
			for (int num17 = 0; num17 < _zDensity; num17++)
			{
				for (int num18 = 0; num18 < _xDensity; num18++)
				{
					Vector3 vector4 = new Vector3(array[num18], array2[num16], array3[num17]);
					if (!Physics.Raycast(vector4, Vector3.right, out hitInfo, ratio, _layerMask))
					{
						continue;
					}
					Bounds bound2 = new Bounds(vector4, size);
					SetVoxel(num18, num16, num17, 1, GetDensity(bound2));
					for (int num19 = num18 + 1; num19 < _xDensity; num19++)
					{
						Vector3 vector5 = new Vector3(array[num19], array2[num16], array3[num17]);
						if (!Physics.Raycast(vector5, Vector3.left, out hitInfo, ratio, _layerMask))
						{
							SetVoxel(num19, num16, num17, 1, 1f);
							continue;
						}
						num18 = num19;
						SetVoxel(num19, num16, num17, 1, GetDensity(new Bounds(vector5, size)));
						break;
					}
				}
			}
		}
		for (int num20 = 0; num20 < _xDensity; num20++)
		{
			for (int num21 = 0; num21 < _zDensity; num21++)
			{
				for (int num22 = 0; num22 < _yDensity; num22++)
				{
					Vector3 vector6 = new Vector3(array[num20], array2[num22], array3[num21]);
					if (!Physics.Raycast(vector6, Vector3.up, out hitInfo, ratio, _layerMask))
					{
						continue;
					}
					Bounds bound3 = new Bounds(vector6, size);
					SetVoxel(num20, num22, num21, 1, GetDensity(bound3));
					for (int num23 = num22 + 1; num23 < _yDensity; num23++)
					{
						Vector3 vector7 = new Vector3(array[num20], array2[num23], array3[num21]);
						if (!Physics.Raycast(vector7, Vector3.down, out hitInfo, ratio, _layerMask))
						{
							SetVoxel(num20, num23, num21, 1, 1f);
							continue;
						}
						num22 = num23;
						SetVoxel(num20, num23, num21, 1, GetDensity(new Bounds(vector7, size)));
						break;
					}
				}
			}
		}
		SmoothVoxelMap(smoothIterations);
	}
}
