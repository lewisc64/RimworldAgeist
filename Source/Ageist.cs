using Harmony;
using RimWorld;
using System.Reflection;
using System.Linq;
using Verse;

namespace Ageist
{
    public enum Age
    {
        Baby,
        Toddler,
        Child,
        Teenager,
        Adult,
    }

    [StaticConstructorOnStartup]
    public class Ageist
    {
        static Ageist()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.Ageist");
            HarmonyInstance.DEBUG = true;
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Logger.Info("Loaded.");
        }
    }

    [HarmonyPatch(typeof(PawnGenerator))]
    [HarmonyPatch(nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) })]
    public static class GeneratePawn_Patch
    {
        [HarmonyPostfix]
        internal static void GeneratePawn(ref PawnGenerationRequest request, ref Pawn __result)
        {
            Pawn pawn = __result;

            if (pawn.RaceProps.Humanlike && pawn.ageTracker.CurLifeStageIndex <= (int)Age.Teenager) {
                pawn.health.AddHediff(HediffDef.Named(Hediff_HumanGrowth.HediffName));
            }
        }
    }

    public static class Utils
    {
        private static ThingDef[] drugs =
        {
            ThingDef.Named("Beer"),
            ThingDef.Named("Ambrosia"),
            ThingDef.Named("GoJuice"),
            ThingDef.Named("Luciferium"),
            ThingDef.Named("Penoxycyline"),
            ThingDef.Named("Flake"),
            ThingDef.Named("PsychiteTea"),
            ThingDef.Named("WakeUp"),
            ThingDef.Named("SmokeleafJoint"),
        };

        internal static BodyPartRecord GetBodyPart(Pawn pawn, string bodyPart)
        {
            return pawn.RaceProps.body.AllParts.FirstOrDefault(x => x.def == DefDatabase<BodyPartDef>.GetNamed(bodyPart, true));
        }

        public static T GetHediffObject<T>(Pawn pawn) where T : Hediff
        {
            return pawn.health.hediffSet.GetHediffs<T>().FirstOrDefault();
        }

        public static bool HasBionicPart(Pawn pawn)
        {
            return GetHediffObject<Hediff_AddedPart>(pawn) != null;
        }

        public static bool HasTakenAnyDrug(Pawn pawn)
        {
            foreach (ThingDef drug in drugs)
            {
                if (pawn.drugs.HasEverTaken(drug))
                {
                    return true;
                }
            }
            return false;
        }

        public static Age GetGrowthStage(Pawn pawn)
        {
            Hediff_HumanGrowth diff = GetHediffObject<Hediff_HumanGrowth>(pawn);
            return diff?.GetAge() ?? Age.Adult;
        }

        public static bool HasTrait(Pawn pawn, string traitName)
        {
            return pawn.story.traits.HasTrait(TraitDef.Named(traitName));
        }

        public static void GiveTrait(Pawn pawn, string traitName)
        {
            pawn.story.traits.GainTrait(new Trait(TraitDef.Named(traitName)));
        }

        public static bool HadThought(Pawn pawn, ThoughtDef thought)
        {
            return pawn.needs.mood.thoughts.memories.Memories.Find(x => x.def == thought) != null;
        }
    }

    public static class Logger
    {
        private const string format = "[Ageist] {0}";

        private static string FormatMessage(string message)
        {
            return string.Format(format, message);
        }

        public static void Debug(string message)
        {
            Info(message);
        }

        public static void Info(string message)
        {
            Log.Message(FormatMessage(message));
        }

        public static void Warning(string message)
        {
            Log.Warning(FormatMessage(message));
        }

        public static void Error(string message)
        {
            Log.Error(FormatMessage(message));
        }
    }
}