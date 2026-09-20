using System;
using System.Collections.Generic;
using Assets.Scripts.Networking;
using Assets.Scripts.UI.ImGuiUi;
using ImGuiNET;
using UI.ImGuiUi;
using UnityEngine;

namespace Assets.Scripts;

public static class NetworkDebugWindow
{
	private sealed class Conn
	{
		public string Name;

		public Vector4 Color;

		public int Epoch;

		public NetworkManager.NetClientSample Last;

		public string SparkId;

		public float[] SendqMB = new float[120];

		public float[] Ping = new float[120];

		public float[] Loss = new float[120];

		public float[] DownKBs = new float[120];

		public float[] UpKBs = new float[120];

		public string CellPing;

		public string CellLoss;

		public string CellSendq;

		public string CellOut;

		public string OvPing;

		public string OvLoss;

		public string OvDown;

		public string OvUp;
	}

	private sealed class StreamRow
	{
		public string Tick = "";

		public string Last = "";

		public string Rate = "";

		public string Cycles = "";

		public string Dropped = "";

		public Vector4 DroppedColor;
	}

	private sealed class SectionStat
	{
		public int Last;

		public int Max;
	}

	public static bool Show;

	private const int HISTORY = 120;

	private const float SAMPLE_INTERVAL = 1f;

	private const string IdTick = "##nd_tick";

	private const string IdWorst = "##nd_worst";

	private const string IdUp = "##nd_up";

	private const string IdPay = "##nd_pay";

	private const string IdHist = "##nd_hist";

	private const string IdPhysRec = "##nd_physrec";

	private const string IdCliApplied = "##nd_cliapp";

	private static readonly Vector4 Lbl = new Vector4(0.49f, 0.53f, 0.55f, 1f);

	private static readonly Vector4 Sage = new Vector4(0.53f, 0.77f, 0.43f, 1f);

	private static readonly Vector4 Sky = new Vector4(0.37f, 0.69f, 0.85f, 1f);

	private static readonly Vector4 Amber = new Vector4(0.85f, 0.71f, 0.3f, 1f);

	private static readonly Vector4 Red = new Vector4(0.89f, 0.34f, 0.29f, 1f);

	private static readonly Vector4[] Palette = new Vector4[8]
	{
		new Vector4(0.53f, 0.77f, 0.43f, 1f),
		new Vector4(0.37f, 0.69f, 0.85f, 1f),
		new Vector4(0.37f, 0.79f, 0.75f, 1f),
		new Vector4(0.69f, 0.55f, 0.88f, 1f),
		new Vector4(0.85f, 0.71f, 0.3f, 1f),
		new Vector4(0.89f, 0.34f, 0.29f, 1f),
		new Vector4(0.88f, 0.54f, 0.29f, 1f),
		new Vector4(0.6f, 0.62f, 0.64f, 1f)
	};

	private static readonly Dictionary<long, Conn> _conns = new Dictionary<long, Conn>();

	private static readonly List<long> _toRemove = new List<long>();

	private static readonly NetworkManager.NetClientSample[] _buf = new NetworkManager.NetClientSample[NetworkManager.MaxConnections];

	private static float[] _hist = new float[NetworkManager.MaxConnections];

	private static readonly float[] _tickHz = new float[120];

	private static readonly float[] _worstMB = new float[120];

	private static readonly float[] _outKBs = new float[120];

	private static readonly float[] _payloadKB = new float[120];

	private static readonly float[] _physKB = new float[120];

	private static readonly float[] _physRecords = new float[120];

	private static readonly float[] _cliApplied = new float[120];

	private static readonly float[] _cliStateKBs = new float[120];

	private static readonly float[] _cliPhysKBs = new float[120];

	private static string _physRecOv = "";

	private static string _cliAppliedOv = "";

	private static long _prevApplied;

	private static long _prevStateBytes;

	private static long _prevPhysBytes;

	private static long _prevStateSent;

	private static long _prevPhysSent;

	private static readonly StreamRow _stateRow = new StreamRow();

	private static readonly StreamRow _physRow = new StreamRow();

	private static string _tileApplied = "";

	private static string _tileUnknown = "";

	private static string _tileStale = "";

	private static Vector4 _tileUnknownColor;

	private static Vector4 _tileStaleColor;

	private static float _overlayPeak;

