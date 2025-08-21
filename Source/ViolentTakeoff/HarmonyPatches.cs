using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
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
            MapComponent_ViolentTakeoff.Ensure(map)?.StartShockwave(originalCell);

			Verse.Log.Message("Started shockwave at " + originalCell);
		}

		[HarmonyPatch(typeof(WorldComponent_GravshipController), "TakeoffEnded")]
		[HarmonyPostfix]
		private static void Postfix_TakeoffEnded(WorldComponent_GravshipController __instance) {
			DevastationManager.SuppressRoofChecks = false;
		}

        [HarmonyPatch(typeof(TickManager), nameof(TickManager.ForcePaused), MethodType.Getter)]
		[HarmonyPostfix]
		private static void Postfix_TickManager_ForcePaused(ref bool __result)
		{
			if (!WorldComponent_GravshipController.CutsceneInProgress)
				return;

			bool otherPause = false;
			var ws = Find.WindowStack;
			if (ws != null && ws.WindowsForcePause) otherPause = true;
			if (LongEventHandler.ForcePause) otherPause = true;
			if (Find.TilePicker.Active) otherPause = true;

			if (!otherPause)
			{
				__result = false;
			}
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