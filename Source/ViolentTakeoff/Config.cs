using Verse;

namespace ViolentTakeoff
{
	public static class Config
	{
		public static float ShockwaveSpeedCellsPerSecond => Mod_ViolentTakeoff.Settings?.ShockwaveSpeedCellsPerSecond ?? 22.5f;
		public static float FireStartChancePerCell => Mod_ViolentTakeoff.Settings?.FireStartChancePerCell ?? 0.01f;
		public static float ExplosionChancePerCell => Mod_ViolentTakeoff.Settings?.ExplosionChancePerCell ?? 0.001f;
		public static float FireSize => Mod_ViolentTakeoff.Settings?.FireSize ?? 0.6f;
		public static float VisualsFleckChance => Mod_ViolentTakeoff.Settings?.VisualsFleckChance ?? 0.8f;
		public static int CellsPerFrame => Mod_ViolentTakeoff.Settings?.CellsPerFrame ?? 1500;
		public static float BaseCellChance => Mod_ViolentTakeoff.Settings?.BaseCellChance ?? 0.55f;
		public static bool RemoveGrass => Mod_ViolentTakeoff.Settings?.RemoveGrass ?? true;
		public static bool DestroyItems => Mod_ViolentTakeoff.Settings?.DestroyItems ?? true;
		public static bool DestroyBuildings => Mod_ViolentTakeoff.Settings?.DestroyBuildings ?? true;
		public static bool KillPawns => Mod_ViolentTakeoff.Settings?.KillPawns ?? true;
		public static bool VisualsOnly => Mod_ViolentTakeoff.Settings?.VisualsOnly ?? false;
		public static bool DestroyRoofholders => Mod_ViolentTakeoff.Settings?.DestroyRoofholders ?? false;
		public static bool AffectTerrain => Mod_ViolentTakeoff.Settings?.AffectTerrain ?? true;


	}

	public static class DevastationManager {
		public static bool SuppressRoofChecks = false;
	}
} 