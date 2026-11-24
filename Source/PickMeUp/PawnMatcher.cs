using System.Collections.Generic;
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

        public static bool Matches(Pawn pawn, PickMeUpSettings settings)
        {
            if (settings == null || settings.conditions.Count == 0 || pawn?.skills == null)
            {
                return false; // nothing to look for
            }

            foreach (SkillCondition condition in settings.conditions)
            {
                bool met = Meets(pawn, condition);
                if (settings.matchAll && !met)
                {
                    return false;
                }
                if (!settings.matchAll && met)
                {
                    return true;
                }
            }

            // AND: nothing failed. OR: nothing matched.
            return settings.matchAll;
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

            return record.Level >= condition.minLevel;
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
