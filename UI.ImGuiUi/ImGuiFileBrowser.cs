using System;
using System.Collections.Generic;
using System.IO;
using Assets.Scripts.UI.ImGuiUi;
using ImGuiNET;
using UnityEngine;

namespace UI.ImGuiUi;

public class ImGuiFileBrowser
{
	public Action OnSelected;

	public Action OnDoubleClicked;

	protected readonly List<string> _directories = new List<string>(64);

	protected readonly List<string> _files = new List<string>(256);

	private string _selectedPath = "";

	public bool IsInit;

	protected string ErrorMessage;

	private readonly string DefaultPath;

	private string _lastCurrentPath;

	public string CurrentPath { get; protected set; }

	protected virtual string FileSearchPattern => "*";

	public string SelectedPath
	{
		get
		{
			return _selectedPath;
		}
		protected set
		{
			if (!string.Equals(value, _selectedPath))
			{
				_selectedPath = value;
				OnSelected?.Invoke();
			}
		}
	}

	public ImGuiFileBrowser(string defaultPath)
	{
		DefaultPath = defaultPath;
	}

	protected virtual bool FileSelected()
	{
		return true;
	}

	public virtual void Clear()
	{
		ErrorMessage = string.Empty;
		SelectedPath = string.Empty;
		_lastCurrentPath = CurrentPath;
		CurrentPath = string.Empty;
		IsInit = false;
	}

	public virtual void Init()
	{
		CurrentPath = (string.IsNullOrEmpty(_lastCurrentPath) ? DefaultPath : _lastCurrentPath);
		UpdateFileList();
		IsInit = true;
	}

	public bool TrySetCurrentPath(string path)
	{
		CurrentPath = path;
		if (UpdateFileList())
		{
			SelectedPath = CurrentPath;
			return true;
		}
		return false;
	}

	protected bool UpdateFileList()
	{
		try
		{
			_directories.Clear();
			_files.Clear();
			_directories.AddRange(Directory.GetDirectories(CurrentPath));
			_files.AddRange(Directory.GetFiles(CurrentPath, FileSearchPattern));
			return true;
		}
		catch (Exception ex)
		{
			Console.WriteLine("Error reading directory: " + ex.Message);
			return false;
		}
	}

	public virtual void Draw()
	{
		if (!string.IsNullOrEmpty(ErrorMessage))
		{
			ImGui.TextColored(ImGuiColor.Float4.Red, ErrorMessage);
			ImGui.Separator();
		}
		if (ImGui.Button("Up###DirectoryUp"))
		{
			DirectoryInfo parent = Directory.GetParent(CurrentPath);
			if (parent != null)
			{
				CurrentPath = parent.FullName;
				UpdateFileList();
			}
		}
		ImGui.SameLine();
		ImGui.Separator();
		ImGui.SameLine();
		string currentPath = CurrentPath;
		char directorySeparatorChar = Path.DirectorySeparatorChar;
		string[] array = (currentPath.EndsWith(directorySeparatorChar.ToString()) ? CurrentPath.TrimEnd(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar) : CurrentPath.Split(Path.DirectorySeparatorChar));
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
				CurrentPath = string.Join(Path.DirectorySeparatorChar, array2);
				if (CurrentPath.EndsWith(":"))
				{
					string currentPath2 = CurrentPath;
					directorySeparatorChar = Path.DirectorySeparatorChar;
					CurrentPath = currentPath2 + directorySeparatorChar;
				}
				UpdateFileList();
			}
			if (i < array.Length - 1)
			{
				ImGui.SameLine();
				ImGui.Text("/");
				ImGui.SameLine();
			}
		}
		ImGui.Text("[Folders]");
		foreach (string directory in _directories)
		{
			if (ImGui.Selectable(Path.GetFileName(directory) + "/"))
			{
				CurrentPath = directory;
				UpdateFileList();
				break;
			}
		}
		ImGui.Separator();
		ImGui.Text("[Files]");
		foreach (string file in _files)
		{
			if (ImGui.Selectable(Path.GetFileName(file)))
			{
				SelectedPath = file;
				if (!FileSelected())
				{
					SelectedPath = string.Empty;
				}
				break;
			}
			if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
			{
				OnDoubleClicked?.Invoke();
			}
		}
		ImGui.Separator();
	}
}
