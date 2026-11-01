using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace ViolentTakeoff
{
	// Every despawn removes the thing from the map's lister lists with List.Remove: a linear search plus an array
	// shift over lists that hold every thing on the map. Destroying thousands of things that way dominates the cost.
	// While a destruction batch runs, the big lists are moved aside and left empty (Remove becomes trivial); after
	// the batch they get back every entry that is still spawned, plus whatever was added meanwhile.
	// Lists that the game reads during the batch look empty for its duration; nothing ticks during the cutscene.
	public class ListerStash
	{
		// Smaller lists are cheap to search; copying them would cost more than it saves.
		private const int MinListSize = 256;

		private static readonly AccessTools.FieldRef<ListerThings, List<Thing>[]> ListsByGroup =
			AccessTools.FieldRefAccess<ListerThings, List<Thing>[]>("listsByGroup");
		private static readonly AccessTools.FieldRef<ListerThings, Dictionary<ThingDef, List<Thing>>> ListsByDef =
			AccessTools.FieldRefAccess<ListerThings, Dictionary<ThingDef, List<Thing>>>("listsByDef");

		private readonly Map map;
		private readonly List<List<Thing>> thingLists = new List<List<Thing>>();
		private readonly List<List<Thing>> thingStashes = new List<List<Thing>>();
		private readonly List<List<Building>> buildingLists = new List<List<Building>>();
		private readonly List<List<Building>> buildingStashes = new List<List<Building>>();
		private readonly List<Thing> tmpAddedThings = new List<Thing>();
		private readonly List<Building> tmpAddedBuildings = new List<Building>();

		public bool Active { get; private set; }

		public ListerStash(Map map)
		{
			this.map = map;
		}

		public void Begin()
		{
			if (Active) return;
			Active = true;
			foreach (List<Thing> list in ListsByGroup(map.listerThings)) StashThings(list);
			foreach (List<Thing> list in ListsByDef(map.listerThings).Values) StashThings(list);
			StashBuildings(map.listerBuildings.allBuildingsColonist);
			StashBuildings(map.listerBuildings.allBuildingsNonColonist);
		}

		public void End()
		{
			if (!Active) return;
			Active = false;
			for (int i = 0; i < thingLists.Count; i++)
			{
				Restore(thingLists[i], thingStashes[i], tmpAddedThings);
			}
			for (int i = 0; i < buildingLists.Count; i++)
			{
				Restore(buildingLists[i], buildingStashes[i], tmpAddedBuildings);
			}
			thingLists.Clear();
			buildingLists.Clear();
		}

		private void StashThings(List<Thing> list)
		{
			if (list == null || list.Count < MinListSize) return;
			if (thingStashes.Count <= thingLists.Count) thingStashes.Add(new List<Thing>());
			List<Thing> stash = thingStashes[thingLists.Count];
			stash.Clear();
			stash.AddRange(list);
			list.Clear();
			thingLists.Add(list);
		}

		private void StashBuildings(List<Building> list)
		{
			if (list == null || list.Count < MinListSize) return;
			if (buildingStashes.Count <= buildingLists.Count) buildingStashes.Add(new List<Building>());
			List<Building> stash = buildingStashes[buildingLists.Count];
			stash.Clear();
			stash.AddRange(list);
			list.Clear();
			buildingLists.Add(list);
		}

		private void Restore<T>(List<T> list, List<T> stash, List<T> tmpAdded) where T : Thing
		{
			tmpAdded.Clear();
			tmpAdded.AddRange(list);
			list.Clear();
			foreach (T thing in stash)
			{
				if (thing.Spawned && thing.Map == map) list.Add(thing);
			}
			list.AddRange(tmpAdded);
			tmpAdded.Clear();
			stash.Clear();
		}
	}
}
