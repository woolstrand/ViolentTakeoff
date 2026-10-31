using Verse;
using UnityEngine;

namespace ViolentTakeoff
{
	public enum DestructionMode
	{
		// Really destroy things and floors (subject to the per-category toggles).
		Destroy,
		// Paint scorched ground over the swept area instead of destroying anything (pawns can still be killed).
		Scorch,
		// Effects only, the map is left as is.
		EffectsOnly,
	}

	public class Settings_ViolentTakeoff : ModSettings
	{
		public float ShockwaveSpeedCellsPerSecond = 12f;
		public float FireStartChancePerCell = 0.01f;
		public float ExplosionChancePerCell = 0.0015f;
		public float FireSize = 2f;
		public float VisualsFleckChance = 0.8f;
		public float FrameBudgetMs = 6f;
		public float BaseCellChance = 0.95f;
		public DestructionMode Mode = DestructionMode.Destroy;
		public bool FastBookkeeping = true;
		public bool SkipFilthFloors = true;
		public bool RemoveGrass = false;
		public bool DestroyItems = true;
		public bool DestroyBuildings = true;
		public bool KillPawns = true;
		public bool DestroyRoofholders = true;
		public bool AffectTerrain = true;

		private Vector2 scrollPosition;
		private float contentHeight = 1000f;

		public override void ExposeData()
		{
			Scribe_Values.Look(ref ShockwaveSpeedCellsPerSecond, "ShockwaveSpeed", 12f);
			Scribe_Values.Look(ref FireStartChancePerCell, "FireStartChancePerCell", 0.01f);
			Scribe_Values.Look(ref ExplosionChancePerCell, "ExplosionChancePerCell", 0.0015f);
			Scribe_Values.Look(ref FireSize, "FireSize", 2.0f);
			Scribe_Values.Look(ref VisualsFleckChance, "VisualsFleckChance", 0.8f);
			Scribe_Values.Look(ref FrameBudgetMs, "FrameBudgetMs", 6f);
			Scribe_Values.Look(ref BaseCellChance, "BaseCellCoverage", 0.95f);
			Scribe_Values.Look(ref Mode, "Mode", DestructionMode.Destroy);
			Scribe_Values.Look(ref FastBookkeeping, "FastBookkeeping", true);
			Scribe_Values.Look(ref SkipFilthFloors, "SkipFilthFloors", true);
			Scribe_Values.Look(ref RemoveGrass, "RemoveGrass", false);
			Scribe_Values.Look(ref DestroyItems, "DestroyItems", true);
			Scribe_Values.Look(ref DestroyBuildings, "DestroyBuildings", true);
			Scribe_Values.Look(ref KillPawns, "KillPawns", true);
			Scribe_Values.Look(ref DestroyRoofholders, "DestroyRoofholders", true);
			Scribe_Values.Look(ref AffectTerrain, "AffectTerrain", true);

			// Older versions had a "visuals only" checkbox; it is the EffectsOnly mode now. Read, never written.
			if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				bool legacyVisualsOnly = false;
				Scribe_Values.Look(ref legacyVisualsOnly, "VisualsOnly", false);
				if (legacyVisualsOnly) Mode = DestructionMode.EffectsOnly;
			}
		}

		private void ApplyPresetSlow()
		{
			Mode = DestructionMode.Scorch;
			FastBookkeeping = true;
			SkipFilthFloors = true;
			FrameBudgetMs = 3f;
			BaseCellChance = 0.8f;
			ShockwaveSpeedCellsPerSecond = 12f;
			VisualsFleckChance = 0.6f;
			FireStartChancePerCell = 0.005f;
			ExplosionChancePerCell = 0.0005f;
			FireSize = 1.0f;
			RemoveGrass = false;
			DestroyItems = false;
			DestroyBuildings = false;
			KillPawns = false;
			DestroyRoofholders = false;
			AffectTerrain = true;
		}

		private void ApplyPresetDefault()
		{
			Mode = DestructionMode.Destroy;
			FastBookkeeping = true;
			SkipFilthFloors = true;
			FrameBudgetMs = 6f;
			BaseCellChance = 0.95f;
			ShockwaveSpeedCellsPerSecond = 12f;
			VisualsFleckChance = 0.8f;
			FireStartChancePerCell = 0.01f;
			ExplosionChancePerCell = 0.0015f;
			FireSize = 2.0f;
			RemoveGrass = false;
			DestroyItems = true;
			DestroyBuildings = true;
			KillPawns = true;
			DestroyRoofholders = true;
			AffectTerrain = true;
		}

		private void ApplyPresetPowerful()
		{
			Mode = DestructionMode.Destroy;
			FastBookkeeping = true;
			SkipFilthFloors = false;
			FrameBudgetMs = 10f;
			BaseCellChance = 1f;
			ShockwaveSpeedCellsPerSecond = 12f;
			VisualsFleckChance = 0.7f;
			FireStartChancePerCell = 0.02f;
			ExplosionChancePerCell = 0.0025f;
			FireSize = 2.0f;
			RemoveGrass = true;
			DestroyItems = true;
			DestroyBuildings = true;
			KillPawns = true;
			DestroyRoofholders = true;
			AffectTerrain = true;
		}

		public void DoWindowContents(Rect inRect)
		{
			Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, contentHeight);
			Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
			var listing = new Listing_Standard();
			listing.Begin(viewRect);

			// Tip note
			listing.Label("VT_Settings_TipNote".Translate());

