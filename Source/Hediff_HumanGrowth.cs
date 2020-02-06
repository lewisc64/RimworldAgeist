using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;

namespace Ageist
{
    internal class ChildhoodFeat
    {
        private readonly string traitName;
        private readonly string success;
        private readonly string failure;

        public ChildhoodFeat(string traitName, string success, string failure)
        {
            this.traitName = traitName;
            this.success = success;
            this.failure = failure;
        }

        public string GetDescription(Pawn pawn)
        {
            if (pawn.HasTrait(traitName))
            {
                return success;
            }
            return failure;
        }
    }

    internal class ChildhoodDescriptionBuilder
    {
        private List<ChildhoodFeat> feats;

        public ChildhoodDescriptionBuilder()
        {
            feats = new List<ChildhoodFeat>();
        }

        public void AddFeat(ChildhoodFeat feat)
        {
            feats.Add(feat);
        }

        public string BuildDescription(Pawn pawn, string separator = " ")
        {
            StringBuilder output = new StringBuilder();
            foreach (ChildhoodFeat feat in feats)
            {
                string desc = feat.GetDescription(pawn);
                if (desc != null)
                {
                    output.Append(desc);
                    output.Append(separator);
                }
            }
            return output.ToString();
        }
    }

    internal class QueuedPassion : IExposable
    {
        private int activationAge; // ticks
        private bool applied;

        public QueuedPassion()
        {
            activationAge = 0;
            applied = false;
        }

        public QueuedPassion(int activationAge)
        {
            this.activationAge = activationAge;
            applied = false;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref activationAge, "activationAge");
            Scribe_Values.Look(ref applied, "applied");
        }

        public void Apply(Pawn pawn)
        {
            Dictionary<SkillRecord, float> weights = new Dictionary<SkillRecord, float>();

            foreach (SkillRecord skill in pawn.skills.skills)
            {
                weights[skill] = skill.Level;
            }

            int tries = 0;
            SkillRecord increaseSkill = null;
            while ((increaseSkill == null || increaseSkill.passion == Passion.Major) && ++tries < 300)
            {
                increaseSkill = pawn.skills.skills.RandomElementByWeight((skill) => weights[skill]);
            }

            if (tries >= 300)
            {
                Logger.Error($"failed to add passion to {pawn.Name}, skipping passion.");
                return;
            }

            increaseSkill.passion = (Passion)((int)increaseSkill.passion + 1);

            Messages.Message("MessageChildGainPassion".Translate(pawn.Name.ToStringShort, increaseSkill.def.skillLabel), pawn, MessageTypeDefOf.PositiveEvent);
            Logger.Info($"{pawn.Name} gained a passion in {increaseSkill.def.skillLabel}.");
        }

