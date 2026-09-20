using System.Text;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.Util;

namespace Trading;

public class RoomCondition : ConditionData
{
	[XmlAttribute("RoomType")]
	public RoomType RoomType = RoomType.Undefined;

	[XmlAttribute("MinSize")]
	public int MinSize;

	[XmlAttribute("MaxSize")]
	public int MaxSize = -1;

	public override string DebugName => $"RoomType {RoomType}";

	public override int GetChecksum()
	{
		return (int)(((((((uint)base.GetChecksum() ^ (uint)RoomType) * 41) ^ (uint)MinSize) * 41) ^ (uint)MaxSize) * 41);
	}

	public override bool Evaluate<T>(T t)
	{
		if (t is Thing thing)
		{
			Room room = RoomController.World.GetRoom(new WorldGrid(thing.Position));
			if (room == null)
			{
				return false;
			}
			bool num = RoomType == RoomType.Undefined || room.RoomType == RoomType;
			bool flag = room.Grids.Count >= MinSize && (MaxSize == -1 || room.Grids.Count <= MaxSize);
			if (num && flag)
			{
				return base.Evaluate(t);
			}
			return false;
		}
		if (t is Room room2)
		{
			bool num2 = RoomType == RoomType.Undefined || room2.RoomType == RoomType;
			bool flag2 = room2.Grids.Count >= MinSize && (MaxSize == -1 || room2.Grids.Count <= MaxSize);
			if (num2 && flag2)
			{
				return base.Evaluate(t);
			}
			return false;
		}
		return false;
	}

	protected override void AppendToolTip(StringBuilder stringBuilder, int generations)
	{
		string value = RoomType.ToString();
		for (int i = 0; i < generations; i++)
		{
			stringBuilder.Append("    ");
		}
		if (RoomType != RoomType.Undefined)
		{
			stringBuilder.AppendLine(GameStrings.RoomState.AsString(value.AsColor("yellow")));
		}
		if (MinSize > 0)
		{
			stringBuilder.AppendFormat(GameStrings.RoomMinSizeCondition, StringManager.Get(MinSize));
		}
		if (MaxSize > 0)
		{
			stringBuilder.AppendFormat(GameStrings.RoomMaxSizeCondition, StringManager.Get(MaxSize));
		}
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
