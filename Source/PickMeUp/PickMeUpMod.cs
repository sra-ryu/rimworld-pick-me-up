using UnityEngine;
using Verse;

namespace PickMeUp
{
    // Entry point: RimWorld instantiates every Mod subclass once at startup.
    public class PickMeUpMod : Mod
    {
        public static PickMeUpSettings Settings { get; private set; }

        private readonly ConditionEditor editor = new ConditionEditor();

        public PickMeUpMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<PickMeUpSettings>();
            Log.Message("[Pick Me Up] Loaded.");
        }

        /// <summary>Writes the settings to disk. The mod settings page does this itself when it closes.</summary>
        public static void Save() => LoadedModManager.GetMod<PickMeUpMod>()?.WriteSettings();

        public override string SettingsCategory() => "PMU_SettingsTitle".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            editor.Draw(inRect);
        }
    }
}
