using UnityEngine;

namespace Assets.Scripts.Objects.Weapons;

public interface IProjectile
{
	void OnProjectileLaunched(Vector3 origin, Vector3 velocity);
}
