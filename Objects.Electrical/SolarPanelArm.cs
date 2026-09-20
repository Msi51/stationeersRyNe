using TerrainSystem;
using UnityEngine;

namespace Objects.Electrical;

public class SolarPanelArm : GameBase
{
	public Transform Cells;

	public Transform YawPivot;

	public Transform PitchPivot;

	public bool SingleRaycast;

	private float _visibility;

	private float _efficiency;

	private static Vector3[] _rayOffsets = new Vector3[5]
	{
		new Vector3(0f, 0f, 0.5f),
		new Vector3(0.5f, 0.5f, 0.5f),
		new Vector3(-0.5f, 0.5f, 0.5f),
		new Vector3(0.5f, -0.5f, 0.5f),
		new Vector3(-0.5f, -0.5f, 0.5f)
	};

	public Vector3 FacingDirection => Cells.forward;

	public void SetYaw(double value)
	{
		if ((bool)YawPivot)
		{
			YawPivot.localRotation = Quaternion.Euler(0f, 0f, (float)value);
		}
	}

	public void SetPitch(double value)
	{
		if ((bool)PitchPivot)
		{
			PitchPivot.localRotation = Quaternion.Euler(Mathf.Lerp(-75f, 75f, (float)value), 0f, 0f);
		}
	}

	public float CalculateSolarEfficiency(RaycastHit[] hits, LayerMask collisionMask)
	{
		if (Vector3.Dot(FacingDirection, OrbitalSimulation.WorldSunVector) <= 0f)
		{
			return 0f;
		}
		if (OrbitalSimulation.IsEclipse)
		{
			return 0f;
		}
		_visibility = 0f;
		if (SingleRaycast)
		{
			if (IsRaycastObscured(_rayOffsets[0], hits, collisionMask))
			{
				_efficiency = 0f;
				return _efficiency;
			}
			_visibility = 1f;
		}
		else
		{
			int num = _rayOffsets.Length;
			Vector3[] rayOffsets = _rayOffsets;
			foreach (Vector3 offset in rayOffsets)
			{
				if (IsRaycastObscured(offset, hits, collisionMask))
				{
					num--;
				}
			}
			if (num == 0)
			{
				_efficiency = 0f;
				return _efficiency;
			}
			_visibility = (float)num / (float)_rayOffsets.Length;
		}
		if (VoxelTerrain.Instance.OctreeRaycast(Cells.position, OrbitalSimulation.WorldSunVector.normalized))
		{
			_efficiency = 0f;
			return _efficiency;
		}
		_efficiency = Mathf.Clamp((1f - (FacingDirection - OrbitalSimulation.WorldSunVector).magnitude) * _visibility, 0f, 1f);
		return _efficiency;
	}

	private bool IsRaycastObscured(Vector3 offset, RaycastHit[] hits, LayerMask collisionMask)
	{
		offset = Cells.rotation * offset;
		if (Physics.RaycastNonAlloc(new Ray(Cells.position + offset, OrbitalSimulation.WorldSunVector), hits, float.PositiveInfinity, collisionMask) > 0)
		{
			if ((bool)hits[0].collider)
			{
				return !hits[0].collider.isTrigger;
			}
			return false;
		}
		return false;
	}
}
