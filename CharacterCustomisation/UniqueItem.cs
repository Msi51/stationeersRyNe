using Assets.Scripts;
using UnityEngine;

namespace CharacterCustomisation;

public class UniqueItem : ScriptableObject
{
	[Tooltip("Unique GUID")]
	[SerializeField]
	[ReadOnly]
	private string _id;

	public string Id => _id ?? string.Empty;
}
