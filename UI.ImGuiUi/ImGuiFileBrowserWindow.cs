using System;
using System.Collections.Generic;
using System.IO;
using ImGuiNET;
using UI.ImGuiUi.ImGuiWindows;
using UnityEngine;

namespace UI.ImGuiUi;

public class ImGuiFileBrowserWindow : UI.ImGuiUi.ImGuiWindows.ImGuiWindow
{
	public Action<string> OnFolderSelected;

	public Action<string> OnFileSelected;

	public Action<string> OnFileDoubleClicked;

	public Action<string> OnButtonClicked;

	private readonly List<string> _directories = new List<string>(64);

	private readonly List<string> _files = new List<string>(256);

	private string _currentDirectory;

	private string _selectedPath;

	private string _newFolderName = string.Empty;

	private ImGuiFileBrowserSettings _settings;

	public bool PathSelected => !string.IsNullOrEmpty(_selectedPath);

	public string SelectedPath => _selectedPath;

	public ImGuiFileBrowserWindow(ImGuiFileBrowserSettings settings, string title = "File Browser")
		: base(title, new Vector2(600f, 600f))
	{
		_settings = settings;
		_currentDirectory = _settings.DefaultDirectory;
	}

	public override void OnOpen()
	{
		UpdateFileList();
	}

	public override void OnClose()
	{
	}

	public void Clear()
	{
		_selectedPath = null;
	}

	public override void DrawContent()
	{
		DrawMain();
		if (ImGui.Button(_settings.ButtonText))
		{
			OnButtonClicked?.Invoke(_selectedPath);
			CloseWindow();
		}
		if (!_settings.AllowAddNewFolders)
		{
			return;
		}
		ImGui.SameLine();
		if (ImGui.Button("+###AddFolder") && !string.IsNullOrEmpty(_newFolderName))
		{
			string currentDirectory = _currentDirectory;
			string path = _currentDirectory + "\\" + _newFolderName;
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
				if (!TrySetCurrentPath(path))
				{
					TrySetCurrentPath(currentDirectory);
				}
			}
		}
		ImGui.SameLine();
		ImGui.InputText("###TitleText", ref _newFolderName, 50u);
	}

	private void DrawMain()
	{
		ImGui.Text("Selected: " + (string.IsNullOrEmpty(_selectedPath) ? "None" : _selectedPath));
		ImGui.Separator();
		if (ImGui.Button("Up###DirectoryUp"))
		{
			DirectoryInfo parent = Directory.GetParent(_currentDirectory);
			if (parent != null)
			{
				_currentDirectory = parent.FullName;
				UpdateFileList();
			}
		}
		ImGui.SameLine();
		ImGui.Spacing();
		ImGui.SameLine();
		string[] array = _currentDirectory.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i];
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			float x = ImGui.CalcTextSize(text).x;
			if (ImGui.Selectable(text, new Vector2(x, 0f)))
			{
				int num = i + 1;
				string[] array2 = new string[num];
				Array.Copy(array, array2, num);
				_currentDirectory = string.Join(Path.DirectorySeparatorChar, array2);
				if (_currentDirectory.EndsWith(":"))
				{
					string currentDirectory = _currentDirectory;
					char directorySeparatorChar = Path.DirectorySeparatorChar;
					_currentDirectory = currentDirectory + directorySeparatorChar;
				}
				UpdateFileList();
			}
			if (i < array.Length - 1)
			{
				ImGui.SameLine();
				char directorySeparatorChar = Path.DirectorySeparatorChar;
				ImGui.Text(directorySeparatorChar.ToString());
				ImGui.SameLine();
			}
		}
		ImGui.BeginChild("ResultsPanel", new Vector2(ImGui.GetContentRegionAvail().x, 400f), border: true, ImGuiWindowFlags.HorizontalScrollbar);
		ImGui.Text("[Folders]");
		foreach (string directory in _directories)
		{
			if (ImGui.Selectable(Path.GetFileName(directory) + "/", _selectedPath == directory) && !_settings.DisallowFolderSelection)
			{
				_selectedPath = directory;
				OnFolderSelected?.Invoke(_selectedPath);
			}
			if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
			{
				_currentDirectory = directory;
				UpdateFileList();
				break;
			}
		}
		if (!_settings.ExcludeFiles)
		{
			ImGui.Separator();
			ImGui.Text("[Files]");
			foreach (string file in _files)
			{
				if (ImGui.Selectable(Path.GetFileName(file), _selectedPath == file))
				{
					_selectedPath = file;
					OnFileSelected?.Invoke(_selectedPath);
				}
				if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
				{
					OnFileDoubleClicked?.Invoke(_selectedPath);
					CloseWindow();
					break;
				}
			}
		}
		ImGui.EndChild();
	}

	private bool TrySetCurrentPath(string path)
	{
		_currentDirectory = path;
		if (UpdateFileList())
		{
			_selectedPath = _currentDirectory;
			return true;
		}
		return false;
	}

	private bool UpdateFileList()
	{
		try
		{
			_directories.Clear();
			_files.Clear();
			_directories.AddRange(Directory.GetDirectories(_currentDirectory));
			_files.AddRange(Directory.GetFiles(_currentDirectory, _settings.FileSearchPattern));
			return true;
		}
		catch (Exception ex)
		{
			Console.WriteLine("Error reading directory: " + ex.Message);
			return false;
		}
	}
}
