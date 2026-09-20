using UnityEngine;

public class DynamicDecalSettings : ScriptableObject
{
	public SystemRenderingPath systemRenderingPath;

	public string[] layerNames;

	public int poolLimit;

	public bool lockForwardDepth;

	public bool lockDeferredDepth;

	public DynamicDecalSettings()
	{
		systemRenderingPath = SystemRenderingPath.Auto;
		layerNames = new string[4] { "Layer 1", "Layer 2", "Layer 3", "Layer 4" };
		poolLimit = 500;
		lockForwardDepth = true;
		lockDeferredDepth = false;
	}
}
