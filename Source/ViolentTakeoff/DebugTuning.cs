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
		public float FlashSolid = 0.1f, FlashFade = 0.6f;
		public float FlashR = 1f, FlashG = 0.84f, FlashB = 0.35f, FlashBrightness = 1.34f;
		// Rim: one short, bright flash per cell as the front reaches it (color: ring color whitened by RimWhiten).
		public float RimChance = 1f, RimScaleMin = 1f, RimScaleMax = 1.6f, RimLife = 0.15f, RimBrightness = 2.5f, RimWhiten = 0.5f;
		// When the ring runs slower than its nominal speed, flashes live up to this many times longer so the band
		// keeps the same width on screen.
		public float MaxStretch = 4f;
		// Dust puffs thrown with a flash.
		public float DustChance = 0.5f, DustScale = 1.2f, DustFade = 1f, DustBrightness = 1f;
		// Explosions: center flash, colored blast per cell, smoke.
		public float ExplosionRadius = 5.5f, ExplosionFlashScale = 6f, ExplosionFlashBrightness = 1.5f;
		public float BlastR = 1f, BlastG = 0.5f, BlastB = 0.3f, BlastBrightness = 1.39f, BlastFade = 0.29f;
		public float ExplosionSmokeCount = 4f;
		// Each explosion's radius is the average times 1 +- spread; the blast expands from the center at this speed.
		public float ExplosionSizeSpread = 0.4f, ExplosionSpeed = 40f;
		// Flickering flames left in the wake (vanilla fire textures): up to this many per fire, living this long.
		public float FlamesPerFire = 3f, FlameScale = 1f, FlameLifeMin = 6f, FlameLifeMax = 14f;
		// Flames (fire glow) and their smoke.
		public float FireScaleMin = 1.3f, FireScaleMax = 1.9f, FireSolid = 2f, FireFade = 1.5f, FireBrightness = 1f;
		public float SmokeBrightness = 1f;
		// Walls (roof holders): always destroyed up to WallFullRadius, then the chance falls linearly to
		// WallEdgeChance at the map edge (half the map size from the center).
		public float WallFullRadius = 25f, WallEdgeChance = 0.3f;
		// Scorch mode.
		public float ScorchBrightness = 0.5f, AshChance = 0.5f, AshAlpha = 0.85f;
		// Camera shake during the cutscene (vanilla: 0.2).
		public float ShakeMagnitude = 0.04f;

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
			Add("Ring rim", nameof(RimChance), "chance per cell", 0f, 1f, t => t.RimChance, (t, v) => t.RimChance = v);
			Add("Ring rim", nameof(RimScaleMin), "scale min", 0.2f, 6f, t => t.RimScaleMin, (t, v) => t.RimScaleMin = v);
			Add("Ring rim", nameof(RimScaleMax), "scale max", 0.2f, 6f, t => t.RimScaleMax, (t, v) => t.RimScaleMax = v);
			Add("Ring rim", nameof(RimLife), "life (s)", 0.02f, 1f, t => t.RimLife, (t, v) => t.RimLife = v);
			Add("Ring rim", nameof(RimBrightness), "brightness", 0f, 5f, t => t.RimBrightness, (t, v) => t.RimBrightness = v);
			Add("Ring rim", nameof(RimWhiten), "whiten (0 = ring color, 1 = white)", 0f, 1f, t => t.RimWhiten, (t, v) => t.RimWhiten = v);
			Add("Ring rim", nameof(MaxStretch), "max life stretch when the ring slows", 1f, 8f, t => t.MaxStretch, (t, v) => t.MaxStretch = v);
			Add("Dust", nameof(DustChance), "chance per flash", 0f, 1f, t => t.DustChance, (t, v) => t.DustChance = v);
			Add("Dust", nameof(DustScale), "scale", 0.2f, 4f, t => t.DustScale, (t, v) => t.DustScale = v);
			Add("Dust", nameof(DustFade), "fade time (s)", 0.1f, 4f, t => t.DustFade, (t, v) => t.DustFade = v);
			Add("Dust", nameof(DustBrightness), "brightness", 0f, 2f, t => t.DustBrightness, (t, v) => t.DustBrightness = v);
			Add("Explosions", nameof(ExplosionRadius), "average radius", 1f, 12f, t => t.ExplosionRadius, (t, v) => t.ExplosionRadius = v);
			Add("Explosions", nameof(ExplosionSizeSpread), "radius spread (+-, fraction of average)", 0f, 1f, t => t.ExplosionSizeSpread, (t, v) => t.ExplosionSizeSpread = v);
			Add("Explosions", nameof(ExplosionSpeed), "expansion speed (cells/s)", 2f, 120f, t => t.ExplosionSpeed, (t, v) => t.ExplosionSpeed = v);
			Add("Explosions", nameof(ExplosionFlashScale), "center flash scale (x radius)", 0f, 12f, t => t.ExplosionFlashScale, (t, v) => t.ExplosionFlashScale = v);
			Add("Explosions", nameof(ExplosionFlashBrightness), "center flash brightness", 0f, 3f, t => t.ExplosionFlashBrightness, (t, v) => t.ExplosionFlashBrightness = v);
			Add("Explosions", nameof(BlastR), "blast red", 0f, 1f, t => t.BlastR, (t, v) => t.BlastR = v);
			Add("Explosions", nameof(BlastG), "blast green", 0f, 1f, t => t.BlastG, (t, v) => t.BlastG = v);
			Add("Explosions", nameof(BlastB), "blast blue", 0f, 1f, t => t.BlastB, (t, v) => t.BlastB = v);
			Add("Explosions", nameof(BlastBrightness), "blast brightness", 0f, 3f, t => t.BlastBrightness, (t, v) => t.BlastBrightness = v);
			Add("Explosions", nameof(BlastFade), "blast fade time (s)", 0f, 2f, t => t.BlastFade, (t, v) => t.BlastFade = v);
			Add("Explosions", nameof(ExplosionSmokeCount), "smoke puffs", 0f, 12f, t => t.ExplosionSmokeCount, (t, v) => t.ExplosionSmokeCount = v);
			Add("Flames", nameof(FlamesPerFire), "flames per fire (max)", 1f, 9f, t => t.FlamesPerFire, (t, v) => t.FlamesPerFire = v);
			Add("Flames", nameof(FlameScale), "flame size multiplier", 0.2f, 3f, t => t.FlameScale, (t, v) => t.FlameScale = v);
			Add("Flames", nameof(FlameLifeMin), "flame life min (s)", 0.5f, 30f, t => t.FlameLifeMin, (t, v) => t.FlameLifeMin = v);
			Add("Flames", nameof(FlameLifeMax), "flame life max (s)", 0.5f, 30f, t => t.FlameLifeMax, (t, v) => t.FlameLifeMax = v);
			Add("Flames", nameof(FireScaleMin), "glow scale min (x fire size)", 0.1f, 4f, t => t.FireScaleMin, (t, v) => t.FireScaleMin = v);
			Add("Flames", nameof(FireScaleMax), "glow scale max (x fire size)", 0.1f, 4f, t => t.FireScaleMax, (t, v) => t.FireScaleMax = v);
			Add("Flames", nameof(FireSolid), "glow solid time (s)", 0f, 8f, t => t.FireSolid, (t, v) => t.FireSolid = v);
			Add("Flames", nameof(FireFade), "glow fade time (s)", 0f, 4f, t => t.FireFade, (t, v) => t.FireFade = v);
			Add("Flames", nameof(FireBrightness), "glow brightness", 0f, 3f, t => t.FireBrightness, (t, v) => t.FireBrightness = v);
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
		public Color RimColor => Color.Lerp(new Color(FlashR, FlashG, FlashB), Color.white, RimWhiten) * RimBrightness;
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
			listing.Gap(6f);
			listing.Label("Look tuning. Applies to the next wave.");
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
