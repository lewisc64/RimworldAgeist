using Harmony;
using System.Reflection;
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