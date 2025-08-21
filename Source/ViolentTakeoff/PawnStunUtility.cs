using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ViolentTakeoff
{
	public static class PawnStunUtility
	{
		public static void FreezeColonists(int ticks)
		{
			List<Pawn> pawns = PawnsFinder.AllMaps_FreeColonists;
			if (pawns == null || pawns.Count == 0) return;

			for (int i = 0; i < pawns.Count; i++)
			{
				Pawn pawn = pawns[i];
				if (pawn?.stances?.stunner != null)
				{
					pawn.stances.stunner.StunFor(ticks: 600, instigator: null, addBattleLog: false, showMote: false);
				}
			}
		}
	}
} 