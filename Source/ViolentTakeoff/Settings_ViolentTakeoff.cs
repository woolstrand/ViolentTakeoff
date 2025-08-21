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
			listing.Label("Note: Hover over a setting name to see a tooltip with details.");

			// Preset buttons row
			Rect row = listing.GetRect(28f);
			float gap = 6f;
			float third = (row.width - 2 * gap) / 3f;
			Rect rSlow = new Rect(row.x, row.y, third, row.height);
			Rect rDefault = new Rect(rSlow.xMax + gap, row.y, third, row.height);
			Rect rFast = new Rect(rDefault.xMax + gap, row.y, third, row.height);
			if (Widgets.ButtonText(rSlow, "Slow computer preset"))
			{
				ApplyPresetSlow();
			}
			if (Widgets.ButtonText(rDefault, "Default preset"))
			{
				ApplyPresetDefault();
			}
			if (Widgets.ButtonText(rFast, "Powerful computer preset"))
			{
				ApplyPresetPowerful();
			}

			listing.Gap(8f);

			listing.Label("Shockwave speed (cells per second): " + ShockwaveSpeedCellsPerSecond.ToString("0.###"), tooltip: "How many cells the shockwave expands in one second.");
			ShockwaveSpeedCellsPerSecond = Widgets.HorizontalSlider(listing.GetRect(22f), ShockwaveSpeedCellsPerSecond, 0.01f, 120f);

			listing.Gap(6f);
			listing.Label("Fire start chance per cell: " + FireStartChancePerCell.ToString("0.####"), tooltip: "Chance to start a fire in an affected cell.");
			FireStartChancePerCell = Widgets.HorizontalSlider(listing.GetRect(22f), FireStartChancePerCell, 0f, 0.2f);

			listing.Gap(6f);
			listing.Label("Explosion chance per cell: " + ExplosionChancePerCell.ToString("0.####"), tooltip: "Chance to spawn a small explosion in an affected cell.");
			ExplosionChancePerCell = Widgets.HorizontalSlider(listing.GetRect(22f), ExplosionChancePerCell, 0f, 0.05f);

			listing.Gap(6f);
			listing.Label("Fire size: " + FireSize.ToString("0.##"), tooltip: "Size of fires created by the shockwave.");
			FireSize = Widgets.HorizontalSlider(listing.GetRect(22f), FireSize, 0.1f, 5f);

			listing.Gap(6f);
			listing.Label("Visual effects chance: " + VisualsFleckChance.ToString("0.##"), tooltip: "Chance to display a flash on each processed cell.");
			VisualsFleckChance = Widgets.HorizontalSlider(listing.GetRect(22f), VisualsFleckChance, 0f, 1f);

			listing.Gap(6f);
			listing.Label("Update frequency divider: " + TickFrequency, tooltip: "Higher values update less often (faster). For example, value of 10 means a single updates happens once every 10 ticks.");
			TickFrequency = (int)Widgets.HorizontalSlider(listing.GetRect(22f), TickFrequency, 1, 15);

			listing.Gap(6f);
			listing.Label("Base cell processing chance: " + BaseCellChance.ToString("0.##"), tooltip: "Controls how many neighboring cells are affected to fill gaps from ring rasterization. Higher values look fuller but cost more performance.");
			BaseCellChance = Widgets.HorizontalSlider(listing.GetRect(22f), BaseCellChance, 0f, 1f);

			listing.Gap(12f);
			listing.CheckboxLabeled("Visuals only", ref VisualsOnly, "Skip all destruction and show only visual effects. Recommended for slow computers.");

			if (!VisualsOnly)
			{
				listing.Gap(6f);
				listing.CheckboxLabeled("Remove grass", ref RemoveGrass, "Removes plants and grass in affected cells. Can significantly slow down in lush biomes.");
				listing.CheckboxLabeled("Destroy items", ref DestroyItems, "Destroy items in affected cells.");
				listing.CheckboxLabeled("Destroy buildings", ref DestroyBuildings, "Destroy buildings in affected cells.");
				listing.CheckboxLabeled("Kill pawns", ref KillPawns, "Kill pawns in affected cells.");
				listing.CheckboxLabeled("Destroy roofholders", ref DestroyRoofholders, "Also destroy roof-holding buildings. This can trigger roof collapse calculations and slow the animation.");
				listing.CheckboxLabeled("Affect terrain", ref AffectTerrain, "Removes floors or bridges, destroys orbital platforms.");
			}

			listing.End();
		}
	}
} 