	private static string _overlayPeakOv = "";

	private static string _legendState = "";

	private static string _legendPhys = "";

	private const int SECTION_COLLAPSE_BYTES = 16;

	private static readonly Dictionary<string, SectionStat> _sectionStats = new Dictionary<string, SectionStat>(40);

	private static readonly List<KeyValuePair<string, SectionStat>> _sectionSort = new List<KeyValuePair<string, SectionStat>>(40);

	private static readonly List<(string name, string bytes, string max, string share, Vector4 color)> _sectionRows = new List<(string, string, string, string, Vector4)>(40);

	private static string _sectionHiddenLabel = "";

	private static string _tickOv = "";

	private static string _worstOv = "";

	private static string _outOv = "";

	private static string _payOv = "";

	private static string _header = "";

	private static float _lastSample = -999f;

	private static int _epoch;

	public static void Toggle()
	{
		Show = !Show;
		Reset();
	}

	private static void Reset()
	{
		_conns.Clear();
		Array.Clear(_tickHz, 0, _tickHz.Length);
		Array.Clear(_worstMB, 0, _worstMB.Length);
		Array.Clear(_outKBs, 0, _outKBs.Length);
		Array.Clear(_payloadKB, 0, _payloadKB.Length);
		Array.Clear(_hist, 0, _hist.Length);
		Array.Clear(_physKB, 0, _physKB.Length);
		Array.Clear(_physRecords, 0, _physRecords.Length);
		Array.Clear(_cliApplied, 0, _cliApplied.Length);
		Array.Clear(_cliStateKBs, 0, _cliStateKBs.Length);
		Array.Clear(_cliPhysKBs, 0, _cliPhysKBs.Length);
		_sectionRows.Clear();
		_sectionStats.Clear();
		_sectionSort.Clear();
		_sectionHiddenLabel = "";
		_physRecOv = (_cliAppliedOv = "");
		_tileApplied = (_tileUnknown = (_tileStale = ""));
		_overlayPeak = 0f;
		_overlayPeakOv = (_legendState = (_legendPhys = ""));
		_prevApplied = FragmentHandler.PhysicsRecordsApplied;
		_prevStateBytes = FragmentHandler.State.TotalBytesReceived;
		_prevPhysBytes = FragmentHandler.Physics.TotalBytesReceived;
		_prevStateSent = FragmentHandler.State.TotalBytesSent;
		_prevPhysSent = FragmentHandler.Physics.TotalBytesSent;
		_tickOv = (_worstOv = (_outOv = (_payOv = (_header = ""))));
		_lastSample = -999f;
		_epoch = 0;
	}

	private static void Push(float[] ring, float value)
	{
		Array.Copy(ring, 1, ring, 0, ring.Length - 1);
		ring[^1] = value;
	}

	private static float NiceMax(float[] ring, float floor)
	{
		float num = floor;
		for (int i = 0; i < ring.Length; i++)
		{
			if (ring[i] > num)
			{
				num = ring[i];
			}
		}
		return num * 1.12f;
	}

	private static string FmtBytes(ulong b)
	{
		if (b < 1024)
		{
			return b + "B";
		}
		if (b < 1048576)
		{
			return ((float)b / 1024f).ToString("0.0") + "KB";
		}
		return ((float)b / 1048576f).ToString("0.0") + "MB";
	}

