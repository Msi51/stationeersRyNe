using System;
using System.IO;
using ThingImport;
using UnityEngine;
using UnityEngine.Rendering;

namespace UI.ImGuiUi;

public class MeshBrowser : ImGuiFileBrowser
{
	public const float DEFAULT_SCALE = 1f;

	public float Scale = 1f;

	public Mesh LoadedMesh;

	protected override string FileSearchPattern => "*.obj";

	public MeshBrowser(string defaultPath)
		: base(defaultPath)
	{
	}

	public bool Reload()
	{
		return LoadMesh();
	}

	protected override bool FileSelected()
	{
		return LoadMesh();
	}

	private bool LoadMesh()
	{
		if (string.IsNullOrEmpty(base.SelectedPath))
		{
			LoadedMesh = null;
			return false;
		}
		Mesh mesh;
		try
		{
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(base.SelectedPath);
			OBJImporter.MeshData meshData = OBJImporter.ImportOBJ(File.ReadAllText(base.SelectedPath), Scale);
			mesh = new Mesh
			{
				name = fileNameWithoutExtension,
				indexFormat = ((meshData.vertices.Length > 65535) ? IndexFormat.UInt32 : IndexFormat.UInt16)
			};
			mesh.SetVertices(meshData.vertices);
			mesh.SetNormals(meshData.normals);
			mesh.SetUVs(0, meshData.uvs);
			mesh.SetTriangles(meshData.triangles, 0);
			mesh.RecalculateTangents();
			mesh.RecalculateBounds();
			mesh.Optimize();
		}
		catch (Exception ex)
		{
			ErrorMessage = ex.Message;
			LoadedMesh = null;
			return false;
		}
		LoadedMesh = mesh;
		ErrorMessage = string.Empty;
		return true;
	}
}
