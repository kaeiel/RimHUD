﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimHUD.Interface;
using RimHUD.Interface.HUD;
using RimHUD.Patch;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace RimHUD.Data.Models
{
    internal class PawnModel
    {
        public static PawnModel Selected => GetSelected();

        public Pawn Base { get; }

        public HudTarget Target => GetTargetType();

        public string Name => Base.GetName();
        public TextModel GenderAndAge => GetGenderAndAge();

        public bool IsPlayerFaction => Base.Faction?.IsPlayer ?? false;
        public bool IsPlayerManaged => (Base.Faction?.IsPlayer ?? false) || (Base.HostFaction?.IsPlayer ?? false);

        private Color? _factionRelationColor;
        public Color FactionRelationColor => _factionRelationColor ?? (_factionRelationColor = GetFactionRelationColor()).Value;
        public TextModel RelationKindAndFaction => GetRelationKindAndFaction();

        public PawnHealthModel Health { get; }
        public PawnMindModel Mind { get; }

        public bool IsAnimal => Base.RaceProps.Animal;
        public TextModel Master => GetMaster();

        public bool HasNeeds => Base.needs != null;
        public NeedModel Rest => GetNeedModel(NeedDefOf.Rest);
        public NeedModel Food => GetNeedModel(NeedDefOf.Food);
        public NeedModel Recreation => GetNeedModel(NeedDefOf.Joy);
        public NeedModel Mood => GetNeedModel(Access.NeedDefOfMood);
        public float MoodThresholdMinor => Base.mindState?.mentalBreaker?.BreakThresholdMinor ?? -1f;
        public float MoodThresholdMajor => Base.mindState?.mentalBreaker?.BreakThresholdMajor ?? -1f;
        public float MoodThresholdExtreme => Base.mindState?.mentalBreaker?.BreakThresholdExtreme ?? -1f;

        public bool HasSkills => Base.skills != null;
        public SkillModel Shooting => GetSkillModel(SkillDefOf.Shooting);
        public SkillModel Melee => GetSkillModel(SkillDefOf.Melee);
        public SkillModel Construction => GetSkillModel(SkillDefOf.Construction);
        public SkillModel Mining => GetSkillModel(SkillDefOf.Mining);
        public SkillModel Cooking => GetSkillModel(SkillDefOf.Cooking);
        public SkillModel Plants => GetSkillModel(SkillDefOf.Plants);
        public SkillModel Animals => GetSkillModel(SkillDefOf.Animals);
        public SkillModel Crafting => GetSkillModel(SkillDefOf.Crafting);
        public SkillModel Artistic => GetSkillModel(SkillDefOf.Artistic);
        public SkillModel Medicine => GetSkillModel(SkillDefOf.Medicine);
        public SkillModel Social => GetSkillModel(SkillDefOf.Social);
        public SkillModel Intellectual => GetSkillModel(SkillDefOf.Intellectual);

        public bool HasTraining => Base.training != null;
        public TrainingModel Tameness => GetTrainingModel(TrainableDefOf.Tameness);
        public TrainingModel Obedience => GetTrainingModel(TrainableDefOf.Obedience);
        public TrainingModel Release => GetTrainingModel(TrainableDefOf.Release);
        public TrainingModel Rescue => GetTrainingModel(Access.TrainableDefOfRescue);
        public TrainingModel Haul => GetTrainingModel(Access.TrainableDefOfHaul);

        public string Activity => GetActivity();
        public string Queued => GetQueued();

        public string Equipped => GetEquipped();
        public string Carrying => GetCarrying();

        public string CompInfo => GetCompInfo();

        public TipSignal? BioTooltip => GetBioTooltip();
        public TipSignal? AnimalTooltip => GetAnimalTooltip();

        public SelectorModel OutfitSelector => new OutfitModel(this);
        public SelectorModel FoodSelector => new FoodModel(this);
        public SelectorModel TimetableSelector => new TimetableModel(this);
        public SelectorModel AreaSelector => new AreaModel(this);

        private PawnModel(Pawn pawn)
        {
            Base = pawn;
            Health = new PawnHealthModel(this);
            Mind = new PawnMindModel(this);
        }

        private static PawnModel GetSelected()
        {
            var selected = State.SelectedPawn;
            return selected == null ? null : new PawnModel(selected);
        }

        private HudTarget GetTargetType()
        {
            if (IsPlayerFaction) { return IsAnimal ? HudTarget.PlayerAnimal : HudTarget.PlayerColonist; }
            return IsAnimal ? HudTarget.OtherAnimal : HudTarget.OtherColonist;
        }

        private NeedModel GetNeedModel(NeedDef def) => new NeedModel(this, def);
        private SkillModel GetSkillModel(SkillDef def) => new SkillModel(this, def);
        private TrainingModel GetTrainingModel(TrainableDef def) => new TrainingModel(this, def);

        private string GetGender() => Base.gender == Gender.None ? null : Base.GetGenderLabel().CapitalizeFirst() + " ";
        private TextModel GetGenderAndAge()
        {
            var gender = GetGender();
            var race = gender == null ? Base.kindDef.race.LabelCap : Base.kindDef.race.label;

            Base.ageTracker.AgeBiologicalTicks.TicksToPeriod(out var years, out var quadrums, out var days, out _);
            var ageDays = (quadrums * GenDate.DaysPerQuadrum) + days;

            var age = years.ToString().Bold();
            if ((ageDays == 0) || (ageDays == GenDate.DaysPerYear)) { age += Lang.Get("Age.Birthday"); }
            else if (ageDays == 1) { age += Lang.Get("Age.Day"); }
            else { age += Lang.Get("Age.Days", ageDays); }

            return TextModel.Create(Lang.Get("GenderAndAge", gender, race, age), GetBioTooltip(), FactionRelationColor);
        }

        private string GetKind()
        {
            if (!IsAnimal) { return Base.Faction == Faction.OfPlayer ? Base.story?.Title ?? Base.KindLabel : Base.TraderKind?.label ?? Base.KindLabel; }

            if (Base.Faction == null)
            {
                if (Base.RaceProps.petness > 0.5f) { return Lang.Get("Creature.Stray"); }
                if (Base.RaceProps.predator) { return Lang.Get("Creature.Predator"); }
                if (Base.kindDef.race.tradeTags?.Contains("AnimalInsect") ?? false) { return Lang.Get("Creature.Insect"); }
                return Lang.Get("Creature.Wild");
            }

            if (Base.RaceProps.petness > 0.5f) { return Lang.Get("Creature.Pet"); }
            if (Base.RaceProps.petness > 0f) { return Lang.Get("Creature.ExoticPet"); }
            if (Base.RaceProps.predator) { return Lang.Get("Creature.Hunt"); }
            if (Base.RaceProps.packAnimal) { return Lang.Get("Creature.Pack"); }
            if (Base.kindDef.race.tradeTags?.Contains("AnimalFarm") ?? false) { return Lang.Get("Creature.Farm"); }
            if (Base.RaceProps.herdAnimal) { return Lang.Get("Creature.Herd"); }
            if (Base.kindDef.race.tradeTags?.Contains("AnimalInsect") ?? false) { return Lang.Get("Creature.Insect"); }

            return Lang.Get("Creature.Tame");
        }

        private Color GetFactionRelationColor()
        {
            if (Base.Faction == null) { return IsAnimal ? Theme.FactionWildColor.Value : Theme.FactionIndependentColor.Value; }
            if (Base.Faction.IsPlayer) { return Theme.FactionOwnColor.Value; }

            if (Base.Faction.PlayerRelationKind == FactionRelationKind.Hostile) { return Theme.FactionHostileColor.Value; }
            return Base.Faction.PlayerRelationKind == FactionRelationKind.Ally ? Theme.FactionAlliedColor.Value : Theme.FactionIndependentColor.Value;
        }

        private string GetFactionRelation()
        {
            if (Base.Faction == null) { return IsAnimal ? (Base.kindDef == PawnKindDefOf.WildMan ? null : Lang.Get("Faction.Wild")) : Lang.Get("Faction.Independent"); }
            if (Base.Faction.IsPlayer) { return null; }

            var relation = Base.Faction.PlayerRelationKind;
            if (relation == FactionRelationKind.Hostile) { return Base.RaceProps.IsMechanoid ? Lang.Get("Faction.Hostile") : Lang.Get("Faction.Enemy"); }
            return relation == FactionRelationKind.Ally ? Lang.Get("Faction.Allied") : null;
        }

        private TextModel GetRelationKindAndFaction()
        {
            var faction = (Base.Faction == null) || !Base.Faction.HasName ? null : Lang.Get("OfFaction", Base.Faction.Name);
            var relation = GetFactionRelation();
            var kind = GetKind();

            return TextModel.Create(Lang.Get("RelationKindAndFaction", relation, relation == null ? kind.CapitalizeFirst() : kind, faction), GetBioTooltip(), FactionRelationColor);
        }

        private string GetActivity()
        {
            var lord = Base.GetLord()?.LordJob?.GetReport()?.CapitalizeFirst();
            var jobText = Base.jobs?.curDriver?.GetReport()?.TrimEnd('.').CapitalizeFirst();

            var target = (Base.jobs?.curJob?.def == JobDefOf.AttackStatic) || (Base.jobs?.curJob?.def == JobDefOf.Wait_Combat) ? Base.TargetCurrentlyAimingAt.Thing?.LabelCap : null;
            var activity = target == null ? lord.NullOrEmpty() ? jobText : $"{lord} ({jobText})" : Lang.Get("Info.Attacking", target);

            return activity == null ? null : Lang.Get("Info.Activity", activity.Bold());
        }

        private string GetQueued()
        {
            if ((Base.jobs?.curJob == null) || (Base.jobs.jobQueue.Count == 0)) { return null; }

            var queued = Base.jobs.jobQueue[0].job.GetReport(Base)?.TrimEnd('.').CapitalizeFirst().Bold();
            var remaining = Base.jobs.jobQueue.Count - 1;
            if (remaining > 0) { queued += $" (+{remaining})"; }

            return queued == null ? null : Lang.Get("Info.Queued", queued);
        }

        private string GetCarrying()
        {
            var carried = Base.carryTracker?.CarriedThing?.LabelCap;
            return carried == null ? null : Lang.Get("Info.Carrying", carried.Bold());
        }

        private string GetEquipped()
        {
            var equipped = Base.equipment?.Primary?.LabelCap;

            return RestraintsUtility.ShouldShowRestraintsInfo(Base) ? "InRestraints".Translate() : equipped == null ? null : Lang.Get("Info.Equipped", equipped.Bold());
        }

        private string GetCompInfo()
        {
            var list = new List<string>();
            foreach (var comp in Base.AllComps)
            {
                var text = comp.CompInspectStringExtra();
                if (!text.NullOrEmpty()) { list.Add(text.Replace('\n', ' ')); }
            }
            var comps = list.ToArray();
            return comps.Length > 0 ? comps.ToCommaList() : null;
        }

        private TipSignal? GetBioTooltip()
        {
            if (IsAnimal) { return GetAnimalTooltip(); }

            if (Base.story == null) { return null; }

            var builder = new StringBuilder();

            var title = Base.story?.TitleCap;
            if (title != null) { builder.TryAppendLine(Lang.Get("Bio.Title", title)); }
            var faction = Base.Faction?.Name;
            if (faction != null) { builder.TryAppendLine(Lang.Get("Bio.Faction", faction)); }

            builder.AppendLine();

            var childhood = Base.story.GetBackstory(BackstorySlot.Childhood);
            var childhoodText = childhood == null ? null : $"{"Childhood".Translate()}: {childhood.TitleCapFor(Base.gender)}";
            var adulthood = Base.story.GetBackstory(BackstorySlot.Adulthood);
            var adulthoodText = adulthood == null ? null : $"{"Adulthood".Translate()}: {adulthood.TitleCapFor(Base.gender)}";

            builder.TryAppendLine(childhoodText);
            builder.TryAppendLine(adulthoodText);

            builder.AppendLine();

            var traits = Base.story.traits.allTraits.Count > 0 ? "Traits".Translate() + ": " + Base.story.traits.allTraits.Select(trait => trait.LabelCap).ToCommaList(true) : null;
            builder.TryAppendLine(traits);

            builder.AppendLine();

            var disabledWork = Base.story.CombinedDisabledWorkTags;
            var incapable = disabledWork == WorkTags.None ? null : "IncapableOf".Translate() + ": " + disabledWork.GetAllSelectedItems<WorkTags>().Where(tag => tag != WorkTags.None).Select(tag => tag.LabelTranslated().CapitalizeFirst()).ToCommaList(true);

            builder.TryAppendLine(incapable?.Color(Theme.CriticalColor.Value));

            return builder.Length > 0 ? builder.ToStringTrimmed().Size(Theme.RegularTextStyle.ActualSize) : null;
        }

        private TextModel GetMaster()
        {
            var master = Base.playerSettings?.Master;
            if (master == null) { return null; }

            var masterName = master.LabelShort;
            var relation = Base.GetMostImportantRelation(master)?.LabelCap;
            return TextModel.Create(Lang.Get("Bio.Master", masterName), GetAnimalTooltip(), relation == null ? (Color?) null : Theme.SkillMinorPassionColor.Value);
        }

        public TipSignal? GetAnimalTooltip(TrainableDef def = null)
        {
            var builder = new StringBuilder();

            builder.AppendLine(Lang.Get("Bio.Trainability", Base.RaceProps.trainability.LabelCap));
            builder.AppendLine(Lang.Get("Bio.Wildness", Base.RaceProps.wildness.ToStringPercent()));

            builder.AppendLine($"{"TrainingDecayInterval".Translate()}: {TrainableUtility.DegradationPeriodTicks(Base.def).ToStringTicksToDays()}");
            if (!TrainableUtility.TamenessCanDecay(Base.def)) { builder.AppendLine("TamenessWillNotDecay".Translate()); }

            builder.AppendLine(Lang.Get("Bio.Petness", Base.RaceProps.petness.ToStringPercent()));
            builder.AppendLine(Lang.Get("Bio.Diet", Base.RaceProps.ResolvedDietCategory.ToStringHuman()));

            var master = Base.playerSettings?.Master?.LabelShort;
            if (!master.NullOrEmpty())
            {
                builder.AppendLine();
                builder.AppendLine(Lang.Get("Bio.Master", master));
            }

            if (def != null)
            {
                builder.AppendLine();
                builder.AppendLine(def.description);
            }

            var text = builder.ToStringTrimmed().Size(Theme.RegularTextStyle.ActualSize);
            return new TipSignal(() => text, GUIPlus.TooltipId);
        }
    }
}