	private static void Sample()
	{
		_epoch++;
		int num = NetworkManager.SampleConnections(_buf);
		ulong num2 = 0uL;
		double num3 = 0.0;
		for (int i = 0; i < num; i++)
		{
			NetworkManager.NetClientSample last = _buf[i];
			if (!_conns.TryGetValue(last.Id, out var value))
			{
				value = new Conn
				{
					Color = Palette[_conns.Count % Palette.Length],
					SparkId = "##sq" + last.Id
				};
				_conns[last.Id] = value;
			}
			value.Name = last.Name;
			value.Epoch = _epoch;
			value.Last = last;
			Push(value.SendqMB, (float)last.SendQueueBytes / 1048576f);
			Push(value.Ping, last.AvgPing);
			Push(value.Loss, last.Loss * 100f);
			Push(value.DownKBs, (float)last.BytesRecvPerSec / 1024f);
			Push(value.UpKBs, (float)last.BytesSentPerSec / 1024f);
			value.CellPing = $"{last.AvgPing}/{last.LastPing}/{last.LowPing}";
			value.CellLoss = (last.Loss * 100f).ToString("0.0") + "%";
			value.CellSendq = last.SendQueueMsgs + " / " + FmtBytes(last.SendQueueBytes);
			value.CellOut = ((float)last.BytesSentPerSec / 1024f).ToString("0") + " KB/s";
			value.OvPing = last.AvgPing + " ms";
			value.OvLoss = value.CellLoss;
			value.OvDown = ((float)last.BytesRecvPerSec / 1024f).ToString("0") + " KB/s";
			value.OvUp = value.CellOut;
			if (last.SendQueueBytes > num2)
			{
				num2 = last.SendQueueBytes;
			}
			num3 += (double)last.BytesSentPerSec;
		}
		_toRemove.Clear();
		foreach (KeyValuePair<long, Conn> conn in _conns)
		{
			if (conn.Value.Epoch != _epoch)
			{
				_toRemove.Add(conn.Key);
			}
		}
		foreach (long item in _toRemove)
		{
			_conns.Remove(item);
		}
		Push(_tickHz, 1000f / (float)Mathf.Max(1, NetworkServer.CurrentTickIntervalMs));
		Push(_worstMB, (float)num2 / 1048576f);
		Push(_outKBs, (float)(num3 / 1024.0));
		Push(_payloadKB, (float)FragmentHandler.LastTickCompressedBytes / 1024f);
		_tickOv = Mathf.RoundToInt(_tickHz[119]) + " Hz";
		_worstOv = _worstMB[119].ToString("0.0") + " MB";
		_outOv = _outKBs[119].ToString("0") + " KB/s";
		_payOv = _payloadKB[119].ToString("0") + " KB";
		if (NetworkManager.IsServer)
		{
			bool flag = NetworkServer.CurrentTickIntervalMs > 50;
			_header = string.Format("Send tick {0}{1} ({2} ms)   Clients {3}", flag ? "▼ " : "", _tickOv, NetworkServer.CurrentTickIntervalMs, _conns.Count);
			SampleHostStreams();
		}
		else if (num > 0)
		{
			NetworkManager.NetClientSample netClientSample = _buf[0];
			_header = $"Link to host  ping {netClientSample.AvgPing}/{netClientSample.LastPing}/{netClientSample.LowPing} ms   MTU {netClientSample.Mtu}   {netClientSample.State}";
			SampleClientStreams();
		}
	}

	private static void SampleHostStreams()
	{
		Push(_physKB, (float)FragmentHandler.LastPhysicsCompressedBytes / 1024f);
		Push(_physRecords, FragmentHandler.LastPhysicsRecordCount);
		_physRecOv = FragmentHandler.LastPhysicsRecordCount + " written";
		FragmentStream state = FragmentHandler.State;
		FragmentStream physics = FragmentHandler.Physics;
		float num = (float)(state.TotalBytesSent - _prevStateSent) / 1024f;
		float num2 = (float)(physics.TotalBytesSent - _prevPhysSent) / 1024f;
		_prevStateSent = state.TotalBytesSent;
		_prevPhysSent = physics.TotalBytesSent;
		_stateRow.Tick = FragmentHandler.NetworkTick.ToString();
		_stateRow.Last = FmtBytes((ulong)FragmentHandler.LastTickCompressedBytes);
		_stateRow.Rate = num.ToString("0") + " KB/s";
		_stateRow.Cycles = state.CyclesSent.ToString();
		_stateRow.Dropped = "-";
		_stateRow.DroppedColor = Lbl;
		_physRow.Tick = FragmentHandler.NetworkTick.ToString();
		_physRow.Last = FmtBytes((ulong)FragmentHandler.LastPhysicsCompressedBytes);
		_physRow.Rate = num2.ToString("0") + " KB/s";
		_physRow.Cycles = physics.CyclesSent.ToString();
		_physRow.Dropped = FragmentHandler.LastPhysicsSuppressedCount.ToString();
		_physRow.DroppedColor = Lbl;
		SampleOverlay(_payloadKB[119], _physKB[119], " KB");
		SampleSections();
	}

