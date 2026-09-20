using UnityEngine;

namespace Assets.Scripts;

public class MinableVisualiser : GameBase
{
	public void Reset()
	{
		SetActive(active: false);
		Transform.position = Vector3.zero;
		Transform.rotation = Quaternion.identity;
	}
}
