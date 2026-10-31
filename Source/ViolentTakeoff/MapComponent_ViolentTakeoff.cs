using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ViolentTakeoff
{
	public class MapComponent_ViolentTakeoff : MapComponent
	{
		// A long hitch (e.g. the first frame after the takeoff long event) must not fling the wave across the map.
		private const float MaxFrameDelta = 0.1f;

		private readonly List<ShockwaveInstance> active = new List<ShockwaveInstance>();

		public MapComponent_ViolentTakeoff(Map map) : base(map)
		{
		}

		// MapComponentUpdate runs every frame even while the game is paused, unlike MapComponentTick.
		public override void MapComponentUpdate()
		{
			if (active.Count == 0) return;

			float deltaTime = Mathf.Min(Time.deltaTime, MaxFrameDelta);
			for (int i = active.Count - 1; i >= 0; i--)
			{
				if (active[i].Update(deltaTime))
				{
					active.RemoveAt(i);
				}
			}
		}

		public void StartShockwave(IntVec3 center)
		{
			map.fogGrid.ClearAllFog();
			active.Add(new ShockwaveInstance(center, map));
		}

		public void StopAll()
		{
			active.Clear();
		}

		public static MapComponent_ViolentTakeoff For(Map map)
		{
			return map?.GetComponent<MapComponent_ViolentTakeoff>();
		}
	}
}
