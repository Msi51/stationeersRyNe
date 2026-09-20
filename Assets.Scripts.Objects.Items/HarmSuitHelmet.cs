namespace Assets.Scripts.Objects.Items;

public class HarmSuitHelmet : GasMask
{
	public override float LavaDamage => base.LavaDamage * 0.25f;

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		if (GameManager.RunSimulation && base.InternalAtmosphere != null)
		{
			base.InternalAtmosphere.Volume = base.Volume;
		}
	}
}
