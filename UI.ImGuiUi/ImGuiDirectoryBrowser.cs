using System.IO;
using Assets.Scripts.UI.ImGuiUi;
using ImGuiNET;

namespace UI.ImGuiUi;

public class ImGuiDirectoryBrowser : ImGuiFileBrowser
{
	public ImGuiDirectoryBrowser(string defaultPath)
		: base(defaultPath)
	{
	}

	public override void Init()
	{
		base.Init();
		base.SelectedPath = base.CurrentPath;
	}

	public override void Draw()
	{
		if (!string.IsNullOrEmpty(ErrorMessage))
		{
			ImGui.TextColored(ImGuiColor.Float4.Red, ErrorMessage);
			ImGui.Separator();
		}
		if (ImGui.Button("Up###DirectoryUp"))
		{
			DirectoryInfo parent = Directory.GetParent(base.CurrentPath);
			if (parent != null)
			{
				base.CurrentPath = parent.FullName;
				base.SelectedPath = base.CurrentPath;
				UpdateFileList();
			}
		}
		ImGui.Text("[Folders]");
		foreach (string directory in _directories)
		{
			if (ImGui.Selectable(Path.GetFileName(directory) + "/"))
			{
				base.CurrentPath = directory;
				base.SelectedPath = directory;
				UpdateFileList();
				break;
			}
		}
		ImGui.Separator();
	}
}
