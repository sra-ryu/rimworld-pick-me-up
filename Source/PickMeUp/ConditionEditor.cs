using System;
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
        private const float SkillButtonWidth = 120f;
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

            // [Add skill] [Add trait] [Reset]. A button is left out when nothing is left to add;
            // when neither is possible only the reset button remains.
            bool canAddSkill = UnusedSkills(null).Any();
            bool canAddTrait = UnusedTraits(null).Any();
            int addCount = (canAddSkill ? 1 : 0) + (canAddTrait ? 1 : 0);

            Rect buttonRow = listing.GetRect(30f);
            float resetWidth = addCount > 0 ? buttonRow.width / 5f : buttonRow.width;
            var resetRect = new Rect(buttonRow.xMax - resetWidth, buttonRow.y, resetWidth, buttonRow.height);
            float addWidth = addCount == 2
                ? (buttonRow.width - resetWidth - 12f) / 2f
                : buttonRow.width - resetWidth - 6f;

            if (canAddSkill)
            {
                var skillRect = new Rect(buttonRow.x, buttonRow.y, addWidth, buttonRow.height);
                if (Widgets.ButtonText(skillRect, "PMU_AddSkill".Translate()))
                {
                    OpenAddSkillMenu();
                }
            }
            if (canAddTrait)
            {
                float x = canAddSkill ? buttonRow.x + addWidth + 6f : buttonRow.x;
                var traitRect = new Rect(x, buttonRow.y, addWidth, buttonRow.height);
                if (Widgets.ButtonText(traitRect, "PMU_AddTrait".Translate()))
                {
                    OpenAddTraitMenu();
                }
            }

            TooltipHandler.TipRegion(resetRect, "PMU_ResetConditionsTip".Translate());
            if (Widgets.ButtonText(resetRect, "PMU_ResetConditions".Translate()))
            {
                // Asks first: the reset throws away every condition the player has set up.
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "PMU_ResetConditionsConfirm".Translate(), Settings.ResetConditions, destructive: true));
            }
            if (addCount == 0)
            {
                GUI.color = Color.gray;
                listing.Label("PMU_AllConditionsUsed".Translate());
                GUI.color = Color.white;
            }

            listing.Gap(6f);

            if (Settings.ConditionCount == 0)
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

        /// <summary>Skill rows first, then trait rows.</summary>
        private void DrawConditionList(Rect outRect)
        {
            int skillCount = Settings.conditions.Count;
            int traitCount = Settings.traitConditions.Count;
            var view = new Rect(0f, 0f, outRect.width - 16f, (skillCount + traitCount) * RowHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, view);

            int removeSkill = -1;
            for (int i = 0; i < skillCount; i++)
            {
                var row = new Rect(0f, i * RowHeight, view.width, RowHeight - 2f);
                if (DrawConditionRow(row, Settings.conditions[i]))
                {
                    removeSkill = i;
                }
            }

            int removeTrait = -1;
            for (int j = 0; j < traitCount; j++)
            {
                var row = new Rect(0f, (skillCount + j) * RowHeight, view.width, RowHeight - 2f);
                if (DrawTraitRow(row, Settings.traitConditions[j]))
                {
                    removeTrait = j;
                }
            }

            if (removeSkill >= 0)
            {
                Settings.conditions.RemoveAt(removeSkill);
            }
            if (removeTrait >= 0)
            {
                Settings.traitConditions.RemoveAt(removeTrait);
            }

            Widgets.EndScrollView();
        }

        /// <summary>Draws one skill row. Returns true when its delete button was clicked.</summary>
        private bool DrawConditionRow(Rect row, SkillCondition condition)
        {
            // [skill dropdown] [passion toggle] [ level slider ] [level number] [x]
            var skillRect = new Rect(row.x, row.y, SkillButtonWidth, row.height);
            var passionRect = new Rect(skillRect.xMax + 6f, row.y, PassionButtonWidth, row.height);
            var deleteRect = DeleteRect(row);
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

        /// <summary>Draws one trait row. Traits are yes/no, so there is only a name and a delete button.</summary>
        private bool DrawTraitRow(Rect row, TraitCondition condition)
        {
            // [trait dropdown]                        [x]
            var traitRect = new Rect(row.x, row.y, SkillButtonWidth + 6f + PassionButtonWidth, row.height);
            var deleteRect = DeleteRect(row);

            if (Widgets.ButtonText(traitRect, TraitLabel(condition)))
            {
                OpenTraitMenu(condition);
            }

            TooltipHandler.TipRegion(deleteRect, "PMU_RemoveCondition".Translate());
            return Widgets.ButtonImage(deleteRect, TexButton.Delete);
        }

        private static Rect DeleteRect(Rect row)
        {
            return new Rect(row.xMax - DeleteSize, row.y + (row.height - DeleteSize) / 2f, DeleteSize, DeleteSize);
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

        private static string TraitLabel(TraitCondition condition)
        {
            TraitDef def = condition.TraitDef;
            if (def != null)
            {
                return TraitCondition.DisplayName(def);
            }
            return condition.traitDefName ?? "?";
        }

        /// <summary>Skills not already used by another condition, so a skill appears at most once.</summary>
        private static IEnumerable<SkillDef> UnusedSkills(SkillCondition except)
        {
            var used = new HashSet<string>(Settings.conditions
                .Where(c => c != except && c.skillDefName != null)
                .Select(c => c.skillDefName));
            return DefDatabase<SkillDef>.AllDefsListForReading.Where(d => !used.Contains(d.defName));
        }

        /// <summary>Traits not already used by another condition, so a trait appears at most once.</summary>
        private static IEnumerable<TraitDef> UnusedTraits(TraitCondition except)
        {
            var used = new HashSet<string>(Settings.traitConditions
                .Where(c => c != except && c.traitDefName != null)
                .Select(c => c.traitDefName));
            return DefDatabase<TraitDef>.AllDefsListForReading
                .Where(d => !used.Contains(d.defName))
                .OrderBy(d => TraitCondition.DisplayName(d));
        }

        /// <summary>"Add skill" opens the skill list directly; picking one adds it as a new row.</summary>
        private static void OpenAddSkillMenu()
        {
            OpenPickMenu(UnusedSkills(null), def => def.skillLabel.CapitalizeFirst(), def =>
                Settings.conditions.Add(new SkillCondition { skillDefName = def.defName }));
        }

        /// <summary>"Add trait" opens the trait list directly; picking one adds it as a new row.</summary>
        private static void OpenAddTraitMenu()
        {
            OpenPickMenu(UnusedTraits(null), def => TraitCondition.DisplayName(def), def =>
                Settings.traitConditions.Add(new TraitCondition { traitDefName = def.defName }));
        }

        /// <summary>Changes the skill of an existing row.</summary>
        private static void OpenSkillMenu(SkillCondition condition)
        {
            OpenPickMenu(UnusedSkills(condition), def => def.skillLabel.CapitalizeFirst(),
                def => condition.skillDefName = def.defName);
        }

        /// <summary>Changes the trait of an existing row.</summary>
        private static void OpenTraitMenu(TraitCondition condition)
        {
            OpenPickMenu(UnusedTraits(condition), def => TraitCondition.DisplayName(def),
                def => condition.traitDefName = def.defName);
        }

        private static void OpenPickMenu<T>(IEnumerable<T> items, Func<T, string> label, Action<T> onPick)
        {
            var options = new List<FloatMenuOption>();
            foreach (T item in items)
            {
                T captured = item;
                options.Add(new FloatMenuOption(label(captured), () => onPick(captured)));
            }
            if (options.Count > 0)
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }
    }
}
