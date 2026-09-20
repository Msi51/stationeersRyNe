using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Networks;
using UnityEngine;

namespace Objects.Rockets.UI;

public sealed class RocketMapIconRenderer
{
	private sealed class DrawEntry
	{
		public Mesh Mesh;

		public bool OwnsMesh;

		public Matrix4x4 LocalMatrix;

		public Material[] Materials;

		public MaterialPropertyBlock[] Properties;
	}

	private struct Rendered
	{
		public Sprite Icon;

		public Sprite Highlight;
	}

	private const int Size = 256;

	private const float LeanDegrees = -45f;

	private const int HighlightDilation = 5;

	private static readonly Vector3 KeyLightEuler = new Vector3(0.462f, -37.805f, 8.079f);

	private static readonly Vector3[] CornerSigns = new Vector3[8]
	{
		new Vector3(1f, 1f, 1f),
		new Vector3(1f, 1f, -1f),
		new Vector3(1f, -1f, 1f),
		new Vector3(1f, -1f, -1f),
		new Vector3(-1f, 1f, 1f),
		new Vector3(-1f, 1f, -1f),
		new Vector3(-1f, -1f, 1f),
		new Vector3(-1f, -1f, -1f)
	};

	private Camera _camera;

	private RenderTexture _renderTexture;

	private readonly List<DrawEntry> _entries = new List<DrawEntry>();

	public static RocketMapIconRenderer Instance { get; } = new RocketMapIconRenderer();

	public Sprite GetIcon(LaunchMount launchMount)
	{
		Sprite sprite;
		try
		{
			sprite = Render(launchMount);
		}
		catch (Exception ex)
		{
			ConsoleWindow.PrintError("RocketMapIconRenderer: failed to render icon for " + launchMount.DisplayName + ": " + ex.Message);
			ClearEntries();
			return null;
		}
		if (sprite == null)
		{
			return null;
		}
		return sprite;
	}

	public Sprite GetIcon(Rocket rocket, int fallbackColorIndex)
	{
		if (rocket == null)
		{
			return null;
		}
		int num = ComputeColorKey(rocket, fallbackColorIndex);
		if (!rocket.MapIconDirty && rocket.MapIcon != null && rocket.MapIconColorIndex == num)
		{
			return rocket.MapIcon;
		}
		Rendered rendered;
		try
		{
			rendered = Render(rocket);
		}
		catch (Exception ex)
		{
			ConsoleWindow.PrintError("RocketMapIconRenderer: failed to render icon for " + rocket.DisplayName + ": " + ex.Message);
			ClearEntries();
			return null;
		}
		if (rendered.Icon == null)
		{
			return null;
		}
		DestroySprite(rocket.MapIcon);
		DestroySprite(rocket.MapHighlight);
		rocket.MapIcon = rendered.Icon;
		rocket.MapHighlight = rendered.Highlight;
		rocket.MapIconColorIndex = num;
		rocket.MapIconDirty = false;
		return rendered.Icon;
	}

	private static int ComputeColorKey(Rocket rocket, int fallbackColorIndex)
	{
		RocketNetwork rocketNetwork = rocket.RocketNetwork;
		int num = fallbackColorIndex;
		if (rocketNetwork == null)
		{
			return num;
		}
		foreach (INetworkedStructure structure in rocketNetwork.StructureList)
		{
			num = (num * 397) ^ ColorIndexOf(structure?.GetAsThing);
		}
		foreach (IRocketEngine engine in rocketNetwork.Engines)
		{
			num = (num * 397) ^ ColorIndexOf(engine as Thing);
		}
		return num;
	}

	private static int ColorIndexOf(Thing thing)
	{
		if (!(thing != null) || thing.CustomColor == null || !thing.CustomColor.IsSet)
		{
			return -1;
		}
		return thing.CustomColor.Index;
	}

	public static void DestroySprite(Sprite sprite)
	{
		if (!(sprite == null))
		{
			if (sprite.texture != null)
			{
				UnityEngine.Object.Destroy(sprite.texture);
			}
			UnityEngine.Object.Destroy(sprite);
		}
	}

