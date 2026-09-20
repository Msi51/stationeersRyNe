using System;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Inventory;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using ImGuiNET;
using ImGuiNET.Unity;
using Objects.Rockets;
using TerrainSystem;
using UI.ImGuiUi;
using UI.ImGuiUi.ImGuiWindows;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Weather;

namespace Assets.Scripts.UI;

public class ImGuiManager : MonoBehaviour
{
	private static ImGuiManager current;

	[SerializeField]
	private RawImage outputRawImage;

	[SerializeField]
	private Camera overlayCamera;

	[SerializeField]
	private Camera subscreenCamera;

	public Texture[] LoadingScreenTextures;

	private const string CommandBufferTag = "ImGuiManagerCommandBuffer";

	[SerializeField]
	private CursorShapesAsset _cursorShapes;

	[SerializeField]
	private ShaderResourcesAsset _shaders;

	[SerializeField]
	private StyleAsset _style;

	[SerializeField]
	private FontAtlasConfigAsset _fontAtlasConfiguration;

	[SerializeField]
	private IOConfig _initialConfiguration;

	private static IntPtr igContext = IntPtr.Zero;

	public static TextureManager igTextureManager;

	private static ImGuiRendererMesh igRenderer;

	private static ImGuiPlatformInputManager igInput;

	private RenderTexture renderTexture;

	private CommandBuffer commandBuffer;

	public static Texture RandomLoadingTexture()
	{
		return current.LoadingScreenTextures.Pick();
	}

	private void Awake()
	{
		current = this;
		CreateRenderTexture();
		commandBuffer = new CommandBuffer
		{
			name = "ImGuiManagerCommandBuffer"
		};
		igContext = ImGui.CreateContext();
		igTextureManager = new TextureManager();
		igInput = new ImGuiPlatformInputManager(_cursorShapes, null);
		igRenderer = new ImGuiRendererMesh(_shaders, igTextureManager);
	}

	private void OnDestroy()
	{
		if (current == this)
		{
			current = null;
		}
		commandBuffer?.Release();
		ImGui.DestroyContext(igContext);
	}

	private void OnEnable()
	{
		overlayCamera.AddCommandBuffer(CameraEvent.AfterEverything, commandBuffer);
		InitializeImGui();
	}

	private void OnDisable()
	{
		overlayCamera.RemoveAllCommandBuffers();
		ShutdownImGui();
	}

	private void CreateRenderTexture()
	{
		renderTexture = new RenderTexture(Screen.width, Screen.height, GraphicsFormat.R8G8B8A8_SRGB, GraphicsFormat.D24_UNorm_S8_UInt);
		overlayCamera.targetTexture = renderTexture;
		outputRawImage.texture = renderTexture;
	}

	public static void SetBlockUguiClicks(bool block)
	{
		if (current == null || current.outputRawImage == null)
		{
			return;
		}
		Canvas canvas = current.outputRawImage.canvas;
		if (!(canvas == null))
		{
			GraphicRaycaster graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
			if (graphicRaycaster == null)
			{
				graphicRaycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
			}
			graphicRaycaster.enabled = block;
			current.outputRawImage.raycastTarget = block;
			canvas.overrideSorting = block;
			if (block)
			{
				canvas.sortingOrder = 32767;
			}
		}
	}

	private void InitializeImGui()
	{
		ImGui.SetCurrentContext(igContext);
		ImGuiIOPtr iO = ImGui.GetIO();
		_initialConfiguration.ApplyTo(iO);
		ImGuiStylePtr style = ImGui.GetStyle();
		_style.ApplyTo(style);
		igTextureManager.BuildFontAtlas(iO, in _fontAtlasConfiguration);
		igTextureManager.Initialize(iO);
		igInput?.Initialize(iO);
		igRenderer?.Initialize(iO);
	}

	private void ShutdownImGui()
	{
		ImGui.SetCurrentContext(igContext);
		ImGuiIOPtr iO = ImGui.GetIO();
		igRenderer?.Shutdown(iO);
		igInput?.Shutdown(iO);
		igTextureManager.Shutdown();
		igTextureManager.DestroyFontAtlas(iO);
		ImGui.SetCurrentContext(IntPtr.Zero);
	}

	public void LateUpdate()
	{
		if (renderTexture.width != Screen.width || renderTexture.height != Screen.height)
		{
			CreateRenderTexture();
		}
		RenderOverlay();
	}

	private void RenderOverlay()
	{
		PrepareImGuiFrame();
		ConsoleWindow.Draw();
		if (SplashBehaviour.IsActive)
		{
			SplashBehaviour.Draw();
		}
		else if (ImGuiLoadingScreen.IsShowing)
		{
			ImGuiLoadingScreen.DrawStandardLoading();
		}
		else
		{
			OrbitalSimulation.Draw();
			CinematicCamera.DrawOverlay();
			RegionManager.DrawDebug();
			ThreadProfiler.DrawBatchInfo();
			if (NetworkDebugWindow.Show)
			{
				NetworkDebugWindow.Draw();
			}
			RocketDebugWindow.DrawDebugOverlays();
			if (RocketDebugWindow.Show)
			{
				RocketDebugWindow.Draw();
			}
			if (WorldSettingToolsImguiWindow.Show)
			{
				WorldSettingToolsImguiWindow.Draw();
			}
			ImGuiWindowManager.Draw();
			if (ImGuiTerrainEditorTool.IsShowing)
			{
				ImGuiTerrainEditorTool.Draw();
			}
			ImguiCreativeSpawnMenu.Draw();
			ImGuiInWorldManager.DrawDebugActions();
			LavaData.DebugLavaHeight(InventoryManager.ParentPosition);
			Rocket.DrawDebug();
			Vein.DrawDebug();
			PlanetaryAtmosphereSimulation.DrawOnScreenDebug();
			TerraForming.ImguiDebug();
			WeatherManager.DrawDebug();
		}
		commandBuffer.Clear();
		RenderImGuiTo(commandBuffer);
	}

	private void PrepareImGuiFrame()
	{
		ImGui.SetCurrentContext(igContext);
		ImGuiIOPtr iO = ImGui.GetIO();
		igTextureManager.PrepareFrame(iO);
		igInput.PrepareFrame(iO, overlayCamera.pixelRect);
		ImGui.NewFrame();
	}

	private void PrepareCommandBuffer()
	{
		commandBuffer.Clear();
		RenderImGuiTo(commandBuffer);
	}

	private void RenderComputerScreens()
	{
		PrepareImGuiFrame();
	}

	private void RenderImGuiTo(CommandBuffer cb)
	{
		ImGui.Render();
		ImDrawDataPtr drawData = ImGui.GetDrawData();
		igRenderer.RenderDrawLists(cb, drawData);
	}

	private static void RenderCommandBufferToCamera(CommandBuffer cb, Camera cam, CameraEvent camEvent = CameraEvent.AfterEverything)
	{
		cam.AddCommandBuffer(camEvent, cb);
		cam.Render();
	}

	public static IntPtr ImGuiPointerFor(Texture texture)
	{
		return (IntPtr)igTextureManager.GetTextureId(texture);
	}
}
