using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Verse;

namespace ViolentTakeoff
{
	public class MapComponent_ViolentTakeoff : MapComponent
	{
		// A long hitch (e.g. the first frame after the takeoff long event) must not fling the wave across the map.
		private const float MaxFrameDelta = 0.1f;

		private readonly List<ShockwaveInstance> active = new List<ShockwaveInstance>();
		// Stays drawn after the wave is done, until the map is gone.
		private ScorchOverlay scorch;
		private readonly EffectsRunner effects;

		public MapComponent_ViolentTakeoff(Map map) : base(map)
		{
			effects = new EffectsRunner(map);
		}

		// MapComponentUpdate runs every frame even while the game is paused, unlike MapComponentTick.
		public override void MapComponentUpdate()
		{
			float deltaTime = Mathf.Min(Time.deltaTime, MaxFrameDelta);
			scorch?.Draw();
			effects.Update(deltaTime);
			effects.Draw();
			if (active.Count == 0) return;

			stopwatch.Restart();
			for (int i = active.Count - 1; i >= 0; i--)
			{
				if (active[i].Update(deltaTime))
				{
					active[i].Finish();
					LogStats(active[i], final: true);
					active.RemoveAt(i);
				}
			}
			stopwatch.Stop();
			TrackStats(active.Count > 0 ? active[0] : null);
		}

		// Dev mode only: once per second, how long our update took vs. the whole frame, to tell our cost from the engine's.
		private readonly Stopwatch stopwatch = new Stopwatch();
		private int statFrames;
		private double statOwnMs, statOwnMaxMs;
		private float statFrameSecs, statFrameMaxSecs, statElapsed;

		private void TrackStats(ShockwaveInstance wave)
		{
			if (!Prefs.DevMode) return;
			double ownMs = stopwatch.Elapsed.TotalMilliseconds;
			statFrames++;
			statOwnMs += ownMs;
			statOwnMaxMs = Mathf.Max((float)statOwnMaxMs, (float)ownMs);
			statFrameSecs += Time.unscaledDeltaTime;
			statFrameMaxSecs = Mathf.Max(statFrameMaxSecs, Time.unscaledDeltaTime);
			if (statFrameSecs >= 1f && wave != null) LogStats(wave, final: false);
		}

		private void LogStats(ShockwaveInstance wave, bool final)
		{
			if (!Prefs.DevMode || statFrames == 0) return;
			statElapsed += statFrameSecs;
			Log.Message($"[ViolentTakeoff] t={statElapsed:0.0}s{(final ? " done" : "")} radius={wave.Radius:0} cells={wave.CellsDone}/{wave.CellsTotal} " +
				$"fps={statFrames / statFrameSecs:0} frame avg/max={statFrameSecs * 1000f / statFrames:0.0}/{statFrameMaxSecs * 1000f:0.0}ms " +
				$"ours avg/max={statOwnMs / statFrames:0.0}/{statOwnMaxMs:0.0}ms | {Profile.Report()}");
			statFrames = 0;
			statOwnMs = statOwnMaxMs = 0;
			statFrameSecs = statFrameMaxSecs = 0;
		}

		public void StartShockwave(IntVec3 center, CellRect? area = null, bool duringTakeoff = false)
		{
			map.fogGrid.ClearAllFog();
			ShockwaveEffects.ApplyTuning();
			if (Config.Mode == DestructionMode.Scorch && scorch == null) scorch = new ScorchOverlay(map);
			active.Add(new ShockwaveInstance(center, map, area, scorch, effects, duringTakeoff));
		}

		// Stops the waves; the scorch overlay (if any) stays drawn.
		public void StopAll()
		{
			foreach (ShockwaveInstance wave in active) wave.Finish();
			active.Clear();
		}

		public static MapComponent_ViolentTakeoff For(Map map)
		{
			return map?.GetComponent<MapComponent_ViolentTakeoff>();
		}
	}
}
