using UnityEngine;

namespace Assets.Scripts.Objects;

public class BodyPart : Item
{
	[Header("Body Part")]
	public int SpawnIndex;

	public string[] MeshNames;

	public void HideParentMesh(Entity entity)
	{
		string[] meshNames = MeshNames;
		foreach (string n in meshNames)
		{
			Renderer component = entity.ThingTransform.Find(n).GetComponent<Renderer>();
			if ((bool)component)
			{
				component.enabled = false;
			}
		}
	}
}