        public void Update(Pawn pawn)
        {
            if (!applied)
            {
                if (pawn.ageTracker.AgeBiologicalTicks >= activationAge && pawn.Awake() && pawn.MentalStateDef == null)
                {
                    Apply(pawn);
                    applied = true;
                }
            }
        }
    }

    /// <summary>
    /// Removes backstories and traits.
    /// Adds "current" backstories.
    /// Cripples babies, toddlers, and chilren. Gives them the wimp trait, which may be removed when they grow up.
    /// </summary>
    public class Hediff_HumanGrowth : HediffWithComps
    {
        public const string HediffName = "HumanGrowth";

        public override bool Visible => true;

        private bool loaded = false; // do not scribe
        private bool initOnPawn = false;
        private int _currentStage = -1;
        private Age ageTracker = Age.Adult; // lags behind CurrentStage, used to apply growths once only.
        private int skillLevelTracker = 0;

        private List<QueuedPassion> queuedPassions = new List<QueuedPassion>();

        private List<Fear> knownFears = new List<Fear>();
        private List<Fear> fears = new List<Fear>();

        private int CurrentStage
        {
            get
            {
                return _currentStage == -1 ? (int)Age.Adult : _currentStage;
            }
            set
            {
                if (_currentStage == value && loaded)
                {
                    return;
                }

                _currentStage = value;

                switch ((Age)_currentStage)
                {
                    case Age.Baby:
                        Severity = 0.1f;
                        break;
                    case Age.Toddler:
                        Severity = 0.25f;
                        break;
                    case Age.Child:
                        Severity = 0.50f;
                        break;
                    case Age.Teenager:
                        Severity = 0.75f;
                        break;
                    default:
                        Severity = 1.0f;
                        break;
                }

                PortraitsCache.SetDirty(pawn);
                LongEventHandler.ExecuteWhenFinished(() => {
                    pawn.Drawer.renderer.graphics.ResolveAllGraphics();
                });
                loaded = true;
            }
        }

        private int TotalSkillValue
        {
            get
            {
                var total = 0;
                foreach (var skill in pawn.skills.skills)
                {
                    total += skill.Level;
                }
                return total;
            }
        }

        public void RegisterFear(Fear fear, int percentChance = 50)
        {
            try
            {
                if (knownFears.Contains(fear))
                {
                    return;
                }
                if (Rand.RangeInclusive(1, 100) <= percentChance)
                {
                    fears.Add(fear);
                }
                knownFears.Add(fear);
            }
            catch (NullReferenceException)
            {
                Logger.Error($"{pawn.Name} NullReferenceException while trying to register fear. Initialising lists.");
                knownFears = new List<Fear>();
                fears = new List<Fear>();
            }
        }

        public void ForgetFears()
        {
            try
            {
                knownFears.Clear();
                fears.Clear();
            }
            catch (NullReferenceException)
            {
                Logger.Error($"{pawn.Name} NullReferenceException while trying to forget fears. Initialising lists.");
                knownFears = new List<Fear>();
                fears = new List<Fear>();
            }
        }

        public bool HasFear(Fear fear)
        {
            return fears.Contains(fear);
        }

        public Age GetAge()
        {
            return ageTracker;
        }

        /// <summary>
        /// What did the child do during their childhood? How has it shaped who they are now?
        /// </summary>
        private void ChildhoodRetrospective(int traits, bool ignoreGenetic = true)
        {
            Logger.Debug($"performing childhood retrospective on {pawn.Name}, trying to increase amount of traits to {traits}.");
            Backstory backstory = new Backstory();
            backstory.SetTitle("Child", "Child");
            backstory.SetTitleShort("child", "child");
            backstory.baseDesc = "";

            List<Trait> potentialTraits = new List<Trait>();
            ChildhoodDescriptionBuilder description = new ChildhoodDescriptionBuilder();

            float humanKills = pawn.records.GetValue(RecordDefOf.KillsHumanlikes);
            float animalKills = pawn.records.GetValue(RecordDefOf.KillsAnimals);
            float animalsTamed = pawn.records.GetValue(RecordDefOf.AnimalsTamed);
            bool ateHumanMeat = pawn.HadThought(ThoughtDefOf.AteHumanlikeMeatAsIngredient);

            string preferredName = pawn.Name.ToStringShort; // name should remain childhood name, no translation.

            if (CurrentStage > (int)Age.Child)
            {
                if (humanKills >= pawn.ageTracker.AgeBiologicalYears / 3 && !pawn.HasTrait("Bloodlust"))
                {
                    potentialTraits.Add(new Trait(TraitDefOf.Bloodlust));
                    description.AddFeat(new ChildhoodFeat(
                        "Bloodlust",
                        $"{preferredName} struck many humans down. Perhaps it was out of necessity, but deep down, {preferredName} enjoyed it.",
                        $"{preferredName} struck many humans down, but only did what was necessary."));
                }

                if (pawn.records.GetValue(RecordDefOf.TimesInMentalState) >= pawn.ageTracker.AgeBiologicalYears)
                {
                    if (!pawn.HasTrait("Nerves"))
                    {
                        potentialTraits.Add(new Trait(TraitDefOf.Nerves, PawnGenerator.RandomTraitDegree(TraitDefOf.Nerves)));
                    }
                    if (!pawn.HasTrait("Neurotic"))
                    {
                        TraitDef def = TraitDef.Named("Neurotic");
                        potentialTraits.Add(new Trait(def, PawnGenerator.RandomTraitDegree(def)));
                    }
                    if (!pawn.HasTrait("Psychopath"))
                    {
                        potentialTraits.Add(new Trait(TraitDefOf.Psychopath));
                        description.AddFeat(new ChildhoodFeat(
                            "Psychopath",
                            $"{preferredName} experienced life on a knife's edge, and their mental state suffered for it.",
                            $"{preferredName} didn't have healthiest upbringing, but pushed through relatively unscathed."));
                    }
                }

                if (pawn.records.GetValue(RecordDefOf.CellsMined) > 10 && !pawn.HasTrait("Undergrounder"))
                {
                    potentialTraits.Add(new Trait(TraitDefOf.Undergrounder));
                }

                if (pawn.records.GetValue(RecordDefOf.ThingsHauled) >= pawn.ageTracker.AgeBiologicalTicks / 60000 * 2 && !pawn.HasTrait("Tough"))
                {
                    description.AddFeat(new ChildhoodFeat(
                        "Tough",
                        $"Long days of hard work toughened {preferredName}, and made them strong.",
                        null));
                    potentialTraits.Add(new Trait(TraitDefOf.Tough));
                }

                if (ateHumanMeat && !pawn.HasTrait("Cannibal"))
                {
                    potentialTraits.Add(new Trait(TraitDefOf.Cannibal));
                    description.AddFeat(new ChildhoodFeat(
                        "Cannibal",
                        $"{preferredName} had a taste of human meat, and enjoyed it.",
                        null));
                }
            }

            if (CurrentStage == (int)Age.Teenager)
            {
                if (pawn.records.GetValue(RecordDefOf.FiresExtinguished) > 0)
                {
                    potentialTraits.Add(new Trait(TraitDefOf.Pyromaniac));
                    description.AddFeat(new ChildhoodFeat(
                        "Pyromaniac",
                        $"{preferredName} experienced fire, and enjoyed the flickering heat consuming everything they gave it.",
                        null));
                }
            }

            if (pawn.GetAllDrugsTaken().Count() >= 3)
            {
                potentialTraits.Add(new Trait(TraitDefOf.DrugDesire, PawnGenerator.RandomTraitDegree(TraitDefOf.DrugDesire)));
            }

            List<TraitDef> invalidExtras = new List<TraitDef> {
                TraitDefOf.Bloodlust,
                TraitDefOf.Psychopath,
                TraitDefOf.Cannibal,
            };

            invalidExtras.AddRange(pawn.story.traits.allTraits.Select(x => x.def));
            invalidExtras.AddRange(potentialTraits.Select(x => x.def));
            if (ignoreGenetic)
            {
                invalidExtras.AddRange(Hediff_HumanPregnancy.geneticTraits);
            }

            if (pawn.HasBionicPart())
            {
                invalidExtras.Add(TraitDefOf.BodyPurist); // this would be really annoying otherwise.
            }

            if (pawn.records.GetValue(RecordDefOf.TimesOnFire) > 0)
            {
                invalidExtras.Add(TraitDefOf.Pyromaniac);
            }

            if (pawn.records.GetValue(RecordDefOf.ShotsFired) == 0 || pawn.records.GetValue(RecordDefOf.DamageDealt) == 0)
            {
                invalidExtras.Add(TraitDefOf.ShootingAccuracy);
            }

            if (pawn.records.GetValue(RecordDefOf.DamageDealt) == 0 || pawn.records.GetValue(RecordDefOf.ShotsFired) > 0)
            {
                invalidExtras.Add(TraitDefOf.Brawler);
            }

            if (!pawn.HasTakenAnyDrug())
            {
                invalidExtras.Add(TraitDefOf.DrugDesire);
            }

            TraitDef[] allDefs = DefCollections.Traits.Where(x => !invalidExtras.Contains(x)).ToArray();

            // add random traits to the pool for variety.
            int oldCount = potentialTraits.Count;
            while (potentialTraits.Count  < oldCount * 2 || potentialTraits.Count < 2)
            {
                Trait trait = null;

                int times = 0;
                while (++times < 300)
                {
                    TraitDef def = allDefs.RandomElement();
                    trait = new Trait(def, PawnGenerator.RandomTraitDegree(def));
                    bool conflicts = false;
                    foreach (Trait potential in potentialTraits)
                    {
                        if (trait.def == potential.def || trait.def.ConflictsWith(potential))
                        {
                            conflicts = true;
                            break;
                        }
                    }
                    if (!conflicts)
                    {
                        break;
                    }
                }
                if (trait == null || times >= 300)
                {
                    Logger.Error($"tried 300 times to add extra trait to {pawn.Name} childhood retrospective.");
                    break;
                }

                potentialTraits.Add(trait);
            }

            int amount = 0;

            while (pawn.story.traits.allTraits.Count < traits)
            {
                Trait trait = potentialTraits.RandomElement();
                while (potentialTraits.Contains(trait))
                {
                    potentialTraits.Remove(trait);
                }
                pawn.story.traits.GainTrait(trait);
                amount++;
            }

            Logger.Debug($"added {amount} new traits");

            backstory.baseDesc += $"\n\n{description.BuildDescription(pawn, "\n\n")}";
            pawn.story.childhood = backstory;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref _currentStage, "_currentStage");
            Scribe_Values.Look(ref ageTracker, "ageTracker");
            Scribe_Values.Look(ref skillLevelTracker, "skillLevelTracker");
            Scribe_Values.Look(ref initOnPawn, "initOnPawn");
            Scribe_Collections.Look(ref fears, "fears", LookMode.Value);
            Scribe_Collections.Look(ref knownFears, "knownFears", LookMode.Value);
            Scribe_Collections.Look(ref queuedPassions, "queuedPassions", LookMode.Deep);
            base.ExposeData();
        }

        public override bool ShouldRemove { 
            get
            {
                if (base.ShouldRemove)
                {
                    return true;
                }
                return ageTracker == Age.Adult;
            }
        }

        private void InitOnPawn()
        {
            Logger.Debug($"initializing on pawn {pawn.Name}");

            pawn.story.adulthood = null;
            if (CurrentStage <= (int)Age.Toddler)
            {
                // do not touch skills assigned by game on older pawns.
                foreach (SkillRecord skill in pawn.skills.skills)
                {
                    skill.Level = 0;
                }
            }
            if (CurrentStage <= (int)Age.Child)
            {
                pawn.story.traits.allTraits.Clear();
                pawn.HasTrait("Wimp");
            }
            else if (CurrentStage <= (int)Age.Teenager)
            {
                pawn.story.childhood = null;
                while (pawn.story.traits.allTraits.Count > 2)
                {
                    pawn.story.traits.allTraits.Pop();
                }
            }

            foreach (SkillRecord skill in pawn.skills.skills)
            {
                skill.passion = Passion.None;
            }

            knownFears = new List<Fear>();
            fears = new List<Fear>();
            queuedPassions = new List<QueuedPassion>();
        }

        private void UpdateAge()
        {
            if (pawn.ageTracker.AgeBiologicalYears < 1)
            {
                CurrentStage = (int)Age.Baby;
            }
            else if (pawn.ageTracker.AgeBiologicalYears < 4)
            {
                CurrentStage = (int)Age.Toddler;
            }
            else
            {
                CurrentStage = pawn.ageTracker.CurLifeStageIndex;
            }

            if (!initOnPawn && loaded)
            {
                InitOnPawn();
                initOnPawn = true;
            }

            Age age = (Age)CurrentStage;

            if (ageTracker != age)
            {
                switch (age)
                {
                    case Age.Baby:
                        break;
                    case Age.Toddler:
                        break;
                    case Age.Child:
                        ChildhoodRetrospective(2);
                        break;
                    case Age.Teenager:
                        if (pawn.HasTrait("Wimp"))
                        {
                            pawn.story.traits.allTraits.Remove(pawn.story.traits.allTraits.First(x => x.def.defName == "Wimp"));
                        }
                        else
                        {
                            Logger.Warning($"{pawn.Name} advanced to teenager, but there was no wimp trait to remove.");
                        }
                        ForgetFears();
                        ChildhoodRetrospective(2);
                        break;
                    default:
                        ChildhoodRetrospective(3);
                        Logger.Debug($"{pawn.Name} has grown up. flagging HumanGrowth for removal.");
                        break;
                }
                ageTracker = age;
            }
        }

        private void UpdatePassions()
        {
            if (TotalSkillValue != skillLevelTracker)
            {
                var diff = TotalSkillValue - skillLevelTracker;
                for (int i = 0; i < diff; i++)
                {
                    if (Rand.RangeInclusive(1, 7) == 1)
                    {
                        queuedPassions.Add(new QueuedPassion());
                    }
                }
                skillLevelTracker += diff;
            }

            foreach (QueuedPassion queuedPassion in queuedPassions)
            {
                queuedPassion.Update(pawn);
            }
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            UpdateAge();
        }

        public override void PostMake()
        {
            base.PostMake();
            UpdateAge();
        }

        public override void Tick()
        {
            base.Tick();
            UpdateAge();
            UpdatePassions();
        }
    }
}
