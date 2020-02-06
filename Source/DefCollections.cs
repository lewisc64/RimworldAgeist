using RimWorld;
using System.Reflection;
using System.Linq;
using Verse;
using System.Collections.Generic;

namespace Ageist
{
    public static class DefCollections
    {
        public static ThingDef[] Drugs => new[]
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

        public static TraitDef[] Traits
        {
            get
            {
                List<TraitDef> allDefs = typeof(TraitDefOf)
                    .GetFields(BindingFlags.Static | BindingFlags.Public)
                    .Select(x => x.GetValue(null))
                    .Cast<TraitDef>()
                    .Where(x => x != null)
                    .ToList();

                // why aren't these in TraitDefOf?
                allDefs.Add(TraitDef.Named("FastLearner"));
                allDefs.Add(TraitDef.Named("Nimble"));
                allDefs.Add(TraitDef.Named("Masochist"));
                allDefs.Add(TraitDef.Named("NightOwl"));
                allDefs.Add(TraitDef.Named("Jealous"));
                allDefs.Add(TraitDef.Named("Wimp"));
                allDefs.Add(TraitDef.Named("Gourmand"));
                allDefs.Add(TraitDef.Named("QuickSleeper"));
                allDefs.Add(TraitDef.Named("Neurotic"));
                allDefs.Add(TraitDef.Named("Immunity"));

                return allDefs.Distinct().ToArray();
            }
        }
    }
}