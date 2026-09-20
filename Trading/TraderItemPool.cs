using System;
using System.Collections.Generic;

namespace Trading;

public class TraderItemPool
{
	public readonly int Index;

	private List<TransactionData> Transactions = new List<TransactionData>(8);

	public TraderItemPool(int index)
	{
		Index = index;
	}

	public void Add(TransactionData transactionData)
	{
		if (!Transactions.Contains(transactionData))
		{
			Transactions.Add(transactionData);
		}
	}

	public TransactionData Pick(Random random)
	{
		if (Transactions.Count == 0)
		{
			return null;
		}
		int num = 0;
		for (int i = 0; i < Transactions.Count; i++)
		{
			TransactionData transactionData = Transactions[i];
			num += transactionData.ItemPoolData.Weight;
		}
		int num2 = random.Next(0, num);
		for (int j = 0; j < Transactions.Count; j++)
		{
			TransactionData transactionData2 = Transactions[j];
			if (num2 < transactionData2.ItemPoolData.Weight)
			{
				return transactionData2;
			}
			num2 -= transactionData2.ItemPoolData.Weight;
		}
		throw new ArgumentOutOfRangeException();
	}
}
