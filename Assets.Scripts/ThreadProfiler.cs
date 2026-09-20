using System.Collections.Generic;
using Assets.Scripts.Util;
using ImGuiNET;
using Unity.Profiling;
using UnityEngine;

namespace Assets.Scripts;

public class ThreadProfiler
{
	public abstract class ProfilerBase
	{
		public const int MAX_INTEGER = 1000000;

		public string Header;

		public string DisplayName;

		public ProfilerCategory Category;

		public ProfilerRecorder Recorder;

		public ProfilerBase(string header, string displayName)
		{
			Header = header;
			DisplayName = displayName;
		}

		public void Start()
		{
			Recorder = ProfilerRecorder.StartNew(Category, Header);
		}

		public void Stop()
		{
			Recorder.Dispose();
		}

		public abstract void Draw();
	}

	public class MemoryProfiler : ProfilerBase
	{
		public MemoryProfiler(string header, string displayName)
			: base(header, displayName)
		{
			Category = ProfilerCategory.Memory;
		}

		public override void Draw()
		{
			ImGui.Text(DisplayName);
			ImGui.NextColumn();
			long lastValue = Recorder.LastValue;
			if (lastValue > 10000000000L)
			{
				ImGui.Text(StringManager.Get((float)lastValue * 1E-09f));
				ImGui.NextColumn();
				ImGui.Text("GB");
				ImGui.NextColumn();
			}
			else if (lastValue > 10000)
			{
				ImGui.Text(StringManager.Get((float)lastValue * 1E-06f));
				ImGui.NextColumn();
				ImGui.Text("MB");
				ImGui.NextColumn();
			}
			else
			{
				ImGui.Text(StringManager.Get((float)lastValue * 0.001f));
				ImGui.NextColumn();
				ImGui.Text("KB");
				ImGui.NextColumn();
			}
		}
	}

	public class RenderProfiler : ProfilerBase
	{
		public RenderProfiler(string header, string displayName)
			: base(header, displayName)
		{
			Category = ProfilerCategory.Render;
		}

		public override void Draw()
		{
			ImGui.Text(DisplayName);
			ImGui.NextColumn();
			long lastValue = Recorder.LastValue;
			if (lastValue > 1000000)
			{
				ImGui.Text(StringManager.Get((double)lastValue * 0.001));
				ImGui.NextColumn();
				ImGui.Text("K");
				ImGui.NextColumn();
			}
			else
			{
				ImGui.Text(StringManager.Get(lastValue));
				ImGui.NextColumn();
				ImGui.NextColumn();
			}
		}
	}

	private static bool _showInfo;

	private static List<ProfilerBase> _profiled = new List<ProfilerBase>
	{
		new MemoryProfiler("Total Used Memory", "total"),
		new MemoryProfiler("GC Used Memory", "gc"),
		new RenderProfiler("SetPass Calls Count", "setpass"),
		new RenderProfiler("Draw Calls Count", "drawcalls"),
		new RenderProfiler("Triangles Count", "triangles"),
		new RenderProfiler("Vertices Count", "vertices"),
		new RenderProfiler("Batches Count", "batches"),
		new RenderProfiler("Shadow Casters Count", "shadowcasters")
	};

	private static string _f2 = "F2";

	private static string _batchInfo = "BatchInfo";

	private static string _threadsTitle = "threads";

	private static string _ms = "ms";

	private static string _msAvg = "ms(avg)";

	private static string _jobCount = "items ";

	public static bool ShowInfo
	{
		get
		{
			return _showInfo;
		}
		set
		{
			if (_showInfo == value)
			{
				return;
			}
			_showInfo = value;
			if (_showInfo)
			{
				for (int i = 0; i < _profiled.Count; i++)
				{
					_profiled[i].Start();
				}
			}
			else
			{
				for (int j = 0; j < _profiled.Count; j++)
				{
					_profiled[j].Stop();
				}
			}
		}
	}

	public static void DrawBatchInfo()
	{
		if (!ShowInfo)
		{
			return;
		}
		ImGui.Begin(_batchInfo, (ImGuiWindowFlags)12687);
		Vector2 windowSize = new Vector2(600f, 1000f);
		ImGui.SetWindowPos(new Vector2((float)Screen.width - windowSize.x, 0f), ImGuiCond.Always);
		ProfilerCategory profilerCategory = default(ProfilerCategory);
		for (int i = 0; i < _profiled.Count; i++)
		{
			ProfilerBase profilerBase = _profiled[i];
			if ((ushort)profilerBase.Category != (ushort)profilerCategory)
			{
				profilerCategory = profilerBase.Category;
				ImGui.Columns(1, border: false);
				ImGui.NewLine();
				ImGui.Text(profilerCategory.Name);
				ImGui.Columns(3, border: false);
				ImGui.SetColumnWidth(0, 200f);
				ImGui.SetColumnWidth(1, 160f);
				ImGui.SetColumnWidth(2, 160f);
			}
			ImGui.Text(StringManager.Get(i));
			ImGui.SameLine();
			profilerBase.Draw();
		}
		ImGui.Columns(1);
		ImGui.NewLine();
		ImGui.Text(_threadsTitle);
		ImGui.Columns(4, border: false);
		ImGui.SetColumnWidth(4, 160f);
		List<IThreadedWorker> managedThreads = IThreadedWorker.ManagedThreads;
		for (int j = 0; j < managedThreads.Count; j++)
		{
			IThreadedWorker threadedWorker = managedThreads[j];
			ImGui.Text(StringManager.Get(j));
			ImGui.SameLine();
			ImGui.Text(threadedWorker.GetName);
			ImGui.NextColumn();
			ImGui.Text(threadedWorker.LastTick.ToStringRounded());
			ImGui.SameLine();
			ImGui.Text(_ms);
			ImGui.NextColumn();
			ImGui.Text(threadedWorker.AverageTick.ToStringRounded());
			ImGui.SameLine();
			ImGui.Text(_msAvg);
			ImGui.NextColumn();
			ImGui.Text(_jobCount);
			ImGui.SameLine();
			ImGui.Text(StringManager.Get(threadedWorker.JobCount));
			ImGui.NextColumn();
		}
		ImGui.SetWindowSize(windowSize);
		ImGui.End();
	}
}
