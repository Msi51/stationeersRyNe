public class LocalModData : ModData
{
	public LocalModData()
	{
	}

	public LocalModData(string directoryPath, bool enabled)
	{
		DirectoryPath = new PathReference(directoryPath);
		Enabled = enabled;
	}
}