	private static void SampleClientStreams()
	{
		long physicsRecordsApplied = FragmentHandler.PhysicsRecordsApplied;
		Push(_cliApplied, physicsRecordsApplied - _prevApplied);
		_prevApplied = physicsRecordsApplied;
		_cliAppliedOv = _cliApplied[119].ToString("0") + " /s";
		FragmentStream state = FragmentHandler.State;
		FragmentStream physics = FragmentHandler.Physics;
		Push(_cliStateKBs, (float)(state.TotalBytesReceived - _prevStateBytes) / 1024f);
		_prevStateBytes = state.TotalBytesReceived;
		Push(_cliPhysKBs, (float)(physics.TotalBytesReceived - _prevPhysBytes) / 1024f);
		_prevPhysBytes = physics.TotalBytesReceived;
		_stateRow.Tick = state.LastReceivedTick.ToString();
		_stateRow.Last = FmtBytes((ulong)state.LastReceivedBytes);
		_stateRow.Rate = _cliStateKBs[119].ToString("0") + " KB/s";
		_stateRow.Cycles = state.CyclesReceived.ToString();
		_stateRow.Dropped = state.CyclesAbandoned.ToString();
		_stateRow.DroppedColor = ((state.CyclesAbandoned == 0L) ? Sage : Amber);
		_physRow.Tick = physics.LastReceivedTick.ToString();
		_physRow.Last = FmtBytes((ulong)physics.LastReceivedBytes);
		_physRow.Rate = _cliPhysKBs[119].ToString("0") + " KB/s";
		_physRow.Cycles = physics.CyclesReceived.ToString();
		_physRow.Dropped = physics.CyclesAbandoned.ToString();
		_physRow.DroppedColor = ((physics.CyclesAbandoned == 0L) ? Sage : Amber);
		_tileApplied = physicsRecordsApplied.ToString("N0");
		_tileUnknown = FragmentHandler.PhysicsRecordsSkippedUnknown.ToString("N0");
		_tileStale = FragmentHandler.PhysicsRecordsSkippedStale.ToString("N0");
		_tileUnknownColor = ((FragmentHandler.PhysicsRecordsSkippedUnknown == 0L) ? Sage : Amber);
		_tileStaleColor = ((FragmentHandler.PhysicsRecordsSkippedStale < 100) ? Sage : Amber);
		SampleOverlay(_cliStateKBs[119], _cliPhysKBs[119], " KB/s");
	}

	private static void SampleOverlay(float stateValue, float physValue, string unit)
	{
		_overlayPeak = Mathf.Max(_overlayPeak, stateValue, physValue);
		_overlayPeakOv = "peak " + _overlayPeak.ToString("0.0") + unit;
		_legendState = "State  " + stateValue.ToString("0.0") + unit;
		_legendPhys = "Physics  " + physValue.ToString("0.0") + unit;
	}

	private static void SampleSections()
	{
		IReadOnlyList<(string, int)> lastSectionBytes = FragmentHandler.LastSectionBytes;
		int num = 0;
		for (int i = 0; i < lastSectionBytes.Count; i++)
		{
			num += lastSectionBytes[i].Item2;
		}
		for (int j = 0; j < lastSectionBytes.Count; j++)
		{
			var (key, num2) = lastSectionBytes[j];
			if (!_sectionStats.TryGetValue(key, out var value))
			{
				value = new SectionStat();
				_sectionStats[key] = value;
			}
			value.Last = num2;
			if (num2 > value.Max)
			{
				value.Max = num2;
			}
		}
		_sectionSort.Clear();
		foreach (KeyValuePair<string, SectionStat> sectionStat in _sectionStats)
		{
			_sectionSort.Add(sectionStat);
		}
		_sectionSort.Sort((KeyValuePair<string, SectionStat> a, KeyValuePair<string, SectionStat> b) => b.Value.Max.CompareTo(a.Value.Max));
		_sectionRows.Clear();
		int num3 = 0;
		foreach (KeyValuePair<string, SectionStat> item2 in _sectionSort)
		{
			if (item2.Value.Last < 16 && item2.Value.Max < 16)
			{
				num3++;
				continue;
			}
			float num4 = ((num > 0) ? ((float)item2.Value.Last * 100f / (float)num) : 0f);
			Vector4 item = ((num4 >= 25f) ? Red : ((num4 >= 10f) ? Amber : Sage));
			_sectionRows.Add((item2.Key, FmtBytes((ulong)item2.Value.Last), FmtBytes((ulong)item2.Value.Max), num4.ToString("0.0") + "%", item));
		}
		_sectionHiddenLabel = ((num3 > 0) ? $"...{num3} more, all under {16}B" : "");
	}

