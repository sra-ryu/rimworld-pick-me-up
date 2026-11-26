using System.Linq;
using RimWorld;
using Verse;

namespace PickMeUp
{
    /// <summary>One "the pawn has this trait" rule (e.g. Tough, Kind). Traits are yes/no, so there is no level.</summary>
    public class TraitCondition : IExposable
    {
        public string traitDefName;

        /// <summary>Null when the trait no longer exists (e.g. a removed modded trait).</summary>
        public TraitDef TraitDef =>
            traitDefName == null ? null : DefDatabase<TraitDef>.GetNamedSilentFail(traitDefName);

        /// <summary>
        /// The translated name shown to the player. Traits keep their names on their degree data
        /// (the translation files translate that), so def.label alone is not enough.
        /// </summary>
        public static string DisplayName(TraitDef def)
        {
            TraitDegreeData degree = def.degreeDatas?.FirstOrDefault(d => d.degree == 0)
                ?? def.degreeDatas?.FirstOrDefault();
            string text = degree?.label ?? def.label ?? def.defName;
            return text.CapitalizeFirst();
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref traitDefName, "trait");
        }
    }
}
