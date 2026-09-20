using Assets.Scripts.Util;

namespace Assets.Scripts.Objects.Electrical;

public class LogicBinding
{
	public string Header;

	public string Label;

	public LogicBinding(int deviceIndex, string label)
	{
		Header = "d" + ((deviceIndex >= 0) ? StringManager.Get(deviceIndex) : "b");
		Label = label;
	}

	public LogicBinding(string label)
	{
		Header = "db";
		Label = label;
	}
}
