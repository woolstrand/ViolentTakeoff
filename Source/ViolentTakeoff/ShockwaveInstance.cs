using RimWorld;
using UnityEngine;
using Verse;
using Verse.Noise;
using System.Linq;

namespace ViolentTakeoff
{
	public class ShockwaveInstance
	{
		public readonly Map Map;
		public readonly IntVec3 Center;
		public float CurrentRadius;

		public ShockwaveInstance(IntVec3 center, Map map)
		{
			Center = center;
			Map = map;
			CurrentRadius = 10f;
		}

		public bool TickOnce()
		{
			if (Map == null || !Center.InBounds(Map))
			{
				return true;
			}

			float advance = Mathf.Max(0.01f, Config.ShockwaveAdvancePerUpdate);
			float radius = CurrentRadius;

			// Process only cells at the exact current radius
			ProcessRingAtRadius(radius);

			CurrentRadius += advance;

			float maxRadius = MaxRadiusForMap();
			return CurrentRadius > maxRadius;
		}

		private void ProcessRingAtRadius(float radius)
		{
			if (radius <= 0f) return;

			// Use integer radius for cell-based calculations
			int intRadius = Mathf.RoundToInt(radius);
			
			// Traverse the ring using Bresenham's circle algorithm
			int x = intRadius;
			int y = 0;
			int err = 0;

			while (x >= y)
			{
				// Process the 8 octants of the circle
				ProcessCell(Center.x + x, Center.z + y);
				ProcessCell(Center.x + y, Center.z + x);
				ProcessCell(Center.x - y, Center.z + x);
				ProcessCell(Center.x - x, Center.z + y);
				ProcessCell(Center.x - x, Center.z - y);
				ProcessCell(Center.x - y, Center.z - x);
				ProcessCell(Center.x + y, Center.z - x);
				ProcessCell(Center.x + x, Center.z - y);

				if (err < 0)
				{
					y += 1;
					err += 2 * y + 1;
				}
				if (err >= 0)
				{
					x -= 1;
					err -= 2 * x + 1;
				}
			}
		}

		private void ProcessCell(int x, int z)
		{
			var cell = new IntVec3(x, 0, z);
			if (!cell.InBounds(Map, contractedBy: 1)) return;

			float baseCellChance = Config.BaseCellChance;
            if (Rand.Chance(baseCellChance)) AffectCell(new IntVec3(x, 0, z));
            if (Rand.Chance(baseCellChance)) AffectCell(new IntVec3(x + 1, 0, z));
            if (Rand.Chance(baseCellChance)) AffectCell(new IntVec3(x - 1, 0, z));
            if (Rand.Chance(baseCellChance)) AffectCell(new IntVec3(x, 0, z + 1));
            if (Rand.Chance(baseCellChance)) AffectCell(new IntVec3(x, 0, z - 1));
        }

        private float MaxRadiusForMap()
		{
			var size = Map.Size;
			var corners = new[]
			{
				new IntVec3(0, 0, 0),
				new IntVec3(size.x - 1, 0, 0),
				new IntVec3(0, 0, size.z - 1),
				new IntVec3(size.x - 1, 0, size.z - 1)
			};
			float max = 0f;
			for (int i = 0; i < corners.Length; i++)
			{
				float dist = (corners[i] - Center).LengthHorizontal;
				if (dist > max) max = dist;
			}
			return max + 2f;
		}

		private void AffectCell(IntVec3 cell) {
			if (!Config.VisualsOnly) {
				AffectThings(cell);
			}
			if (Config.AffectTerrain) {
				AffectTerrain(cell);
            }
			AddVisuals(cell);
		}

		private void AffectTerrain(IntVec3 cell) {
            var terrain = Map.terrainGrid.TerrainAt(cell);
            if (terrain == TerrainDefOf.MechanoidPlatform || terrain == TerrainDefOf.OrbitalPlatform) {
                Map.terrainGrid.SetTerrain(cell, TerrainDefOf.Space);
            } else {
                Map.terrainGrid.RemoveTopLayer(cell, doLeavings: false);
            }
        }

        private void AffectThings(IntVec3 cell) {
            var things = GridsUtility.GetThingList(cell, Map).Take(10).ToList();
			int processed = 0;
			foreach (Thing thing in things) {

				//early stop for long stacks
				processed += 1;
				if (processed > 10) return;

				if (thing == null || thing.Destroyed || !thing.def.destroyable) continue;
				if (thing is Skyfaller) continue;

				// Skip motes
				if (thing.def.destroyOnDrop)
					continue;

				// Skip natural rock (mountains)
				if (thing.def.building != null && thing.def.building.isNaturalRock)
					continue;

                // Keep roof holders to avoid expensive collapse recalcs
                if (thing.def.holdsRoof && !Config.DestroyRoofholders) {
                    continue;
                }

                // Handle plants (remove grass, crops, etc.)
                if (thing is Plant plant && Config.RemoveGrass) {
                    plant.Destroy(DestroyMode.Vanish);
                    continue;
                }

                if (thing is Pawn pawn && Config.KillPawns) {
                    pawn.Kill(null);
                    continue;
                }

                if (thing.def.category == ThingCategory.Building && Config.DestroyBuildings) {
					thing.Destroy(DestroyMode.KillFinalize);
					continue;
				}

                if (thing.def.category == ThingCategory.Item && Config.DestroyItems) {
					thing.Destroy(DestroyMode.KillFinalize);
					continue;
				}

			}
		}

        private void AddVisuals(IntVec3 cell) {
            // Visuals
            if (Rand.Chance(Config.VisualsFleckChance))
			{
				float scale = Rand.Range(Config.FleckScaleMin, Config.FleckScaleMax);
				FleckMaker.ThrowExplosionCell(cell, Map, FleckDefOf.ExplosionFlash, Color.yellow);
			}
			if (Rand.Chance(Config.VisualsSmokeChance))
			{
				float size = Rand.Range(Config.SmokeSizeMin, Config.SmokeSizeMax);
				FleckMaker.ThrowSmoke(cell.ToVector3Shifted(), Map, size);
			}
            if (Rand.Chance(Config.FireStartChancePerCell)) {
                FireUtility.TryStartFireIn(cell, Map, Config.FireSize, instigator: null);
            }
            if (Rand.Chance(Config.ExplosionChancePerCell)) {
				GenExplosion.DoExplosion(center: cell, map: Map, radius: 4.5f, damType: DamageDefOf.Bomb, instigator: null, damAmount: 1, screenShakeFactor: 0);
            }
        }

        private static void PlaceRubble(Map map, IntVec3 origin) {
            foreach (var c in GenRadial.RadialCellsAround(origin, 3, true)) {
                if (!c.InBounds(map) || c.Filled(map))
                    continue;

                if (Rand.Chance(0.2f)) // chance per cell
                {
                    GenSpawn.Spawn(ThingDefOf.Filth_RubbleBuilding, c, map, WipeMode.Vanish);
                }
            }
        }
    }
} 