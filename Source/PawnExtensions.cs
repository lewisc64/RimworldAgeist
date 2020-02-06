using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Ageist
{
    internal static class PawnExtensions
    {
        internal static BodyPartRecord GetBodyPart(this Pawn pawn, string bodyPartName)
        {
            return pawn.RaceProps.body.AllParts.FirstOrDefault(x => x.def == DefDatabase<BodyPartDef>.GetNamed(bodyPartName, true));
        }

        public static T GetHediffObject<T>(this Pawn pawn) where T : Hediff
        {
            return pawn.health.hediffSet.GetHediffs<T>().FirstOrDefault();
        }

        public static bool HasBionicPart(this Pawn pawn)
        {
            return GetHediffObject<Hediff_AddedPart>(pawn) != null;
        }

        public static IEnumerable<ThingDef> GetAllDrugsTaken(this Pawn pawn)
        {
            foreach (ThingDef drug in DefCollections.Drugs)
            {
                if (pawn.drugs.HasEverTaken(drug))
                {
                    yield return drug;
                }
            }
        }

        public static bool HasTakenAnyDrug(this Pawn pawn)
        {
            return pawn.GetAllDrugsTaken().Count() > 0;
        }

        public static Age GetGrowthStage(this Pawn pawn)
        {
            Hediff_HumanGrowth diff = GetHediffObject<Hediff_HumanGrowth>(pawn);
            return diff?.GetAge() ?? Age.Adult;
        }

        public static bool HasTrait(this Pawn pawn, string traitName)
        {
            return pawn.story.traits.HasTrait(TraitDef.Named(traitName));
        }

        public static void GiveTrait(this Pawn pawn, string traitName)
        {
            pawn.story.traits.GainTrait(new Trait(TraitDef.Named(traitName)));
        }

        public static bool HadThought(this Pawn pawn, ThoughtDef thought)
        {
            return pawn.needs.mood.thoughts.memories.Memories.Find(x => x.def == thought) != null;
        }
    }
}
