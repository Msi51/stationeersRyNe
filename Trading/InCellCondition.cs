using System.Text;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.Util;
using UnityEngine;

namespace Trading;

public class InCellCondition : ConditionData
{
	[XmlAttribute("X")]
	public int CellX;

	[XmlAttribute("Y")]
	public int CellY;

	[XmlAttribute("Z")]
	public int CellZ;

	[XmlIgnore]
	public Vector3Int Grid;

	public override string DebugName => "Should be in cell: " + StringManager.Get(Grid);

	public override void Initialise()
	{
		base.Initialise();
		Grid = new Vector3Int(CellX, CellY, CellZ);
	}

	public override int GetChecksum()
	{
		return (((((base.GetChecksum() ^ CellX) * 41) ^ CellY) * 41) ^ CellZ) * 41;
	}

	public override bool Evaluate<T>(T t)
	{
		bool flag = false;
		if (t is DynamicThing dynamicThing)
		{
			Grid3 grid = GridController.World.WorldToLocalGrid(dynamicThing.CenterPosition);
			flag = grid.x == Grid.x && grid.y == Grid.y && grid.z == Grid.z;
		}
		if (flag)
		{
			return base.Evaluate(t);
		}
		return false;
	}

	protected override void AppendToolTip(StringBuilder stringBuilder, int generations)
	{
		for (int i = 0; i < generations; i++)
		{
			stringBuilder.Append("    ");
		}
		stringBuilder.AppendLine(GameStrings.HelperHintInCellCondition.AsString(StringManager.Get(Grid)));
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
