using Verse;
using RimWorld;
using System.Collections.Generic;

namespace Ageist
{
    class Hediff_HumanPregnancy : HediffWithComps
    {
        // Either assign randomly to baby, or use parents. Do before HumanGrowth is added.
        public static TraitDef[] geneticTraits = new[]
        {
            TraitDefOf.SpeedOffset,
            TraitDefOf.PsychicSensitivity,
            TraitDefOf.Beauty,
            TraitDefOf.AnnoyingVoice,
            TraitDefOf.CreepyBreathing,
        };

        private const int minWaterBreakDays = 35;
        private const int maxWaterBreakDays = 45;
        private const int normalWaterBreakDays = 40;

        private const int earlyStageDays = 13;
        private const int midStageDays = 26;
        private const int lateStageDays = 40;

        private bool initialized = false;
        private int startAge;
        private int accumulatedRisk;
        private int daysUntilWaterBreak;

        private int PawnAgeDays
        {
            get
            {
                return (int)(pawn.ageTracker.AgeBiologicalTicks / 60000);
            }
        }

        private int DaysActive
        {
            get
            {
                return PawnAgeDays - startAge;
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref initialized, "initialized", false);
            Scribe_Values.Look(ref startAge, "startAge");
            Scribe_Values.Look(ref accumulatedRisk, "accumulatedRisk");
            Scribe_Values.Look(ref daysUntilWaterBreak, "daysUntilWaterBreak");
            base.ExposeData();
        }

        private void Initialize()
        {
            startAge = PawnAgeDays;
            accumulatedRisk = 0;
            daysUntilWaterBreak = Rand.RangeInclusive(minWaterBreakDays, maxWaterBreakDays);
            initialized = true;
        }

        private void Update()
        {
            if (!initialized)
            {
                Initialize();
            }

            if (DaysActive > daysUntilWaterBreak)
            {
                Severity = 0.5f;
            }
            else if (DaysActive < earlyStageDays)
            {
                Severity = 0.1f;
            }
            else if (DaysActive < midStageDays)
            {
                Severity = 0.2f;
            }
            else if (DaysActive < lateStageDays)
            {
                Severity = 0.3f;
            }
            else
            {
                Severity = 0.4f;
            }
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            Update();
        }

        public override void PostMake()
        {
            base.PostMake();
            Update();
        }

        public override void Tick()
        {
            base.Tick();
            if (pawn.IsHashIntervalTick(2500))
            {
                Update();
            }
        }
    }
}
