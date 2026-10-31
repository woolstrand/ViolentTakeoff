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
	// Two cursors walk the list: effects every frame (cheap, keeps the front smooth), destruction in batches
	// (expensive; each batch dirties map mesh sections, and fewer batches mean fewer section rebuilds).
	public class ShockwaveInstance
	{
		private const float StartRadius = 10f;
		private const int MaxThingsPerCell = 10;
		private const float DestructionBatchInterval = 0.1f;

		public readonly Map Map;
		public readonly IntVec3 Center;

		private readonly int[] cellIndices;
		private readonly float[] cellDistances;
		private int nextVisualCell;
		private int nextDestroyCell;
		private float radius = StartRadius;
		private float sinceDestructionBatch;

		private readonly List<Thing> tmpThings = new List<Thing>();

		public float Radius => radius;
		public int CellsDone => nextDestroyCell;
		public int CellsTotal => cellIndices.Length;

		public float MaxRadius => cellDistances.Length > 0 ? cellDistances[cellDistances.Length - 1] : 0f;

		// area: cells the wave touches at all; null = whole map. The takeoff passes the part of the map its camera
		// can show: anything else is never seen and is discarded with the map anyway.
		public ShockwaveInstance(IntVec3 center, Map map, CellRect? area = null)
		{
			Center = center;
			Map = map;

			CellRect rect = CellRect.WholeMap(map).ContractedBy(1);
			if (area.HasValue) rect = area.Value.ClipInsideRect(rect);
			var indices = new List<int>(rect.Area);
			var distances = new List<float>(rect.Area);
			foreach (IntVec3 cell in rect)
			{
				float dist = (cell - center).LengthHorizontal;
				if (dist < StartRadius) continue;
				indices.Add(map.cellIndices.CellToIndex(cell));
				distances.Add(dist);
			}
			cellIndices = indices.ToArray();
			cellDistances = distances.ToArray();
			Array.Sort(cellDistances, cellIndices);

			ShockwaveEffects.Ring(map, center, StartRadius, Config.ShockwaveSpeedCellsPerSecond, MaxRadius);
		}

		// Returns true when the wave has passed every cell.
		public bool Update(float deltaTime)
		{
			radius += Config.ShockwaveSpeedCellsPerSecond * deltaTime;

			while (nextVisualCell < cellIndices.Length && cellDistances[nextVisualCell] <= radius)
			{
				Profile.Visuals.Start();
				if (Rand.Chance(Config.BaseCellChance)) AddVisuals(Map.cellIndices.IndexToCell(cellIndices[nextVisualCell]));
				Profile.Visuals.Stop();
				nextVisualCell++;
			}

			sinceDestructionBatch += deltaTime;
			if (sinceDestructionBatch >= DestructionBatchInterval || nextVisualCell >= cellIndices.Length)
			{
				sinceDestructionBatch = 0f;
				// Budget keeps a dense base from causing a hitch; leftover cells are picked up next batch.
				int budget = Config.CellsPerFrame;
				while (nextDestroyCell < nextVisualCell && budget-- > 0)
				{
					if (Rand.Chance(Config.BaseCellChance)) Devastate(Map.cellIndices.IndexToCell(cellIndices[nextDestroyCell]));
					nextDestroyCell++;
				}
			}
			return nextDestroyCell >= cellIndices.Length;
		}

		private void Devastate(IntVec3 cell)
		{
			// TEMP profiling (dev mode): split our per-cell cost by category.
			if (!Config.VisualsOnly)
			{
				Profile.Things.Start();
				AffectThings(cell);
				Profile.Things.Stop();
			}
			if (Config.AffectTerrain)
			{
				Profile.Terrain.Start();
				AffectTerrain(cell);
				Profile.Terrain.Stop();
			}
		}

		private void AffectTerrain(IntVec3 cell)
		{
			var terrain = Map.terrainGrid.TerrainAt(cell);
			if (terrain == TerrainDefOf.MechanoidPlatform || terrain == TerrainDefOf.OrbitalPlatform)
			{
				Map.terrainGrid.SetTerrain(cell, TerrainDefOf.Space);
			}
			else
			{
				if (Map.terrainGrid.CanRemoveTopLayerAt(cell)) Profile.TerrainChanged++;
				Map.terrainGrid.RemoveTopLayer(cell, doLeavings: false);
			}
		}

		private void AffectThings(IntVec3 cell)
		{
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
				if (thing.def.holdsRoof && !Config.DestroyRoofholders) continue;

				Profile.Count(thing);
				if (thing is Plant)
				{
					if (Config.RemoveGrass) thing.Destroy(DestroyMode.Vanish);
					continue;
				}

				if (thing is Pawn pawn)
				{
					if (Config.KillPawns) pawn.Kill(null);
					continue;
				}

				// Vanish: no leavings or filth to spawn on a map that is about to be discarded.
				if (thing.def.category == ThingCategory.Building && Config.DestroyBuildings)
				{
					thing.Destroy(DestroyMode.Vanish);
					continue;
				}

				if (thing.def.category == ThingCategory.Item && Config.DestroyItems)
				{
					thing.Destroy(DestroyMode.Vanish);
				}
			}
			tmpThings.Clear();
		}

		private void AddVisuals(IntVec3 cell)
		{
			if (Rand.Chance(Config.VisualsFleckChance))
			{
				ShockwaveEffects.CellFlash(Map, cell);
			}
			if (Rand.Chance(Config.FireStartChancePerCell))
			{
				ShockwaveEffects.Fire(Map, cell, Config.FireSize);
			}
			if (Rand.Chance(Config.ExplosionChancePerCell))
			{
				ShockwaveEffects.Explosion(Map, cell);
			}
		}
	}

	// TEMP profiling helper, remove once performance is settled.
	public static class Profile
	{
		public static readonly System.Diagnostics.Stopwatch Things = new System.Diagnostics.Stopwatch();
		public static readonly System.Diagnostics.Stopwatch Terrain = new System.Diagnostics.Stopwatch();
		public static readonly System.Diagnostics.Stopwatch Visuals = new System.Diagnostics.Stopwatch();
		public static int Plants, Buildings, RoofHolders, Items, Pawns, Other, TerrainChanged;

		public static void Count(Thing thing)
		{
			if (thing is Plant) Plants++;
			else if (thing is Pawn) Pawns++;
			else if (thing.def.category == ThingCategory.Building) { if (thing.def.holdsRoof) RoofHolders++; else Buildings++; }
			else if (thing.def.category == ThingCategory.Item) Items++;
			else Other++;
		}

		public static string Report()
		{
			string r = $"things={Things.Elapsed.TotalMilliseconds:0}ms terrain={Terrain.Elapsed.TotalMilliseconds:0}ms visuals={Visuals.Elapsed.TotalMilliseconds:0}ms " +
				$"| plants={Plants} buildings={Buildings} roofholders={RoofHolders} items={Items} pawns={Pawns} other={Other} terrainChanged={TerrainChanged}";
			Things.Reset(); Terrain.Reset(); Visuals.Reset();
			Plants = Buildings = RoofHolders = Items = Pawns = Other = TerrainChanged = 0;
			return r;
		}
	}
}
