using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sound;
using UnityEngine;

namespace Assets.Scripts.Objects.Entities;

[Serializable]
public class LeakReference
{
	public GameObject Visualizer;

	public Transform Transform;

	public UniTask LeakTask;

	public CancellationTokenSource TaskCancel;

	public PooledAudioSource LeakAudio;

	public void Cancel()
	{
		TaskCancel?.Cancel();
	}
}
