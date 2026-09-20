using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class WireCutter : ManualTool
{
	public static readonly int EquipWireCuttersHash = Animator.StringToHash("EquipWireCutters");

	public static readonly int UnEquipWireCuttersHash = Animator.StringToHash("UnEquipWireCutters");

	public override int EquipSoundHash => EquipWireCuttersHash;

	public override int UnEquipSoundHash => UnEquipWireCuttersHash;
}
