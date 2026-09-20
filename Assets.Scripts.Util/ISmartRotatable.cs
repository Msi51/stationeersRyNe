using System.Collections.Generic;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts.Util;

public interface ISmartRotatable
{
	Transform Transform { get; set; }

	int GetOpenEndsCount();

	int ConnectedCount();

	SmartRotate.ConnectionType GetConnectionType();

	List<Connection> GetOpenEnds();

	float GetGridSize();

	void SetOpenEndsPermutation(int[] permutation);

	int[] GetOpenEndsPermutation();

	void SetConnectionType(SmartRotate.ConnectionType connectionType);

	CanConstructInfo CanConstruct();

	PlacementSnap GetPlacementType();

	void Rotate(Vector3 vector, float angle, Quaternion offset, Vector3 centerOfRotation);

	RotationAxis GetRotationAxis();

	AllowedRotations GetAllowedRotations();
}
