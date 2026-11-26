using System.Collections.Generic;
using Verse;

namespace PickMeUp
{
    /// <summary>Everything that is remembered between game sessions.</summary>
    public class PickMeUpSettings : ModSettings
    {
        /// <summary>Master switch for the arrow markers. The conditions are kept while it is off.</summary>
        public bool enabled = true;

        /// <summary>Skills: true = every skill must match (AND), false = any one is enough (OR).</summary>
        public bool matchAll = true;

        /// <summary>Traits: true = every trait must match (AND), false = any one is enough (OR).</summary>
        public bool matchAllTraits = true;

        public List<SkillCondition> conditions = new List<SkillCondition>();

        public List<TraitCondition> traitConditions = new List<TraitCondition>();

        public List<HealthCondition> healthConditions = new List<HealthCondition>();

        /// <summary>Skill, trait and health conditions together.</summary>
        public int ConditionCount => conditions.Count + traitConditions.Count + healthConditions.Count;

        /// <summary>Removes every condition, back to the empty state a fresh install starts with.</summary>
        public void ResetConditions()
        {
            conditions.Clear();
            traitConditions.Clear();
            healthConditions.Clear();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enabled, "enabled", true);
            Scribe_Values.Look(ref matchAll, "matchAll", true);
            Scribe_Values.Look(ref matchAllTraits, "matchAllTraits", true);
            Scribe_Collections.Look(ref conditions, "conditions", LookMode.Deep);
            Scribe_Collections.Look(ref traitConditions, "traitConditions", LookMode.Deep);
            Scribe_Collections.Look(ref healthConditions, "healthConditions", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (conditions == null)
                {
                    conditions = new List<SkillCondition>();
                }
                if (traitConditions == null)
                {
                    traitConditions = new List<TraitCondition>();
                }
                conditions.RemoveAll(c => c == null);
                traitConditions.RemoveAll(c => c == null);
                if (healthConditions == null)
                {
                    healthConditions = new List<HealthCondition>();
                }
                healthConditions.RemoveAll(c => c == null);
            }
        }
    }
}
