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

		public MapComponent_ViolentTakeoff(Map map) : base(map)
		{
		}

		// MapComponentUpdate runs every frame even while the game is paused, unlike MapComponentTick.
		public override void MapComponentUpdate()
		{
			scorch?.Draw();
			if (active.Count == 0) return;

			float deltaTime = Mathf.Min(Time.deltaTime, MaxFrameDelta);
			stopwatch.Restart();
			for (int i = active.Count - 1; i >= 0; i--)
			{
				if (active[i].Update(deltaTime))
				{
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

		public void StartShockwave(IntVec3 center, CellRect? area = null)
		{
			map.fogGrid.ClearAllFog();
			if (Config.Mode == DestructionMode.Scorch && scorch == null) scorch = new ScorchOverlay(map);
			active.Add(new ShockwaveInstance(center, map, area, scorch));
		}

		public void StopAll()
		{
			active.Clear();
			scorch = null;
		}

		public static MapComponent_ViolentTakeoff For(Map map)
		{
			return map?.GetComponent<MapComponent_ViolentTakeoff>();
		}
	}
}