			// Preset buttons row
			Rect row = listing.GetRect(28f);
			float gap = 6f;
			float third = (row.width - 2 * gap) / 3f;
			Rect rSlow = new Rect(row.x, row.y, third, row.height);
			Rect rDefault = new Rect(rSlow.xMax + gap, row.y, third, row.height);
			Rect rFast = new Rect(rDefault.xMax + gap, row.y, third, row.height);
			if (Widgets.ButtonText(rSlow, "VT_Preset_Slow".Translate()))
			{
				ApplyPresetSlow();
			}
			if (Widgets.ButtonText(rDefault, "VT_Preset_Default".Translate()))
			{
				ApplyPresetDefault();
			}
			if (Widgets.ButtonText(rFast, "VT_Preset_Powerful".Translate()))
			{
				ApplyPresetPowerful();
			}

			listing.Gap(8f);

			listing.Label("VT_ShockwaveSpeed_Label".Translate(ShockwaveSpeedCellsPerSecond.ToString("0.#")), tooltip: "VT_ShockwaveSpeed_Tooltip".Translate());
			ShockwaveSpeedCellsPerSecond = Widgets.HorizontalSlider(listing.GetRect(22f), ShockwaveSpeedCellsPerSecond, 4f, 40f, roundTo: 0.5f);

			listing.Gap(6f);
			listing.Label("VT_FireStartChance_Label".Translate(FireStartChancePerCell.ToString("0.####")), tooltip: "VT_FireStartChance_Tooltip".Translate());
			FireStartChancePerCell = Widgets.HorizontalSlider(listing.GetRect(22f), FireStartChancePerCell, 0f, 0.2f);

			listing.Gap(6f);
			listing.Label("VT_ExplosionChance_Label".Translate(ExplosionChancePerCell.ToString("0.####")), tooltip: "VT_ExplosionChance_Tooltip".Translate());
			ExplosionChancePerCell = Widgets.HorizontalSlider(listing.GetRect(22f), ExplosionChancePerCell, 0f, 0.05f);

			listing.Gap(6f);
			listing.Label("VT_FireSize_Label".Translate(FireSize.ToString("0.##")), tooltip: "VT_FireSize_Tooltip".Translate());
			FireSize = Widgets.HorizontalSlider(listing.GetRect(22f), FireSize, 0.1f, 5f);

			listing.Gap(6f);
			listing.Label("VT_VisualsFleckChance_Label".Translate(VisualsFleckChance.ToString("0.##")), tooltip: "VT_VisualsFleckChance_Tooltip".Translate());
			VisualsFleckChance = Widgets.HorizontalSlider(listing.GetRect(22f), VisualsFleckChance, 0f, 1f);

			listing.Gap(6f);
			listing.Label("VT_BaseCellChance_Label".Translate(BaseCellChance.ToString("0.##")), tooltip: "VT_BaseCellChance_Tooltip".Translate());
			BaseCellChance = Widgets.HorizontalSlider(listing.GetRect(22f), BaseCellChance, 0f, 1f);

			listing.Gap(12f);
			listing.Label("VT_Mode_Label".Translate());
			if (listing.RadioButton("VT_Mode_Destroy".Translate(), Mode == DestructionMode.Destroy, 12f, "VT_Mode_Destroy_Tooltip".Translate()))
				Mode = DestructionMode.Destroy;
			if (listing.RadioButton("VT_Mode_Scorch".Translate(), Mode == DestructionMode.Scorch, 12f, "VT_Mode_Scorch_Tooltip".Translate()))
				Mode = DestructionMode.Scorch;
			if (listing.RadioButton("VT_Mode_EffectsOnly".Translate(), Mode == DestructionMode.EffectsOnly, 12f, "VT_Mode_EffectsOnly_Tooltip".Translate()))
				Mode = DestructionMode.EffectsOnly;

			if (Mode == DestructionMode.Destroy)
			{
				listing.Gap(6f);
				listing.Label("VT_FrameBudget_Label".Translate(FrameBudgetMs.ToString("0.#")), tooltip: "VT_FrameBudget_Tooltip".Translate());
				FrameBudgetMs = Widgets.HorizontalSlider(listing.GetRect(22f), FrameBudgetMs, 1f, 20f, roundTo: 0.5f);
				listing.CheckboxLabeled("VT_FastBookkeeping_Label".Translate(), ref FastBookkeeping, "VT_FastBookkeeping_Tooltip".Translate());
				listing.CheckboxLabeled("VT_SkipFilthFloors_Label".Translate(), ref SkipFilthFloors, "VT_SkipFilthFloors_Tooltip".Translate());
				listing.Gap(6f);
				listing.CheckboxLabeled("VT_RemoveGrass_Label".Translate(), ref RemoveGrass, "VT_RemoveGrass_Tooltip".Translate());
				listing.CheckboxLabeled("VT_DestroyItems_Label".Translate(), ref DestroyItems, "VT_DestroyItems_Tooltip".Translate());
				listing.CheckboxLabeled("VT_DestroyBuildings_Label".Translate(), ref DestroyBuildings, "VT_DestroyBuildings_Tooltip".Translate());
				listing.CheckboxLabeled("VT_DestroyRoofholders_Label".Translate(), ref DestroyRoofholders, "VT_DestroyRoofholders_Tooltip".Translate());
				listing.CheckboxLabeled("VT_AffectTerrain_Label".Translate(), ref AffectTerrain, "VT_AffectTerrain_Tooltip".Translate());
			}
			if (Mode != DestructionMode.EffectsOnly)
			{
				listing.CheckboxLabeled("VT_KillPawns_Label".Translate(), ref KillPawns, "VT_KillPawns_Tooltip".Translate());
			}

			contentHeight = listing.CurHeight + 12f;
			listing.End();
			Widgets.EndScrollView();
		}
	}
}
