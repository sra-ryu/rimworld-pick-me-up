using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace PickMeUp
{
    /// <summary>
    /// One "the pawn has this trait" rule (e.g. Tough, Fast walker). Some traits come in steps (degrees),
    /// such as Slowpoke, Fast walker and Jogger; each step is a separate rule, identified by degree.
    /// </summary>
    public class TraitCondition : IExposable
    {
        public string traitDefName;
        public int degree;

        /// <summary>Null when the trait no longer exists (e.g. a removed modded trait).</summary>
        public TraitDef TraitDef =>
            traitDefName == null ? null : DefDatabase<TraitDef>.GetNamedSilentFail(traitDefName);

        /// <summary>
        /// The translated name shown to the player. Traits keep their names on their degree data
        /// (the translation files translate that), so def.label alone is not enough.
        /// </summary>
        public static string DisplayName(TraitDef def, int degree)
        {
            TraitDegreeData data = def.degreeDatas?.FirstOrDefault(d => d.degree == degree)
                ?? def.degreeDatas?.FirstOrDefault();
            string text = data?.label ?? def.label ?? def.defName;
            return text.CapitalizeFirst();
        }

        /// <summary>The degrees a trait comes in. A trait without degrees has just degree 0.</summary>
        public static IEnumerable<int> DegreesOf(TraitDef def)
        {
            if (def.degreeDatas == null || def.degreeDatas.Count == 0)
            {
                return new[] { 0 };
            }
            return def.degreeDatas.Select(d => d.degree);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref traitDefName, "trait");
            Scribe_Values.Look(ref degree, "degree", 0);
        }
    }
}
