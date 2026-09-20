using Assets.Scripts.Objects.Pipes;
using UnityEngine;

namespace Assets.Scripts.Objects.Chutes;

public class Hopper : ChuteInlet
{
	[Header("Hopper")]
	public bool RequiresIsOpen = true;

	public override void OnServerTick(float deltaTime)
	{
		if (IsOpen || !RequiresIsOpen)
		{
			base.OnServerTick(deltaTime);
		}
	}
}
