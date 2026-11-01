using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ViolentTakeoff
{
	// Effects that live longer than one fleck and have to animate in real time while the game is paused:
	// explosions that expand from their center, and flickering flames left in the wave's wake.
	// Updated and drawn from MapComponentUpdate; keeps running after the wave itself has finished.
	public class EffectsRunner
	{
		private class Explosion
		{
			public IntVec3 center;
			public float radius;
			public float age;
			public int nextCell;
			public int cellCount;
		}

		private class Flame
		{
			public Vector3 pos;
			public float size;
			public float age;
			public float life;
			public float nextSmoke;
			public int seed;
		}

		// Vanilla Graphic_Flicker switches frames every 15 ticks.
		private const float FlameFrameSeconds = 0.25f;
		// Vanilla fires smoke every 130-200 ticks.
		private static readonly FloatRange SmokeInterval = new FloatRange(2.2f, 3.3f);
		private const float FlameFadeSeconds = 1f;

		private static Material[] flameMats;

		private readonly Map map;
		private readonly List<Explosion> explosions = new List<Explosion>();
		private readonly List<Flame> flames = new List<Flame>();
		private readonly float flameAltitude = AltitudeLayer.MoteOverheadLow.AltitudeFor() - 0.01f;

		public EffectsRunner(Map map)
		{
			this.map = map;
		}

		private static DebugTuning T => Config.Tuning;

		public void StartExplosion(IntVec3 center)
		{
			float spread = Mathf.Clamp01(T.ExplosionSizeSpread);
			float radius = Mathf.Min(T.ExplosionRadius * Rand.Range(1f - spread, 1f + spread), GenRadial.MaxRadialPatternRadius - 1f);
			ShockwaveEffects.ExplosionStart(map, center, radius);
			explosions.Add(new Explosion { center = center, radius = radius, cellCount = GenRadial.NumCellsInRadius(radius) });
		}

		// A small cluster of flames around the cell, like a fire that has caught on several tiles.
		public void StartFire(IntVec3 cell, float fireSize)
		{
			int count = Rand.RangeInclusive(1, Mathf.Max(1, Mathf.RoundToInt(T.FlamesPerFire)));
			for (int i = 0; i < count; i++)
			{
				IntVec3 c = i == 0 ? cell : cell + GenAdj.AdjacentCellsAndInside[Rand.Range(0, 9)];
				if (!c.InBounds(map) || !CanBurn(c)) continue;
				// Vanilla draw size: fireSize / 1.2, capped at 1.2 cells.
				float size = Mathf.Min(fireSize * Rand.Range(0.6f, 1f) / 1.2f, 1.2f) * T.FlameScale;
				flames.Add(new Flame
				{
					pos = c.ToVector3Shifted() + new Vector3(Rand.Range(-0.2f, 0.2f), 0f, Rand.Range(-0.2f, 0.2f)),
					size = size,
					life = Rand.Range(T.FlameLifeMin, Mathf.Max(T.FlameLifeMin, T.FlameLifeMax)),
					nextSmoke = Rand.Range(0f, SmokeInterval.max),
					seed = Rand.Int,
				});
			}
			if (CanBurn(cell)) ShockwaveEffects.FireGlow(map, cell, fireSize);
		}

		// No flames on water or on top of mountains.
		private bool CanBurn(IntVec3 c)
		{
			if (c.GetTerrain(map).IsWater) return false;
			Building edifice = c.GetEdifice(map);
			return edifice == null || !edifice.def.building.isNaturalRock;
		}

		public void Update(float deltaTime)
		{
			for (int i = explosions.Count - 1; i >= 0; i--)
			{
				Explosion e = explosions[i];
				e.age += deltaTime;
				// Vanilla: a cell is hit 1.5 ticks per cell of distance after the start (40 cells/s at normal speed).
				float reached = e.age * T.ExplosionSpeed;
				while (e.nextCell < e.cellCount)
				{
					IntVec3 offset = GenRadial.RadialPattern[e.nextCell];
					if (offset.LengthHorizontal > reached) break;
					IntVec3 cell = e.center + offset;
					if (cell.InBounds(map)) ShockwaveEffects.BlastCell(map, e.center, cell, e.radius);
					e.nextCell++;
				}
				if (e.nextCell >= e.cellCount) explosions.RemoveAt(i);
			}

			for (int i = flames.Count - 1; i >= 0; i--)
			{
				Flame f = flames[i];
				f.age += deltaTime;
				if (f.age >= f.life)
				{
					flames.RemoveAt(i);
					continue;
				}
				f.nextSmoke -= deltaTime;
				if (f.nextSmoke <= 0f)
				{
					f.nextSmoke = SmokeInterval.RandomInRange;
					ShockwaveEffects.Smoke(map, f.pos, f.size);
					if (f.size > 0.5f) ShockwaveEffects.FireGlow(map, f.pos.ToIntVec3(), f.size);
				}
			}
		}

		public void Draw()
		{
			if (flames.Count == 0 || Find.CurrentMap != map) return;
			if (flameMats == null)
			{
				flameMats = new[]
				{
					MaterialPool.MatFrom("Things/Special/Fire/FireA", ShaderDatabase.TransparentPostLight),
					MaterialPool.MatFrom("Things/Special/Fire/FireB", ShaderDatabase.TransparentPostLight),
					MaterialPool.MatFrom("Things/Special/Fire/FireC", ShaderDatabase.TransparentPostLight),
				};
			}
			float now = Time.realtimeSinceStartup;
			foreach (Flame f in flames)
			{
				// Like Graphic_Flicker: a pseudo-random frame per step, plus a tiny jitter of the position.
				int step = (int)(now / FlameFrameSeconds) + (f.seed & 0xFF);
				int frame = Mathf.Abs(step ^ (f.seed * 391)) % flameMats.Length;
				Vector3 jitter = GenRadial.RadialPattern[step % GenRadial.RadialPattern.Length].ToVector3() / GenRadial.MaxRadialPatternRadius * 0.05f * f.size;
				// Shrink away at the end instead of popping out.
				float scale = f.size * Mathf.Clamp01((f.life - f.age) / FlameFadeSeconds);
				Vector3 pos = f.pos + jitter;
				pos.y = flameAltitude;
				Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(scale, 1f, scale));
				Graphics.DrawMesh(MeshPool.plane10, matrix, flameMats[frame], 0);
			}
		}
	}
}
