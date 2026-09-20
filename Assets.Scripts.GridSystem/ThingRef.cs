using System.Runtime.CompilerServices;
using Assets.Scripts.Objects;

namespace Assets.Scripts.GridSystem;

public readonly struct ThingRef<T> where T : IReferencable
{
	public readonly long ReferenceId;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ThingRef(long referenceId)
	{
		ReferenceId = referenceId;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ThingRef(IReferencable instance)
	{
		ReferenceId = instance.ReferenceId;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T Get()
	{
		return Thing.Find<T>(ReferenceId);
	}

	public T2 Get<T2>() where T2 : IReferencable
	{
		return Thing.Find<T2>(ReferenceId);
	}

	public bool TryGet<T2>(out T2 found) where T2 : IReferencable
	{
		found = Get<T2>();
		return found != null;
	}
}