	public static void Draw()
	{
		if (NetworkManager.IsActive && Time.unscaledTime - _lastSample >= 1f)
		{
			_lastSample = Time.unscaledTime;
			Sample();
		}
		ImGui.Begin("Network Debug", ref Show, (ImGuiWindowFlags)288);
		ImGui.SetWindowSize(ImguiHelper.StandardResizableScaled, ImGuiCond.Once);
		if (!NetworkManager.IsActive)
		{
			ImGui.TextColored(Lbl, "Not connected. Host or join a game to see network stats.");
		}
		else if (ImGui.BeginTabBar("##ndtabs"))
		{
			if (ImGui.BeginTabItem("Overview"))
			{
				if (NetworkManager.IsServer)
				{
					DrawHost();
				}
				else
				{
					DrawClient();
				}
				ImGui.EndTabItem();
			}
			if (ImGui.BeginTabItem("Streams"))
			{
				if (NetworkManager.IsServer)
				{
					DrawHostStreams();
				}
				else
				{
					DrawClientStreams();
				}
				ImGui.EndTabItem();
			}
			ImGui.EndTabBar();
		}
		ImGui.End();
		if (!Show)
		{
			Reset();
		}
	}

	private static void Graph(string title, string id, float[] ring, Vector4 color, float min, float max, string overlay)
	{
		ImGui.TextColored(Lbl, title);
		float x = ImGui.GetContentRegionAvail().x;
		ImGui.PushStyleColor(ImGuiCol.PlotLines, color);
		ImGui.PlotLines(id, ref ring, ring.Length, 0, overlay, min, max, new Vector2(x, 64f * ImguiHelper.UIScale));
		ImGui.PopStyleColor();
	}

	private static void DrawHost()
	{
		ImGui.TextColored((NetworkServer.CurrentTickIntervalMs > 50) ? Amber : Sage, _header);
		ImGui.Separator();
		ImGui.Columns(2, "agg", border: false);
		Graph("Adaptive tick (Hz)", "##nd_tick", _tickHz, Amber, 0f, 22f, _tickOv);
		ImGui.NextColumn();
		Graph("Worst backlog (MB)", "##nd_worst", _worstMB, Red, 0f, NiceMax(_worstMB, 8f), _worstOv);
		ImGui.NextColumn();
		Graph("Server upload (KB/s)", "##nd_up", _outKBs, Sky, 0f, NiceMax(_outKBs, 64f), _outOv);
		ImGui.NextColumn();
		Graph("Tick payload (KB)", "##nd_pay", _payloadKB, Sky, 0f, NiceMax(_payloadKB, 16f), _payOv);
		ImGui.Columns(1);
		ImGui.Separator();
		int num = 0;
		float num2 = 1f;
		foreach (KeyValuePair<long, Conn> conn in _conns)
		{
			if (num >= _hist.Length)
			{
				break;
			}
			float num3 = (float)conn.Value.Last.SendQueueBytes / 1048576f;
			_hist[num++] = num3;
			if (num3 > num2)
			{
				num2 = num3;
			}
		}
		ImGui.TextColored(Lbl, "Backlog by client — now (MB)");
		ImGui.PushStyleColor(ImGuiCol.PlotHistogram, Red);
		ImGui.PlotHistogram("##nd_hist", ref _hist, num, 0, "", 0f, num2 * 1.12f, new Vector2(ImGui.GetContentRegionAvail().x, 60f * ImguiHelper.UIScale));
		ImGui.PopStyleColor();
		ImGui.Separator();
		if (!BeginStyledTable("netconns", 6))
		{
			return;
		}
		ImGui.TableSetupColumn("Client");
		ImGui.TableSetupColumn("Ping a/l/l");
		ImGui.TableSetupColumn("Loss");
		ImGui.TableSetupColumn("SENDQ");
		ImGui.TableSetupColumn("Out");
		ImGui.TableSetupColumn("SENDQ history");
		ImGui.TableHeadersRow();
		float scale_max = NiceMax(_worstMB, 8f);
		foreach (KeyValuePair<long, Conn> conn2 in _conns)
		{
			Conn value = conn2.Value;
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.TextColored(value.Color, value.Name);
			ImGui.TableNextColumn();
			ImGui.Text(value.CellPing);
			ImGui.TableNextColumn();
			ImGui.Text(value.CellLoss);
			ImGui.TableNextColumn();
			ImGui.Text(value.CellSendq);
			ImGui.TableNextColumn();
			ImGui.Text(value.CellOut);
			ImGui.TableNextColumn();
			ImGui.PushStyleColor(ImGuiCol.PlotLines, value.Color);
			ImGui.PlotLines(value.SparkId, ref value.SendqMB, value.SendqMB.Length, 0, "", 0f, scale_max, new Vector2(120f * ImguiHelper.UIScale, 22f * ImguiHelper.UIScale));
			ImGui.PopStyleColor();
		}
		EndStyledTable();
	}

