using Verse;
using UnityEngine;

namespace ViolentTakeoff
{
	public class Settings_ViolentTakeoff : ModSettings
	{
		public float ShockwaveSpeedCellsPerSecond = 22.5f;
		public float FireStartChancePerCell = 0.01f;
		public float ExplosionChancePerCell = 0.001f;
		public float FireSize = 0.6f;
		public float VisualsFleckChance = 0.8f;
		public int TickFrequency = 4;
		public float BaseCellChance = 0.55f;
		public bool RemoveGrass = true;
		public bool DestroyItems = true;
		public bool DestroyBuildings = true;
		public bool KillPawns = true;
		public bool VisualsOnly = false;
		public bool DestroyRoofholders = false;
		public bool AffectTerrain = true;

		public override void ExposeData()
		{
			Scribe_Values.Look(ref ShockwaveSpeedCellsPerSecond, "ShockwaveSpeedCellsPerSecond", 25.0f);
			Scribe_Values.Look(ref FireStartChancePerCell, "FireStartChancePerCell", 0.01f);
			Scribe_Values.Look(ref ExplosionChancePerCell, "ExplosionChancePerCell", 0.0015f);
			Scribe_Values.Look(ref FireSize, "FireSize", 2.0f);
			Scribe_Values.Look(ref VisualsFleckChance, "VisualsFleckChance", 0.8f);
			Scribe_Values.Look(ref TickFrequency, "TickFrequency", 2);
			Scribe_Values.Look(ref BaseCellChance, "BaseCellChance", 0.6f);
			Scribe_Values.Look(ref RemoveGrass, "RemoveGrass", false);
			Scribe_Values.Look(ref DestroyItems, "DestroyItems", true);
			Scribe_Values.Look(ref DestroyBuildings, "DestroyBuildings", true);
			Scribe_Values.Look(ref KillPawns, "KillPawns", true);
			Scribe_Values.Look(ref VisualsOnly, "VisualsOnly", false);
			Scribe_Values.Look(ref DestroyRoofholders, "DestroyRoofholders", true);
			Scribe_Values.Look(ref AffectTerrain, "AffectTerrain", true);
		}

		private void ApplyPresetSlow()
		{
			VisualsOnly = true;
			TickFrequency = 3;
			BaseCellChance = 0.5f;
			ShockwaveSpeedCellsPerSecond = 20.0f;
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
			VisualsOnly = false;
			TickFrequency = 2;
			BaseCellChance = 0.6f;
			ShockwaveSpeedCellsPerSecond = 25.0f;
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
			VisualsOnly = false;
			TickFrequency = 1;
			BaseCellChance = 0.8f;
			ShockwaveSpeedCellsPerSecond = 25.0f;
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
			var listing = new Listing_Standard();
			listing.Begin(inRect);

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

			listing.Label("VT_ShockwaveSpeed_Label".Translate(ShockwaveSpeedCellsPerSecond.ToString("0.###")), tooltip: "VT_ShockwaveSpeed_Tooltip".Translate());
			ShockwaveSpeedCellsPerSecond = Widgets.HorizontalSlider(listing.GetRect(22f), ShockwaveSpeedCellsPerSecond, 0.01f, 120f);

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
			listing.Label("VT_TickFrequency_Label".Translate(TickFrequency.ToString()), tooltip: "VT_TickFrequency_Tooltip".Translate());
			TickFrequency = (int)Widgets.HorizontalSlider(listing.GetRect(22f), TickFrequency, 1, 15);

			listing.Gap(6f);
			listing.Label("VT_BaseCellChance_Label".Translate(BaseCellChance.ToString("0.##")), tooltip: "VT_BaseCellChance_Tooltip".Translate());
			BaseCellChance = Widgets.HorizontalSlider(listing.GetRect(22f), BaseCellChance, 0f, 1f);

			listing.Gap(12f);
			listing.CheckboxLabeled("VT_VisualsOnly_Label".Translate(), ref VisualsOnly, "VT_VisualsOnly_Tooltip".Translate());

			if (!VisualsOnly)
			{
				listing.Gap(6f);
				listing.CheckboxLabeled("VT_RemoveGrass_Label".Translate(), ref RemoveGrass, "VT_RemoveGrass_Tooltip".Translate());
				listing.CheckboxLabeled("VT_DestroyItems_Label".Translate(), ref DestroyItems, "VT_DestroyItems_Tooltip".Translate());
				listing.CheckboxLabeled("VT_DestroyBuildings_Label".Translate(), ref DestroyBuildings, "VT_DestroyBuildings_Tooltip".Translate());
				listing.CheckboxLabeled("VT_KillPawns_Label".Translate(), ref KillPawns, "VT_KillPawns_Tooltip".Translate());
				listing.CheckboxLabeled("VT_DestroyRoofholders_Label".Translate(), ref DestroyRoofholders, "VT_DestroyRoofholders_Tooltip".Translate());
				listing.CheckboxLabeled("VT_AffectTerrain_Label".Translate(), ref AffectTerrain, "VT_AffectTerrain_Tooltip".Translate());
			}

			listing.End();
		}
	}
} 