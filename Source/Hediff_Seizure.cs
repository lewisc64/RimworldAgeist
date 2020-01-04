using RimWorld;
using Verse;

namespace Ageist
{
    class Hediff_Seizure : HediffWithComps
    {
        private const int injuries = 2;
        private const float fallDamage = 1.0f;

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            for (int i = 0; i < injuries; i++)
            {
                pawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, fallDamage));
            }
        }
    }
}
