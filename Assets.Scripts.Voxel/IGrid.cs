using UnityEngine;

namespace Assets.Scripts.Voxel;

public interface IGrid
{
	int GetSize(int dim);

	Vector3 GetSize();

	Voxel GetVoxel(Vector3 pos);

	bool SetVoxel(byte type, byte density, short x, short y, short z, bool updated = true);
}
