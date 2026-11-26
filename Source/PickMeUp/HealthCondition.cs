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
        /// <summary>A permanent scar left by a healed injury (scratch, cut, bite, ...).</summary>
        Scar,
        /// <summary>An addiction to a drug.</summary>
        Addiction,
        /// <summary>No illness, no missing part, no addiction and no pain. Has no value.</summary>
        NoIssue,
    }

    /// <summary>
    /// One health rule. Illness, MissingPart and Addiction leave out every pawn that has that kind of
    /// issue (any illness, any missing part, any addiction). NoIssue keeps only pawns with no issues.
    /// </summary>
    public class HealthCondition : IExposable
    {
        public HealthKind kind = HealthKind.Illness;

        public void ExposeData()
        {
            Scribe_Values.Look(ref kind, "kind", HealthKind.Illness);
        }

        /// <summary>
        /// True when the pawn meets this rule. Only the health items the game shows (Hediff.Visible) are used,
        /// so things that are not listed in the Health tab (for example a race's built-in drug need) are ignored.
        /// </summary>
        public bool IsMetBy(Pawn pawn)
        {
            HediffSet hediffSet = pawn.health?.hediffSet;
            if (hediffSet == null)
            {
                return false;
            }

            List<Hediff> shown = hediffSet.hediffs.Where(h => h.Visible).ToList();
            switch (kind)
            {
                case HealthKind.Illness:
                    return !shown.Any(h => IsIllnessDef(h.def));
                case HealthKind.MissingPart:
                    return !shown.OfType<Hediff_MissingPart>().Any();
                case HealthKind.Addiction:
                    return !shown.Any(IsAddiction);
                case HealthKind.Scar:
                    return !shown.Any(IsScar);
                case HealthKind.NoIssue:
                    return !shown.Any(h => IsIllnessDef(h.def))
                        && !shown.OfType<Hediff_MissingPart>().Any()
                        && !shown.Any(IsAddiction)
                        && !shown.Any(IsScar)
                        && hediffSet.PainTotal <= 0f;
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
                && !typeof(Hediff_Addiction).IsAssignableFrom(type)
                && !typeof(Hediff_ChemicalDependency).IsAssignableFrom(type);
        }

        /// <summary>A healed injury that stayed as a scar.</summary>
        public static bool IsScar(Hediff hediff)
        {
            return hediff is Hediff_Injury injury && injury.IsPermanent();
        }

        /// <summary>
        /// Drug dependencies: normal addictions, and the drug needs some xenotypes have (GeneticDrugNeed).
        /// </summary>
        public static bool IsAddiction(Hediff hediff)
        {
            return hediff is Hediff_Addiction || hediff is Hediff_ChemicalDependency;
        }
    }
}
