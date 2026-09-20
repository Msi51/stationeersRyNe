namespace UI.ImGuiUi;

public struct ImGuiFileBrowserSettings
{
	public string DefaultDirectory;

	public string FileSearchPattern;

	public bool ExcludeFiles;

	public string ButtonText;

	public bool DisallowFolderSelection;

	public bool AllowAddNewFolders;

	public static ImGuiFileBrowserSettings PngOnly(string defaultDir)
	{
		return new ImGuiFileBrowserSettings
		{
			AllowAddNewFolders = false,
			ButtonText = "Select",
			DefaultDirectory = defaultDir,
			ExcludeFiles = false,
			DisallowFolderSelection = true,
			FileSearchPattern = "*.png"
		};
	}

	public static ImGuiFileBrowserSettings AnyType(string defaultDir)
	{
		return new ImGuiFileBrowserSettings
		{
			AllowAddNewFolders = false,
			ButtonText = "Select",
			DefaultDirectory = defaultDir,
			ExcludeFiles = false,
			DisallowFolderSelection = true,
			FileSearchPattern = "*"
		};
	}

	public static ImGuiFileBrowserSettings Folders(string defaultDir)
	{
		return new ImGuiFileBrowserSettings
		{
			AllowAddNewFolders = true,
			ButtonText = "Select",
			DefaultDirectory = defaultDir,
			ExcludeFiles = true
		};
	}
}
