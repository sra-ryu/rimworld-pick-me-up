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

        // Whether the health checkbox popup is open, and where the "Add health" button is this frame.
        private bool healthPanelOpen;
        private Rect healthButtonRect;

        // "Add skill" and "Add trait" button rects (this window's coordinates), and the open lists, if any.
        private Rect skillButtonRect;
        private Rect traitButtonRect;
        private FloatMenu skillMenu;
        private float skillMenuClosedAt = -10f;

        // The open trait list, if any, and where it opens.
        private FloatMenu traitMenu;
        private float traitMenuClosedAt = -10f;
        private Vector2 traitListAnchor;

        // A click that closes a list also reaches the button it was opened from. Ignore that click, so the
        // button closes the list instead of reopening it.
        private const float ToggleGraceSeconds = 0.3f;

        // How far right of the end of the button text the list opens, in UI units.
        private const float SkillListExtraOffset = 24f;

        private static readonly HealthKind[] HealthKinds =
        {
            HealthKind.Illness, HealthKind.MissingPart, HealthKind.Addiction, HealthKind.Scar,
            HealthKind.NoIssue,
        };

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

            // Skills and traits each have their own AND/OR. Health is not listed here: it always applies.
            DrawMatchRow(listing, "PMU_CombineSkills", ref Settings.matchAll);
            DrawMatchRow(listing, "PMU_CombineTraits", ref Settings.matchAllTraits);

            listing.GapLine();
            listing.Label("PMU_Conditions".Translate());

            // [Add skill] [Add trait] [Add health]. The buttons share the row evenly.
            // Only the add buttons that still have something to add are shown.
            var adds = new List<KeyValuePair<string, Action>>();
            adds.Add(new KeyValuePair<string, Action>("PMU_AddSkill", () => ToggleAddSkill()));
            adds.Add(new KeyValuePair<string, Action>("PMU_AddTrait", () => ToggleAddTrait()));
            adds.Add(new KeyValuePair<string, Action>("PMU_AddHealth", ToggleHealthPanel));

            Rect buttonRow = listing.GetRect(30f);
            const float gap = 6f;
            float addWidth = (buttonRow.width - gap * (adds.Count - 1)) / adds.Count;
            for (int i = 0; i < adds.Count; i++)
            {
                var addRect = new Rect(buttonRow.x + i * (addWidth + gap), buttonRow.y, addWidth, buttonRow.height);
                if (adds[i].Key == "PMU_AddHealth")
                {
                    healthButtonRect = addRect;
                }
                if (adds[i].Key == "PMU_AddSkill")
                {
                    skillButtonRect = addRect;
                }
                if (adds[i].Key == "PMU_AddTrait")
                {
                    traitButtonRect = addRect;
                }
                if (Widgets.ButtonText(addRect, adds[i].Key.Translate()))
                {
                    adds[i].Value();
                }
            }

            listing.Gap(6f);

            // The hint is about skills only: it shows until the first skill is added.
            if (Settings.conditions.Count == 0)
            {
                GUI.color = Color.gray;
                listing.Label("PMU_NoConditions".Translate());
                GUI.color = Color.white;
                DrawHealthPanel();
                listing.End();
                return;
            }

            // The condition list scrolls inside whatever space is left. It must be drawn before
            // listing.End(): the listing works in a group, so its rects are relative to that group.
            Rect outRect = listing.GetRect(inRect.height - listing.CurHeight);
            DrawConditionList(outRect);
            DrawHealthPanel();
            listing.End();
        }

        /// <summary>One row: a label on the left, and AND / OR choices on the right.</summary>
        private static void DrawMatchRow(Listing_Standard listing, string labelKey, ref bool matchAll)
        {
            Rect row = listing.GetRect(26f);
            var andRect = new Rect(row.xMax - 150f, row.y, 70f, row.height);
            var orRect = new Rect(row.xMax - 70f, row.y, 70f, row.height);

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(row.x, row.y, row.width - 160f, row.height), labelKey.Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            TooltipHandler.TipRegion(andRect, "PMU_MatchAllTip".Translate());
            if (Widgets.RadioButtonLabeled(andRect, "PMU_AndShort".Translate(), matchAll))
            {
                matchAll = true;
            }
            TooltipHandler.TipRegion(orRect, "PMU_MatchAnyTip".Translate());
            if (Widgets.RadioButtonLabeled(orRect, "PMU_OrShort".Translate(), !matchAll))
            {
                matchAll = false;
            }
        }

        /// <summary>Skill rows. Traits are picked in their own list, and health in its own panel.</summary>
        private void DrawConditionList(Rect outRect)
        {
            int skillCount = Settings.conditions.Count;
            var view = new Rect(0f, 0f, outRect.width - 16f, skillCount * RowHeight);
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


            if (removeSkill >= 0)
            {
                Settings.conditions.RemoveAt(removeSkill);
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

        public static string HealthKindLabel(HealthKind kind)
        {
            switch (kind)
            {
                case HealthKind.Illness: return "PMU_KindIllness".Translate();
                case HealthKind.MissingPart: return "PMU_KindMissingPart".Translate();
                case HealthKind.Addiction: return "PMU_KindAddiction".Translate();
                case HealthKind.Scar: return "PMU_KindScar".Translate();
                default: return "PMU_NoIssue".Translate();
            }
        }

        /// <summary>Every skill. The same skill can be added more than once.</summary>
        private static IEnumerable<SkillDef> AllSkills => DefDatabase<SkillDef>.AllDefsListForReading;

        /// <summary>
        /// The health checkboxes, floating right under the "Add health" button. Drawn last so they sit on top
        /// of the condition list. Clicking outside the popup and the button closes it.
        /// Illness, missing part and addiction can be combined; "No health issues" stands alone.
        /// </summary>
        private void DrawHealthPanel()
        {
            if (!healthPanelOpen)
            {
                return;
            }

            const float rowHeight = 26f;
            const float padding = 8f;
            // Aligned to the button's right edge and grown leftwards, so it stays inside the window.
            float panelWidth = Mathf.Max(healthButtonRect.width, 200f);
            var panelRect = new Rect(healthButtonRect.xMax - panelWidth, healthButtonRect.yMax + 4f,
                panelWidth, HealthKinds.Length * rowHeight + padding * 2f);

            Vector2 mouse = Event.current.mousePosition;
            if (Event.current.type == EventType.MouseDown
                && !panelRect.Contains(mouse) && !healthButtonRect.Contains(mouse))
            {
                healthPanelOpen = false;
                return;
            }

            Widgets.DrawWindowBackground(panelRect);
            for (int i = 0; i < HealthKinds.Length; i++)
            {
                HealthKind kind = HealthKinds[i];
                var row = new Rect(panelRect.x + padding, panelRect.y + padding + i * rowHeight,
                    panelRect.width - padding * 2f, rowHeight);

                bool on = IsHealthKindOn(kind);
                bool before = on;
                Widgets.CheckboxLabeled(row, HealthKindLabel(kind), ref on);
                if (on != before)
                {
                    SetHealthKind(kind, on);
                }
            }
        }

        private static bool IsHealthKindOn(HealthKind kind)
        {
            return Settings.healthConditions.Any(h => h.kind == kind);
        }

        private static void SetHealthKind(HealthKind kind, bool on)
        {
            if (kind == HealthKind.NoIssue)
            {
                // "No health issues" is exclusive: it replaces everything else, or is simply removed.
                Settings.healthConditions.Clear();
                if (on)
                {
                    Settings.healthConditions.Add(new HealthCondition { kind = HealthKind.NoIssue });
                }
                return;
            }

            Settings.healthConditions.RemoveAll(h => h.kind == kind);
            if (on)
            {
                Settings.healthConditions.RemoveAll(h => h.kind == HealthKind.NoIssue);
                Settings.healthConditions.Add(new HealthCondition { kind = kind });
            }
        }

        /// <summary>Changes the skill of an existing row.</summary>
        private static void OpenSkillMenu(SkillCondition condition)
        {
            OpenPickMenu(AllSkills, def => def.skillLabel.CapitalizeFirst(),
                def => condition.skillDefName = def.defName);
        }

        /// <summary>"Add health": unfolds or folds the checkboxes, and closes the skill and trait lists.</summary>
        private void ToggleHealthPanel()
        {
            CloseMenu(ref skillMenu);
            CloseMenu(ref traitMenu);
            healthPanelOpen = !healthPanelOpen;
        }

        private static void CloseMenu(ref FloatMenu menu)
        {
            if (menu != null && Find.WindowStack.Windows.Contains(menu))
            {
                menu.Close(false);
            }
            menu = null;
        }

        /// <summary>"Add skill": opens the skill list next to its button, or closes it when it is open.</summary>
        private void ToggleAddSkill()
        {
            healthPanelOpen = false;
            if (skillMenu != null && Find.WindowStack.Windows.Contains(skillMenu))
            {
                skillMenu.Close(false);
                return;
            }
            if (Time.realtimeSinceStartup - skillMenuClosedAt < ToggleGraceSeconds)
            {
                return;
            }

            var options = AllSkills.Select(def => new FloatMenuOption(
                def.skillLabel.CapitalizeFirst(),
                () => Settings.conditions.Add(new SkillCondition { skillDefName = def.defName }))).ToList();
            if (options.Count == 0)
            {
                return;
            }

            skillMenu = new PlacedFloatMenu(options, ButtonListAnchor(skillButtonRect, "PMU_AddSkill", 2f), centered: false)
            {
                onClose = () => skillMenuClosedAt = Time.realtimeSinceStartup,
            };
            Find.WindowStack.Add(skillMenu);
        }

        /// <summary>"Add trait": opens the trait list next to its button, or closes it when it is open.</summary>
        private void ToggleAddTrait()
        {
            healthPanelOpen = false;
            if (traitMenu != null && Find.WindowStack.Windows.Contains(traitMenu))
            {
                traitMenu.Close(false);
                return;
            }
            if (Time.realtimeSinceStartup - traitMenuClosedAt < ToggleGraceSeconds)
            {
                return;
            }

            traitListAnchor = ButtonListAnchor(traitButtonRect, "PMU_AddTrait", 1f);
            OpenTraitMenu();
        }

        /// <summary>
        /// The trait list, in the game's own list style. Selected traits are green. Picking a trait toggles it,
        /// and the list opens again in the same place so the colours are up to date.
        /// </summary>
        private void OpenTraitMenu()
        {
            // One entry per step of each trait, so e.g. Slowpoke, Fast walker and Jogger are separate.
            var steps = DefDatabase<TraitDef>.AllDefsListForReading
                .SelectMany(def => TraitCondition.DegreesOf(def).Select(degree => new { def, degree }))
                .OrderBy(e => TraitCondition.DisplayName(e.def, e.degree))
                .ToList();
            var options = steps.Select(e =>
                {
                    bool selected = IsTraitSelected(e.def, e.degree);
                    string name = TraitCondition.DisplayName(e.def, e.degree);
                    string label = selected ? "<color=#00FF00>" + name + "</color>" : name;
                    return new FloatMenuOption(label, () =>
                    {
                        ToggleTrait(e.def, e.degree, selected);
                        OpenTraitMenu();
                    });
                })
                .ToList();

            traitMenu = new PlacedFloatMenu(options, traitListAnchor, centered: false)
            {
                onClose = () => traitMenuClosedAt = Time.realtimeSinceStartup,
            };
            Find.WindowStack.Add(traitMenu);
        }

        private static bool IsTraitSelected(TraitDef def, int degree)
        {
            return Settings.traitConditions.Any(t => t.traitDefName == def.defName && t.degree == degree);
        }

        private static void ToggleTrait(TraitDef def, int degree, bool selected)
        {
            if (selected)
            {
                Settings.traitConditions.RemoveAll(t => t.traitDefName == def.defName && t.degree == degree);
            }
            else
            {
                Settings.traitConditions.Add(new TraitCondition { traitDefName = def.defName, degree = degree });
            }
        }

        /// <summary>
        /// Screen position just below the right end of a button's text, moved a little further right and down.
        /// The button is in this window's coordinates, so the window's own position on screen is added.
        /// </summary>
        private static Vector2 ButtonListAnchor(Rect buttonRect, string buttonKey, float buttonHeightsBelow)
        {
            Window host = Find.WindowStack.Windows.LastOrDefault(w =>
                !(w is FloatMenu) && !(w is Window_PassionPicker));
            Vector2 origin = host != null ? host.windowRect.position : Vector2.zero;
            float textEndX = buttonRect.center.x + Text.CalcSize(buttonKey.Translate()).x / 2f;
            return origin + new Vector2(textEndX + SkillListExtraOffset,
                buttonRect.yMax + 2f + buttonHeightsBelow * buttonRect.height);
        }

        /// <summary>The game's own FloatMenu, placed at a given screen position (or centered) instead of at the mouse.</summary>
        private class PlacedFloatMenu : FloatMenu
        {
            private readonly Vector2 at;
            private readonly bool centered;

            public Action onClose;

            public PlacedFloatMenu(List<FloatMenuOption> options, Vector2 at, bool centered) : base(options)
            {
                this.at = at;
                this.centered = centered;
                vanishIfMouseDistant = false;
            }

            public override void PreOpen()
            {
                base.PreOpen();
                float w = windowRect.width;
                float h = windowRect.height;
                float x = centered ? (UI.screenWidth - w) / 2f : at.x;
                float y = centered ? (UI.screenHeight - h) / 2f : at.y;
                x = Mathf.Clamp(x, 0f, Mathf.Max(0f, UI.screenWidth - w));
                y = Mathf.Clamp(y, 0f, Mathf.Max(0f, UI.screenHeight - h));
                windowRect = new Rect(x, y, w, h);
            }

            public override void PostClose()
            {
                base.PostClose();
                onClose?.Invoke();
            }
        }

        /// <summary>Opens the game's list of choices at the mouse, as before.</summary>
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
