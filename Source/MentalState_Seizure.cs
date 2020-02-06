using Verse;
using Verse.AI;

namespace Ageist
{
    public class MentalState_Seizure : MentalState
    {
        public override void PostStart(string reason)
        {
            base.PostStart(reason);
            RecoverFromState();
            pawn.health.AddHediff(HediffDef.Named("Seizure"), pawn.GetBodyPart("Brain"));
        }
    }
}
