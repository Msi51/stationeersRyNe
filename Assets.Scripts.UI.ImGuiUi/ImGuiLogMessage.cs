using UnityEngine;

namespace Assets.Scripts.UI.ImGuiUi;

public struct ImGuiLogMessage
{
	public string Log;

	public LogType Type;

	public string Stacktrace;

	public uint TextColor;
}
