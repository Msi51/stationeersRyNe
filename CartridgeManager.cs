using Assets.Scripts.Objects.Items;

public static class CartridgeManager
{
	public static void CartridgeTick()
	{
		foreach (Cartridge allCartridge in Cartridge.AllCartridges)
		{
			if (!(allCartridge.Tablet == null) && allCartridge.Tablet.OnOff && allCartridge.Tablet.Powered)
			{
				allCartridge.OnMainTick();
			}
		}
	}
}
