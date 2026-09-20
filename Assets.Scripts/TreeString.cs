using System.Collections.Generic;
using System.Text;

namespace Assets.Scripts;

public class TreeString
{
	public string String;

	public TreeString Parent;

	public List<TreeString> Children = new List<TreeString>();

	private const string IND_NEW = "   ";

	private const string IND_CHILD = "│  ";

	private const string ARROW_MID = "├─ ";

	private const string ARROW_END = "└─ ";

	private const string IND_CHILD_VAR = "│  ";

	private const string ARROW_MID_VAR = "├─ ";

	private const string ARROW_END_VAR = "└─ ";

	private bool _variable;

	public static TreeString Node(string myString, TreeString myParent = null)
	{
		return new TreeString(myString, myParent);
	}

	public static TreeString Variable(string myString, TreeString myParent = null)
	{
		return new TreeString(myString, myParent)
		{
			_variable = true
		};
	}

	public TreeString(string myString, TreeString myParent = null)
	{
		String = myString;
		Parent = myParent;
		Parent?.Children.Add(this);
	}

	public void ToConsole()
	{
		ConsoleWindow.PrintAction(String);
		StringBuilder stringBuilder = new StringBuilder();
		BuildString(stringBuilder, "", skipFirst: true);
		ConsoleWindow.Print(stringBuilder.ToString());
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		BuildString(stringBuilder, "");
		return stringBuilder.ToString();
	}

	private string GetArrowMid()
	{
		if (!_variable)
		{
			return "├─ ";
		}
		return "├─ ";
	}

	private string GetArrowEnd()
	{
		if (!_variable)
		{
			return "└─ ";
		}
		return "└─ ";
	}

	private string GetIndentChild()
	{
		if (!_variable)
		{
			return "│  ";
		}
		return "│  ";
	}

	private void BuildString(StringBuilder builder, string indent, bool skipFirst = false)
	{
		if (Parent != null)
		{
			string text = ((Parent.Children.IndexOf(this) == Parent.Children.Count - 1) ? GetArrowEnd() : GetArrowMid());
			builder.AppendLine(indent + text + String);
			indent += ((Parent.Children.IndexOf(this) == Parent.Children.Count - 1) ? "   " : GetIndentChild());
		}
		else if (!skipFirst)
		{
			builder.AppendLine(String);
		}
		foreach (TreeString child in Children)
		{
			child.BuildString(builder, indent);
		}
	}
}
