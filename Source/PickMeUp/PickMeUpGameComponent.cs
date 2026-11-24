using RimWorld;
using UnityEngine;
using Verse;

namespace PickMeUp
{
    /// <summary>Listens for the optional keyboard shortcuts. RimWorld creates it for every game.</summary>
    public class PickMeUpGameComponent : GameComponent
    {
        private static KeyBindingDef openKey;
        private static KeyBindingDef toggleKey;

        public PickMeUpGameComponent(Game game)
        {
        }

        public override void GameComponentOnGUI()
        {
            openKey ??= DefDatabase<KeyBindingDef>.GetNamedSilentFail("PickMeUp_OpenWindow");
            toggleKey ??= DefDatabase<KeyBindingDef>.GetNamedSilentFail("PickMeUp_ToggleMarkers");

            if (openKey != null && openKey.KeyDownEvent)
            {
                Window_Conditions.Toggle();
                Event.current.Use();
            }
            else if (toggleKey != null && toggleKey.KeyDownEvent)
            {
                PickMeUpMod.Settings.enabled = !PickMeUpMod.Settings.enabled;
                PickMeUpMod.Save();
                string key = PickMeUpMod.Settings.enabled ? "PMU_MarkersOn" : "PMU_MarkersOff";
                Messages.Message(key.Translate(), MessageTypeDefOf.SilentInput, false);
                Event.current.Use();
            }
        }
    }
}
