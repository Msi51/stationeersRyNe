using System.Collections.Generic;

namespace Assets.Scripts.UI;

public class PreviousValues
{
	public List<string> Values = new List<string>();

	public int CurrentIndex = -1;

	public bool Contains(string value)
	{
		return Values.Contains(value);
	}

	public bool Next()
	{
		if (Values.Count == 0)
		{
			return true;
		}
		if (CurrentIndex + 1 < Values.Count)
		{
			CurrentIndex++;
		}
		return true;
	}

	public bool Previous()
	{
		if (CurrentIndex >= 0)
		{
			CurrentIndex--;
		}
		if (Values.Count != 0)
		{
			_ = CurrentIndex;
			_ = -1;
		}
		return true;
	}

	public string Text()
	{
		if (CurrentIndex > Values.Count || CurrentIndex == -1)
		{
			return InputWindow.PreviousString;
		}
		return Values[CurrentIndex];
	}

	public bool IsAtMax()
	{
		return CurrentIndex >= Values.Count - 1;
	}

	public bool IsAtMin()
	{
		return CurrentIndex == -1;
	}

	public bool HasText()
	{
		return Values.Count > 0;
	}
}
