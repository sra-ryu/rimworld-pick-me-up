using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace PickMeUp
{
    /// <summary>Decides which pawns match the skill conditions from the settings.</summary>
    public static class PawnMatcher
    {
        /// <summary>
        /// Pawns that can be picked: spawned, alive, able to have skills, and hostile to the player.
        /// Prisoners and slaves of the colony are not hostile, so they are skipped.
        /// </summary>
        public static bool IsCandidate(Pawn pawn)
        {
            return pawn != null
                && pawn.Spawned
                && !pawn.Dead
                && pawn.skills != null
                && pawn.HostileTo(Faction.OfPlayer);
        }

        /// <summary>True when the pawn satisfies the current conditions (AND or OR, as set in the settings).</summary>
        public static bool Matches(Pawn pawn)
        {
            return Matches(pawn, PickMeUpMod.Settings);
        }

        /// <summary>
        /// Skills are combined with their own AND/OR, traits with theirs, and health always applies:
        /// the pawn must pass the skill group, the trait group and every health rule.
        /// </summary>
        public static bool Matches(Pawn pawn, PickMeUpSettings settings)
        {
            if (settings == null || settings.ConditionCount == 0 || pawn?.skills == null)
            {
                return false; // nothing to look for
            }

            bool skillsOk = settings.conditions.Count == 0
                || (settings.matchAll
                    ? settings.conditions.All(c => Meets(pawn, c))
                    : settings.conditions.Any(c => Meets(pawn, c)));

            bool traitsOk = settings.traitConditions.Count == 0
                || (settings.matchAllTraits
                    ? settings.traitConditions.All(t => HasTrait(pawn, t))
                    : settings.traitConditions.Any(t => HasTrait(pawn, t)));

            bool healthOk = settings.healthConditions.All(h => h.IsMetBy(pawn));

            return skillsOk && traitsOk && healthOk;
        }

        private static bool Meets(Pawn pawn, SkillCondition condition)
        {
            SkillDef def = condition.SkillDef;
            if (def == null)
            {
                return false; // skill no longer exists (e.g. a removed mod)
            }

            SkillRecord record = pawn.skills.GetSkill(def);
            if (record == null || record.TotallyDisabled)
            {
                return false; // the pawn can never do this skill
            }

            return record.Level >= condition.minLevel && record.passion >= condition.passion;
        }

        private static bool HasTrait(Pawn pawn, TraitCondition condition)
        {
            TraitDef def = condition.TraitDef;
            if (def == null || pawn.story?.traits == null)
            {
                return false;
            }
            // The exact step of the trait must match, e.g. Fast walker, not Jogger.
            Trait trait = pawn.story.traits.GetTrait(def);
            return trait != null && trait.Degree == condition.degree;
        }

        /// <summary>Every matching pawn on the given map, hostile ones only.</summary>
        public static List<Pawn> FindMatches(Map map)
        {
            var result = new List<Pawn>();
            if (map == null)
            {
                return result;
            }

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (IsCandidate(pawn) && Matches(pawn))
                {
                    result.Add(pawn);
                }
            }
            return result;
        }
    }
}
