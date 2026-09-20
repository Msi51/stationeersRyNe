using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Util;
using Objects.Rockets;

namespace Util.Commands;

public class SpaceMapCommand : CommandBase
{
	private const string ARG_REGENERATE = "regenerate";

	private const string ARG_FILL = "fill";

	private const string ARG_CHART = "chart";

	private const string ARG_TEST_PATHS = "testpaths";

	public override string HelpText => "Inspects or manipulates the current space map. With no arguments, prints the current map's tree. Use 'regenerate' to rebuild it, 'fill' to chart and populate dynamic sites, 'chart' to mark all entry/static nodes as charted, or 'testpaths' to run the path validator.";

	public override string[] Arguments => new string[4] { "regenerate", "fill", "chart", "testpaths" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame | CommandScope.HostOrSinglePlayer;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("spacemap"))
		{
			return null;
		}
		if (args.Length > 2)
		{
			return "Invalid syntax";
		}
		if (SpaceMap.Current == null)
		{
			ConsoleWindow.PrintError("Spacemap is not loaded.", suppressStacktrace: true);
			return null;
		}
		if (args.Length == 0)
		{
			SpaceMap current = SpaceMap.Current;
			ConsoleWindow.PrintAction("SpaceMap: " + current.DisplayName + " #" + current.Id);
			ConsoleWindow.Print(SpaceMap.Current.EntryNode.ToTreeWithChildren().ToString());
			return null;
		}
		return args[0] switch
		{
			"regenerate" => Regenerate(), 
			"fill" => FillMap(), 
			"chart" => ChartAllNodes(), 
			"testpaths" => TestPaths(), 
			_ => "Invalid syntax", 
		};
	}

	public string ChartAllNodes()
	{
		ConsoleWindow.PrintAction($"Charting all nodes for {SpaceMap.Current}.");
		foreach (SpaceMapNode node in SpaceMap.Current.Nodes)
		{
			NodeType nodeType = node.NodeType;
			if (nodeType == NodeType.Entry || nodeType == NodeType.Static)
			{
				node.IsCharted = true;
				if (node.SurveyData != null)
				{
					node.SurveyPoints = node.SurveyData.Difficulty;
				}
			}
		}
		return null;
	}

	private string FillMap()
	{
		ChartAllNodes();
		List<SpaceMapNode> nodes = SpaceMap.Current.Nodes;
		for (int num = nodes.Count - 1; num >= 0; num--)
		{
			SpaceMapNode spaceMapNode = nodes[num];
			if (spaceMapNode.NodeType == NodeType.Static && spaceMapNode.DiscoverData != null)
			{
				while (spaceMapNode.DynamicNodeCount() < spaceMapNode.DynamicNodeCapacity)
				{
					SpaceMapNode spaceMapNode2 = SpaceMapNode.CreateSite(spaceMapNode.DiscoverData.SiteGenerations.Pick(), spaceMapNode, 0L);
					spaceMapNode2.SurveyPoints = spaceMapNode2.SurveyData.Difficulty;
				}
			}
		}
		return null;
	}

	private string Regenerate()
	{
		SpaceMap.RefreshMap(SpaceMap.Current.Id);
		return null;
	}

	private string TestPaths()
	{
		SpaceMap.TestAllPaths();
		return null;
	}
}
