using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ViolentTakeoff
{
	// Driven by real time (frame deltaTime), not ticks: the takeoff cutscene keeps the game paused.
	// All cells are sorted by distance from the center once; the wave walks that list as the radius grows,
	// so every cell is reached exactly once and no cell is skipped when the radius jumps.
	// Two cursors walk the list: effects (cheap) and destruction (expensive, limited to a time budget per step).
	// Timing beats speed: in Destroy mode the ring never runs ahead of destruction. When destruction can not keep
	// up, the ring slows down to its pace (smoothed, so it does not stop and go between steps).
	public class ShockwaveInstance
	{
		private const float StartRadius = 10f;
		private const int MaxThingsPerCell = 10;
		// How far the ring may be ahead of the destroyed area, and how far destruction may run ahead of the ring.
		private const float MaxLead = 0.5f;
		private const float MaxAhead = 1f;
		// Per destruction step, how quickly the ring speed follows the measured destruction pace.
		private const float PaceSmoothing = 0.3f;
		// Every destruction step dirties the map sections along the ring, and the engine rebuilds them on the next
		// frame. 15 steps a second keeps each step under a cell at normal speeds (no visible chunks) while cutting
		// those rebuilds to a quarter of per-frame destruction.
		private const float DestructionInterval = 1f / 15f;

		public readonly Map Map;
		public readonly IntVec3 Center;

		private readonly int[] cellIndices;
		private readonly float[] cellDistances;
		private readonly DestructionMode mode;
		private readonly ScorchOverlay scorch;
		private readonly EffectsRunner effects;
		private readonly ListerStash listerStash;
		private readonly bool holdStash;
		private int nextVisualCell;
		private int nextDestroyCell;
		private float radius = StartRadius;
		private float sinceDestruction = DestructionInterval;
		private float ringSpeed = -1f;
		// Measured ring speed (smoothed): flashes live longer when it falls below nominal, so the band keeps its width.
		private float measuredSpeed = -1f;
		private const float MeasuredSpeedSmoothing = 0.1f;

		private readonly System.Diagnostics.Stopwatch frameWatch = new System.Diagnostics.Stopwatch();
		private readonly List<Thing> tmpThings = new List<Thing>();

		public float Radius => radius;
		public int CellsDone => mode == DestructionMode.Destroy ? nextDestroyCell : nextVisualCell;
		public int CellsTotal => cellIndices.Length;

		// area: cells the wave touches at all; null = whole map. The takeoff passes the part of the map its camera
		// can show: anything else is never seen and is discarded with the map anyway.
		// duringTakeoff: the game stays paused and the map is discarded right after, so the lister lists can stay set
		// aside for the whole wave (restored by Finish). Otherwise (debug tool, time may run) only per step.
		public ShockwaveInstance(IntVec3 center, Map map, CellRect? area, ScorchOverlay scorch, EffectsRunner effects, bool duringTakeoff)
		{
			this.effects = effects;
			holdStash = duringTakeoff;
			Center = center;
			Map = map;
			mode = Config.Mode;
			this.scorch = scorch;
			if (mode == DestructionMode.Destroy && Config.FastBookkeeping) listerStash = new ListerStash(map);

			CellRect rect = CellRect.WholeMap(map).ContractedBy(1);
			if (area.HasValue) rect = area.Value.ClipInsideRect(rect);
			var indices = new List<int>(rect.Area);
			var distances = new List<float>(rect.Area);
			foreach (IntVec3 cell in rect)
			{
				float dist = (cell - center).LengthHorizontal;
				if (dist < StartRadius)
				{
					// The wave starts outside the ship's footprint; the ground it took off from is scorched at once.
					scorch?.Add(cell);
					continue;
				}
				indices.Add(map.cellIndices.CellToIndex(cell));
				distances.Add(dist);
			}
			cellIndices = indices.ToArray();
			cellDistances = distances.ToArray();
			Array.Sort(cellDistances, cellIndices);
			if (mode == DestructionMode.Destroy) OrderSpawnedThingsForRemoval();
		}

		// map.spawnedThings removes with LastIndexOf + RemoveAt: instant for things at the end of its list, a search
		// and a shift of the whole list for things spawned early (like most plants and ruins, spawned at map
		// generation). Ordering it so the cells the wave reaches first come last makes every removal instant.
		// The order of that list means nothing to the game.
		private void OrderSpawnedThingsForRemoval()
		{
			if (!(Map.spawnedThings is ThingOwner<Thing> owner)) return;
			var rank = new int[Map.cellIndices.NumGridCells];
			for (int i = 0; i < rank.Length; i++) rank[i] = int.MaxValue;
			for (int i = 0; i < cellIndices.Length; i++) rank[cellIndices[i]] = i;
			owner.InnerListForReading.Sort((a, b) => Rank(b).CompareTo(Rank(a)));

			int Rank(Thing t) => t.Spawned ? rank[Map.cellIndices.CellToIndex(t.Position)] : int.MaxValue;
		}

		// Returns true when the wave has passed every cell.
		public bool Update(float deltaTime)
		{
			float speed = Config.ShockwaveSpeedCellsPerSecond;
			float target = radius + speed * deltaTime;
			float previousRadius = radius;

			if (mode == DestructionMode.Destroy)
			{
				if (ringSpeed < 0f) ringSpeed = speed;
				sinceDestruction += deltaTime;
				if (sinceDestruction >= DestructionInterval)
				{
					float before = DestroyedRadius;
					bool caughtUp = DestroyUpTo(radius + Mathf.Min(MaxAhead, speed * DestructionInterval));
					// Out of budget: destruction is the bottleneck, so the ring takes its pace. Otherwise full speed.
					float pace = caughtUp ? speed : Mathf.Min(speed, (DestroyedRadius - before) / sinceDestruction);
					ringSpeed = Mathf.Lerp(ringSpeed, pace, PaceSmoothing);
					sinceDestruction = 0f;
				}
				radius = Mathf.Min(radius + ringSpeed * deltaTime, DestroyedRadius + MaxLead);
			}
			else
			{
				radius = target;
			}

			if (deltaTime > 0f)
			{
				float speedNow = (radius - previousRadius) / deltaTime;
				measuredSpeed = measuredSpeed < 0f ? speed : Mathf.Lerp(measuredSpeed, speedNow, MeasuredSpeedSmoothing);
			}
			float stretch = Mathf.Clamp(speed / Mathf.Max(measuredSpeed, 0.01f), 1f, Config.Tuning.MaxStretch);

			while (nextVisualCell < cellIndices.Length && cellDistances[nextVisualCell] <= radius)
			{
				Profile.Start(Profile.Visuals);
				IntVec3 cell = Map.cellIndices.IndexToCell(cellIndices[nextVisualCell]);
				// Scorch covers every cell: gaps would show as bright squares of untouched ground.
				scorch?.Add(cell);
				if (Rand.Chance(Config.BaseCellChance)) AffectCellCheaply(cell, stretch);
				Profile.Stop(Profile.Visuals);
				nextVisualCell++;
			}

			bool destroyDone = mode != DestructionMode.Destroy || nextDestroyCell >= cellIndices.Length;
			return destroyDone && nextVisualCell >= cellIndices.Length;
		}

		// Must run before the map is used for anything but this wave again (end of wave, end of takeoff, stop).
		public void Finish()
		{
			ReleaseStash();
		}

		private void ReleaseStash()
		{
			Profile.Start(Profile.Stash);
			listerStash?.End();
			Profile.Stop(Profile.Stash);
		}

		// Radius of the first cell not destroyed yet.
		private float DestroyedRadius => nextDestroyCell < cellIndices.Length ? cellDistances[nextDestroyCell] : float.MaxValue;

		// Returns false if the time budget ran out before reaching the limit.
		private bool DestroyUpTo(float limit)
		{
			float budgetMs = Config.FrameBudgetMs;
			frameWatch.Restart();
			try
			{
				while (nextDestroyCell < cellIndices.Length && cellDistances[nextDestroyCell] <= limit)
				{
					if (frameWatch.Elapsed.TotalMilliseconds >= budgetMs) return false;
					if (Rand.Chance(Config.BaseCellChance)) Devastate(Map.cellIndices.IndexToCell(cellIndices[nextDestroyCell]), cellDistances[nextDestroyCell]);
					nextDestroyCell++;
				}
				return true;
			}
			finally
			{
				if (!holdStash) ReleaseStash();
			}
		}

		// Effects, scorch and (in scorch mode) pawns: everything that runs with the visible ring itself.
		private void AffectCellCheaply(IntVec3 cell, float stretch)
		{
			AddVisuals(cell, stretch);
			if (mode == DestructionMode.Scorch && Config.KillPawns) KillPawns(cell);
		}

		private void KillPawns(IntVec3 cell)
		{
			List<Thing> things = Map.thingGrid.ThingsListAtFast(cell);
			for (int i = things.Count - 1; i >= 0; i--)
			{
				if (i < things.Count && things[i] is Pawn pawn && !pawn.Dead) pawn.Kill(null);
			}
		}

		private void Devastate(IntVec3 cell, float distance)
		{
			Profile.Start(Profile.Things);
			AffectThings(cell, distance);
			Profile.Stop(Profile.Things);
			if (Config.AffectTerrain)
			{
				Profile.Start(Profile.Terrain);
				AffectTerrain(cell);
				Profile.Stop(Profile.Terrain);
			}
		}

		// Called right before anything is despawned, so batches that destroy nothing skip the lister shuffle.
		private void BeforeDespawn()
		{
			if (listerStash != null && !listerStash.Active) { Profile.Start(Profile.Stash); listerStash.Begin(); Profile.Stop(Profile.Stash); }
		}

		private void AffectTerrain(IntVec3 cell)
		{
			var terrain = Map.terrainGrid.TerrainAt(cell);
			if (terrain == TerrainDefOf.MechanoidPlatform || terrain == TerrainDefOf.OrbitalPlatform)
			{
				BeforeDespawn();
				Map.terrainGrid.SetTerrain(cell, TerrainDefOf.Space);
			}
			else
			{
				if (!Map.terrainGrid.CanRemoveTopLayerAt(cell)) return;
				// Removing a floor destroys the filth on it, and every despawn is costly; a dirty floor can stay.
				if (Config.SkipFilthFloors && HasFilth(cell)) return;
				if (Profile.Enabled) Profile.TerrainChanged++;
				BeforeDespawn();
				Map.terrainGrid.RemoveTopLayer(cell, doLeavings: false);
			}
		}

		private bool HasFilth(IntVec3 cell)
		{
			List<Thing> things = Map.thingGrid.ThingsListAtFast(cell);
			for (int i = 0; i < things.Count; i++)
			{
				if (things[i].def.category == ThingCategory.Filth) return true;
			}
			return false;
		}

		private void AffectThings(IntVec3 cell, float distance)
		{
			// Walls thin out with distance: the core is flattened, the outskirts keep some of their walls.
			bool wallsGo = Rand.Chance(Config.Tuning.WallChance(distance, Map));
			// Copy: destroying things mutates the grid list.
			List<Thing> things = Map.thingGrid.ThingsListAtFast(cell);
			tmpThings.Clear();
			for (int i = 0; i < things.Count && i < MaxThingsPerCell; i++)
			{
				tmpThings.Add(things[i]);
			}

			foreach (Thing thing in tmpThings)
			{
				if (thing == null || thing.Destroyed || !thing.Spawned || !thing.def.destroyable) continue;
				if (thing is Skyfaller) continue;

				// Skip motes
				if (thing.def.destroyOnDrop) continue;

				// Skip natural rock (mountains)
				if (thing.def.building != null && thing.def.building.isNaturalRock) continue;

				// Keep roof holders to avoid expensive collapse recalcs
				if (thing.def.holdsRoof && (!Config.DestroyRoofholders || !wallsGo)) continue;

				if (Profile.Enabled) Profile.Count(thing);
				if (thing is Plant)
				{
					if (Config.RemoveGrass) { BeforeDespawn(); thing.Destroy(DestroyMode.Vanish); }
					continue;
				}

				if (thing is Pawn pawn)
				{
					if (Config.KillPawns) { BeforeDespawn(); pawn.Kill(null); }
					continue;
				}

				// Vanish: no leavings or filth to spawn on a map that is about to be discarded.
				if (thing.def.category == ThingCategory.Building && Config.DestroyBuildings)
				{
					BeforeDespawn();
					thing.Destroy(DestroyMode.Vanish);
					continue;
				}

				if (thing.def.category == ThingCategory.Item && Config.DestroyItems)
				{
					BeforeDespawn();
					thing.Destroy(DestroyMode.Vanish);
				}
			}
			tmpThings.Clear();
		}

		private void AddVisuals(IntVec3 cell, float stretch)
		{
			if (Rand.Chance(Config.Tuning.RimChance))
			{
				ShockwaveEffects.RimFlash(Map, cell, stretch);
			}
			if (Rand.Chance(Config.VisualsFleckChance))
			{
				ShockwaveEffects.CellFlash(Map, cell, stretch);
			}
			if (Rand.Chance(Config.FireStartChancePerCell))
			{
				effects?.StartFire(cell, Config.FireSize);
			}
			if (Rand.Chance(Config.ExplosionChancePerCell))
			{
				effects?.StartExplosion(cell);
			}
		}
	}
}
