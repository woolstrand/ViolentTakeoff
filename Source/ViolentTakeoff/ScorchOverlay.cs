using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ViolentTakeoff
{
	// "Scorch" mode: instead of destroying anything, the swept area is painted over with charred ground and ash,
	// drawn above buildings, items and pawns. Costs a few meshes no matter how much stuff is on the map.
	// Meshes are split into small chunks so adding cells only rebuilds the chunks the wave front touched.
	public class ScorchOverlay
	{
		private const int ChunkSize = 8;
		// Soil texture spans this many cells before repeating.
		private const float GroundTiling = 10f;
		private const float AshSize = 3f;

		private const int AshVariants = 3;
		// Charred soil hue; brightness comes from tuning.
		private static readonly Color GroundTint = new Color(1f, 0.86f, 0.74f);

		// Per overlay, so tuned colors apply to the next takeoff.
		private readonly Material groundMat;
		private readonly Material[] ashMats;
		private readonly float ashChance;

		private class Chunk
		{
			public readonly List<IntVec3> ground = new List<IntVec3>();
			public readonly List<(Vector3 pos, float angle, int variant)> ash = new List<(Vector3, float, int)>();
			public Mesh groundMesh;
			public readonly Mesh[] ashMeshes = new Mesh[AshVariants];
			public bool dirty;
		}

		private readonly Map map;
		private readonly float altitude = AltitudeLayer.Blueprint.AltitudeFor();
		private readonly Dictionary<int, Chunk> chunks = new Dictionary<int, Chunk>();
		private readonly List<Chunk> dirtyChunks = new List<Chunk>();

		private static readonly List<Vector3> verts = new List<Vector3>();
		private static readonly List<Vector2> uvs = new List<Vector2>();
		private static readonly List<int> tris = new List<int>();

		public ScorchOverlay(Map map)
		{
			this.map = map;
			DebugTuning tuning = Config.Tuning;
			Color ground = GroundTint * tuning.ScorchBrightness;
			ground.a = 1f;
			groundMat = MaterialPool.MatFrom("Terrain/Surfaces/Soil", ShaderDatabase.Transparent, ground);
			Color ash = new Color(1f, 1f, 1f, tuning.AshAlpha);
			ashMats = new[]
			{
				MaterialPool.MatFrom("Things/Filth/Ash/AshA", ShaderDatabase.Transparent, ash),
				MaterialPool.MatFrom("Things/Filth/Ash/AshB", ShaderDatabase.Transparent, ash),
				MaterialPool.MatFrom("Things/Filth/Ash/AshC", ShaderDatabase.Transparent, ash),
			};
			ashChance = tuning.AshChance;
		}

		public void Add(IntVec3 cell)
		{
			// Water and mountains would look wrong charred.
			if (cell.GetTerrain(map).IsWater) return;
			Building edifice = cell.GetEdifice(map);
			if (edifice != null && edifice.def.building.isNaturalRock) return;

			int key = (cell.x / ChunkSize) * 10000 + cell.z / ChunkSize;
			if (!chunks.TryGetValue(key, out Chunk chunk))
			{
				chunk = new Chunk();
				chunks.Add(key, chunk);
			}
			chunk.ground.Add(cell);
			if (Rand.Chance(ashChance))
			{
				chunk.ash.Add((cell.ToVector3Shifted() + new Vector3(Rand.Range(-0.5f, 0.5f), 0f, Rand.Range(-0.5f, 0.5f)), Rand.Range(0f, 360f), Rand.Range(0, AshVariants)));
			}
			if (!chunk.dirty)
			{
				chunk.dirty = true;
				dirtyChunks.Add(chunk);
			}
		}

		// Called every frame (MapComponentUpdate): Graphics.DrawMesh only draws for the current frame.
		public void Draw()
		{
			foreach (Chunk chunk in dirtyChunks)
			{
				Rebuild(chunk);
				chunk.dirty = false;
			}
			dirtyChunks.Clear();

			if (Find.CurrentMap != map) return;
			foreach (Chunk chunk in chunks.Values)
			{
				Graphics.DrawMesh(chunk.groundMesh, Vector3.zero, Quaternion.identity, groundMat, 0);
				for (int i = 0; i < AshVariants; i++)
				{
					if (chunk.ashMeshes[i] != null) Graphics.DrawMesh(chunk.ashMeshes[i], Vector3.zero, Quaternion.identity, ashMats[i], 0);
				}
			}
		}

		private void Rebuild(Chunk chunk)
		{
			verts.Clear(); uvs.Clear(); tris.Clear();
			foreach (IntVec3 c in chunk.ground)
			{
				float u = (c.x % GroundTiling) / GroundTiling, v = (c.z % GroundTiling) / GroundTiling, d = 1f / GroundTiling;
				AddQuad(new Vector3(c.x, altitude, c.z), new Vector3(c.x, altitude, c.z + 1), new Vector3(c.x + 1, altitude, c.z + 1), new Vector3(c.x + 1, altitude, c.z),
					new Vector2(u, v), new Vector2(u, v + d), new Vector2(u + d, v + d), new Vector2(u + d, v));
			}
			chunk.groundMesh = Fill(chunk.groundMesh);

			for (int variant = 0; variant < AshVariants; variant++)
			{
				verts.Clear(); uvs.Clear(); tris.Clear();
				foreach (var ash in chunk.ash)
				{
					if (ash.variant != variant) continue;
					// Slightly above the ground layer so ash is never hidden by it.
					Vector3 center = new Vector3(ash.pos.x, altitude + 0.001f, ash.pos.z);
					Quaternion rot = Quaternion.AngleAxis(ash.angle, Vector3.up);
					float h = AshSize / 2f;
					AddQuad(center + rot * new Vector3(-h, 0f, -h), center + rot * new Vector3(-h, 0f, h), center + rot * new Vector3(h, 0f, h), center + rot * new Vector3(h, 0f, -h),
						new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f));
				}
				chunk.ashMeshes[variant] = verts.Count > 0 ? Fill(chunk.ashMeshes[variant]) : null;
			}
		}

		private static void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
		{
			int i = verts.Count;
			verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
			uvs.Add(ua); uvs.Add(ub); uvs.Add(uc); uvs.Add(ud);
			tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
			tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
		}

		private static Mesh Fill(Mesh mesh)
		{
			if (mesh == null) mesh = new Mesh();
			mesh.Clear();
			mesh.SetVertices(verts);
			mesh.SetUVs(0, uvs);
			mesh.SetTriangles(tris, 0);
			mesh.RecalculateBounds();
			return mesh;
		}
	}
}
