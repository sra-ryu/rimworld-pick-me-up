using System.Text;
using LudeonTK;
using RimWorld;
using System.Linq;
using Verse;

namespace PickMeUp
{
    /// <summary>Dev-mode helpers to check the matching without the arrow marker.</summary>
    public static class PickMeUpDebug
    {
        [DebugAction("Pick Me Up", "Log matching hostile pawns", false, false, false, false, false,
            0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void LogMatchingPawns()
        {
            Map map = Find.CurrentMap;
            var sb = new StringBuilder("[Pick Me Up] Matching hostile pawns:");
            int hostile = 0;
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (PawnMatcher.IsCandidate(pawn))
                {
                    hostile++;
                }
            }

            var matches = PawnMatcher.FindMatches(map);
            sb.Append($" {matches.Count} of {hostile} hostile pawns");
            foreach (Pawn pawn in matches)
            {
                sb.Append($"\n  {pawn.LabelShort} ({pawn.Faction?.Name ?? "no faction"}):");
                foreach (SkillCondition c in PickMeUpMod.Settings.conditions)
                {
                    SkillDef def = c.SkillDef;
                    if (def != null)
                    {
                        sb.Append($" {def.defName}={pawn.skills.GetSkill(def).Level}");
                    }
                }
            }
            // Health diagnosis: the health items each hostile pawn has, and which of them count as an illness.
            sb.Append("\nHealth items of hostile pawns:");
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned.Where(PawnMatcher.IsCandidate))
            {
                sb.Append($"\n  {pawn.LabelShort}:");
                foreach (Hediff h in pawn.health.hediffSet.hediffs)
                {
                    sb.Append($" {h.def.defName}[{h.def.hediffClass?.Name}, visible={h.Visible}, isBad={h.def.isBad}, illness={HealthCondition.IsIllnessDef(h.def)}]");
                }
                sb.Append($" painTotal={pawn.health.hediffSet.PainTotal:0.00}");
            }
            Log.Message(sb.ToString());
        }
    }
}
