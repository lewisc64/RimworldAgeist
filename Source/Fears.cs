using Harmony;
using RimWorld;
using System;
using Verse;

namespace Ageist
{
    public enum Fear
    {
        Dark,
        Thunder,
        Gunfire,
    }

    public abstract class ThoughtWorker_FearBase : ThoughtWorker
    {
        protected virtual Fear FearType => throw new NotImplementedException();

        protected virtual Age[] AgeRange => new[] { Age.Toddler, Age.Child };

        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            Hediff_HumanGrowth diff = Utils.GetHediffObject<Hediff_HumanGrowth>(pawn);
            if (diff == null)
            {
                return false;
            }

            Age age = diff.GetAge();

            if (age < AgeRange[0] || age > AgeRange[1])
            {
                return false;
            }

            diff.RegisterFear(FearType);
            if (diff.HasFear(FearType))
            {
                return ShouldGiveThought(pawn);
            }
            return false;
        }

        protected abstract bool ShouldGiveThought(Pawn pawn);
    }

    public class ThoughtWorker_FearOfTheDark : ThoughtWorker_FearBase
    {
        protected override Fear FearType => Fear.Dark;

        protected override bool ShouldGiveThought(Pawn pawn)
        {
            return pawn.Awake() && pawn.needs.mood.recentMemory.TicksSinceLastLight > 1200;
        }
    }

    public class ThoughtWorker_FearOfThunder : ThoughtWorker_FearBase
    {
        protected override Fear FearType => Fear.Thunder;

        protected override bool ShouldGiveThought(Pawn pawn)
        {
            if (pawn.Map.weatherManager.curWeatherAge > 1500)
            {
                foreach (string defName in new[] { "DryThunderstorm", "RainyThunderstorm" })
                {
                    if (pawn.Map.weatherManager.curWeather == WeatherDef.Named(defName))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    public class ThoughtWorker_FearOfGunfire : ThoughtWorker_FearBase
    {
        const int maxRepeatBuffer = 18;

        protected override Fear FearType => Fear.Gunfire;
        
        private int repeatBuffer = 0;

        protected override bool ShouldGiveThought(Pawn pawn)
        {
            if (pawn.TargetCurrentlyAimingAt != null)
            {
                return false;
            }

            bool found = false;
            foreach (Pawn other in pawn.Map.mapPawns.AllPawns)
            {
                if (other.NonHumanlikeOrWildMan())
                {
                    continue;
                }
                if (other.TargetCurrentlyAimingAt != null)
                {
                    found = true;
                    if (repeatBuffer < maxRepeatBuffer)
                    {
                        repeatBuffer++;
                    }
                }
            }
            if (!found)
            {
                repeatBuffer--;
            }
            return repeatBuffer > 6;
        }
    }
}
