using UnityEngine;

namespace Objects.Items;

public class RocketScanningHead : Consumable
{
	[SerializeField]
	private int scanLevel;

	public int ScanLevel => scanLevel;
}
