using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ViolentTakeoff
{
	// Driven by real time (frame deltaTime), not ticks: the takeoff cutscene keeps the game paused.
	// All map cells are sorted by distance from the center once; the wave walks that list as the radius
	// grows, so every cell is reached exactly once and no cell is skipped when the radius jumps.
	public class ShockwaveInstance
	{
		private const float StartRadius = 10f;
		private const int MaxThingsPerCell = 10;

		public readonly Map Map;
		public readonly IntVec3 Center;

		private readonly int[] cellIndices;
		private readonly float[] cellDistances;
		private int nextCell;
		private float radius = StartRadius;

		private readonly List<Thing> tmpThings = new List<Thing>();

		public float MaxRadius => cellDistances.Length > 0 ? cellDistances[cellDistances.Length - 1] : 0f;

		public ShockwaveInstance(IntVec3 center, Map map)
		{
			Center = center;
			Map = map;

			CellRect rect = CellRect.WholeMap(map).ContractedBy(1);
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

			// Budget keeps a dense base from causing a hitch; leftover cells are picked up next frame.
			int budget = Config.CellsPerFrame;
			while (nextCell < cellIndices.Length && cellDistances[nextCell] <= radius && budget-- > 0)
			{
				ProcessCell(Map.cellIndices.IndexToCell(cellIndices[nextCell]));
				nextCell++;
			}
			return nextCell >= cellIndices.Length;
		}

		private void ProcessCell(IntVec3 cell)
		{
			if (!Rand.Chance(Config.BaseCellChance)) return;

			if (!Config.VisualsOnly)
			{
				AffectThings(cell);
			}
			if (Config.AffectTerrain)
			{
				AffectTerrain(cell);
			}
			AddVisuals(cell);
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
			if (Rand.Chance(Config.DustPuffChance))
			{
				ShockwaveEffects.DustPuff(Map, cell);
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
}
