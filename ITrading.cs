using System.Collections.Generic;

public interface ITrading
{
	static List<ITrading> AllTradingList;

	static ITrading()
	{
		AllTradingList = new List<ITrading>();
	}
}
