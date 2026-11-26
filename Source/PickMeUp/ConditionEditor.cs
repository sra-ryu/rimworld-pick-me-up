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
        private const float TopResetWidth = 110f;

        private static PickMeUpSettings Settings => PickMeUpMod.Settings;

        private Vector2 scrollPosition;

        public void Draw(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            // Top row: the arrow switch on the left, Reset on the right. Both act on the whole settings.
            Rect topRow = listing.GetRect(28f);
            var resetRect = new Rect(topRow.xMax - TopResetWidth, topRow.y, TopResetWidth, topRow.height);
            var enabledRect = new Rect(topRow.x, topRow.y, topRow.width - TopResetWidth - 6f, topRow.height);
            Widgets.CheckboxLabeled(enabledRect, "PMU_Enabled".Translate(), ref Settings.enabled);
            TooltipHandler.TipRegion(enabledRect, "PMU_EnabledTip".Translate());
            TooltipHandler.TipRegion(resetRect, "PMU_ResetConditionsTip".Translate());
            if (Widgets.ButtonText(resetRect, "PMU_ResetConditions".Translate()))
            {
                // Asks first: the reset throws away every condition the player has set up.
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "PMU_ResetConditionsConfirm".Translate(), Settings.ResetConditions, destructive: true));
            }
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

            // [Add skill] [Add trait] [Add health]. The buttons share the row evenly.
            // Only the add buttons that still have something to add are shown.
            var adds = new List<KeyValuePair<string, Action>>();
            if (UnusedSkills(null).Any())
            {
                adds.Add(new KeyValuePair<string, Action>("PMU_AddSkill", OpenAddSkillMenu));
            }
            if (UnusedTraits(null).Any())
            {
                adds.Add(new KeyValuePair<string, Action>("PMU_AddTrait", OpenAddTraitMenu));
            }
            adds.Add(new KeyValuePair<string, Action>("PMU_AddHealth", OpenAddHealthMenu));

            Rect buttonRow = listing.GetRect(30f);
            const float gap = 6f;
            float addWidth = (buttonRow.width - gap * (adds.Count - 1)) / adds.Count;
            for (int i = 0; i < adds.Count; i++)
            {
                var addRect = new Rect(buttonRow.x + i * (addWidth + gap), buttonRow.y, addWidth, buttonRow.height);
                if (Widgets.ButtonText(addRect, adds[i].Key.Translate()))
                {
                    adds[i].Value();
                }
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

        /// <summary>Skill rows, then trait rows, then health rows.</summary>
        private void DrawConditionList(Rect outRect)
        {
            int skillCount = Settings.conditions.Count;
            int traitCount = Settings.traitConditions.Count;
            int healthCount = Settings.healthConditions.Count;
            var view = new Rect(0f, 0f, outRect.width - 16f, (skillCount + traitCount + healthCount) * RowHeight);
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

            int removeHealth = -1;
            for (int k = 0; k < healthCount; k++)
            {
                var row = new Rect(0f, (skillCount + traitCount + k) * RowHeight, view.width, RowHeight - 2f);
                if (DrawHealthRow(row, Settings.healthConditions[k]))
                {
                    removeHealth = k;
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
            if (removeHealth >= 0)
            {
                Settings.healthConditions.RemoveAt(removeHealth);
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

        /// <summary>
        /// Draws one health row: [kind: value dropdown] [x]. "No pain" has no value, so it is only a label.
        /// </summary>
        private bool DrawHealthRow(Rect row, HealthCondition condition)
        {
            var valueRect = new Rect(row.x, row.y, SkillButtonWidth + 6f + PassionButtonWidth, row.height);
            var deleteRect = DeleteRect(row);

            if (condition.kind == HealthKind.NoPain)
            {
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(valueRect, HealthLabel(condition));
                Text.Anchor = TextAnchor.UpperLeft;
            }
            else if (Widgets.ButtonText(valueRect, HealthLabel(condition)))
            {
                OpenHealthValueMenu(condition);
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

        /// <summary>"Kind: value", or "any" when no value is set. Pain is shown on its own.</summary>
        private static string HealthLabel(HealthCondition condition)
        {
            switch (condition.kind)
            {
                case HealthKind.Illness:
                    return "PMU_KindIllness".Translate() + ": " + HealthValueLabel(condition);
                case HealthKind.MissingPart:
                    return "PMU_KindMissingPart".Translate() + ": " + HealthValueLabel(condition);
                case HealthKind.Addiction:
                    return "PMU_KindAddiction".Translate() + ": " + HealthValueLabel(condition);
                default:
                    return "PMU_NoPain".Translate();
            }
        }

        private static string HealthValueLabel(HealthCondition condition)
        {
            if (condition.defName == null)
            {
                switch (condition.kind)
                {
                    case HealthKind.Illness: return "PMU_AnyIllness".Translate();
                    case HealthKind.MissingPart: return "PMU_AnyMissingPart".Translate();
                    default: return "PMU_AnyAddiction".Translate();
                }
            }

            Def def = DefForHealth(condition);
            return def != null ? HealthCondition.DisplayName(def) : condition.defName;
        }

        private static Def DefForHealth(HealthCondition condition)
        {
            switch (condition.kind)
            {
                case HealthKind.Illness: return DefDatabase<HediffDef>.GetNamedSilentFail(condition.defName);
                case HealthKind.MissingPart: return DefDatabase<BodyPartDef>.GetNamedSilentFail(condition.defName);
                case HealthKind.Addiction: return DefDatabase<ChemicalDef>.GetNamedSilentFail(condition.defName);
                default: return null;
            }
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

        /// <summary>
        /// "Add health" lists the kinds. Picking one adds a row that means "any" of that kind;
        /// the row's own dropdown then narrows it down to a specific illness, part or drug.
        /// </summary>
        private static void OpenAddHealthMenu()
        {
            var kinds = new[] { HealthKind.Illness, HealthKind.MissingPart, HealthKind.Addiction, HealthKind.NoPain };
            OpenPickMenu(kinds, kind => HealthLabel(new HealthCondition { kind = kind }), kind =>
                Settings.healthConditions.Add(new HealthCondition { kind = kind }));
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

        /// <summary>
        /// Changes the value of a health row. The first entry is "any" (null), then every specific def.
        /// </summary>
        private static void OpenHealthValueMenu(HealthCondition condition)
        {
            IEnumerable<Def> defs;
            switch (condition.kind)
            {
                case HealthKind.Illness: defs = HealthCondition.IllnessDefs; break;
                case HealthKind.MissingPart: defs = HealthCondition.BodyPartDefs; break;
                case HealthKind.Addiction: defs = HealthCondition.ChemicalDefs; break;
                default: return;
            }

            var values = new List<string> { null };
            values.AddRange(defs.Select(d => d.defName));
            OpenPickMenu(values, name => HealthValueLabel(new HealthCondition
                {
                    kind = condition.kind,
                    defName = name,
                }),
                name => condition.defName = name);
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
