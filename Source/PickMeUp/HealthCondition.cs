using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace PickMeUp
{
    /// <summary>The kinds of health condition. Each kind has its own list of values in the editor.</summary>
    public enum HealthKind
    {
        /// <summary>A disease, e.g. flu or asthma.</summary>
        Illness,
        /// <summary>A missing organ or limb.</summary>
        MissingPart,
        /// <summary>An addiction to a drug.</summary>
        Addiction,
        /// <summary>Pawns with any pain are left out. Has no value.</summary>
        NoPain,
    }

    /// <summary>
    /// One health rule. defName is the specific illness, body part or chemical. When it is null the
    /// rule means "any of this kind". NoPain has no value.
    /// </summary>
    public class HealthCondition : IExposable
    {
        public HealthKind kind = HealthKind.Illness;
        public string defName;

        public void ExposeData()
        {
            Scribe_Values.Look(ref kind, "kind", HealthKind.Illness);
            Scribe_Values.Look(ref defName, "def");
        }

        /// <summary>True when the pawn meets this rule.</summary>
        public bool IsMetBy(Pawn pawn)
        {
            HediffSet hediffs = pawn.health?.hediffSet;
            if (hediffs == null)
            {
                return false;
            }

            switch (kind)
            {
                case HealthKind.Illness:
                    return hediffs.hediffs.Any(h => h.def != null && IsIllnessDef(h.def)
                        && (defName == null || h.def.defName == defName));
                case HealthKind.MissingPart:
                    return hediffs.hediffs.OfType<Hediff_MissingPart>()
                        .Any(h => defName == null || h.Part?.def?.defName == defName);
                case HealthKind.Addiction:
                    return hediffs.hediffs.OfType<Hediff_Addiction>()
                        .Any(h => defName == null || h.Chemical?.defName == defName);
                case HealthKind.NoPain:
                    return hediffs.PainTotal <= 0f;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Health problems that are diseases: bad hediffs that are not injuries, missing parts,
        /// prosthetics or addictions.
        /// </summary>
        public static bool IsIllnessDef(HediffDef def)
        {
            Type type = def.hediffClass;
            return def.isBad
                && type != null
                && !typeof(Hediff_Injury).IsAssignableFrom(type)
                && !typeof(Hediff_MissingPart).IsAssignableFrom(type)
                && !typeof(Hediff_AddedPart).IsAssignableFrom(type)
                && !typeof(Hediff_Addiction).IsAssignableFrom(type);
        }

        public static IEnumerable<HediffDef> IllnessDefs =>
            DefDatabase<HediffDef>.AllDefsListForReading.Where(IsIllnessDef).OrderBy(DisplayName);

        public static IEnumerable<BodyPartDef> BodyPartDefs =>
            DefDatabase<BodyPartDef>.AllDefsListForReading.OrderBy(DisplayName);

        public static IEnumerable<ChemicalDef> ChemicalDefs =>
            DefDatabase<ChemicalDef>.AllDefsListForReading.OrderBy(DisplayName);

        /// <summary>The translated name of a def, with its first letter capitalised.</summary>
        public static string DisplayName(Def def)
        {
            return (def.label ?? def.defName).CapitalizeFirst();
        }
    }
}
