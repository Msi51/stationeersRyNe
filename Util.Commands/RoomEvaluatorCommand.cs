using System.Text;
using Assets.Scripts.GridSystem;
using Rooms;

namespace Util.Commands;

internal class RoomEvaluatorCommand : CommandBase
{
	public override string HelpText => "Prints room evaluator diagnostics or toggles its pause state. Defaults to 'status' when no argument is given.";

	public override string[] Arguments => new string[1] { "[status | pause]" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("roomevaluator"))
		{
			return null;
		}
		string result = "status";
		if (args.Length != 0)
		{
			CommandBase.Get(args, 0, "action", out result);
		}
		RoomEvaluator instance = RoomEvaluator.Instance;
		if (instance == null)
		{
			return "RoomEvaluator not initialized.";
		}
		string text = result.ToLower();
		if (!(text == "pause"))
		{
			if (!(text == "status"))
			{
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("RoomEvaluator Last Tick:");
			stringBuilder.AppendLine($"  Queue depth:       {instance.LastQueueDepth}");
			stringBuilder.AppendLine($"  Pending:           {instance.PendingCount}");
			stringBuilder.AppendLine($"  Processed/Skipped/Blocked: {instance.LastGridsProcessed}/{instance.LastGridsSkipped}/{instance.LastGridsBlocked}");
			stringBuilder.AppendLine($"  Fills run:         {instance.LastFillsRun} (success: {instance.LastFillsSuccess}, limit: {instance.LastFillsIterationLimit})");
			stringBuilder.AppendLine($"  Total iterations:  {instance.LastTotalIterations}");
			stringBuilder.AppendLine($"  Rooms created:     {instance.LastRoomsCreated}");
			stringBuilder.AppendLine($"  Atmos cloned:      {instance.LastAtmosCloned}");
			stringBuilder.AppendLine($"  Time:              {instance.LastTotalMs:0.00}ms");
			stringBuilder.AppendLine($"  Total rooms:       {Room.AllRooms.Count}");
			return stringBuilder.ToString();
		}
		instance.Pause = !instance.Pause;
		return $"RoomEvaluator paused: {instance.Pause}.";
	}
}
