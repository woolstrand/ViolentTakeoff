using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Verse;

namespace ViolentTakeoff
{
	// Look-and-feel knobs. Saved with the mod settings, editable only in dev mode (a block at the bottom of the
	// settings window), so they can stay in the release build without cluttering the normal settings.
	public class DebugTuning : IExposable
	{
		private class Knob
		{
			public string section, key, label;
			public float min, max, defaultValue;
			public Func<DebugTuning, float> get;
			public Action<DebugTuning, float> set;
		}

		// Ring flashes (additive glow): color times brightness, alpha fades them over solid + fade seconds.
		public float FlashScaleMin = 1.3f, FlashScaleMax = 2f;
		public float FlashSolid = 0.12f, FlashFade = 0.35f;
		public float FlashR = 1f, FlashG = 0.92f, FlashB = 0.02f, FlashBrightness = 1f;
		// Dust puffs thrown with a flash.
		public float DustChance = 0.5f, DustScale = 1.2f, DustFade = 1f, DustBrightness = 1f;
		// Explosions: center flash, colored blast per cell, smoke.
		public float ExplosionRadius = 3.5f, ExplosionFlashScale = 6f, ExplosionFlashBrightness = 1f;
		public float BlastR = 1f, BlastG = 0.5f, BlastB = 0.3f, BlastBrightness = 1f, BlastFade = 0.3f;
		public float ExplosionSmokeCount = 4f;
		// Flames (fire glow) and their smoke.
		public float FireScaleMin = 1f, FireScaleMax = 1.5f, FireSolid = 2f, FireFade = 1.5f, FireBrightness = 1f;
		public float SmokeBrightness = 1f;
		// Walls (roof holders): always destroyed up to WallFullRadius, then the chance falls linearly to
		// WallEdgeChance at the map edge (half the map size from the center).
		public float WallFullRadius = 25f, WallEdgeChance = 0.3f;
		// Scorch mode.
		public float ScorchBrightness = 0.5f, AshChance = 0.35f, AshAlpha = 0.85f;
		// Camera shake during the cutscene (vanilla: 0.2).
		public float ShakeMagnitude = 0.02f;

		private static readonly List<Knob> Knobs = new List<Knob>();

		static DebugTuning()
		{
			Add("Ring flashes", nameof(FlashScaleMin), "scale min", 0.2f, 6f, t => t.FlashScaleMin, (t, v) => t.FlashScaleMin = v);
			Add("Ring flashes", nameof(FlashScaleMax), "scale max", 0.2f, 6f, t => t.FlashScaleMax, (t, v) => t.FlashScaleMax = v);
			Add("Ring flashes", nameof(FlashSolid), "solid time (s)", 0f, 1f, t => t.FlashSolid, (t, v) => t.FlashSolid = v);
			Add("Ring flashes", nameof(FlashFade), "fade time (s)", 0f, 2f, t => t.FlashFade, (t, v) => t.FlashFade = v);
			Add("Ring flashes", nameof(FlashR), "red", 0f, 1f, t => t.FlashR, (t, v) => t.FlashR = v);
			Add("Ring flashes", nameof(FlashG), "green", 0f, 1f, t => t.FlashG, (t, v) => t.FlashG = v);
			Add("Ring flashes", nameof(FlashB), "blue", 0f, 1f, t => t.FlashB, (t, v) => t.FlashB = v);
			Add("Ring flashes", nameof(FlashBrightness), "brightness", 0f, 3f, t => t.FlashBrightness, (t, v) => t.FlashBrightness = v);
			Add("Dust", nameof(DustChance), "chance per flash", 0f, 1f, t => t.DustChance, (t, v) => t.DustChance = v);
			Add("Dust", nameof(DustScale), "scale", 0.2f, 4f, t => t.DustScale, (t, v) => t.DustScale = v);
			Add("Dust", nameof(DustFade), "fade time (s)", 0.1f, 4f, t => t.DustFade, (t, v) => t.DustFade = v);
			Add("Dust", nameof(DustBrightness), "brightness", 0f, 2f, t => t.DustBrightness, (t, v) => t.DustBrightness = v);
			Add("Explosions", nameof(ExplosionRadius), "radius", 1f, 8f, t => t.ExplosionRadius, (t, v) => t.ExplosionRadius = v);
			Add("Explosions", nameof(ExplosionFlashScale), "center flash scale (x radius)", 0f, 12f, t => t.ExplosionFlashScale, (t, v) => t.ExplosionFlashScale = v);
			Add("Explosions", nameof(ExplosionFlashBrightness), "center flash brightness", 0f, 3f, t => t.ExplosionFlashBrightness, (t, v) => t.ExplosionFlashBrightness = v);
			Add("Explosions", nameof(BlastR), "blast red", 0f, 1f, t => t.BlastR, (t, v) => t.BlastR = v);
			Add("Explosions", nameof(BlastG), "blast green", 0f, 1f, t => t.BlastG, (t, v) => t.BlastG = v);
			Add("Explosions", nameof(BlastB), "blast blue", 0f, 1f, t => t.BlastB, (t, v) => t.BlastB = v);
			Add("Explosions", nameof(BlastBrightness), "blast brightness", 0f, 3f, t => t.BlastBrightness, (t, v) => t.BlastBrightness = v);
			Add("Explosions", nameof(BlastFade), "blast fade time (s)", 0f, 2f, t => t.BlastFade, (t, v) => t.BlastFade = v);
			Add("Explosions", nameof(ExplosionSmokeCount), "smoke puffs", 0f, 12f, t => t.ExplosionSmokeCount, (t, v) => t.ExplosionSmokeCount = v);
			Add("Flames", nameof(FireScaleMin), "scale min (x fire size)", 0.1f, 4f, t => t.FireScaleMin, (t, v) => t.FireScaleMin = v);
			Add("Flames", nameof(FireScaleMax), "scale max (x fire size)", 0.1f, 4f, t => t.FireScaleMax, (t, v) => t.FireScaleMax = v);
			Add("Flames", nameof(FireSolid), "solid time (s)", 0f, 8f, t => t.FireSolid, (t, v) => t.FireSolid = v);
			Add("Flames", nameof(FireFade), "fade time (s)", 0f, 4f, t => t.FireFade, (t, v) => t.FireFade = v);
			Add("Flames", nameof(FireBrightness), "brightness", 0f, 3f, t => t.FireBrightness, (t, v) => t.FireBrightness = v);
			Add("Flames", nameof(SmokeBrightness), "smoke brightness", 0f, 2f, t => t.SmokeBrightness, (t, v) => t.SmokeBrightness = v);
			Add("Walls", nameof(WallFullRadius), "always destroyed within (cells)", 0f, 150f, t => t.WallFullRadius, (t, v) => t.WallFullRadius = v);
			Add("Walls", nameof(WallEdgeChance), "chance at map edge", 0f, 1f, t => t.WallEdgeChance, (t, v) => t.WallEdgeChance = v);
			Add("Scorch", nameof(ScorchBrightness), "ground brightness", 0f, 1f, t => t.ScorchBrightness, (t, v) => t.ScorchBrightness = v);
			Add("Scorch", nameof(AshChance), "ash chance", 0f, 1f, t => t.AshChance, (t, v) => t.AshChance = v);
			Add("Scorch", nameof(AshAlpha), "ash opacity", 0f, 1f, t => t.AshAlpha, (t, v) => t.AshAlpha = v);
			Add("Camera", nameof(ShakeMagnitude), "cutscene shake", 0f, 0.2f, t => t.ShakeMagnitude, (t, v) => t.ShakeMagnitude = v);
		}

