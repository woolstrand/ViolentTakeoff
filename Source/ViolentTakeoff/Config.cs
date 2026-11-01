using Verse;

namespace ViolentTakeoff
{
	public static class Config
	{
		private static readonly Settings_ViolentTakeoff Defaults = new Settings_ViolentTakeoff();
		private static Settings_ViolentTakeoff S => Mod_ViolentTakeoff.Settings ?? Defaults;

		public static float ShockwaveSpeedCellsPerSecond => S.ShockwaveSpeedCellsPerSecond;
		public static float FireStartChancePerCell => S.FireStartChancePerCell;
		public static float ExplosionChancePerCell => S.ExplosionChancePerCell;
		public static float FireSize => S.FireSize;
		public static float VisualsFleckChance => S.VisualsFleckChance;
		public static float FrameBudgetMs => S.FrameBudgetMs;
		public static float BaseCellChance => S.BaseCellChance;
		public static DestructionMode Mode => S.Mode;
		public static bool FastBookkeeping => S.FastBookkeeping;
		public static bool SkipFilthFloors => S.SkipFilthFloors;
		public static bool RemoveGrass => S.RemoveGrass;
		public static bool DestroyItems => S.DestroyItems;
		public static bool DestroyBuildings => S.DestroyBuildings;
		public static bool KillPawns => S.KillPawns;
		public static bool DestroyRoofholders => S.DestroyRoofholders;
		public static bool AffectTerrain => S.AffectTerrain;
		public static bool ProfilingLog => S.ProfilingLog;
		public static DebugTuning Tuning => S.Tuning ?? (S.Tuning = new DebugTuning());
	}

	public static class DevastationManager {
		public static bool SuppressRoofChecks = false;
	}
}
