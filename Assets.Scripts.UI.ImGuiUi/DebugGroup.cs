using System.Collections.Generic;
using System.Diagnostics;

namespace Assets.Scripts.UI.ImGuiUi;

public class DebugGroup
{
	public Dictionary<string, DebugLine> Lines = new Dictionary<string, DebugLine>(50);

	private Stopwatch _stopwatch = new Stopwatch();

	private long _lastElapsed;

	private bool _hasBegun;

	public void Begin()
	{
		_hasBegun = true;
		_stopwatch.Restart();
		_lastElapsed = 0L;
	}

	public void End()
	{
		_stopwatch.Stop();
		_lastElapsed = 0L;
		_hasBegun = false;
	}

	public void Reset()
	{
		_lastElapsed = _stopwatch.ElapsedMilliseconds;
	}

	public void Update(string key)
	{
		if (_hasBegun)
		{
			if (!Lines.TryGetValue(key, out var value))
			{
				value = new DebugLine();
				Lines.Add(key, value);
			}
			long elapsedMilliseconds = _stopwatch.ElapsedMilliseconds;
			value.Set(elapsedMilliseconds - _lastElapsed);
			_lastElapsed = elapsedMilliseconds;
		}
	}
}