		private static void Add(string section, string key, string label, float min, float max, Func<DebugTuning, float> get, Action<DebugTuning, float> set)
		{
			Knobs.Add(new Knob { section = section, key = key, label = label, min = min, max = max, get = get, set = set, defaultValue = get(new DebugTuning()) });
		}

		public Color FlashColor => new Color(FlashR, FlashG, FlashB) * FlashBrightness;
		public Color BlastColorCenter => new Color(BlastR, BlastG, BlastB) * BlastBrightness;
		// Same falloff toward grey as vanilla's bomb (edge color (0.6, 0.5, 0.4) vs center (1, 0.5, 0.3)).
		public Color BlastColorEdge => new Color(0.6f, 0.5f, 0.4f) * BlastBrightness;

		public float WallChance(float distance, Map map)
		{
			float edge = Mathf.Min(map.Size.x, map.Size.z) / 2f;
			if (distance <= WallFullRadius || edge <= WallFullRadius) return 1f;
			return Mathf.Lerp(1f, WallEdgeChance, Mathf.InverseLerp(WallFullRadius, edge, distance));
		}

		public void ExposeData()
		{
			foreach (Knob knob in Knobs)
			{
				float value = knob.get(this);
				Scribe_Values.Look(ref value, knob.key, knob.defaultValue);
				knob.set(this, value);
			}
		}

		public void Reset()
		{
			foreach (Knob knob in Knobs) knob.set(this, knob.defaultValue);
		}

		public string Describe()
		{
			var sb = new StringBuilder("[ViolentTakeoff] tuning:");
			foreach (Knob knob in Knobs)
			{
				sb.Append(' ').Append(knob.key).Append('=').Append(knob.get(this).ToString("0.###", CultureInfo.InvariantCulture));
			}
			return sb.ToString();
		}

		public void DoSettings(Listing_Standard listing)
		{
			listing.GapLine();
			listing.Label("Debug tuning (dev mode only). Applies to the next takeoff.");
			Rect row = listing.GetRect(28f);
			if (Widgets.ButtonText(row.LeftHalf().ContractedBy(2f), "Reset tuning")) Reset();
			if (Widgets.ButtonText(row.RightHalf().ContractedBy(2f), "Log values")) Log.Message(Describe());

			string section = null;
			foreach (Knob knob in Knobs)
			{
				if (knob.section != section)
				{
					section = knob.section;
					listing.Gap(6f);
					listing.Label(section);
				}
				float value = knob.get(this);
				listing.Label($"  {knob.label}: {value:0.###}");
				float newValue = Widgets.HorizontalSlider(listing.GetRect(18f), value, knob.min, knob.max);
				// Only write back real changes: drawing alone must never alter a value.
				if (newValue != value) knob.set(this, newValue);
			}
			listing.Gap(6f);
			listing.Label("Ring flash color preview:");
			Widgets.DrawBoxSolid(listing.GetRect(18f).LeftHalf(), FlashColor);
		}
	}
}
