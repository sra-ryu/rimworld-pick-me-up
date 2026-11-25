using UnityEngine;
using Verse;

namespace PickMeUp
{
    /// <summary>
    /// The condition editor as a window you can open during play, with an optional pawn list
    /// panel on the right.
    /// </summary>
    public class Window_Conditions : Window
    {
        private const float EditorWidth = 520f;
        private const float PanelWidth = 300f;
        private const float WindowHeight = 480f;
        private const float ToggleHeight = 30f;

        // Remembered while the game runs, so reopening the window keeps the player's choice.
        private static bool showPanel = true;

        private readonly ConditionEditor editor = new ConditionEditor();
        private readonly MatchListPanel panel = new MatchListPanel();

        public override Vector2 InitialSize => new Vector2(CurrentWidth, WindowHeight);

        private static float CurrentWidth => showPanel ? EditorWidth + PanelWidth : EditorWidth;

        public Window_Conditions()
        {
            doCloseX = true;
            draggable = true;
            absorbInputAroundWindow = false; // keep the game playable behind it
            closeOnClickedOutside = false;
            preventCameraMotion = false;
            optionalTitle = "PMU_SettingsTitle".Translate().ToString();
        }

        public static void Toggle()
        {
            Window_Conditions open = Find.WindowStack.WindowOfType<Window_Conditions>();
            if (open != null)
            {
                open.Close();
            }
            else
            {
                Find.WindowStack.Add(new Window_Conditions());
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Left: the editor, with the panel toggle under it. Right: the pawn list.
            float editorWidth = EditorWidth - Margin * 2f;
            var editorRect = new Rect(inRect.x, inRect.y, editorWidth, inRect.height - ToggleHeight - 6f);
            editor.Draw(editorRect);

            var toggleRect = new Rect(inRect.x, inRect.yMax - ToggleHeight, editorWidth, ToggleHeight);
            string toggleLabel = (showPanel ? "PMU_HideList" : "PMU_ShowList").Translate();
            if (Widgets.ButtonText(toggleRect, toggleLabel))
            {
                showPanel = !showPanel;
                windowRect.width = CurrentWidth;
            }

            if (showPanel)
            {
                float x = inRect.x + editorWidth + 12f;
                panel.Draw(new Rect(x, inRect.y, inRect.xMax - x, inRect.height));
            }
        }

        public override void PostClose()
        {
            PickMeUpMod.Save();
        }
    }
}
