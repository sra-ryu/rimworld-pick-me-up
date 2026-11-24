using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PickMeUp
{
    // Entry point: RimWorld instantiates every Mod subclass once at startup.
    public class PickMeUpMod : Mod
    {
        private const float RowHeight = 32f;
        private const float SkillButtonWidth = 180f;
        private const float LevelLabelWidth = 36f;
        private const float DeleteSize = 24f;

        public static PickMeUpSettings Settings { get; private set; }

        private Vector2 scrollPosition;

        public PickMeUpMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<PickMeUpSettings>();
            Log.Message("[Pick Me Up] Loaded.");
        }

        public override string SettingsCategory() => "PMU_SettingsTitle".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("PMU_MatchMode".Translate());
            if (listing.RadioButton("PMU_MatchAll".Translate(), Settings.matchAll,
                    tooltip: "PMU_MatchAllTip".Translate()))
            {
                Settings.matchAll = true;
            }
            if (listing.RadioButton("PMU_MatchAny".Translate(), !Settings.matchAll,
                    tooltip: "PMU_MatchAnyTip".Translate()))
            {
                Settings.matchAll = false;
            }

            listing.GapLine();
            listing.Label("PMU_Conditions".Translate());

            bool canAdd = UnusedSkills(null).Any();
            if (canAdd)
            {
                if (listing.ButtonText("PMU_AddCondition".Translate()))
                {
                    AddCondition();
                }
            }
            else
            {
                GUI.color = Color.gray;
                listing.Label("PMU_AllSkillsUsed".Translate());
                GUI.color = Color.white;
            }
            listing.Gap(6f);

            if (Settings.conditions.Count == 0)
            {
                GUI.color = Color.gray;
                listing.Label("PMU_NoConditions".Translate());
                GUI.color = Color.white;
                listing.End();
                return;
            }

            // The condition list scrolls inside whatever space is left. It must be drawn before
            // listing.End(): the listing works in a group, so its rects are relative to that group.
            Rect outRect = listing.GetRect(inRect.height - listing.CurHeight);
            DrawConditionList(outRect);
            listing.End();
        }

        private void DrawConditionList(Rect outRect)
        {
            var view = new Rect(0f, 0f, outRect.width - 16f, Settings.conditions.Count * RowHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, view);

            int removeIndex = -1;
            for (int i = 0; i < Settings.conditions.Count; i++)
            {
                var row = new Rect(0f, i * RowHeight, view.width, RowHeight - 2f);
                if (DrawConditionRow(row, Settings.conditions[i]))
                {
                    removeIndex = i;
                }
            }
            if (removeIndex >= 0)
            {
                Settings.conditions.RemoveAt(removeIndex);
            }

            Widgets.EndScrollView();
        }

        /// <summary>Draws one row. Returns true when its delete button was clicked.</summary>
        private bool DrawConditionRow(Rect row, SkillCondition condition)
        {
            // [skill dropdown] [ level slider ] [level number] [x]
            var skillRect = new Rect(row.x, row.y, SkillButtonWidth, row.height);
            var deleteRect = new Rect(row.xMax - DeleteSize, row.y + (row.height - DeleteSize) / 2f,
                DeleteSize, DeleteSize);
            var levelRect = new Rect(deleteRect.x - LevelLabelWidth - 6f, row.y, LevelLabelWidth, row.height);
            var sliderRect = new Rect(skillRect.xMax + 10f, row.y + 6f,
                levelRect.x - skillRect.xMax - 20f, row.height - 12f);

            if (Widgets.ButtonText(skillRect, SkillLabel(condition)))
            {
                OpenSkillMenu(condition);
            }

            condition.minLevel = Mathf.RoundToInt(Widgets.HorizontalSlider(
                sliderRect, condition.minLevel, 0f, SkillCondition.MaxLevel, roundTo: 1f));

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(levelRect, condition.minLevel.ToString());
            Text.Anchor = TextAnchor.UpperLeft;

            TooltipHandler.TipRegion(deleteRect, "PMU_RemoveCondition".Translate());
            return Widgets.ButtonImage(deleteRect, TexButton.Delete);
        }

        private static string SkillLabel(SkillCondition condition)
        {
            SkillDef def = condition.SkillDef;
            if (def != null)
            {
                return def.skillLabel.CapitalizeFirst();
            }
            return condition.skillDefName ?? "?";
        }

        /// <summary>Skills not already used by another condition, so a skill appears at most once.</summary>
        private static IEnumerable<SkillDef> UnusedSkills(SkillCondition except)
        {
            var used = new HashSet<string>(Settings.conditions
                .Where(c => c != except && c.skillDefName != null)
                .Select(c => c.skillDefName));
            return DefDatabase<SkillDef>.AllDefsListForReading.Where(d => !used.Contains(d.defName));
        }

        private void AddCondition()
        {
            SkillDef first = UnusedSkills(null).FirstOrDefault();
            if (first == null)
            {
                return;
            }
            Settings.conditions.Add(new SkillCondition { skillDefName = first.defName });
        }

        private static void OpenSkillMenu(SkillCondition condition)
        {
            var options = new List<FloatMenuOption>();
            foreach (SkillDef def in UnusedSkills(condition))
            {
                SkillDef captured = def;
                options.Add(new FloatMenuOption(def.skillLabel.CapitalizeFirst(),
                    () => condition.skillDefName = captured.defName));
            }
            if (options.Count > 0)
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }
    }
}
