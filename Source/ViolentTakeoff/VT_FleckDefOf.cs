using RimWorld;
using Verse;

namespace ViolentTakeoff
{
	[DefOf]
	public static class VT_FleckDefOf
	{
		public static FleckDef VT_ExplosionFlash;
		public static FleckDef VT_BlastDry;
		public static FleckDef VT_DustPuffThick;
		public static FleckDef VT_Smoke;
		public static FleckDef VT_FireGlow;
		public static FleckDef VT_ShockwaveRing;

		static VT_FleckDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(VT_FleckDefOf));
		}
	}
}
