using System.Collections.Generic;
using Verse;

namespace PickMeUp
{
    /// <summary>Everything that is remembered between game sessions.</summary>
    public class PickMeUpSettings : ModSettings
    {
        /// <summary>Master switch for the arrow markers. The conditions are kept while it is off.</summary>
        public bool enabled = true;

        /// <summary>true = every condition must match (AND), false = any one is enough (OR).</summary>
        public bool matchAll = true;

        public List<SkillCondition> conditions = new List<SkillCondition>();

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enabled, "enabled", true);
            Scribe_Values.Look(ref matchAll, "matchAll", true);
            Scribe_Collections.Look(ref conditions, "conditions", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (conditions == null)
                {
                    conditions = new List<SkillCondition>();
                }
                conditions.RemoveAll(c => c == null);
            }
        }
    }
}
