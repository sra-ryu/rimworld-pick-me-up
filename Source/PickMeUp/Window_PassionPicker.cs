using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PickMeUp
{
    /// <summary>
    /// A small popup opened under the mouse with the three passion levels as flame icons.
    /// Picking one sets the condition's passion and closes the popup.
    /// </summary>
    public class Window_PassionPicker : Window
    {
        private const float RowHeight = 34f;
        private const float IconSize = 26f;
        private const float Width = 190f;

        // A Window's content area is inset by its margin on every side, so the window itself
        // must be larger than the rows, or the last row is cut off.
        private const float WindowMargin = 18f;

        private static readonly Passion[] Levels = { Passion.None, Passion.Minor, Passion.Major };

        private readonly SkillCondition condition;

        // The window that was on top when the picker was opened (the condition window or the
        // mod settings). When it closes, the picker closes with it.
        private readonly Window owner;

        public override Vector2 InitialSize =>
            new Vector2(Width + 2f * WindowMargin, Levels.Length * RowHeight + 2f * WindowMargin);

        public Window_PassionPicker(SkillCondition condition, Vector2 at)
        {
            this.condition = condition;
            owner = Find.WindowStack.Windows.LastOrDefault();
            doCloseX = false;
            closeOnClickedOutside = true;
            closeOnCancel = true;
            draggable = false;
            resizeable = false;
            absorbInputAroundWindow = false;
            preventCameraMotion = false;
            windowRect = new Rect(at.x, at.y + 4f, InitialSize.x, InitialSize.y);
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (owner != null && !Find.WindowStack.Windows.Contains(owner))
            {
                Close(false);
                return;
            }

            // Close on any click outside this window. The game's own "close when clicking outside" does
            // not work for these windows, because a click on another window moves that window to the top.
            if (Event.current.type == EventType.MouseDown && !windowRect.Contains(UI.MousePositionOnUI))
            {
                Close(false);
                return;
            }

            for (int i = 0; i < Levels.Length; i++)
            {
                Passion level = Levels[i];
                var row = new Rect(0f, i * RowHeight, inRect.width, RowHeight);

                Widgets.DrawHighlightIfMouseover(row);
                if (level == condition.passion)
                {
                    Widgets.DrawHighlightSelected(row);
                }

                var iconRect = new Rect(row.x + 6f, row.y + (RowHeight - IconSize) / 2f, IconSize, IconSize);
                Widgets.DrawTextureFitted(iconRect, IconFor(level), 1f);

                var labelRect = new Rect(iconRect.xMax + 8f, row.y, row.width - iconRect.xMax - 8f, RowHeight);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(labelRect, LabelFor(level));
                Text.Anchor = TextAnchor.UpperLeft;

                if (Widgets.ButtonInvisible(row))
                {
                    condition.passion = level;
                    Close();
                }
            }
        }

        public static Texture2D IconFor(Passion passion)
        {
            switch (passion)
            {
                case Passion.Minor: return PassionTextures.Minor;
                case Passion.Major: return PassionTextures.Major;
                default: return PassionTextures.None;
            }
        }

        private static string LabelFor(Passion passion)
        {
            switch (passion)
            {
                case Passion.Minor: return "PMU_PassionMinor".Translate();
                case Passion.Major: return "PMU_PassionMajor".Translate();
                default: return "PMU_PassionNone".Translate();
            }
        }
    }
}
