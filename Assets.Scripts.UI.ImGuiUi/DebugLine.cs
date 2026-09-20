namespace Assets.Scripts.UI.ImGuiUi;

public class DebugLine
{
	private int COUNT = 5;

	private long[] _times;

	private long _currentTime;

	private int _currentIndex;

	public DebugLine()
	{
		_times = new long[COUNT];
	}

	public float GetCurrent()
	{
		return _currentTime;
	}

	public float GetAverage()
	{
		float num = 0f;
		long[] times = _times;
		foreach (long num2 in times)
		{
			num += (float)num2;
		}
		return num / (float)_times.Length;
	}

	public void Set(long elapsed)
	{
		_currentTime = elapsed;
		_times[_currentIndex] = elapsed;
		_currentIndex++;
		if (_currentIndex >= _times.Length)
		{
			_currentIndex = 0;
		}
	}
}
