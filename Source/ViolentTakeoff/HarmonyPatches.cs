using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ViolentTakeoff
{
	[HarmonyPatch]
	public static class HarmonyPatches
	{
		[HarmonyPatch(typeof(WorldComponent_GravshipController), "BeginTakeoffCutscene")]
		[HarmonyPostfix]
		private static void Postfix_BeginTakeoffCutscene(WorldComponent_GravshipController __instance)
		{
			Verse.Log.Message("Beginning takeoff cutscene postfix");
			if (__instance == null)
				return;

			var instType = __instance.GetType();
			var gravshipField = AccessTools.Field(instType, "gravship");
			var gravshipObj = gravshipField?.GetValue(__instance);
			if (gravshipObj == null)
				return;
			
			Verse.Log.Message("Got gravship");

			var originalPosField = AccessTools.Field(gravshipObj.GetType(), "originalPosition");
			if (originalPosField == null)
				return;
			IntVec3 originalCell = (IntVec3)originalPosField.GetValue(gravshipObj);

			Verse.Log.Message("Got starting cell");

			Map map = null;
			var mapField = AccessTools.Field(instType, "map");
			if (mapField != null)
				map = mapField.GetValue(__instance) as Map;

			if (map == null)
				return;

            if (map.listerThings.AnyThingWithDef(ThingDefOf.GravAnchor)) {
                return;
            }

            Verse.Log.Message("Got map");

            DevastationManager.SuppressRoofChecks = true;
            SoftenCutsceneShake();
            // The game stays paused for the whole cutscene; the shockwave animates on real time.
            CellRect? area = CutsceneViewArea(__instance, gravshipObj);
            MapComponent_ViolentTakeoff.For(map)?.StartShockwave(originalCell, area, duringTakeoff: true);

			Verse.Log.Message("Started shockwave at " + originalCell + ", area " + (area?.ToString() ?? "whole map") +
				$", mode {Config.Mode}, fastBookkeeping {Config.FastBookkeeping}, keepDirtyFloors {Config.SkipFilthFloors}, speed {Config.ShockwaveSpeedCellsPerSecond}, budget {Config.FrameBudgetMs}ms");
		}

		// The cutscene camera stays on the capture center and zooms out to zoomRange.max (an orthographic half-height
		// in cells). Returns the most it will ever show, or null (whole map) if the player can move the camera.
		private static CellRect? CutsceneViewArea(WorldComponent_GravshipController controller, object gravship)
		{
			if (Find.CameraDriver.config.gravshipFreeCam) return null;
			var capture = AccessTools.Field(gravship.GetType(), "capture")?.GetValue(gravship) as Capture;
			var zoomField = AccessTools.Field(controller.GetType(), "zoomRange");
			if (capture == null || zoomField == null) return null;
			float halfHeight = ((FloatRange)zoomField.GetValue(controller)).max;
			float halfWidth = halfHeight * UI.screenWidth / UI.screenHeight;
			const float Margin = 4f;
			Vector3 c = capture.captureCenter;
			return CellRect.FromLimits(
				Mathf.FloorToInt(c.x - halfWidth - Margin), Mathf.FloorToInt(c.z - halfHeight - Margin),
				Mathf.CeilToInt(c.x + halfWidth + Margin), Mathf.CeilToInt(c.z + halfHeight + Margin));
		}

		// Vanilla starts the cutscene with DoShake(0.2f, 120). Its duration counts game ticks, which stand still while
		// the cutscene keeps the game paused, so it shakes at full strength until takeoff ends. Swap it for a weaker one.
		private const float CutsceneShakeMagnitude = 0.02f;
		private static readonly AccessTools.FieldRef<CameraShaker, System.Collections.IList> ExtendedShakeRequests =
			AccessTools.FieldRefAccess<CameraShaker, System.Collections.IList>("extendedShakeRequests");

		private static void SoftenCutsceneShake()
		{
			CameraShaker shaker = Find.CameraDriver?.shaker;
			if (shaker == null) return;
			System.Collections.IList requests = ExtendedShakeRequests(shaker);
			if (requests != null && requests.Count > 0)
				requests.RemoveAt(requests.Count - 1); // the one BeginTakeoffCutscene just added
			shaker.StopAllShaking();
			shaker.DoShake(CutsceneShakeMagnitude, 120);
		}

		// Prefix: the map is abandoned and torn down inside TakeoffEnded, and its thing lists must be whole by then.
		[HarmonyPatch(typeof(WorldComponent_GravshipController), "TakeoffEnded")]
		[HarmonyPrefix]
		private static void Prefix_TakeoffEnded(WorldComponent_GravshipController __instance) {
			DevastationManager.SuppressRoofChecks = false;
			var map = AccessTools.Field(__instance.GetType(), "map")?.GetValue(__instance) as Map;
			MapComponent_ViolentTakeoff.For(map)?.StopAll();
		}


        [HarmonyPatch(typeof(RoofCollapseCellsFinder), "ProcessRoofHolderDespawned")]
        [HarmonyPrefix]		
        static bool Prefix_RoofCollapseCellFinder_ProcessRoofHolderDespawned() {
            if (DevastationManager.SuppressRoofChecks)
                return false; // skip collapse checks entirely
            return true; // normal
        }
    }
} 