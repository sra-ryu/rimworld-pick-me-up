using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace PickMeUp
{
    /// <summary>
    /// Lists the hostile pawns that match the conditions on the current map, with a checkbox per
    /// pawn to show or hide its arrow.
    /// </summary>
    public class MatchListPanel
    {
        private const float RowHeight = 44f;
        private const float CheckboxSize = 24f;
        private const float HeaderHeight = 30f;
        private const float RefreshInterval = 0.25f; // real time, so it also works while paused

        private readonly List<Pawn> pawns = new List<Pawn>();
        private float nextRefreshTime;
        private Map lastMap;
        private Vector2 scrollPosition;

        public void Draw(Rect inRect)
        {
            Map map = Find.CurrentMap;
            ArrowMarkerMapComponent markers = map?.GetComponent<ArrowMarkerMapComponent>();
            if (markers == null)
            {
                return;
            }

            if (map != lastMap || Time.realtimeSinceStartup >= nextRefreshTime)
            {
                lastMap = map;
                nextRefreshTime = Time.realtimeSinceStartup + RefreshInterval;
                pawns.Clear();
                pawns.AddRange(PawnMatcher.FindMatches(map));
            }

            DrawHeader(new Rect(inRect.x, inRect.y, inRect.width, HeaderHeight), markers);
            Widgets.DrawLineHorizontal(inRect.x, inRect.y + HeaderHeight, inRect.width);

            Rect outRect = new Rect(inRect.x, inRect.y + HeaderHeight + 6f,
                inRect.width, inRect.height - HeaderHeight - 6f);

            if (pawns.Count == 0)
            {
                GUI.color = Color.gray;
                Widgets.Label(outRect, "PMU_NoMatches".Translate());
                GUI.color = Color.white;
                return;
            }

            DrawList(outRect, markers);
        }

        private void DrawHeader(Rect rect, ArrowMarkerMapComponent markers)
        {
            const float buttonWidth = 100f;
            bool anyHidden = markers.HiddenCount > 0;
            float labelWidth = anyHidden ? rect.width - buttonWidth - 6f : rect.width;

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(rect.x, rect.y, labelWidth, rect.height),
                "PMU_MatchList".Translate(pawns.Count));
            Text.Anchor = TextAnchor.UpperLeft;

            if (anyHidden && Widgets.ButtonText(
                    new Rect(rect.xMax - buttonWidth, rect.y + 2f, buttonWidth, rect.height - 4f),
                    "PMU_ShowAll".Translate()))
            {
                markers.ShowAllHidden();
            }
        }

        private void DrawList(Rect outRect, ArrowMarkerMapComponent markers)
        {
            var view = new Rect(0f, 0f, outRect.width - 16f, pawns.Count * RowHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, view);

            bool oldWrap = Text.WordWrap;
            Text.WordWrap = false;

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                // The list can be up to RefreshInterval old: skip pawns that left or died since.
                if (!pawn.Spawned || pawn.Dead)
                {
                    continue;
                }
                DrawRow(new Rect(0f, i * RowHeight, view.width, RowHeight - 2f), pawn, markers, i);
            }

            Text.WordWrap = oldWrap;
            Widgets.EndScrollView();
        }

        private static void DrawRow(Rect row, Pawn pawn, ArrowMarkerMapComponent markers, int index)
        {
            // [name + skills] [arrow checkbox]
            if (index % 2 == 0)
            {
                Widgets.DrawAltRect(row);
            }

            bool hidden = markers.IsHidden(pawn);
            var checkRect = new Rect(row.xMax - CheckboxSize - 6f,
                row.y + (row.height - CheckboxSize) / 2f, CheckboxSize, CheckboxSize);
            var textRect = new Rect(row.x + 6f, row.y, checkRect.x - row.x - 12f, row.height);

            GUI.color = hidden ? Color.gray : Color.white;
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(textRect.x, textRect.y + 3f, textRect.width, 22f), pawn.LabelShortCap);
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(textRect.x, textRect.y + 24f, textRect.width, 18f), SkillSummary(pawn));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            TooltipHandler.TipRegion(checkRect, "PMU_ArrowCheckboxTip".Translate());
            bool shown = !hidden;
            Widgets.Checkbox(checkRect.position, ref shown, CheckboxSize);
            if (shown == hidden)
            {
                markers.SetHidden(pawn, !shown);
            }
        }

        /// <summary>The pawn's level in every skill used by the conditions, e.g. "Shooting 8, Melee 6".</summary>
        private static string SkillSummary(Pawn pawn)
        {
            var parts = new List<string>();
            foreach (SkillCondition condition in PickMeUpMod.Settings.conditions)
            {
                SkillDef def = condition.SkillDef;
                SkillRecord record = def == null ? null : pawn.skills?.GetSkill(def);
                if (record != null && !record.TotallyDisabled)
                {
                    parts.Add(def.skillLabel.CapitalizeFirst() + " " + record.Level);
                }
            }
            return string.Join(", ", parts);
        }
    }
}
