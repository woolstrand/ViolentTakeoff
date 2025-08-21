using System.Collections.Generic;
using Verse;

namespace ViolentTakeoff
{
	public class MapComponent_ViolentTakeoff : MapComponent
	{
		private readonly List<ShockwaveInstance> active = new List<ShockwaveInstance>();
		private int tickCount = 0;

		public MapComponent_ViolentTakeoff(Map map) : base(map)
		{
		}

		public override void MapComponentTick()
		{
			tickCount += 1;
			if (tickCount % Config.TickFrequency != 0) { return; }

			for (int i = active.Count - 1; i >= 0; i--)
			{
				bool finished = active[i].TickOnce();
				if (finished)
				{
					active.RemoveAt(i);
				}
			}

			if (active.Count == 0)
			{
				map.components.Remove(this);
			}
		}

		public void StartShockwave(IntVec3 center)
		{
			// Stun colonists for 10 seconds (600 ticks) at shockwave start
			PawnStunUtility.FreezeColonists(600);
			map.fogGrid.ClearAllFog();
			active.Add(new ShockwaveInstance(center, map));
		}

		public void StopAll()
		{
			active.Clear();
		}

		public static MapComponent_ViolentTakeoff For(Map map)
		{
			if (map == null) return null;
			return map.GetComponent<MapComponent_ViolentTakeoff>();
		}

		public static MapComponent_ViolentTakeoff Ensure(Map map)
		{
			if (map == null) return null;
			var comp = map.GetComponent<MapComponent_ViolentTakeoff>();
			if (comp != null) return comp;
			comp = new MapComponent_ViolentTakeoff(map);
			map.components.Add(comp);
			return comp;
		}
	}
} 