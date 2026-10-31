using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ViolentTakeoff
{
	// Visuals built only from realTime flecks, so they play while the game is paused.
	// Nothing here spawns Things (Explosion, Fire): those need ticks to do anything.
	public static class ShockwaveEffects
	{
		private const float ExplosionRadius = 3.5f;
		private const float MinSecondsBetweenExplosionSounds = 0.12f;
		private static readonly Color ExplosionColorCenter = new Color(1f, 0.5f, 0.3f);
		private static readonly Color ExplosionColorEdge = new Color(0.6f, 0.5f, 0.4f);

		private static float lastExplosionSoundTime = float.MinValue;

		public static void Ring(Map map, IntVec3 center, float startRadius, float speed, float maxRadius)
		{
			FleckDef def = VT_FleckDefOf.VT_ShockwaveRing;
			// Growth is a def-level rate (draw size per second); the ring's diameter grows twice as fast as the radius.
			def.growthRate = speed * 2f;
			FleckCreationData data = FleckMaker.GetDataStatic(center.ToVector3Shifted(), map, def, startRadius * 2f);
			data.solidTimeOverride = Mathf.Max(0f, (maxRadius - startRadius) / Mathf.Max(speed, 0.01f) - def.fadeInTime);
			map.flecks.CreateFleck(data);
		}

		// Same as vanilla FleckMaker.ThrowExplosionCell (scale 1, dust puff 70% of the time), on realTime flecks.
		public static void CellFlash(Map map, IntVec3 cell)
		{
			FleckCreationData data = FleckMaker.GetDataStatic(cell.ToVector3Shifted(), map, VT_FleckDefOf.VT_ExplosionFlash);
			data.rotation = 90 * Rand.RangeInclusive(0, 3);
			data.instanceColor = Color.yellow;
			map.flecks.CreateFleck(data);
			if (Rand.Value < 0.7f)
			{
				DustPuff(map, cell, 1.2f);
			}
		}

		public static void DustPuff(Map map, IntVec3 cell, float scale)
		{
			Vector3 loc = cell.ToVector3() + new Vector3(Rand.Value, 0f, Rand.Value);
			FleckCreationData data = FleckMaker.GetDataStatic(loc, map, VT_FleckDefOf.VT_DustPuff, 1.9f * scale);
			data.rotationRate = Rand.Range(-60, 60);
			data.velocityAngle = Rand.Range(0, 360);
			data.velocitySpeed = Rand.Range(0.6f, 0.75f);
			map.flecks.CreateFleck(data);
		}

		public static void Smoke(Map map, Vector3 loc, float size)
		{
			FleckCreationData data = FleckMaker.GetDataStatic(loc, map, VT_FleckDefOf.VT_Smoke, Rand.Range(1.5f, 2.5f) * size);
			data.rotationRate = Rand.Range(-30f, 30f);
			data.velocityAngle = Rand.Range(30, 40);
			data.velocitySpeed = Rand.Range(0.5f, 0.7f);
			map.flecks.CreateFleck(data);
		}

		public static void Fire(Map map, IntVec3 cell, float size)
		{
			Vector3 loc = cell.ToVector3Shifted() + new Vector3(Rand.Value - 0.5f, 0f, Rand.Value - 0.5f);
			FleckCreationData data = FleckMaker.GetDataStatic(loc, map, VT_FleckDefOf.VT_FireGlow, Rand.Range(1f, 1.5f) * size);
			data.rotationRate = Rand.Range(-3f, 3f);
			data.velocityAngle = Rand.Range(0, 360);
			data.velocitySpeed = 0.12f;
			map.flecks.CreateFleck(data);
			Smoke(map, loc, size * 0.5f);
		}

		// Mimics Explosion + DamageWorker visuals: center flash, colored blast per cell, smoke, sound.
		public static void Explosion(Map map, IntVec3 center)
		{
			float now = Time.realtimeSinceStartup;
			Vector3 centerLoc = center.ToVector3Shifted();
			map.flecks.CreateFleck(FleckMaker.GetDataStatic(centerLoc, map, VT_FleckDefOf.VT_ExplosionFlash, ExplosionRadius * 6f));
			for (int i = 0; i < 4; i++)
			{
				Smoke(map, centerLoc + Gen.RandomHorizontalVector(ExplosionRadius * 0.7f), ExplosionRadius * 0.6f);
			}
			foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, ExplosionRadius, useCenter: true))
			{
				if (!cell.InBounds(map)) continue;
				FleckCreationData data = FleckMaker.GetDataStatic(cell.ToVector3Shifted(), map, VT_FleckDefOf.VT_BlastDry);
				data.rotation = 90 * Rand.RangeInclusive(0, 3);
				data.instanceColor = Color.Lerp(ExplosionColorCenter, ExplosionColorEdge, (center - cell).LengthHorizontal / ExplosionRadius);
				map.flecks.CreateFleck(data);
			}
			// Many explosions can land in one frame; stacking their sounds only clips.
			if (now - lastExplosionSoundTime >= MinSecondsBetweenExplosionSounds)
			{
				lastExplosionSoundTime = now;
				DamageDefOf.Bomb.soundExplosion?.PlayOneShot(new TargetInfo(center, map));
			}
		}
	}
}
