using Verse;
using RimWorld;

namespace PickMeUp
{
    /// <summary>One "skill X must be at least level N" rule.</summary>
    public class SkillCondition : IExposable
    {
        public const int DefaultLevel = 5;
        public const int MaxLevel = 20;

        public string skillDefName;
        public int minLevel = DefaultLevel;

        /// <summary>Null when the skill no longer exists (e.g. a removed modded skill).</summary>
        public SkillDef SkillDef =>
            skillDefName == null ? null : DefDatabase<SkillDef>.GetNamedSilentFail(skillDefName);

        public void ExposeData()
        {
            Scribe_Values.Look(ref skillDefName, "skill");
            // forceSave: RimWorld skips values equal to the default, which would silently change
            // saved conditions whenever DefaultLevel changes.
            Scribe_Values.Look(ref minLevel, "minLevel", DefaultLevel, forceSave: true);
        }
    }
}
