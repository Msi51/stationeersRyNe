using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Util;

namespace Objects.Rockets;

public readonly struct SpaceMapCode
{
	public static readonly Dictionary<ulong, SpaceMapNode> LookUp = new Dictionary<ulong, SpaceMapNode>();

	public readonly ulong Value;

	private const ulong STATIC_NODE_ADDRESS_SPACE_BEGIN = 1uL;

	private const ulong STATIC_NODE_ADDRESS_SPACE_END = 19uL;

	private const ulong GENERATED_NODE_ADDRESS_SPACE_BEGIN = 20uL;

	private const ulong GENERATED_NODE_ADDRESS_SPACE_END = 39uL;

	private const ulong LAUNCHPAD_NODE_ADDRESS_SPACE_BEGIN = 40uL;

	private const ulong LAUNCHPAD_NODE_ADDRESS_SPACE_END = 99uL;

	public readonly string String;

	public const ulong MAXIMUM_ADDRESS_VALUE = 9007199254740992uL;

	public bool IsValid => Value != 0;

	public static implicit operator string(SpaceMapCode code)
	{
		return code.String;
	}

	public static implicit operator ulong(SpaceMapCode code)
	{
		return code.Value;
	}

	public static SpaceMapCode Create(SpaceMapNode parentNode, NodeType nodeType = NodeType.Generated)
	{
		return new SpaceMapCode(parentNode, nodeType);
	}

	public static SpaceMapCode Create(ulong value)
	{
		return new SpaceMapCode(value);
	}

	public SpaceMapCode(SpaceMapNode parentNode, NodeType nodeType)
	{
		ulong num = nodeType switch
		{
			NodeType.Entry => throw new Exception("Entry Nodes must have a unique predefined code between 01 and 99 "), 
			NodeType.Static => 1uL, 
			NodeType.Generated => 20uL, 
			NodeType.LaunchPad => 40uL, 
			NodeType.LowOrbitLaunchPad => 40uL, 
			_ => throw new ArgumentOutOfRangeException("nodeType", nodeType, null), 
		};
		ulong num2 = nodeType switch
		{
			NodeType.Static => 19uL, 
			NodeType.Generated => 39uL, 
			NodeType.LaunchPad => 99uL, 
			NodeType.LowOrbitLaunchPad => 99uL, 
			_ => throw new ArgumentOutOfRangeException("nodeType", nodeType, null), 
		};
		ulong value = parentNode.Code.Value;
		ulong num3;
		if (value < 100000000)
		{
			num3 = (ulong)((value < 10000) ? ((value >= 100) ? 10000 : 100) : ((value >= 1000000) ? 100000000 : 1000000));
		}
		else if (value < 1000000000000L)
		{
			num3 = ((value >= 10000000000L) ? 1000000000000uL : 10000000000uL);
		}
		else
		{
			if (value >= 100000000000000L)
			{
				throw new Exception("SpaceMap Generation Failed! " + parentNode.Id + " Cannot have child Nodes because the new node's Code would exceed the maximum address space size.");
			}
			num3 = 100000000000000uL;
		}
		ulong num4 = num3;
		for (ulong num5 = num; num5 < 100; num5++)
		{
			ulong num6 = num5 * num4 + parentNode.Code.Value;
			if (num6 > 9007199254740992L)
			{
				throw new Exception("SpaceMapNode Generation Failed! " + parentNode.Id + " Cannot have child Nodes because the new node's Code would exceed the maximum address space size.");
			}
			if (num5 > num2)
			{
				throw new Exception("SpaceMapNode Generation Failed! " + parentNode.Id + " Cannot have any more child Nodes because the new node's code would exceed the address space for this node type.");
			}
			if (!LookUp.ContainsKey(num6))
			{
				Value = num6;
				String = string.Empty;
				String = GenerateDisplayString();
				return;
			}
		}
		throw new IndexOutOfRangeException();
	}

	private SpaceMapCode(ulong value)
	{
		Value = value;
		String = string.Empty;
		String = GenerateDisplayString();
	}

	private string GenerateDisplayString()
	{
		return $"{(ulong)((double)Value / 100000000000000.0):D2}-{(ulong)((double)Value % 100000000000000.0 / 1000000000000.0):D2}-{(ulong)((double)Value % 1000000000000.0 / 10000000000.0):D2}-{(ulong)((double)Value % 10000000000.0 / 100000000.0):D2}-{(ulong)((double)Value % 100000000.0 / 1000000.0):D2}-{(ulong)((double)Value % 1000000.0 / 10000.0):D2}-{(ulong)((double)Value % 10000.0 / 100.0):D2}-{(ulong)((double)Value % 100.0):D2}";
	}

	public static SpaceMapNode Get(ulong code)
	{
		if (!LookUp.TryGetValue(code, out var value))
		{
			return null;
		}
		return value;
	}

	public static void Register(SpaceMapNode node, SpaceMapCode code)
	{
		if (!code.IsValid)
		{
			ConsoleWindow.PrintError("node " + node.DisplayName + " has invalid code " + StringManager.Get(code.Value));
		}
		if (!LookUp.TryAdd(code.Value, node))
		{
			ConsoleWindow.PrintError("node " + node.DisplayName + " has duplicate code " + StringManager.Get(code.Value));
		}
	}

	public static void Deregister(SpaceMapNode toDeregister)
	{
		if (toDeregister != null)
		{
			LookUp.Remove(toDeregister.Code.Value);
		}
	}

	public static void Clear()
	{
		LookUp.Clear();
	}
}
