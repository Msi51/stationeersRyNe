using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rendering;

public static class LogicDisplayDigitRenderer
{
	private class GlyphBatch
	{
		public Mesh Mesh;

		public Material Material;

		public int Layer;

		public readonly Matrix4x4[] Matrices = new Matrix4x4[1023];

		public int Count;
	}

	private const int MAX_DISPLAYS = 1024;

	public static readonly DensePool<LogicDisplay> ActiveDisplays = new DensePool<LogicDisplay>("ActiveDisplays", 1024);

	private const int BATCH_CAP = 1023;

	private static readonly List<GlyphBatch> _batches = new List<GlyphBatch>();

	private static int _batchCount;

	public static void Render()
	{
		if (GameManager.GameState != GameState.Running)
		{
			return;
		}
		_batchCount = 0;
		DensePool<LogicDisplay>.ActiveEnumerable.Enumerator enumerator = ActiveDisplays.Active().GetEnumerator();
		while (enumerator.MoveNext())
		{
			Accumulate(enumerator.Current);
		}
		for (int i = 0; i < _batchCount; i++)
		{
			GlyphBatch glyphBatch = _batches[i];
			if (glyphBatch.Count != 0)
			{
				Graphics.DrawMeshInstanced(glyphBatch.Mesh, 0, glyphBatch.Material, glyphBatch.Matrices, glyphBatch.Count, null, ShadowCastingMode.Off, receiveShadows: true, glyphBatch.Layer, null, LightProbeUsage.Off, null);
			}
		}
	}

	private static void Accumulate(LogicDisplay display)
	{
		if (display == null || display.IsCursor || !display.OnOff || !display.Powered)
		{
			return;
		}
		IReadOnlyList<LogicDisplay.DigitGlyph> digitGlyphs = display.DigitGlyphs;
		if (digitGlyphs.Count == 0)
		{
			return;
		}
		Material digitOn = display.DigitOn;
		Transform digitTransform = display.DigitTransform;
		if (digitOn == null || digitTransform == null)
		{
			return;
		}
		Matrix4x4 localToWorldMatrix = digitTransform.localToWorldMatrix;
		int layer = digitTransform.gameObject.layer;
		for (int i = 0; i < digitGlyphs.Count; i++)
		{
			LogicDisplay.DigitGlyph digitGlyph = digitGlyphs[i];
			if (!(digitGlyph.Mesh == null))
			{
				GlyphBatch batch = GetBatch(digitGlyph.Mesh, digitOn, layer);
				batch.Matrices[batch.Count++] = localToWorldMatrix * Matrix4x4.Translate(digitGlyph.LocalOffset);
			}
		}
	}

	private static GlyphBatch GetBatch(Mesh mesh, Material material, int layer)
	{
		for (int i = 0; i < _batchCount; i++)
		{
			GlyphBatch glyphBatch = _batches[i];
			if (glyphBatch.Count < 1023 && glyphBatch.Mesh == mesh && glyphBatch.Material == material && glyphBatch.Layer == layer)
			{
				return glyphBatch;
			}
		}
		GlyphBatch glyphBatch2;
		if (_batchCount < _batches.Count)
		{
			glyphBatch2 = _batches[_batchCount];
		}
		else
		{
			glyphBatch2 = new GlyphBatch();
			_batches.Add(glyphBatch2);
		}
		glyphBatch2.Mesh = mesh;
		glyphBatch2.Material = material;
		glyphBatch2.Layer = layer;
		glyphBatch2.Count = 0;
		_batchCount++;
		return glyphBatch2;
	}

	public static void ClearAll()
	{
		ActiveDisplays.Clear();
		_batchCount = 0;
	}
}
