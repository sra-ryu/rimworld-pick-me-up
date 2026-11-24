using UnityEngine;
using Verse;

namespace PickMeUp
{
    /// <summary>The condition editor as a window you can open during play.</summary>
    public class Window_Conditions : Window
    {
        private readonly ConditionEditor editor = new ConditionEditor();

        public override Vector2 InitialSize => new Vector2(520f, 480f);

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
            editor.Draw(inRect);
        }

        public override void PostClose()
        {
            PickMeUpMod.Save();
        }
    }
}
