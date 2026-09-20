namespace Assets.Scripts.Serialization;

public static class SaveLoadConstants
{
	public static readonly string TerrainFileName = "terrain.dat";

	public static readonly string MetaFileName = "world_meta.xml";

	public static readonly string WorldFileName = "world.xml";

	public static readonly string PreviewFileName = "preview.png";

	public static readonly string ScreenshotFileName = "screenshot.png";

	public static readonly string QuickSaveFolder = "quicksave";

	public static readonly string AutoSaveFolder = "autosave";

	public static readonly string ManualSaveFolder = "manualsave";

	public static readonly string SaveFileExtension = ".save";

	public static readonly string SaveFileSearchPattern = "*.save";

	public static readonly string DateTimeFormat = "ddMMyy_HHmmss";

	public static readonly int ZipCompressionLevel = 3;
}
