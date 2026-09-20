using Assets.Scripts;
using Assets.Scripts.Objects.Items;

public class EggShell : Stackable
{
	public readonly float BiomassValue = 1f;

	public override bool CanStack(IMergeable targetStack)
	{
		if (targetStack == null)
		{
			return false;
		}
		return targetStack is EggShell;
	}
}
