using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using UnityEngine;

public interface IExitable
{
	bool FreeLook { get; }

	Vector3 GetExitPosition(Entity entity);

	void Exit(Human human);

	Transform GetCameraPoint(Entity entity);
}
