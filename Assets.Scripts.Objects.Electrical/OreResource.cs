using System;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

[Serializable]
public class OreResource
{
	public string OrePrefab;

	[ReadOnly]
	public Item GeneratorOre;

	public void Initialize()
	{
		GeneratorOre = Prefab.Find(Animator.StringToHash(OrePrefab)) as Item;
	}
}
