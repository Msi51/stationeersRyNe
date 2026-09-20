using System.Text;
using Assets.Scripts.Util;

namespace Assets.Scripts.UI.HelperHints.Extensions;

public static class HelperHintsStringBuilderExtensions
{
	private static class SpriteIndex
	{
		public const int RightArrow = 0;

		public const int DownArrow = 1;

		public const int CloseButtonRed = 2;
	}

	private const float IndentSize = 3f;

	private const float IndentOffset = 1f;

	private static string GasColor => "#44AD83";

	private static void BeginLineHeight(this StringBuilder sb, float percent)
	{
		sb.AppendFormat("<line-height={0}%>", StringManager.Get(percent));
	}

	private static void EndLineHeight(this StringBuilder sb)
	{
		sb.AppendFormat("</line-height>");
	}

	public static void BeginSize(this StringBuilder sb, float percent)
	{
		sb.AppendFormat("<size={0}%>", StringManager.Get(percent));
	}

	public static void EndSize(this StringBuilder sb)
	{
		sb.AppendFormat("</size>");
	}

	public static void BeginIndent(this StringBuilder sb, int indent)
	{
		sb.AppendFormat("<indent={0}%>", StringManager.Get(1f + 3f * (float)indent));
	}

	public static void EndIndent(this StringBuilder sb)
	{
		sb.Append("</indent>");
	}

	public static void BeginLink(this StringBuilder sb, string link)
	{
		sb.AppendFormat("<link=\"{0}\">", link);
	}

	public static void EndLink(this StringBuilder sb)
	{
		sb.Append("</link>");
	}

	public static void BeginColor(this StringBuilder sb, string color)
	{
		sb.AppendFormat("<color={0}>", color);
	}

	public static void EndColor(this StringBuilder sb)
	{
		sb.Append("</color>");
	}

	private static void AppendSprite(this StringBuilder sb, int index)
	{
		sb.AppendFormat("<sprite={0}>", StringManager.Get(index));
	}

	private static void AppendSprite(this StringBuilder sb, int index, string color)
	{
		sb.AppendFormat("<sprite={0} color=\"{1}\">", StringManager.Get(index), color);
	}

	public static void AppendColorText(this StringBuilder sb, string color, string text, bool italic = false)
	{
		sb.AppendFormat(italic ? "<color={0}><i>{1}</i></color>" : "<color={0}>{1}</color>", color, text);
	}

	public static void AppendBranchColorText(this StringBuilder sb, string color, string text, bool italic = false)
	{
		sb.AppendColorText(color, text, italic: true);
	}

	public static void AppendLinkText(this StringBuilder sb, string link, string text)
	{
		sb.AppendFormat("<link=\"{0}\"><color=\"green\">{1}</color></link>", link, text);
	}

	public static void AppendGasText(this StringBuilder sb, string text)
	{
		sb.AppendFormat("<color={0}>{1}</color>", GasColor, text);
	}

	public static void HalfNewline(this StringBuilder sb)
	{
		sb.Append("<line-height=50%>\n</line-height>");
	}

	public static void Newline(this StringBuilder sb)
	{
		sb.Append("\n");
	}

	public static void OneAndHalfNewline(this StringBuilder sb)
	{
		sb.Append("<line-height=150%>\n</line-height>");
	}

	public static void DoubleNewline(this StringBuilder sb)
	{
		sb.Append("<line-height=200%>\n</line-height>");
	}

	public static void AppendGroupHeader(this StringBuilder sb, bool expanded, bool strikeout, string color, string text, string expandId, string dismissId)
	{
		sb.BeginLink(expandId);
		sb.AppendSprite(expanded ? 1 : 0);
		if (strikeout && Localization.CurrentLanguage != LanguageCode.CN)
		{
			sb.Append("<s>");
		}
		sb.Append("<allcaps>");
		sb.AppendColorText(color, text);
		sb.Append("</allcaps>");
		if (strikeout && Localization.CurrentLanguage != LanguageCode.CN)
		{
			sb.Append("</s>");
		}
		sb.EndLink();
		sb.BeginLink(dismissId);
		sb.AppendSprite(2);
		sb.EndLink();
	}
}
