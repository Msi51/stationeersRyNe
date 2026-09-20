using System;
using System.Globalization;
using Assets.Scripts;
using UnityEngine;

namespace Util.Commands;

public class SystemInfoCommand : CommandBase
{
	public override string HelpText => "Prints a tree of information about the application, hardware, OS, graphics, audio, memory, processor and locale.";

	public override string[] Arguments => Array.Empty<string>();

	public override bool IsLaunchCmd => false;

	public override string Execute(string[] args)
	{
		TreeString treeString = new TreeString("system info");
		TreeString myParent = TreeString.Node("application", treeString);
		TreeString.Node("name: " + Application.productName, myParent);
		TreeString.Node("version: " + GameManager.GetGameVersion(), myParent);
		TreeString.Node($"dedicated: {GameManager.IsBatchMode}", myParent);
		TreeString.Node("unity version: " + Application.unityVersion, myParent);
		TreeString.Node($"background: {Application.runInBackground}", myParent);
		TreeString myParent2 = TreeString.Node("device", treeString);
		TreeString.Node("name: " + SystemInfo.deviceName, myParent2);
		TreeString.Node("model: " + SystemInfo.deviceModel, myParent2);
		TreeString.Node($"type: {SystemInfo.deviceType}", myParent2);
		TreeString myParent3 = TreeString.Node("operating system", treeString);
		TreeString.Node("type: " + SystemInfo.operatingSystem, myParent3);
		TreeString.Node($"family: {SystemInfo.operatingSystemFamily}", myParent3);
		TreeString.Node($"internet: {Application.internetReachability}", myParent3);
		TreeString myParent4 = TreeString.Node("graphics", treeString);
		TreeString.Node("name: " + SystemInfo.graphicsDeviceName, myParent4);
		TreeString.Node($"type: {SystemInfo.graphicsDeviceType}", myParent4);
		TreeString.Node("version: " + SystemInfo.graphicsDeviceVersion, myParent4);
		TreeString.Node($"shader level: {SystemInfo.graphicsShaderLevel}", myParent4);
		TreeString.Node($"compute shaders: {SystemInfo.supportsComputeShaders}", myParent4);
		TreeString.Node($"target frame rate: {Application.targetFrameRate}", myParent4);
		TreeString myParent5 = TreeString.Node("screen", treeString);
		TreeString.Node($"resolution: {Screen.currentResolution.width}x{Screen.currentResolution.height}", myParent5);
		TreeString.Node($"refresh rate: {Screen.currentResolution.refreshRate}", myParent5);
		TreeString.Node($"dpi: {Screen.dpi}", myParent5);
		TreeString.Node($"full screen: {Screen.fullScreen}", myParent5);
		TreeString.Node($"orientation: {Screen.orientation}", myParent5);
		TreeString myParent6 = TreeString.Node("memory", treeString);
		TreeString.Node($"system: {SystemInfo.systemMemorySize} MB", myParent6);
		TreeString.Node($"graphics: {SystemInfo.graphicsMemorySize} MB", myParent6);
		TreeString.Node($"max texture: {SystemInfo.maxTextureSize}", myParent4);
		TreeString myParent7 = TreeString.Node("processor", treeString);
		TreeString.Node($"count: {Environment.ProcessorCount}", myParent7);
		TreeString.Node("type: " + SystemInfo.processorType, myParent7);
		TreeString.Node($"frequency: {SystemInfo.processorFrequency}", myParent7);
		TreeString myParent8 = TreeString.Node("audio", treeString);
		TreeString.Node($"dsp time: {AudioSettings.dspTime}", myParent8);
		TreeString.Node($"sample rate: {AudioSettings.outputSampleRate}", myParent8);
		TreeString.Node($"speaker mode: {AudioSettings.speakerMode}", myParent8);
		TreeString.Node($"volume: {AudioListener.volume}", myParent8);
		TreeString myParent9 = TreeString.Node("localization", treeString);
		TreeString.Node($"system language: {Application.systemLanguage}", myParent9);
		TreeString.Node("installed locale: " + CultureInfo.InstalledUICulture.Name, myParent9);
		TreeString.Node("culture locale: " + CultureInfo.CurrentCulture.Name, myParent9);
		TreeString.Node("ui culture: " + CultureInfo.CurrentUICulture.Name, myParent9);
		treeString.ToConsole();
		return null;
	}
}
