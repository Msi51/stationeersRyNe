using System;
using System.Buffers;

namespace Assets.Scripts.Util;

public readonly struct PooledSpan<T> : IDisposable where T : class, IDensePoolable
{
	private readonly T[] _buffer;

	public ReadOnlySpan<T> Collection => _buffer;

	internal PooledSpan(T[] buffer)
	{
		_buffer = buffer;
	}

	public void Dispose()
	{
		ArrayPool<T>.Shared.Return(_buffer, clearArray: true);
	}
}
