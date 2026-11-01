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
		private const float MinSecondsBetweenExplosionSounds = 0.12f;

		private static float lastExplosionSoundTime = float.MinValue;

		private static DebugTuning T => Config.Tuning;

		// Fleck lifetimes live on the defs; copy the tuned ones there before a wave starts.
		public static void ApplyTuning()
		{
			VT_FleckDefOf.VT_ExplosionFlash.solidTime = T.FlashSolid;
			VT_FleckDefOf.VT_ExplosionFlash.fadeOutTime = T.FlashFade;
			VT_FleckDefOf.VT_DustPuff.fadeOutTime = T.DustFade;
			VT_FleckDefOf.VT_BlastDry.fadeOutTime = T.BlastFade;
			VT_FleckDefOf.VT_FireGlow.solidTime = T.FireSolid;
			VT_FleckDefOf.VT_FireGlow.fadeOutTime = T.FireFade;
		}

		// Like vanilla FleckMaker.ThrowExplosionCell on realTime flecks. Defaults sit between the original's look
		// (scale 1, 70% dust: reads pale here) and a scale of 1.2-5.2 (reads as an orange band).
		public static void CellFlash(Map map, IntVec3 cell)
		{
			FleckCreationData data = FleckMaker.GetDataStatic(cell.ToVector3Shifted(), map, VT_FleckDefOf.VT_ExplosionFlash, Rand.Range(T.FlashScaleMin, T.FlashScaleMax));
			data.rotation = 90 * Rand.RangeInclusive(0, 3);
			data.instanceColor = T.FlashColor;
			map.flecks.CreateFleck(data);
			if (Rand.Value < T.DustChance)
			{
				DustPuff(map, cell, T.DustScale);
			}
		}

		public static void DustPuff(Map map, IntVec3 cell, float scale)
		{
			Vector3 loc = cell.ToVector3() + new Vector3(Rand.Value, 0f, Rand.Value);
			FleckCreationData data = FleckMaker.GetDataStatic(loc, map, VT_FleckDefOf.VT_DustPuff, 1.9f * scale);
			data.rotationRate = Rand.Range(-60, 60);
			data.velocityAngle = Rand.Range(0, 360);
			data.velocitySpeed = Rand.Range(0.6f, 0.75f);
			data.instanceColor = Color.white * T.DustBrightness;
			map.flecks.CreateFleck(data);
		}

		public static void Smoke(Map map, Vector3 loc, float size)
		{
			FleckCreationData data = FleckMaker.GetDataStatic(loc, map, VT_FleckDefOf.VT_Smoke, Rand.Range(1.5f, 2.5f) * size);
			data.rotationRate = Rand.Range(-30f, 30f);
			data.velocityAngle = Rand.Range(30, 40);
			data.velocitySpeed = Rand.Range(0.5f, 0.7f);
			data.instanceColor = Color.white * T.SmokeBrightness;
			map.flecks.CreateFleck(data);
		}

		public static void Fire(Map map, IntVec3 cell, float size)
		{
			Vector3 loc = cell.ToVector3Shifted() + new Vector3(Rand.Value - 0.5f, 0f, Rand.Value - 0.5f);
			FleckCreationData data = FleckMaker.GetDataStatic(loc, map, VT_FleckDefOf.VT_FireGlow, Rand.Range(T.FireScaleMin, T.FireScaleMax) * size);
			data.rotationRate = Rand.Range(-3f, 3f);
			data.velocityAngle = Rand.Range(0, 360);
			data.velocitySpeed = 0.12f;
			data.instanceColor = Color.white * T.FireBrightness;
			map.flecks.CreateFleck(data);
			Smoke(map, loc, size * 0.5f);
		}

		// Mimics Explosion + DamageWorker visuals: center flash, colored blast per cell, smoke, sound.
		public static void Explosion(Map map, IntVec3 center)
		{
			float now = Time.realtimeSinceStartup;
			float radius = T.ExplosionRadius;
			Vector3 centerLoc = center.ToVector3Shifted();
			FleckCreationData flash = FleckMaker.GetDataStatic(centerLoc, map, VT_FleckDefOf.VT_ExplosionFlash, radius * T.ExplosionFlashScale);
			flash.instanceColor = Color.white * T.ExplosionFlashBrightness;
			map.flecks.CreateFleck(flash);
			int smokeCount = Mathf.RoundToInt(T.ExplosionSmokeCount);
			for (int i = 0; i < smokeCount; i++)
			{
				Smoke(map, centerLoc + Gen.RandomHorizontalVector(radius * 0.7f), radius * 0.6f);
			}
			Color colorCenter = T.BlastColorCenter, colorEdge = T.BlastColorEdge;
			foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, useCenter: true))
			{
				if (!cell.InBounds(map)) continue;
				FleckCreationData data = FleckMaker.GetDataStatic(cell.ToVector3Shifted(), map, VT_FleckDefOf.VT_BlastDry);
				data.rotation = 90 * Rand.RangeInclusive(0, 3);
				data.instanceColor = Color.Lerp(colorCenter, colorEdge, (center - cell).LengthHorizontal / radius);
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
