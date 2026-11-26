using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PickMeUp
{
    /// <summary>
    /// The condition editing UI. Shared by the mod settings page and the in-game window,
    /// so the player sees the same thing in both and edits the same settings.
    /// </summary>
    public class ConditionEditor
    {
        private const float RowHeight = 32f;
        private const float SkillButtonWidth = 180f;
        private const float PassionButtonWidth = 44f;
        private const float LevelLabelWidth = 36f;
        private const float DeleteSize = 24f;

        private static PickMeUpSettings Settings => PickMeUpMod.Settings;

        private Vector2 scrollPosition;

        public void Draw(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.CheckboxLabeled("PMU_Enabled".Translate(), ref Settings.enabled,
                tooltip: "PMU_EnabledTip".Translate());
            listing.GapLine();

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

            // [Add condition] [Reset]. When every skill is used there is nothing to add, so only
            // the reset button is left.
            Rect buttonRow = listing.GetRect(30f);
            bool canAdd = UnusedSkills(null).Any();
            float resetWidth = canAdd ? buttonRow.width / 5f : buttonRow.width;
            var resetRect = new Rect(buttonRow.xMax - resetWidth, buttonRow.y, resetWidth, buttonRow.height);
            if (canAdd)
            {
                var addRect = new Rect(buttonRow.x, buttonRow.y, buttonRow.width - resetWidth - 6f, buttonRow.height);
                if (Widgets.ButtonText(addRect, "PMU_AddCondition".Translate()))
                {
                    OpenAddMenu();
                }
            }
            TooltipHandler.TipRegion(resetRect, "PMU_ResetConditionsTip".Translate());
            if (Widgets.ButtonText(resetRect, "PMU_ResetConditions".Translate()))
            {
                // Asks first: the reset throws away every condition the player has set up.
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "PMU_ResetConditionsConfirm".Translate(), Settings.ResetConditions, destructive: true));
            }
            if (!canAdd)
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
            // [skill dropdown] [passion toggle] [ level slider ] [level number] [x]
            var skillRect = new Rect(row.x, row.y, SkillButtonWidth, row.height);
            var passionRect = new Rect(skillRect.xMax + 6f, row.y, PassionButtonWidth, row.height);
            var deleteRect = new Rect(row.xMax - DeleteSize, row.y + (row.height - DeleteSize) / 2f,
                DeleteSize, DeleteSize);
            var levelRect = new Rect(deleteRect.x - LevelLabelWidth - 6f, row.y, LevelLabelWidth, row.height);
            var sliderRect = new Rect(passionRect.xMax + 10f, row.y + 6f,
                levelRect.x - passionRect.xMax - 20f, row.height - 12f);

            if (Widgets.ButtonText(skillRect, SkillLabel(condition)))
            {
                OpenSkillMenu(condition);
            }

            // Shows the current passion as flames. Click to open the picker below the mouse.
            TooltipHandler.TipRegion(passionRect, "PMU_PassionTip".Translate());
            Widgets.DrawHighlightIfMouseover(passionRect);
            var passionIconRect = new Rect(passionRect.center.x - 11f, passionRect.center.y - 11f, 22f, 22f);
            Widgets.DrawTextureFitted(passionIconRect, Window_PassionPicker.IconFor(condition.passion), 1f);
            if (Widgets.ButtonInvisible(passionRect))
            {
                Find.WindowStack.Add(new Window_PassionPicker(condition, UI.MousePositionOnUI));
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

        /// <summary>"Add condition" opens the skill list directly; picking one adds it as a new row.</summary>
        private static void OpenAddMenu()
        {
            OpenSkillMenu(UnusedSkills(null), def => Settings.conditions.Add(
                new SkillCondition { skillDefName = def.defName }));
        }

        /// <summary>Changes the skill of an existing row.</summary>
        private static void OpenSkillMenu(SkillCondition condition)
        {
            OpenSkillMenu(UnusedSkills(condition), def => condition.skillDefName = def.defName);
        }

        private static void OpenSkillMenu(IEnumerable<SkillDef> skills, System.Action<SkillDef> onPick)
        {
            var options = new List<FloatMenuOption>();
            foreach (SkillDef def in skills)
            {
                SkillDef captured = def;
                options.Add(new FloatMenuOption(def.skillLabel.CapitalizeFirst(), () => onPick(captured)));
            }
            if (options.Count > 0)
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }
    }
}