	private void EnsureRig()
	{
		if (!(_camera != null))
		{
			GameObject gameObject = new GameObject("~RocketMapIconCamera")
			{
				hideFlags = HideFlags.HideAndDontSave
			};
			UnityEngine.Object.DontDestroyOnLoad(gameObject);
			_camera = gameObject.AddComponent<Camera>();
			_camera.enabled = false;
			_camera.orthographic = true;
			_camera.clearFlags = CameraClearFlags.Color;
			_camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
			_camera.cullingMask = 1 << (int)Layers.ThumbnailCreation;
			_camera.allowHDR = false;
			_camera.allowMSAA = true;
			GameObject gameObject2 = new GameObject("Key");
			gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
			Light light = gameObject2.AddComponent<Light>();
			light.type = LightType.Directional;
			light.color = Color.white;
			light.intensity = 1f;
			light.cullingMask = 1 << (int)Layers.ThumbnailCreation;
			gameObject2.transform.localRotation = Quaternion.Euler(KeyLightEuler);
			_renderTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32)
			{
				antiAliasing = 8,
				filterMode = FilterMode.Trilinear
			};
		}
	}

	private Sprite Render(LaunchMount launchMount)
	{
		ClearEntries();
		AddThingRenderers(launchMount, launchMount.Transform.worldToLocalMatrix);
		EnsureRig();
		Quaternion rotation = Quaternion.AngleAxis(20f, Vector3.up) * Quaternion.AngleAxis(50f, Vector3.right);
		CreateSprite($"RocketMapIcon_{launchMount.ReferenceId}", rotation, 1f, out var sprite, out var _);
		return sprite;
	}

	private Rendered Render(Rocket rocket)
	{
		RocketNetwork rocketNetwork = rocket.RocketNetwork;
		if (rocketNetwork == null || rocketNetwork.StructureList.Count == 0)
		{
			return default(Rendered);
		}
		Thing getAsThing = rocketNetwork.StructureList[0].GetAsThing;
		if (getAsThing == null)
		{
			return default(Rendered);
		}
		Matrix4x4 worldToLocalMatrix = getAsThing.Transform.worldToLocalMatrix;
		BuildEntries(rocketNetwork, worldToLocalMatrix);
		if (_entries.Count == 0)
		{
			return default(Rendered);
		}
		EnsureRig();
		CreateSprite($"RocketMapIcon_{rocketNetwork.ReferenceId}", ComputeViewRotation(), 1.25f, out var sprite, out var texture);
		Sprite sprite2 = null;
		Texture2D texture2D = BuildHighlight(texture.GetPixels32(), 256, 256, 5);
		if (texture2D != null)
		{
			sprite2 = Sprite.Create(texture2D, new Rect(0f, 0f, 256f, 256f), new Vector2(0.5f, 0.5f), 100f);
			sprite2.name = $"RocketMapHighlight_{rocketNetwork.ReferenceId}";
			texture2D.name = sprite2.name;
		}
		return new Rendered
		{
			Icon = sprite,
			Highlight = sprite2
		};
	}

	private void CreateSprite(string name, Quaternion rotation, float zoom, out Sprite sprite, out Texture2D texture)
	{
		Bounds bounds = ComputeBounds();
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		Vector3[] cornerSigns = CornerSigns;
		foreach (Vector3 b in cornerSigns)
		{
			Vector3 vector = rotation * Vector3.Scale(bounds.extents, b);
			num = Mathf.Max(num, Mathf.Abs(vector.x));
			num2 = Mathf.Max(num2, Mathf.Abs(vector.y));
			num3 = Mathf.Max(num3, Mathf.Abs(vector.z));
		}
		_camera.orthographicSize = Mathf.Max(num, num2, 0.01f) * zoom;
		float num4 = num3 + 1f;
		_camera.nearClipPlane = 0.05f;
		_camera.farClipPlane = num4 + num3 + 2f;
		_camera.transform.SetPositionAndRotation(new Vector3(0f, 0f, num4), Quaternion.LookRotation(Vector3.back, Vector3.up));
		Matrix4x4 root = Matrix4x4.Rotate(rotation) * Matrix4x4.Translate(-bounds.center);
		SubmitDrawMeshes(root);
		_camera.transparencySortMode = TransparencySortMode.Perspective;
		_camera.targetTexture = _renderTexture;
		_camera.Render();
		_camera.targetTexture = null;
		RenderTexture active = RenderTexture.active;
		RenderTexture.active = _renderTexture;
		texture = new Texture2D(256, 256, TextureFormat.ARGB32, mipChain: false);
		texture.ReadPixels(new Rect(0f, 0f, 256f, 256f), 0, 0);
		texture.Apply(updateMipmaps: false);
		RenderTexture.active = active;
		ClearEntries();
		sprite = Sprite.Create(texture, new Rect(0f, 0f, 256f, 256f), new Vector2(0.5f, 0.5f), 100f);
		sprite.name = name;
		texture.name = sprite.name;
	}

	private static Texture2D BuildHighlight(Color32[] icon, int width, int height, int radius)
	{
		byte[] array = new byte[width * height];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = icon[i].a;
		}
		byte[] array2 = new byte[width * height];
		for (int j = 0; j < height; j++)
		{
			int num = j * width;
			for (int k = 0; k < width; k++)
			{
				byte b = 0;
				int num2 = Mathf.Max(0, k - radius);
				int num3 = Mathf.Min(width - 1, k + radius);
				for (int l = num2; l <= num3; l++)
				{
					byte b2 = array[num + l];
					if (b2 > b)
					{
						b = b2;
					}
				}
				array2[num + k] = b;
			}
		}
		Color32[] array3 = new Color32[width * height];
		for (int m = 0; m < height; m++)
		{
			for (int n = 0; n < width; n++)
			{
				byte b3 = 0;
				int num4 = Mathf.Max(0, m - radius);
				int num5 = Mathf.Min(height - 1, m + radius);
				for (int num6 = num4; num6 <= num5; num6++)
				{
					byte b4 = array2[num6 * width + n];
					if (b4 > b3)
					{
						b3 = b4;
					}
				}
				int num7 = Mathf.Max(0, b3 - array[m * width + n]);
				array3[m * width + n] = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)num7);
			}
		}
		Texture2D texture2D = new Texture2D(width, height, TextureFormat.ARGB32, mipChain: false);
		texture2D.SetPixels32(array3);
		texture2D.Apply(updateMipmaps: false);
		return texture2D;
	}

	private void BuildEntries(RocketNetwork network, Matrix4x4 worldToReference)
	{
		ClearEntries();
		foreach (INetworkedStructure structure in network.StructureList)
		{
			AddThingRenderers(structure?.GetAsThing, worldToReference);
		}
		foreach (IRocketEngine engine in network.Engines)
		{
			AddThingRenderers(engine as Thing, worldToReference);
		}
	}

	private void AddThingRenderers(Thing thing, Matrix4x4 worldToReference)
	{
		if (thing == null)
		{
			return;
		}
		Transform transform = thing.Transform;
		Dictionary<Renderer, bool> rendererShow = null;
		Dictionary<GameObject, bool> goActive = null;
		if (thing is Structure { BuildStates: not null } structure && structure.BuildStates.Count > 0)
		{
			int num = structure.CurrentBuildStateIndex;
			if (num < 0)
			{
				num = structure.BuildStates.Count - 1;
			}
			rendererShow = new Dictionary<Renderer, bool>();
			goActive = new Dictionary<GameObject, bool>();
			ComputeBuildStateVisibility(structure, num, rendererShow, goActive);
		}
		foreach (ThingRenderer renderer2 in thing.Renderers)
		{
			Renderer renderer = renderer2.GetRenderer();
			if (renderer == null || !ShouldDraw(renderer, transform, rendererShow, goActive))
			{
				continue;
			}
			Mesh mesh = null;
			bool ownsMesh = false;
			if (renderer.TryGetComponent<MeshFilter>(out var component) && component.sharedMesh != null)
			{
				mesh = component.sharedMesh;
			}
			else if (renderer is SkinnedMeshRenderer skinnedMeshRenderer && skinnedMeshRenderer.sharedMesh != null)
			{
				mesh = new Mesh();
				skinnedMeshRenderer.BakeMesh(mesh);
				ownsMesh = true;
			}
			if (mesh == null)
			{
				continue;
			}
			Material[] sharedMaterials = renderer.sharedMaterials;
			if (sharedMaterials != null && sharedMaterials.Length != 0)
			{
				MaterialPropertyBlock[] array = new MaterialPropertyBlock[sharedMaterials.Length];
				for (int i = 0; i < sharedMaterials.Length; i++)
				{
					MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
					renderer.GetPropertyBlock(materialPropertyBlock, i);
					array[i] = materialPropertyBlock;
				}
				_entries.Add(new DrawEntry
				{
					Mesh = mesh,
					OwnsMesh = ownsMesh,
					LocalMatrix = worldToReference * renderer.transform.localToWorldMatrix,
					Materials = sharedMaterials,
					Properties = array
				});
			}
		}
	}

	private static bool ShouldDraw(Renderer renderer, Transform root, Dictionary<Renderer, bool> rendererShow, Dictionary<GameObject, bool> goActive)
	{
		if (rendererShow != null && rendererShow.TryGetValue(renderer, out var value) && !value)
		{
			return false;
		}
		Transform transform = renderer.transform;
		while (transform != null && transform != root)
		{
			GameObject gameObject = transform.gameObject;
			if (!((goActive != null && goActive.TryGetValue(gameObject, out var value2)) ? value2 : gameObject.activeSelf))
			{
				return false;
			}
			transform = transform.parent;
		}
		return true;
	}

	private static void ComputeBuildStateVisibility(Structure structure, int stateIndex, Dictionary<Renderer, bool> rendererShow, Dictionary<GameObject, bool> goActive)
	{
		List<BuildState> buildStates = structure.BuildStates;
		for (int i = 0; i < buildStates.Count; i++)
		{
			BuildState buildState = buildStates[i];
			if (buildState == null)
			{
				continue;
			}
			bool flag = i == stateIndex;
			if (buildState.LinkedGameObjects != null)
			{
				foreach (GameObject linkedGameObject in buildState.LinkedGameObjects)
				{
					if (linkedGameObject != null)
					{
						goActive[linkedGameObject] = flag;
					}
				}
			}
			bool value = buildState.RenderMode switch
			{
				BuildStateRenderMode.OnMineAndPreviousStates => i <= stateIndex, 
				BuildStateRenderMode.OnMyState => flag, 
				_ => false, 
			};
			if (buildState.Visualizer != null)
			{
				rendererShow[buildState.Visualizer] = value;
			}
			else if (buildState.RendererInstance != null && buildState.RendererInstance.HasRenderer())
			{
				GameObject rendererGameObject = buildState.RendererInstance.GetRendererGameObject();
				if (rendererGameObject != null && rendererGameObject.TryGetComponent<Renderer>(out var component))
				{
					rendererShow[component] = value;
				}
			}
		}
	}

	private Bounds ComputeBounds()
	{
		bool flag = false;
		Bounds result = default(Bounds);
		foreach (DrawEntry entry in _entries)
		{
			Bounds bounds = entry.Mesh.bounds;
			Vector3[] cornerSigns = CornerSigns;
			foreach (Vector3 b in cornerSigns)
			{
				Vector3 point = bounds.center + Vector3.Scale(bounds.extents, b);
				Vector3 vector = entry.LocalMatrix.MultiplyPoint3x4(point);
				if (!flag)
				{
					result = new Bounds(vector, Vector3.zero);
					flag = true;
				}
				else
				{
					result.Encapsulate(vector);
				}
			}
		}
		if (!flag)
		{
			return new Bounds(Vector3.zero, Vector3.one);
		}
		return result;
	}

	private static Quaternion ComputeViewRotation()
	{
		return Quaternion.AngleAxis(-45f, Vector3.forward);
	}

	private void SubmitDrawMeshes(Matrix4x4 root)
	{
		int layer = Layers.ThumbnailCreation;
		foreach (DrawEntry entry in _entries)
		{
			Matrix4x4 matrix = root * entry.LocalMatrix;
			int b = entry.Mesh.subMeshCount - 1;
			for (int i = 0; i < entry.Materials.Length; i++)
			{
				Material material = entry.Materials[i];
				if (!(material == null))
				{
					Graphics.DrawMesh(entry.Mesh, matrix, material, layer, _camera, Mathf.Min(i, b), entry.Properties[i]);
				}
			}
		}
	}

	private void ClearEntries()
	{
		foreach (DrawEntry entry in _entries)
		{
			if (entry.OwnsMesh && entry.Mesh != null)
			{
				UnityEngine.Object.Destroy(entry.Mesh);
			}
		}
		_entries.Clear();
	}
}