	private static bool BeginStyledTable(string id, int columns, ImGuiTableFlags flags = (ImGuiTableFlags)1984)
	{
		ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(2f, 2f) * ImguiHelper.UIScale);
		ImGui.PushStyleColor(ImGuiCol.TableHeaderBg, new Vector4(0.13f, 0.14f, 0.17f, 1f));
		ImGui.PushStyleColor(ImGuiCol.TableRowBg, new Vector4(0.06f, 0.07f, 0.09f, 1f));
		ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, new Vector4(0.1f, 0.11f, 0.13f, 1f));
		ImGui.PushStyleColor(ImGuiCol.TableBorderStrong, new Vector4(0.2f, 0.22f, 0.26f, 1f));
		ImGui.PushStyleColor(ImGuiCol.TableBorderLight, new Vector4(0.13f, 0.14f, 0.17f, 1f));
		if (ImGui.BeginTable(id, columns, flags))
		{
			return true;
		}
		ImGui.PopStyleColor(5);
		ImGui.PopStyleVar();
		return false;
	}

	private static void EndStyledTable()
	{
		ImGui.EndTable();
		ImGui.PopStyleColor(5);
		ImGui.PopStyleVar();
	}

	private static void GraphOverlaid(string title, float[] a, float[] b, string overlay)
	{
		ImGui.TextColored(Lbl, title);
		float scale_max = Mathf.Max(_overlayPeak, 1f) * 1.12f;
		Vector2 graph_size = new Vector2(ImGui.GetContentRegionAvail().x, 96f * ImguiHelper.UIScale);
		Vector2 cursorPos = ImGui.GetCursorPos();
		ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGuiColor.Float4.Transparent);
		ImGui.SetCursorPos(cursorPos);
		ImGui.PushStyleColor(ImGuiCol.PlotLines, Sage);
		ImGui.PlotLines("", ref a, a.Length, 0, overlay, 0f, scale_max, graph_size);
		ImGui.PopStyleColor();
		ImGui.SetCursorPos(cursorPos);
		ImGui.PushStyleColor(ImGuiCol.PlotLines, Sky);
		ImGui.PlotLines("", ref b, b.Length, 0, "", 0f, scale_max, graph_size);
		ImGui.PopStyleColor(2);
		ImGui.TextColored(Sage, _legendState);
		ImGui.SameLine(0f, 18f * ImguiHelper.UIScale);
		ImGui.TextColored(Sky, _legendPhys);
	}

	private static void DrawStreamsTable(string countHeader, string lastHeader)
	{
		if (BeginStyledTable("ndstreams", 6))
		{
			ImGui.TableSetupColumn("STREAM");
			ImGui.TableSetupColumn("TICK");
			ImGui.TableSetupColumn("LAST");
			ImGui.TableSetupColumn("RATE");
			ImGui.TableSetupColumn(countHeader);
			ImGui.TableSetupColumn(lastHeader);
			ImGui.TableHeadersRow();
			DrawStreamRow("State", Sage, _stateRow);
			DrawStreamRow("Physics", Sky, _physRow);
			EndStyledTable();
		}
	}

	private static void DrawStreamRow(string name, Vector4 color, StreamRow row)
	{
		ImGui.TableNextRow();
		ImGui.TableNextColumn();
		ImGui.TextColored(color, name);
		ImGui.TableNextColumn();
		ImGui.Text(row.Tick);
		ImGui.TableNextColumn();
		ImGui.Text(row.Last);
		ImGui.TableNextColumn();
		ImGui.Text(row.Rate);
		ImGui.TableNextColumn();
		ImGui.Text(row.Cycles);
		ImGui.TableNextColumn();
		ImGui.TextColored(row.DroppedColor, row.Dropped);
	}

	private static void DrawStatTile(string value, Vector4 valueColor, string label)
	{
		ImGui.TableNextColumn();
		ImGui.TextColored(valueColor, value);
		ImGui.TextColored(Lbl, label);
	}

	private static void DrawHostStreams()
	{
		DrawStreamsTable("CYCLES", "SUPPRESSED");
		ImGui.Separator();
		GraphOverlaid("Payload by stream (KB/tick)", _payloadKB, _physKB, _overlayPeakOv);
		Graph("Physics records (awake bodies)", "##nd_physrec", _physRecords, Sage, 0f, NiceMax(_physRecords, 32f), _physRecOv);
		ImGui.Separator();
		ImGui.TextColored(Lbl, "State payload by section: raw bytes before compression, sorted by peak");
		if (!BeginStyledTable("ndsections", 4, (ImGuiTableFlags)33556416))
		{
			return;
		}
		ImGui.TableSetupColumn("Section");
		ImGui.TableSetupColumn("Bytes");
		ImGui.TableSetupColumn("Max");
		ImGui.TableSetupColumn("Share");
		ImGui.TableHeadersRow();
		foreach (var sectionRow in _sectionRows)
		{
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.Text(sectionRow.name);
			ImGui.TableNextColumn();
			ImGui.TextColored(sectionRow.color, sectionRow.bytes);
			ImGui.TableNextColumn();
			ImGui.Text(sectionRow.max);
			ImGui.TableNextColumn();
			ImGui.TextColored(sectionRow.color, sectionRow.share);
		}
		if (_sectionHiddenLabel.Length > 0)
		{
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.TextColored(Lbl, _sectionHiddenLabel);
			ImGui.TableNextColumn();
			ImGui.TableNextColumn();
			ImGui.TableNextColumn();
		}
		EndStyledTable();
	}

	private static void DrawClientStreams()
	{
		DrawStreamsTable("RECEIVED", "DROPPED");
		if (ImGui.BeginTable("ndtiles", 3, ImGuiTableFlags.None))
		{
			ImGui.TableNextRow();
			DrawStatTile(_tileApplied, Sky, "records applied");
			DrawStatTile(_tileUnknown, _tileUnknownColor, "unknown id");
			DrawStatTile(_tileStale, _tileStaleColor, "stale skipped");
			ImGui.EndTable();
		}
		ImGui.Separator();
		GraphOverlaid("Download by stream (KB/s)", _cliStateKBs, _cliPhysKBs, _overlayPeakOv);
		Graph("Physics records applied (/s)", "##nd_cliapp", _cliApplied, Sky, 0f, NiceMax(_cliApplied, 32f), _cliAppliedOv);
	}

	private static void DrawClient()
	{
		if (_conns.Count == 0)
		{
			ImGui.TextColored(Lbl, "Waiting for host connection…");
			return;
		}
		Conn conn = null;
		using (Dictionary<long, Conn>.Enumerator enumerator = _conns.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				conn = enumerator.Current.Value;
			}
		}
		ImGui.TextColored(Sky, _header);
		ImGui.Separator();
		ImGui.Columns(2, "cl", border: false);
		Graph("Latency (ms)", "##nd_tick", conn.Ping, Sky, 0f, NiceMax(conn.Ping, 80f), conn.OvPing);
		ImGui.NextColumn();
		Graph("Packet loss (%)", "##nd_worst", conn.Loss, Red, 0f, Mathf.Max(5f, NiceMax(conn.Loss, 2f)), conn.OvLoss);
		ImGui.NextColumn();
		Graph("Download (KB/s)", "##nd_up", conn.DownKBs, Sky, 0f, NiceMax(conn.DownKBs, 32f), conn.OvDown);
		ImGui.NextColumn();
		Graph("Upload (KB/s)", "##nd_pay", conn.UpKBs, Sage, 0f, NiceMax(conn.UpKBs, 8f), conn.OvUp);
		ImGui.Columns(1);
	}
}
