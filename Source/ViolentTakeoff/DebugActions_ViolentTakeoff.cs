using RimWorld;
using Verse;
using LudeonTK;
using System.Linq;

namespace ViolentTakeoff
{
	public static class DebugActions_ViolentTakeoff
	{
        [DebugAction("Violent Takeoff", "Start shockwave...", false, false, false, false, false, 0, false, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void StartShockwaveTool()
		{
			var map = Find.CurrentMap;
			if (map == null) return;
			MapComponent_ViolentTakeoff.Ensure(map)?.StartShockwave(UI.MouseCell());
		}

		[DebugAction("Violent Takeoff", "Stop all shockwaves", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void StopAllShockwaves()
		{
			var map = Find.CurrentMap;
			if (map == null) return;
			MapComponent_ViolentTakeoff.For(map)?.StopAll();
			Messages.Message("Stopped all shockwaves on map", MessageTypeDefOf.NeutralEvent);
		}
	}
} 