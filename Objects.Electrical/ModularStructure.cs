using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using Networks;

namespace Objects.Electrical;

public abstract class ModularStructure<TModular, TNetworkedModular> : BaseModularStructure where TModular : ModularStructure<TModular, TNetworkedModular> where TNetworkedModular : INetworkedPad, INetworkedStructure
{
	protected static readonly Dictionary<Type, List<INetworkedStructure>> FoundStructures = new Dictionary<Type, List<INetworkedStructure>>();

	public StructureNetwork StructureNetwork { get; set; }

	public abstract StructureNetwork CreateNewNetwork();

	public override void WillJoinNetwork(Span<ConnectionRef> connBuf, ref int connCount)
	{
		base.WillJoinNetwork(connBuf, ref connCount);
		foreach (Connection openEnd in OpenEnds)
		{
			if (openEnd.ConnectionType == NetworkConnectionType)
			{
				Grid3 localGrid = base.GridController.WorldToLocalGrid(openEnd.Transform.position, GridSize, GridOffset);
				SmallCell smallCell = base.GridController.GetSmallCell(localGrid);
				if ((object)smallCell?.Other != null && smallCell.Other as TModular != this && smallCell.Other.IsConnected(openEnd) && smallCell.Other is INetworkedPad)
				{
					connBuf[connCount++] = openEnd;
				}
				else if ((object)smallCell?.Device != null && smallCell.Device.IsConnected(openEnd) && smallCell.Device is INetworkedPad)
				{
					connBuf[connCount++] = openEnd;
				}
			}
		}
	}

	public override void OnRegistered(Cell cell)
	{
		if (GameManager.GameState != GameState.Loading && GameManager.RunSimulation)
		{
			ConnectToStructureNetworks();
		}
		base.OnRegistered(cell);
	}

	private void ConnectToStructureNetworks()
	{
		List<StructureNetwork> structureNetworks = StructureNetwork.ConnectedNetworks(this as INetworkedStructure);
		base.Position = Transform.position;
		if (StructureNetwork.Merge(structureNetworks, out var mergedNetwork))
		{
			mergedNetwork.Add(this as INetworkedStructure);
		}
		else
		{
			CreateNewNetwork().Add(this as INetworkedStructure);
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		if (StructureNetwork == null)
		{
			ConnectToStructureNetworks();
		}
	}

	public List<INetworkedStructure> ConnectedStructures()
	{
		Type typeFromHandle = typeof(TModular);
		if (!FoundStructures.TryGetValue(typeFromHandle, out var value))
		{
			value = new List<INetworkedStructure>();
			FoundStructures[typeFromHandle] = value;
		}
		value.Clear();
		foreach (Connection openEnd in OpenEnds)
		{
			Grid3 localGrid = base.GridController.WorldToLocalGrid(openEnd.Transform.position);
			SmallCell smallCell = base.GridController.GetSmallCell(localGrid);
			if (smallCell?.Other != null && smallCell.Other as TModular != this && smallCell.Other.IsConnected(openEnd) && smallCell.Other is TNetworkedModular val)
			{
				FoundStructures[typeFromHandle].Add(val);
			}
			else if (smallCell?.Device != null && smallCell.Device.IsConnected(openEnd) && smallCell.Device is TNetworkedModular val2)
			{
				FoundStructures[typeFromHandle].Add(val2);
			}
		}
		return FoundStructures[typeFromHandle];
	}

	public override void OnDestroy()
	{
		if (!Singleton<GameManager>.IsQuitting && !IsCursor && GameManager.GameState != GameState.None)
		{
			StructureNetwork?.Remove(this as INetworkedStructure);
			base.OnDestroy();
		}
	}

	public override bool IsConnected(Connection otherEnd)
	{
		if (otherEnd.ConnectionType != NetworkConnectionType)
		{
			return false;
		}
		Grid3 localGrid = otherEnd.GetLocalGrid();
		foreach (Connection openEnd in OpenEnds)
		{
			Grid3 localGrid2 = openEnd.GetLocalGrid();
			if (localGrid == localGrid2)
			{
				return true;
			}
		}
		return false;
	}
}
