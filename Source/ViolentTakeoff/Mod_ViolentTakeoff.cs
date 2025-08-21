using HarmonyLib;
using Verse;
using UnityEngine;

namespace ViolentTakeoff
{
	public class Mod_ViolentTakeoff : Mod
	{
		public static Settings_ViolentTakeoff Settings;

		public Mod_ViolentTakeoff(ModContentPack content) : base(content)
		{
			Settings = GetSettings<Settings_ViolentTakeoff>();
			var harmony = new Harmony("ViolentTakeoff.Mod");
			harmony.PatchAll();
		}

		public override string SettingsCategory() => "VT_Settings_Category".Translate();

		public override void DoSettingsWindowContents(Rect inRect)
		{
			Settings?.DoWindowContents(inRect);
		}
	}
} 