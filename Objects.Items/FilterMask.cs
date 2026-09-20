using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects.Items;

namespace Objects.Items;

public class FilterMask : GasMask
{
	public GasFilter Filter1 => Slots[0].Get<GasFilter>();

	public GasFilter Filter2 => Slots[1].Get<GasFilter>();

	protected override bool HasPaintableMaskMaterial => false;

	public override void OnAtmosphericTick()
	{
		if (base.WorldAtmosphere != null && (object)ParentHuman != null)
		{
			base.WorldAtmosphere.Add(base.InternalAtmosphere.RemoveAll());
			AtmosphereHelper.MoveToEqualize(base.WorldAtmosphere, base.InternalAtmosphere, PressurekPa.MaxValue, AtmosphereHelper.MatterState.All);
			Filter1?.FilterGas(ref base.InternalAtmosphere.GasMixture, ref base.WorldAtmosphere.GasMixture, base.InternalAtmosphere, 0.001f);
			Filter2?.FilterGas(ref base.InternalAtmosphere.GasMixture, ref base.WorldAtmosphere.GasMixture, base.InternalAtmosphere, 0.001f);
		}
	}
}
