using System.Diagnostics;
using RimWorld;
using Verse;

namespace ViolentTakeoff
{
	// Profiling log (dev mode, "Profiling log" toggle in the settings): once per second while a wave runs, frame
	// times and our own cost split by category, to tell our work from the engine's. Off: no timers, no log.
	public static class Profile
	{
		public static bool Enabled;

		public static readonly Stopwatch Things = new Stopwatch();
		public static readonly Stopwatch Terrain = new Stopwatch();
		public static readonly Stopwatch Visuals = new Stopwatch();
		public static readonly Stopwatch Stash = new Stopwatch();
		public static int Plants, Buildings, RoofHolders, Items, Pawns, Other, TerrainChanged;

		// Read once per wave: the setting can not change while the settings window is closed.
		public static void Refresh() => Enabled = Prefs.DevMode && Config.ProfilingLog;

		public static void Start(Stopwatch watch)
		{
			if (Enabled) watch.Start();
		}

		public static void Stop(Stopwatch watch)
		{
			if (Enabled) watch.Stop();
		}

		public static void Count(Thing thing)
		{
			if (thing is Plant) Plants++;
			else if (thing is Pawn) Pawns++;
			else if (thing.def.category == ThingCategory.Building) { if (thing.def.holdsRoof) RoofHolders++; else Buildings++; }
			else if (thing.def.category == ThingCategory.Item) Items++;
			else Other++;
		}

		public static string Report()
		{
			string r = $"things={Things.Elapsed.TotalMilliseconds:0}ms terrain={Terrain.Elapsed.TotalMilliseconds:0}ms visuals={Visuals.Elapsed.TotalMilliseconds:0}ms stash={Stash.Elapsed.TotalMilliseconds:0}ms " +
				$"| plants={Plants} buildings={Buildings} roofholders={RoofHolders} items={Items} pawns={Pawns} other={Other} terrainChanged={TerrainChanged}";
			Things.Reset(); Terrain.Reset(); Visuals.Reset(); Stash.Reset();
			Plants = Buildings = RoofHolders = Items = Pawns = Other = TerrainChanged = 0;
			return r;
		}
	}
}
