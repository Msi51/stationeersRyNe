using UnityEngine;

namespace Assets.Scripts.Objects;

public static class Layers
{
	public static LayerMask CharacterCreation = LayerMask.NameToLayer("CharacterCreation");

	public static LayerMask PlayerInvisible = LayerMask.NameToLayer("PlayerInvisible");

	public static LayerMask Player = LayerMask.NameToLayer("Player");

	public static LayerMask PlayerImmune = LayerMask.NameToLayer("PlayerImmune");

	public static LayerMask PlayerRagdoll = LayerMask.NameToLayer("PlayerRagdoll");

	public static LayerMask Default = LayerMask.NameToLayer("Default");

	public static LayerMask IgnoreRaycast = LayerMask.NameToLayer("Ignore Raycast");

	public static LayerMask ThumbnailCreation = LayerMask.NameToLayer("ThumbnailCreation");

	public static LayerMask Terrain = LayerMask.NameToLayer("Terrain");
}
